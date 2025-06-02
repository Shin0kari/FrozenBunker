using System.Collections.Generic;
using UnityEngine;

// INHERITANCE
[RequireComponent(typeof(SpawnRoomManager))]
public class LinearTransitionSpawnZoneManager : SpawnRoomManager
{
    private int countStartedImportantRooms;
    private Dictionary<Vector3, RoomData> _newSpawnedRooms;
    public PlayerController playerController; // изменить тип безопасности

    private void Start() {
        _roomsPool = GetComponent<RoomsPool>();
        _spawnedRoomsData = GetComponent<SpawnRoomManager>().SpawnedRoomsData;
    }

    public (bool, Dictionary<Vector3, RoomData>) TryToGenerateNewZone(GameObject transitionRoom, Vector3 oldPlayerWorldPos)
    {
        _newSpawnedRooms = new();
        countStartedImportantRooms =
            _roomsPool.GetCountImportantRoomInZone(transitionRoom.GetComponent<TransitionRoomManager>().TransitionToZone);
        bool isNewZoneGenerated = RecursiveZoneGeneration(transitionRoom, oldPlayerWorldPos);
        return (isNewZoneGenerated, _newSpawnedRooms);
    }

    private bool RecursiveZoneGeneration(GameObject parentRoom, Vector3 oldWorldPos) {
        var roomManager = parentRoom.GetComponent<RoomManager>();
        var indexNewZone = TryGetIndex(parentRoom);

        if (indexNewZone == -1) {return false;}

        for (int consideringDir = 0; consideringDir < DirectionsCount; consideringDir++)
        {
            var newWorldPos = CalculateAdjacentPosition(
                roomManager.LocalPos,
                (Direction)consideringDir
            );

            if (ShouldSkipPosition(newWorldPos, oldWorldPos, roomManager.exitsFromRoom[consideringDir])) 
                continue;

            ProcessRoomCreation(indexNewZone, newWorldPos, roomManager);
        }

        return countStartedImportantRooms != _roomsPool.GetCountImportantRoomInZone(indexNewZone);
    }

    // !!!!! нужно облегчить функцию уменьшив количество вызовол TryGetComponent
    private int TryGetIndex(GameObject room) {
        if (room.TryGetComponent(out TransitionRoomManager transitionRoomManager)) {
            return transitionRoomManager.TransitionToZone;
        } else {
            return room.GetComponent<RoomManager>().zoneType;
        }
    }

    private void ProcessRoomCreation(int zoneType, Vector3 parentRoomWorldPos, RoomManager parentManager) {
        var newRoom = CreateAdjacentRoomFromPool(parentRoomWorldPos, zoneType);

        RecursiveZoneGeneration(newRoom, parentRoomWorldPos);
    }

    private bool ShouldSkipPosition(Vector3 newPosition, Vector3 oldPlayerPos, bool hasExit)
    {
        return !hasExit || 
            newPosition == oldPlayerPos || 
            _spawnedRoomsData.ContainsKey(newPosition) || 
            _newSpawnedRooms.ContainsKey(newPosition);
    }

    // POLYMORPHISM
    protected override void ChangeSettingsInRoomManager(GameObject room, Vector3 worldPos, Quaternion rotation, bool isUsed)
    {
        room.transform.SetPositionAndRotation(worldPos, rotation);

        var roomManager = room.GetComponent<RoomManager>();

        UsingRoom(roomManager, isUsed);
        roomManager.LocalPos = new Vector3(worldPos.x / 25, worldPos.y / 25, worldPos.z / 25);
    }

    protected override void AddDataToSpawnedRoom(Vector3 worldPos, RoomData newRoomData)
    {
        _newSpawnedRooms.Add(
            new Vector3(worldPos.x / 25, worldPos.y / 25, worldPos.z / 25), 
            newRoomData
        );
    }

    protected override bool TryGetRoomData(Vector3 worldPos, out RoomData data) {
        bool isGetValue = 
            _spawnedRoomsData.TryGetValue(worldPos, out RoomData roomData) || 
            _newSpawnedRooms.TryGetValue(worldPos, out roomData);
        data = roomData;
        
        return isGetValue;
    }

    protected override int CalculateMinNumRequiredExits(int zoneType) {
        if (zoneType == DeadEndZoneType)
            return 1;
        return (CountZoneRoomsExits > 1 || _roomsPool.GetCountImportantRoomInZone(zoneType) < 2) ? 1 : 2;
    }
}
