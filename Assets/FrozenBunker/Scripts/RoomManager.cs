using System.Linq;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    // If have the exits are from the north, east and south - [true, true, true, false]
    // If have the exits are from the north, south and west - [true, false, true, true]
    public bool[] exitsFromRoom = new bool[4];
    public bool canUsedInfinitely;
    public bool isUsed;
    // Если комната не является переходом в другую зону и если центр комнаты ничем не занят 
    // (к примеру колонной), то комнату можно использовать как стартовую
    // Исключение - переходная комната, которая была переходной на другой этаж.
    [SerializeField] private bool canBeUsedAsStartingRoom;
    public bool CanBeUsedAsStartingRoom { get { return canBeUsedAsStartingRoom; } }

    [SerializeField] protected bool[] availableForNextPoolRooms = new bool[11];
    public bool[] AvailableForNextPoolRooms { get { return availableForNextPoolRooms; } }

    // ENCAPSULATION
    [SerializeField] private Vector2 worldPos2D = new(0, 0);
    public Vector2 _2DWorldPos
    {
        get { return worldPos2D; }
        set { worldPos2D = value; }
    }

    [SerializeField] private bool isImportantRoom = false;
    public bool IsImportantRoom { get { return isImportantRoom; } }
    protected SpawnRoomManager spawnRoomManager;
    public SpawnRoomManager SpawnRoomManager
    {
        set { spawnRoomManager = value; }
    }

    [SerializeField] private bool isRoomTransitional = false;
    public bool IsRoomTransitional {
        get { return isRoomTransitional; }
    }
    
    // zoneType определяется как index пула из которого берётся комната.
    // Если рядом стоят комнаты у которых разные zoneType, то между ними нет перехода.
    // Исключение - переходная комната.    
    public int zoneType;


    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) {
            // Debug.Log("TriggerEnter! In RoomManager");
            PlayerController playerController = other.GetComponent<PlayerController>();
            spawnRoomManager.DespawnOldAdjacentRooms(playerController.PlayerPos, _2DWorldPos);
            playerController.PlayerPos = _2DWorldPos;

            if (!gameObject.TryGetComponent<DeadEndRoomManager>(out DeadEndRoomManager _component)) {spawnRoomManager.SpawnAdjacentRooms(gameObject);}
            
            // if (!availableForNextPoolRooms.Last()) { spawnRoomManager.SpawnAdjacentRooms(gameObject); }
        
            // spawnRoomManager.SpawnAdjacentRooms(gameObject);
        }
    }
}