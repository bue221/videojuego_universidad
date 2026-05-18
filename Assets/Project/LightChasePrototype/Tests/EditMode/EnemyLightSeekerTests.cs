using System.Reflection;
using LightChasePrototype;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public class EnemyLightSeekerTests
{
    private const string TempControllerFolder = "Assets/Project/LightChasePrototype/Tests/Temp";

    private GameObject _enemyRoot;

    [SetUp]
    public void SetUp()
    {
        EnsureTempFolderExists();
        _enemyRoot = new GameObject("EnemyRoot");
        _enemyRoot.AddComponent<NavMeshAgent>();
        _enemyRoot.AddComponent<AudioSource>();
        _enemyRoot.AddComponent<Animator>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_enemyRoot != null)
        {
            Object.DestroyImmediate(_enemyRoot);
        }

        if (AssetDatabase.IsValidFolder(TempControllerFolder))
        {
            AssetDatabase.DeleteAsset(TempControllerFolder);
        }
    }

    [Test]
    public void Awake_PicksChildAnimatorWhenItHasBetterRuntimeCandidate()
    {
        var rootAnimator = _enemyRoot.GetComponent<Animator>();
        rootAnimator.runtimeAnimatorController = CreateController("EnemyRootController", addSpeedParameter: true);

        var visual = new GameObject("EnemyVisual");
        visual.transform.SetParent(_enemyRoot.transform, false);
        var childAnimator = visual.AddComponent<Animator>();
        childAnimator.runtimeAnimatorController = CreateController("EnemyChildController", addSpeedParameter: true);

        var enemy = _enemyRoot.AddComponent<EnemyLightSeeker>();
        InvokePrivate(enemy, "Awake");

        var selectedAnimator = (Animator)GetPrivateField(enemy, "_animator");
        Assert.That(selectedAnimator, Is.EqualTo(childAnimator));
    }

    [Test]
    public void UpdateAnimator_UsesAnimatorSpeedFallbackWhenSpeedParameterIsMissing()
    {
        var visual = new GameObject("EnemyVisual");
        visual.transform.SetParent(_enemyRoot.transform, false);
        var childAnimator = visual.AddComponent<Animator>();
        childAnimator.runtimeAnimatorController = CreateController("EnemyNoSpeedController", addSpeedParameter: false);

        var enemy = _enemyRoot.AddComponent<EnemyLightSeeker>();
        InvokePrivate(enemy, "Awake");

        childAnimator.speed = 1f;

        InvokePrivate(enemy, "UpdateAnimator", 0.6f, true);

        Assert.That(childAnimator.speed, Is.EqualTo(0.6f).Within(0.001f));
    }

    [Test]
    public void UpdateAnimator_ClampsAnimatorSpeedToMinimumWhenIdle()
    {
        var visual = new GameObject("EnemyVisual");
        visual.transform.SetParent(_enemyRoot.transform, false);
        var childAnimator = visual.AddComponent<Animator>();
        childAnimator.runtimeAnimatorController = CreateController("EnemyMinSpeedController", addSpeedParameter: false);

        var enemy = _enemyRoot.AddComponent<EnemyLightSeeker>();
        InvokePrivate(enemy, "Awake");

        InvokePrivate(enemy, "UpdateAnimator", 0f, false);

        Assert.That(childAnimator.speed, Is.EqualTo(0.1f).Within(0.001f));
    }

    [Test]
    public void UpdateAnimator_SetsSpeedParameterWhenAvailable()
    {
        var visual = new GameObject("EnemyVisual");
        visual.transform.SetParent(_enemyRoot.transform, false);
        var childAnimator = visual.AddComponent<Animator>();
        childAnimator.runtimeAnimatorController = CreateController("EnemyWithSpeed", addSpeedParameter: true);

        var enemy = _enemyRoot.AddComponent<EnemyLightSeeker>();
        InvokePrivate(enemy, "Awake");

        InvokePrivate(enemy, "UpdateAnimator", 1.35f, true);

        Assert.That(childAnimator.GetFloat("Speed"), Is.EqualTo(1.35f).Within(0.001f));
        Assert.That(childAnimator.speed, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void UpdateAnimator_KeepsAnimatorEnabledAcrossAllStates()
    {
        var visual = new GameObject("EnemyVisual");
        visual.transform.SetParent(_enemyRoot.transform, false);
        var childAnimator = visual.AddComponent<Animator>();
        childAnimator.runtimeAnimatorController = CreateController("EnemyAlwaysOnController", addSpeedParameter: true);

        var enemy = _enemyRoot.AddComponent<EnemyLightSeeker>();
        InvokePrivate(enemy, "Awake");

        // El enemigo debe verse vivo en idle, alerta y chase. El gating del Animator
        // se elimino porque dejaba al enemigo congelado en pose y rompia la lectura
        // de la fantasia de persecucion.
        InvokePrivate(enemy, "UpdateAnimator", 0.45f, false);
        Assert.That(childAnimator.enabled, Is.True, "Animator must stay enabled in idle state");

        InvokePrivate(enemy, "UpdateAnimator", 1f, false);
        Assert.That(childAnimator.enabled, Is.True, "Animator must stay enabled while alerted");

        InvokePrivate(enemy, "UpdateAnimator", 1.35f, true);
        Assert.That(childAnimator.enabled, Is.True, "Animator must stay enabled while chasing");

        InvokePrivate(enemy, "UpdateAnimator", 0.45f, false);
        Assert.That(childAnimator.enabled, Is.True, "Animator must stay enabled when chase ends");
    }

    [Test]
    public void UpdateAnimator_ReEnablesAnimatorIfExternallyDisabled()
    {
        var visual = new GameObject("EnemyVisual");
        visual.transform.SetParent(_enemyRoot.transform, false);
        var childAnimator = visual.AddComponent<Animator>();
        childAnimator.runtimeAnimatorController = CreateController("EnemyResilientController", addSpeedParameter: true);

        var enemy = _enemyRoot.AddComponent<EnemyLightSeeker>();
        InvokePrivate(enemy, "Awake");

        childAnimator.enabled = false;

        InvokePrivate(enemy, "UpdateAnimator", 0.45f, false);

        Assert.That(childAnimator.enabled, Is.True, "Animator must be re-enabled defensively if something turned it off");
    }

    private static RuntimeAnimatorController CreateController(string controllerName, bool addSpeedParameter)
    {
        var path = $"{TempControllerFolder}/{controllerName}.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        if (addSpeedParameter)
        {
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        }

        return controller;
    }

    private static object GetPrivateField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field.GetValue(instance);
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(instance, args);
    }

    private static void EnsureTempFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Project/LightChasePrototype/Tests"))
        {
            AssetDatabase.CreateFolder("Assets/Project/LightChasePrototype", "Tests");
        }

        if (!AssetDatabase.IsValidFolder(TempControllerFolder))
        {
            AssetDatabase.CreateFolder("Assets/Project/LightChasePrototype/Tests", "Temp");
        }
    }
}
