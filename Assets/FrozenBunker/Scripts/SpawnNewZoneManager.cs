using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// INHERITANCE
[RequireComponent(typeof(SpawnRoomManager))]
public class SpawnNewZoneManager : SpawnRoomManager
{
    private int countStartedImportantRooms;
    private Dictionary<Vector3, RoomData> _newSpawnedRooms;
    public PlayerController playerController; // изменить тип безопасности

    private void Start() {
        _roomsPool = GetComponent<RoomsPool>();
        _spawnedRooms = GetComponent<SpawnRoomManager>()._spawnedRooms;
    }

    public void AddImportantRoomToCounter(int indexNewZone) {
        if (indexNewZone == 10) {return;}
        countImportantZoneRooms[indexNewZone] += 1;
    }

    public (bool, Dictionary<Vector3, RoomData>) TryToGenerateNewZone(GameObject transitionRoom, Vector3 oldPlayerPos) {
        _newSpawnedRooms = new();
        countStartedImportantRooms = 
            countImportantZoneRooms[transitionRoom.GetComponent<TransitionRoomManager>().TransitionToZone]; // new
        bool isNewZoneGenerated = RecursiveZoneGeneration(transitionRoom, oldPlayerPos);
        return (isNewZoneGenerated, _newSpawnedRooms);
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

            ProcessRoomCreation(indexNewZone, newWorldPos, roomManager);
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

    private void ProcessRoomCreation(int zoneType, Vector3 worldPos, RoomManager parentManager) {
        var newRoom = CreateAdjacentRoomFromPool(worldPos, zoneType);

        RecursiveZoneGeneration(newRoom, worldPos);
    }

    // POLYMORPHISM
    protected override void ChangeSettingsInRoomManager(GameObject room, Vector3 worldPos, Quaternion rotation, bool isUsed)
    {
        room.transform.SetPositionAndRotation(worldPos, rotation);

        var roomManager = room.GetComponent<RoomManager>();

        UsingRoom(roomManager, isUsed);

        UpdateAvailableExitsCount(roomManager.exitsFromRoom);
    }

    protected override void AddDataToSpawnedRoom(Vector3 worldPos, RoomData newRoomData)
    {
        _newSpawnedRooms.Add(worldPos, newRoomData);
    }

    protected int GetExitRequirement(Vector3 worldPos, GameEnums.Direction direction, int indexCheckedZone)
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

    protected int[] CheckRequiredRoomType(Vector3 worldPos, int indexCheckedZone)
    {
        var requiredRoomType = new int[GameEnums.XOZDirectionsCount];
        
        for (int i = 0; i < GameEnums.XOZDirectionsCount; i++)
        {
            requiredRoomType[i] = GetExitRequirement(worldPos, (GameEnums.Direction)i, indexCheckedZone);
        }
        
        return requiredRoomType;
    }

    protected override (bool, RoomData) TryGetRoomData(Vector3 worldPos) {
        bool isGetValue = _spawnedRooms.TryGetValue(worldPos, out RoomData data)
            || _newSpawnedRooms.TryGetValue(worldPos, out data);
        return (isGetValue, data);
    }


    private bool ShouldSkipPosition(Vector3 newWorldPos, Vector3 oldPlayerPos, bool hasExit)
    {
        return !hasExit || 
            newWorldPos == oldPlayerPos || 
            _spawnedRooms.ContainsKey(newWorldPos) || 
            _newSpawnedRooms.ContainsKey(newWorldPos);
    }
}
