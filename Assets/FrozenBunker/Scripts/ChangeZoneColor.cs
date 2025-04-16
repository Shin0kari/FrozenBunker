using UnityEngine;

public class ChangeZoneColor : MonoBehaviour
{
    public Material[] newZoneMaterials;
    public void ChangeColor(int zoneType) {

        switch (zoneType) {
            case 3:
                GetComponent<Renderer>().material = newZoneMaterials[0];
                break;
            case 6:
                GetComponent<Renderer>().material = newZoneMaterials[1];
                break;
            case 8:
                GetComponent<Renderer>().material = newZoneMaterials[2];
                break;
            default:
                return;
        }
    }
}
