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

        // Distancia hacia arriba desde el pivote para originar el raycast que busca
        // el piso. Suficiente para sortear la holgura del NavMesh y la altura del
        // modelo sin chocar con techos bajos antes del piso.
        private const float GroundProbeRayUp = 1.5f;

        // Distancia maxima hacia abajo del raycast antes de declarar "sin piso debajo".
        private const float GroundProbeRayDown = 5f;

        // Margen anti Z-fighting: deja los pies justo encima del collider del piso.
        private const float GroundProbeContactEpsilon = 0.01f;

        [SerializeField] private float lightSignatureMultiplier = 1.85f;
        [SerializeField] private float maximumDetectionRange = 20f;
        [SerializeField] private float repathInterval = 0.2f;
        [SerializeField] private float baseMoveSpeed = DefaultBaseMoveSpeed;
        [SerializeField] private float chaseMoveSpeed = DefaultChaseMoveSpeed;
        [SerializeField] private float contactDamageRange = 1.65f;
        [SerializeField] private float damageInterval = 1.2f;
        [SerializeField] private float patrolRadius = 12f;
        [SerializeField] private float patrolArrivalDistance = 1.25f;
        [SerializeField] private float patrolIdleMin = 1.25f;
        [SerializeField] private float patrolIdleMax = 2.75f;
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

        // Offset vertical entre el pivote del transform del enemigo y los pies VISIBLES
        // del modelo. Se descubre con un raycast al piso en Start: si el modelo aparece
        // flotando o hundido, este valor se calibra para que los pies queden apoyados
        // sobre el primer collider que haya debajo. Lo exponemos como SerializeField
        // por si el raycast falla (escena sin colliders en el piso) y hay que afinar a
        // mano.
        [SerializeField] private float manualPivotToFeetOffset = 0f;
        [SerializeField] private bool autoCalibrateGroundOffset = true;

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
        private Vector3 _patrolOrigin;
        private Vector3 _patrolDestination;
        private bool _hasPatrolTarget;
        private float _patrolIdleTimer;
        private bool _wasChasingLastFrame;

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

            if (_animator != null)
            {
                _animator.enabled = true;
                // Inicializar Speed antes del primer Update evita el "T-pose flash" del
                // primer frame: el state Walk usa Speed como speedParameter, asi que
                // sin esto la animacion arranca pausada y se ve rigida.
                PrimeAnimatorSpeed(_animator, IdleAnimatorSpeed);
            }
        }

        private static void PrimeAnimatorSpeed(Animator animator, float initialSpeed)
        {
            if (animator.runtimeAnimatorController == null)
            {
                return;
            }

            if (HasFloatParameter(animator, "Speed"))
            {
                animator.SetFloat("Speed", initialSpeed);
                animator.speed = 1f;
            }
            else
            {
                animator.speed = Mathf.Max(0.1f, initialSpeed);
            }
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
            if (_playerTransform == null && _playerLightState != null)
            {
                _playerTransform = _playerLightState.transform;
            }

            CalibrateBaseOffsetFromGround();
            _patrolOrigin = transform.position;
        }

        // Hace que los pies VISIBLES del enemigo apoyen sobre el piso real, no sobre
        // la superficie voxelizada del NavMesh. La estrategia:
        //
        //   1. Mide donde esta el piso debajo del pivote con un raycast (fisica real).
        //   2. Estima cuanto sobresale el modelo por debajo del pivote usando los
        //      bounds de los renderers (mas un bias manual para ajustar a ojo si los
        //      bounds estan inflados).
        //   3. Calcula que valor deberia tener pivote.y para que pies == piso y ajusta
        //      NavMeshAgent.baseOffset al delta requerido. El agente respeta ese offset
        //      en el siguiente Update y deja al enemigo apoyado.
        //
        // Sirve para corregir tanto la voxelizacion del NavMesh (NavMesh por encima del
        // piso visible) como plataformas a distintas alturas (lago, mesetas, escalones).
        private void CalibrateBaseOffsetFromGround()
        {
            if (_agent == null || !autoCalibrateGroundOffset)
            {
                ApplyManualOffsetIfRequested();
                return;
            }

            if (!TryRaycastGroundBelow(out var groundY))
            {
                ApplyManualOffsetIfRequested();
                return;
            }

            // pivote a pies: lo aproximamos por bounds porque es la informacion mas
            // estable disponible sin bakear mesh. Si bounds esta inflado, el usuario
            // puede afinar con manualPivotToFeetOffset.
            var feetBelowPivot = EstimateFeetOffsetBelowPivot();

            // Queremos: pies del modelo apoyan en groundY + epsilon.
            // Como pivot = pies + feetBelowPivot, entonces pivotDeseado = groundY + epsilon + feetBelowPivot.
            var desiredPivotY = groundY + GroundProbeContactEpsilon + feetBelowPivot;
            var deltaY = desiredPivotY - _agent.nextPosition.y;
            _agent.baseOffset += deltaY;
        }

        private void ApplyManualOffsetIfRequested()
        {
            if (Mathf.Approximately(manualPivotToFeetOffset, 0f))
            {
                return;
            }

            _agent.baseOffset += manualPivotToFeetOffset;
        }

        private bool TryRaycastGroundBelow(out float groundY)
        {
            groundY = 0f;
            var pivotXZ = transform.position;
            var rayOrigin = new Vector3(pivotXZ.x, pivotXZ.y + GroundProbeRayUp, pivotXZ.z);
            var ownCollider = GetComponent<Collider>();
            var hadCollider = ownCollider != null && ownCollider.enabled;
            if (hadCollider)
            {
                ownCollider.enabled = false;
            }

            var foundGround = Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out var hit,
                GroundProbeRayUp + GroundProbeRayDown,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (hadCollider)
            {
                ownCollider.enabled = true;
            }

            if (!foundGround)
            {
                return false;
            }

            groundY = hit.point.y;
            return true;
        }

        private float EstimateFeetOffsetBelowPivot()
        {
            // Suma el offset configurado manualmente (puede ser 0). Sumarlo aqui en
            // lugar de mutar baseOffset deja el calculo en una sola formula.
            var manualBias = manualPivotToFeetOffset;

            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return manualBias;
            }

            var minY = float.PositiveInfinity;
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled)
                {
                    continue;
                }

                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                {
                    continue;
                }

                var bMin = r.bounds.min.y;
                if (bMin < minY)
                {
                    minY = bMin;
                }
            }

            if (float.IsPositiveInfinity(minY))
            {
                return manualBias;
            }

            // feetBelowPivot positivo si los pies estan POR DEBAJO del pivote (caso
            // tipico de AlignModelFeetToParent funcionando bien) y negativo si los
            // pies estan POR ENCIMA del pivote (los famosos bounds inflados).
            return (transform.position.y - minY) + manualBias;
        }

        private void Update()
        {
            if (_playerLightState == null || _playerTransform == null)
            {
                // Reference lost (e.g. avatar swap) — re-acquire from scene
                _playerLightState = Object.FindAnyObjectByType<PlayerLightState>();
                if (_playerLightState != null)
                    _playerTransform = _playerLightState.transform;

                if (_playerLightState == null || _playerTransform == null)
                {
                    UpdateAnimator(IdleAnimatorSpeed, isChasing: false);
                    UpdatePatrol();
                    return;
                }
            }

            if (_levelManager != null && (_levelManager.GameOver || _levelManager.LevelCompleted))
            {
                _agent.speed = baseMoveSpeed;
                _warningAudioSource.volume = 0f;
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }

                _hasPatrolTarget = false;
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

            if (!isChasing)
            {
                _damageTimer = 0f;

                // Al salir de chase, el agente arrastra el ultimo SetDestination al
                // jugador. Si no limpiamos ese path, UpdatePatrol piensa que ya hay
                // un destino vivo y se queda quieto hasta que expire el chase path.
                if (_wasChasingLastFrame && _agent != null && _agent.isOnNavMesh && _agent.hasPath)
                {
                    _agent.ResetPath();
                    _hasPatrolTarget = false;
                    _patrolIdleTimer = 0f;
                }

                UpdatePatrol();
                var isPatrolling = IsPatrollingMovement();
                UpdateAnimator(ResolveAnimatorSpeed(false, isAlerted || isPatrolling), isChasing: false);
                UpdateGlow(isAlerted ? 0.5f : 0f);
                _wasChasingLastFrame = false;
                return;
            }

            UpdateAnimator(ResolveAnimatorSpeed(true, isAlerted), isChasing: true);
            UpdateGlow(1f);

            _hasPatrolTarget = false;
            _patrolIdleTimer = 0f;
            _wasChasingLastFrame = true;

            TryDamagePlayer(distance);

            _repathTimer -= Time.deltaTime;
            if (_repathTimer > 0f)
            {
                return;
            }

            _repathTimer = repathInterval;
            if (_agent.isStopped)
            {
                _agent.isStopped = false;
            }

            _agent.SetDestination(_playerTransform.position);
        }

        // Animator debe verse caminando tanto cuando el agente ya tiene velocidad
        // como en los primeros frames despues de SetDestination, donde la path
        // todavia se esta calculando y velocity == 0 pero el enemigo SI se va a
        // mover. Sin esto, cada nuevo waypoint genera un parpadeo de idle.
        private bool IsPatrollingMovement()
        {
            if (_agent == null || !_agent.isOnNavMesh)
            {
                return false;
            }

            if (_agent.pathPending)
            {
                return true;
            }

            if (_hasPatrolTarget && _agent.hasPath)
            {
                return true;
            }

            return _agent.velocity.sqrMagnitude > 0.01f;
        }

        // Patrullaje sobre el NavMesh: el enemigo camina entre puntos aleatorios
        // alrededor de su origen para que el mapa se sienta vivo aun cuando el
        // jugador no esta cerca. Al detectar la luz, el flujo principal de Update
        // limpia el estado y entra inmediatamente en persecucion.
        //
        // No se usa NavMeshAgent.remainingDistance para detectar arribo porque
        // devuelve 0 durante el frame en que el path aun se esta calculando
        // (pathPending == true), o tambien apenas pathPending baja a false antes
        // de que el agente arranque a moverse. El resultado era que el enemigo
        // daba un par de pasos y se quedaba "llegado" enseguida. En su lugar
        // comparamos la posicion XZ contra el destino guardado, que es estable.
        private void UpdatePatrol()
        {
            if (_agent == null || !_agent.isOnNavMesh)
            {
                return;
            }

            if (_hasPatrolTarget)
            {
                if (_agent.pathPending)
                {
                    return;
                }

                if (HasArrivedAt(_patrolDestination))
                {
                    _hasPatrolTarget = false;
                    _patrolIdleTimer = Random.Range(patrolIdleMin, patrolIdleMax);
                    if (_agent.hasPath)
                    {
                        _agent.ResetPath();
                    }
                }
                else
                {
                    return;
                }
            }

            if (_patrolIdleTimer > 0f)
            {
                _patrolIdleTimer -= Time.deltaTime;
                return;
            }

            if (TryFindPatrolDestination(out var destination))
            {
                if (_agent.isStopped)
                {
                    _agent.isStopped = false;
                }

                if (_agent.SetDestination(destination))
                {
                    _patrolDestination = destination;
                    _hasPatrolTarget = true;
                }
                else
                {
                    _patrolIdleTimer = patrolIdleMin;
                }
            }
            else
            {
                _patrolIdleTimer = patrolIdleMin;
            }
        }

        private bool HasArrivedAt(Vector3 destination)
        {
            // Comparamos en XZ para no contar mal por diferencia de altura entre
            // el pivote del agente y el destino sampleado en el NavMesh.
            var here = transform.position;
            var dx = here.x - destination.x;
            var dz = here.z - destination.z;
            var distanceSqr = (dx * dx) + (dz * dz);
            var threshold = Mathf.Max(patrolArrivalDistance, _agent.stoppingDistance + 0.1f);
            return distanceSqr <= threshold * threshold;
        }

        private bool TryFindPatrolDestination(out Vector3 destination)
        {
            const int maxAttempts = 8;
            const float minStepFromCurrent = 2.5f;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var randomOffset = Random.insideUnitSphere * patrolRadius;
                randomOffset.y = 0f;
                var candidate = _patrolOrigin + randomOffset;
                if (!NavMesh.SamplePosition(candidate, out var hit, patrolRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                // Evita elegir un destino casi encima del enemigo, que daria la
                // sensacion de "se movio dos pasos y se quedo".
                var here = transform.position;
                var dx = hit.position.x - here.x;
                var dz = hit.position.z - here.z;
                if ((dx * dx) + (dz * dz) < minStepFromCurrent * minStepFromCurrent)
                {
                    continue;
                }

                destination = hit.position;
                return true;
            }

            destination = _patrolOrigin;
            return false;
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

            // Enemigo siempre debe verse vivo: idle, alerta y chase animan,
            // solo cambia la velocidad de la animacion.
            if (!_animator.enabled)
            {
                _animator.enabled = true;
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
        // En idle/patrulla la luz es neutra para no contaminar el color real de la
        // textura. Cuando el enemigo entra en alerta o chase la luz vira a calido y
        // luego a rojo, comunicando peligro sin pintar al enemigo todo el tiempo.
        enemyGlow.color = intensityFactor > 0.8f
            ? new Color(1f, 0.5f, 0.3f)
            : intensityFactor > 0.3f
                ? new Color(1f, 0.85f, 0.6f)
                : new Color(1f, 0.95f, 0.88f);
    }

        private void UpdateBodyGlow(float intensityFactor)
        {
            if (enemyBodyGlow == null)
            {
                return;
            }

        enemyBodyGlow.intensity = Mathf.Lerp(_bodyGlowBaseIntensity * 0.55f, _bodyGlowBaseIntensity * 1.6f, intensityFactor);
        enemyBodyGlow.color = intensityFactor > 0.8f
            ? new Color(1f, 0.45f, 0.25f)
            : intensityFactor > 0.3f
                ? new Color(1f, 0.78f, 0.45f)
                : new Color(1f, 0.92f, 0.82f);
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
