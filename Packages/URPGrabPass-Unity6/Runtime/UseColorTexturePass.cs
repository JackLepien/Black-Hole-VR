using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URPGrabPass.Runtime
{
    /// <summary>
    ///     Pass to render objects that use grabbed texture (Render Graph).
    /// </summary>
    public class UseColorTexturePass : ScriptableRenderPass
    {
        private readonly GrabColorTexturePass _grabPass;
        private readonly List<ShaderTagId> _shaderTagIds;
        private readonly SortingCriteria _sortingCriteria;

        private FilteringSettings _filteringSettings;

        public UseColorTexturePass(GrabTiming timing, IEnumerable<string> shaderLightModes,
            SortingCriteria sortingCriteria, GrabColorTexturePass grabPass)
        {
            _filteringSettings = new FilteringSettings(RenderQueueRange.all);

            // Add 1 for processing after GrabColorTexturePass.
            renderPassEvent = timing.ToRenderPassEvent() + 1;

            _shaderTagIds = new List<ShaderTagId>();
            foreach (var shaderLightMode in shaderLightModes)
            {
                _shaderTagIds.Add(new ShaderTagId(shaderLightMode));
            }

            _sortingCriteria = sortingCriteria;
            _grabPass = grabPass;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // The grab pass did not run (e.g. rendering directly to the back buffer).
            if (!_grabPass.GrabbedTexture.IsValid())
            {
                return;
            }

            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(nameof(UseColorTexturePass),
                       out var passData))
            {
                // Render objects with specified LightModes.
                var drawingSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, renderingData,
                    cameraData, lightData, _sortingCriteria);
                var rendererListParams =
                    new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
                passData.RendererList = renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.RendererList);

                // Declares the dependency on the grabbed copy (shaders sample it as a global texture).
                builder.UseTexture(_grabPass.GrabbedTexture);

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.RendererList);
                });
            }
        }

        private class PassData
        {
            public RendererListHandle RendererList;
        }
    }
}
