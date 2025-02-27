using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public bool[] exitsFromRoom = new bool[4]; // If the exits are from the north, east and south - [true, true, true, false]
    private Vector2 worldPos = new Vector2(0, 0);

    public Vector2 WorldPos
    {
        get { return worldPos; }
        set { worldPos = value; }
    }
}