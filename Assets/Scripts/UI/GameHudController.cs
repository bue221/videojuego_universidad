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

        public static GameHudController EnsureHudExists(PrototypeLevelManager levelManager)
        {
            var existingHud = Object.FindAnyObjectByType<GameHudController>();
            if (existingHud != null)
            {
                existingHud.TryBindLevelManager(levelManager);
                existingHud.Refresh();
                return existingHud;
            }

            var canvasObject = new GameObject("GameplayHUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            canvasObject.AddComponent<GraphicRaycaster>();

            var hud = canvasObject.AddComponent<GameHudController>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var livesText = CreateHudText(canvas.transform, "LivesText", font, new Vector2(20f, -20f), TextAnchor.UpperLeft, 30, new Vector2(450f, 48f));
            var scoreText = CreateHudText(canvas.transform, "ScoreText", font, new Vector2(20f, -58f), TextAnchor.UpperLeft, 30, new Vector2(450f, 48f));
            var timerText = CreateHudText(canvas.transform, "TimerText", font, new Vector2(-20f, -20f), TextAnchor.UpperRight, 30, new Vector2(450f, 48f));
            var statusText = CreateHudText(canvas.transform, "StatusText", font, new Vector2(0f, 40f), TextAnchor.LowerCenter, 28, new Vector2(900f, 70f));

            hud.Configure(levelManager, livesText, scoreText, timerText, statusText);
            return hud;
        }

        public void Configure(PrototypeLevelManager assignedLevelManager, Text assignedLivesText, Text assignedScoreText, Text assignedTimerText, Text assignedStatusText)
        {
            livesText = assignedLivesText;
            scoreText = assignedScoreText;
            timerText = assignedTimerText;
            statusText = assignedStatusText;
            TryBindLevelManager(assignedLevelManager);
            Refresh();
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
                livesText.text = $"Vidas: {levelManager.LivesRemaining}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: {levelManager.Score}";
            }

            if (timerText != null)
            {
                timerText.text = $"Tiempo: {FormatTime(levelManager.RemainingTime)}";
            }

            if (statusText != null)
            {
                statusText.text = BuildStatusMessage();
            }
        }

        private string BuildStatusMessage()
        {
            if (levelManager.LevelCompleted)
            {
                return "Escapaste con la luz intacta.";
            }

            if (levelManager.TimerExpired)
            {
                return "Tiempo agotado. La oscuridad te alcanzo.";
            }

            if (levelManager.LivesRemaining <= 0)
            {
                return "Sin vidas. El perseguidor te atrapo.";
            }

            return levelManager.ExitUnlocked
                ? "Portal desbloqueado. Corre a la salida."
                : $"Recolecta estrellas: {levelManager.CollectedStars}/{levelManager.StarsRequiredToExit}";
        }

        private static string FormatTime(float remainingTime)
        {
            var clampedTime = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
            var minutes = clampedTime / 60;
            var seconds = clampedTime % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static Text CreateHudText(Transform parent, string objectName, Font font, Vector2 anchoredPosition, TextAnchor alignment, int fontSize, Vector2 size)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.95f, 0.95f, 1f, 0.98f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (alignment == TextAnchor.UpperRight)
            {
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
            }
            else if (alignment == TextAnchor.LowerCenter)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
            }

            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            return text;
        }
    }
}
