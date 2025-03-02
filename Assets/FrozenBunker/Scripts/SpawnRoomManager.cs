using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class SpawnRoomManager : MonoBehaviour
{
    [SerializeField] private GameObject[] rooms;
    [SerializeField] private Vector3 startPosition = new Vector3(0, 0, -12);
    private Dictionary<Vector2, GameObject> roomsWorldPositions = new Dictionary<Vector2, GameObject>();

    void Start()
    {
        // Возможно баг в том, что в списке одинаковые prefab - это один и тот же room. И меняя ExitsFromRoom у одного prefab, меняется и у другого. !!!!!!!!!!!!!!!
        GameObject room = SpawnStartRoom();
        SpawnAdjacentRooms(room);
    }

    private GameObject SpawnStartRoom() {
        int randomIndex = Random.Range(0, rooms.Length);
        
        Vector2 start2DWorldPos = new Vector2(0, 0);

        int newDirection = Random.Range(0, 3);
        Quaternion newRotation = Quaternion.Euler(0, newDirection * 90, 0);
        Debug.Log("Start_Dir: " + newDirection);

        // Quaternion randomDirection = SetRandomDirection();
        GameObject room = SpawnRoom(randomIndex, rooms, startPosition, newRotation, start2DWorldPos);
        bool[] startExitsFromRoom = CheckNumAvailableExits(room);
        Debug.Log("StartExists: \n" + startExitsFromRoom[0] + ", " + startExitsFromRoom[1] + ", " + startExitsFromRoom[2] + "," + startExitsFromRoom[3]);
        ChangeExistFromRoom(newDirection, startExitsFromRoom);
        Debug.Log("StartExistsWithNewDir: \n" + startExitsFromRoom[0] + ", " + startExitsFromRoom[1] + ", " + startExitsFromRoom[2] + "," + startExitsFromRoom[3]);
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
    private GameObject SpawnRoom(int randomIndex, GameObject[] rooms, Vector3 worldPos, Quaternion rotation, Vector2 new2DPos) {
        if (roomsWorldPositions.ContainsKey(new2DPos)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

        GameObject room = Instantiate(rooms[randomIndex], worldPos, rotation);
        room.GetComponent<RoomManager>().WorldPos = new2DPos;
        roomsWorldPositions.Add(new2DPos, room); // Добавляем новую позицию в HashSet
        return room;
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
        Debug.Log("ExistsInAdjacent: 0 - \n" + existFromRoom[0] + ", 1 - " + existFromRoom[1] + ", 2 - " + existFromRoom[2] + ", 3 - " + existFromRoom[3]);
        for (int i = 0; i < existFromRoom.Length; i++) {
            Debug.Log("I: " + i + "; ExistsInAdjacent - " + existFromRoom[i]);
            // i = 0 -> north, 1 -> east, 2 -> south, 3 -> west
            if (!existFromRoom[i]) {
                Debug.Log("I to continue: " + i);
                continue;
            }

            Quaternion newRotation;
            Vector3 newPosition;
            Vector2 new2DWorldPos;

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

            int[] requiredRoomType = CheckRequiredRoomType(new2DWorldPos);
            GameObject[] availableRooms = DetermineListAvailableRooms(rooms, requiredRoomType);
            int randomIndex = Random.Range(0, availableRooms.Length);

            int newDirection = GetDirectionForRoom(availableRooms[randomIndex], requiredRoomType);
            newRotation = Quaternion.Euler(0, newDirection * 90, 0);
            
            GameObject newRoom = SpawnRoom(randomIndex, availableRooms, newPosition, newRotation, new2DWorldPos);
            bool[] newRoomExitsFromRoom = newRoom.GetComponent<RoomManager>().exitsFromRoom;
            ChangeExistFromRoom(newDirection, newRoomExitsFromRoom);
        }
    }

    private int GetDirectionForRoom(GameObject room, int[] requiredRoomType) {
        List<int> possibleRotations = new List<int>();
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
            Debug.Log("Rotation: " + possibleRotations[randomIndex]);
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
    private GameObject[] DetermineListAvailableRooms(GameObject[] rooms, int[] requiredRoomType) {
        List<GameObject> availableRooms = new List<GameObject>();

        foreach (GameObject room in rooms) {
            bool[] exitsFromRoom = room.GetComponent<RoomManager>().exitsFromRoom;

            for (int rotation = 0; rotation < 4; rotation++) {
                bool isMatch = CheckIsMatchForDetermineAvailableDirection(exitsFromRoom, rotation, requiredRoomType);

                if (isMatch) {
                    availableRooms.Add(room);
                    break;
                }
            }
        }

        return availableRooms.ToArray();
    }

    private bool CheckIsMatchForDetermineAvailableDirection(bool[] exitsFromRoom, int rotation, int[] requiredRoomType) {
        bool isMatch = true;

        for (int i = 0; i < 4; i++) {
            int required = requiredRoomType[i];
            bool exit = exitsFromRoom[(i + (4 - rotation)) % 4];

            if ((required == 1 && !exit) || (required == -1 && exit)) {
                isMatch = false;
                break;
            }
        }

        return isMatch;
    }

    /*
    private GameObject[] DetermineListAvailableRooms(GameObject[] rooms, Vector2 roomWorldPos) {
        List<GameObject> availableRooms = new List<GameObject>();
        int[] requiredRoomType = CheckRequiredRoomType(roomWorldPos);
        bool[] exitsFromRoom;
        foreach (GameObject room in rooms) {
            exitsFromRoom = room.GetComponent<RoomManager>().exitsFromRoom;

            foreach (int availabilityExit in requiredRoomType) {
                
            }

            availableRooms.Add(room);
        }

        return availableRooms.ToArray();
    }
    */

    /// <summary>
    /// Задаёт рандомное направление для комнаты (север, восток, юг, запад (по часовой стрелке))
    /// </summary>
    /// <returns></returns>
    private Quaternion SetRandomDirection() {
        int randomIndex = Random.Range(0, 4);
        Quaternion newRotation;

        switch (randomIndex) {
            case 0: // north
                newRotation = Quaternion.Euler(0, 0, 0);
                break;
            case 1: // east
                newRotation = Quaternion.Euler(0, 90, 0);
                break;
            case 2: // south
                newRotation = Quaternion.Euler(0, 180, 0);
                break;
            case 3: // west
                newRotation = Quaternion.Euler(0, 270, 0);
                break;
            default:
                newRotation = Quaternion.identity;
                break;
        }

        return newRotation;
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
            // В части где индекс меняем местами север с югом, запад с востоком, так как на северной смежной карте должен быть выход на юге этой карты
            if (adjacentRoom.GetComponent<RoomManager>().exitsFromRoom[(index + 2) % 4]) {
                requiredRoomType[index] = 1;
            } else {
                requiredRoomType[index] = -1;
            }
        }
    }
}
