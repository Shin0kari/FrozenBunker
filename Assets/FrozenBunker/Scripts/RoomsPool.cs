using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomsPool : MonoBehaviour
{
    [SerializeField] private GameObject[] roomPrefabs;
    // Количество комнат в каждой зоне должно быть не меньше 5 (возможно 5 * 4 = 20). Иначе есть шанс, что соседняя комната не заспавнится. 
    // Так как когда игрок появляется, есть шанс спавна 4 смежных комнат (а с учётом стартовой комнаты их 5). 
    // И при переходе в соседнюю комнату, комнаты, которые находятся через одну комнату, выключаются и снова становятся доступны для спавна
    [SerializeField] private List<GameObject> poolCommonAreaRooms = new();
    [SerializeField] private List<GameObject> poolLivingAreaRooms = new();
    [SerializeField] private List<GameObject> poolHydroponicAreaRooms = new();
    [SerializeField] private List<GameObject> poolFactoryAreaRooms = new();
    [SerializeField] private List<GameObject> poolCommunicationAreaRooms = new();
    [SerializeField] private List<GameObject> poolEnergyProductionAreaRooms = new();
    [SerializeField] private List<GameObject> poolExitAreaRooms = new();
    [SerializeField] private List<GameObject> poolWarehouseAreaRooms = new();
    [SerializeField] private List<GameObject> poolMedicalAreaRooms = new();
    [SerializeField] private List<GameObject> poolHeatingAreaRooms = new();

    [SerializeField] private List<GameObject> poolDeadEndRooms = new();

    void Awake() {
        // ABSTRACTION
        InitializingZoneDataFromPrefabs();
    }

    private void InitializingZoneDataFromPrefabs() {
        bool[] availableForNextPoolRooms;
        GameObject room;
        RoomManager roomManager;

        foreach (var roomPrefab in roomPrefabs) {
            availableForNextPoolRooms = roomPrefab.GetComponent<RoomManager>().AvailableForNextPoolRooms;
            
            for (int zoneType = 0; zoneType < availableForNextPoolRooms.Length; zoneType++) {
                if (!availableForNextPoolRooms[zoneType]) {
                    continue;
                }

                room = Instantiate(roomPrefab, Vector3.zero, Quaternion.Euler(Vector3.zero));
                // префабы и так неактивны, так что можно удалить нижнюю строку
                room.SetActive(false);
                roomManager = room.GetComponent<RoomManager>();

                switch (zoneType) {
                    case var type when type == 3 || type == 6 || type == 8:
                        room.AddComponent<NewZoneRoomManager>();
                        room.GetComponentInChildren<ChangeZoneColor>().ChangeColor();
                        roomManager.zoneType = zoneType; 
                        break;
                    case var type when type == availableForNextPoolRooms.Length - 1:
                        room.AddComponent<DeadEndRoomManager>();
                        roomManager.zoneType = -1;
                        break;
                    default:
                        roomManager.zoneType = zoneType;
                        break;
                }

                AddingRoomInZonePool(room, roomManager, zoneType);
                GetComponent<SpawnNewZoneManager>().AddImportantRoomToCounter(zoneType);
            }
        }
    }

    private void AddingRoomInZonePool(GameObject room, RoomManager roomManager, int zoneType) {
        roomManager.SpawnRoomManager = GetComponent<SpawnRoomManager>();
        GetPoolRooms(zoneType).Add(room);
    }

    public List<GameObject> GetPoolRooms(int index) {
        return index switch
        {
            0 => poolCommonAreaRooms,
            1 => poolLivingAreaRooms,
            2 => poolHydroponicAreaRooms,
            3 => poolFactoryAreaRooms,
            4 => poolCommunicationAreaRooms,
            5 => poolEnergyProductionAreaRooms,
            6 => poolExitAreaRooms,
            7 => poolWarehouseAreaRooms,
            8 => poolMedicalAreaRooms,
            9 => poolHeatingAreaRooms,
            _ => poolDeadEndRooms,
        };
    }
}
