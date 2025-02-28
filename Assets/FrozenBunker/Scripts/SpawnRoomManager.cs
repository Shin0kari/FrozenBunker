using UnityEngine;
using System.Collections.Generic;

public class SpawnRoomManager : MonoBehaviour
{
    [SerializeField] private GameObject[] rooms;
    [SerializeField] private Vector3 startPosition = new Vector3(0, 0, -12);
    private bool[] existFromRoom = new bool[4];
    // private HashSet<Vector2> occupiedPositions = new HashSet<Vector2>();
    private Dictionary<Vector2, GameObject> roomsWorldPositions = new Dictionary<Vector2, GameObject>();


    void Start()
    {
        GameObject room = SpawnStartRoom();
        CheckNumAvailableExits(room);
        SpawnAdjacentRooms(room);
    }

    private GameObject SpawnStartRoom() {
        int randomIndex = Random.Range(0, rooms.Length);
        
        Vector2 startWorldPos = new Vector2(0, 0);
        Quaternion randomDirection = SetRandomDirection();
        GameObject room = SpawnRoom(randomIndex, rooms, startPosition, randomDirection, startWorldPos);
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
    private GameObject SpawnRoom(int randomIndex, GameObject[] rooms, Vector3 pos, Quaternion rotation, Vector2 newPos) {
        if (roomsWorldPositions.ContainsKey(newPos)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

        GameObject room = Instantiate(rooms[randomIndex], pos, rotation);
        room.GetComponent<RoomManager>().WorldPos = newPos;
        roomsWorldPositions.Add(newPos, room); // Добавляем новую позицию в HashSet
        return room;
    }

    /// <summary>
    /// Переносит выходы из скрипта RoomManager (Room Data) в SpawnRoomManager.
    /// </summary>
    /// <param name="room"></param>
    private void CheckNumAvailableExits(GameObject room) {
        existFromRoom = room.GetComponent<RoomManager>().exitsFromRoom;
    }

    /// <summary>
    /// Если в направлении света имеется выход, то там спавнится комната. 
    /// Её направление уже определяется от того, куда была направлена дверь.
    /// К примеру, если у нас спавнится комната  от стартовой комнаты на южном направлении. Локально, стандартное направление у неё на север, но глобально это направление будет юг.
    /// </summary>
    /// <param name="room"></param>
    private void SpawnAdjacentRooms(GameObject room) {
        for (int i = 0; i < existFromRoom.Length; i++) {
            // i = 0 -> north, 1 -> east, 2 -> south, 3 -> west
            if (!existFromRoom[i]) {
                continue;
            }

            Quaternion newRotation;
            Vector3 newPosition;
            Vector2 newWorldPos;

            switch (i) {
                case 0: // north
                    newPosition = room.transform.position + new Vector3(25, 0, 0);
                    // newRotation = room.transform.rotation * Quaternion.Euler(0, 0, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(1, 0);
                    break;
                case 1: // east
                    newPosition = room.transform.position + new Vector3(0, 0, -25);
                    // newRotation = room.transform.rotation * Quaternion.Euler(0, 90, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(0, -1);
                    break;
                case 2: // south
                    newPosition = room.transform.position + new Vector3(-25, 0, 0);
                    // newRotation = room.transform.rotation * Quaternion.Euler(0, 180, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(-1, 0);
                    break;
                case 3: // west
                    newPosition = room.transform.position + new Vector3(0, 0, 25);
                    // newRotation = room.transform.rotation * Quaternion.Euler(0, 270, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(0, 1);
                    break;
                default:
                    return;
            }

            GameObject[] availableRooms = DetermineListAvailableRooms(rooms, newWorldPos);
            int randomIndex = Random.Range(0, availableRooms.Length);

            newRotation = GetRotationForRoom(availableRooms[randomIndex]);
            // newRotation = room.transform.rotation;
            // Изменить код ниже, так как планируется убрать повороты комнаты !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            ChangeExistFromRoom(i);
            SpawnRoom(randomIndex, availableRooms, newPosition, newRotation, newWorldPos);
        }
    }

    /// <summary>
    /// Эта функция делает так, что при повороте комнаты existFromRoom останется мировым а не локальным.
    /// </summary>
    /// <param name="direct"></param>
    private void ChangeExistFromRoom(int direct) {
        bool presenceNorthDoor;
        bool presenceEastDoor;
        bool presenceSouthDoor;
        bool presenceWestDoor;
        switch (direct) {
            case 0: // north
                break;
            case 1: // east
                presenceNorthDoor = existFromRoom[0];
                existFromRoom[0] = existFromRoom[1];
                existFromRoom[1] = existFromRoom[2];
                existFromRoom[2] = existFromRoom[3];
                existFromRoom[3] = presenceNorthDoor;
                break;
            case 2: // south
                presenceSouthDoor = existFromRoom[2];
                presenceWestDoor = existFromRoom[3];
                existFromRoom[0] = presenceSouthDoor;
                existFromRoom[1] = presenceWestDoor;
                existFromRoom[2] = existFromRoom[0];
                existFromRoom[3] = existFromRoom[1];
                break;
            case 3: // west
                presenceNorthDoor = existFromRoom[0];
                presenceEastDoor = existFromRoom[1];
                presenceSouthDoor = existFromRoom[2];
                existFromRoom[0] = existFromRoom[3];
                existFromRoom[1] = presenceNorthDoor;
                existFromRoom[2] = presenceEastDoor;
                existFromRoom[3] = presenceSouthDoor;
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
    private GameObject[] DetermineListAvailableRooms(GameObject[] rooms, Vector2 roomWorldPos) {
        List<GameObject> availableRooms = new List<GameObject>();
        int[] requiredRoomType = CheckRequiredRoomType(roomWorldPos);

        foreach (GameObject room in rooms) {
            bool[] exitsFromRoom = room.GetComponent<RoomManager>().exitsFromRoom;
            bool isMatch;

            for (int rotation = 0; rotation < 4; rotation++) {
                isMatch = true;

                for (int i = 0; i < 4; i++) {
                    int required = requiredRoomType[i];
                    bool exit = exitsFromRoom[(i + rotation) % 4];

                    if ((required == 1 && !exit) || (required == -1 && exit)) {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch) {
                    availableRooms.Add(room);
                    break;
                }
            }
        }

        return availableRooms.ToArray();
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
        int[] requiredRoomType = new int[4] {0, 0, 0, 0};
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
