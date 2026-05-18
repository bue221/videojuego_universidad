using UnityEngine;

namespace LightChasePrototype
{
    [RequireComponent(typeof(Collider))]
    public class WaterVolume : MonoBehaviour
    {
        [Header("Traversal")]
        [SerializeField, Range(0.2f, 1f)] private float moveSpeedMultiplier = 0.6f;
        [SerializeField, Range(0.2f, 1f)] private float sprintSpeedMultiplier = 0.65f;
        [SerializeField, Range(0f, 1f)] private float jumpHeightMultiplier = 0.3f;

        [Header("Presentation")]
        [SerializeField, Range(0f, 1.25f)] private float visualSinkDepth = 0.55f;

        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float SprintSpeedMultiplier => sprintSpeedMultiplier;
        public float JumpHeightMultiplier => jumpHeightMultiplier;
        public float VisualSinkDepth => visualSinkDepth;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerWaterTraversal>(out var playerWaterTraversal))
            {
                playerWaterTraversal.EnterWater(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerWaterTraversal>(out var playerWaterTraversal))
            {
                playerWaterTraversal.ExitWater(this);
            }
        }

        private void EnsureTriggerCollider()
        {
            if (TryGetComponent<Collider>(out var collider))
            {
                collider.isTrigger = true;
            }
        }
    }
}
