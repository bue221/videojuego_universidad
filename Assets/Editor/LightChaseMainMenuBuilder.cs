using System.IO;
using LightChasePrototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightChaseMainMenuBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplaySceneName = "LightChasePrototype";

    [MenuItem("Tools/Prototype/Build Main Menu")]
    public static void BuildMainMenu()
    {
        EnsureSceneFolderExists();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureCamera();
        ConfigureMenu();

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"Main menu listo en {MainMenuScenePath}");
    }

    private static void EnsureSceneFolderExists()
    {
        var folderPath = Path.GetDirectoryName(MainMenuScenePath);
        if (!string.IsNullOrEmpty(folderPath) && !AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    private static void ConfigureCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.04f, 0.1f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void ConfigureMenu()
    {
        MainMenuController.EnsureMenuExists(GameplaySceneName);
    }
}
