using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SpawnRoomManager))]
public class SpawnNewZoneManager : MonoBehaviour
{
    [SerializeField] private int[] numOfImportantRoomsInZone = new int[10]; // длина массива - это количество зон с вычетом зоны с комнатами типа DeadEndRoom. 
    private int startedNumOfImportantRooms = -1;
    private List<GameObject> zoneRooms;
    private Dictionary<Vector2, RoomData> newRoomsData;
    private int numOfAvailableExitsFromRoomsInZone = 0;
    public PlayerController playerController;


    private Dictionary<Vector2, RoomData> roomsData;
    private List<GameObject> poolDeadEndRooms;

    private void Start() {
        SpawnRoomManager spawnRoomManager = GetComponent<SpawnRoomManager>();
        roomsData = spawnRoomManager.roomsData;
        poolDeadEndRooms = GetComponent<RoomsPool>().GetPoolRooms(-1);
    }

    public void AddImportantRoomToCounter(int indexNewZone) {
        if (indexNewZone == 10) {return;}
        numOfImportantRoomsInZone[indexNewZone] += 1;
    }

    private void UsingRoom(RoomManager roomManager) {
        if (!roomManager.isUsed) {
            // roomManager.isUsed = true;
            if (!roomManager.AvailableForNextPoolRooms.Last()) {
                roomManager.isUsed = true;
                numOfImportantRoomsInZone[roomManager.zoneType] -= 1;
            }
        }
        
        // if (!roomManager.isUsed && roomManager.IsImportantRoom) {
        //     roomManager.isUsed = true;
        //     numOfImportantRoomsInZone[roomManager.zoneType] -= 1;
        // }
    }

    public (bool, Dictionary<Vector2, RoomData>) TryToGenerateNewZone(GameObject transitionRoom, Vector2 oldPlayerPos) {
        newRoomsData = new();
        bool isNewZoneGenerated = GenNewZone(transitionRoom, oldPlayerPos);
        return (isNewZoneGenerated, newRoomsData);
    }

    private bool GenNewZone(GameObject room, Vector2 oldPlayerPos) {
        RoomManager roomManager = room.GetComponent<RoomManager>();
        int indexNewZone = room.GetComponent<TransitionRoomManager>().TransitionToZone;

        startedNumOfImportantRooms = numOfImportantRoomsInZone[indexNewZone]; // new

        zoneRooms = GetComponent<RoomsPool>().GetPoolRooms(indexNewZone);
        bool[] existFromRoom = roomManager.exitsFromRoom;

        Quaternion newRotation;
        Vector3 newPosition;
        Vector2 new2DWorldPos;

        int[] requiredRoomType;
        bool[] newRoomExitsFromRoom;

        numOfAvailableExitsFromRoomsInZone = 0;
        int minNumExits;

        for (int i = 0; i < existFromRoom.Length; i++) {
            // i = 0 -> north, 1 -> east, 2 -> south, 3 -> west
            if (!existFromRoom[i]) { // new
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
                    return false;
            }

            // Debug.Log("oldPlayerPos: (" + oldPlayerPos.x + ", " + oldPlayerPos.y + "); new2DWorldPos: " + new2DWorldPos.x + ", " + new2DWorldPos.y + ");");

            if (oldPlayerPos == new2DWorldPos) {
                continue;
            }

            minNumExits = (numOfAvailableExitsFromRoomsInZone > 1 || numOfImportantRoomsInZone[indexNewZone] < 2) ? 1 : 2;

            requiredRoomType = CheckRequiredRoomType(new2DWorldPos, indexNewZone);
            List<GameObject> availableRooms = DetermineListAvailableRooms(zoneRooms, requiredRoomType, minNumExits);

            int randomIndex = Random.Range(0, availableRooms.Count - 1);

            int newDirection = GetComponent<SpawnRoomManager>().GetDirectionForRoom(availableRooms[randomIndex], requiredRoomType);
            newRotation = Quaternion.Euler(0, newDirection * 90, 0);

            GameObject newRoom = SpawnRoom(randomIndex, availableRooms, newPosition, newRotation, new2DWorldPos);
            newRoomExitsFromRoom = newRoom.GetComponent<RoomManager>().exitsFromRoom;
            GetComponent<SpawnRoomManager>().ChangeExistFromRoom(newDirection, newRoomExitsFromRoom);

            newRoom.GetComponent<NewZoneRoomManager>().ZoneParentCreateRoom = (roomManager._2DWorldPos, room);
            SpawnNewRoomInZone(newRoom);
        }

        return !(startedNumOfImportantRooms == numOfImportantRoomsInZone[indexNewZone]); // new
    }

