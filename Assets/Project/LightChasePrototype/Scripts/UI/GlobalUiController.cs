using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LightChasePrototype.UI
{
    public class GlobalUiController : MonoBehaviour
    {
        private const string RootObjectName = "GlobalUIRoot";
        private const string MainMenuSceneName = "MainMenu";

        [Header("Level Flow")]
        [SerializeField] private float advanceToNextLevelDelaySeconds = 1.0f;

        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private GameHudController gameHudController;
        [SerializeField] private string gameplaySceneName = LightChaseLevelCatalog.DefaultSceneName;

        private PrototypeLevelManager _levelManager;
        private bool _advancingToNextLevel;

        public static GlobalUiController Instance { get; private set; }

        public static GlobalUiController EnsureExists(
            PrototypeLevelManager levelManager = null,
            string assignedGameplaySceneName = null)
        {
            if (Instance != null)
            {
                Instance.Initialize(levelManager, assignedGameplaySceneName);
                return Instance;
            }

            var existing = FindAnyObjectByType<GlobalUiController>();
            if (existing != null)
            {
                existing.Initialize(levelManager, assignedGameplaySceneName);
                return existing;
            }

            var rootObject = new GameObject(RootObjectName);
            var canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = rootObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            rootObject.AddComponent<GraphicRaycaster>();

            var controller = rootObject.AddComponent<GlobalUiController>();
            controller.Initialize(levelManager, assignedGameplaySceneName);
            return controller;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (_levelManager != null)
            {
                _levelManager.StateChanged -= HandleLevelStateChanged;
            }
        }

        public void Initialize(PrototypeLevelManager levelManager, string assignedGameplaySceneName = null)
        {
            var resolvedSceneName = ResolveGameplaySceneName(assignedGameplaySceneName);
            if (!string.IsNullOrWhiteSpace(resolvedSceneName))
            {
                gameplaySceneName = resolvedSceneName;
            }

            CleanupLegacySceneUi();
            mainMenuController = MainMenuController.EnsureMenuExists(gameplaySceneName, transform);
            mainMenuController.SetGameplaySceneName(gameplaySceneName);
            mainMenuController.MenuVisibilityChanged -= HandleMenuVisibilityChanged;
            mainMenuController.MenuVisibilityChanged += HandleMenuVisibilityChanged;

            BindLevelManager(levelManager);
            gameHudController = GameHudController.EnsureHudExists(levelManager, transform);
            gameHudController.BindLevelManager(levelManager);
            UpdateHudVisibility(levelManager);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (Instance != this)
            {
                return;
            }

            StopAllCoroutines();
            _advancingToNextLevel = false;

            var levelManager = FindAnyObjectByType<PrototypeLevelManager>();
            Initialize(levelManager, scene.name);
        }

        private string ResolveGameplaySceneName(string assignedGameplaySceneName)
        {
            if (LightChaseLevelCatalog.IsKnownSceneName(assignedGameplaySceneName))
            {
                return assignedGameplaySceneName;
            }

            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return gameplaySceneName;
            }

            // If we don't have a known gameplay scene name yet, default to Level 1.
            return LightChaseLevelCatalog.DefaultSceneName;
        }

        private void CleanupLegacySceneUi()
        {
            DestroyLegacySibling("GameplayHUD");
            DestroyLegacySibling("MainMenuOverlay");
        }

        private void HandleMenuVisibilityChanged(bool menuVisible)
        {
            UpdateHudVisibility(FindAnyObjectByType<PrototypeLevelManager>());
        }

        private void BindLevelManager(PrototypeLevelManager levelManager)
        {
            if (_levelManager != null)
            {
                _levelManager.StateChanged -= HandleLevelStateChanged;
            }

            _levelManager = levelManager;

            if (_levelManager != null)
            {
                _levelManager.StateChanged -= HandleLevelStateChanged;
                _levelManager.StateChanged += HandleLevelStateChanged;
            }
        }

        private void HandleLevelStateChanged()
        {
            if (_levelManager == null || mainMenuController == null)
            {
                return;
            }

            if (_levelManager.LevelCompleted)
            {
                TryAdvanceToNextLevel();
            }

            if (_levelManager.GameOver)
            {
                var title = _levelManager.TimerExpired
                    ? "TIEMPO AGOTADO"
                    : "TE ATRAPARON";
                var message = _levelManager.TimerExpired
                    ? "La oscuridad cerró la ruta. Reintenta o vuelve al menu principal."
                    : "Perdiste todas las vidas. Reintenta o vuelve al menu principal.";
                mainMenuController.ShowDefeatOverlay(title, message);
            }

            UpdateHudVisibility(_levelManager);
        }

        private void TryAdvanceToNextLevel()
        {
            if (_advancingToNextLevel)
            {
                return;
            }

            var activeSceneName = SceneManager.GetActiveScene().name;
            if (!LightChaseLevelCatalog.TryGetNextLevelSceneName(activeSceneName, out var nextSceneName))
            {
                // End of the prototype run: go back to menu.
                mainMenuController.ShowMenu();
                return;
            }

            _advancingToNextLevel = true;
            StartCoroutine(AdvanceAfterDelay(nextSceneName));
        }

        private System.Collections.IEnumerator AdvanceAfterDelay(string nextSceneName)
        {
            var delay = Mathf.Max(0f, advanceToNextLevelDelaySeconds);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            // Prefer gameplay scenes; menu scene is just a fallback.
            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }

            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void UpdateHudVisibility(PrototypeLevelManager levelManager)
        {
            if (gameHudController == null)
            {
                return;
            }

            var hudShouldBeVisible = levelManager != null
                && (mainMenuController == null || !mainMenuController.MenuVisible);
            gameHudController.SetHudVisible(hudShouldBeVisible);
        }

        private void DestroyLegacySibling(string objectName)
        {
            var legacyObject = GameObject.Find(objectName);
            while (legacyObject != null)
            {
                if (legacyObject.transform.parent == transform)
                {
                    break;
                }

                DestroyImmediate(legacyObject);
                legacyObject = GameObject.Find(objectName);
            }
        }
    }
}
