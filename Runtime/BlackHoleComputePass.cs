using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CannoliCat.Space {
    public class BlackHoleComputePass : ScriptableRenderPass {
        private static readonly int ResultId = Shader.PropertyToID("Result");
        private static readonly int SceneColorId = Shader.PropertyToID("SceneColor");
        private static readonly int SceneDepthId = Shader.PropertyToID("SceneDepth");
        private static readonly int ScreenWidthId = Shader.PropertyToID("ScreenWidth");
        private static readonly int ScreenHeightId = Shader.PropertyToID("ScreenHeight");
        private static readonly int SkipGeometryId = Shader.PropertyToID("SkipGeometry");
        private static readonly int SceneDepthMatId = Shader.PropertyToID("_SceneDepth");
        private const int CompositePass = 2;

        private readonly ComputeShader cs;
        private readonly Material compositeMaterial;
        public BlackHoleParams settings;

        public BlackHoleComputePass(ComputeShader shader, Material composite) {
            cs = shader;
            compositeMaterial = composite;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        private class PassData {
            public ComputeShader cs;
            public Material material;
            public BlackHoleParams settings;
            public int width, height;
            public bool lowRes;
            public TextureHandle output, sceneColor, sceneDepth;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();
            var cam = cameraData.camera;
            
            if (settings.skyTexture != null)
                cs.SetTexture(0, BlackHoleParams.SkyTex, settings.skyTexture);
            if (settings.blackbodyLut != null)
                cs.SetTexture(0, BlackHoleParams.BlackbodyLutTex, settings.blackbodyLut);
            cs.SetKeyword(new LocalKeyword(cs, BlackHoleParams.KerrKeyword), settings.exactKerr);
            
            var scale = compositeMaterial != null ? Mathf.Clamp(settings.renderScale, 0.25f, 1f) : 1f;
            var lowRes = scale < 0.999f;
            var width = lowRes ? Mathf.Max(1, Mathf.CeilToInt(cam.pixelWidth * scale)) : cam.pixelWidth;
            var height = lowRes ? Mathf.Max(1, Mathf.CeilToInt(cam.pixelHeight * scale)) : cam.pixelHeight;

            var desc = new TextureDesc(width, height) {
                name = "BlackHoleColor",
                enableRandomWrite = true,
                colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                filterMode = FilterMode.Bilinear
            };
            var outputTex = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddComputePass<PassData>("BlackHole Compute", out var data)) {
                data.cs = cs;
                data.settings = settings;
                data.width = width;
                data.height = height;
                data.lowRes = lowRes;
                data.output = outputTex;
                data.sceneColor = resourceData.cameraColor;
                data.sceneDepth = resourceData.cameraDepthTexture;

                builder.UseTexture(outputTex, AccessFlags.Write);
                builder.UseTexture(data.sceneColor);
                builder.UseTexture(data.sceneDepth);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData d, ComputeGraphContext ctx) => {
                    d.settings.ApplyToCompute(ctx.cmd, d.cs);
                    ctx.cmd.SetComputeIntParam(d.cs, ScreenWidthId, d.width);
                    ctx.cmd.SetComputeIntParam(d.cs, ScreenHeightId, d.height);
                    ctx.cmd.SetComputeIntParam(d.cs, SkipGeometryId, d.lowRes ? 1 : 0);
                    ctx.cmd.SetComputeTextureParam(d.cs, 0, ResultId, d.output);
                    ctx.cmd.SetComputeTextureParam(d.cs, 0, SceneColorId, d.sceneColor);
                    ctx.cmd.SetComputeTextureParam(d.cs, 0, SceneDepthId, d.sceneDepth);
                    ctx.cmd.DispatchCompute(d.cs, 0, Mathf.CeilToInt(d.width / 8f), Mathf.CeilToInt(d.height / 8f), 1);
                });
            }
            
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("BlackHole Blit", out var data)) {
                data.output = outputTex;
                data.lowRes = lowRes;
                data.material = compositeMaterial;
                data.sceneDepth = resourceData.cameraDepthTexture;
                builder.UseTexture(outputTex);
                if (lowRes) builder.UseTexture(data.sceneDepth);
                builder.SetRenderAttachment(resourceData.cameraColor, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => {
                    if (d.lowRes) {
                        RTHandle depth = d.sceneDepth;
                        d.material.SetTexture(SceneDepthMatId, depth);
                        Blitter.BlitTexture(ctx.cmd, d.output, new Vector4(1f, 1f, 0f, 0f), d.material, CompositePass);
                    }
                    else {
                        Blitter.BlitTexture(ctx.cmd, d.output, new Vector4(1f, 1f, 0f, 0f), 0, false);
                    }
                });
            }
        }
    }
}
