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
    public bool CanBeUsedAsStartingRoom         { get { return canBeUsedAsStartingRoom; } }
    public bool IsImportantRoom                 { get { return isImportantRoom; } }
    public bool IsRoomTransitional              { get { return isRoomTransitional; } }
    public bool[] AvailableForNextPoolRooms     { get { return availableForNextPoolRooms; } }
    public SpawnRoomManager SpawnRoomManager    { set { spawnRoomManager = value; } }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            var worldPos = gameObject.GetComponent<Transform>().position;

            if (!gameObject.TryGetComponent(out DeadEndRoomManager _component))
            {
                spawnRoomManager.DespawnOldAdjacentRooms(playerController.PlayerPos, worldPos);
                spawnRoomManager.SpawnAdjacentRooms(gameObject);
            }
            
            playerController.PlayerPos = worldPos;
        }
    }
}