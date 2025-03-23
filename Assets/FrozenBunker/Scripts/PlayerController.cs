using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector2 playerPos;
    public Vector2 PlayerPos
    {
        get { return playerPos; }
        set { playerPos = value; }
    }
}
