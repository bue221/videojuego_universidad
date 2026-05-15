using UnityEngine;
using UnityEngine.AI;

namespace LightChasePrototype
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyLightSeeker : MonoBehaviour
    {
        [SerializeField] private float lightSignatureMultiplier = 1.85f;
        [SerializeField] private float maximumDetectionRange = 20f;
        [SerializeField] private float repathInterval = 0.2f;
        [SerializeField] private float baseMoveSpeed = 2.75f;
        [SerializeField] private float chaseMoveSpeed = 5.5f;
        [SerializeField] private Renderer enemyRenderer;

        private NavMeshAgent _agent;
        private PlayerLightState _playerLightState;
        private Transform _playerTransform;
        private float _repathTimer;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = baseMoveSpeed;
        }

        public void ConfigureRenderer(Renderer assignedRenderer)
        {
            enemyRenderer = assignedRenderer;
        }

        private void Start()
        {
            _playerLightState = Object.FindAnyObjectByType<PlayerLightState>();
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

            var distance = Vector3.Distance(transform.position, _playerTransform.position);
            var detectionRange = LightChaseMath.ComputeLightSignatureRange(
                _playerLightState.CurrentBrightness,
                lightSignatureMultiplier,
                maximumDetectionRange);
            var isChasing = distance <= detectionRange;

            _agent.speed = isChasing
                ? Mathf.Lerp(baseMoveSpeed, chaseMoveSpeed, Mathf.Clamp01(_playerLightState.NormalizedBrightness + 0.25f))
                : baseMoveSpeed;

            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = isChasing ? new Color(1f, 0.35f, 0.2f) : new Color(0.3f, 0.3f, 0.38f);
                enemyRenderer.material.SetColor("_EmissionColor", isChasing
                    ? new Color(1.2f, 0.2f, 0.1f)
                    : new Color(0.05f, 0.05f, 0.05f));
            }

            if (!isChasing)
            {
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }

                return;
            }

            _repathTimer -= Time.deltaTime;
            if (_repathTimer > 0f)
            {
                return;
            }

            _repathTimer = repathInterval;
            _agent.SetDestination(_playerTransform.position);
        }
    }
}
