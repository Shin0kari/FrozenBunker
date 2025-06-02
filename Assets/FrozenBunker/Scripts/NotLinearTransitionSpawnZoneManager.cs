using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// INHERITANCE
[RequireComponent(typeof(SpawnRoomManager))]
public class NotLinearTransitionSpawnZoneManager : SpawnRoomManager
{
    private Dictionary<Vector3, RoomData> _newSpawnedRooms;
    public PlayerController playerController; // изменить тип безопасности

    void Start()
    {
        _roomsPool = GetComponent<RoomsPool>();
        _spawnedRoomsData = GetComponent<SpawnRoomManager>().SpawnedRoomsData;
    }

    public (bool, Dictionary<Vector3, RoomData>) TryToGenerateLift(GameObject parentRoom)
    {
        _newSpawnedRooms = null;
        var parentTransitionRoomManager = parentRoom.GetComponent<TransitionRoomManager>();
        var parentPos = parentTransitionRoomManager.LocalPos;
        _spawnedRoomsData.TryGetValue(parentPos, out RoomData parentRoomData);
        var parentRotation = parentRoomData.RotationY;
        // parentRoom.GetComponent<TransitionRoomManager>().FloorToZone;
        int floor = 0;

        for (int floorNum = parentTransitionRoomManager.FloorToZone[0][0]; floorNum <= parentTransitionRoomManager.FloorToZone.Last()[0]; floorNum++)
        {
            var liftPartPos = new Vector3(parentPos.x, floorNum, parentPos.z) * 25;
            // !!! добавить проверку на наличие выхода из комнаты с лифтом
            if (floorNum == parentPos.y)
                continue;
            if (_spawnedRoomsData.ContainsKey(liftPartPos))
                return (false, _newSpawnedRooms);

            List<GameObject> liftRooms;
            if (floorNum == parentTransitionRoomManager.FloorToZone[floor][0])
            {
                liftRooms = FilterRoomsPoolByTypeTransitional(parentTransitionRoomManager.FloorToZone[floor][1], 1);
                floor++;
            }
            else
            {
                liftRooms = FilterRoomsByLiftShaft(parentTransitionRoomManager.zoneType);
            }

            var liftRoom = GetRandomRoomFromPool(liftRooms);

            var room = SpawnRoom(
                liftRoom,
                liftPartPos,
                Quaternion.Euler(0, parentRotation, 0)
            );

            UpdateRoomExits((int)(parentRotation / 90), room);
        }
        return (true, _newSpawnedRooms);
    }

    public (bool, Dictionary<Vector3, RoomData>) TryToGenerateLadder(GameObject parentRoom)
    {
        _newSpawnedRooms = null;
        var parentTransitionRoomManager = parentRoom.GetComponent<TransitionRoomManager>();
        var parentPos = parentTransitionRoomManager.LocalPos;
        _spawnedRoomsData.TryGetValue(parentPos, out RoomData parentRoomData);
        var parentRotation = parentRoomData.RotationY;
        int floor = 0;

        for (int floorNum = parentTransitionRoomManager.FloorToZone[0][0]; floorNum <= parentTransitionRoomManager.FloorToZone.Last()[0]; floorNum++)
        {
            var liftPartPos = new Vector3(parentPos.x, floorNum, parentPos.z) * 25;
            // !!! добавить проверку на наличие выхода из комнаты с лифтом
            if (floorNum == parentPos.y)
                continue;
            if (_spawnedRoomsData.ContainsKey(liftPartPos))
                return (false, _newSpawnedRooms);

            List<GameObject> ladderRooms;
            if (floorNum == parentTransitionRoomManager.FloorToZone[floor][0])
            {
                ladderRooms = FilterRoomsPoolByTypeTransitional(parentTransitionRoomManager.FloorToZone[floor][1], 2);
                floor++;
            }
            else
            {
                ladderRooms = FilterRoomsByLadderShaft(parentTransitionRoomManager.zoneType);
            }

            var liftRoom = GetRandomRoomFromPool(ladderRooms);

            var room = SpawnRoom(
                liftRoom,
                liftPartPos,
                Quaternion.Euler(0, parentRotation, 0)
            );

            UpdateRoomExits((int)(parentRotation / 90), room);
        }
        return (true, _newSpawnedRooms);
    }

    private List<GameObject> FilterRoomsPoolByTypeTransitional(int zoneIndex, int typeTransitional)
    {
        var roomsPool = _roomsPool.GetPoolRooms(zoneIndex);
        var availableRoomsPool = new List<GameObject>();

        foreach (var room in roomsPool)
        {
            if (!room.activeSelf && room.TryGetComponent(out TransitionRoomManager transitionRoomManager))
            {
                if (transitionRoomManager.TypeTransitionRoom == typeTransitional)
                {
                    availableRoomsPool.Add(room);
                    continue;
                }
            }
        }
        return availableRoomsPool;
    }

    private List<GameObject> FilterRoomsByLiftShaft(int zoneIndex)
    {
        var roomsPool = _roomsPool.GetPoolRooms(zoneIndex);
        var availableRoomsPool = new List<GameObject>();

        foreach (var room in roomsPool)
        {
            if (!room.activeSelf && room.TryGetComponent(out TransitionRoomManager transitionRoomManager))
            {
                if (transitionRoomManager.IsRoomLiftShaft)
                {
                    availableRoomsPool.Add(room);
                    continue;
                }
            }
        }
        return availableRoomsPool;
    }

    private List<GameObject> FilterRoomsByLadderShaft(int zoneIndex)
    {
        var roomsPool = _roomsPool.GetPoolRooms(zoneIndex);
        var availableRoomsPool = new List<GameObject>();

        foreach (var room in roomsPool)
        {
            if (!room.activeSelf && room.TryGetComponent(out TransitionRoomManager transitionRoomManager))
            {
                if (transitionRoomManager.IsRoomLadderShaft)
                {
                    availableRoomsPool.Add(room);
                    continue;
                }
            }
        }
        return availableRoomsPool;
    }
    
    protected override GameObject SpawnRoom(GameObject room, Vector3 worldPos, Quaternion rotation)
    {
        if (TryGetRoomData(worldPos, out RoomData _roomData))
        {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

        ChangeSettingsInRoomManager(room, worldPos, rotation, true);

        RoomData newRoomData = new()
        {
            RoomObject = room,
            RotationY = rotation.eulerAngles.y,
            // Loot = GenerateLoot()
        };

        newRoomData.RoomObject.GetComponent<RoomManager>();
        Debug.Log("Rotation: " + newRoomData.RotationY);
        Debug.Log("_newSpawnedRooms Count: " + _newSpawnedRooms.Count);

        AddDataToSpawnedRoom(worldPos, newRoomData); // Добавляем новую позицию в HashSet

        return room;
    }

    protected override void AddDataToSpawnedRoom(Vector3 worldPos, RoomData newRoomData) {
        _newSpawnedRooms.Add(
            new Vector3(worldPos.x / 25, worldPos.y / 25, worldPos.z / 25), 
            newRoomData
        );
    }

    protected override void ChangeSettingsInRoomManager(GameObject room, Vector3 worldPos, Quaternion rotation, bool isUsed) {
        room.transform.SetPositionAndRotation(worldPos, rotation);

        var roomManager = room.GetComponent<RoomManager>();

        UsingRoom(roomManager, isUsed);
        roomManager.LocalPos = new Vector3(worldPos.x / 25, worldPos.y / 25, worldPos.z / 25);
    }
}
