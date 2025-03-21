using UnityEngine;
using System.Collections.Generic;

public class SpawnRoomManager : MonoBehaviour
{
    private List<GameObject> rooms;
    [SerializeField] private int numOfZones = 10;
    [SerializeField] private Vector3 startPosition = new(0, 0, -12);
    private readonly Dictionary<Vector2, GameObject> roomsWorldPositions = new();

    void Start()
    {
        int startIndexZone = GetStartingIndexZone();
        rooms = GetComponent<RoomsPool>().GetPoolRooms(startIndexZone);

        GameObject room = SpawnStartRoom(rooms);
        SpawnAdjacentRooms(room);
    }

    private GameObject SpawnStartRoom(List<GameObject> rooms) {
        List<GameObject> startingRooms = GetStartingRoomsPoolFromZoneRoomsPool(rooms);
        int randomIndexRoom = Random.Range(0, startingRooms.Count - 1);
        
        Vector2 start2DWorldPos = new(0, 0);

        (Quaternion newRotation, int newDirection) = SetRandomRotationAndDirection();

        GameObject room = SpawnRoom(randomIndexRoom, startingRooms, startPosition, newRotation, start2DWorldPos);
        bool[] startExitsFromRoom = CheckNumAvailableExits(room);
        ChangeExistFromRoom(newDirection, startExitsFromRoom);

        return room;
    }