    private void SpawnNewRoomInZone(GameObject parentRoom) {
        Debug.Log("SpawnNewRoomInZone!");
        RoomManager roomManager = parentRoom.GetComponent<RoomManager>();
        if (roomManager.zoneType == -1) {return;}
        int indexNewZone = roomManager.zoneType;

        zoneRooms = GetComponent<RoomsPool>().GetPoolRooms(indexNewZone);

        Quaternion newRotation;
        Vector3 newPosition;
        Vector2 new2DWorldPos;

        int[] requiredRoomType;
        bool[] newRoomExitsFromRoom;

        int minNumExits;

        bool[] existFromRoom = roomManager.exitsFromRoom;
        NewZoneRoomManager newZoneRoomManager = parentRoom.GetComponent<NewZoneRoomManager>();
        bool[] chechedAdjacentRoom = newZoneRoomManager.IsZoneRoomCreated;
        
        for (int numCheckedRoom = 0; numCheckedRoom < chechedAdjacentRoom.Length; numCheckedRoom++) {
            if (!existFromRoom[numCheckedRoom]) {
                chechedAdjacentRoom[numCheckedRoom] = true;
                continue;
            }
            if (chechedAdjacentRoom[numCheckedRoom]) {continue;}

            switch (numCheckedRoom) {
                case 0: // north
                    newPosition = parentRoom.transform.position + new Vector3(25, 0, 0);
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(1, 0);
                    break;
                case 1: // east
                    newPosition = parentRoom.transform.position + new Vector3(0, 0, -25);
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(0, -1);
                    break;
                case 2: // south
                    newPosition = parentRoom.transform.position + new Vector3(-25, 0, 0);
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(-1, 0);
                    break;
                case 3: // west
                    newPosition = parentRoom.transform.position + new Vector3(0, 0, 25);
                    new2DWorldPos = roomManager._2DWorldPos + new Vector2(0, 1);
                    break;
                default:
                    return;
            }
            if (newRoomsData.ContainsKey(new2DWorldPos) || roomsData.ContainsKey(new2DWorldPos)) {
                chechedAdjacentRoom[numCheckedRoom] = true;
                continue;
            }

            minNumExits = (numOfAvailableExitsFromRoomsInZone > 1 || numOfImportantRoomsInZone[indexNewZone] < 2) ? 1 : 2;

            requiredRoomType = CheckRequiredRoomType(new2DWorldPos, indexNewZone);
            Debug.Log("requiredRoomType: [" + requiredRoomType[0] + ", " + requiredRoomType[1] + ", " + requiredRoomType[2] + ", " + requiredRoomType[3] + "]");
            List<GameObject> availableRooms = DetermineListAvailableRooms(zoneRooms, requiredRoomType, minNumExits);

            int randomIndex = Random.Range(0, availableRooms.Count - 1);

            int newDirection = GetComponent<SpawnRoomManager>().GetDirectionForRoom(availableRooms[randomIndex], requiredRoomType);
            newRotation = Quaternion.Euler(0, newDirection * 90, 0);

            GameObject newRoom = SpawnRoom(randomIndex, availableRooms, newPosition, newRotation, new2DWorldPos);
            RoomManager newRoomManager = newRoom.GetComponent<RoomManager>();
            newRoomExitsFromRoom = newRoomManager.exitsFromRoom;
            GetComponent<SpawnRoomManager>().ChangeExistFromRoom(newDirection, newRoomExitsFromRoom);

            if (!newRoom.TryGetComponent(out DeadEndRoomManager _component)) {newRoom.GetComponent<NewZoneRoomManager>().ZoneParentCreateRoom = (roomManager._2DWorldPos, parentRoom);}
            SpawnNewRoomInZone(newRoom);
            chechedAdjacentRoom[numCheckedRoom] = true;
        }
    }

    protected bool CheckIsMatchForDetermineAvailableDirection(bool[] exitsFromRoom, int rotation, int[] requiredRoomType, int minNumExitsInRoom) {
        bool isMatch = true;
        int required;
        int numExits = 0;
        bool exit;

        // Проверяем, чтобы все выходы для комнаты были допустимы
        for (int i = 0; i < 4; i++) {
            required = requiredRoomType[i];
            exit = exitsFromRoom[(i + (4 - rotation)) % 4];
            if (exit) {numExits++;}

            if ((required == 1 && !exit) || (required == -1 && exit) || required == -2) {
                isMatch = false;
                continue;
            }

            // чтобы посчитать количество доступных выходов, сначала прибавляем все возможные выходы, а затем отнимаем (4 - количество выходов в комнате).
            if (required != -1) {numOfAvailableExitsFromRoomsInZone++;}
        }

        if (numExits < minNumExitsInRoom) {isMatch = false;}

        return isMatch;
    }

    protected List<GameObject> DetermineListAvailableRooms(List<GameObject> rooms, int[] requiredRoomType, int minNumExitsInRoom) {
        List<GameObject> availableRooms = new();
        RoomManager roomManager;
        bool[] exitsFromRoom;
        bool isMatch;

        foreach (GameObject room in rooms) {
            roomManager = room.GetComponent<RoomManager>();
            exitsFromRoom = roomManager.exitsFromRoom;

            for (int rotation = 0; rotation < 4; rotation++) {
                isMatch = CheckIsMatchForDetermineAvailableDirection(exitsFromRoom, rotation, requiredRoomType, minNumExitsInRoom);

                if (isMatch && !roomManager.isUsed) {
                    availableRooms.Add(room);
                    break;
                }
            }
        }

        // Если больше нет доступных для использования комнат, то используется комната-заглушка. 
        if (availableRooms.Count == 0) {
            foreach (var deadEndRoom in poolDeadEndRooms) {
                if (!deadEndRoom.GetComponent<RoomManager>().isUsed) {
                    availableRooms.Add(deadEndRoom);
                }
            }
        }

        return availableRooms;
    }

