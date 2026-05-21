using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LightChasePrototype.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject instructionsPanel;
        [SerializeField] private string gameplaySceneName = "LightChasePrototype";
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private bool pauseGameplayWhileVisible = true;
        [SerializeField] private GameObject mainActionsPanel;
        [SerializeField] private GameObject levelSelectionPanel;
        [SerializeField] private GameObject avatarSelectionPanel;
        [SerializeField] private Text avatarDescriptionText;
        [SerializeField] private Image avatarPreviewImage;
        [SerializeField] private Text levelDescriptionText;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Text defeatTitleText;
        [SerializeField] private Text defeatMessageText;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryTitleText;
        [SerializeField] private Text victoryMessageText;

        [Header("Decorative Background")]
        [SerializeField] private GameObject glowElement;
        [SerializeField] private GameObject institutionBanner;
        [SerializeField] private GameObject gameTitle;
        [SerializeField] private GameObject subtitleElement;
        [SerializeField] private GameObject creditsFooter;

        private Action<string> _sceneLoader;
        private Action _quitAction;
        private Func<string> _activeSceneNameProvider;
        private MonoBehaviour _starterAssetsInputs;
        private readonly List<AvatarButtonBinding> _avatarButtons = new();
        private readonly List<LevelButtonBinding> _levelButtons = new();
        // Tracks whether the user already pressed JUGAR and is now choosing an avatar.
        // We avoid relying solely on avatarSelectionPanel.activeSelf because the panel
        // state can be desynced after scene changes or overlay flows (defeat/victory).
        private bool _awaitingAvatarConfirmation;
        public event Action<bool> MenuVisibilityChanged;

        public bool InstructionsVisible => instructionsPanel != null && instructionsPanel.activeSelf;
        public string GameplaySceneName => gameplaySceneName;
        public bool MenuVisible => menuCanvasGroup != null && menuCanvasGroup.gameObject.activeSelf;
        public string SelectedAvatarId => PlayerAvatarSelection.SelectedAvatarId;
        public bool AvatarSelectionVisible => avatarSelectionPanel != null && avatarSelectionPanel.activeSelf;
        public bool LevelSelectionVisible => levelSelectionPanel != null && levelSelectionPanel.activeSelf;
        public bool DefeatVisible => defeatPanel != null && defeatPanel.activeSelf;
        public bool VictoryVisible => victoryPanel != null && victoryPanel.activeSelf;
        public string SelectedLevelId => ResolveSelectedLevel().Id;
        public string SelectedLevelSceneName => ResolveSelectedLevel().SceneName;

        public static MainMenuController EnsureMenuExists(string assignedGameplaySceneName = "LightChasePrototype", Transform parent = null)
        {
            var existingMenu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            if (existingMenu != null)
            {
                existingMenu.EnsureUiReferences();
                if (!existingMenu.HasRequiredMenuStructure())
                {
                    DestroyUnityObject(existingMenu.menuCanvasGroup != null
                        ? existingMenu.menuCanvasGroup.gameObject
                        : existingMenu.transform.parent != null
                            ? existingMenu.transform.parent.gameObject
                            : existingMenu.gameObject);
                }
                else
                {
                    existingMenu.gameplaySceneName = assignedGameplaySceneName;
                    return existingMenu;
                }
            }

            EnsureEventSystemExists();

            var canvasObject = new GameObject("MainMenuOverlay", typeof(RectTransform));
            if (parent != null)
            {
                canvasObject.transform.SetParent(parent, false);
                var menuRect = canvasObject.GetComponent<RectTransform>();
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.offsetMin = Vector2.zero;
                menuRect.offsetMax = Vector2.zero;
            }
            else
            {
                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                var scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var menuGroup = canvasObject.AddComponent<CanvasGroup>();

            var root = CreatePanel("Root", canvasObject.transform, new Color(0.039f, 0.020f, 0.008f, 0.96f));
            Stretch(root.GetComponent<RectTransform>());

            var glowObject = CreatePanel("Glow", root.transform, new Color(1.0f, 0.420f, 0.0f, 0.14f));
            SetAnchoredRect(glowObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.58f), new Vector2(1100f, 520f));

            var bannerObject = CreatePanel("InstitutionBanner", root.transform, new Color(0.06f, 0.03f, 0.01f, 0.92f));
            SetAnchoredRect(bannerObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.92f), new Vector2(1100f, 100f));

            CreateStyledText("UniversityTitle", bannerObject.transform, "UNIVERSIDAD CENTRAL", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.878f, 0.698f));
            SetAnchoredRect(bannerObject.transform.Find("UniversityTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.62f), new Vector2(960f, 44f));

            CreateStyledText("CourseTitle", bannerObject.transform, "MODELADO 3D Y VIDEOJUEGOS 2026", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.78f, 0.58f, 0.38f));
            SetAnchoredRect(bannerObject.transform.Find("CourseTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.28f), new Vector2(960f, 34f));

            var gameTitleObject = CreateStyledTextWithObject("GameTitle", root.transform, "CORRE CORRE QUE TE ATRAPO", 48, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.878f, 0.698f));
            SetAnchoredRect(gameTitleObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.76f), new Vector2(1100f, 70f));

            var subtitleObject = CreateStyledTextWithObject("Subtitle", root.transform, "Modelado 3D y videojuegos", 22, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(1f, 0.72f, 0.42f));
            SetAnchoredRect(subtitleObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.67f), new Vector2(1000f, 38f));

            var creditsFooterObject = CreateStyledTextWithObject("CreditsFooter", root.transform, "Hecho por Andres Plaza y Nicolas Fonseca", 20, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.78f, 0.58f, 0.38f));
            SetAnchoredRect(creditsFooterObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.04f), new Vector2(1200f, 32f));

            var levelSection = CreateLevelSelectionPanel(root.transform);

            var avatarSection = CreateAvatarSelectionPanel(root.transform);

            var defeatSection = CreateDefeatPanel(root.transform);

            var victorySection = CreateVictoryPanel(root.transform);

            var buttonStack = new GameObject("ButtonStack", typeof(RectTransform), typeof(VerticalLayoutGroup));
            buttonStack.transform.SetParent(root.transform, false);
            var stackRect = buttonStack.GetComponent<RectTransform>();
            stackRect.anchorMin = new Vector2(0.5f, 0.32f);
            stackRect.anchorMax = new Vector2(0.5f, 0.32f);
            stackRect.sizeDelta = new Vector2(420f, 280f);

            var layout = buttonStack.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var instructions = CreateInstructionsPanel(root.transform);
            var controller = root.AddComponent<MainMenuController>();
            controller.instructionsPanel = instructions;
            controller.gameplaySceneName = assignedGameplaySceneName;
            controller.menuCanvasGroup = menuGroup;
            controller.mainActionsPanel = buttonStack;
            controller.levelSelectionPanel = levelSection;
            controller.avatarSelectionPanel = avatarSection;
            controller.avatarDescriptionText = avatarSection.transform.Find("AvatarDescription").GetComponent<Text>();
            controller.avatarPreviewImage = avatarSection.transform.Find("AvatarPreviewFrame/AvatarPreview").GetComponent<Image>();
            controller.levelDescriptionText = levelSection.transform.Find("LevelDescription").GetComponent<Text>();
            controller.defeatPanel = defeatSection;
            controller.defeatTitleText = defeatSection.transform.Find("DefeatTitle").GetComponent<Text>();
            controller.defeatMessageText = defeatSection.transform.Find("DefeatMessage").GetComponent<Text>();
            controller.victoryPanel = victorySection;
            controller.victoryTitleText = victorySection.transform.Find("VictoryTitle").GetComponent<Text>();
            controller.victoryMessageText = victorySection.transform.Find("VictoryMessage").GetComponent<Text>();
            controller.glowElement = glowObject;
            controller.institutionBanner = bannerObject;
            controller.gameTitle = gameTitleObject;
            controller.subtitleElement = subtitleObject;
            controller.creditsFooter = creditsFooterObject;
            foreach (var option in PlayerAvatarSelection.GetAllOptions())
            {
                var btn = avatarSection.transform.Find("AvatarButtonRow/" + AvatarButtonName(option.Id))?.GetComponent<Button>();
                if (btn != null)
                {
                    controller.RegisterAvatarButton(option.Id, btn);
                }
            }
            controller.RegisterLevelButton(LightChaseLevelCatalog.PrototypeLevelId, levelSection.transform.Find("LevelButtonRow/Nivel1Button").GetComponent<Button>());
            controller.RegisterLevelButton(LightChaseLevelCatalog.NatureLevelId, levelSection.transform.Find("LevelButtonRow/Nivel2Button").GetComponent<Button>());
            controller.RegisterLevelButton(LightChaseLevelCatalog.WaterLevelId, levelSection.transform.Find("LevelButtonRow/Nivel3Button").GetComponent<Button>());
            controller.RegisterLevelButton(LightChaseLevelCatalog.LakeLevelId, levelSection.transform.Find("LevelButtonRow/Nivel4Button").GetComponent<Button>());
            levelSection.transform.Find("LevelActionRow/ConfirmLevelButton").GetComponent<Button>().onClick.AddListener(controller.ShowAvatarSelectionFromLevel);
            levelSection.transform.Find("LevelActionRow/BackLevelButton").GetComponent<Button>().onClick.AddListener(controller.HideLevelSelection);
            avatarSection.transform.Find("AvatarActionRow/ConfirmAvatarButton").GetComponent<Button>().onClick.AddListener(controller.StartGameFromAvatarSelection);
            avatarSection.transform.Find("AvatarActionRow/BackAvatarButton").GetComponent<Button>().onClick.AddListener(controller.HideAvatarSelection);
            defeatSection.transform.Find("DefeatButtonRow/RetryButton").GetComponent<Button>().onClick.AddListener(controller.RetryCurrentLevel);
            defeatSection.transform.Find("DefeatButtonRow/MenuButton").GetComponent<Button>().onClick.AddListener(controller.ReturnToMainMenu);
            victorySection.transform.Find("VictoryButtonRow/VictoryMenuButton").GetComponent<Button>().onClick.AddListener(controller.ReturnToMainMenu);

            instructions.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(controller.HideInstructions);
            CreateMenuButton(buttonStack.transform, "Jugar", new Color(0.20f, 0.10f, 0.02f), controller.PlayGame);
            CreateMenuButton(buttonStack.transform, "Instrucciones", new Color(0.14f, 0.07f, 0.01f), controller.ShowInstructions);
            CreateMenuButton(buttonStack.transform, "Salir", new Color(0.20f, 0.03f, 0.03f), controller.QuitGame);

            controller.RefreshAvatarSelectionUi();
            controller.ShowMenu();
            return controller;
        }

        public void Configure(GameObject assignedInstructionsPanel, string assignedGameplaySceneName)
        {
            instructionsPanel = assignedInstructionsPanel;
            gameplaySceneName = assignedGameplaySceneName;
            EnsureUiReferences();
            HideInstructions();
            HideLevelSelection();
            HideAvatarSelection();
            HideDefeat();
            HideVictory();
            ShowMainActions();
            RefreshAvatarSelectionUi();
            RefreshLevelSelectionUi();
        }

        public void ConfigureActionsForTests(Action<string> sceneLoader, Action quitAction, Func<string> activeSceneNameProvider = null)
        {
            _sceneLoader = sceneLoader;
            _quitAction = quitAction;
            _activeSceneNameProvider = activeSceneNameProvider;
        }

        public void SetGameplaySceneName(string assignedGameplaySceneName)
        {
            if (string.IsNullOrWhiteSpace(assignedGameplaySceneName))
            {
                return;
            }

            gameplaySceneName = assignedGameplaySceneName;
            RefreshLevelSelectionUi();
        }

        private void Awake()
        {
            EnsureUiReferences();
            SyncSelectedLevelToActiveScene();
            HideInstructions();
            HideLevelSelection();
            HideAvatarSelection();
            HideDefeat();
            HideVictory();
            ShowMainActions();
            RefreshLevelSelectionUi();
        }

        private void Update()
        {
            if (InstructionsVisible && WasCancelPressedThisFrame())
            {
                HideInstructions();
                return;
            }

            if (LevelSelectionVisible && WasCancelPressedThisFrame())
            {
                HideLevelSelection();
                return;
            }

            if (AvatarSelectionVisible && WasCancelPressedThisFrame())
            {
                HideAvatarSelection();
            }
        }

        private static bool WasCancelPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            // This project runs with the new Input System. Do not fall back to legacy Input here,
            // because it throws InvalidOperationException when Active Input Handling is set to Input System only.
            return false;
