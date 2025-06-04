using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// INHERITANCE
[RequireComponent(typeof(SpawnRoomManager))]
public class SpawnNewZoneManager : SpawnRoomManager
{
    private int countStartedImportantRooms;
    private Dictionary<Vector3, RoomData> _generalSpawnedRooms;
    private Dictionary<Vector3, GameObject> deadEndRoomsUpdater;
    public PlayerController playerController; // изменить тип безопасности

    private void Start() {
        InitializingStartData();
    }

    protected override void InitializingStartData()
    {
        _roomsPool = GetComponent<RoomsPool>();
    }

    private void InitializingData()
    {
        _spawnedRooms = new();
        deadEndRoomsUpdater = new();
        _generalSpawnedRooms = GetComponent<SpawnRoomManager>()._spawnedRooms;
    }

    public void AddImportantRoomToCounter(int indexNewZone)
    {
        if (indexNewZone == 10) { return; }
        countImportantZoneRooms[indexNewZone] += 1;
    }

    public (bool, Dictionary<Vector3, RoomData>) TryToGenerateNewZone(GameObject transitionRoom, Vector3 oldPlayerPos) {
        InitializingData();

        countStartedImportantRooms = 
            countImportantZoneRooms[transitionRoom.GetComponent<TransitionRoomManager>().TransitionToZone]; // new
        bool isNewZoneGenerated = RecursiveZoneGeneration(transitionRoom, oldPlayerPos);

        return (isNewZoneGenerated, _spawnedRooms);
    }

    // !!! имеется баг !!! почему то имеет возможность в новой зоне заспавнить одну и ту же DeadEndRoom
    private bool RecursiveZoneGeneration(GameObject parentRoom, Vector3 oldPos)
    {
        var roomManager = parentRoom.GetComponent<RoomManager>();
        var indexNewZone = TryGetIndex(parentRoom);

        if (indexNewZone == -1) { return false; }

        for (int dir = 0; dir < GameEnums.XOZDirectionsCount; dir++)
        {
            var newWorldPos = GetAdjacentPosition(
                parentRoom.transform.position,
                (GameEnums.Direction)dir
            );

            if (ShouldSkipPosition(newWorldPos, oldPos, roomManager.exitsFromRoom[dir]))
                continue;

            ProcessRoomCreation(indexNewZone, newWorldPos, deadEndRoomsUpdater);
        }

        return countStartedImportantRooms != countImportantZoneRooms[indexNewZone];
    }

    // !!!!! нужно облегчить функцию уменьшив количество вызовол TryGetComponent
    private int TryGetIndex(GameObject room) {
        if (room.TryGetComponent(out TransitionRoomManager transitionRoomManager)) {
            return transitionRoomManager.TransitionToZone;
        } else {
            return room.GetComponent<RoomManager>().zoneType;
        }
    }

    private void ProcessRoomCreation(int zoneType, Vector3 worldPos, Dictionary<Vector3, GameObject> deadEndRoomsUpdater) {
        var newRoom = CreateAdjacentRoomFromPool(worldPos, zoneType);

        if (newRoom.GetComponent<RoomManager>().zoneType == GameEnums.DeadEndZoneType)
        {
            deadEndRoomsUpdater.Add(worldPos, newRoom);
        }

        RecursiveZoneGeneration(newRoom, worldPos);
    }
    
    protected override GameObject CreateAdjacentRoomFromPool(Vector3 worldPos, int zoneType) {
        var minNumExits = CalculateMinNumRequiredExits(zoneType);
        var requiredExits = CheckRequiredRoomType(worldPos, zoneType);
        var availableRooms = FilterRoomsPoolByExits(zoneType, requiredExits, minNumExits, worldPos);

        if (availableRooms.Count == 0)
        {
            availableRooms = FilterRoomsPoolByExits(GameEnums.DeadEndZoneType, new int[GameEnums.XOZDirectionsCount], minNumExits, worldPos);
        }

        var selectedRoom = availableRooms.GetRandom();
        var (rotation, direction) = CalculateRequiredRotationAndDirection(selectedRoom, requiredExits);
        
        var room = SpawnRoom(selectedRoom, worldPos, rotation);
        if (room == null) return null;

        UpdateRoomExits(direction, room);

        return room;
    }

