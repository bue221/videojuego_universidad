using System;
using UnityEngine;
using LightChasePrototype.UI;
using UnityEngine.SceneManagement;

namespace LightChasePrototype
{
    public class PrototypeLevelManager : MonoBehaviour
    {
        [SerializeField] private int starsRequiredToExit = 5;
        [SerializeField] private int startingLives = 3;
        [SerializeField] private int scorePerStar = 100;
        [SerializeField] private float levelTimeSeconds = 180f;

        public int CollectedStars
        {
            get
            {
                EnsureInitialized();
                return _session.CollectedStars;
            }
        }

        public int LivesRemaining
        {
            get
            {
                EnsureInitialized();
                return _session.LivesRemaining;
            }
        }

        public int Score
        {
            get
            {
                EnsureInitialized();
                return _session.Score;
            }
        }

        public float RemainingTime
        {
            get
            {
                EnsureInitialized();
                return _session.RemainingTime;
            }
        }

        public int StarsRequiredToExit => starsRequiredToExit;
        public bool ExitUnlocked
        {
            get
            {
                EnsureInitialized();
                return _session.ExitUnlocked;
            }
        }

        public bool TimerExpired
        {
            get
            {
                EnsureInitialized();
                return _session.TimerExpired;
            }
        }

        public bool LevelCompleted
        {
            get
            {
                EnsureInitialized();
                return _session.LevelCompleted;
            }
        }

        public bool GameOver
        {
            get
            {
                EnsureInitialized();
                return _session.GameOver;
            }
        }

        public GameSessionManager Session
        {
            get
            {
                EnsureInitialized();
                return _session;
            }
        }
        public event Action StateChanged
        {
            add => Session.StateChanged += value;
            remove
            {
                if (_session != null)
                {
                    _session.StateChanged -= value;
                }
            }
        }

        private GameSessionManager _session;
        private Transform _playerTransform;
        private CharacterController _playerCharacterController;
        private TrailRenderer _playerTrailRenderer;
        private PlayerLightState _playerLightState;
        private Vector3 _playerSpawnPosition = PlayerAvatarSetup.DefaultSpawnPosition;
        private Quaternion _playerSpawnRotation = Quaternion.identity;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void ResetRun()
        {
            EnsureInitialized();
            Session.ConfigureLevel(starsRequiredToExit, startingLives, scorePerStar, levelTimeSeconds);
            ResetRuntimeRunState(respawnPlayer: true);
        }

        public void Tick(float deltaTime)
        {
            EnsureInitialized();
            Session.Tick(deltaTime);
        }

        public void RegisterStarCollected(int amount = 1)
        {
            EnsureInitialized();
            Session.RegisterStarCollected(amount);
            Debug.Log($"Stars: {CollectedStars}/{starsRequiredToExit} | Score: {Score}");
        }

        public bool ApplyPlayerHit(int damage = 1)
        {
            EnsureInitialized();
            if (!Session.ApplyPlayerHit(damage))
            {
                return false;
            }

            Debug.Log($"Vidas restantes: {LivesRemaining}");
            if (LivesRemaining > 0)
            {
                Session.ResetCollectedProgressAfterLifeLoss();
                ResetRuntimeRunState(respawnPlayer: true);
            }

            return true;
        }

        public void MarkLevelCompleted()
        {
            EnsureInitialized();
            Session.MarkLevelCompleted();
        }

        public bool CanExit()
        {
            EnsureInitialized();
            return Session.CanExit();
        }

        // Returns true when the given player GameObject matches the currently-selected
        // avatar. Derived from ResourcePath (e.g. "PlayerAvatars/PlayerAndres" → "PlayerAndres")
        // so no Resources.Load is needed — safe to call from Awake.
        private static bool IsSelectedAvatar(GameObject player)
        {
            if (player == null) return false;
            var option   = PlayerAvatarSelection.GetAvatar(PlayerAvatarSelection.SelectedAvatarId);
            var resPath  = option.ResourcePath ?? string.Empty;
            var slash    = resPath.LastIndexOf('/');
            var expected = slash >= 0 ? resPath.Substring(slash + 1) : resPath;
            return string.Equals(player.name, expected, System.StringComparison.OrdinalIgnoreCase)
                || player.name.Contains(expected);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerLightState>()?.gameObject;

            // Ensure the player in scene matches the avatar the user selected.
            // When transitioning between levels the scene always starts with the
            // default PlayerArmature; without this check it would be used even if
            // the player had picked a different avatar from the menu.
            if (player == null || !IsSelectedAvatar(player))
            {
                player = PlayerAvatarSetup.EnsureSelectedAvatarInScene()
                    ?? UnityEngine.Object.FindAnyObjectByType<PlayerLightState>()?.gameObject;
            }

            _session = GameSessionManager.EnsureExists();
            _session.ConfigureLevel(starsRequiredToExit, startingLives, scorePerStar, levelTimeSeconds);
            LightChaseAtmosphere.ApplyRenderSettings();
            LightChaseAtmosphere.ApplyToSceneCameras();
            CachePlayerReferences(player);
            _initialized = true;
            GlobalUiController.EnsureExists(this, SceneManager.GetActiveScene().name);
        }

        private void CachePlayerReferences(GameObject player)
        {
            if (player == null)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerLightState>()?.gameObject;
            }

            if (player == null)
            {
                return;
            }

            _playerTransform = player.transform;
            _playerCharacterController = player.GetComponent<CharacterController>();
            _playerTrailRenderer = player.GetComponentInChildren<TrailRenderer>();
            _playerLightState = player.GetComponent<PlayerLightState>();
            _playerSpawnPosition = _playerTransform.position;
            _playerSpawnRotation = _playerTransform.rotation;
        }

        private void ResetRuntimeRunState(bool respawnPlayer)
        {
            if (_playerLightState == null)
            {
                // Preserve the original spawn position before re-caching — CachePlayerReferences
                // would overwrite it with the player's current (mid-game) position otherwise.
                var savedSpawnPos = _playerSpawnPosition;
                var savedSpawnRot = _playerSpawnRotation;
                CachePlayerReferences(null);
                _playerSpawnPosition = savedSpawnPos;
                _playerSpawnRotation = savedSpawnRot;
            }

            _playerLightState?.ResetCollectedProgress();
            StarPickup.ResetCollectedPickups();

            if (respawnPlayer)
            {
                RespawnPlayerAtStart();
            }
        }

        private void RespawnPlayerAtStart()
        {
            if (_playerTransform == null)
            {
                CachePlayerReferences(null);
            }

            if (_playerTransform == null)
            {
                return;
            }

            var hadCharacterController = _playerCharacterController != null;
            if (hadCharacterController)
            {
                _playerCharacterController.enabled = false;
            }

            _playerTransform.SetPositionAndRotation(_playerSpawnPosition, _playerSpawnRotation);

            if (hadCharacterController)
            {
                _playerCharacterController.enabled = true;
            }

            if (_playerTrailRenderer != null)
            {
                _playerTrailRenderer.Clear();
            }
        }
    }
}
