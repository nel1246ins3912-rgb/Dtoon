// ============================================================================
//  URP14_RenderFeature.cs
//  ----------------------------------------------------------------------------
//  ScriptableRendererFeature for the URP 14 renderer.
//  Will host the dedicated outline pass and (later) the character shadow pass.
//
//  Renders the DToon outline shader pass. Kept in the URP adapter because
//  URP does not draw extra material passes from the normal opaque forward
//  pass unless a renderer feature requests them explicitly.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DToon.URP14
{
    public class URP14_RenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent outlinePassEvent = RenderPassEvent.AfterRenderingOpaques;
        [SerializeField] private LayerMask layerMask = -1;

        private DToonOutlinePass outlinePass;

        public override void Create()
        {
            outlinePass = new DToonOutlinePass(outlinePassEvent, layerMask);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (outlinePass != null)
            {
                renderer.EnqueuePass(outlinePass);
            }
        }

        private sealed class DToonOutlinePass : ScriptableRenderPass
        {
            private static readonly ShaderTagId OutlineShaderTag = new ShaderTagId("SRPDefaultUnlit");
            private FilteringSettings filteringSettings;
            private readonly ProfilingSampler dtoonProfilingSampler = new ProfilingSampler("DToon Outline");

            public DToonOutlinePass(RenderPassEvent passEvent, LayerMask layerMask)
            {
                renderPassEvent = passEvent;
                filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, dtoonProfilingSampler))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                    DrawingSettings drawingSettings = CreateDrawingSettings(
                        OutlineShaderTag,
                        ref renderingData,
                        sortingCriteria
                    );

                    drawingSettings.perObjectData = PerObjectData.None;
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
