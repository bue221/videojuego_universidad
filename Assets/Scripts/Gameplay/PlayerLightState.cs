using UnityEngine;

namespace LightChasePrototype
{
    public class PlayerLightState : MonoBehaviour
    {
        [Header("Brightness")]
        [SerializeField] private float baseBrightness = 0.45f;
        [SerializeField] private float brightnessPerStar = 0.45f;
        [SerializeField] private float maximumBrightness = 3.6f;

        [Header("Visuals")]
        [SerializeField] private Light glowLight;
        [SerializeField] private TrailRenderer glowTrail;
        [SerializeField] private Gradient dimTrail;
        [SerializeField] private Gradient brightTrail;

        public int StarsCollected { get; private set; }
        public float CurrentBrightness { get; private set; }
        public float NormalizedBrightness => LightChaseMath.NormalizeBrightness(CurrentBrightness, baseBrightness, maximumBrightness);

        private void Awake()
        {
            RecalculateBrightness();
        }

        public void ConfigureVisuals(Light assignedGlowLight, TrailRenderer assignedGlowTrail, Gradient assignedDimTrail, Gradient assignedBrightTrail)
        {
            glowLight = assignedGlowLight;
            glowTrail = assignedGlowTrail;
            dimTrail = assignedDimTrail;
            brightTrail = assignedBrightTrail;
            RecalculateBrightness();
        }

        public void CollectStar(int amount = 1)
        {
            StarsCollected += Mathf.Max(0, amount);
            RecalculateBrightness();
        }

        private void RecalculateBrightness()
        {
            CurrentBrightness = Mathf.Clamp(
                baseBrightness + (StarsCollected * brightnessPerStar),
                baseBrightness,
                maximumBrightness);

            if (glowLight != null)
            {
                glowLight.intensity = CurrentBrightness;
                glowLight.range = Mathf.Lerp(3.25f, 13.5f, NormalizedBrightness);
                glowLight.color = Color.Lerp(new Color(0.6f, 0.8f, 1f), new Color(1f, 0.85f, 0.3f), NormalizedBrightness);
            }

            if (glowTrail != null)
            {
                glowTrail.time = Mathf.Lerp(0.2f, 0.85f, NormalizedBrightness);
                glowTrail.startWidth = Mathf.Lerp(0.08f, 0.24f, NormalizedBrightness);
                glowTrail.endWidth = Mathf.Lerp(0.02f, 0.08f, NormalizedBrightness);
                glowTrail.colorGradient = NormalizedBrightness > 0.5f ? brightTrail : dimTrail;
            }
        }

        public float GetEnemyDetectionRange(float baseRange, float brightnessMultiplier)
        {
            return LightChaseMath.ComputeDetectionRange(baseRange, CurrentBrightness, brightnessMultiplier);
        }
    }
}
