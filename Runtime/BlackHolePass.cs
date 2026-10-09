using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CannoliCat.Space {
    public class BlackHolePass : ScriptableRenderPass {
        private static readonly int SceneDepthId = Shader.PropertyToID("_SceneDepth");

        private const int FullResPass = 0;
        private const int LowResPass = 1;
        private const int CompositePass = 2;

        private readonly Material material;
        public BlackHoleParams settings;

        public BlackHolePass(Material material) {
            this.material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        private class PassData {
            public Material material;
            public BlackHoleParams settings;
            public TextureHandle source, sceneDepth;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            var resourceData = frameData.Get<UniversalResourceData>();
            var scale = Mathf.Clamp(settings.renderScale, 0.25f, 1f);

            var desc = renderGraph.GetTextureDesc(resourceData.cameraColor);
            desc.name = "BlackHoleColor";
            desc.clearBuffer = false;
            desc.depthBufferBits = DepthBits.None;

            if (scale >= 0.999f) {
                var dest = renderGraph.CreateTexture(desc);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("BlackHole", out var data)) {
                    data.material = material;
                    data.settings = settings;
                    data.source = resourceData.cameraColor;
                    data.sceneDepth = resourceData.cameraDepthTexture;

                    builder.UseTexture(data.source);
                    builder.UseTexture(data.sceneDepth);
                    builder.SetRenderAttachment(dest, 0);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => {
                        d.settings.ApplyToMaterial(d.material);
                        RTHandle depth = d.sceneDepth;
                        d.material.SetTexture(SceneDepthId, depth);
                        Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), d.material, FullResPass);
                    });
                }

                resourceData.cameraColor = dest;
                return;
            }

            desc.name = "BlackHoleColorLowRes";
            desc.width = Mathf.Max(1, Mathf.CeilToInt(desc.width * scale));
            desc.height = Mathf.Max(1, Mathf.CeilToInt(desc.height * scale));
            desc.msaaSamples = MSAASamples.None;
            desc.filterMode = FilterMode.Bilinear;
            var lowRes = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("BlackHole Low Res", out var data)) {
                data.material = material;
                data.settings = settings;
                data.source = resourceData.cameraColor;

                builder.UseTexture(data.source);
                builder.SetRenderAttachment(lowRes, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => {
                    d.settings.ApplyToMaterial(d.material);
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), d.material, LowResPass);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("BlackHole Composite", out var data)) {
                data.material = material;
                data.source = lowRes;
                data.sceneDepth = resourceData.cameraDepthTexture;

                builder.UseTexture(data.source);
                builder.UseTexture(data.sceneDepth);
                builder.SetRenderAttachment(resourceData.cameraColor, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => {
                    RTHandle depth = d.sceneDepth;
                    d.material.SetTexture(SceneDepthId, depth);
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), d.material, CompositePass);
                });
            }
        }
    }
}
