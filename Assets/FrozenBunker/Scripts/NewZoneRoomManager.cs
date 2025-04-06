using UnityEngine;

// Применяется к следующим зонам:
// poolFactoryAreaRooms, poolExitAreaRooms, poolMedicalAreaRooms
// Это те зоны, которые появляются на том же уровне, что и зона poolCommonAreaRooms
// Эти зоны генерируются не обычным способом, а сразу полностью.
// Так сделано для того, чтобы избежать генерации зоны с 0 количеством полезных зон.
public class NewZoneRoomManager : MonoBehaviour 
{
    private (Vector2, GameObject) zoneParentCreateRoom;
    public (Vector2, GameObject) ZoneParentCreateRoom {
        get { return zoneParentCreateRoom; }
        set { zoneParentCreateRoom = value; }
    }
    [SerializeField] private bool[] isZoneRoomCreated = new bool[4];
    public bool[] IsZoneRoomCreated {
        get { return isZoneRoomCreated; }
        set { isZoneRoomCreated = value; }
    }
}