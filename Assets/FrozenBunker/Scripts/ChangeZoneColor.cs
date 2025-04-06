using UnityEngine;

public class ChangeZoneColor : MonoBehaviour
{
    public Material newZoneMaterial;
    public void ChangeColor() {
        gameObject.GetComponent<Renderer>().material = newZoneMaterial;
    }
}
