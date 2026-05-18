using LightChasePrototype;
using NUnit.Framework;
using UnityEngine;

public class PlayerWaterTraversalTests
{
    private GameObject _player;
    private PlayerWaterTraversal _waterTraversal;
    private WaterVolume _waterVolume;
    private Transform _visualRoot;
    private MonoBehaviour _controller;
    private System.Type _controllerType;

    [SetUp]
    public void SetUp()
    {
        _player = new GameObject("Player");
        _player.AddComponent<CharacterController>();
        _controllerType = System.Type.GetType("StarterAssets.ThirdPersonController, Assembly-CSharp");
        Assert.That(_controllerType, Is.Not.Null);

        _controller = _player.AddComponent(_controllerType) as MonoBehaviour;
        SetControllerFloat("MoveSpeed", 2f);
        SetControllerFloat("SprintSpeed", 5.335f);
        SetControllerFloat("JumpHeight", 1.2f);

        _visualRoot = new GameObject("VisualRoot").transform;
        _visualRoot.SetParent(_player.transform, false);
        _visualRoot.localPosition = new Vector3(0f, 1.1f, 0f);
        _visualRoot.gameObject.AddComponent<MeshRenderer>();

        _waterTraversal = _player.AddComponent<PlayerWaterTraversal>();

        var waterObject = new GameObject("WaterVolume");
        waterObject.AddComponent<BoxCollider>().isTrigger = true;
        _waterVolume = waterObject.AddComponent<WaterVolume>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_waterVolume.gameObject);
        Object.DestroyImmediate(_player);
    }

    [Test]
    public void EnterWater_ReducesMovementAndSinksVisualRoot()
    {
        _waterTraversal.EnterWater(_waterVolume);

        Assert.That(_waterTraversal.IsInWater, Is.True);
        Assert.That(GetControllerFloat("MoveSpeed"), Is.LessThan(2f));
        Assert.That(GetControllerFloat("SprintSpeed"), Is.LessThan(5.335f));
        Assert.That(GetControllerFloat("JumpHeight"), Is.LessThan(1.2f));
        Assert.That(_visualRoot.localPosition.y, Is.LessThan(1.1f));
    }

    [Test]
    public void ExitWater_RestoresDefaultMovementAndVisualPosition()
    {
        _waterTraversal.EnterWater(_waterVolume);

        _waterTraversal.ExitWater(_waterVolume);

        Assert.That(_waterTraversal.IsInWater, Is.False);
        Assert.That(GetControllerFloat("MoveSpeed"), Is.EqualTo(2f).Within(0.001f));
        Assert.That(GetControllerFloat("SprintSpeed"), Is.EqualTo(5.335f).Within(0.001f));
        Assert.That(GetControllerFloat("JumpHeight"), Is.EqualTo(1.2f).Within(0.001f));
        Assert.That(_visualRoot.localPosition.y, Is.EqualTo(1.1f).Within(0.001f));
    }

    private float GetControllerFloat(string memberName)
    {
        var field = _controllerType.GetField(memberName);
        Assert.That(field, Is.Not.Null);
        return (float)field.GetValue(_controller);
    }

    private void SetControllerFloat(string memberName, float value)
    {
        var field = _controllerType.GetField(memberName);
        Assert.That(field, Is.Not.Null);
        field.SetValue(_controller, value);
    }
}
