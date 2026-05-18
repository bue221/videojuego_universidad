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
    private GameObject _levelSelectionPanel;
    private MainMenuController _controller;

    [SetUp]
    public void SetUp()
    {
        LightChaseLevelCatalog.SelectLevel(LightChaseLevelCatalog.PrototypeLevelId);
        GlobalUiController.ResetGameplayProgress();
        _root = new GameObject("MainMenuRoot");
        _menuCanvas = new GameObject("MenuCanvas");
        _mainActionsPanel = new GameObject("ButtonStack");
        _mainActionsPanel.transform.SetParent(_root.transform);
        _instructionsPanel = new GameObject("InstructionsPanel");
        _instructionsPanel.transform.SetParent(_root.transform);
        _levelSelectionPanel = new GameObject("LevelSelectionPanel");
        _levelSelectionPanel.transform.SetParent(_root.transform);
        _levelSelectionPanel.SetActive(false);
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
        controllerType.GetField("levelSelectionPanel", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_controller, _levelSelectionPanel);
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

        if (_levelSelectionPanel != null)
        {
            Object.DestroyImmediate(_levelSelectionPanel);
        }

        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        DestroyNamedObject("GameSessionManager");
        DestroyNamedObject("GlobalUIRoot");
        GlobalUiController.ResetGameplayProgress();
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
    public void PlayGame_AlwaysStartsOnLevel1FromMenu()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "Boot");
        _controller.SelectLevel(LightChaseLevelCatalog.NatureLevelId);

        _controller.PlayGame();
        _controller.PlayGame();

        Assert.That(loadedScene, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void PlayGame_FirstShowsAvatarSelectionInsteadOfLoadingScene()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "Boot");

        _controller.PlayGame();

        Assert.That(_controller.LevelSelectionVisible, Is.False);
        Assert.That(_controller.AvatarSelectionVisible, Is.True);
        Assert.That(_mainActionsPanel.activeSelf, Is.False);
        Assert.That(loadedScene, Is.Null);
    }

    [Test]
    public void PlayGame_SecondCallLoadsScene()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "Boot");

        _controller.PlayGame();
        _controller.PlayGame();

        Assert.That(_controller.LevelSelectionVisible, Is.False);
        Assert.That(loadedScene, Is.EqualTo("LightChasePrototype"));
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
    public void PlayGame_AfterVictoryReturnToMenu_ShowsAvatarSelectionFirst()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "LightChasePrototype_Level04");

        _controller.ShowVictoryOverlay("GANASTE", "Completaste todos los niveles.");
        _controller.ShowMenu();
        _controller.PlayGame();

        Assert.That(loadedScene, Is.Null, "Un clic en JUGAR no debe cargar la escena directamente; primero debe mostrar la selección de avatar.");
        Assert.That(_controller.AvatarSelectionVisible, Is.True, "El panel de selección de avatar debe quedar visible tras un solo clic en JUGAR.");
        Assert.That(_controller.VictoryVisible, Is.False);
        Assert.That(_mainActionsPanel.activeSelf, Is.False);
    }

    [Test]
    public void PlayGame_InGameplayScene_ResumesAfterFullSelectionFlow()
    {
        string loadedScene = null;
        _controller.ShowMenu();
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "LightChasePrototype");

        _controller.PlayGame();
        _controller.PlayGame();

        Assert.That(loadedScene, Is.Null);
        Assert.That(_controller.MenuVisible, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void PlayGame_InNonDefaultGameplayScene_LoadsLevel1()
    {
        string loadedScene = null;
        _controller.ConfigureActionsForTests(sceneName => loadedScene = sceneName, null, () => "LightChasePrototype_Level02");
        _controller.SelectLevel(LightChaseLevelCatalog.WaterLevelId);

        _controller.PlayGame();
        _controller.PlayGame();

        Assert.That(loadedScene, Is.EqualTo("LightChasePrototype"));
    }

    [Test]
    public void ShowInstructions_HidesAvatarSelectionPanel()
    {
        _controller.ShowAvatarSelection();

        _controller.ShowInstructions();

        Assert.That(_controller.AvatarSelectionVisible, Is.False);
        Assert.That(_controller.LevelSelectionVisible, Is.False);
        Assert.That(_controller.InstructionsVisible, Is.True);
        Assert.That(_mainActionsPanel.activeSelf, Is.False);
    }

    [Test]
    public void ShowInstructions_HidesLevelSelectionPanel()
    {
        _controller.ShowLevelSelection();

        _controller.ShowInstructions();

        Assert.That(_controller.LevelSelectionVisible, Is.False);
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
    public void EnsureMenuExists_ShowsGameTitleAndSubtitle()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        MainMenuController.EnsureMenuExists();

        var gameTitle = GameObject.Find("GameTitle").GetComponent<Text>();
        var subtitle = GameObject.Find("Subtitle").GetComponent<Text>();

        Assert.That(gameTitle.text, Is.EqualTo("CORRE CORRE QUE TE ATRAPO"));
        Assert.That(subtitle.text, Is.EqualTo("Modelado 3D y videojuegos"));
    }

    [Test]
    public void EnsureMenuExists_ShowVictoryOverlay_DisplaysVictoryPanelWithGameWonMessage()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();

        createdMenu.ShowVictoryOverlay("GANASTE EL JUEGO", "Completaste todos los niveles.");

        var victoryTitle = GameObject.Find("VictoryTitle").GetComponent<Text>();
        var victoryMessage = GameObject.Find("VictoryMessage").GetComponent<Text>();

        Assert.That(createdMenu.VictoryVisible, Is.True);
        Assert.That(createdMenu.DefeatVisible, Is.False);
        Assert.That(victoryTitle.text, Is.EqualTo("GANASTE EL JUEGO"));
        Assert.That(victoryMessage.text, Is.EqualTo("Completaste todos los niveles."));
    }

    [Test]
    public void EnsureMenuExists_ShowsCreditsFooterWithAuthors()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        MainMenuController.EnsureMenuExists();

        var creditsFooter = GameObject.Find("CreditsFooter")?.GetComponent<Text>();

        Assert.That(creditsFooter, Is.Not.Null, "El menu principal debe incluir un footer de creditos.");
        Assert.That(creditsFooter.text, Is.EqualTo("Hecho por Andres Plaza y Nicolas Fonseca"));
    }

    [Test]
    public void StartGameFromAvatarSelection_MarksGameplayInProgress()
    {
        GlobalUiController.ResetGameplayProgress();
        _controller.ConfigureActionsForTests(sceneName => { }, null, () => "Boot");

        _controller.PlayGame();
        _controller.PlayGame();

        Assert.That(GlobalUiController.GameplayInProgress, Is.True,
            "Presionar Comenzar tras elegir avatar debe marcar la corrida como iniciada para que el menu no reaparezca al cargar el nivel.");
    }

    [Test]
    public void HideInstructions_RestoresDecorativeHeaderAndFooter()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();

        createdMenu.ShowInstructions();
        createdMenu.HideInstructions();

        Assert.That(GameObject.Find("GameTitle").activeInHierarchy, Is.True,
            "El titulo del juego debe volver a verse al cerrar el modal de instrucciones.");
        Assert.That(GameObject.Find("Subtitle").activeInHierarchy, Is.True);
        Assert.That(GameObject.Find("InstitutionBanner").activeInHierarchy, Is.True);
        Assert.That(GameObject.Find("CreditsFooter").activeInHierarchy, Is.True,
            "El footer de creditos debe volver a verse al cerrar el modal de instrucciones.");
        Assert.That(GameObject.Find("Glow").activeInHierarchy, Is.True);
    }

    [Test]
    public void EnsureMenuExists_ShowsPlayInstructionsAndExitInInitialMainActions()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_menuCanvas);
        _root = null;
        _menuCanvas = null;

        var createdMenu = MainMenuController.EnsureMenuExists();

        var buttonStack = GameObject.Find("ButtonStack").transform;
        Assert.That(buttonStack.childCount, Is.EqualTo(3));
        Assert.That(buttonStack.GetChild(0).Find("Label").GetComponent<Text>().text, Is.EqualTo("JUGAR"));
        Assert.That(buttonStack.GetChild(1).Find("Label").GetComponent<Text>().text, Is.EqualTo("INSTRUCCIONES"));
        Assert.That(buttonStack.GetChild(2).Find("Label").GetComponent<Text>().text, Is.EqualTo("SALIR"));
        Assert.That(createdMenu.transform.Find("InstructionsPanel").gameObject.activeSelf, Is.False);
        Assert.That(createdMenu.transform.Find("AvatarSelectionPanel").gameObject.activeSelf, Is.False);
        Assert.That(createdMenu.transform.Find("LevelSelectionPanel").gameObject.activeSelf, Is.False);
    }
}
