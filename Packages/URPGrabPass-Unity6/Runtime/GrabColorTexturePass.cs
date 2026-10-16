using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URPGrabPass.Runtime
{
    /// <summary>
    ///     Pass that grabs the color texture of the camera (Render Graph).
    /// </summary>
    public class GrabColorTexturePass : ScriptableRenderPass
    {
        private readonly string _grabbedTextureName;
        private readonly int _grabbedTexturePropertyId;

        public GrabColorTexturePass(GrabTiming timing, string grabbedTextureName)
        {
            renderPassEvent = timing.ToRenderPassEvent();
            _grabbedTextureName = grabbedTextureName;
            _grabbedTexturePropertyId = Shader.PropertyToID(grabbedTextureName);

            // The camera color must be an intermediate texture so that it can be copied.
            requiresIntermediateTexture = true;
        }

        /// <summary>
        ///     Handle of the grabbed texture for the frame being recorded.
        ///     Only valid between RecordRenderGraph calls of the same camera.
        /// </summary>
        public TextureHandle GrabbedTexture { get; private set; }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            GrabbedTexture = TextureHandle.nullHandle;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                return;
            }

            var source = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(source);
            desc.name = _grabbedTextureName;
            desc.clearBuffer = false;
            desc.msaaSamples = MSAASamples.None;
            desc.depthBufferBits = 0;
            var destination = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(nameof(GrabColorTexturePass),
                       out var passData))
            {
                passData.Source = source;

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);
                builder.SetGlobalTextureAfterPass(destination, _grabbedTexturePropertyId);

                // Nothing in the graph consumes the grabbed texture through a handle (shaders read it as a
                // global), so the pass must be kept explicitly.
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }

            GrabbedTexture = destination;
        }

        private class PassData
        {
            public TextureHandle Source;
        }
    }
}