    /// <summary>
    /// Спавнит рандомную комнату из списка с указанным поворотом и глобальной позицией в мире.
    /// </summary>
    /// <param name="randomIndex"></param>
    /// <param name="pos"></param>
    /// <param name="rotation"></param>
    /// <param name="newPos"></param>
    /// <returns></returns>
    private GameObject SpawnRoom(int randomIndex, List<GameObject> rooms, Vector3 worldPos, Quaternion rotation, Vector2 new2DPos) {
        if (roomsWorldPositions.ContainsKey(new2DPos)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

        GameObject room = rooms[randomIndex];
        room.transform.SetPositionAndRotation(worldPos, rotation);
        room.SetActive(true);

        // GameObject room = Instantiate(rooms[randomIndex], worldPos, rotation);
        RoomManager roomManager = room.GetComponent<RoomManager>();
        roomManager.WorldPos = new2DPos;
        roomsWorldPositions.Add(new2DPos, room); // Добавляем новую позицию в HashSet
        if (!roomManager.canUsedInfinitely) {
            roomManager.isUsed = true; // комнаты должны использоваться только 1 раз кроме случая если они canUsedInfinitely
        }
        return room;
    }

    private List<GameObject> GetStartingRoomsPoolFromZoneRoomsPool(List<GameObject> rooms) {
        List<GameObject> startingRooms = new();
        foreach (GameObject room in rooms) {
            if (room.GetComponent<RoomManager>().CanBeUsedAsStartingRoom) {
                startingRooms.Add(room);
            }
        }
        if (startingRooms.Count == 0) {
            Debug.LogError("No starting rooms in this zone!");
            return startingRooms;
        }
        return startingRooms;
    }


    /// <summary>
    /// Переносит выходы из скрипта RoomManager (Room Data) в SpawnRoomManager.
    /// </summary>
    /// <param name="room"></param>
    private bool[] CheckNumAvailableExits(GameObject room) {
        return room.GetComponent<RoomManager>().exitsFromRoom;
    }

    /// <summary>
    /// Если в направлении света имеется выход, то там спавнится комната. 
    /// Её направление уже определяется от того, куда была направлена дверь.
    /// К примеру, если у нас спавнится комната  от стартовой комнаты на южном направлении. Локально, стандартное направление у неё на север, но глобально это направление будет юг.
    /// </summary>
    /// <param name="room"></param>
    private void SpawnAdjacentRooms(GameObject room) {
        RoomManager roomManager = room.GetComponent<RoomManager>();
        bool[] existFromRoom = roomManager.exitsFromRoom;

        Quaternion newRotation;
        Vector3 newPosition;
        Vector2 new2DWorldPos;

        int[] requiredRoomType;
        bool[] newRoomExitsFromRoom;

        for (int i = 0; i < existFromRoom.Length; i++) {
            // i = 0 -> north, 1 -> east, 2 -> south, 3 -> west
            if (!existFromRoom[i]) {
                continue;
            }

            switch (i) {
                case 0: // north
                    newPosition = room.transform.position + new Vector3(25, 0, 0);
                    new2DWorldPos = roomManager.WorldPos + new Vector2(1, 0);
                    break;
                case 1: // east
                    newPosition = room.transform.position + new Vector3(0, 0, -25);
                    new2DWorldPos = roomManager.WorldPos + new Vector2(0, -1);
                    break;
                case 2: // south
                    newPosition = room.transform.position + new Vector3(-25, 0, 0);
                    new2DWorldPos = roomManager.WorldPos + new Vector2(-1, 0);
                    break;
                case 3: // west
                    newPosition = room.transform.position + new Vector3(0, 0, 25);
                    new2DWorldPos = roomManager.WorldPos + new Vector2(0, 1);
                    break;
                default:
                    return;
            }

            requiredRoomType = CheckRequiredRoomType(new2DWorldPos);
            List<GameObject> availableRooms = DetermineListAvailableRooms(rooms, requiredRoomType);
            int randomIndex = Random.Range(0, availableRooms.Count - 1);

            int newDirection = GetDirectionForRoom(availableRooms[randomIndex], requiredRoomType);
            newRotation = Quaternion.Euler(0, newDirection * 90, 0);
            
            GameObject newRoom = SpawnRoom(randomIndex, availableRooms, newPosition, newRotation, new2DWorldPos);
            newRoomExitsFromRoom = newRoom.GetComponent<RoomManager>().exitsFromRoom;
            ChangeExistFromRoom(newDirection, newRoomExitsFromRoom);
        }
    }

    private int GetDirectionForRoom(GameObject room, int[] requiredRoomType) {
        List<int> possibleRotations = new();
        bool[] exitsFromRoom = room.GetComponent<RoomManager>().exitsFromRoom;
        bool isMatch;

        for (int rotation = 0; rotation < 4; rotation++) {
            isMatch = CheckIsMatchForDetermineAvailableDirection(exitsFromRoom, rotation, requiredRoomType);

            if (isMatch) {
                possibleRotations.Add(rotation);
            }
        }

        if (possibleRotations.Count > 0) {
            int randomIndex = Random.Range(0, possibleRotations.Count - 1);
            return possibleRotations[randomIndex];
        } else {
            return 0; // Возвращаем без поворота, если нет подходящих поворотов
        }
    }

    /// <summary>
    /// Эта функция делает так, что при повороте комнаты existFromRoom останется мировым а не локальным.
    /// </summary>
    /// <param name="direct"></param>
    private void ChangeExistFromRoom(int direct, bool[] existFromRoom) {
        bool presenceNorthDoor;
        bool presenceEastDoor;
        bool presenceSouthDoor;
        bool presenceWestDoor;
        switch (direct) {
            case 0: // north
                return;
            case 1: // rotate north direct prefab to east
                presenceNorthDoor = existFromRoom[0];
                presenceEastDoor = existFromRoom[1];
                presenceSouthDoor = existFromRoom[2];
                presenceWestDoor = existFromRoom[3];
                existFromRoom[0] = presenceWestDoor;
                existFromRoom[1] = presenceNorthDoor;
                existFromRoom[2] = presenceEastDoor;
                existFromRoom[3] = presenceSouthDoor;
                break;
            case 2: // rotate north direct prefab to south
                presenceNorthDoor = existFromRoom[0];
                presenceEastDoor = existFromRoom[1];
                presenceSouthDoor = existFromRoom[2];
                presenceWestDoor = existFromRoom[3];
                existFromRoom[0] = presenceSouthDoor;
                existFromRoom[1] = presenceWestDoor;
                existFromRoom[2] = presenceNorthDoor;
                existFromRoom[3] = presenceEastDoor;
                break;
            case 3: // rotate north direct prefab to west
                presenceNorthDoor = existFromRoom[0];
                presenceEastDoor = existFromRoom[1];
                presenceSouthDoor = existFromRoom[2];
                presenceWestDoor = existFromRoom[3];
                existFromRoom[0] = presenceEastDoor;
                existFromRoom[1] = presenceSouthDoor;
                existFromRoom[2] = presenceWestDoor;
                existFromRoom[3] = presenceNorthDoor;

                break;
            default:
                Debug.LogError("Invalid direction");
                return;
        }
    }

    /// <summary>
    /// Определяет тип допустимой комнаты и выводит список допустимых комнат.
    /// </summary>
    /// <param name="rooms"></param>
    /// <returns></returns>
    private List<GameObject> DetermineListAvailableRooms(List<GameObject> rooms, int[] requiredRoomType) {
        List<GameObject> availableRooms = new();
        RoomManager roomManager;
        bool[] exitsFromRoom;
        bool isMatch;

        foreach (GameObject room in rooms) {
            roomManager = room.GetComponent<RoomManager>();
            exitsFromRoom = roomManager.exitsFromRoom;

            for (int rotation = 0; rotation < 4; rotation++) {
                isMatch = CheckIsMatchForDetermineAvailableDirection(exitsFromRoom, rotation, requiredRoomType);

                if (isMatch && !roomManager.isUsed) {
                    availableRooms.Add(room);
                    break;
                }
            }
        }

        return availableRooms;
    }

    /// <summary>
    /// Узнаёт, можно ли использовать комнату прокручивая её по часовой стрелке.
    /// </summary>
    /// <param name="exitsFromRoom"></param>
    /// <param name="rotation"></param>
    /// <param name="requiredRoomType"></param>
    /// <returns></returns>
    private bool CheckIsMatchForDetermineAvailableDirection(bool[] exitsFromRoom, int rotation, int[] requiredRoomType) {
        bool isMatch = true;
        int required;
        bool exit;

        // Проверяем, чтобы все выходя для комнаты были допустимы
        for (int i = 0; i < 4; i++) {
            required = requiredRoomType[i];
            exit = exitsFromRoom[(i + (4 - rotation)) % 4];

            if ((required == 1 && !exit) || (required == -1 && exit)) {
                isMatch = false;
                break;
            }
        }

        return isMatch;
    }

    /// <summary>
    /// Задаёт рандомное направление для комнаты (север, восток, юг, запад (по часовой стрелке))
    /// </summary>
    /// <returns></returns>
    private (Quaternion, int) SetRandomRotationAndDirection() {
        int randomIndex = Random.Range(0, 3);
        return (Quaternion.Euler(0, randomIndex * 90, 0), randomIndex);
    }

    /// <summary>
    /// Проверяет на наличие смежных комнат с 4х сторон света. Заполняет requiredRoomType комнаты для определения необходимого комнаты.
    /// </summary>
    /// <param name="roomWorldPos"></param>
    /// <returns></returns>
    private int[] CheckRequiredRoomType(Vector2 roomWorldPos) {
        // 0 - комната не создана; 1 - смежная комната имеет общую дверь с этой комнатой; -1 - смежная комната не имеет общей двери с этой комнатой
        int[] requiredRoomType = new int[4];
        
        for (int i = 0; i < 4; i++) {
            switch (i) {
                case 0: // north
                    CheckAdjacentRoom(roomWorldPos + new Vector2(1, 0), requiredRoomType, i);
                    break;
                case 1: // east
                    CheckAdjacentRoom(roomWorldPos + new Vector2(0, -1), requiredRoomType, i);
                    break;
                case 2: // south
                    CheckAdjacentRoom(roomWorldPos + new Vector2(-1, 0), requiredRoomType, i);
                    break;
                case 3: // west
                    CheckAdjacentRoom(roomWorldPos + new Vector2(0, 1), requiredRoomType, i);
                    break;
            }
        }
        return requiredRoomType;
    }

    /// <summary>
    /// Проверяет на наличие смежной комнаты. Если она имеется, заполняет requiredRoomType.
    /// </summary>
    /// <param name="adjacentRoomWorldPos"></param>
    /// <param name="requiredRoomType"></param>
    /// <param name="index"></param>
    private void CheckAdjacentRoom(Vector2 adjacentRoomWorldPos, int[] requiredRoomType, int index) {
        if (roomsWorldPositions.ContainsKey(adjacentRoomWorldPos)) {
            roomsWorldPositions.TryGetValue(adjacentRoomWorldPos, out GameObject adjacentRoom);
            // В части где индекс меняем местами север с югом, запад с востоком, 
            // так как на северной смежной карте (вверхней соседней комнате) должен быть выход на юг в комнату в которой находится игрок
            if (adjacentRoom.GetComponent<RoomManager>().exitsFromRoom[(index + 2) % 4]) {
                requiredRoomType[index] = 1;
            } else {
                requiredRoomType[index] = -1;
            }
        }
    }

    /// <summary>
    /// Возвращает индекс стартовой зоны. По стандарту игрок появляется в зоне коридоров.
    /// Если стоит дополнительный флажок "random = true", то стартовая зона рандомится.
    /// </summary>
    /// <param name="random"></param>
    /// <returns></returns>
    private int GetStartingIndexZone(bool random = false) {
        if (random) {
            return Random.Range(0, numOfZones - 1);
        } else {
            return 0;
        }
    } 
}