#endif
        }

        public void PlayGame()
        {
            EnsureUiReferences();
            HideDefeat();

            if (_awaitingAvatarConfirmation && AvatarSelectionVisible)
            {
                StartGameFromAvatarSelection();
                return;
            }

            // Level selection is disabled: pressing Play always starts a fresh
            // run from Level 1, regardless of the currently loaded scene.
            LightChaseLevelCatalog.SelectLevel(LightChaseLevelCatalog.PrototypeLevelId);

            ShowAvatarSelection();
            _awaitingAvatarConfirmation = true;
        }

        public void ShowLevelSelection()
        {
            EnsureUiReferences();

            if (levelSelectionPanel == null)
            {
                return;
            }

            HideInstructions();
            HideAvatarSelection();
            HideMainActions();
            HideDecorativeElements();
            levelSelectionPanel.SetActive(true);
            RefreshLevelSelectionUi();
        }

        public void HideLevelSelection()
        {
            if (levelSelectionPanel == null)
            {
                ReturnToMainActions();
                return;
            }

            levelSelectionPanel.SetActive(false);
            ReturnToMainActions();
        }

        public void ShowAvatarSelectionFromLevel()
        {
            HideLevelSelection();
            ShowAvatarSelection();
        }

        public void ShowAvatarSelection()
        {
            EnsureUiReferences();

            if (avatarSelectionPanel == null)
            {
                return;
            }

            HideInstructions();
            HideLevelSelection();
            HideMainActions();
            HideDecorativeElements();
            avatarSelectionPanel.SetActive(true);
            RefreshAvatarSelectionUi();
        }

        public void HideAvatarSelection()
        {
            _awaitingAvatarConfirmation = false;

            if (avatarSelectionPanel == null)
            {
                ReturnToMainActions();
                return;
            }

            avatarSelectionPanel.SetActive(false);
            ReturnToMainActions();
        }

        public void ShowInstructions()
        {
            EnsureUiReferences();

            if (instructionsPanel == null)
            {
                return;
            }

            HideLevelSelection();
            HideAvatarSelection();
            HideMainActions();
            HideDecorativeElements();
            instructionsPanel.SetActive(true);
        }

        public void HideInstructions()
        {
            if (instructionsPanel == null)
            {
                ReturnToMainActions();
                return;
            }

            instructionsPanel.SetActive(false);
            ReturnToMainActions();
        }

        private void ReturnToMainActions()
        {
            ShowMainActions();
            ShowDecorativeElements();
        }

        public void QuitGame()
        {
            if (_quitAction != null)
            {
                _quitAction.Invoke();
                return;
            }

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowMenu()
        {
            EnsureUiReferences();
            _awaitingAvatarConfirmation = false;
            HideInstructions();
            HideLevelSelection();
            HideAvatarSelection();
            HideDefeat();
            HideVictory();
            ShowMainActions();
            ShowDecorativeElements();
            RefreshAvatarSelectionUi();
            RefreshLevelSelectionUi();

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.gameObject.SetActive(true);
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }

            if (pauseGameplayWhileVisible)
            {
                Time.timeScale = 0f;
            }

            SetCursorForMenu(true);
            MenuVisibilityChanged?.Invoke(true);
        }

        public void HideMenu()
        {
            HideInstructions();
            HideLevelSelection();
            HideAvatarSelection();
            HideDefeat();
            HideVictory();
            HideMainActions();
            // Re-resolve inputs after avatar swaps (a newly instantiated player may have a new StarterAssetsInputs).
            _starterAssetsInputs = null;

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 0f;
                menuCanvasGroup.interactable = false;
                menuCanvasGroup.blocksRaycasts = false;
                menuCanvasGroup.gameObject.SetActive(false);
            }

            if (pauseGameplayWhileVisible)
            {
                Time.timeScale = 1f;
            }

            // Deselect any focused UI element so the Input System stops routing
            // navigation events to the UI module and delivers them to PlayerInput instead.
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null)
                es.SetSelectedGameObject(null);

            // If the active control scheme is a gamepad but no gamepad is actually
            // being used, switch back to keyboard+mouse so WASD/mouse work immediately.
            ForceKeyboardMouseSchemeIfNoGamepad();

            SetCursorForMenu(false);
            MenuVisibilityChanged?.Invoke(false);
        }

        private static void ForceKeyboardMouseSchemeIfNoGamepad()
        {
            var playerInput = UnityEngine.Object.FindAnyObjectByType<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput == null) return;

            var scheme = playerInput.currentControlScheme;
            if (string.IsNullOrEmpty(scheme) || scheme == "KeyboardMouse") return;

            // Check if a real gamepad is connected and active
            bool hasGamepad = false;
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
            {
                if (device is UnityEngine.InputSystem.Gamepad && device.enabled)
                {
                    hasGamepad = true;
                    break;
                }
            }

            if (!hasGamepad)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (kb != null)
                    playerInput.SwitchCurrentControlScheme("KeyboardMouse",
                        mouse != null ? new UnityEngine.InputSystem.InputDevice[] { kb, mouse }
                                      : new UnityEngine.InputSystem.InputDevice[] { kb });
            }
        }

        public void ShowDefeatOverlay(string title, string message)
        {
            EnsureUiReferences();
            HideInstructions();
            HideLevelSelection();
            HideAvatarSelection();
            HideMainActions();
            HideDecorativeElements();
            HideVictory();

            if (defeatTitleText != null)
            {
                defeatTitleText.text = title;
            }

            if (defeatMessageText != null)
            {
                defeatMessageText.text = message;
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.gameObject.SetActive(true);
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }

            if (pauseGameplayWhileVisible)
            {
                Time.timeScale = 0f;
            }

            SetCursorForMenu(true);
            MenuVisibilityChanged?.Invoke(true);
        }

        public void ShowVictoryOverlay(string title, string message)
        {
            EnsureUiReferences();
            HideInstructions();
            HideLevelSelection();
            HideAvatarSelection();
            HideMainActions();
            HideDecorativeElements();
            HideDefeat();

            if (victoryTitleText != null)
            {
                victoryTitleText.text = title;
            }

            if (victoryMessageText != null)
            {
                victoryMessageText.text = message;
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.gameObject.SetActive(true);
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }

            if (pauseGameplayWhileVisible)
            {
                Time.timeScale = 0f;
            }

            SetCursorForMenu(true);
            MenuVisibilityChanged?.Invoke(true);
        }

        private string GetActiveSceneName()
        {
            return _activeSceneNameProvider != null
                ? _activeSceneNameProvider.Invoke()
                : SceneManager.GetActiveScene().name;
        }

        private static void EnsureEventSystemExists()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            if (!TryAddInputSystemUiModule(eventSystem))
            {
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private void SetCursorForMenu(bool menuVisible)
        {
            _starterAssetsInputs ??= FindStarterAssetsInputs();

            if (_starterAssetsInputs != null)
            {
                SetBooleanFieldOrProperty(_starterAssetsInputs, "cursorLocked", !menuVisible);
                SetBooleanFieldOrProperty(_starterAssetsInputs, "cursorInputForLook", !menuVisible);
            }

            Cursor.lockState = menuVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menuVisible;
        }

        private static bool TryAddInputSystemUiModule(GameObject eventSystem)
        {
            var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType == null)
            {
                return false;
            }

            var component = eventSystem.AddComponent(inputModuleType);
            var assignDefaultActions = inputModuleType.GetMethod("AssignDefaultActions", Type.EmptyTypes);
            assignDefaultActions?.Invoke(component, null);
            return true;
        }

        private static MonoBehaviour FindStarterAssetsInputs()
        {
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().FullName == "StarterAssets.StarterAssetsInputs")
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static void SetBooleanFieldOrProperty(MonoBehaviour target, string memberName, bool value)
        {
            var targetType = target.GetType();
            var field = targetType.GetField(memberName);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(target, value);
                return;
            }

            var property = targetType.GetProperty(memberName);
            if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
            {
                property.SetValue(target, value);
            }
        }

        public void SelectAvatar(string avatarId)
        {
            PlayerAvatarSelection.SelectAvatar(avatarId);
            RefreshAvatarSelectionUi();
        }

        public void SelectLevel(string levelId)
        {
            LightChaseLevelCatalog.SelectLevel(levelId);
            RefreshLevelSelectionUi();
        }

        private void StartGameFromAvatarSelection()
        {
            _awaitingAvatarConfirmation = false;
            HideDefeat();
            GlobalUiController.MarkGameplayStarted();
            var selectedLevel = ResolveSelectedLevel();

            if (GetActiveSceneName() == selectedLevel.SceneName)
            {
                var levelManager = UnityEngine.Object.FindAnyObjectByType<PrototypeLevelManager>();
                if (levelManager != null && (levelManager.GameOver || levelManager.LevelCompleted))
                {
                    LoadLevelScene(selectedLevel.SceneName);
                    return;
                }

                PlayerAvatarSetup.EnsureSelectedAvatarInScene();
                HideMenu();
                return;
            }

            LoadLevelScene(selectedLevel.SceneName);
        }

        private void LoadLevelScene(string sceneName)
        {
            var selectedLevel = LightChaseLevelCatalog.GetLevelBySceneName(sceneName);

            if (_sceneLoader != null)
            {
                _sceneLoader.Invoke(selectedLevel.SceneName);
                return;
            }

#if UNITY_EDITOR
            var scenePath = LightChaseLevelCatalog.GetScenePath(selectedLevel.SceneName);
            EnsureSelectedLevelSceneExists(selectedLevel, scenePath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif

            SceneManager.LoadScene(selectedLevel.SceneName);
        }

#if UNITY_EDITOR
        private static void EnsureSelectedLevelSceneExists(LightChaseLevelCatalog.LevelOption selectedLevel, string scenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                return;
            }

            var menuItemPath = selectedLevel.Id switch
            {
                LightChaseLevelCatalog.PrototypeLevelId => "Tools/Prototype/Build Light Chase Level",
                LightChaseLevelCatalog.NatureLevelId => "Tools/Prototype/Build Light Chase Level 02",
                LightChaseLevelCatalog.WaterLevelId => "Tools/Prototype/Build Light Chase Level 03",
                LightChaseLevelCatalog.LakeLevelId => "Tools/Prototype/Build Light Chase Level 04",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(menuItemPath))
            {
                Debug.LogWarning($"No hay builder registrado para la escena faltante {selectedLevel.SceneName}.");
                return;
            }

            if (!EditorApplication.ExecuteMenuItem(menuItemPath))
            {
                Debug.LogError($"No se pudo ejecutar el builder '{menuItemPath}' para crear {selectedLevel.SceneName}.");
                return;
            }

            AssetDatabase.Refresh();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                return;
            }

            Debug.LogError($"El builder se ejecuto pero la escena {selectedLevel.SceneName} sigue sin existir en {scenePath}.");
        }
