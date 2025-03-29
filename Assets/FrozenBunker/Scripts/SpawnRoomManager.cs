using UnityEngine;
using System.Collections.Generic;

public class RoomData
{
    public GameObject RoomObject;
    public float Rotation;
    // public List<ChestLoot> Loot { get; set; } // Пример: список сундуков
}

public class ChestLoot
{
    public List<GameObject> Items;
}

public class SpawnRoomManager : MonoBehaviour
{
    [SerializeField] private GameRuleManager gameRuleManager;
    [SerializeField] private GameObject deadEndRoom;
    private List<GameObject> rooms;
    [SerializeField] private int numOfZones = 10;
    [SerializeField] private Vector3 startPosition = new(0, 0, -12);
    private Dictionary<Vector2, RoomData> roomsData;

    public Quaternion NorthTestQuaternion;
    public Quaternion EastTestQuaternion;
    public Quaternion SouthTestQuaternion;
    public Quaternion WestTestQuaternion;

    void Start()
    {
        // необходимо добавить проверку, если игра была уже когда то сохранена и загружена, то заново спавнить комнату не нужно 
        roomsData = new();
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
        // room.GetComponent<BoxCollider>().enabled = false;
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
        if (roomsData.ContainsKey(new2DPos)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }
    
        GameObject room = rooms[randomIndex];
        RoomManager roomManager = room.GetComponent<RoomManager>();

        if (roomManager.CanBeUsedAsDeadEndRoom) {
            room = Instantiate(room, worldPos, rotation);
            roomManager = room.GetComponent<RoomManager>();
            roomManager.SpawnRoomManager = gameObject.GetComponent<SpawnRoomManager>();
        } else {
            room.transform.SetPositionAndRotation(worldPos, rotation);
            roomManager.isUsed = true;
        }
        
        // GameObject room = Instantiate(rooms[randomIndex], worldPos, rotation);
        roomManager._2DWorldPos = new2DPos;
        room.SetActive(true);

        RoomData newRoomData = new()
        {
            RoomObject = room,
            Rotation = rotation.eulerAngles.y,
            // Loot = GenerateLoot()
        };

        roomsData.Add(new2DPos, newRoomData); // Добавляем новую позицию в HashSet
        return room;
    }

    private bool IsRoomCreated(Vector2 new2DPos) {
        if (roomsData.ContainsKey(new2DPos)) {
            roomsData.TryGetValue(new2DPos, out RoomData roomData);
            GameObject room = roomData.RoomObject;
            Quaternion newRotation = Quaternion.Euler(0, roomData.Rotation, 0); 
            room.transform.SetPositionAndRotation(new Vector3(new2DPos.x * 25, 0, new2DPos.y * 25), newRotation);
            room.SetActive(true);
            RoomManager roomManager = room.GetComponent<RoomManager>(); 
            roomManager.isUsed = true;
            roomManager._2DWorldPos = new2DPos;
            return true; // Если позиция уже занята, не создаем новую комнату
        }
        else return false;
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
    /// Переносит данные о выходах комнаты exitsFromRoom из скрипта RoomManager в SpawnRoomManager.
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
    public void SpawnAdjacentRooms(GameObject room) {
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
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(1, 0);
                    break;
                case 1: // east
                    newPosition = room.transform.position + new Vector3(0, 0, -25);
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(0, -1);
                    break;
                case 2: // south
                    newPosition = room.transform.position + new Vector3(-25, 0, 0);
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(-1, 0);
                    break;
                case 3: // west
                    newPosition = room.transform.position + new Vector3(0, 0, 25);
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(0, 1);
                    break;
                default:
                    return;
            }

            if (IsRoomCreated(new2DWorldPos)) {
                continue;
            }

            requiredRoomType = CheckRequiredRoomType(new2DWorldPos);
            // добавить проверку, если включён режим бесконечной игры, то проверяется не 4 комнаты, а 12.
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
    /// К примеру, у нас была комната с следующим existFromRoom: [true, false, false, false].
    /// Мы поворачиваем комнату по часовой стрелке на 90 градусов.
    /// Эта функция изменит для конматы existFromRoom следующим образом: [false, true, false, false]
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

        // Если больше нет доступных для использования комнат, то используется комната-заглушка. 
        if (availableRooms.Count == 0) {availableRooms.Add(deadEndRoom);}

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

        // Проверяем, чтобы все выходы для комнаты были допустимы
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

        if (roomsData.ContainsKey(adjacentRoomWorldPos)) {
            roomsData.TryGetValue(adjacentRoomWorldPos, out RoomData roomData);
            GameObject adjacentRoom = roomData.RoomObject;

            // В части где индекс, мы меняем местами север с югом, запад с востоком, 
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

    /// <summary>
    /// Относительно прошлой комнаты игрока деспавнит соседние комнаты, кроме комнаты в которую пошёл игрок.
    /// </summary>
    /// <param name="oldPlayerPos"></param>
    /// <param name="newPlayerPos"></param>
    public void DespawnOldAdjacentRooms(Vector2 oldPlayerPos, Vector2 newPlayerPos) {
        // пропускает срабатывание функции для стартовой комнаты 
        // (или комнаты в которой появился игрок при переходе в другие зоны по специальным комнатам)
        if (oldPlayerPos == newPlayerPos) {
            return;
        }

        Vector2 playerPosChangeValue = newPlayerPos - oldPlayerPos;
        Vector2[] directions = new Vector2[] {
            new(1, 0),
            new(0, -1),
            new(-1, 0),
            new(0, 1)
        };
        RoomManager roomManager;

        foreach (Vector2 direction in directions)
        {
            if (direction == playerPosChangeValue) {
                continue;
            }
            Vector2 adjacentRoomWorldPos = oldPlayerPos + direction;
            
            if (roomsData.ContainsKey(adjacentRoomWorldPos)) {
                roomsData.TryGetValue(adjacentRoomWorldPos, out RoomData roomData);
                GameObject adjacentRoom = roomData.RoomObject;
                roomManager = adjacentRoom.GetComponent<RoomManager>();
                if (roomManager.canUsedInfinitely && gameRuleManager.IsInfinityGame) {
                    roomManager.isUsed = false;
                }
                adjacentRoom.SetActive(false);
            }
        }
    }
}
