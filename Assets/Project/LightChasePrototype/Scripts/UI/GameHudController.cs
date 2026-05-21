using LightChasePrototype;
using UnityEngine;
using UnityEngine.UI;

namespace LightChasePrototype.UI
{
    public class GameHudController : MonoBehaviour
    {
        [SerializeField] private PrototypeLevelManager levelManager;
        [SerializeField] private Text livesText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text statusText;
        [SerializeField] private Image livesPanel;
        [SerializeField] private Image scorePanel;
        [SerializeField] private Image timerPanel;
        [SerializeField] private Image statusPanel;

        private static readonly Color HudTextColor = new(1f, 0.878f, 0.698f, 1f);
        private static readonly Color LivesPanelColor = new(0.28f, 0.04f, 0.04f, 0.92f);
        private static readonly Color ScorePanelColor = new(0.22f, 0.09f, 0.01f, 0.92f);
        private static readonly Color TimerPanelColor = new(0.16f, 0.08f, 0.01f, 0.92f);
        private static readonly Color StatusInfoColor = new(0.07f, 0.035f, 0.008f, 0.94f);
        private static readonly Color StatusWarningColor = new(0.30f, 0.12f, 0.01f, 0.94f);
        private static readonly Color StatusDangerColor = new(0.34f, 0.04f, 0.04f, 0.96f);
        private static readonly Color StatusSuccessColor = new(0.04f, 0.25f, 0.08f, 0.94f);
        private enum StatusTone
        {
            Info,
            Warning,
            Danger,
            Success
        }

