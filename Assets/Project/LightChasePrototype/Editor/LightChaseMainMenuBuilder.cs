using System.IO;
using LightChasePrototype;
using LightChasePrototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightChaseMainMenuBuilder
{
    private const string MainMenuScenePath = "Assets/Project/LightChasePrototype/Scenes/MainMenu.unity";
    private static readonly string GameplaySceneName = LightChaseLevelCatalog.DefaultSceneName;

    [MenuItem("Tools/Prototype/Build Main Menu")]
    public static void BuildMainMenu()
    {
        EnsureSceneFolderExists();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureCamera();
        ConfigureMenu();

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        EnsureSceneIncludedInBuildSettings(MainMenuScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"Main menu listo en {MainMenuScenePath}");
    }

    private static void EnsureSceneFolderExists()
    {
        var folderPath = Path.GetDirectoryName(MainMenuScenePath);
        if (!string.IsNullOrEmpty(folderPath) && !AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Project/LightChasePrototype", "Scenes");
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

    private static void EnsureSceneIncludedInBuildSettings(string scenePath)
    {
        var existingScenes = EditorBuildSettings.scenes;
        foreach (var existingScene in existingScenes)
        {
            if (existingScene.path == scenePath)
            {
                return;
            }
        }

        var updatedScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
        for (var i = 0; i < existingScenes.Length; i++)
        {
            updatedScenes[i] = existingScenes[i];
        }

        updatedScenes[^1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updatedScenes;
    }
}