#endif

        private void ShowMainActions()
        {
            SetMainActionsVisible(true);
        }

        private void HideMainActions()
        {
            SetMainActionsVisible(false);
        }

        private void SetMainActionsVisible(bool visible)
        {
            if (mainActionsPanel == null)
            {
                return;
            }

            mainActionsPanel.SetActive(visible);
        }

        private void SetDecorativeElementsVisible(bool visible)
        {
            if (glowElement != null) glowElement.SetActive(visible);
            if (institutionBanner != null) institutionBanner.SetActive(visible);
            if (gameTitle != null) gameTitle.SetActive(visible);
            if (subtitleElement != null) subtitleElement.SetActive(visible);
            if (creditsFooter != null) creditsFooter.SetActive(visible);
        }

        private void HideDecorativeElements()
        {
            SetDecorativeElementsVisible(false);
        }

        private void ShowDecorativeElements()
        {
            SetDecorativeElementsVisible(true);
        }

        private void HideDefeat()
        {
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(false);
            }
        }

        private void HideVictory()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
        }

        private void RetryCurrentLevel()
        {
            SyncSelectedLevelToActiveScene();
            HideDefeat();
            LoadLevelScene(ResolveSelectedLevel().SceneName);
        }

        private void ReturnToMainMenu()
        {
            GlobalUiController.ResetGameplayProgress();
            ShowMenu();
        }

        private void OnDestroy()
        {
            DestroyAvatarPreviewSprite();
        }

        private void EnsureUiReferences()
        {
            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = GetComponentInParent<CanvasGroup>();
            }

            if (mainActionsPanel == null)
            {
                mainActionsPanel = transform.Find("ButtonStack")?.gameObject;
            }

            if (levelSelectionPanel == null)
            {
                levelSelectionPanel = transform.Find("LevelSelectionPanel")?.gameObject;
            }

            if (avatarSelectionPanel == null)
            {
                avatarSelectionPanel = transform.Find("AvatarSelectionPanel")?.gameObject;
            }

            if (instructionsPanel == null)
            {
                instructionsPanel = transform.Find("InstructionsPanel")?.gameObject;
            }

            if (avatarDescriptionText == null)
            {
                avatarDescriptionText = avatarSelectionPanel?.transform.Find("AvatarDescription")?.GetComponent<Text>();
            }

            if (avatarPreviewImage == null)
            {
                avatarPreviewImage = avatarSelectionPanel?.transform.Find("AvatarPreviewFrame/AvatarPreview")?.GetComponent<Image>();
            }

            if (levelDescriptionText == null)
            {
                levelDescriptionText = transform.Find("LevelSelectionPanel/LevelDescription")?.GetComponent<Text>();
            }

            if (defeatPanel == null)
            {
                defeatPanel = transform.Find("DefeatPanel")?.gameObject;
            }

            if (defeatTitleText == null)
            {
                defeatTitleText = defeatPanel?.transform.Find("DefeatTitle")?.GetComponent<Text>();
            }

            if (defeatMessageText == null)
            {
                defeatMessageText = defeatPanel?.transform.Find("DefeatMessage")?.GetComponent<Text>();
            }

            if (victoryPanel == null)
            {
                victoryPanel = transform.Find("VictoryPanel")?.gameObject;
            }

            if (victoryTitleText == null)
            {
                victoryTitleText = victoryPanel?.transform.Find("VictoryTitle")?.GetComponent<Text>();
            }

            if (victoryMessageText == null)
            {
                victoryMessageText = victoryPanel?.transform.Find("VictoryMessage")?.GetComponent<Text>();
            }

            if (glowElement == null)
            {
                glowElement = transform.Find("Glow")?.gameObject;
            }

            if (institutionBanner == null)
            {
                institutionBanner = transform.Find("InstitutionBanner")?.gameObject;
            }

            if (gameTitle == null)
            {
                gameTitle = transform.Find("GameTitle")?.gameObject;
            }

            if (subtitleElement == null)
            {
                subtitleElement = transform.Find("Subtitle")?.gameObject;
            }

            if (creditsFooter == null)
            {
                creditsFooter = transform.Find("CreditsFooter")?.gameObject;
            }
        }

        private bool HasRequiredMenuStructure()
        {
            if (mainActionsPanel == null || avatarSelectionPanel == null || levelSelectionPanel == null
                || instructionsPanel == null || victoryPanel == null)
            {
                return false;
            }

            // Verify level buttons
            if (levelSelectionPanel.transform.Find("LevelButtonRow/Nivel1Button") == null
                || levelSelectionPanel.transform.Find("LevelButtonRow/Nivel2Button") == null
                || levelSelectionPanel.transform.Find("LevelButtonRow/Nivel3Button") == null
                || levelSelectionPanel.transform.Find("LevelButtonRow/Nivel4Button") == null
                || levelSelectionPanel.transform.Find("LevelActionRow/ConfirmLevelButton") == null
                || levelSelectionPanel.transform.Find("LevelActionRow/BackLevelButton") == null)
            {
                return false;
            }

            // Verify avatar panel structure (dynamic: check that buttons exist for ALL current options)
            var avatarButtonRow = avatarSelectionPanel.transform.Find("AvatarButtonRow");
            if (avatarButtonRow == null || avatarButtonRow.childCount == 0)
            {
                return false;
            }

            foreach (var option in PlayerAvatarSelection.GetAllOptions())
            {
                if (avatarButtonRow.Find(AvatarButtonName(option.Id)) == null)
                {
                    return false;
                }
            }

            return avatarSelectionPanel.transform.Find("AvatarActionRow/ConfirmAvatarButton") != null
                && avatarSelectionPanel.transform.Find("AvatarActionRow/BackAvatarButton") != null
                && avatarSelectionPanel.transform.Find("AvatarPreviewFrame/AvatarPreview") != null
                && victoryPanel.transform.Find("VictoryButtonRow/VictoryMenuButton") != null;
        }

        private void RegisterAvatarButton(string avatarId, Button button)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() => SelectAvatar(avatarId));
            _avatarButtons.Add(new AvatarButtonBinding(avatarId, button));
        }

        private void RegisterLevelButton(string levelId, Button button)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() => SelectLevel(levelId));
            _levelButtons.Add(new LevelButtonBinding(levelId, button));
        }

        private void RefreshAvatarSelectionUi()
        {
            var selectedAvatar = PlayerAvatarSelection.SelectedAvatar;

            if (avatarDescriptionText != null)
            {
                avatarDescriptionText.text = $"{selectedAvatar.DisplayName}: {selectedAvatar.Description}";
            }

            if (avatarPreviewImage != null && (avatarSelectionPanel == null || avatarSelectionPanel.activeSelf))
            {
                DestroyAvatarPreviewSprite();
                avatarPreviewImage.sprite = PlayerAvatarSelection.BuildAvatarPreviewSprite(selectedAvatar.Id);
                avatarPreviewImage.preserveAspect = true;
                avatarPreviewImage.enabled = avatarPreviewImage.sprite != null;
            }

            foreach (var binding in _avatarButtons)
            {
                if (binding.Button == null)
                {
                    continue;
                }

                var colors = binding.Button.colors;
                var isSelected = binding.AvatarId == selectedAvatar.Id;
                var baseColor = isSelected
                    ? new Color(1f, 0.42f, 0f, 1f)
                    : new Color(0.15f, 0.075f, 0.015f, 1f);

                colors.normalColor = baseColor;
                colors.highlightedColor = baseColor * 1.1f;
                colors.pressedColor = baseColor * 0.9f;
                colors.selectedColor = baseColor * 1.05f;
                colors.disabledColor = new Color(0.24f, 0.24f, 0.24f, 0.75f);
                binding.Button.colors = colors;

                var image = binding.Button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = baseColor;
                }

                var label = binding.Button.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.text = isSelected
                        ? $"{PlayerAvatarSelection.GetAvatar(binding.AvatarId).DisplayName.ToUpperInvariant()}  ACTIVO"
                        : PlayerAvatarSelection.GetAvatar(binding.AvatarId).DisplayName.ToUpperInvariant();
                }
            }
        }

        private void RefreshLevelSelectionUi()
        {
            var selectedLevel = ResolveSelectedLevel();

            if (levelDescriptionText != null)
            {
                levelDescriptionText.text = $"{selectedLevel.DisplayName}: {selectedLevel.Description}";
            }

            foreach (var binding in _levelButtons)
            {
                if (binding.Button == null)
                {
                    continue;
                }

                var colors = binding.Button.colors;
                var isSelected = binding.LevelId == selectedLevel.Id;
                var baseColor = isSelected
                    ? new Color(1f, 0.42f, 0f, 1f)
                    : new Color(0.14f, 0.07f, 0.01f, 1f);

                colors.normalColor = baseColor;
                colors.highlightedColor = baseColor * 1.1f;
                colors.pressedColor = baseColor * 0.9f;
                colors.selectedColor = baseColor * 1.05f;
                colors.disabledColor = new Color(0.24f, 0.24f, 0.24f, 0.75f);
                binding.Button.colors = colors;

                var image = binding.Button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = baseColor;
                }

                var label = binding.Button.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.text = isSelected
                        ? $"{LightChaseLevelCatalog.GetLevel(binding.LevelId).DisplayName.ToUpperInvariant()}  ACTIVO"
                        : LightChaseLevelCatalog.GetLevel(binding.LevelId).DisplayName.ToUpperInvariant();
                }
            }
        }

        private LightChaseLevelCatalog.LevelOption ResolveSelectedLevel()
        {
            var selectedLevel = LightChaseLevelCatalog.SelectedLevel;
            if (!string.IsNullOrWhiteSpace(selectedLevel.SceneName))
            {
                return selectedLevel;
            }

            return LightChaseLevelCatalog.GetLevelBySceneName(gameplaySceneName);
        }

        private void SyncSelectedLevelToActiveScene()
        {
            var activeSceneName = GetActiveSceneName();
            if (!LightChaseLevelCatalog.IsKnownSceneName(activeSceneName))
            {
                return;
            }

            LightChaseLevelCatalog.SelectLevel(LightChaseLevelCatalog.GetLevelBySceneName(activeSceneName).Id);
        }

        private void DestroyAvatarPreviewSprite()
        {
            if (avatarPreviewImage?.sprite == null)
            {
                return;
            }

            var previousSprite = avatarPreviewImage.sprite;
            var previousTexture = previousSprite.texture;
            avatarPreviewImage.sprite = null;

            if (previousSprite != null)
            {
                DestroyUnityObject(previousSprite);
            }

            if (previousTexture != null)
            {
                DestroyUnityObject(previousTexture);
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(unityObject);
                return;
            }

            DestroyImmediate(unityObject);
        }

        private static GameObject CreateAvatarSelectionPanel(Transform parent)
        {
            var panel = CreatePanel("AvatarSelectionPanel", parent, new Color(0.055f, 0.028f, 0.008f, 0.96f));
            SetAnchoredRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1000f, 440f));

            CreateStyledText("AvatarTitle", panel.transform, "ELIGE TU AVATAR", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.72f));
            var titleRect = panel.transform.Find("AvatarTitle").GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.90f);
            titleRect.anchorMax = new Vector2(0.5f, 0.90f);
            titleRect.sizeDelta = new Vector2(520f, 44f);
            titleRect.anchoredPosition = Vector2.zero;

            var previewFrame = CreatePanel("AvatarPreviewFrame", panel.transform, new Color(0.08f, 0.04f, 0.01f, 0.96f));
            SetAnchoredRect(previewFrame.GetComponent<RectTransform>(), new Vector2(0.24f, 0.48f), new Vector2(240f, 240f));

            var previewImageObject = new GameObject("AvatarPreview", typeof(RectTransform), typeof(Image));
            previewImageObject.transform.SetParent(previewFrame.transform, false);
            var previewImageRect = previewImageObject.GetComponent<RectTransform>();
            previewImageRect.anchorMin = new Vector2(0.06f, 0.06f);
            previewImageRect.anchorMax = new Vector2(0.94f, 0.94f);
            previewImageRect.offsetMin = Vector2.zero;
            previewImageRect.offsetMax = Vector2.zero;
            var previewImage = previewImageObject.GetComponent<Image>();
            previewImage.color = Color.white;
            previewImage.preserveAspect = true;

            var buttonRow = new GameObject("AvatarButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(panel.transform, false);
            var rowRect = buttonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.68f, 0.66f);
            rowRect.anchorMax = new Vector2(0.68f, 0.66f);
            rowRect.anchoredPosition = Vector2.zero;

            var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 16f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            // Build one button per avatar option (hardcoded + catalog).
            var avatarOptions = PlayerAvatarSelection.GetAllOptions();
            var btnWidth = Mathf.Min(210f, (900f - (avatarOptions.Length - 1) * 16f) / avatarOptions.Length);
            rowRect.sizeDelta = new Vector2(avatarOptions.Length * btnWidth + (avatarOptions.Length - 1) * 16f, 68f);

            foreach (var option in avatarOptions)
            {
                var btn = CreateMenuButton(buttonRow.transform, option.DisplayName, new Color(0.15f, 0.075f, 0.015f), null);
                btn.name = AvatarButtonName(option.Id);
                btn.GetComponent<RectTransform>().sizeDelta = new Vector2(btnWidth, 64f);
            }

            CreateStyledText("AvatarDescription", panel.transform, string.Empty, 20, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.8f, 0.88f, 0.97f));
            var descriptionRect = panel.transform.Find("AvatarDescription").GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0.68f, 0.40f);
            descriptionRect.anchorMax = new Vector2(0.68f, 0.40f);
            descriptionRect.sizeDelta = new Vector2(480f, 86f);
            descriptionRect.anchoredPosition = Vector2.zero;

            var actionRow = new GameObject("AvatarActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionRow.transform.SetParent(panel.transform, false);
            var actionRect = actionRow.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.68f, 0.14f);
            actionRect.anchorMax = new Vector2(0.68f, 0.14f);
            actionRect.sizeDelta = new Vector2(460f, 64f);

            var actionLayout = actionRow.GetComponent<HorizontalLayoutGroup>();
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.spacing = 20f;
            actionLayout.childControlWidth = false;
            actionLayout.childControlHeight = false;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;

            var confirmButton = CreateMenuButton(actionRow.transform, "Comenzar", new Color(0.20f, 0.10f, 0.02f), null);
            confirmButton.name = "ConfirmAvatarButton";
            confirmButton.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 60f);

            var backButton = CreateMenuButton(actionRow.transform, "Volver", new Color(0.10f, 0.05f, 0.01f), null);
            backButton.name = "BackAvatarButton";
            backButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 60f);

            panel.SetActive(false);

            return panel;
        }

        private static GameObject CreateLevelSelectionPanel(Transform parent)
        {
            var panel = CreatePanel("LevelSelectionPanel", parent, new Color(0.055f, 0.028f, 0.008f, 0.96f));
            SetAnchoredRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1000f, 340f));

            CreateStyledText("LevelTitle", panel.transform, "ELIGE LA RUTA", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.72f));
            SetAnchoredRect(panel.transform.Find("LevelTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.86f), new Vector2(480f, 44f));

            var levelButtonRow = new GameObject("LevelButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            levelButtonRow.transform.SetParent(panel.transform, false);
            var rowRect = levelButtonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.58f);
            rowRect.anchorMax = new Vector2(0.5f, 0.58f);
            rowRect.sizeDelta = new Vector2(980f, 64f);

            var rowLayout = levelButtonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 18f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var nivel1Button = CreateMenuButton(levelButtonRow.transform, "Nivel 1", new Color(0.14f, 0.07f, 0.01f), null);
            nivel1Button.name = "Nivel1Button";
            nivel1Button.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 60f);

            var nivel2Button = CreateMenuButton(levelButtonRow.transform, "Nivel 2", new Color(0.14f, 0.07f, 0.01f), null);
            nivel2Button.name = "Nivel2Button";
            nivel2Button.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 60f);

            var nivel3Button = CreateMenuButton(levelButtonRow.transform, "Nivel 3", new Color(0.14f, 0.07f, 0.01f), null);
            nivel3Button.name = "Nivel3Button";
            nivel3Button.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 60f);

            var nivel4Button = CreateMenuButton(levelButtonRow.transform, "Nivel 4", new Color(0.14f, 0.07f, 0.01f), null);
            nivel4Button.name = "Nivel4Button";
            nivel4Button.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 60f);

            CreateStyledText("LevelDescription", panel.transform, string.Empty, 20, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.8f, 0.88f, 0.97f));
            SetAnchoredRect(panel.transform.Find("LevelDescription").GetComponent<RectTransform>(), new Vector2(0.5f, 0.34f), new Vector2(880f, 56f));

            var actionRow = new GameObject("LevelActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionRow.transform.SetParent(panel.transform, false);
            var actionRect = actionRow.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.5f, 0.12f);
            actionRect.anchorMax = new Vector2(0.5f, 0.12f);
            actionRect.sizeDelta = new Vector2(480f, 64f);

            var actionLayout = actionRow.GetComponent<HorizontalLayoutGroup>();
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.spacing = 20f;
            actionLayout.childControlWidth = false;
            actionLayout.childControlHeight = false;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;

            var confirmButton = CreateMenuButton(actionRow.transform, "Continuar", new Color(0.20f, 0.10f, 0.02f), null);
            confirmButton.name = "ConfirmLevelButton";
            confirmButton.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 60f);

            var backButton = CreateMenuButton(actionRow.transform, "Volver", new Color(0.10f, 0.05f, 0.01f), null);
            backButton.name = "BackLevelButton";
            backButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 60f);

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateDefeatPanel(Transform parent)
        {
            var panel = CreatePanel("DefeatPanel", parent, new Color(0.04f, 0.015f, 0.005f, 0.97f));
            SetAnchoredRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(900f, 340f));

            CreateStyledText("DefeatTitle", panel.transform, "PARTIDA TERMINADA", 36, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.42f, 0f));
            SetAnchoredRect(panel.transform.Find("DefeatTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.78f), new Vector2(640f, 54f));

            CreateStyledText("DefeatMessage", panel.transform, string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 0.878f, 0.698f));
            SetAnchoredRect(panel.transform.Find("DefeatMessage").GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), new Vector2(780f, 100f));

            var buttonRow = new GameObject("DefeatButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(panel.transform, false);
            var rowRect = buttonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.14f);
            rowRect.anchorMax = new Vector2(0.5f, 0.14f);
            rowRect.sizeDelta = new Vector2(540f, 64f);

            var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 20f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var retryButton = CreateMenuButton(buttonRow.transform, "Reintentar", new Color(0.20f, 0.10f, 0.02f), null);
            retryButton.name = "RetryButton";
            retryButton.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 60f);

            var menuButton = CreateMenuButton(buttonRow.transform, "Al menu principal", new Color(0.10f, 0.05f, 0.01f), null);
            menuButton.name = "MenuButton";
            menuButton.GetComponent<RectTransform>().sizeDelta = new Vector2(270f, 60f);

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateVictoryPanel(Transform parent)
        {
            var panel = CreatePanel("VictoryPanel", parent, new Color(0.04f, 0.015f, 0.005f, 0.97f));
            SetAnchoredRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(900f, 340f));

            CreateStyledText("VictoryTitle", panel.transform, "GANASTE EL JUEGO", 42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.42f, 0f));
            SetAnchoredRect(panel.transform.Find("VictoryTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.78f), new Vector2(720f, 60f));

            CreateStyledText("VictoryMessage", panel.transform, string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 0.878f, 0.698f));
            SetAnchoredRect(panel.transform.Find("VictoryMessage").GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), new Vector2(780f, 100f));

            var buttonRow = new GameObject("VictoryButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(panel.transform, false);
            var rowRect = buttonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.14f);
            rowRect.anchorMax = new Vector2(0.5f, 0.14f);
            rowRect.sizeDelta = new Vector2(360f, 64f);

            var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 20f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var menuButton = CreateMenuButton(buttonRow.transform, "Al menu principal", new Color(0.20f, 0.10f, 0.02f), null);
            menuButton.name = "VictoryMenuButton";
            menuButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 60f);

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateInstructionsPanel(Transform parent)
        {
            var panel = CreatePanel("InstructionsPanel", parent, new Color(0.04f, 0.020f, 0.008f, 0.97f));
            SetAnchoredRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1100f, 680f));

            CreateStyledText("InstructionsTitle", panel.transform, "COMO JUGAR", 38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.42f, 0f));
            SetAnchoredRect(panel.transform.Find("InstructionsTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.88f), new Vector2(600f, 56f));

            var bodyObject = new GameObject("InstructionsBody", typeof(RectTransform), typeof(Text));
            bodyObject.transform.SetParent(panel.transform, false);
            var bodyText = bodyObject.GetComponent<Text>();
            bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyText.text =
                "OBJETIVO\n" +
                "Recoge 5 de las 7 estrellas para activar el portal de salida y escapar.\n\n" +
                "CONTROLES\n" +
                "WASD  Moverte por el nivel\n" +
                "Mouse  Girar la camara\n" +
                "Shift izquierdo  Correr\n" +
                "Espacio  Saltar\n\n" +
                "COMO FUNCIONA EL RIESGO\n" +
                "Cada estrella aumenta tu brillo, hace mas visible tu rastro y permite que el enemigo te detecte desde mas lejos.\n" +
                "Recoger rapido te acerca a la meta, pero tambien te expone mas.\n\n" +
                "QUE HACE EL ENEMIGO\n" +
                "El perseguidor reacciona a tu firma de luz. Si estas muy brillante, te encuentra antes, acelera durante la persecucion y te quita vidas al alcanzarte.\n" +
                "Si oyes o ves que entra en alerta, cambia de ruta o corre al portal si ya esta activo.\n\n" +
                "CONDICIONES DE PARTIDA\n" +
                "Tienes 3 vidas y 180 segundos. Si te quedas sin tiempo o sin vidas, pierdes la partida.";
            bodyText.fontSize = 22;
            bodyText.fontStyle = FontStyle.Normal;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = new Color(1f, 0.878f, 0.698f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 16;
            bodyText.resizeTextMaxSize = 22;

            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(920f, 420f);
            bodyRect.anchoredPosition = new Vector2(0f, -8f);

            var closeButton = CreateMenuButton(panel.transform, "Cerrar", new Color(0.14f, 0.07f, 0.01f), null);
            closeButton.name = "CloseButton";
            SetAnchoredRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.07f), new Vector2(280f, 60f));
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateMenuButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(420f, 68f);

            var image = buttonObject.GetComponent<Image>();
            image.color = color;

            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.42f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = new Color(1.0f, 0.42f, 0.0f, 1.0f);
            colors.pressedColor = new Color(0.6f, 0.25f, 0.0f, 1.0f);
            colors.selectedColor = new Color(0.8f, 0.35f, 0.0f, 1.0f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            button.colors = colors;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            CreateStyledText("Label", buttonObject.transform, label.ToUpperInvariant(), 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(buttonObject.transform.Find("Label").GetComponent<RectTransform>());
            return buttonObject;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static void CreateText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 10);
            text.resizeTextMaxSize = fontSize;
        }

        private static void CreateStyledText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 12);
            text.resizeTextMaxSize = fontSize;

            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        private static GameObject CreateTextWithObject(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 10);
            text.resizeTextMaxSize = fontSize;
            return textObject;
        }

        private static GameObject CreateStyledTextWithObject(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 12);
            text.resizeTextMaxSize = fontSize;

            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -3f);

            return textObject;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchor, Vector2 size)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private readonly struct AvatarButtonBinding
        {
            public AvatarButtonBinding(string avatarId, Button button)
            {
                AvatarId = avatarId;
                Button = button;
            }

            public string AvatarId { get; }
            public Button Button { get; }
        }

        private readonly struct LevelButtonBinding
        {
            public LevelButtonBinding(string levelId, Button button)
            {
                LevelId = levelId;
                Button = button;
            }

            public string LevelId { get; }
            public Button Button { get; }
        }

        private static string AvatarButtonName(string avatarId) => $"AvatarBtn_{avatarId}";
    }
}
