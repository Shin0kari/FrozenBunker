using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector3 playerPos;
    public Vector3 PlayerPos
    {
        get { return playerPos; }
        set { playerPos = value; }
    }
}
