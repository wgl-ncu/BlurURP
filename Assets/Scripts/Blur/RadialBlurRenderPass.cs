using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RadialBlurRenderPass : ScriptableRenderPass
{
    private Material m_material;
    private RadialBlurQuality quality;
    private float centerX;
    private float centerY;
    private float blurRadius;
    private RenderTargetIdentifier source;
    private readonly ProfilingSampler sampler = new ProfilingSampler(PostProcessProfileTag.RadialBlurTag);

    private static int Params = Shader.PropertyToID("_Params");
    private static int Params2 = Shader.PropertyToID("_Params2");
    private static int Source = Shader.PropertyToID("_Source");
    private static int temp = Shader.PropertyToID("_Temp");
    public RadialBlurRenderPass(Shader shader)
    {
        m_material = CoreUtils.CreateEngineMaterial(shader);
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, sampler))
        {
            cmd.SetGlobalVector(Params, new Vector4(blurRadius * 0.02f, centerX, centerY));
            cmd.SetGlobalVector(Params2, new Vector4((int)quality, 1 / (float)quality));
            cmd.SetGlobalTexture(Source, source);
            cmd.GetTemporaryRT(temp, renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height);
            Blit(cmd, source, temp, m_material);
            Blit(cmd, temp, source);
            
            
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.ReleaseTemporaryRT(temp);
        cmd.Release();
    }

    public void SetUp(RadialBlurQuality quality, float centerX, float centerY, float blurRadius, RenderTargetIdentifier identifier)
    {
        this.quality = quality;
        this.centerX = centerX;
        this.centerY = centerY;
        this.blurRadius = blurRadius;
        this.source = identifier;
    }
}
