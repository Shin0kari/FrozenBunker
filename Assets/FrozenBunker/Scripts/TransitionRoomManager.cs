using UnityEngine;

// Применяется к следующим зонам:
// poolFactoryAreaRooms, poolExitAreaRooms, poolMedicalAreaRooms
// Это те зоны, которые появляются на том же уровне, что и зона poolCommonAreaRooms
// Эти зоны генерируются не обычным способом, а сразу полностью.
// Так сделано для того, чтобы избежать генерации зоны с 0 количеством полезных зон.
// INHERITANCE
public class TransitionRoomManager : RoomManager
{
    [SerializeField] private bool isRoomLiftShaft;
    [SerializeField] private bool isRoomLadderShaft;
    [SerializeField] private int transitionToZone;
    // 0 - linear type; 1 - lift type; 2 - ladder type
    [SerializeField] private int typeTransitionRoom;
    // 1 значение - номер этажа; 2 значение - индекс зоны
    [SerializeField] private Vector2Int[] floorToZone;

    public bool IsRoomLiftShaft { get { return isRoomLiftShaft; } }
    public bool IsRoomLadderShaft { get { return isRoomLadderShaft; } }
    // обочзначает информацию о том, в какую зону будет происходить переход
    public int TransitionToZone { get { return transitionToZone; } }
    public int TypeTransitionRoom { get { return typeTransitionRoom; } }
    public Vector2Int[] FloorToZone
    {
        get { return floorToZone; }
        set { floorToZone = value; }
    }
}