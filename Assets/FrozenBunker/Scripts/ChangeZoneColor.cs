using UnityEngine;

public class ChangeZoneColor : MonoBehaviour
{
    public Material newZoneMaterial;

    public void ChangeColor()
    {
        GetComponent<Renderer>().material = newZoneMaterial;
    }
}
