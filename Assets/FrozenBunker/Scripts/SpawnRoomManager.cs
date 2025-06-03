using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoomData
{
    public GameObject RoomObject;
    public float RotationY;
}

[RequireComponent(typeof(RoomsPool))]
public class SpawnRoomManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameRuleManager _gameRuleManager;
    protected RoomsPool _roomsPool;

    [Header("Settings")]
    [SerializeField] protected int[] countImportantZoneRooms;
    [SerializeField] private Vector3 _startPosition;
    protected int CountZoneRoomsExits;

    public Dictionary<Vector2, RoomData> _spawnedRooms = new();

    private void Start() {
        InitializingStartData();
        var startingZoneIndex = GetStartingZoneIndex();
        var startingNumFloor = GetStartingNumFloor(startingZoneIndex);
        var startingRoomPos = GetStartingPosition(startingNumFloor);
        SpawnStartingRoom(startingZoneIndex, startingRoomPos);
    }

    private void InitializingStartData()
    {
        _roomsPool = GetComponent<RoomsPool>();
        _startPosition = Vector3.up * GameEnums._roomSize;
        // -1 тк DeadEndZoneType не имеет важных комнат
        countImportantZoneRooms = new int[_roomsPool.GetCountPools - 1];
    }

    private int GetStartingNumFloor(int zoneIndex, bool random = false)
    {
        if (!random)
        {
            return GameEnums._startFloor;
        }
        var zoneFloorData = GameEnums.GetZoneFloorData;
        if (random || zoneIndex != 0)
        {
            // !!! баг !!! Нужно сделать так, что если zoneIndex != 0 но не random, то нужно использовать бд о этажах и зонах
            int[] matchingFloors = GameEnums.GetZoneFloorData
                .Where(pair => pair.y == zoneIndex)
                .Select(pair => pair.x)
                .ToArray();

            return matchingFloors[Random.Range(0, matchingFloors.Length)];
        }
        else
        {
            return GameEnums._startFloor;
        }
    }

    private Vector3 GetStartingPosition(int numFloor) {
        return new Vector3(_startPosition.x, _startPosition.y * numFloor, _startPosition.z);
    }

    private void SpawnStartingRoom(int zoneIndex, Vector3 startWorldPos)
    {
        CountZoneRoomsExits = 0;
        var availableRooms = _roomsPool.GetPoolRooms(zoneIndex);
        var startingRooms = FilterStartingRooms(availableRooms);
        if (startingRooms.Count == 0)
        {
            Debug.LogError($"No starting rooms in zone {zoneIndex}!");
            return;
        }
        var startingRoom = GetRandomRoomFromPool(startingRooms);

        var (rotation, direction) = GetRandomRotationAndDirection();
        var room = SpawnRoom(
            startingRoom,
            startWorldPos,
            rotation,
            Vector2.zero
        );

        UpdateRoomExits(direction, room);
    }

    // возможно изменить код RotateExitsClockwise, переписывает переменную 1 - 4 раза
    protected void UpdateRoomExits(int rotationSteps, GameObject room) {
        var roomManager = room.GetComponent<RoomManager>();
        var exits = roomManager.exitsFromRoom;
        
        for (int i = 0; i < rotationSteps; i++)
        {
            exits = RotateExitsClockwise(exits);
        }
        
        roomManager.exitsFromRoom = exits;
    }

    private bool[] RotateExitsClockwise(bool[] exits) {
        return new[]
        {
            exits[(int)GameEnums.Direction.West],
            exits[(int)GameEnums.Direction.North],
            exits[(int)GameEnums.Direction.East],
            exits[(int)GameEnums.Direction.South]
        };
    }

    protected GameObject SpawnRoom(GameObject room, Vector3 worldPos, Quaternion rotation, Vector2 _2DWorldPos) {
        if (_spawnedRooms.ContainsKey(_2DWorldPos)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

        ChangeSettingsInRoomManager(room, _2DWorldPos, worldPos, rotation, true);

        RoomData newRoomData = new()
        {
            RoomObject = room,
            RotationY = rotation.eulerAngles.y,
            // Loot = GenerateLoot()
        };
        AddDataToSpawnedRoom(_2DWorldPos, newRoomData); // Добавляем новую позицию в HashSet

        return room;
    }

    protected virtual void AddDataToSpawnedRoom(Vector2 _2DWorldPos, RoomData newRoomData) {
        _spawnedRooms.Add(_2DWorldPos, newRoomData);
    }

    protected virtual void ChangeSettingsInRoomManager(GameObject room, Vector2 _2DWorldPos, Vector3 worldPos, Quaternion rotation, bool isUsed) {
        room.transform.SetPositionAndRotation(worldPos, rotation);
        room.SetActive(true);

        var roomManager = room.GetComponent<RoomManager>();

        UsingRoom(roomManager, isUsed);
        roomManager._2DWorldPos = _2DWorldPos;

        UpdateAvailableExitsCount(roomManager.exitsFromRoom);
    }

    protected void UsingRoom(RoomManager roomManager, bool isUsed) {
        if (!roomManager.isUsed) {
            roomManager.isUsed = isUsed;
            if (!roomManager.AvailableForNextPoolRooms.Last()) {
                // roomManager.isUsed = isUsed;
                countImportantZoneRooms[roomManager.zoneType] -= 1;
            }
        }
        
        // if (!roomManager.isUsed && roomManager.IsImportantRoom) {
        //     roomManager.isUsed = true;
        //     countImportantZoneRooms[roomManager.zoneType] -= 1;
        // }
    }

    protected void UpdateAvailableExitsCount(bool[] exits, bool isStartRoom = false) {
        // Базовое количество добавляемых выходов (4 для стартовой комнаты, 3 для обычной)
        int baseExitsToAdd = isStartRoom ? GameEnums.XOZDirectionsCount : 3;
        
        // Подсчет стен без выходов
        int wallsWithoutExits = 0;
        foreach (var exit in exits) {if (!exit) {wallsWithoutExits++;}}
        
        // Обновление общего количества доступных выходов в зоне
        CountZoneRoomsExits += baseExitsToAdd - wallsWithoutExits;
    }

    private List<GameObject> FilterStartingRooms(List<GameObject> rooms) {
        List<GameObject> startingRooms = new();
        foreach (GameObject room in rooms) {
            if (room.GetComponent<RoomManager>().CanBeUsedAsStartingRoom)
                startingRooms.Add(room);
        }
        return startingRooms;
    }

    private Quaternion GetRotationFromSteps(int rotationSteps)
    {
        return Quaternion.Euler(0, rotationSteps * GameEnums.RotationStep, 0);
    }

    private (Quaternion, int) GetRandomRotationAndDirection() {
        int randomIndex = Random.Range(0, GameEnums.XOZDirectionsCount);
        return (GetRotationFromSteps(randomIndex), randomIndex);
    }

    private (Quaternion, int) GetRandomRotationAndDirection(List<int> validRotations)
    {
        int randomIndex = Random.Range(0, validRotations.Count);
        int rotation = validRotations[randomIndex];
        return (GetRotationFromSteps(rotation), rotation);
    }

    private int GetStartingZoneIndex(bool random = false) {
        if (random) {
            return Random.Range(0, _roomsPool.GetCountPools - 1);
        } else {
            return 0;
        }
    }

    public void SpawnAdjacentRooms(GameObject room) {
        var roomManager = room.GetComponent<RoomManager>();
        var exits = roomManager.exitsFromRoom;

        for (int i = 0; i < exits.Length; i++)
        {
            if (!exits[i])
                continue;

            var direction = (GameEnums.Direction)i;
            var (newRoomPos3D, newRoomPos2D) = CalculateAdjacentPosition(
                roomManager._2DWorldPos, 
                direction
            );

            if (RoomExists(newRoomPos2D))
                continue;

            var newRoom = CreateAdjacentRoomFromPool(
                newRoomPos3D,
                newRoomPos2D,
                roomManager.zoneType
            );

            if (newRoom != null)
            {
                ProcessTransitionRoom(newRoom);
            }
        }
    }

    private void ProcessTransitionRoom(GameObject room)
    {
        if (!room.TryGetComponent(out TransitionRoomManager _transitionManager))
            return;

        var spawnNewZoneManager = GetComponent<SpawnNewZoneManager>();
        var (success, newZoneRoomData) = spawnNewZoneManager.TryToGenerateNewZone(room, spawnNewZoneManager.playerController.PlayerPos);

        if (!success)
            ReplaceWithDeadEnd(room);
        else
            AddingDataToSpawnedRoom(newZoneRoomData);
    }

    private void AddingDataToSpawnedRoom(Dictionary<Vector2, RoomData> newZoneRoomData) {
        foreach (var pair in newZoneRoomData) {
            _spawnedRooms.Add(pair.Key, pair.Value);
        }
    }

    private void ReplaceWithDeadEnd(GameObject room) {
        var _transitionManager = room.GetComponent<TransitionRoomManager>();
        var deadEndPosition2D = _transitionManager._2DWorldPos;
        _spawnedRooms.Remove(_transitionManager._2DWorldPos);
        _transitionManager.isUsed = false;
        room.SetActive(false);

        var deadEndRoom = _roomsPool.GetPoolRooms(GameEnums.DeadEndZoneType).GetRandom();
        var (rotation, _direction) = GetRandomRotationAndDirection();
        
        SpawnRoom(
            deadEndRoom, 
            new Vector3(deadEndPosition2D.x * GameEnums._roomSize, room.GetComponent<Transform>().position.y, deadEndPosition2D.y * GameEnums._roomSize), 
            rotation, 
            deadEndPosition2D
        );
    }

    protected GameObject CreateAdjacentRoomFromPool(Vector3 position3D, Vector2 position2D, int zoneType) {
        var minNumExits = CalculateMinNumRequiredExits(zoneType);
        var requiredExits = CheckRequiredRoomType(position2D);
        var availableRooms = FilterRoomsPoolByExits(zoneType, requiredExits, minNumExits);

        if (availableRooms.Count == 0)
        {
            availableRooms = FilterRoomsPoolByExits(GameEnums.DeadEndZoneType, new int[GameEnums.XOZDirectionsCount], minNumExits);
        }

        var selectedRoom = GetRandomRoomFromPool(availableRooms);
        var (rotation, direction) = CalculateRequiredRotationAndDirection(selectedRoom, requiredExits);
        
        var room = SpawnRoom(selectedRoom, position3D, rotation, position2D);
        if (room == null) return null;

        UpdateRoomExits(direction, room);

        return room;
    }

    protected GameObject GetRandomRoomFromPool(List<GameObject> availableRooms) {
        return availableRooms[Random.Range(0, availableRooms.Count)];
    }

    protected (Quaternion, int) CalculateRequiredRotationAndDirection(GameObject room, int[] requiredRoomType) {
        var roomManager = room.GetComponent<RoomManager>();
        bool[] roomExits = roomManager.exitsFromRoom;
        
        var validRotations = FindValidRotations(roomExits, requiredRoomType);
        
        return validRotations.Count > 0 
            ? GetRandomRotationAndDirection(validRotations) 
            : DefaultRotation();
    }

    private (Quaternion, int) DefaultRotation() => 
        (Quaternion.identity, 0);

    private List<int> FindValidRotations(bool[] roomExits, int[] requiredExits)
    {
        var validRotations = new List<int>();
        
        for (int rotation = 0; rotation < GameEnums.XOZDirectionsCount; rotation++)
        {
            if (CheckExitsMatch(roomExits, rotation, requiredExits))
            {
                validRotations.Add(rotation);
            }
        }
        
        return validRotations;
    }

    protected List<GameObject> FilterRoomsPoolByExits(int zoneIndex, int[] requiredExits, int minNumExits)
    {
        var roomsPool = _roomsPool.GetPoolRooms(zoneIndex);
        var availableRoomsPool = new List<GameObject>();
        
        foreach (var room in roomsPool)
        {
            if (zoneIndex == -1 && !room.activeSelf) {
                availableRoomsPool.Add(room);
                continue;
            }
            
            if (!room.GetComponent<RoomManager>().isUsed && CheckRoomCompatibility(room, requiredExits, minNumExits))
            {
                availableRoomsPool.Add(room);
            }
        }
        return availableRoomsPool;
    }

    private bool CheckRoomCompatibility(GameObject room, int[] requiredExits, int minNumExits)
    {
        var exits = room.GetComponent<RoomManager>().exitsFromRoom;
        
        for (int rotation = 0; rotation < GameEnums.XOZDirectionsCount; rotation++)
        {
            if (CheckExitsMatch(exits, rotation, requiredExits, minNumExits))
                return true;
        }
        return false;
    }

    private bool CheckExitsMatch(bool[] roomExits, int rotation, int[] requiredRoomType, int minNumExits = 0) {
        int exitCount = 0;
        bool meetsRequirements = true;

        for (int direction = 0; direction < GameEnums.XOZDirectionsCount; direction++)
        {
            int requirement = requiredRoomType[direction];
            bool hasExit = GetRotatedExit(roomExits, direction, rotation);

            if (!CheckRequirement(requirement, hasExit))
            {
                meetsRequirements = false;
                break;
            }

            if (hasExit) exitCount++;
        }

        return meetsRequirements && exitCount >= minNumExits;
    }

    /// <summary>
    /// Получает состояние выхода с учетом поворота комнаты
    /// </summary>
    private bool GetRotatedExit(bool[] roomExits, int direction, int rotation)
    {
        int rotatedDirection = (direction + (GameEnums.XOZDirectionsCount - rotation)) % GameEnums.XOZDirectionsCount;
        return roomExits[rotatedDirection];
    }

    /// <summary>
    /// Проверяет соответствие выхода требованиям
    /// </summary>
    private bool CheckRequirement(int requirement, bool hasExit)
    {
        return requirement switch
        {
            1 => hasExit,       // Должен быть выход
            -1 => !hasExit,     // Не должно быть выхода
            -2 => false,        // Специальное требование (стена)
            _ => true           // 0 или другое - не проверяем
        };
    }

    protected int[] CheckRequiredRoomType(Vector2 position)
    {
        var requiredRoomType = new int[GameEnums.XOZDirectionsCount];
        
        for (int i = 0; i < GameEnums.XOZDirectionsCount; i++)
        {
            requiredRoomType[i] = GetExitRequirement(position, (GameEnums.Direction)i);
        }
        
        return requiredRoomType;
    }

    protected int GetExitRequirement(Vector2 position, GameEnums.Direction direction)
    {
        var adjacentPos = GetAdjacentPosition(position, direction);
        var (isGetValue, data) = TryGetRoomData(adjacentPos);
        if (!isGetValue)
            return 0;

        var adjacentRoomManager = 
            data.RoomObject.TryGetComponent(out TransitionRoomManager transitionRoomManager) 
            ? transitionRoomManager : data.RoomObject.GetComponent<RoomManager>();
        var oppositeDirection = GetOppositeDirection(position - adjacentPos);
        
        return adjacentRoomManager.exitsFromRoom[(int)oppositeDirection] ? 1 : -1;
    }

    protected virtual (bool, RoomData) TryGetRoomData(Vector2 pos) {
        bool isGetValue = _spawnedRooms.TryGetValue(pos, out RoomData data);
        return (isGetValue, data);
    }

    protected GameEnums.Direction GetOppositeDirection(Vector2 offset) => offset switch
    {
        { y: -1 } => GameEnums.Direction.East,
        { y: 1 } => GameEnums.Direction.West,
        { x: 1 } => GameEnums.Direction.North,
        { x: -1 } => GameEnums.Direction.South,
    };

    protected Vector2 GetAdjacentPosition(Vector2 position, GameEnums.Direction direction) {
        return position + GetDirectionOffset(direction);
    }

    protected int CalculateMinNumRequiredExits(int zoneType) {
        if (zoneType == -1)
            return 1;
        return (CountZoneRoomsExits > 1 || countImportantZoneRooms[zoneType] < 2) ? 1 : 2;
    }

    private bool RoomExists(Vector2 position) {
        var (isRoomGet, roomData) = TryGetRoomData(position);
        if (!isRoomGet) {return isRoomGet;}

        GameObject room = roomData.RoomObject;

        room.transform.SetPositionAndRotation(
            new Vector3(position.x * GameEnums._roomSize, GameEnums._startFloor, position.y * GameEnums._roomSize),
            Quaternion.Euler(0, roomData.RotationY, 0)
        );
        room.SetActive(true);
        RoomManager roomManager = room.GetComponent<RoomManager>(); 
        roomManager.isUsed = true;
        roomManager._2DWorldPos = position;

        return isRoomGet;
    }

    protected (Vector3, Vector2) CalculateAdjacentPosition(Vector2 parentRoomPosition, GameEnums.Direction direction) {
        var offset = GetDirectionOffset(direction);
        var position2D = parentRoomPosition + offset;
        var position3D = new Vector3(
            position2D.x * GameEnums._roomSize, 
            GameEnums._startFloor * GameEnums._roomSize, 
            position2D.y * GameEnums._roomSize
        );
        
        return (position3D, position2D);
    }

    private Vector2 GetDirectionOffset(GameEnums.Direction direction) => direction switch {
        GameEnums.Direction.North => Vector2.right,
        GameEnums.Direction.East => Vector2.down,
        GameEnums.Direction.South => Vector2.left,
        GameEnums.Direction.West => Vector2.up,
        _ => Vector2.right
    };

    public void DespawnOldAdjacentRooms(Vector2 oldPlayerPos, Vector2 newPlayerPos)
    {
        Vector2 playerPosChangeValue = newPlayerPos - oldPlayerPos;

        for (int i = 0; i < GameEnums.XOZDirectionsCount + GameEnums.OYDirectionsCount; i++)
        {
            Vector2 direction2D = GetDirectionOffset((GameEnums.Direction)i);
            if (direction2D == playerPosChangeValue) {
                continue;
            }
            Vector2 adjacentRoomPos2D = oldPlayerPos + direction2D;
            
            TryDespawnRoom(adjacentRoomPos2D);
        }
    }

    private void TryDespawnRoom(Vector2 roomPos2D) {
        if (!_spawnedRooms.ContainsKey(roomPos2D)) {
            return;
        }
        _spawnedRooms.TryGetValue(roomPos2D, out RoomData roomData);
        GameObject adjacentRoom = roomData.RoomObject;
        var roomManager = 
            adjacentRoom.TryGetComponent(out TransitionRoomManager transitionRoomManager) 
            ? transitionRoomManager : adjacentRoom.GetComponent<RoomManager>();

        if (roomManager.canUsedInfinitely && 
                (_gameRuleManager.IsInfinityGame || 
                roomManager.AvailableForNextPoolRooms.AsEnumerable().Last())) {
            roomManager.isUsed = false;
        }
        adjacentRoom.SetActive(false);
    }
}
