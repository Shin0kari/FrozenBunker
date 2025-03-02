using UnityEngine;

public class RoomManager : MonoBehaviour
{
    // If have the exits are from the north, east and south - [true, true, true, false]
    // If have the exits are from the north, south and west - [true, false, true, true]
    public bool[] exitsFromRoom = new bool[4];
    private Vector2 worldPos = new Vector2(0, 0);

    public Vector2 WorldPos
    {
        get { return worldPos; }
        set { worldPos = value; }
    }
}