    private GameObject SpawnRoom(int randomIndex, List<GameObject> rooms, Vector3 worldPos, Quaternion rotation, Vector2 new2DPos) {
        Debug.Log("SpawnRoom!");
        if (newRoomsData.ContainsKey(new2DPos)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

    
        int numWallsWithoutExit = 0;
        GameObject room = rooms[randomIndex];
        RoomManager roomManager = room.GetComponent<RoomManager>();

        room.transform.SetPositionAndRotation(worldPos, rotation);
        
        UsingRoom(roomManager);
        
        roomManager._2DWorldPos = new2DPos;

        RoomData newRoomData = new()
        {
            RoomObject = room,
            Rotation = rotation.eulerAngles.y,
            // Loot = GenerateLoot()
        };

        newRoomsData.Add(new2DPos, newRoomData); // Добавляем новую позицию в HashSet
        if (roomManager.IsImportantRoom) {
            numOfImportantRoomsInZone[roomManager.zoneType] -= 1;
        }
        
        // чтобы посчитать количество доступных выходов, сначала прибавляем все возможные выходы, а затем отнимаем (4 - количество выходов в комнате).
        foreach (var exit in roomManager.exitsFromRoom) {if (!exit) {numWallsWithoutExit++;}}
        numOfAvailableExitsFromRoomsInZone -= numWallsWithoutExit;

        return room;
    }

    protected int[] CheckRequiredRoomType(Vector2 roomWorldPos, int indexCheckedZone) {
        Debug.Log("CheckRequiredRoomType!");
        // 0 - комната не создана; 1 - смежная комната имеет общую дверь с этой комнатой; -1 - смежная комната не имеет общей двери с этой комнатой
        int[] requiredRoomType = new int[4];
        
        for (int i = 0; i < 4; i++) {
            switch (i) {
                case 0: // north
                    CheckAdjacentRoom(roomWorldPos + new Vector2(1, 0), requiredRoomType, i, indexCheckedZone);
                    break;
                case 1: // east
                    CheckAdjacentRoom(roomWorldPos + new Vector2(0, -1), requiredRoomType, i, indexCheckedZone);
                    break;
                case 2: // south
                    CheckAdjacentRoom(roomWorldPos + new Vector2(-1, 0), requiredRoomType, i, indexCheckedZone);
                    break;
                case 3: // west
                    CheckAdjacentRoom(roomWorldPos + new Vector2(0, 1), requiredRoomType, i, indexCheckedZone);
                    break;
            }
        }

        return requiredRoomType;
    }

    protected void CheckAdjacentRoom(Vector2 adjacentRoomWorldPos, int[] requiredRoomType, int index, int indexCheckedZone)
    {
        // Debug.Log("CheckAdjacentRoom!");
        // Debug.Log("Count roomsData: " + roomsData.Count);
        // Debug.Log("adjacentRoomWorldPos: (" + adjacentRoomWorldPos.x + ", " + adjacentRoomWorldPos.y + ");");
        RoomData roomData;
        if (!roomsData.ContainsKey(adjacentRoomWorldPos)) {
            if (!newRoomsData.ContainsKey(adjacentRoomWorldPos)) {
                return;
            } else {
                newRoomsData.TryGetValue(adjacentRoomWorldPos, out roomData);
            }
        } else {
            roomsData.TryGetValue(adjacentRoomWorldPos, out roomData);
        }

        GameObject adjacentRoom = roomData.RoomObject;
        int zoneType;
        bool[] exitsFromRoom;
        bool isRoomTransitional = false;

        if (adjacentRoom.TryGetComponent(out TransitionRoomManager transitionRoomManager)) {
            Debug.Log("Room is Transitional!");
            isRoomTransitional = true;
            zoneType = transitionRoomManager.zoneType;
            exitsFromRoom = transitionRoomManager.exitsFromRoom;
        } else {
            zoneType = adjacentRoom.GetComponent<RoomManager>().zoneType;
            exitsFromRoom = adjacentRoom.GetComponent<RoomManager>().exitsFromRoom;
        }

        // В части где индекс, мы меняем местами север с югом, запад с востоком, 
        // так как на северной смежной карте (вверхней соседней комнате) должен быть выход на юг в комнату в которой находится игрок
        if (exitsFromRoom[(index + 2) % 4] ) {
            requiredRoomType[index] = 1;
        } else {
            requiredRoomType[index] = -1;
        }
        // true /\ true /\ false) = true
        if (!(requiredRoomType[index] == -1) && (zoneType != indexCheckedZone) && (!isRoomTransitional)) {
            // Делаем так, чтобы если соседняя комната относится к другой зоне, то мы специально делаем неподходящий для всех комнат тип, чтобы заспавнилась DeadEndRoom
            Debug.Log("Change requiredRoomType[index] to -2");
            requiredRoomType[index] = -2;
        }
    }
}
