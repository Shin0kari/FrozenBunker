using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// INHERITANCE
[RequireComponent(typeof(SpawnRoomManager))]
public class SpawnNewZoneManager : SpawnRoomManager
{
    private int countStartedImportantRooms = -1;
    private Dictionary<Vector2, RoomData> _newSpawnedRooms;
    public PlayerController playerController; // изменить тип безопасности

    private void Start() {
        _roomsPool = GetComponent<RoomsPool>();
        _spawnedRooms = GetComponent<SpawnRoomManager>()._spawnedRooms;
    }

    public void AddImportantRoomToCounter(int indexNewZone) {
        if (indexNewZone == 10) {return;}
        countImportantZoneRooms[indexNewZone] += 1;
    }

    public (bool, Dictionary<Vector2, RoomData>) TryToGenerateNewZone(GameObject transitionRoom, Vector2 oldPlayerPos) {
        _newSpawnedRooms = new();
        countStartedImportantRooms = 
            countImportantZoneRooms[transitionRoom.GetComponent<TransitionRoomManager>().TransitionToZone]; // new
        bool isNewZoneGenerated = RecursiveZoneGeneration(transitionRoom, oldPlayerPos);
        return (isNewZoneGenerated, _newSpawnedRooms);
    }

    private bool RecursiveZoneGeneration(GameObject parentRoom, Vector2 oldPos) {
        var roomManager = parentRoom.GetComponent<RoomManager>();
        var indexNewZone = TryGetIndex(parentRoom);

        if (indexNewZone == -1) {return false;}

        foreach (Direction consideringDir in System.Enum.GetValues(typeof(Direction)))
        {
            var (newPos3D, newPos2D) = CalculateAdjacentPosition(
                roomManager._2DWorldPos, 
                consideringDir
            );

            if (ShouldSkipPosition(newPos2D, oldPos, roomManager.exitsFromRoom[(int)consideringDir])) 
                continue;

            ProcessRoomCreation(indexNewZone, newPos3D, newPos2D, roomManager);
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

    private void ProcessRoomCreation(int zoneType, Vector3 position3D, Vector2 position2D, RoomManager parentManager) {
        var newRoom = CreateAdjacentRoomFromPool(position3D, position2D, zoneType);

        RecursiveZoneGeneration(newRoom, parentManager._2DWorldPos);
    }

    // POLYMORPHISM
    protected override void ChangeSettingsInRoomManager(GameObject room, Vector2 _2DWorldPos, Vector3 worldPos, Quaternion rotation, bool isUsed)
    {
        room.transform.SetPositionAndRotation(worldPos, rotation);

        var roomManager = room.GetComponent<RoomManager>();

        UsingRoom(roomManager, isUsed);
        roomManager._2DWorldPos = _2DWorldPos;

        UpdateAvailableExitsCount(roomManager.exitsFromRoom);
    }

    protected override void AddDataToSpawnedRoom(Vector2 _2DWorldPos, RoomData newRoomData)
    {
        _newSpawnedRooms.Add(_2DWorldPos, newRoomData);
    }

    protected int GetExitRequirement(Vector2 position, Direction direction, int indexCheckedZone)
    {
        var adjacentPos = GetAdjacentPosition(position, direction);
        var (isGetValue, data) = TryGetRoomData(adjacentPos);
        if (!isGetValue)
            return 0;

        var adjacentRoomManager = data.RoomObject.GetComponent<RoomManager>();
        var oppositeDirection = GetOppositeDirection(position - adjacentPos);
        
        
        var returningValue = adjacentRoomManager.exitsFromRoom[(int)oppositeDirection] ? 1 : -1;
        return (returningValue != -1 && 
            adjacentRoomManager.zoneType != indexCheckedZone && 
            !adjacentRoomManager.IsRoomTransitional) 
            ? -2 : returningValue;
    }

    protected int[] CheckRequiredRoomType(Vector2 position, int indexCheckedZone)
    {
        var requiredRoomType = new int[DirectionsCount];
        
        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            requiredRoomType[(int)direction] = GetExitRequirement(position, direction, indexCheckedZone);
        }
        
        return requiredRoomType;
    }

    protected override (bool, RoomData) TryGetRoomData(Vector2 pos) {
        bool isGetValue = _spawnedRooms.TryGetValue(
            pos, out RoomData data) || 
            _newSpawnedRooms.TryGetValue(pos, out data);
        return (isGetValue, data);
    }


    private bool ShouldSkipPosition(Vector2 newPosition, Vector2 oldPlayerPos, bool hasExit)
    {
        return !hasExit || 
            newPosition == oldPlayerPos || 
            _spawnedRooms.ContainsKey(newPosition) || 
            _newSpawnedRooms.ContainsKey(newPosition);
    }
}
