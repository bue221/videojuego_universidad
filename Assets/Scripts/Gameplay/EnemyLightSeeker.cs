using UnityEngine;
using UnityEngine.AI;

namespace LightChasePrototype
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    public class EnemyLightSeeker : MonoBehaviour
    {
        private const float LegacyBaseMoveSpeed = 2.75f;
        private const float LegacyChaseMoveSpeed = 5.5f;
        private const float DefaultBaseMoveSpeed = 1.35f;
        private const float DefaultChaseMoveSpeed = 2.25f;

        [SerializeField] private float lightSignatureMultiplier = 1.85f;
        [SerializeField] private float maximumDetectionRange = 20f;
        [SerializeField] private float repathInterval = 0.2f;
        [SerializeField] private float baseMoveSpeed = DefaultBaseMoveSpeed;
        [SerializeField] private float chaseMoveSpeed = DefaultChaseMoveSpeed;
        [SerializeField] private float contactDamageRange = 1.65f;
        [SerializeField] private float damageInterval = 1.2f;
        [SerializeField] private float preDetectionWarningPadding = 5.5f;
        [SerializeField] private float minimumWarningVolume = 0.05f;
        [SerializeField] private float alertWarningVolume = 0.22f;
        [SerializeField] private float maximumWarningVolume = 0.6f;
        [SerializeField] private float minimumWarningPitch = 0.9f;
        [SerializeField] private float alertWarningPitch = 1.08f;
        [SerializeField] private float maximumWarningPitch = 1.35f;
        [SerializeField] private Renderer enemyRenderer;

        private NavMeshAgent _agent;
        private AudioSource _warningAudioSource;
        private PlayerLightState _playerLightState;
        private PrototypeLevelManager _levelManager;
        private Transform _playerTransform;
        private float _repathTimer;
        private float _damageTimer;

        private void Awake()
        {
            ApplyFallbackBalanceForLegacyScenes();
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = baseMoveSpeed;
            _warningAudioSource = GetComponent<AudioSource>();
            ConfigureWarningAudio();
        }

        public void ConfigureRenderer(Renderer assignedRenderer)
        {
            enemyRenderer = assignedRenderer;
        }

        private void Start()
        {
            _playerLightState = Object.FindAnyObjectByType<PlayerLightState>();
            _levelManager = Object.FindAnyObjectByType<PrototypeLevelManager>();
            if (_playerLightState != null)
            {
                _playerTransform = _playerLightState.transform;
            }
        }

        private void Update()
        {
            if (_playerLightState == null || _playerTransform == null)
            {
                return;
            }

            if (_levelManager != null && (_levelManager.GameOver || _levelManager.LevelCompleted))
            {
                _agent.speed = baseMoveSpeed;
                _warningAudioSource.volume = 0f;
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }

                return;
            }

            var distance = Vector3.Distance(transform.position, _playerTransform.position);
            var detectionRange = LightChaseMath.ComputeLightSignatureRange(
                _playerLightState.CurrentBrightness,
                lightSignatureMultiplier,
                maximumDetectionRange);
            var warningLevel = LightChaseMath.ComputeWarningLevel(distance, detectionRange, preDetectionWarningPadding);
            var isChasing = distance <= detectionRange;
            var isAlerted = !isChasing && warningLevel > 0f;

            _agent.speed = isChasing
                ? Mathf.Lerp(baseMoveSpeed, chaseMoveSpeed, Mathf.Clamp01(_playerLightState.NormalizedBrightness + 0.25f))
                : baseMoveSpeed;

            UpdateWarningAudio(warningLevel, isChasing);

            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = isChasing
                    ? new Color(1f, 0.35f, 0.2f)
                    : isAlerted
                        ? new Color(1f, 0.82f, 0.28f)
                        : new Color(0.3f, 0.3f, 0.38f);
                enemyRenderer.material.SetColor("_EmissionColor", isChasing
                    ? new Color(1.2f, 0.2f, 0.1f)
                    : isAlerted
                        ? new Color(0.85f, 0.6f, 0.08f)
                        : new Color(0.05f, 0.05f, 0.05f));
            }

            if (!isChasing)
            {
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }

                _damageTimer = 0f;
                return;
            }

            TryDamagePlayer(distance);

            _repathTimer -= Time.deltaTime;
            if (_repathTimer > 0f)
            {
                return;
            }

            _repathTimer = repathInterval;
            _agent.SetDestination(_playerTransform.position);
        }

        private void ConfigureWarningAudio()
        {
            _warningAudioSource.playOnAwake = false;
            _warningAudioSource.loop = true;
            _warningAudioSource.spatialBlend = 0f;
            _warningAudioSource.minDistance = 1f;
            _warningAudioSource.maxDistance = 8f;
            _warningAudioSource.rolloffMode = AudioRolloffMode.Linear;
            _warningAudioSource.volume = 0f;
            _warningAudioSource.pitch = minimumWarningPitch;

            if (_warningAudioSource.clip == null)
            {
                _warningAudioSource.clip = CreateWarningClip();
            }

            _warningAudioSource.Play();
        }

        private void ApplyFallbackBalanceForLegacyScenes()
        {
            if (baseMoveSpeed >= LegacyBaseMoveSpeed - 0.01f)
            {
                baseMoveSpeed = DefaultBaseMoveSpeed;
            }

            if (chaseMoveSpeed >= LegacyChaseMoveSpeed - 0.01f)
            {
                chaseMoveSpeed = DefaultChaseMoveSpeed;
            }

            chaseMoveSpeed = Mathf.Max(chaseMoveSpeed, baseMoveSpeed + 0.25f);
        }

        private void UpdateWarningAudio(float warningLevel, bool isChasing)
        {
            if (_warningAudioSource == null)
            {
                return;
            }

            if (warningLevel <= 0.01f)
            {
                _warningAudioSource.volume = 0f;
                _warningAudioSource.pitch = minimumWarningPitch;
                return;
            }

            if (isChasing)
            {
                _warningAudioSource.volume = Mathf.Lerp(alertWarningVolume, maximumWarningVolume, warningLevel);
                _warningAudioSource.pitch = Mathf.Lerp(alertWarningPitch, maximumWarningPitch, warningLevel);
                return;
            }

            _warningAudioSource.volume = Mathf.Lerp(minimumWarningVolume, alertWarningVolume, warningLevel);
            _warningAudioSource.pitch = Mathf.Lerp(minimumWarningPitch, alertWarningPitch, warningLevel);
        }

        private void TryDamagePlayer(float distance)
        {
            if (_levelManager == null || distance > contactDamageRange)
            {
                return;
            }

            _damageTimer -= Time.deltaTime;
            if (_damageTimer > 0f)
            {
                return;
            }

            if (_levelManager.ApplyPlayerHit())
            {
                _damageTimer = damageInterval;
            }
        }

        private static AudioClip CreateWarningClip()
        {
            const int sampleRate = 44100;
            const float durationSeconds = 0.24f;
            const float frequency = 220f;
            var totalSamples = Mathf.CeilToInt(sampleRate * durationSeconds);
            var samples = new float[totalSamples];

            for (var i = 0; i < totalSamples; i++)
            {
                var t = i / (float)sampleRate;
                var pulse = Mathf.Sin(Mathf.PI * (i / (float)totalSamples));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * pulse * 0.25f;
            }

            var clip = AudioClip.Create("EnemyWarningPulse", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
