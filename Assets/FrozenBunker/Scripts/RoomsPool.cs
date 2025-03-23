using System.Collections.Generic;
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

    void Awake() {
        InitializingZonePools();
    }

    private void InitializingZonePools() {
        bool[] availableForNextPoolRooms;
        GameObject room;

        foreach (var roomPrefab in roomPrefabs) {
            availableForNextPoolRooms = roomPrefab.GetComponent<RoomManager>().AvailableForNextPoolRooms;
            for (int i = 0; i < availableForNextPoolRooms.Length; i++) {
                if (!availableForNextPoolRooms[i]) {
                    continue;
                }

                room = Instantiate(roomPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                // префабы и так неактивны, так что можно удалить нижнюю строку
                room.SetActive(false);
                room.GetComponent<RoomManager>().SpawnRoomManager = gameObject.GetComponent<SpawnRoomManager>();

                GetPoolRooms(i).Add(room);
            }
        }
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
            _ => new List<GameObject>(),
        };
    }
}
