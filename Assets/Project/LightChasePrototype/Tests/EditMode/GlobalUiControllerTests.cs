using LightChasePrototype;
using LightChasePrototype.UI;
using NUnit.Framework;
using UnityEngine;

public class GlobalUiControllerTests
{
    [TearDown]
    public void TearDown()
    {
        DestroyNamedObject("GlobalUIRoot");
        DestroyNamedObject("GameSessionManager");
        DestroyNamedObject("EventSystem");
        Time.timeScale = 1f;
    }

    [Test]
    public void EnsureExists_CreatesSharedRootForMenuAndHud()
    {
        var controller = GlobalUiController.EnsureExists();

        var root = GameObject.Find("GlobalUIRoot");
        var menu = root.transform.Find("MainMenuOverlay")?.gameObject;
        var hud = root.transform.Find("GameplayHUD")?.gameObject;

        Assert.That(controller, Is.Not.Null);
        Assert.That(root, Is.Not.Null);
        Assert.That(root.GetComponent<Canvas>(), Is.Not.Null);
        Assert.That(menu.transform.parent, Is.EqualTo(root.transform));
        Assert.That(hud.transform.parent, Is.EqualTo(root.transform));
    }

    [Test]
    public void EnsureExists_WithoutLevelManager_HidesHudUntilGameplayStarts()
    {
        GlobalUiController.EnsureExists();

        var root = GameObject.Find("GlobalUIRoot");
        var hud = root.transform.Find("GameplayHUD")?.gameObject;
        var menuController = Object.FindAnyObjectByType<MainMenuController>();

        Assert.That(hud.activeSelf, Is.False);
        Assert.That(menuController, Is.Not.Null);
        Assert.That(menuController.MenuVisible, Is.True);
    }

    [Test]
    public void MenuVisible_HidesHudWhileOverlayIsOpen()
    {
        var levelRoot = new GameObject("LevelManager");
        var levelManager = levelRoot.AddComponent<PrototypeLevelManager>();
        var controller = GlobalUiController.EnsureExists(levelManager);
        var root = controller.transform;
        var hud = root.Find("GameplayHUD")?.gameObject;
        var menuController = Object.FindAnyObjectByType<MainMenuController>();

        menuController.HideMenu();
        Assert.That(hud.activeSelf, Is.True);

        menuController.ShowMenu();
        Assert.That(hud.activeSelf, Is.False);

        Object.DestroyImmediate(levelRoot);
    }

    [Test]
    public void EnsureExists_RemovesLegacySceneHudAndMenuObjects()
    {
        var legacyHud = new GameObject("GameplayHUD");
        var legacyMenu = new GameObject("MainMenuOverlay");

        var controller = GlobalUiController.EnsureExists();
        var root = controller.transform;

        Assert.That(legacyHud == null, Is.True);
        Assert.That(legacyMenu == null, Is.True);
        Assert.That(root.Find("GameplayHUD"), Is.Not.Null);
        Assert.That(root.Find("MainMenuOverlay"), Is.Not.Null);
    }

    [Test]
    public void GameOver_ShowsDefeatOverlayAndHidesHud()
    {
        var player = new GameObject("Player");
        player.AddComponent<PlayerLightState>();
        player.AddComponent<CharacterController>();

        var levelRoot = new GameObject("LevelManager");
        var levelManager = levelRoot.AddComponent<PrototypeLevelManager>();
        var controller = GlobalUiController.EnsureExists(levelManager);
        var menuController = Object.FindAnyObjectByType<MainMenuController>();
        var hud = controller.transform.Find("GameplayHUD")?.gameObject;

        menuController.HideMenu();
        levelManager.ApplyPlayerHit();
        levelManager.ApplyPlayerHit();
        levelManager.ApplyPlayerHit();

        Assert.That(menuController.DefeatVisible, Is.True);
        Assert.That(hud.activeSelf, Is.False);
        Assert.That(GameObject.Find("DefeatTitle").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("TE ATRAPARON"));

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(levelRoot);
    }

    private static void DestroyNamedObject(string objectName)
    {
        var target = GameObject.Find(objectName);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }
}
