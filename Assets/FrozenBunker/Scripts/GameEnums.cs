using UnityEngine;

public class GameEnums
{
    private static Vector2Int[] zoneFloorData = new Vector2Int[]
    {
        new(1, 2),
        new(1, 4),

        new(0, 0),
        new(0, 6),
        new(0, 7),

        new(-1, 0),
        new(-1, 1),
        new(-1, 3),
        new(-1, 8),

        new(-2, 5),

        new(-3, 9)
    };

    public static Vector2Int[] GetZoneFloorData { get { return zoneFloorData; } }

    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
        Up = 4,
        Down = 5
    }

    public const int DeadEndZoneType = -1;

    public const int XOZDirectionsCount = 4;
    public const int OYDirectionsCount = 2;
    public const int _startFloor = -1;

    public const float RotationStep = 90f;
    public const float _roomSize = 25f;

}