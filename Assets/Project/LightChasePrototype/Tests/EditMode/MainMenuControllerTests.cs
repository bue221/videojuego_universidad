using LightChasePrototype;
using LightChasePrototype.UI;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuControllerTests
{
    private GameObject _root;
    private GameObject _instructionsPanel;
    private GameObject _menuCanvas;
    private GameObject _mainActionsPanel;
    private MainMenuController _controller;

    [SetUp]
    public void SetUp()
    {
        LightChaseLevelCatalog.SelectLevel(LightChaseLevelCatalog.PrototypeLevelId);
        _root = new GameObject("MainMenuRoot");
        _menuCanvas = new GameObject("MenuCanvas");
        _mainActionsPanel = new GameObject("ButtonStack");
        _mainActionsPanel.transform.SetParent(_root.transform);
        _instructionsPanel = new GameObject("InstructionsPanel");
        _instructionsPanel.transform.SetParent(_root.transform);
        var avatarPanel = new GameObject("AvatarSelectionPanel");
        avatarPanel.transform.SetParent(_root.transform);
        avatarPanel.SetActive(false);
        var avatarDescription = new GameObject("AvatarDescription", typeof(Text));
        avatarDescription.transform.SetParent(avatarPanel.transform);
        var avatarPreview = new GameObject("AvatarPreview", typeof(Image));
        avatarPreview.transform.SetParent(avatarPanel.transform);

        _controller = _root.AddComponent<MainMenuController>();
        _controller.Configure(_instructionsPanel, "LightChasePrototype");
        var controllerType = _controller.GetType();
        controllerType.GetField("menuCanvasGroup", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, _menuCanvas.AddComponent<CanvasGroup>());
        controllerType.GetField("mainActionsPanel", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, _mainActionsPanel);
        controllerType.GetField("avatarSelectionPanel", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, avatarPanel);
        controllerType.GetField("avatarDescriptionText", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, avatarDescription.GetComponent<Text>());
        controllerType.GetField("avatarPreviewImage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, avatarPreview.GetComponent<Image>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        if (_menuCanvas != null)
        {
            Object.DestroyImmediate(_menuCanvas);
        }

        if (_mainActionsPanel != null)
        {
            Object.DestroyImmediate(_mainActionsPanel);
        }

        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        DestroyNamedObject("GameSessionManager");
        DestroyNamedObject("GlobalUIRoot");
        Time.timeScale = 1f;
    }

    private static void DestroyNamedObject(string objectName)
    {
        var target = GameObject.Find(objectName);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void Configure_HidesInstructionsByDefault()
    {
        Assert.That(_controller.InstructionsVisible, Is.False);
        Assert.That(_instructionsPanel.activeSelf, Is.False);
        Assert.That(_controller.GameplaySceneName, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void ShowInstructions_MakesPanelVisible()
    {
        _controller.ShowInstructions();

        Assert.That(_controller.InstructionsVisible, Is.True);
    }

    [Test]
    public void HideInstructions_HidesPanelAfterShowingIt()
    {
        _controller.ShowInstructions();

        _controller.HideInstructions();

        Assert.That(_controller.InstructionsVisible, Is.False);
    }

    [Test]
    public void PlayGame_UsesConfiguredSceneLoader()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "Boot");

        _controller.PlayGame();
        _controller.PlayGame();

        Assert.That(loadedScene, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void PlayGame_LoadsSelectedNatureLevelWhenChosen()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "Boot");
        _controller.SelectLevel(LightChaseLevelCatalog.NatureLevelId);

        _controller.PlayGame();
        _controller.PlayGame();

        Assert.That(loadedScene, Is.EqualTo("LightChasePrototype_Level02"));
    }

    [Test]
    public void SelectLevel_UpdatesSelectedSceneName()
    {
        _controller.SelectLevel(LightChaseLevelCatalog.NatureLevelId);

        Assert.That(_controller.SelectedLevelSceneName, Is.EqualTo("LightChasePrototype_Level02"));
    }

    [Test]
    public void SelectLevel_UpdatesSelectedSceneNameForWaterRoute()
    {
        _controller.SelectLevel(LightChaseLevelCatalog.WaterLevelId);

        Assert.That(_controller.SelectedLevelSceneName, Is.EqualTo("LightChasePrototype_Level03"));
    }

    [Test]
    public void ShowMenu_PreservesExplicitSelectedLevel()
    {
        _controller.ConfigureActionsForTests(null, null, () => "LightChasePrototype");
        _controller.SelectLevel(LightChaseLevelCatalog.NatureLevelId);

        _controller.ShowMenu();

        Assert.That(_controller.SelectedLevelSceneName, Is.EqualTo("LightChasePrototype_Level02"));
    }

    [Test]
    public void PlayGame_FirstShowsAvatarSelectionInsteadOfLoadingScene()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "Boot");

        _controller.PlayGame();

        Assert.That(_controller.AvatarSelectionVisible, Is.True);
        Assert.That(_mainActionsPanel.activeSelf, Is.False);
        Assert.That(loadedScene, Is.Null);
    }

    [Test]
    public void ShowAvatarSelection_GeneratesAvatarPreviewImage()
    {
        _controller.ShowAvatarSelection();

        var previewImage = GameObject.Find("AvatarPreview").GetComponent<Image>();

        Assert.That(previewImage.sprite, Is.Not.Null);
    }

    [Test]
    public void SelectAvatar_UpdatesPersistentSelection()
    {
        _controller.SelectAvatar(PlayerAvatarSelection.CapsuleAvatarId);

        Assert.That(_controller.SelectedAvatarId, Is.EqualTo(PlayerAvatarSelection.CapsuleAvatarId));
        Assert.That(PlayerAvatarSelection.SelectedAvatarId, Is.EqualTo(PlayerAvatarSelection.CapsuleAvatarId));
    }

    [Test]
    public void PlayGame_InGameplayScene_HidesMenuWithoutLoadingScene()
    {
        string loadedScene = null;
        _controller.ShowMenu();
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "LightChasePrototype");

        _controller.PlayGame();

        Assert.That(loadedScene, Is.Null);
        Assert.That(_controller.MenuVisible, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void ShowInstructions_HidesAvatarSelectionPanel()
    {
        _controller.ShowAvatarSelection();

        _controller.ShowInstructions();

        Assert.That(_controller.AvatarSelectionVisible, Is.False);
        Assert.That(_controller.InstructionsVisible, Is.True);
        Assert.That(_mainActionsPanel.activeSelf, Is.False);
    }

    [Test]
    public void QuitGame_UsesConfiguredQuitAction()
    {
        var quitCalled = false;
        _controller.ConfigureActionsForTests(null, () => quitCalled = true);

        _controller.QuitGame();

        Assert.That(quitCalled, Is.True);
    }

    [Test]
    public void EnsureMenuExists_CreatesVisibleOverlayAndPausesGame()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();

        Assert.That(createdMenu, Is.Not.Null);
        Assert.That(createdMenu.MenuVisible, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));
    }

    [Test]
    public void EnsureMenuExists_BuildsInstructionsThatExplainControlsAndEnemy()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();
        createdMenu.ShowInstructions();

        var instructionsBody = GameObject.Find("InstructionsBody").GetComponent<Text>();

        Assert.That(instructionsBody.text, Does.Contain("OBJETIVO"));
        Assert.That(instructionsBody.text, Does.Contain("WASD"));
        Assert.That(instructionsBody.text, Does.Contain("Shift izquierdo"));
        Assert.That(instructionsBody.text, Does.Contain("ENEMIGO"));
    }

    [Test]
    public void EnsureMenuExists_ShowsInstitutionalHeader()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        MainMenuController.EnsureMenuExists();

        var universityTitle = GameObject.Find("UniversityTitle").GetComponent<Text>();
        var courseTitle = GameObject.Find("CourseTitle").GetComponent<Text>();

        Assert.That(universityTitle.text, Is.EqualTo("UNIVERSIDAD CENTRAL"));
        Assert.That(courseTitle.text, Is.EqualTo("MODELADO 3D Y VIDEOJUEGOS 2026"));
    }

    [Test]
    public void EnsureMenuExists_ShowsOnlyPlayAndExitInInitialMainActions()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();

        var buttonStack = GameObject.Find("ButtonStack").transform;
        Assert.That(buttonStack.childCount, Is.EqualTo(2));
        Assert.That(buttonStack.GetChild(0).Find("Label").GetComponent<Text>().text, Is.EqualTo("JUGAR"));
        Assert.That(buttonStack.GetChild(1).Find("Label").GetComponent<Text>().text, Is.EqualTo("SALIR"));
        Assert.That(createdMenu.transform.Find("InstructionsPanel").gameObject.activeSelf, Is.False);
        Assert.That(createdMenu.transform.Find("AvatarSelectionPanel").gameObject.activeSelf, Is.False);
    }
}
