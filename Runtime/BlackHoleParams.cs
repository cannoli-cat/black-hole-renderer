using UnityEngine;
using UnityEngine.Rendering;

namespace CannoliCat.Space {
    public struct BlackHoleParams {
        public Vector3 camPos, camForward, camRight, camUp;
        public Vector2 tanHalfFov;
        public Vector3 blackHolePos;
        public float rs, spin, time, noiseScale, diskDensity, twistIntensity;
        public float tempMultiplier, diskThickness, diskOuterRadius, diskExposure, dopplerStrength, temperatureFalloff, turbulenceContrast, filamentSharpness, turbulenceWarp, orbitalStretch, edgeFraying, plungingGas, plungeGlow, escapeRadius, skyIntensity;
        public float skyRotation;
        public Color baseColor;
        public bool exactKerr;
        public Cubemap skyTexture;
        public Texture2D blackbodyLut;

        private static readonly int CamPos = Shader.PropertyToID("_CamPos");
        private static readonly int CamForward = Shader.PropertyToID("_CamForward");
        private static readonly int CamRight = Shader.PropertyToID("_CamRight");
        private static readonly int CamUp = Shader.PropertyToID("_CamUp");
        private static readonly int TanHalfFov = Shader.PropertyToID("_TanHalfFov");
        private static readonly int BlackHolePos = Shader.PropertyToID("_BlackHolePos");
        private static readonly int Rs = Shader.PropertyToID("_Rs");
        private static readonly int Spin = Shader.PropertyToID("_Spin");
        private static readonly int Time = Shader.PropertyToID("_BHTime");
        private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
        private static readonly int DiskDensity = Shader.PropertyToID("_DiskDensity");
        private static readonly int TwistIntensity = Shader.PropertyToID("_TwistIntensity");
        private static readonly int TempMultiplier = Shader.PropertyToID("_TempMultiplier");
        private static readonly int DiskThickness = Shader.PropertyToID("_DiskThickness");
        private static readonly int DiskOuterRadius = Shader.PropertyToID("_DiskOuterRadius");
        private static readonly int DiskExposure = Shader.PropertyToID("_DiskExposure");
        private static readonly int DopplerStrength = Shader.PropertyToID("_DopplerStrength");
        private static readonly int TemperatureFalloff = Shader.PropertyToID("_TemperatureFalloff");
        private static readonly int TurbulenceContrast = Shader.PropertyToID("_TurbulenceContrast");
        private static readonly int FilamentSharpness = Shader.PropertyToID("_FilamentSharpness");
        private static readonly int TurbulenceWarp = Shader.PropertyToID("_TurbulenceWarp");
        private static readonly int OrbitalStretch = Shader.PropertyToID("_OrbitalStretch");
        private static readonly int EdgeFraying = Shader.PropertyToID("_EdgeFraying");
        private static readonly int PlungingGas = Shader.PropertyToID("_PlungingGas");
        private static readonly int PlungeGlow = Shader.PropertyToID("_PlungeGlow");
        private static readonly int EscapeRadius =Shader.PropertyToID("_EscapeRadius");
        private static readonly int SkyIntensity = Shader.PropertyToID("_SkyIntensity");
        private static readonly int SkyRotation = Shader.PropertyToID("_SkyRotation");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BlackbodyRange = Shader.PropertyToID("_BlackbodyRange");
        public static readonly int SkyTex = Shader.PropertyToID("_SkyTex");
        public static readonly int BlackbodyLutTex = Shader.PropertyToID("_BlackbodyLUT");
        public const string KerrKeyword = "BH_KERR";

        private static readonly Vector4 BlackbodyRangeValue =
            new((float)BlackbodyLUT.MinKelvin, (float)BlackbodyLUT.MaxKelvin, 0f, 0f);

