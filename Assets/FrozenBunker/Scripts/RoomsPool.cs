using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ListExtensions
{
    public static T GetRandom<T>(this IList<T> list) => list[Random.Range(0, list.Count)];
}

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

    [SerializeField] private int countPools;
    [SerializeField] private int[] countImportantZoneRooms;

    [SerializeField] private int[] linearTransitionalType;
    [SerializeField] private int[] liftTransitionalType;
    [SerializeField] private int[] ladderTransitionalType;

    public int[] LinearTransitionalTypes { get { return linearTransitionalType; } }
    public int[] LiftTransitionalTypes { get { return liftTransitionalType; } }
    public int[] LadderTransitionalTypes { get { return ladderTransitionalType; } }

    public Material[] newZoneMaterials;
    public int GetCountPools { get { return countPools; } }

    void Awake()
    {
        // ABSTRACTION
        InitStartData();
        InitializingZoneDataFromPrefabs();
    }

    private void InitStartData()
    {
        countPools = roomPrefabs[0].GetComponent<RoomManager>().AvailableForNextPoolRooms.Length;
        countImportantZoneRooms = new int[countPools];
    }

    private void InitializingZoneDataFromPrefabs()
    {
        GameObject room;
        RoomManager roomManager;
        bool[] availableForNextPoolRooms;

        foreach (var roomPrefab in roomPrefabs)
        {
            availableForNextPoolRooms = roomPrefab.GetComponent<RoomManager>().AvailableForNextPoolRooms;

            for (int zoneType = 0; zoneType < countPools; zoneType++)
            {
                if (!availableForNextPoolRooms[zoneType])
                {
                    continue;
                }

                room = Instantiate(roomPrefab, Vector3.zero, Quaternion.Euler(Vector3.zero));
                // префабы и так неактивны, так что можно удалить нижнюю строку
                room.SetActive(false);
                roomManager = room.GetComponent<RoomManager>();

                switch (zoneType)
                {
                    case var type when type == countPools - 1:
                        roomManager.zoneType = -1;
                        room.AddComponent<DeadEndRoomManager>();
                        break;
                    case var type when type != 0 && !roomManager.IsRoomTransitional:
                        roomManager.zoneType = zoneType;

                        var childrenComponent = room.GetComponentInChildren<ChangeZoneColor>();
                        childrenComponent.newZoneMaterial = GetColorFromColorPool(roomManager.zoneType);
                        childrenComponent.ChangeColor();
                        break;
                    default:
                        roomManager.zoneType = zoneType;
                        break;
                }

                AddingRoomInZonePool(room, roomManager, zoneType);
                AddImportantRoomToCounter(zoneType);
            }
        }
    }

    private void AddImportantRoomToCounter(int zoneIndex)
    {
        if (!CheckZoneIndex(zoneIndex)) { return; }
        countImportantZoneRooms[zoneIndex]++;
    }

    public int GetCountImportantRoomInZone(int zoneIndex)
    {
        if (!CheckZoneIndex(zoneIndex)) { return 0; }
        return countImportantZoneRooms[zoneIndex];
    }

    public void SubtractCountImportantRoomInZone(int zoneIndex)
    {
        if (!CheckZoneIndex(zoneIndex)) { return; }
        countImportantZoneRooms[zoneIndex]--;
    }

    private bool CheckZoneIndex(int zoneIndex)
    {
        if (zoneIndex > countPools - 1 || zoneIndex < 0)
            return false;

        return true;
    }

    private void AddingRoomInZonePool(GameObject room, RoomManager roomManager, int zoneType)
    {
        roomManager.SpawnRoomManager = GetComponent<SpawnRoomManager>();
        GetPoolRooms(zoneType).Add(room);
    }

    public List<GameObject> GetPoolRooms(int index)
    {
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

    private Material GetColorFromColorPool(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex > newZoneMaterials.Length)
        {
            Debug.LogError("Can`t change color! Incorrect zone index!");
        }
        return newZoneMaterials[zoneIndex];
    }
}
