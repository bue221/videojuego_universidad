using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LightChasePrototype.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject instructionsPanel;
        [SerializeField] private string gameplaySceneName = "LightChasePrototype";
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private bool pauseGameplayWhileVisible = true;
        [SerializeField] private Text avatarDescriptionText;

        private Action<string> _sceneLoader;
        private Action _quitAction;
        private Func<string> _activeSceneNameProvider;
        private MonoBehaviour _starterAssetsInputs;
        private readonly List<AvatarButtonBinding> _avatarButtons = new();

        public bool InstructionsVisible => instructionsPanel != null && instructionsPanel.activeSelf;
        public string GameplaySceneName => gameplaySceneName;
        public bool MenuVisible => menuCanvasGroup != null && menuCanvasGroup.gameObject.activeSelf;
        public string SelectedAvatarId => PlayerAvatarSelection.SelectedAvatarId;

        public static MainMenuController EnsureMenuExists(string assignedGameplaySceneName = "LightChasePrototype")
        {
            var existingMenu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            if (existingMenu != null)
            {
                existingMenu.gameplaySceneName = assignedGameplaySceneName;
                existingMenu.ShowMenu();
                return existingMenu;
            }

            EnsureEventSystemExists();

            var canvasObject = new GameObject("MainMenuOverlay");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            var menuGroup = canvasObject.AddComponent<CanvasGroup>();

            var root = CreatePanel("Root", canvasObject.transform, new Color(0.02f, 0.03f, 0.08f, 0.94f));
            Stretch(root.GetComponent<RectTransform>());

            var glow = CreatePanel("Glow", root.transform, new Color(0.18f, 0.35f, 0.6f, 0.22f));
            SetAnchoredRect(glow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.72f), new Vector2(820f, 240f));

            CreateText("UniversityTitle", root.transform, "UNIVERSIDAD CENTRAL", 44, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.82f, 0.92f, 1f));
            SetAnchoredRect(root.transform.Find("UniversityTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.84f), new Vector2(900f, 70f));

            CreateText("CourseTitle", root.transform, "MODELADO 3D Y VIDEOJUEGOS", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.66f, 0.84f, 1f));
            SetAnchoredRect(root.transform.Find("CourseTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.78f), new Vector2(900f, 60f));

            CreateText("GameTitle", root.transform, "CORRE CORRE QUE TE ATRAPAN", 54, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.72f));
            SetAnchoredRect(root.transform.Find("GameTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.63f), new Vector2(1100f, 90f));

            CreateText("Subtitle", root.transform, "La luz te delata. Entre mas estrellas recojas, mas facil te cazan.", 24, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.96f));
            SetAnchoredRect(root.transform.Find("Subtitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(1100f, 60f));

            var avatarSection = CreateAvatarSelectionPanel(root.transform);
            SetAnchoredRect(avatarSection.GetComponent<RectTransform>(), new Vector2(0.5f, 0.41f), new Vector2(980f, 170f));

            var buttonStack = new GameObject("ButtonStack", typeof(RectTransform), typeof(VerticalLayoutGroup));
            buttonStack.transform.SetParent(root.transform, false);
            var stackRect = buttonStack.GetComponent<RectTransform>();
            stackRect.anchorMin = new Vector2(0.5f, 0.2f);
            stackRect.anchorMax = new Vector2(0.5f, 0.2f);
            stackRect.sizeDelta = new Vector2(420f, 280f);

            var layout = buttonStack.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 18f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var instructions = CreateInstructionsPanel(root.transform);
            var controller = root.AddComponent<MainMenuController>();
            controller.instructionsPanel = instructions;
            controller.gameplaySceneName = assignedGameplaySceneName;
            controller.menuCanvasGroup = menuGroup;
            controller.avatarDescriptionText = avatarSection.transform.Find("AvatarDescription").GetComponent<Text>();
            controller.RegisterAvatarButton(PlayerAvatarSelection.ArmatureAvatarId, avatarSection.transform.Find("AvatarButtonRow/HumanoButton").GetComponent<Button>());
            controller.RegisterAvatarButton(PlayerAvatarSelection.CapsuleAvatarId, avatarSection.transform.Find("AvatarButtonRow/CapsulaButton").GetComponent<Button>());

            instructions.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(controller.HideInstructions);
            CreateMenuButton(buttonStack.transform, "Jugar", new Color(0.12f, 0.45f, 0.75f), controller.PlayGame);
            CreateMenuButton(buttonStack.transform, "Instrucciones", new Color(0.14f, 0.34f, 0.58f), controller.ShowInstructions);
            CreateMenuButton(buttonStack.transform, "Salir", new Color(0.28f, 0.16f, 0.24f), controller.QuitGame);

            controller.RefreshAvatarSelectionUi();
            controller.ShowMenu();
            return controller;
        }

        public void Configure(GameObject assignedInstructionsPanel, string assignedGameplaySceneName)
        {
            instructionsPanel = assignedInstructionsPanel;
            gameplaySceneName = assignedGameplaySceneName;
            HideInstructions();
            RefreshAvatarSelectionUi();
        }

        public void ConfigureActionsForTests(Action<string> sceneLoader, Action quitAction, Func<string> activeSceneNameProvider = null)
        {
            _sceneLoader = sceneLoader;
            _quitAction = quitAction;
            _activeSceneNameProvider = activeSceneNameProvider;
        }

        private void Awake()
        {
            HideInstructions();
        }

        private void Update()
        {
            if (InstructionsVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                HideInstructions();
            }
        }

        public void PlayGame()
        {
            if (GetActiveSceneName() == gameplaySceneName)
            {
                PlayerAvatarSetup.EnsureSelectedAvatarInScene();
                HideMenu();
                return;
            }

            var sceneLoader = _sceneLoader ?? SceneManager.LoadScene;
            sceneLoader.Invoke(gameplaySceneName);
        }

        public void ShowInstructions()
        {
            if (instructionsPanel == null)
            {
                return;
            }

            instructionsPanel.SetActive(true);
        }

        public void HideInstructions()
        {
            if (instructionsPanel == null)
            {
                return;
            }

            instructionsPanel.SetActive(false);
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
            RefreshAvatarSelectionUi();

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
        }

        public void HideMenu()
        {
            HideInstructions();

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

            SetCursorForMenu(false);
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

        private void RegisterAvatarButton(string avatarId, Button button)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() => SelectAvatar(avatarId));
            _avatarButtons.Add(new AvatarButtonBinding(avatarId, button));
        }

        private void RefreshAvatarSelectionUi()
        {
            var selectedAvatar = PlayerAvatarSelection.SelectedAvatar;

            if (avatarDescriptionText != null)
            {
                avatarDescriptionText.text = $"{selectedAvatar.DisplayName}: {selectedAvatar.Description}";
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
                    ? new Color(0.82f, 0.64f, 0.2f, 1f)
                    : new Color(0.16f, 0.24f, 0.38f, 1f);

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

        private static GameObject CreateAvatarSelectionPanel(Transform parent)
        {
            var panel = CreatePanel("AvatarSelectionPanel", parent, new Color(0.03f, 0.06f, 0.12f, 0.8f));

            CreateText("AvatarTitle", panel.transform, "ELIGE TU AVATAR", 28, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.96f, 1f));
            var titleRect = panel.transform.Find("AvatarTitle").GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.78f);
            titleRect.anchorMax = new Vector2(0.5f, 0.78f);
            titleRect.sizeDelta = new Vector2(520f, 40f);
            titleRect.anchoredPosition = Vector2.zero;

            var buttonRow = new GameObject("AvatarButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(panel.transform, false);
            var rowRect = buttonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(760f, 72f);
            rowRect.anchoredPosition = new Vector2(0f, 6f);

            var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 20f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var humanoButton = CreateMenuButton(buttonRow.transform, "Humano", new Color(0.16f, 0.24f, 0.38f), null);
            humanoButton.name = "HumanoButton";
            humanoButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 68f);

            var capsulaButton = CreateMenuButton(buttonRow.transform, "Capsula", new Color(0.16f, 0.24f, 0.38f), null);
            capsulaButton.name = "CapsulaButton";
            capsulaButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 68f);

            CreateText("AvatarDescription", panel.transform, string.Empty, 20, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.8f, 0.88f, 0.97f));
            var descriptionRect = panel.transform.Find("AvatarDescription").GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0.5f, 0.2f);
            descriptionRect.anchorMax = new Vector2(0.5f, 0.2f);
            descriptionRect.sizeDelta = new Vector2(860f, 40f);
            descriptionRect.anchoredPosition = Vector2.zero;

            return panel;
        }

        private static GameObject CreateInstructionsPanel(Transform parent)
        {
            var panel = CreatePanel("InstructionsPanel", parent, new Color(0.01f, 0.02f, 0.05f, 0.96f));
            SetAnchoredRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1140f, 680f));

            CreateText("InstructionsTitle", panel.transform, "COMO JUGAR", 38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.72f));
            SetAnchoredRect(panel.transform.Find("InstructionsTitle").GetComponent<RectTransform>(), new Vector2(0.5f, 0.86f), new Vector2(600f, 60f));

            CreateText(
                "InstructionsBody",
                panel.transform,
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
                "Tienes 3 vidas y 180 segundos. Si te quedas sin tiempo o sin vidas, pierdes la partida.",
                22,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.82f, 0.9f, 1f));

            var bodyRect = panel.transform.Find("InstructionsBody").GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(920f, 420f);
            bodyRect.anchoredPosition = new Vector2(0f, -10f);

            var closeButton = CreateMenuButton(panel.transform, "Cerrar", new Color(0.16f, 0.33f, 0.5f), null);
            closeButton.name = "CloseButton";
            SetAnchoredRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.11f), new Vector2(280f, 64f));
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateMenuButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(420f, 68f);

            var image = buttonObject.GetComponent<Image>();
            image.color = color;

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.9f;
            colors.selectedColor = color * 1.1f;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            button.colors = colors;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            CreateText("Label", buttonObject.transform, label.ToUpperInvariant(), 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
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
    }
}
