using System.Linq;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    // Если комната не является переходом в другую зону и если центр комнаты ничем не занят 
    // (к примеру колонной), то комнату можно использовать как стартовую
    // Исключение - переходная комната, которая была переходной на другой этаж.
    [SerializeField] private bool canBeUsedAsStartingRoom;
    [SerializeField] private bool isImportantRoom = false;
    [SerializeField] private bool isRoomTransitional = false;
    [SerializeField] private bool[] availableForNextPoolRooms = new bool[11];
    [SerializeField] private Vector3 localPos3D = Vector3.zero;
    private SpawnRoomManager spawnRoomManager;

    // zoneType определяется как index пула из которого берётся комната.
    // Если рядом стоят комнаты у которых разные zoneType, то между ними нет перехода.
    // Исключение - переходная комната.    
    public int zoneType;
    // If have the exits are from the north, east and south - [true, true, true, false]
    // If have the exits are from the north, south and west - [true, false, true, true]
    public bool[] exitsFromRoom = new bool[4];
    public bool canUsedInfinitely;
    public bool isUsed;

    // ENCAPSULATION
    public bool CanBeUsedAsStartingRoom { get { return canBeUsedAsStartingRoom; } }
    public bool IsImportantRoom { get { return isImportantRoom; } }
    public bool IsRoomTransitional { get { return isRoomTransitional; } }
    public bool[] AvailableForNextPoolRooms { get { return availableForNextPoolRooms; } }
    public Vector3 LocalPos { get { return localPos3D; } set { localPos3D = value; } }
    public SpawnRoomManager SpawnRoomManager { set { spawnRoomManager = value; } }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) {
            PlayerController playerController = other.GetComponent<PlayerController>();
            // Debug.Log("Start Despawn!");
            spawnRoomManager.DespawnOldAdjacentRooms(playerController.PlayerPos, LocalPos);
            playerController.PlayerPos = LocalPos;

            if (!gameObject.TryGetComponent(out DeadEndRoomManager _component)) 
                spawnRoomManager.SpawnAdjacentRooms(gameObject);
        }
    }
}