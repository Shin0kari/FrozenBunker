using UnityEngine;
using System.Collections.Generic;

public class SpawnRoomManager : MonoBehaviour
{
    [SerializeField] private GameObject[] rooms;
    [SerializeField] private Vector3 startPosition = new Vector3(0, 0, -12);
    private bool[] existFromRoom = new bool[4];
    private HashSet<Vector2> occupiedPositions = new HashSet<Vector2>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int randomIndex = Random.Range(0, rooms.Length);
        Vector2 startWorldPos = new Vector2(0, 0);
        GameObject room = SpawnRoom(randomIndex, startPosition, Quaternion.identity, startWorldPos);
        CheckNumAvailableExits(room);
        SpawnAdjacentRooms(room);
    }

    // // Update is called once per frame
    // void Update()
    // {

    // }

    private GameObject SpawnRoom(int randomIndex, Vector3 pos, Quaternion rotation, Vector2 newPos) {
        if (occupiedPositions.Contains(newPos)) {
            return null; // Если позиция уже занята, не создаем новую комнату
        }

        GameObject room = Instantiate(rooms[randomIndex], pos, rotation);
        room.GetComponent<RoomManager>().WorldPos = newPos;
        occupiedPositions.Add(newPos); // Добавляем новую позицию в HashSet
        return room;
    }

    private void CheckNumAvailableExits(GameObject room) {
        existFromRoom = room.GetComponent<RoomManager>().exitsFromRoom;
    }

    private void SpawnAdjacentRooms(GameObject room) {
        for (int i = 0; i < existFromRoom.Length; i++) {
            // i = 0 -> north, 1 -> east, 2 -> south, 3 -> west
            if (!existFromRoom[i]) {
                continue;
            }

            int randomIndex = Random.Range(0, rooms.Length);
            Quaternion newRotation;
            Vector3 newPosition;
            Vector2 newWorldPos;

            switch (i) {
                case 0: // north
                    newPosition = room.transform.position + room.transform.rotation * new Vector3(25, 0, 0);
                    newRotation = room.transform.rotation * Quaternion.Euler(0, 0, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(1, 0);
                    break;
                case 1: // east
                    newPosition = room.transform.position + room.transform.rotation * new Vector3(0, 0, -25);
                    newRotation = room.transform.rotation * Quaternion.Euler(0, 90, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(0, -1);
                    break;
                case 2: // south
                    newPosition = room.transform.position + room.transform.rotation * new Vector3(-25, 0, 0);
                    newRotation = room.transform.rotation * Quaternion.Euler(0, 180, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(-1, 0);
                    break;
                case 3: // west
                    newPosition = room.transform.position + room.transform.rotation * new Vector3(0, 0, 25);
                    newRotation = room.transform.rotation * Quaternion.Euler(0, 270, 0);
                    newWorldPos = room.GetComponent<RoomManager>().WorldPos + new Vector2(0, 1);
                    break;
                default:
                    continue;
            }

            SpawnRoom(randomIndex, newPosition, newRotation, newWorldPos);
        }
    }
}
