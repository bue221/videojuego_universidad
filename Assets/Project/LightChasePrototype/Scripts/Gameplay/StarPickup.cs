using UnityEngine;
using System.Collections.Generic;

namespace LightChasePrototype
{
    [RequireComponent(typeof(Collider))]
    public class StarPickup : MonoBehaviour
    {
        [SerializeField] private int starValue = 1;
        [SerializeField] private float rotationSpeed = 75f;
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private Light starLight;

        private static readonly HashSet<StarPickup> RegisteredPickups = new();

        private Collider _trigger;
        private Renderer[] _renderers;
        private Vector3 _startPosition;
        private bool _initialized;

        public bool IsCollected { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _startPosition = transform.position;
            _trigger = GetComponent<Collider>();
            _trigger.isTrigger = true;
            _renderers = GetComponentsInChildren<Renderer>(true);
            RegisteredPickups.Add(this);
            SetCollectedState(false);
            _initialized = true;
        }

        public void ConfigureLight(Light assignedLight)
        {
            starLight = assignedLight;
        }

        private void OnDestroy()
        {
            RegisteredPickups.Remove(this);
        }

        private void Update()
        {
            Initialize();
            if (IsCollected)
            {
                return;
            }

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            var bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = _startPosition + (Vector3.up * bobOffset);

            if (starLight != null)
            {
                starLight.intensity = 0.8f + (Mathf.Sin(Time.time * (bobSpeed * 1.5f)) * 0.15f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Initialize();
            if (!other.TryGetComponent(out PlayerLightState playerLightState))
            {
                return;
            }

            Collect(playerLightState, Object.FindAnyObjectByType<PrototypeLevelManager>());
        }

        public void Collect(PlayerLightState playerLightState, PrototypeLevelManager levelManager = null)
        {
            Initialize();
            if (IsCollected || playerLightState == null)
            {
                return;
            }

            playerLightState.CollectStar(starValue);
            levelManager?.RegisterStarCollected(starValue);
            SetCollectedState(true);
        }

        public void ResetPickup()
        {
            Initialize();
            transform.position = _startPosition;
            SetCollectedState(false);
        }

        public static void ResetCollectedPickups()
        {
            foreach (var pickup in RegisteredPickups)
            {
                if (pickup != null)
                {
                    pickup.ResetPickup();
                }
            }
        }

        private void SetCollectedState(bool collected)
        {
            IsCollected = collected;

            if (_trigger != null)
            {
                _trigger.enabled = !collected;
            }

            if (_renderers != null)
            {
                foreach (var visual in _renderers)
                {
                    if (visual != null)
                    {
                        visual.enabled = !collected;
                    }
                }
            }

            if (starLight != null)
            {
                starLight.enabled = !collected;
            }
        }
    }
}