    private List<GameObject> FilterRoomsPoolByExits(int zoneIndex, int[] requiredExits, int minNumExits, Vector3 worldPos)
    {
        var roomsPool = _roomsPool.GetPoolRooms(zoneIndex);
        var availableRoomsPool = new List<GameObject>();

        foreach (var room in roomsPool)
        {
            var roomManager = room.GetComponent<RoomManager>();
            if (zoneIndex == GameEnums.DeadEndZoneType)
            {
                TryAddDeadEndRoomToList(worldPos, room, availableRoomsPool);
                continue;
            }

            if (!roomManager.isUsed && CheckRoomCompatibility(room, requiredExits, minNumExits))
            {
                availableRoomsPool.Add(room);
            }
        }
        return availableRoomsPool;
    }

    private void TryAddDeadEndRoomToList(Vector3 worldPos, GameObject room, List<GameObject> availableRoomsPool)
    {
        if (CheckRoomAvailable(worldPos, room)) {
            availableRoomsPool.Add(room);
        }
    }

    private bool CheckRoomAvailable(Vector3 worldPos, GameObject room)
    {
        var isRoomAvailable = true;

        for (int dir = 0; dir < GameEnums.XOZDirectionsCount; dir++)
        {
            var checkedWorldPos = GetAdjacentPosition(worldPos, (GameEnums.Direction)dir);

            if (deadEndRoomsUpdater.TryGetValue(checkedWorldPos, out GameObject checkedRoom) && checkedRoom == room)
            {
                isRoomAvailable = false;
            }
        }

        return isRoomAvailable;
    }

    private int GetExitRequirement(Vector3 worldPos, GameEnums.Direction direction, int indexCheckedZone)
    {
        var adjacentPos = GetAdjacentPosition(worldPos, direction);
        var (isGetValue, data) = TryGetRoomData(adjacentPos);
        if (!isGetValue)
            return 0;

        var adjacentRoomManager = data.RoomObject.GetComponent<RoomManager>();
        var oppositeDirection = GetDirectionFromOffset(worldPos - adjacentPos);


        var returningValue = adjacentRoomManager.exitsFromRoom[(int)oppositeDirection] ? 1 : -1;
        return (returningValue != -1 &&
            adjacentRoomManager.zoneType != indexCheckedZone &&
            !adjacentRoomManager.IsRoomTransitional)
            ? -2 : returningValue;
    }

    private int[] CheckRequiredRoomType(Vector3 worldPos, int indexCheckedZone)
    {
        var requiredRoomType = new int[GameEnums.XOZDirectionsCount];
        
        for (int i = 0; i < GameEnums.XOZDirectionsCount; i++)
        {
            requiredRoomType[i] = GetExitRequirement(worldPos, (GameEnums.Direction)i, indexCheckedZone);
        }
        
        return requiredRoomType;
    }
    
    // POLYMORPHISM
    protected override void ChangeSettingsInRoomManager(GameObject room, Vector3 worldPos, Quaternion rotation, bool isUsed)
    {
        room.transform.SetPositionAndRotation(worldPos, rotation);

        var roomManager = room.GetComponent<RoomManager>();

        if (roomManager.zoneType == GameEnums.DeadEndZoneType) { isUsed = false; }
        UsingRoom(roomManager, isUsed);

        UpdateAvailableExitsCount(roomManager.exitsFromRoom);
    }

    protected override (bool, RoomData) TryGetRoomData(Vector3 worldPos)
    {
        bool isGetValue = _generalSpawnedRooms.TryGetValue(worldPos, out RoomData data)
            || _spawnedRooms.TryGetValue(worldPos, out data);
        return (isGetValue, data);
    }


    private bool ShouldSkipPosition(Vector3 newWorldPos, Vector3 oldPlayerPos, bool hasExit)
    {
        return !hasExit || 
            newWorldPos == oldPlayerPos || 
            _generalSpawnedRooms.ContainsKey(newWorldPos) || 
            _spawnedRooms.ContainsKey(newWorldPos);
    }
}
