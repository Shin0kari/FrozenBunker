using UnityEngine;

// Применяется к следующим зонам:
// poolFactoryAreaRooms, poolExitAreaRooms, poolMedicalAreaRooms
// Это те зоны, которые появляются на том же уровне, что и зона poolCommonAreaRooms
// Эти зоны генерируются не обычным способом, а сразу полностью.
// Так сделано для того, чтобы избежать генерации зоны с 0 количеством полезных зон.
// INHERITANCE
public class TransitionRoomManager : RoomManager 
{
    // !!! баг !!! Если TransitionRoom клонировать с помощью availableForNextPoolRooms
    // то перенесутся и настройки по типу "на каком этаже может находится эта переходная комната"
    [SerializeField] private int transitionToZone;
    [SerializeField] private float numFloor;
    [SerializeField] private int transitionType;
    public int TransitionToZone { get { return transitionToZone; } }
    public float GetNumFloor { get { return numFloor; } }
    public int TypeTransitionRoom { get { return transitionType; } }
}