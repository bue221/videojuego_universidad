using UnityEngine;

namespace LightChasePrototype
{
    public static class LightChaseMath
    {
        public static float ComputeDetectionRange(float baseRange, float brightness, float brightnessMultiplier)
        {
            return Mathf.Max(0f, baseRange + (brightness * brightnessMultiplier));
        }

        public static float ComputeLightSignatureRange(float brightness, float signatureMultiplier, float maximumRange)
        {
            var signatureRange = brightness * brightness * signatureMultiplier;
            return Mathf.Clamp(signatureRange, 0f, maximumRange);
        }

        public static float NormalizeBrightness(float currentBrightness, float baseBrightness, float maximumBrightness)
        {
            if (maximumBrightness <= baseBrightness)
            {
                return 0f;
            }

            return Mathf.InverseLerp(baseBrightness, maximumBrightness, currentBrightness);
        }

        public static float ComputeWarningLevel(float distance, float detectionRange, float warningPadding)
        {
            var warningRange = Mathf.Max(detectionRange, detectionRange + Mathf.Max(0f, warningPadding));
            if (warningRange <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(1f - (distance / warningRange));
        }
    }
}
