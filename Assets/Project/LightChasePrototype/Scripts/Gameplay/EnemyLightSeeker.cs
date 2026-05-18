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

        private const float IdleAnimatorSpeed = 0.45f;
        private const float WalkAnimatorSpeed = 1f;
        private const float ChaseAnimatorSpeed = 1.35f;
        private const float InitialIdleAnimationFrames = 2f;

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
        [SerializeField] private Light enemyGlow;
        [SerializeField] private Light enemyBodyGlow;

        private NavMeshAgent _agent;
        private Animator _animator;
        private AudioSource _warningAudioSource;
        private PlayerLightState _playerLightState;
        private PrototypeLevelManager _levelManager;
        private Transform _playerTransform;
        private float _repathTimer;
        private float _damageTimer;
        private float _glowBaseIntensity;
        private float _bodyGlowBaseIntensity;
        private bool _animatorBootstrapped;
        private int _animatorBootstrapFramesRemaining;

        private void Awake()
        {
            ApplyFallbackBalanceForLegacyScenes();
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = baseMoveSpeed;
            _animator = ResolveAnimator();
            _warningAudioSource = GetComponent<AudioSource>();
            ConfigureWarningAudio();

            _glowBaseIntensity = enemyGlow != null ? enemyGlow.intensity : 2.4f;
            _bodyGlowBaseIntensity = enemyBodyGlow != null ? enemyBodyGlow.intensity : 1.1f;

            _animatorBootstrapFramesRemaining = Mathf.CeilToInt(InitialIdleAnimationFrames);
        }

        public void ConfigureRenderer(Renderer assignedRenderer)
        {
            enemyRenderer = assignedRenderer;
        }

        public void ConfigureGlow(Light glowLight)
        {
            enemyGlow = glowLight;
            if (enemyGlow != null)
            {
                _glowBaseIntensity = enemyGlow.intensity;
            }
        }

        public void ConfigureBodyGlow(Light bodyLight)
        {
            enemyBodyGlow = bodyLight;
            if (enemyBodyGlow != null)
            {
                _bodyGlowBaseIntensity = enemyBodyGlow.intensity;
            }
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
                UpdateAnimator(IdleAnimatorSpeed, isChasing: false);
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

                UpdateAnimator(IdleAnimatorSpeed, isChasing: false);
                UpdateGlow(0f);
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
            UpdateAnimator(ResolveAnimatorSpeed(isChasing, isAlerted), isChasing);
            UpdateGlow(isChasing ? 1f : isAlerted ? 0.5f : 0f);

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

        private static float ResolveAnimatorSpeed(bool isChasing, bool isAlerted)
        {
            if (isChasing)
            {
                return ChaseAnimatorSpeed;
            }

            return isAlerted ? WalkAnimatorSpeed : IdleAnimatorSpeed;
        }

        private void UpdateAnimator(float targetAnimatorSpeed, bool isChasing)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            if (HasFloatParameter(_animator, "Speed"))
            {
                _animator.SetFloat("Speed", targetAnimatorSpeed);
                _animator.speed = 1f;
            }
            else
            {
                _animator.speed = Mathf.Max(0.1f, targetAnimatorSpeed);
            }

            ApplyAnimatorActiveState(isChasing);
        }

        private void ApplyAnimatorActiveState(bool isChasing)
        {
            if (!_animatorBootstrapped)
            {
                _animator.enabled = true;
                if (_animatorBootstrapFramesRemaining > 0)
                {
                    _animatorBootstrapFramesRemaining--;
                    return;
                }

                _animatorBootstrapped = true;
            }

            if (_animator.enabled != isChasing)
            {
                _animator.enabled = isChasing;
            }
        }

        private Animator ResolveAnimator()
        {
            var allAnimators = GetComponentsInChildren<Animator>(true);
            Animator bestAnimator = null;
            var bestScore = int.MinValue;

            foreach (var candidate in allAnimators)
            {
                if (candidate == null)
                {
                    continue;
                }

                var score = 0;
                if (candidate.runtimeAnimatorController != null)
                {
                    score += 4;
                }

                if (candidate.avatar != null && candidate.avatar.isValid)
                {
                    score += 2;
                }

                if (candidate.transform != transform)
                {
                    score += 1;
                }

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestAnimator = candidate;
            }

            return bestAnimator ?? GetComponent<Animator>();
        }

        private static bool HasFloatParameter(Animator animator, string parameterName)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateGlow(float intensityFactor)
        {
            UpdateSpotGlow(intensityFactor);
            UpdateBodyGlow(intensityFactor);
        }

        private void UpdateSpotGlow(float intensityFactor)
        {
            if (enemyGlow == null)
            {
                return;
            }

            enemyGlow.intensity = Mathf.Lerp(_glowBaseIntensity * 0.65f, _glowBaseIntensity * 1.4f, intensityFactor);
            enemyGlow.color = intensityFactor > 0.8f
                ? new Color(1f, 0.45f, 0.25f)
                : intensityFactor > 0.3f
                    ? new Color(1f, 0.72f, 0.4f)
                    : new Color(1f, 0.85f, 0.6f);
        }

        private void UpdateBodyGlow(float intensityFactor)
        {
            if (enemyBodyGlow == null)
            {
                return;
            }

            enemyBodyGlow.intensity = Mathf.Lerp(_bodyGlowBaseIntensity * 0.55f, _bodyGlowBaseIntensity * 1.6f, intensityFactor);
            enemyBodyGlow.color = intensityFactor > 0.8f
                ? new Color(1f, 0.4f, 0.2f)
                : intensityFactor > 0.3f
                    ? new Color(1f, 0.6f, 0.3f)
                    : new Color(1f, 0.78f, 0.45f);
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
