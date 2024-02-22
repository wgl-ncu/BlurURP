using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TiltShiftBlurRenderPass : ScriptableRenderPass
{
    private Material _material;
    private TiltShiftBlurFeature.TiltShiftBlurSettings _settings;
    private ProfilingSampler _profilingSampler = new ProfilingSampler(PostProcessProfileTag.TiltShiftBlurTag);

    private int GoldenRotId = Shader.PropertyToID("_GoldenRot");
    private int GradientId = Shader.PropertyToID("_Gradient");
    private int ParamsId = Shader.PropertyToID("_Params");
    private int tempId = Shader.PropertyToID("_Temp");
    private int Source = Shader.PropertyToID("_Source");
    private Vector4 goldenRot;
    private RenderTargetIdentifier _source;
    public TiltShiftBlurRenderPass(Shader _shader)
    {
        _material = CoreUtils.CreateEngineMaterial(_shader);
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        float c = Mathf.Cos(2.39996323f);
        float s = Mathf.Sin(2.39996323f);
        goldenRot = new Vector4(c, s, -s, c);
    }

    public void Setup(TiltShiftBlurFeature.TiltShiftBlurSettings settings, RenderTargetIdentifier source)
    {
        _settings = settings;
        _source = source;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, _profilingSampler))
        {
            cmd.SetGlobalTexture(Source, _source);
            _material.SetVector(GoldenRotId, goldenRot);
            Vector3 gradient = new Vector3(_settings.centerOffset, _settings.areaSize, _settings.areaSmooth);
            _material.SetVector(GradientId, gradient);
            int tw = renderingData.cameraData.cameraTargetDescriptor.width;
            int th = renderingData.cameraData.cameraTargetDescriptor.height;
            Vector4 _params = new Vector4(_settings.iteration,_settings.blurRaius, 1f / tw, 1f / th);
            _material.SetVector(ParamsId, _params);
            cmd.GetTemporaryRT(tempId, tw, th);
            Blit(cmd, _source, tempId, _material, _settings.showPriview ? 1 : 0);
            Blit(cmd, tempId, _source);
            cmd.ReleaseTemporaryRT(tempId);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();
    }
}