        public void ApplyToMaterial(Material m) {
            m.SetKeyword(new LocalKeyword(m.shader, KerrKeyword), exactKerr);
            m.SetVector(CamPos, camPos);
            m.SetVector(CamForward, camForward);
            m.SetVector(CamRight, camRight);
            m.SetVector(CamUp, camUp);
            m.SetVector(TanHalfFov, tanHalfFov);
            m.SetVector(BlackHolePos, blackHolePos);
            m.SetFloat(Rs, rs);
            m.SetFloat(Spin, spin);
            m.SetFloat(Time, time);
            m.SetFloat(NoiseScale, noiseScale);
            m.SetFloat(DiskDensity, diskDensity);
            m.SetFloat(TwistIntensity, twistIntensity);
            m.SetFloat(TempMultiplier, tempMultiplier);
            m.SetFloat(DiskThickness, diskThickness);
            m.SetFloat(DiskOuterRadius, diskOuterRadius);
            m.SetFloat(DiskExposure, diskExposure);
            m.SetFloat(DopplerStrength, dopplerStrength);
            m.SetFloat(TemperatureFalloff, temperatureFalloff);
            m.SetFloat(TurbulenceContrast, turbulenceContrast);
            m.SetFloat(FilamentSharpness, filamentSharpness);
            m.SetFloat(TurbulenceWarp, turbulenceWarp);
            m.SetFloat(OrbitalStretch, orbitalStretch);
            m.SetFloat(EdgeFraying, edgeFraying);
            m.SetFloat(PlungingGas, plungingGas);
            m.SetFloat(PlungeGlow, plungeGlow);
            m.SetFloat(EscapeRadius, escapeRadius);
            m.SetFloat(SkyIntensity, skyIntensity);
            m.SetFloat(SkyRotation, skyRotation);
            m.SetColor(BaseColor, baseColor);
            m.SetVector(BlackbodyRange, BlackbodyRangeValue);
            if (skyTexture != null) m.SetTexture(SkyTex, skyTexture);
            if (blackbodyLut != null) m.SetTexture(BlackbodyLutTex, blackbodyLut);
        }

        public void ApplyToCompute(ComputeCommandBuffer cmd, ComputeShader cs) {
            cmd.SetComputeVectorParam(cs, CamPos, camPos);
            cmd.SetComputeVectorParam(cs, CamForward, camForward);
            cmd.SetComputeVectorParam(cs, CamRight, camRight);
            cmd.SetComputeVectorParam(cs, CamUp, camUp);
            cmd.SetComputeVectorParam(cs, TanHalfFov, tanHalfFov);
            cmd.SetComputeVectorParam(cs, BlackHolePos, blackHolePos);
            cmd.SetComputeFloatParam(cs, Rs, rs);
            cmd.SetComputeFloatParam(cs, Spin, spin);
            cmd.SetComputeFloatParam(cs, Time, time);
            cmd.SetComputeFloatParam(cs, NoiseScale, noiseScale);
            cmd.SetComputeFloatParam(cs, DiskDensity, diskDensity);
            cmd.SetComputeFloatParam(cs, TwistIntensity, twistIntensity);
            cmd.SetComputeFloatParam(cs, TempMultiplier, tempMultiplier);
            cmd.SetComputeFloatParam(cs, DiskThickness, diskThickness);
            cmd.SetComputeFloatParam(cs, DiskOuterRadius, diskOuterRadius);
            cmd.SetComputeFloatParam(cs, DiskExposure, diskExposure);
            cmd.SetComputeFloatParam(cs, DopplerStrength, dopplerStrength);
            cmd.SetComputeFloatParam(cs, TemperatureFalloff, temperatureFalloff);
            cmd.SetComputeFloatParam(cs, TurbulenceContrast, turbulenceContrast);
            cmd.SetComputeFloatParam(cs, FilamentSharpness, filamentSharpness);
            cmd.SetComputeFloatParam(cs, TurbulenceWarp, turbulenceWarp);
            cmd.SetComputeFloatParam(cs, OrbitalStretch, orbitalStretch);
            cmd.SetComputeFloatParam(cs, EdgeFraying, edgeFraying);
            cmd.SetComputeFloatParam(cs, PlungingGas, plungingGas);
            cmd.SetComputeFloatParam(cs, PlungeGlow, plungeGlow);
            cmd.SetComputeFloatParam(cs, EscapeRadius, escapeRadius);
            cmd.SetComputeFloatParam(cs, SkyIntensity, skyIntensity);
            cmd.SetComputeFloatParam(cs, SkyRotation, skyRotation);
            cmd.SetComputeVectorParam(cs, BaseColor, baseColor);
            cmd.SetComputeVectorParam(cs, BlackbodyRange, BlackbodyRangeValue);
        }
    }
}
