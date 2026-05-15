using UnityEngine;

namespace LightChasePrototype
{
    [RequireComponent(typeof(Collider))]
    public class ExitPortal : MonoBehaviour
    {
        [SerializeField] private Renderer portalRenderer;
        [SerializeField] private Color lockedColor = new(0.35f, 0.55f, 1f);
        [SerializeField] private Color unlockedColor = new(0.1f, 1f, 0.45f);

        private PrototypeLevelManager _levelManager;

        public void ConfigureRenderer(Renderer assignedRenderer)
        {
            portalRenderer = assignedRenderer;
            RefreshVisual();
        }

        private void Awake()
        {
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void Start()
        {
            _levelManager = Object.FindAnyObjectByType<PrototypeLevelManager>();
            RefreshVisual();
        }

        private void Update()
        {
            RefreshVisual();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_levelManager == null || !_levelManager.CanExit())
            {
                return;
            }

            if (!other.TryGetComponent(out PlayerLightState _))
            {
                return;
            }

            Debug.Log("Nivel completado: encontraste la salida.");
        }

        private void RefreshVisual()
        {
            if (portalRenderer == null || _levelManager == null)
            {
                return;
            }

            portalRenderer.material.color = _levelManager.CanExit() ? unlockedColor : lockedColor;
        }
    }
}
