using System;
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

    [SerializeField] private List<GameObject> poolDeadEndRooms = new();

    [SerializeField] private int[] countImportantZoneRooms = new int[10];

    [SerializeField] private int[] linearTransitionalType;
    [SerializeField] private int[] liftTransitionalType;
    [SerializeField] private int[] ladderTransitionalType;

    public int[] LinearTransitionalTypes { get { return linearTransitionalType; } }
    public int[] LiftTransitionalTypes { get { return liftTransitionalType; } }
    public int[] LadderTransitionalTypes { get { return ladderTransitionalType; } }

    public Material[] newZoneMaterials;

    /*
    Комната должна хранить инфу, на каком этаже она расположена
    
    Начало пола следующего этажа - 25 м * 2 = 50 м
    
    Необходимо, чтобы спавн лестницы или лифта проверял, 
    есть ли свободное место для спавна "стартовой комнаты" новой зоны
    
    После спавна лифта или лестницы должна заспавнится как минимум 1 комната (стартовая комната новой зоны)

    Добавить проверку для переходной лестницы и лифта, чтобы их шахты не имели "проходов внутрь"
    Т.е. если на высоте 0 начало лифта, на высоте 1 шахта лифта с координатами 0 50 0, 
    то комнаты +-25 50 0 и 0 50 +-25 не имели проходов в шахту лифта

    Переходная комната для обычной является комнатой без выходов. 
    А в данный момент проходная комната имеет 2 выхода для статистики обычной, что является неверным.
    (баг исправлен)  

    Если этажи закрывают спавн лифта для важной зоны, и других мест нет, то зона не заспавнится.
    !!! баг !!! (решение - сделать тогда спав новой зоны с помощью новой сцены и перехода)

    Не правильный подсчёт количества потраченных проходов: 
    если имеется комната на 0 0 0 и комната 50 0 0, 
    и при нахождении на комнате 0 0 0 спавнится deadEndeRoom, то мы отнимаем 1 проход, 
    а возможно надо отнять больше одного.
    (баг исправлен)

    Будет 3 вида переходов: 
    1. На тот же этаж но в другую зону (используем скрипт спавна новой зоны)
        Этаж    :   зона-зона
        0       :   0-6, 0-7
        -1      :   0-1, 0-3, 0-8 
    2. Лестница на этажи выше или ниже (используем скрипт спавна обычной зоны)
        Этаж    -   Этаж    :   зона-зона
        -1      -   -2      :   0-5
        -1      -   -3      :   0-9

    (переход из скрипта  спавна новой зоны в скрипт спавна обычной зоны)
    2.1. Лестница на этаж выше или ниже но для той же зоны (используем скрипт спавна обычной зоны)
        Этаж    :   зона
        -1      :   1, 8 
    3. Лифт на этажи выше или ниже (используем скрипт спавна обычной зоны)
        Этаж    -   Этаж :  зона-зона
        0       -   -1   :  0-0
        -1      -   1    :  0-2, 0-4

    (переход из скрипта  спавна новой зоны в скрипт спавна обычной зоны)
    3.1. Лифт на этажи выше или ниже но для той же зоны (используем скрипт спавна обычной зоны)
        Этаж    :   зона
        -1      :   1, 8

    // если TransitionToZone == x, то используется спавн новой зоны, иначе спавн одной комнаты из новой зоны
    // 1 этаж - poolCommunicationAreaRooms(4), poolHydroponicAreaRooms(2)
    // 0 этаж - poolExitAreaRooms(6), poolCommonAreaRooms(0), poolWarehouseAreaRooms(7)
    // -1 этаж - poolCommonAreaRooms(0), poolFactoryAreaRooms(3), poolMedicalAreaRooms(8), poolLivingAreaRooms(1) 
    // -2 этаж - poolEnergyProductionAreaRooms(5)
    // -3 этаж - poolHeatingAreaRooms(9)

    */

    protected virtual void Awake()
    {
        // ABSTRACTION
        InitializingZoneDataFromPrefabs();
    }

    private void InitializingZoneDataFromPrefabs()
    {
        bool[] availableForNextPoolRooms;
        GameObject room;
        RoomManager roomManager;

        foreach (var roomPrefab in roomPrefabs)
        {
            availableForNextPoolRooms = roomPrefab.GetComponent<RoomManager>().AvailableForNextPoolRooms;

            for (int zoneType = 0; zoneType < availableForNextPoolRooms.Length; zoneType++)
            {
                if (!availableForNextPoolRooms[zoneType])
                {
                    continue;
                }

                room = Instantiate(roomPrefab, Vector3.zero, Quaternion.Euler(Vector3.zero));

                room.SetActive(false);
                roomManager = room.GetComponent<RoomManager>();
                // if (roomManager.IsRoomTransitional)
                // {
                //     continue;
                // }
                

                switch (zoneType)
                {
                    case var type when type == availableForNextPoolRooms.Length - 1:
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

                FixRoomData(room);
                AddingRoomInZonePool(room, roomManager, zoneType);
                if (roomManager.IsImportantRoom) {AddImportantRoomToCounter(roomManager.zoneType);}
            }
        }
    }

    private void AddImportantRoomToCounter(int indexNewZone)
    {
        if (indexNewZone == -1) { return; }
        countImportantZoneRooms[indexNewZone] += 1;
    }

    public int GetCountImportantRoomInZone(int zoneIndex)
    {
        if (CheckZoneIndex(zoneIndex))
        {
            return 0;
        }
        return countImportantZoneRooms[zoneIndex];
    }

    public void SubtractCountImportantRoomInZone(int zoneIndex)
    {
        if (CheckZoneIndex(zoneIndex))
        {
            return;
        }
        countImportantZoneRooms[zoneIndex]--;
    }

    private bool CheckZoneIndex(int zoneIndex)
    {
        if (zoneIndex > roomPrefabs[0].GetComponent<RoomManager>().AvailableForNextPoolRooms.Length - 1 || zoneIndex < 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void FixRoomData(GameObject room)
    {
        RoomManager roomManager = room.GetComponent<RoomManager>();

        if (roomManager.IsRoomTransitional)
        {
            TransitionRoomManager transitionRoomManager = room.GetComponent<TransitionRoomManager>();
            SortFloorToZoneArray(transitionRoomManager);
        }
    }

    private void SortFloorToZoneArray(TransitionRoomManager transitionRoomManager)
    {
        Vector2Int[] floorToZone = transitionRoomManager.FloorToZone;
        Array.Sort(floorToZone, (a, b) => a.x.CompareTo(b.x));
        transitionRoomManager.FloorToZone = floorToZone;
    }

    private void AddingRoomInZonePool(GameObject room, RoomManager roomManager, int zoneType)
    {
        roomManager.SpawnRoomManager = GetComponent<SpawnRoomManager>();
        GetPoolRooms(zoneType).Add(room);
    }

    public virtual List<GameObject> GetPoolRooms(int index)
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
