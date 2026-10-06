using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CannoliCat.Space {
    public class BlackHolePass : ScriptableRenderPass {
        private static readonly int SceneDepthId = Shader.PropertyToID("_SceneDepth");

        private readonly Material material;
        public BlackHoleParams settings;

        public BlackHolePass(Material material) {
            this.material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        private class PassData {
            public Material material;
            public BlackHoleParams settings;
            public TextureHandle sceneColor, sceneDepth;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            var resourceData = frameData.Get<UniversalResourceData>();

            var desc = renderGraph.GetTextureDesc(resourceData.cameraColor);
            desc.name = "BlackHoleColor";
            desc.clearBuffer = false;
            desc.depthBufferBits = DepthBits.None;
            var dest = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("BlackHole", out var data)) {
                data.material = material;
                data.settings = settings;
                data.sceneColor = resourceData.cameraColor;
                data.sceneDepth = resourceData.cameraDepthTexture;

                builder.UseTexture(data.sceneColor);
                builder.UseTexture(data.sceneDepth);
                builder.SetRenderAttachment(dest, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => {
                    d.settings.ApplyToMaterial(d.material);
                    RTHandle depth = d.sceneDepth;
                    d.material.SetTexture(SceneDepthId, depth);
                    Blitter.BlitTexture(ctx.cmd, d.sceneColor, new Vector4(1f, 1f, 0f, 0f), d.material, 0);
                });
            }

            resourceData.cameraColor = dest;
        }
    }
}
