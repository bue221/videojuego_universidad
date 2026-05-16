using System;
using UnityEngine;
using LightChasePrototype.UI;

namespace LightChasePrototype
{
    public class PrototypeLevelManager : MonoBehaviour
    {
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

        private void Awake()
        {
            PlayerAvatarSetup.EnsureSelectedAvatarInScene();
            LightChaseAtmosphere.ApplyRenderSettings();
            LightChaseAtmosphere.ApplyToSceneCameras();
            GameHudController.EnsureHudExists(this);
            MainMenuController.EnsureMenuExists();
            ResetRun();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void ResetRun()
        {
            CollectedStars = 0;
            Score = 0;
            LivesRemaining = Mathf.Max(1, startingLives);
            RemainingTime = Mathf.Max(1f, levelTimeSeconds);
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
            Score += collectedAmount * Mathf.Max(0, scorePerStar);
            Debug.Log($"Stars: {CollectedStars}/{starsRequiredToExit} | Score: {Score}");
            NotifyStateChanged();
        }

        public bool ApplyPlayerHit(int damage = 1)
        {
            if (GameOver || LevelCompleted)
            {
                return false;
            }

            LivesRemaining = Mathf.Max(0, LivesRemaining - Mathf.Max(1, damage));
            Debug.Log($"Vidas restantes: {LivesRemaining}");
            NotifyStateChanged();
            return true;
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
