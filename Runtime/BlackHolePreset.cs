using UnityEngine;

namespace CannoliCat.Space {
    [CreateAssetMenu(menuName = "CannoliCat/Black Hole Preset", fileName = "BlackHolePreset")]
    public class BlackHolePreset : ScriptableObject {
        [Range(0f, 2f)]
        public float skyIntensity = 0.5f;

        [Header("Black Hole Physics")]
        public float schwarzschildRadius = 2.0f;

        [Range(0f, 0.999f)]
        public float spinParameter = 0.9f;

        public BlackHoleFeature.RayTracingMode rayTracing = BlackHoleFeature.RayTracingMode.Exact;

        [Header("Disk Geometry")]
        public float diskThickness = 0.02f;

        [Range(4f, 30f)]
        public float diskOuterRadius = 12f;

        [Header("Disk Physics")]
        public float maxTemperature = 10000f;

        public float diskDensity = 0.5f;

        [Range(0f, 20f)]
        public float diskExposure = 1.0f;

        [Range(0f, 1f)]
        public float dopplerStrength = 1.0f;

        [Range(0f, 1f)]
        public float temperatureFalloff = 1.0f;

        [Header("Turbulence (MRI)")]
        [Range(0.1f, 5f)]
        public float noiseScale = 1.0f;

        public float evolutionSpeed = 1.0f;

        [Range(0f, 8f)]
        public float twistIntensity = 1.5f;

        [Range(0f, 1f)]
        public float turbulenceContrast = 0.3f;

        [Range(0f, 1f)]
        public float filamentSharpness = 0.5f;

        [Range(0f, 2f)]
        public float turbulenceWarp = 0.5f;

        [Range(0f, 40f)]
        public float orbitalStretch = 2.0f;

        [Range(0f, 1f)]
        public float edgeFraying = 0f;

        [Header("Plunging Region")]
        [Range(0f, 1f)]
        public float plungingGas = 0.4f;

        [Range(0f, 1f)]
        public float plungeGlow = 0.3f;

        [Header("Hot Spot")]
        [Range(0f, 3f)]
        public float hotSpotStrength = 0f;

        [Range(1f, 5f)]
        public float hotSpotRadius = 1.5f;

        [Range(0.05f, 1f)]
        public float hotSpotSize = 0.25f;

        [Header("Jet")]
        [Range(0f, 5f)]
        public float jetBrightness = 0f;

        [Range(0f, 0.99f)]
        public float jetSpeed = 0.9f;

        [Range(0.05f, 1f)]
        public float jetWidth = 0.3f;

        [Range(5f, 100f)]
        public float jetLength = 40f;

        [Range(0f, 1f)]
        public float jetKnots = 0.5f;

        [Range(0f, 1f)]
        public float jetTurbulence = 0.5f;

        public Color jetColor = new Color(0.7f, 0.8f, 1f);

        [Header("Color")]
        public Color diskColorTint = Color.white;
    }
}
