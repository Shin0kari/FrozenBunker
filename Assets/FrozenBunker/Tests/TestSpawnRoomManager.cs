using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestSpawnRoomManager
{
    private SpawnRoomManager _manager;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject();
        _manager = go.AddComponent<SpawnRoomManager>();
    }

    [Test]
    public void RotateExitsClockwise_ShouldRotateCorrectly()
    {
        bool[] exits = { true, false, true, false }; // N, E, S, W
        bool[] rotated = _manager.InvokePrivate<bool[]>("RotateExitsClockwise", exits);
        Assert.AreEqual(new[] { false, true, false, true }, rotated);
    }

    [Test]
    public void GetRotationFromSteps_ShouldReturnCorrectQuaternion()
    {
        var q = _manager.InvokePrivate<Quaternion>("GetRotationFromSteps", 2);
        Assert.AreEqual(Quaternion.Euler(0, 180f, 0), q);
    }

    [Test]
    public void GetRandomRotationAndDirection_ShouldReturnValidIndex()
    {
        var (rotation, index) = _manager.InvokePrivate<(Quaternion, int)>("GetRandomRotationAndDirection");
        Assert.IsTrue(index >= 0 && index < 4);
    }

    [Test]
    public void GetRandomRotationAndDirection_ValidList_ShouldReturnValid()
    {
        var list = new List<int> { 0, 2 };
        var (rotation, index) = _manager.InvokePrivate<(Quaternion, int)>("GetRandomRotationAndDirection", list);
        Assert.Contains(index, list);
    }

    [Test]
    public void GetStartingPosition_ShouldReturnCorrectPosition()
    {
        Vector3 pos = _manager.InvokePrivate<Vector3>("GetStartingPosition", 2);
        Assert.AreEqual(new Vector3(0, 50, 0), pos); // _startPosition.y = 25
    }

    [Test]
    public void CheckRequirement_ShouldWorkAsExpected()
    {
        Assert.IsTrue(_manager.InvokePrivate<bool>("CheckRequirement", 1, true));
        Assert.IsFalse(_manager.InvokePrivate<bool>("CheckRequirement", 1, false));
        Assert.IsTrue(_manager.InvokePrivate<bool>("CheckRequirement", 0, true));
        Assert.IsFalse(_manager.InvokePrivate<bool>("CheckRequirement", -1, true));
    }

    [Test]
    public void GetRotatedExit_ShouldReturnCorrectValue()
    {
        bool[] exits = { true, false, false, false }; // N only
        bool result = _manager.InvokePrivate<bool>("GetRotatedExit", exits, 1, 1);
        Assert.IsTrue(result); // direction = 1 (East), rotation = 1 -> maps to North
    }

    [Test]
    public void GetOppositeDirection_ShouldReturnCorrect()
    {
        Assert.AreEqual(Direction.East, _manager.InvokePrivate<Direction>("GetOppositeDirection", new Vector2(0, -1)));
        Assert.AreEqual(Direction.North, _manager.InvokePrivate<Direction>("GetOppositeDirection", new Vector2(-1, 0)));
    }

    [Test]
    public void GetAdjacentPosition_ShouldOffsetCorrectly()
    {
        var result = _manager.InvokePrivate<Vector3>("GetAdjacentPosition", Vector3.zero, Direction.North);
        Assert.AreEqual(Vector3.right * 25f, result);
    }

    [Test]
    public void CalculateMinNumRequiredExits_ShouldReturnCorrect()
    {
        var field = typeof(SpawnRoomManager).GetField("CountZoneRoomsExits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(_manager, 3);

        Assert.AreEqual(1, _manager.InvokePrivate<int>("CalculateMinNumRequiredExits", 0));
        Assert.AreEqual(1, _manager.InvokePrivate<int>("CalculateMinNumRequiredExits", -1)); // DeadEnd
    }

    [Test]
    public void CalculateAdjacentPosition_ShouldReturnCorrect()
    {
        var parent = new Vector3(1, 1, 1);
        var result = _manager.InvokePrivate<Vector3>("CalculateAdjacentPosition", parent, Direction.South);
        Assert.AreEqual(new Vector3(0, 25f, 1) * 25f, result);
    }

    [Test]
    public void GetDirectionOffset_ShouldReturnExpected()
    {
        Assert.AreEqual(Vector3.right, _manager.InvokePrivate<Vector3>("GetDirectionOffset", Direction.North));
        Assert.AreEqual(Vector3.up, _manager.InvokePrivate<Vector3>("GetDirectionOffset", Direction.Up));
    }
}

public static class PrivateMethodInvoker
{
    public static T InvokePrivate<T>(this object instance, string methodName, params object[] args)
    {
        var type = instance.GetType();
        var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)method.Invoke(instance, args);
    }
}

public class MockRoomsPool : RoomsPool
{
    protected override void Awake() {
        // Переопределяем, чтобы не вызывался оригинальный Awake
    }

    public override List<GameObject> GetPoolRooms(int zoneIndex) {
        return new List<GameObject>(); // Пустой список, чтобы не падало
    }
}