using UnityEngine;

// Применяется к следующим зонам:
// poolFactoryAreaRooms, poolExitAreaRooms, poolMedicalAreaRooms
// Это те зоны, которые появляются на том же уровне, что и зона poolCommonAreaRooms
// Эти зоны генерируются не обычным способом, а сразу полностью.
// Так сделано для того, чтобы избежать генерации зоны с 0 количеством полезных зон.
// INHERITANCE
public class TransitionRoomManager : RoomManager 
{
    [SerializeField] private int transitionToZone;
    public int TransitionToZone { get {return transitionToZone; } }
}