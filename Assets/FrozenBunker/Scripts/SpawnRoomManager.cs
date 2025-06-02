using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Unity.VisualScripting;

public enum Direction
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
    Up = 4,
    Down = 5
}

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
    
    [SerializeField] private int _startFloor = -1;
    [SerializeField] private Vector3 _startPosition = Vector3.up * 25;

    // !!! баг !!! Добавить проверку, чтобы спавнилась только та переходная зона, 
    // которая ведёт к доступной для этажа зоне
    // zoneIndex - numFloor
    [SerializeField] private Vector2Int[] zoneFloorData = new Vector2Int[]
    {
        new(1, 2),
        new(1, 4),

        new(0, 0),
        new(0, 6),
        new(0, 7),

        new(-1, 0),
        new(-1, 1),
        new(-1, 3),
        new(-1, 8),

        new(-2, 5),

        new(-3, 9)
    };

    private int _totalZones = 10;
    protected float _roomSize = 25f;
    protected int CountZoneRoomsExits;

    protected Dictionary<Vector3, RoomData> _spawnedRoomsData = new();
    public Dictionary<Vector3, RoomData> SpawnedRoomsData { get { return _spawnedRoomsData; } }

    private const float RotationStep = 90f;
    protected const int DirectionsCount = 4;
    protected const int AdditionalDirCount = 2;
    protected const int DeadEndZoneType = -1;

    private void Start() {
        _roomsPool = GetComponent<RoomsPool>();
        var startingZoneIndex = GetStartingZoneIndex();
        var startingNumFloor = GetStartingNumFloor(startingZoneIndex);
        var startingRoomPos = GetStartingPosition(startingNumFloor);
        SpawnStartingRoom(startingZoneIndex, startingRoomPos);
    }

    private void SpawnStartingRoom(int zoneIndex, Vector3 startWorldPos) {
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
            rotation
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
            exits[(int)Direction.West],
            exits[(int)Direction.North],
            exits[(int)Direction.East],
            exits[(int)Direction.South]
        };
    }

    protected virtual GameObject SpawnRoom(GameObject room, Vector3 worldPos, Quaternion rotation) {
        if (TryGetRoomData(worldPos, out RoomData _roomData)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

        ChangeSettingsInRoomManager(room, worldPos, rotation, true);

        RoomData newRoomData = new()
        {
            RoomObject = room,
            RotationY = rotation.eulerAngles.y,
            // Loot = GenerateLoot()
        };
        AddDataToSpawnedRoom(worldPos, newRoomData); // Добавляем новую позицию в HashSet

        UpdateAvailableExitsCount(room);
        return room;
    }

    protected virtual void AddDataToSpawnedRoom(Vector3 worldPos, RoomData newRoomData) {
        _spawnedRoomsData.Add(
            new Vector3(worldPos.x, worldPos.y, worldPos.z) / _roomSize, 
            newRoomData
        );
    }

    protected virtual void ChangeSettingsInRoomManager(GameObject room, Vector3 worldPos, Quaternion rotation, bool isUsed) {
        room.transform.SetPositionAndRotation(worldPos, rotation);
        room.SetActive(true);

        var roomManager = room.GetComponent<RoomManager>();

        UsingRoom(roomManager, isUsed);
        roomManager.LocalPos = new Vector3(worldPos.x, worldPos.y, worldPos.z) / _roomSize;
    }

    protected void UsingRoom(RoomManager roomManager, bool isUsed) {
        if (!roomManager.isUsed) {
            roomManager.isUsed = isUsed;
            if (!roomManager.AvailableForNextPoolRooms.Last() && roomManager.IsImportantRoom) {
                // roomManager.isUsed = isUsed;
                _roomsPool.SubtractCountImportantRoomInZone(roomManager.zoneType);
            }
        }
        
        // if (!roomManager.isUsed && roomManager.IsImportantRoom) {
        //     roomManager.isUsed = true;
        //     countImportantZoneRooms[roomManager.zoneType] -= 1;
        // }
    }

    /// <summary>
    /// Обновляет число выходов из зоны
    /// </summary>
    /// <param name="room"></param>
    /// <param name="isStartRoom"></param>
    protected void UpdateAvailableExitsCount(GameObject room, bool isStartRoom = false)
    {
        var roomManager = room.GetComponent<RoomManager>();

        if (roomManager.AvailableForNextPoolRooms.Last()
            || roomManager.IsRoomTransitional )
        {
            SubtractFromRoomsCount(roomManager);
        }

        AddToRoomsCount(roomManager, isStartRoom);
    }

    private void SubtractFromRoomsCount(RoomManager roomManager) {
        for (int i = 0; i < roomManager.exitsFromRoom.Length; i++)
        {
            var direction = (Direction)i;
            var worldPos = CalculateAdjacentPosition(
                roomManager.LocalPos, 
                direction
            );

            if (RoomExists(worldPos))
                CountZoneRoomsExits--;
        }
    }

    private void AddToRoomsCount(RoomManager roomManager, bool isStartRoom = false) {
        for (int i = 0; i < roomManager.exitsFromRoom.Length; i++)
        {
            // если у комнаты есть выход
            if (!roomManager.exitsFromRoom[i])
            {
                continue;
            }

            // если выход направлен не на другую комнату
            var direction = (Direction)i;
            var worldPos = CalculateAdjacentPosition(
                roomManager.LocalPos,
                direction
            );
            if (RoomExists(worldPos))
                CountZoneRoomsExits--;
            else
                CountZoneRoomsExits++;
        }
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
        return Quaternion.Euler(0, rotationSteps * RotationStep, 0);
    }

    private (Quaternion, int) GetRandomRotationAndDirection() {
        int randomIndex = Random.Range(0, DirectionsCount);
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
            return Random.Range(0, _totalZones);
        } else {
            return 0;
        }
    }

    private Vector3 GetStartingPosition(int numFloor)
    {
        return new Vector3(_startPosition.x, _startPosition.y * numFloor, _startPosition.z);
    }

    private int GetStartingNumFloor(int zoneIndex, bool random = false)
    {
        if (random || zoneIndex != 0)
        {
            // !!! баг !!! Нужно сделать так, что если zoneIndex != 0 но не random, то нужно использовать бд о этажах и зонах
            int[] matchingFloors = zoneFloorData
                .Where(pair => pair.y == zoneIndex)
                .Select(pair => pair.x)
                .ToArray();

            return matchingFloors[Random.Range(0, matchingFloors.Length)];
        }
        else
        {
            return _startFloor;
        }
    }

    public void SpawnAdjacentRooms(GameObject room)
    {
        var roomManager = room.GetComponent<RoomManager>();
        var exits = roomManager.exitsFromRoom;

        for (int i = 0; i < exits.Length; i++)
        {
            if (!exits[i])
                continue;

            var direction = (Direction)i;
            var worldPos = CalculateAdjacentPosition(
                roomManager.LocalPos,
                direction
            );

            if (RoomExists(worldPos))
                continue;

            var newRoom = CreateAdjacentRoomFromPool(
                worldPos,
                roomManager.zoneType
            );

            if (newRoom != null)
            {
                ProcessTransitionRoom(newRoom);
            }

            Debug.Log("Count RoomPool: " + _spawnedRoomsData.Count);
        }

        // Проверка: если игрок в переходной комнате типа лифт или лестница, то отображаются соответствующие переходные шахты
        for (int i = 4; i <= 5; i++)
        {
            var direction = (Direction)i;
            var worldPos = CalculateAdjacentPosition(
                roomManager.LocalPos,
                direction
            );

            if (RoomExists(worldPos))
                continue;
        }
    }

    private void ProcessTransitionRoom(GameObject room)
    {
        if (!room.TryGetComponent(out TransitionRoomManager _transitionManager))
            return;
        
        if (_roomsPool.LinearTransitionalTypes.Contains(_transitionManager.TypeTransitionRoom)) {
            TrySpawnLinearTransitionalRoom(room);
        } else if (_roomsPool.LiftTransitionalTypes.Contains(_transitionManager.TypeTransitionRoom)) {
            TrySpawnLiftTransitionalRoom(room);
        } else if (_roomsPool.LadderTransitionalTypes.Contains(_transitionManager.TypeTransitionRoom)) {
            TrySpawnLadderTransitionalRoom(room);
        }
    }

    private void TrySpawnLinearTransitionalRoom(GameObject room) {
        var spawnNewZoneManager = GetComponent<LinearTransitionSpawnZoneManager>();
        var (isNewZoneGenerated, newZoneRoomData) = spawnNewZoneManager.TryToGenerateNewZone(room, spawnNewZoneManager.playerController.PlayerPos);
        if (isNewZoneGenerated)
        {
            AddAllDatasToSpawnedRoom(newZoneRoomData);
        }
        else
        {
            ReplaceWithDeadEnd(room);
        }
    }

    // Без модификаций, на этаже имеется всего одна переходная комната типа "Lift" и "Ladder"
    // Уникальная механика переходной комнаты типа "Lift": если комнату заспавнить до нужного этажа не получается, 
    // то комната НЕ заменяется на deadEndRoom. 
    // Комната становится комнатой телепортом в другую сцену которая хранит информацию о другой зоне.
    private void TrySpawnLiftTransitionalRoom(GameObject room)
    {
        var (isLiftGenerated, newZoneRoomData) = GetComponent<NotLinearTransitionSpawnZoneManager>().TryToGenerateLift(room);
        if (isLiftGenerated)
        {
            AddAllDatasToSpawnedRoom(newZoneRoomData);
        }
        else
        {
            GenNewZoneInNewScene(room);
        }
    }

    // 1 лестница ведёт до -2 и -3 этажа
    // Без модификаций, на этаже имеется всего одна переходная комната типа "Lift" и "Ladder"
    private void TrySpawnLadderTransitionalRoom(GameObject room)
    {
        var (isLiftGenerated, newZoneRoomData) = GetComponent<NotLinearTransitionSpawnZoneManager>().TryToGenerateLadder(room);
        if (isLiftGenerated)
        {
            AddAllDatasToSpawnedRoom(newZoneRoomData);
        }
        else
        {
            GenNewZoneInNewScene(room);
        }
    }

    private void GenNewZoneInNewScene(GameObject room) {}

    private void AddAllDatasToSpawnedRoom(Dictionary<Vector3, RoomData> newZoneRoomData)
    {
        foreach (var pair in newZoneRoomData)
        {
            _spawnedRoomsData.Add(pair.Key, pair.Value);
        }
    }

    private void ReplaceWithDeadEnd(GameObject room) {
        var _transitionManager = room.GetComponent<TransitionRoomManager>();
        var deadEndLocalPos = _transitionManager.LocalPos;
        _spawnedRoomsData.Remove(_transitionManager.LocalPos);
        _transitionManager.isUsed = false;
        room.SetActive(false);

        var deadEndRoom = GetRandomRoomFromPool(_roomsPool.GetPoolRooms(DeadEndZoneType));
        var (rotation, _direction) = GetRandomRotationAndDirection();
        
        SpawnRoom(
            deadEndRoom, 
            new Vector3(deadEndLocalPos.x * _roomSize, deadEndLocalPos.y * _roomSize * 2, deadEndLocalPos.y * _roomSize), 
            rotation
        );
    }

    protected GameObject CreateAdjacentRoomFromPool(Vector3 worldPos, int zoneType) {
        var minNumExits = CalculateMinNumRequiredExits(zoneType);
        var requiredExits = CheckRequiredRoomType(worldPos, zoneType);
        var availableRooms = FilterRoomsPoolByExits(zoneType, requiredExits, minNumExits);
        // Debug.Log("AvailableRooms count: " + availableRooms.Count);

        if (availableRooms.Count == 0)
        {
            availableRooms = FilterRoomsPoolByExits(DeadEndZoneType, new int[DirectionsCount], minNumExits);
        }

        var selectedRoom = GetRandomRoomFromPool(availableRooms);
        var (rotation, direction) = CalculateRequiredRotationAndDirection(selectedRoom, requiredExits);
        
        var room = SpawnRoom(selectedRoom, worldPos, rotation);
        if (room == null) return null;

        UpdateRoomExits(direction, room);

        return room;
    }

    protected GameObject GetRandomRoomFromPool(List<GameObject> availableRooms) {
        // Debug.Log("Num rooms in zone: " + availableRooms.Count);
        return availableRooms[Random.Range(0, availableRooms.Count)];
    }

    protected (Quaternion, int) CalculateRequiredRotationAndDirection(GameObject room, int[] requiredRoomType) {
        var roomManager = room.GetComponent<RoomManager>();
        bool[] roomExits = roomManager.exitsFromRoom;
        
        var validRotations = roomManager.zoneType != DeadEndZoneType ? FindValidRotations(roomExits, requiredRoomType) : new List<int> { 0 };
        return GetRandomRotationAndDirection(validRotations);
    }

    private List<int> FindValidRotations(bool[] roomExits, int[] requiredExits)
    {
        var validRotations = new List<int>();
        
        for (int rotation = 0; rotation < DirectionsCount; rotation++)
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

        // Debug.Log("NumExits: " + minNumExits);
        // Debug.Log("ReqExits: " + requiredExits[0] + ", " + requiredExits[1] + ", " + requiredExits[2] + ", " + requiredExits[3]);

        foreach (var room in roomsPool)
        {
            if (zoneIndex == DeadEndZoneType && !room.GetComponent<RoomManager>().isUsed)
            {
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

        for (int rotation = 0; rotation < DirectionsCount; rotation++)
        {
            if (CheckExitsMatch(exits, rotation, requiredExits, minNumExits))
            {
                // Debug.Log("Exits: " + exits[0] + ", " + exits[1] + ", " + exits[2] + ", " + exits[3]);
                return true;
            }
        }
        return false;
    }

    private bool CheckExitsMatch(bool[] roomExits, int rotation, int[] requiredRoomType, int minNumExits = 0) {
        int exitCount = 0;
        bool meetsRequirements = true;

        for (int direction = 0; direction < DirectionsCount; direction++)
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
        int rotatedDirection = (direction + (DirectionsCount - rotation)) % DirectionsCount;
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

    protected int[] CheckRequiredRoomType(Vector3 worldPos, int indexCheckedZone)
    {
        var requiredRoomType = new int[DirectionsCount];
        for (int direction = 0; direction < DirectionsCount; direction++)
        {
            requiredRoomType[direction] = GetExitRequirement(worldPos, (Direction)direction, indexCheckedZone);
        }
        
        return requiredRoomType;
    }

    protected int GetExitRequirement(Vector3 worldPos, Direction direction, int indexCheckedZone)
    {
        var adjacentWorldPos = GetAdjacentPosition(worldPos, direction);
        // Debug.Log("Adj room pos    : " + adjacentWorldPos.x + ", " + adjacentWorldPos.y + ", " + adjacentWorldPos.z);
        if (!TryGetRoomData(adjacentWorldPos, out RoomData roomData))
        {
            return 0;
        }

        var adjacentRoomManager = roomData.RoomObject.GetComponent<RoomManager>();
        var oppositeDirection = GetOppositeDirection(new Vector2(worldPos.x - adjacentWorldPos.x, worldPos.z - adjacentWorldPos.z) / _roomSize);
        
        // return adjacentRoomManager.exitsFromRoom[(int)oppositeDirection] ? 1 : -1;
        var returningValue = adjacentRoomManager.exitsFromRoom[(int)oppositeDirection] ? 1 : -1;
        return (returningValue != -1
                && adjacentRoomManager.zoneType != indexCheckedZone
                && !adjacentRoomManager.IsRoomTransitional) 
            ? -2 : returningValue;
    }

    // В SpawnNewZoneManager мы override эту функцию, пытаемся получить значение не только
    // из _spawnedRoomsData, но и из _newSpawnedRooms
    protected virtual bool TryGetRoomData(Vector3 worldPos, out RoomData data) {
        bool isGetValue = _spawnedRoomsData.TryGetValue(worldPos / _roomSize, out RoomData roomData);
        data = roomData;
        
        return isGetValue;
    }

    protected Direction GetOppositeDirection(Vector2 offset) => offset switch
    {
        { y: -1 } => Direction.East,
        { y: 1 } => Direction.West,
        { x: 1 } => Direction.North,
        { x: -1 } => Direction.South
    };

    protected Vector3 GetAdjacentPosition(Vector3 worldPos, Direction direction) {
        return worldPos + GetDirectionOffset(direction) * _roomSize;
    }

    protected virtual int CalculateMinNumRequiredExits(int zoneType) {
        if (zoneType == DeadEndZoneType)
            return 1;
        return (CountZoneRoomsExits > 2) ? 1 : 2;
    }

    private bool RoomExists(Vector3 worldPos) {
        if (!TryGetRoomData(worldPos, out RoomData roomData)) {
            return false;
        }
        
        GameObject room = roomData.RoomObject;

        room.transform.SetPositionAndRotation(
            worldPos,
            Quaternion.Euler(0, roomData.RotationY, 0)
        );

        room.SetActive(true);
        RoomManager roomManager = room.GetComponent<RoomManager>(); 
        roomManager.isUsed = true;
        roomManager.LocalPos = new Vector3(worldPos.x , worldPos.y , worldPos.z) / _roomSize;

        if (roomManager.zoneType == DeadEndZoneType)
        {
            Debug.Log("Get DeadEndRoom from Pool");
        }

        return true;
    }

    protected Vector3 CalculateAdjacentPosition(Vector3 parentRoomPosition, Direction direction) {
        var offset = GetDirectionOffset(direction);
        var worldPos = new Vector3(
            (parentRoomPosition.x + offset.x) * _roomSize, 
            (parentRoomPosition.y + offset.y) * _roomSize, 
            (parentRoomPosition.z + offset.z) * _roomSize
        );
        
        return worldPos;
    }

    private Vector3 GetDirectionOffset(Direction direction) => direction switch {
        Direction.North => Vector3.right,
        Direction.East => Vector3.back,
        Direction.South => Vector3.left,
        Direction.West => Vector3.forward,
        Direction.Up => Vector3.up,
        Direction.Down => Vector3.down,
        // _ => Vector3.zero
    };

    public void DespawnOldAdjacentRooms(Vector3 oldPlayerPos, Vector3 newPlayerPos)
    {
        Vector3 playerPosChangeValue = newPlayerPos - oldPlayerPos;

        for (int direction = 0; direction < DirectionsCount + AdditionalDirCount; direction++)
        {
            Vector3 dirVector = GetDirectionOffset((Direction)direction);
            if (dirVector == playerPosChangeValue)
            {
                continue;
            }
            Vector3 adjacentRoomPos = oldPlayerPos + dirVector;

            TryDespawnRoom(adjacentRoomPos);
        }
    }

    private void TryDespawnRoom(Vector3 roomPos) {
        if (!TryGetRoomData(roomPos * _roomSize, out RoomData roomData)) {
            // Debug.Log("Cant despawnRoom! Pos: [" + roomPos.x + ", " + roomPos.y + ", " + roomPos.z + "]");
            return;
        }
        Debug.Log("DespawnRoom on Pos: [" + roomPos.x + ", " + roomPos.y + ", " + roomPos.z + "]");
        GameObject adjacentRoom = roomData.RoomObject;
        var roomManager = adjacentRoom.GetComponent<RoomManager>();

        if (roomManager.canUsedInfinitely && 
                (_gameRuleManager.IsInfinityGame
                || roomManager.zoneType == DeadEndZoneType)) {
            roomManager.isUsed = false;
        }
        adjacentRoom.SetActive(false);
    }
}
