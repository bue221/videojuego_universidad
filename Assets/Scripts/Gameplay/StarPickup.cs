using UnityEngine;

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

        private Vector3 _startPosition;

        private void Awake()
        {
            _startPosition = transform.position;
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        public void ConfigureLight(Light assignedLight)
        {
            starLight = assignedLight;
        }

        private void Update()
        {
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
            if (!other.TryGetComponent(out PlayerLightState playerLightState))
            {
                return;
            }

            playerLightState.CollectStar(starValue);

            var levelManager = Object.FindAnyObjectByType<PrototypeLevelManager>();
            if (levelManager != null)
            {
                levelManager.RegisterStarCollected();
            }

            Destroy(gameObject);
        }
    }
}
