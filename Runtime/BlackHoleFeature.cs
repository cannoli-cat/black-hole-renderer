using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CannoliCat.Space {
    public class BlackHoleFeature : ScriptableRendererFeature {
        [Header("Shaders")]
        public Shader shader;
        
        public ComputeShader computeShader;
        
        [Range(0f, 2f)]
        public float skyIntensity = 0.3f;
        
        [Range(0f, 2f)]
        public float webglSkyMultiplier = 1f;
        
        public bool disableOnWebGL;

        [Header("Black Hole Physics")]
        public float schwarzschildRadius = 2.0f;

        [Range(0f, 0.999f)]
        public float spinParameter = 0.9f;

        [Header("Disk Geometry")]
        public float diskThickness = 0.05f;

        [Tooltip("Outer disk edge as a multiple of Rs. Standard: 10–15.")] [Range(4f, 30f)]
        public float diskOuterRadius = 12f;

        [Header("Disk Physics")]
        public float maxTemperature = 25000f;
        
        public float diskDensity = 1.0f;
        
        [Tooltip("Brightness multiplier for the disk only. Doesn't affect the sky.")] [Range(0f, 20f)]
        public float diskExposure = 1.0f;

        [Tooltip("1 = physical Doppler shift and beaming. 0 = none, like the disk in Interstellar.")] [Range(0f, 1f)]
        public float dopplerStrength = 1.0f;

        [Tooltip("1 = physical temperature falloff. Lower keeps the outer disk hotter, like Interstellar.")] [Range(0f, 1f)]
        public float temperatureFalloff = 1.0f;

        [Header("Turbulence (MRI)")]
        [Range(0.1f, 5f)]
        public float noiseScale = 1.5f;
        
        public float evolutionSpeed = 0.3f;
        
        [Range(0f, 8f)]
        public float twistIntensity = 4.0f;

        [Header("Color")]
        public Color diskColorTint = Color.white;

        private Material material;
        private Texture2D blackbodyLut;
        private BlackHolePass fragmentPass;
        private BlackHoleComputePass computePass;

        private float simulatedTime;
        private int lastTimeFrame = -1;

        public override void Create() {
            fragmentPass = null;
            computePass = null;

            if (blackbodyLut == null) {
                blackbodyLut = BlackbodyLUT.Build();
                blackbodyLut.hideFlags = HideFlags.HideAndDontSave;
            }

            if (shader != null) {
                material = CoreUtils.CreateEngineMaterial(shader);
                fragmentPass = new BlackHolePass(material);
            }

            if (computeShader != null) {
                computePass = new BlackHoleComputePass(computeShader);
            }
        }

        protected override void Dispose(bool disposing) {
            CoreUtils.Destroy(material);
            material = null;
            CoreUtils.Destroy(blackbodyLut);
            blackbodyLut = null;
        }

        private static readonly int SkyboxTexId = Shader.PropertyToID("_Tex");
        private static readonly int SkyboxRotationId = Shader.PropertyToID("_Rotation");
        private static Cubemap blackSky;
        
        private static float GetSkyboxRotation() {
            var skybox = RenderSettings.skybox;
            return skybox != null && skybox.HasFloat(SkyboxRotationId)
                ? skybox.GetFloat(SkyboxRotationId)
                : 0f;
        }
        
        private static Cubemap GetSkyboxCubemap() {
            var skybox = RenderSettings.skybox;
            if (skybox != null && skybox.HasTexture(SkyboxTexId) &&
                skybox.GetTexture(SkyboxTexId) is Cubemap cube)
                return cube;

            if (blackSky == null) {
                blackSky = new Cubemap(1, TextureFormat.RGBA32, false) {
                    hideFlags = HideFlags.HideAndDontSave
                };
                var black = new[] { Color.black };
                for (var face = 0; face < 6; face++)
                    blackSky.SetPixels(black, (CubemapFace)face);
                blackSky.Apply();
            }
            return blackSky;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (BlackHole.Active == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (disableOnWebGL) return;
#endif
            
            var isOpenGL = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                        || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;

            var useCompute = computePass != null && SystemInfo.supportsComputeShaders && !isOpenGL;
            if (!useCompute && fragmentPass == null) return;

            var cam = renderingData.cameraData.camera;

            if (Time.frameCount != lastTimeFrame) {
                simulatedTime += (Application.isPlaying ? Time.deltaTime : 0.016f) * evolutionSpeed;
                lastTimeFrame = Time.frameCount;
            }

            var t = cam.transform;
            var halfV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var bhPos = BlackHole.Active.transform.position;

            var camPos = t.position;
            var camForward = t.forward;
            var camRight = t.right;
            var camUp = t.up;

            var p = new BlackHoleParams {
                camPos = camPos,
                camForward = camForward,
                camRight = camRight,
                camUp = camUp,
                tanHalfFov = new Vector2(halfV * cam.aspect, halfV),
                blackHolePos = bhPos,
                rs = schwarzschildRadius,

                spin = spinParameter * schwarzschildRadius * 0.5f,
                time = simulatedTime,
                noiseScale = noiseScale,
                diskDensity = diskDensity,
                twistIntensity = twistIntensity,
                tempMultiplier = maxTemperature,
                diskThickness = diskThickness,
                diskOuterRadius = diskOuterRadius,
                diskExposure = diskExposure,
                dopplerStrength = dopplerStrength,
                temperatureFalloff = temperatureFalloff,

                escapeRadius = Mathf.Max(80f,
                    Vector3.Distance(camPos, bhPos) + diskOuterRadius * schwarzschildRadius * 2f),
#if UNITY_WEBGL
                skyIntensity = skyIntensity * webglSkyMultiplier,
#else
                skyIntensity = skyIntensity,
#endif
                skyRotation = GetSkyboxRotation(),
                baseColor = diskColorTint,
                skyTexture = GetSkyboxCubemap(),
                blackbodyLut = blackbodyLut
            };

            if (useCompute) {
                computePass.settings = p;
                renderer.EnqueuePass(computePass);
            }
            else {
                fragmentPass.settings = p;
                renderer.EnqueuePass(fragmentPass);
            }
        }
    }
}