        public static GameHudController EnsureHudExists(PrototypeLevelManager levelManager, Transform parent = null)
        {
            var canvasObject = parent != null
                ? parent.Find("GameplayHUD")?.gameObject
                : GameObject.Find("GameplayHUD");

            if (canvasObject == null)
            {
                canvasObject = new GameObject("GameplayHUD", typeof(RectTransform));
                if (parent != null)
                {
                    canvasObject.transform.SetParent(parent, false);
                }
            }

            if (parent != null)
            {
                var rt = canvasObject.GetComponent<RectTransform>();
                if (rt == null) rt = canvasObject.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            if (parent == null)
            {
                var canvas = GetOrAddComponent<Canvas>(canvasObject);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;

                var scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GetOrAddComponent<GraphicRaycaster>(canvasObject);
            }

            var hud = GetOrAddComponent<GameHudController>(canvasObject);
            var references = EnsureHudVisuals(canvasObject.transform);
            hud.Configure(levelManager, references);
            hud.SetHudVisible(levelManager != null);
            return hud;
        }

        public void Configure(
            PrototypeLevelManager assignedLevelManager,
            Text assignedLivesText,
            Text assignedScoreText,
            Text assignedTimerText,
            Text assignedStatusText)
        {
            livesText = assignedLivesText;
            scoreText = assignedScoreText;
            timerText = assignedTimerText;
            statusText = assignedStatusText;
            TryBindLevelManager(assignedLevelManager);
            Refresh();
        }

        public void Configure(PrototypeLevelManager assignedLevelManager, HudReferences references)
        {
            livesText = references.LivesText;
            scoreText = references.ScoreText;
            timerText = references.TimerText;
            statusText = references.StatusText;
            livesPanel = references.LivesPanel;
            scorePanel = references.ScorePanel;
            timerPanel = references.TimerPanel;
            statusPanel = references.StatusPanel;
            TryBindLevelManager(assignedLevelManager);
            Refresh();
        }

        public void BindLevelManager(PrototypeLevelManager assignedLevelManager)
        {
            TryBindLevelManager(assignedLevelManager);
            Refresh();
        }

        public void SetHudVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void OnEnable()
        {
            TryBindLevelManager();
            Refresh();
        }

        private void OnDisable()
        {
            if (levelManager != null)
            {
                levelManager.StateChanged -= Refresh;
            }
        }

        private void TryBindLevelManager()
        {
            TryBindLevelManager(levelManager ?? Object.FindAnyObjectByType<PrototypeLevelManager>());
        }

        private void TryBindLevelManager(PrototypeLevelManager assignedLevelManager)
        {
            if (levelManager != null)
            {
                levelManager.StateChanged -= Refresh;
            }

            levelManager = assignedLevelManager;
            if (levelManager == null)
            {
                return;
            }

            levelManager.StateChanged -= Refresh;
            levelManager.StateChanged += Refresh;
        }

        private void Refresh()
        {
            if (levelManager == null)
            {
                return;
            }

            if (livesText != null)
            {
                livesText.text = $"VIDAS {levelManager.LivesRemaining}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"SCORE {levelManager.Score}";
            }

            if (timerText != null)
            {
                timerText.text = $"TIEMPO {FormatTime(levelManager.RemainingTime)}";
            }

            if (statusText != null)
            {
                var tone = BuildStatusTone();
                statusText.text = BuildStatusMessage();
                statusText.color = Color.white;

                if (statusPanel != null)
                {
                    statusPanel.color = GetStatusColor(tone);
                }
            }

        }

        private string BuildStatusMessage()
        {
            if (levelManager.LevelCompleted)
            {
                return "OBJETIVO CUMPLIDO  |  Escapaste con la luz intacta.";
            }

            if (levelManager.TimerExpired)
            {
                return "ALERTA ROJA  |  Tiempo agotado. La oscuridad te alcanzo.";
            }

            if (levelManager.LivesRemaining <= 0)
            {
                return "ALERTA ROJA  |  Sin vidas. El perseguidor te atrapo.";
            }

            if (levelManager.ExitUnlocked)
            {
                return "PORTAL ACTIVO  |  Corre a la salida ahora.";
            }

            return $"RECOLECCION  |  Estrellas {levelManager.CollectedStars}/{levelManager.StarsRequiredToExit}. Cada brillo te hace mas visible.";
        }

        private StatusTone BuildStatusTone()
        {
            if (levelManager.LevelCompleted)
            {
                return StatusTone.Success;
            }

            if (levelManager.TimerExpired || levelManager.LivesRemaining <= 0)
            {
                return StatusTone.Danger;
            }

            if (levelManager.ExitUnlocked)
            {
                return StatusTone.Warning;
            }

            return StatusTone.Info;
        }

        private static Color GetStatusColor(StatusTone tone)
        {
            return tone switch
            {
                StatusTone.Success => StatusSuccessColor,
                StatusTone.Warning => StatusWarningColor,
                StatusTone.Danger => StatusDangerColor,
                _ => StatusInfoColor
            };
        }

        private static string FormatTime(float remainingTime)
        {
            var clampedTime = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
            var minutes = clampedTime / 60;
            var seconds = clampedTime % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static HudReferences EnsureHudVisuals(Transform canvasTransform)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var livesPanel = CreateHudPanel(canvasTransform, "LivesPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(260f, 72f), LivesPanelColor);
            var scorePanel = CreateHudPanel(canvasTransform, "ScorePanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(300f, 72f), ScorePanelColor);
            var timerPanel = CreateHudPanel(canvasTransform, "TimerPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(260f, 72f), TimerPanelColor);
            var statusPanel = CreateHudPanel(canvasTransform, "StatusPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(980f, 80f), StatusInfoColor);

            var livesText = CreatePanelText(livesPanel.transform, "LivesText", font, 28, TextAnchor.MiddleCenter, HudTextColor);
            var scoreText = CreatePanelText(scorePanel.transform, "ScoreText", font, 28, TextAnchor.MiddleCenter, HudTextColor);
            var timerText = CreatePanelText(timerPanel.transform, "TimerText", font, 28, TextAnchor.MiddleCenter, HudTextColor);
            var statusText = CreatePanelText(statusPanel.transform, "StatusText", font, 26, TextAnchor.MiddleCenter, HudTextColor);
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 18;
            statusText.resizeTextMaxSize = 26;

            return new HudReferences(
                livesText,
                scoreText,
                timerText,
                statusText,
                livesPanel,
                scorePanel,
                timerPanel,
                statusPanel);
        }

        private static Image CreateHudPanel(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var panelTransform = parent.Find(objectName);
            var panelObject = panelTransform != null
                ? panelTransform.gameObject
                : new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow));

            if (panelTransform == null)
            {
                panelObject.transform.SetParent(parent, false);
            }

            var rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = ResolvePivot(anchorMin, anchorMax);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var image = GetOrAddComponent<Image>(panelObject);
            image.color = color;

            var outline = GetOrAddComponent<Outline>(panelObject);
            outline.effectColor = new Color(1f, 0.42f, 0f, 0.50f);
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = GetOrAddComponent<Shadow>(panelObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -5f);

            return image;
        }

        private static Text CreatePanelText(Transform parent, string objectName, Font font, int fontSize, TextAnchor alignment, Color color)
        {
            var existingTransform = parent.Find(objectName);
            var textObject = existingTransform != null
                ? existingTransform.gameObject
                : new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));

            if (existingTransform == null)
            {
                textObject.transform.SetParent(parent, false);
            }

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(18f, 12f);
            rectTransform.offsetMax = new Vector2(-18f, -12f);

            var text = GetOrAddComponent<Text>(textObject);
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, fontSize - 10);
            text.resizeTextMaxSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = GetOrAddComponent<Outline>(textObject);
            outline.effectColor = new Color(0f, 0f, 0f, 0.80f);
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = GetOrAddComponent<Shadow>(textObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
            shadow.effectDistance = new Vector2(0f, -3f);

            return text;
        }

        private static Vector2 ResolvePivot(Vector2 anchorMin, Vector2 anchorMax)
        {
            if (anchorMin == anchorMax)
            {
                return anchorMin;
            }

            return new Vector2(
                (anchorMin.x + anchorMax.x) * 0.5f,
                (anchorMin.y + anchorMax.y) * 0.5f);
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            if (!target.TryGetComponent<T>(out var component))
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        public readonly struct HudReferences
        {
            public HudReferences(
                Text livesText,
                Text scoreText,
                Text timerText,
                Text statusText,
                Image livesPanel,
                Image scorePanel,
                Image timerPanel,
                Image statusPanel)
            {
                LivesText = livesText;
                ScoreText = scoreText;
                TimerText = timerText;
                StatusText = statusText;
                LivesPanel = livesPanel;
                ScorePanel = scorePanel;
                TimerPanel = timerPanel;
                StatusPanel = statusPanel;
            }

            public Text LivesText { get; }
            public Text ScoreText { get; }
            public Text TimerText { get; }
            public Text StatusText { get; }
            public Image LivesPanel { get; }
            public Image ScorePanel { get; }
            public Image TimerPanel { get; }
            public Image StatusPanel { get; }
        }
    }
}
