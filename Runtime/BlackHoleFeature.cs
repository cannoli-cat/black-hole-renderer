using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CannoliCat.Space {
    public class BlackHoleFeature : ScriptableRendererFeature {
        public enum RayTracingMode { Fast, Exact }
        public enum ObserverMotion { Manual, Orbiting, Falling }

        [Tooltip("Apply copies this preset's values into the settings below. Editing the sliders afterward doesn't change the preset.")]
        public BlackHolePreset preset;

        [Header("Shaders")]
        public Shader shader;
        
        public ComputeShader computeShader;

        [Tooltip("Renders the black hole and sky at a fraction of the screen resolution, then upscales. 0.75 is about 1.8x cheaper, 0.5 about 4x. Scene objects stay at full resolution.")]
        [Range(0.25f, 1f)]
        public float renderScale = 1f;
        
        [Range(0f, 2f)]
        public float skyIntensity = 0.3f;
        
        [Range(0f, 2f)]
        public float webglSkyMultiplier = 1f;
        
        public bool disableOnWebGL;

        [Header("Black Hole Physics")]
        public float schwarzschildRadius = 2.0f;

        [Range(0f, 0.999f)]
        public float spinParameter = 0.9f;

        [Header("Observer")]
        [Tooltip("Manual: use Observer Velocity. Orbiting: a circular orbit at the camera's distance, moving with the disk. Falling: dropped from rest far away, falling straight in. The camera must stay outside the horizon.")]
        public ObserverMotion observerMotion = ObserverMotion.Manual;

        [Tooltip("The camera's velocity as a fraction of the speed of light, in world space, relative to the black hole. Bends the view toward the direction of motion and shifts colors: blue ahead, red behind. Capped at 0.99.")]
        public Vector3 observerVelocity = Vector3.zero;

        [Tooltip("Exact: true Kerr light paths. Fast: Schwarzschild bending with approximate frame dragging, cheaper and recommended for WebGL and mobile.")]
        public RayTracingMode rayTracing = RayTracingMode.Exact;

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

        [Tooltip("How much the turbulence heats and cools the gas. Higher makes brighter hot spots and darker lanes.")] [Range(0f, 1f)]
        public float turbulenceContrast = 0.3f;

        [Tooltip("0 = soft clouds. 1 = sharp, stringy filaments.")] [Range(0f, 1f)]
        public float filamentSharpness = 0.5f;

        [Tooltip("How strongly the turbulence is swirled into eddies.")] [Range(0f, 2f)]
        public float turbulenceWarp = 0.5f;

        [Tooltip("Stretches the turbulence along the orbit into long streaks, like differential rotation shears real gas.")] [Range(0f, 40f)]
        public float orbitalStretch = 2.0f;

        [Tooltip("0 = the disk is equally solid out to its edge. Higher breaks the outer disk into separate wispy strands with gaps between them.")] [Range(0f, 1f)]
        public float edgeFraying = 0f;

        [Header("Plunging Region (inside the ISCO)")]
        [Tooltip("How much gas spirals in from the ISCO to the horizon. 0 = the disk ends sharply at the ISCO.")] [Range(0f, 1f)]
        public float plungingGas = 0.4f;

        [Tooltip("Temperature of the plunging gas, as a fraction of Max Temperature.")] [Range(0f, 1f)]
        public float plungeGlow = 0.3f;

        [Header("Hot Spot")]
        [Tooltip("Brightness of a single hot blob orbiting in the disk, like the flares seen around Sagittarius A*. 0 = off. Its light reaches you along several paths with different delays, so it appears again in the lensed image and the photon ring.")]
        [Range(0f, 3f)]
        public float hotSpotStrength = 0f;

        [Tooltip("Orbit radius of the hot spot, as a multiple of the innermost stable orbit (ISCO).")]
        [Range(1f, 5f)]
        public float hotSpotRadius = 1.5f;

        [Tooltip("Size of the hot spot, as a multiple of the Schwarzschild radius.")]
        [Range(0.05f, 1f)]
        public float hotSpotSize = 0.25f;

        [Header("Jet")]
        [Tooltip("Brightness of the relativistic jets along the spin axis, like M87's. 0 = off. The jet coming toward you is boosted and the one going away is dimmed by relativistic beaming.")]
        [Range(0f, 5f)]
        public float jetBrightness = 0f;

        [Tooltip("Speed of the jet material, as a fraction of the speed of light. Faster jets are more one-sided, and their knots can appear to move faster than light.")]
        [Range(0f, 0.99f)]
        public float jetSpeed = 0.9f;

        [Tooltip("Jet radius one Schwarzschild radius above the hole, as a multiple of the Schwarzschild radius. The jet widens like a parabola further out.")]
        [Range(0.05f, 1f)]
        public float jetWidth = 0.3f;

        [Tooltip("How far the jet reaches, as a multiple of the Schwarzschild radius.")]
        [Range(5f, 100f)]
        public float jetLength = 40f;

        [Tooltip("How strongly the jet breaks into bright knots that travel outward. 0 = smooth.")]
        [Range(0f, 1f)]
        public float jetKnots = 0.5f;

        [Tooltip("How turbulent the jet is: wispy, twisting filaments that stream outward with the flow. 0 = smooth.")]
        [Range(0f, 1f)]
        public float jetTurbulence = 0.5f;

        [Tooltip("Color of the jet's glow. Jets shine by synchrotron light, which is bluish white rather than a blackbody color.")]
        public Color jetColor = new Color(0.7f, 0.8f, 1f);

        [Header("Hot Flow")]
        [Tooltip("Brightness of a hot, puffy, see-through flow around the hole, like M87* and Sgr A* in the Event Horizon Telescope images. 0 = off. Set Disk Density to 0 for the pure EHT look, or keep both for a disk with a hot corona.")]
        [Range(0f, 5f)]
        public float hotFlowBrightness = 0f;

        [Tooltip("Thickness of the flow. Small values hug the disk plane; large values make it nearly spherical.")]
        [Range(0.1f, 2f)]
        public float hotFlowThickness = 0.7f;

        [Tooltip("How fast the glow falls off with distance from the hole. Higher values concentrate the light near the photon ring.")]
        [Range(1f, 5f)]
        public float hotFlowFalloff = 3f;

        [Tooltip("How much the flow absorbs light passing through it, including its own. 0 = fully transparent.")]
        [Range(0f, 1f)]
        public float hotFlowOpacity = 0f;

        [Tooltip("How lumpy the flow is: clumps of gas that orbit with it and slowly churn. 0 = smooth.")]
        [Range(0f, 1f)]
        public float hotFlowTurbulence = 0.6f;

        [Tooltip("Colors the flow by temperature, like the disk: hottest near the hole, whiter where gas moves toward you and redder where it moves away. 0 = use Hot Flow Color instead. The real light is radio, so this shows how the flow would look if it glowed in visible light.")]
        [Range(0f, 20000f)]
        public float hotFlowTemperature = 0f;

        [Tooltip("Color of the flow's glow when Hot Flow Temperature is 0. The real light is radio, so any color is a choice; the EHT images use orange.")]
        public Color hotFlowColor = new Color(1f, 0.5f, 0.15f);

        [Header("Color")]
        public Color diskColorTint = Color.white;

        public void ApplyPreset(BlackHolePreset p) {
            skyIntensity = p.skyIntensity;
            schwarzschildRadius = p.schwarzschildRadius;
            spinParameter = p.spinParameter;
            rayTracing = p.rayTracing;
            diskThickness = p.diskThickness;
            diskOuterRadius = p.diskOuterRadius;
            maxTemperature = p.maxTemperature;
            diskDensity = p.diskDensity;
            diskExposure = p.diskExposure;
            dopplerStrength = p.dopplerStrength;
            temperatureFalloff = p.temperatureFalloff;
            noiseScale = p.noiseScale;
            evolutionSpeed = p.evolutionSpeed;
            twistIntensity = p.twistIntensity;
            turbulenceContrast = p.turbulenceContrast;
            filamentSharpness = p.filamentSharpness;
            turbulenceWarp = p.turbulenceWarp;
            orbitalStretch = p.orbitalStretch;
            edgeFraying = p.edgeFraying;
            plungingGas = p.plungingGas;
            plungeGlow = p.plungeGlow;
            hotSpotStrength = p.hotSpotStrength;
            hotSpotRadius = p.hotSpotRadius;
            hotSpotSize = p.hotSpotSize;
            jetBrightness = p.jetBrightness;
            jetSpeed = p.jetSpeed;
            jetWidth = p.jetWidth;
            jetLength = p.jetLength;
            jetKnots = p.jetKnots;
            jetTurbulence = p.jetTurbulence;
            jetColor = p.jetColor;
            hotFlowBrightness = p.hotFlowBrightness;
            hotFlowThickness = p.hotFlowThickness;
            hotFlowFalloff = p.hotFlowFalloff;
            hotFlowOpacity = p.hotFlowOpacity;
            hotFlowTurbulence = p.hotFlowTurbulence;
            hotFlowTemperature = p.hotFlowTemperature;
            hotFlowColor = p.hotFlowColor;
            diskColorTint = p.diskColorTint;
        }

        public void SaveToPreset(BlackHolePreset p) {
            p.skyIntensity = skyIntensity;
            p.schwarzschildRadius = schwarzschildRadius;
            p.spinParameter = spinParameter;
            p.rayTracing = rayTracing;
            p.diskThickness = diskThickness;
            p.diskOuterRadius = diskOuterRadius;
            p.maxTemperature = maxTemperature;
            p.diskDensity = diskDensity;
            p.diskExposure = diskExposure;
            p.dopplerStrength = dopplerStrength;
            p.temperatureFalloff = temperatureFalloff;
            p.noiseScale = noiseScale;
            p.evolutionSpeed = evolutionSpeed;
            p.twistIntensity = twistIntensity;
            p.turbulenceContrast = turbulenceContrast;
            p.filamentSharpness = filamentSharpness;
            p.turbulenceWarp = turbulenceWarp;
            p.orbitalStretch = orbitalStretch;
            p.edgeFraying = edgeFraying;
            p.plungingGas = plungingGas;
            p.plungeGlow = plungeGlow;
            p.hotSpotStrength = hotSpotStrength;
            p.hotSpotRadius = hotSpotRadius;
            p.hotSpotSize = hotSpotSize;
            p.jetBrightness = jetBrightness;
            p.jetSpeed = jetSpeed;
            p.jetWidth = jetWidth;
            p.jetLength = jetLength;
            p.jetKnots = jetKnots;
            p.jetTurbulence = jetTurbulence;
            p.jetColor = jetColor;
            p.hotFlowBrightness = hotFlowBrightness;
            p.hotFlowThickness = hotFlowThickness;
            p.hotFlowFalloff = hotFlowFalloff;
            p.hotFlowOpacity = hotFlowOpacity;
            p.hotFlowTurbulence = hotFlowTurbulence;
            p.hotFlowTemperature = hotFlowTemperature;
            p.hotFlowColor = hotFlowColor;
            p.diskColorTint = diskColorTint;
        }

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
                computePass = new BlackHoleComputePass(computeShader, material);
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

            BlackHole.Active.SchwarzschildRadius = schwarzschildRadius;
            BlackHole.Active.Spin = spinParameter;
            BlackHole.Active.SimulatedTime = simulatedTime;

            var motion = observerMotion;
            var exact = rayTracing == RayTracingMode.Exact;
            if (Application.isPlaying) {
                if (cam.TryGetComponent(out BlackHoleFallCamera fall) && fall.isActiveAndEnabled) {
                    motion = ObserverMotion.Falling;
                    exact = true;
                }
                else if (cam.TryGetComponent(out BlackHoleOrbitCamera orbit) && orbit.isActiveAndEnabled)
                    motion = ObserverMotion.Orbiting;
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
                pixelAngle = 2f * halfV / Mathf.Max(cam.pixelHeight * renderScale, 1f),
                renderScale = renderScale,
                blackHolePos = bhPos,
                rs = schwarzschildRadius,

                spin = spinParameter * schwarzschildRadius * 0.5f,
                exactKerr = exact,
                observerVelocity = Vector3.ClampMagnitude(observerVelocity, 0.99f),
                observerMode = (int)motion,
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
                turbulenceContrast = turbulenceContrast,
                filamentSharpness = filamentSharpness,
                turbulenceWarp = turbulenceWarp,
                orbitalStretch = orbitalStretch,
                edgeFraying = edgeFraying,
                hotSpot = new Vector4(hotSpotStrength, hotSpotRadius, hotSpotSize, 0f),
                jet = new Vector4(jetBrightness, jetSpeed, jetWidth, jetLength),
                jetKnots = jetKnots,
                jetTurbulence = jetTurbulence,
                jetColor = jetColor,
                hotFlow = new Vector4(hotFlowBrightness, hotFlowThickness, hotFlowFalloff, hotFlowOpacity),
                hotFlowTurbulence = hotFlowTurbulence,
                hotFlowTemperature = hotFlowTemperature,
                hotFlowColor = hotFlowColor,
                plungingGas = plungingGas,
                plungeGlow = plungeGlow,
                
                escapeRadius = Mathf.Max(80f,
                    Vector3.Distance(camPos, bhPos) + schwarzschildRadius *
                    Mathf.Max(diskOuterRadius * 2f, jetBrightness > 0f ? jetLength * 1.2f : 0f)),
#if UNITY_WEBGL
                skyIntensity = skyIntensity * webglSkyMultiplier,
#else
                skyIntensity = skyIntensity,
#endif
                skyRotation = GetSkyboxRotation(),
                baseColor = diskColorTint,
                skyTexture = GetSkyboxCubemap(),
                blackbodyLut = blackbodyLut,
                noiseTexture = NoiseTexture.Get()
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