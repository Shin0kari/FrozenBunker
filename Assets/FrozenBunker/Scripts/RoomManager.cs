using UnityEngine;

public class RoomManager : MonoBehaviour
{
    // If have the exits are from the north, east and south - [true, true, true, false]
    // If have the exits are from the north, south and west - [true, false, true, true]
    public bool[] exitsFromRoom = new bool[4];
    public bool canUsedInfinitely;
    public bool isUsed;
    // Если комната не является переходом в другую зону и если центр комнаты ничем не занят (к примерру колонной),
    // то комнату можно использовать как стартовую
    [SerializeField] private bool canBeUsedAsStartingRoom;
    public bool CanBeUsedAsStartingRoom { get { return canBeUsedAsStartingRoom; } }

    [SerializeField] private bool[] availableForNextPoolRooms = new bool[10];
    public bool[] AvailableForNextPoolRooms { get { return availableForNextPoolRooms; } }

    private Vector2 worldPos = new(0, 0);
    public Vector2 WorldPos
    {
        get { return worldPos; }
        set { worldPos = value; }
    }
}