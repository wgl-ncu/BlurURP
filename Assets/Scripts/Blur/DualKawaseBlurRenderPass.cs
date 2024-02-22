using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualKawaseBlurRenderPass : ScriptableRenderPass
{
    private struct Level
    {
        public int up;
        public int down;
    }
    private Material _material;
    private RenderTargetIdentifier _source;
    private float blurRadius;
    private float rtDowmScaling;
    private int iteration;
    private ProfilingSampler _sampler = new ProfilingSampler(PostProcessProfileTag.DualKawaseBlurTag);
    private int blurRadiusId = Shader.PropertyToID("_Offset");
    private int sourceTexId = Shader.PropertyToID("_Source");
    private const int maxTimes = 16;
    private Level[] levels;
    public DualKawaseBlurRenderPass(Shader shader)
    {
        _material = CoreUtils.CreateEngineMaterial(shader);
        levels = new Level[maxTimes];
        for (int i = 0; i < levels.Length; ++i)
        {
            levels[i] = new Level()
            {
                up = Shader.PropertyToID("_BlurMipUp" + i),
                down = Shader.PropertyToID("_BlurMipDown" + i)
            };
        }
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(DualKawaseBlurRenderFeature.DualKawaseBlurSetting setting, RenderTargetIdentifier source)
    {
        blurRadius = setting.blurRadius;
        rtDowmScaling = setting.rtDowmScaling;
        iteration = setting.iteration;
        _source = source;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, _sampler))
        {
            int screenW = renderingData.cameraData.cameraTargetDescriptor.width;
            int screenH = renderingData.cameraData.cameraTargetDescriptor.height;
            int tw = (int)(screenW / rtDowmScaling);
            int th = (int)(screenH / rtDowmScaling);
            cmd.SetGlobalFloat(blurRadiusId, blurRadius);
            var lastDown = _source;
            for (int i = 0; i < iteration; ++i)
            {
                int down = levels[i].down;
                int up = levels[i].up;
                cmd.GetTemporaryRT(down, tw, th, 0, FilterMode.Bilinear);
                cmd.GetTemporaryRT(up, tw, th, 0, FilterMode.Bilinear);
                cmd.SetGlobalTexture(sourceTexId, lastDown);
                Blit(cmd, lastDown, down, _material, 0);
                lastDown = down;
                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }

            var lastup = levels[iteration - 1].down;
            for (int i = iteration - 2; i >= 0; --i)
            {
                int up = levels[i].up;
                cmd.SetGlobalTexture(sourceTexId, lastup);
                Blit(cmd, lastup, up, _material, 1);
                lastup = up;
            }
            Blit(cmd, lastup, _source, _material, 1);
            
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();
    }
}
