using System;
using UnityEngine;

namespace LightChasePrototype
{
    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        [SerializeField] private int starsRequiredToExit = 5;
        [SerializeField] private int startingLives = 3;
        [SerializeField] private int scorePerStar = 100;
        [SerializeField] private float levelTimeSeconds = 180f;

        public int CollectedStars { get; private set; }
        public int LivesRemaining { get; private set; }
        public int Score { get; private set; }
        public float RemainingTime { get; private set; }
        public int StarsRequiredToExit => starsRequiredToExit;
        public bool ExitUnlocked => CollectedStars >= starsRequiredToExit;
        public bool TimerExpired => RemainingTime <= 0f;
        public bool LevelCompleted { get; private set; }
        public bool GameOver => !LevelCompleted && (LivesRemaining <= 0 || TimerExpired);
        public event Action StateChanged;

        public static GameSessionManager EnsureExists()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindAnyObjectByType<GameSessionManager>();
            if (existing != null)
            {
                return existing;
            }

            var sessionObject = new GameObject("GameSessionManager");
            return sessionObject.AddComponent<GameSessionManager>();
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

        public void ConfigureLevel(int requiredStars, int lives, int scorePerCollectedStar, float timeLimitSeconds, bool resetRun = true)
        {
            starsRequiredToExit = Mathf.Max(1, requiredStars);
            startingLives = Mathf.Max(1, lives);
            scorePerStar = Mathf.Max(0, scorePerCollectedStar);
            levelTimeSeconds = Mathf.Max(1f, timeLimitSeconds);

            if (resetRun)
            {
                ResetRun();
                return;
            }

            NotifyStateChanged();
        }

        public void ResetRun()
        {
            CollectedStars = 0;
            Score = 0;
            LivesRemaining = startingLives;
            RemainingTime = levelTimeSeconds;
            LevelCompleted = false;
            NotifyStateChanged();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || LevelCompleted || GameOver)
            {
                return;
            }

            RemainingTime = Mathf.Max(0f, RemainingTime - deltaTime);
            NotifyStateChanged();
        }

        public void RegisterStarCollected(int amount = 1)
        {
            if (GameOver || LevelCompleted)
            {
                return;
            }

            var collectedAmount = Mathf.Max(0, amount);
            CollectedStars += collectedAmount;
            Score += collectedAmount * scorePerStar;
            NotifyStateChanged();
        }

        public bool ApplyPlayerHit(int damage = 1)
        {
            if (GameOver || LevelCompleted)
            {
                return false;
            }

            LivesRemaining = Mathf.Max(0, LivesRemaining - Mathf.Max(1, damage));
            NotifyStateChanged();
            return true;
        }

        public void ResetCollectedProgressAfterLifeLoss()
        {
            if (GameOver)
            {
                return;
            }

            CollectedStars = 0;
            Score = 0;
            LevelCompleted = false;
            NotifyStateChanged();
        }

        public void MarkLevelCompleted()
        {
            if (GameOver || LevelCompleted)
            {
                return;
            }

            LevelCompleted = true;
            NotifyStateChanged();
        }

        public bool CanExit()
        {
            return ExitUnlocked && !GameOver;
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
