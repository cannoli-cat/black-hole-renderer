using UnityEngine;
using UnityEngine.Rendering;

namespace CannoliCat.Space {
    public struct BlackHoleParams {
        public Vector3 camPos, camForward, camRight, camUp;
        public Vector2 tanHalfFov;
        public Vector3 blackHolePos;
        public float rs, spin, time, noiseScale, diskDensity, evolutionSpeed, twistIntensity;
        public float tempMultiplier, diskThickness, diskOuterRadius, beamingPower, escapeRadius, skyIntensity;
        public float skyRotation;
        public Color baseColor;
        public Cubemap skyTexture;

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
        private static readonly int EvolutionSpeed = Shader.PropertyToID("_EvolutionSpeed");
        private static readonly int TwistIntensity = Shader.PropertyToID("_TwistIntensity");
        private static readonly int TempMultiplier = Shader.PropertyToID("_TempMultiplier");
        private static readonly int DiskThickness = Shader.PropertyToID("_DiskThickness");
        private static readonly int DiskOuterRadius = Shader.PropertyToID("_DiskOuterRadius");
        private static readonly int BeamingPower = Shader.PropertyToID("_BeamingPower");
        private static readonly int EscapeRadius = Shader.PropertyToID("_EscapeRadius");
        private static readonly int SkyIntensity = Shader.PropertyToID("_SkyIntensity");
        private static readonly int SkyRotation = Shader.PropertyToID("_SkyRotation");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int SkyTex = Shader.PropertyToID("_SkyTex");

        public void ApplyToMaterial(Material m) {
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
            m.SetFloat(EvolutionSpeed, evolutionSpeed);
            m.SetFloat(TwistIntensity, twistIntensity);
            m.SetFloat(TempMultiplier, tempMultiplier);
            m.SetFloat(DiskThickness, diskThickness);
            m.SetFloat(DiskOuterRadius, diskOuterRadius);
            m.SetFloat(BeamingPower, beamingPower);
            m.SetFloat(EscapeRadius, escapeRadius);
            m.SetFloat(SkyIntensity, skyIntensity);
            m.SetFloat(SkyRotation, skyRotation);
            m.SetColor(BaseColor, baseColor);
            if (skyTexture != null) m.SetTexture(SkyTex, skyTexture);
        }

        public void ApplyToCompute(ComputeCommandBuffer cmd, ComputeShader cs, int kernel) {
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
            cmd.SetComputeFloatParam(cs, EvolutionSpeed, evolutionSpeed);
            cmd.SetComputeFloatParam(cs, TwistIntensity, twistIntensity);
            cmd.SetComputeFloatParam(cs, TempMultiplier, tempMultiplier);
            cmd.SetComputeFloatParam(cs, DiskThickness, diskThickness);
            cmd.SetComputeFloatParam(cs, DiskOuterRadius, diskOuterRadius);
            cmd.SetComputeFloatParam(cs, BeamingPower, beamingPower);
            cmd.SetComputeFloatParam(cs, EscapeRadius, escapeRadius);
            cmd.SetComputeFloatParam(cs, SkyIntensity, skyIntensity);
            cmd.SetComputeFloatParam(cs, SkyRotation, skyRotation);
            cmd.SetComputeVectorParam(cs, BaseColor, baseColor);
        }
    }
}
