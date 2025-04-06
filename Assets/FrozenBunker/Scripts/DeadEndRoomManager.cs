using UnityEngine;

public class DeadEndRoomManager : MonoBehaviour 
{
    // POLYMORPHISM
    // protected override void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player")) {
    //         Debug.Log("TriggerEnter! In DeadEndRoomManager");
    //         PlayerController playerController = other.GetComponent<PlayerController>();
    //         spawnRoomManager.DespawnOldAdjacentRooms(playerController.PlayerPos, _2DWorldPos);
    //         playerController.PlayerPos = _2DWorldPos;
    //     }
    // }
}