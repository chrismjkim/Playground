using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class OutlineAfterPostProcessFeature : ScriptableRendererFeature {
  private class OutlineAfterPostProcessPass : ScriptableRenderPass {
    private RTHandle colorTarget;
    private RTHandle depthTarget;

    public OutlineAfterPostProcessPass() {
      profilingSampler = new ProfilingSampler("QuickOutline After Post Processing");
      ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public void Setup(RTHandle colorTargetHandle, RTHandle depthTargetHandle) {
      colorTarget = colorTargetHandle;
      depthTarget = depthTargetHandle;
    }

  #if !UNITY_6000_0_OR_NEWER || URP_COMPATIBILITY_MODE
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
      if (colorTarget != null && depthTarget != null) {
        ConfigureTarget(colorTarget, depthTarget);
      }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
      var outlines = OutlinePP.ActiveOutlines;
      if (outlines == null || outlines.Count == 0) {
        return;
      }

      var cmd = CommandBufferPool.Get();
      using (new ProfilingScope(cmd, profilingSampler)) {
        // 1) Mask pass (stencil write)
        for (var i = 0; i < outlines.Count; i++) {
          var outline = outlines[i];
          if (!IsValidOutline(outline)) {
            continue;
          }

          DrawOutlineMaterials(cmd, outline, outline.OutlineMaskMaterial);
        }

        // 2) Fill pass (stencil test)
        for (var i = 0; i < outlines.Count; i++) {
          var outline = outlines[i];
          if (!IsValidOutline(outline)) {
            continue;
          }

          DrawOutlineMaterials(cmd, outline, outline.OutlineFillMaterial);
        }
      }

      context.ExecuteCommandBuffer(cmd);
      CommandBufferPool.Release(cmd);
    }
  #endif

#if UNITY_6000_0_OR_NEWER
    private class PassData {
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
      var outlines = OutlinePP.ActiveOutlines;
      if (outlines == null || outlines.Count == 0) {
        return;
      }

      var resourceData = frameData.Get<UniversalResourceData>();

      using var builder = renderGraph.AddRasterRenderPass<PassData>("QuickOutline After Post Processing", out _);
      builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
      builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
      builder.AllowPassCulling(false);

      builder.SetRenderFunc((PassData _, RasterGraphContext context) => {
        // 1) Mask pass (stencil write)
        for (var i = 0; i < outlines.Count; i++) {
          var outline = outlines[i];
          if (!IsValidOutline(outline)) {
            continue;
          }

          DrawOutlineMaterials(context.cmd, outline, outline.OutlineMaskMaterial);
        }

        // 2) Fill pass (stencil test)
        for (var i = 0; i < outlines.Count; i++) {
          var outline = outlines[i];
          if (!IsValidOutline(outline)) {
            continue;
          }

          DrawOutlineMaterials(context.cmd, outline, outline.OutlineFillMaterial);
        }
      });
    }
#endif

    private static bool IsValidOutline(OutlinePP outline) {
      return outline != null
        && outline.isActiveAndEnabled
        && outline.Renderers != null
        && outline.OutlineMaskMaterial != null
        && outline.OutlineFillMaterial != null;
    }

    private static void DrawOutlineMaterials(CommandBuffer cmd, OutlinePP outline, Material material) {
      if (material == null) {
        return;
      }

      var renderers = outline.Renderers;
      for (var r = 0; r < renderers.Length; r++) {
        var renderer = renderers[r];
        if (renderer == null || !renderer.enabled) {
          continue;
        }

        var submeshIndex = GetOutlineSubmeshIndex(renderer);
        if (submeshIndex < 0) {
          continue;
        }

        cmd.DrawRenderer(renderer, material, submeshIndex, 0);
      }
    }

#if UNITY_6000_0_OR_NEWER
    private static void DrawOutlineMaterials(RasterCommandBuffer cmd, OutlinePP outline, Material material) {
      if (material == null) {
        return;
      }

      var renderers = outline.Renderers;
      for (var r = 0; r < renderers.Length; r++) {
        var renderer = renderers[r];
        if (renderer == null || !renderer.enabled) {
          continue;
        }

        var submeshIndex = GetOutlineSubmeshIndex(renderer);
        if (submeshIndex < 0) {
          continue;
        }

        cmd.DrawRenderer(renderer, material, submeshIndex, 0);
      }
    }
#endif

    private static int GetOutlineSubmeshIndex(Renderer renderer) {
      Mesh mesh = null;
      if (renderer is SkinnedMeshRenderer skinned) {
        mesh = skinned.sharedMesh;
      } else {
        var filter = renderer.GetComponent<MeshFilter>();
        if (filter != null) {
          mesh = filter.sharedMesh;
        }
      }

      if (mesh == null || mesh.subMeshCount == 0) {
        return -1;
      }

      return mesh.subMeshCount - 1;
    }
  }

  private OutlineAfterPostProcessPass pass;

  public override void Create() {
    pass = new OutlineAfterPostProcessPass {
      renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
    };
  }

  #pragma warning disable 618
  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    if (pass == null) {
      return;
    }

    if (renderingData.cameraData.cameraType == CameraType.Preview
      || renderingData.cameraData.cameraType == CameraType.Reflection) {
      return;
    }

    pass.Setup(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
    renderer.EnqueuePass(pass);
  }
  #pragma warning restore 618
}
