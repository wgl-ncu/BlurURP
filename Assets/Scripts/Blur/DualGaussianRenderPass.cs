using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualGaussianRenderPass : ScriptableRenderPass
{
    private Material _material;
    private ComputeShader computeShader;
    private float _blurRadius;
    private int _iteration;
    private float _rtDownScaling;
    private SamplerIDs[] _samplerIDsGroup;
    private int _maxSamplerGroupSize = 16;
    private RenderTargetIdentifier _source;

    private bool _useCS;

    private readonly int blurOffsetId = Shader.PropertyToID("_BlurOffset");
    private readonly int sourceTexId = Shader.PropertyToID("_Source");
    private ProfilingSampler _sampler = new ProfilingSampler(PostProcessProfileTag.DualGaussianBlurTag);

    private readonly string kernelHorizotnal = "GaussianBlurHorizontal";
    private readonly string kernelVertical = "GaussianBlurVertical";
    private readonly string cbInputBufferName = "Source";
    private readonly string cbOutputBufferName = "Result";
    private readonly string cdBlurRadius = "blurRadius";
    private readonly string cbTexSize = "textureSize";
    private readonly int cbTempBuffer1 = Shader.PropertyToID("_CBTempBuffer1");
    private readonly int cbTempBuffer2 = Shader.PropertyToID("_CBTempBuffer2");
    public DualGaussianRenderPass(Shader shader, ComputeShader compute)
    {
        _material = CoreUtils.CreateEngineMaterial(shader);
        computeShader = compute;
        _samplerIDsGroup = new SamplerIDs[_maxSamplerGroupSize];
        for (int i = 0; i < _maxSamplerGroupSize; ++i)
        {
            _samplerIDsGroup[i] = new SamplerIDs()
            {
                downVertical = Shader.PropertyToID("_GaussianBlurDownV_" + i),
                downHorizontal = Shader.PropertyToID("_GaussianBlurDownH_" + i),
                upVertical =  Shader.PropertyToID("_GaussianBlurUpV_" + i),
                upHorizontal = Shader.PropertyToID("_GaussianBlurUpH_" +i)
            };
        }

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(DualGaussianRenderFeature.DualGaussianSettings settings, RenderTargetIdentifier source)
    {
        _blurRadius = settings.blurRadius;
        _iteration = settings.iteration;
        _rtDownScaling = settings.rtDownScaling;
        _useCS = settings.useCS;
        _source = source;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, _sampler))
        {
            if (!_useCS)
            {
                DualGaussianCommon(cmd, ref renderingData);
            }
            else
            {
                DualGaussianCB(cmd, ref renderingData);
            }
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();
    }

    private void DualGaussianCommon(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var screenW = renderingData.cameraData.cameraTargetDescriptor.width;
        var screenH = renderingData.cameraData.cameraTargetDescriptor.height;
        int tw = (int)(screenW / _rtDownScaling);
        int th = (int)(screenH / _rtDownScaling);
        Vector4 blurOffset = new Vector4(_blurRadius / screenW, _blurRadius / screenH, 0, 0);
        cmd.SetGlobalVector(blurOffsetId, blurOffset);
        RenderTargetIdentifier lastDown = _source;
        cmd.SetGlobalTexture(sourceTexId, _source);
        //降采样
        for (int i = 0; i < _iteration; ++i)
        {
            int downSampleV = _samplerIDsGroup[i].downVertical;
            int downSampleH = _samplerIDsGroup[i].downHorizontal;
            int upSampleV = _samplerIDsGroup[i].upVertical;
            int upSampleH = _samplerIDsGroup[i].upHorizontal;
            cmd.GetTemporaryRT(downSampleV, tw, th, 0, FilterMode.Bilinear);
            cmd.GetTemporaryRT(downSampleH, tw, th, 0, FilterMode.Bilinear);
            cmd.GetTemporaryRT(upSampleH, tw, th, 0, FilterMode.Bilinear);
            cmd.GetTemporaryRT(upSampleV, tw, th, 0, FilterMode.Bilinear);

            cmd.SetGlobalTexture(sourceTexId, lastDown);
            cmd.SetGlobalVector(blurOffsetId, new Vector4(_blurRadius / screenW, 0));
            Blit(cmd, lastDown, downSampleH, _material, 0);
            cmd.SetGlobalTexture(sourceTexId, downSampleH);
            cmd.SetGlobalVector(blurOffsetId, new Vector4(0, _blurRadius / screenH));
            Blit(cmd, downSampleH, downSampleV, _material, 0);

            lastDown = downSampleV;
            tw = Mathf.Max(tw / 2, 1);
            th = Mathf.Max(th / 2, 1);
        }
        //升采样
        int lastUp = _samplerIDsGroup[_iteration - 1].downVertical;
        for (int i = _iteration - 2; i >= 0; --i)
        {
            int upSampleV = _samplerIDsGroup[i].upVertical;
            int upSampleH = _samplerIDsGroup[i].upHorizontal;
            cmd.SetGlobalTexture(sourceTexId, lastUp);
            cmd.SetGlobalVector(blurOffsetId, new Vector4(_blurRadius / screenW, 0));
            Blit(cmd, lastUp, upSampleH, _material, 0);

            cmd.SetGlobalTexture(sourceTexId, upSampleH);
            cmd.SetGlobalVector(blurOffsetId, new Vector4(0, _blurRadius / screenH));
            Blit(cmd, upSampleH, upSampleV, _material, 0);

            lastUp = upSampleV;
        }

        Blit(cmd, lastUp, _source, _material, 1);

        for (int i = 0; i < _iteration; ++i)
        {
            if (_samplerIDsGroup[i].downVertical != lastUp)
            {
                cmd.ReleaseTemporaryRT(_samplerIDsGroup[i].downVertical);
            }
            if (_samplerIDsGroup[i].downHorizontal != lastUp)
            {
                cmd.ReleaseTemporaryRT(_samplerIDsGroup[i].downHorizontal);
            }
            if (_samplerIDsGroup[i].upVertical != lastUp)
            {
                cmd.ReleaseTemporaryRT(_samplerIDsGroup[i].upVertical);
            }
            if (_samplerIDsGroup[i].upHorizontal != lastUp)
            {
                cmd.ReleaseTemporaryRT(_samplerIDsGroup[i].upHorizontal);
            }
        }
    }

    private void DualGaussianCB(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var screenW = renderingData.cameraData.cameraTargetDescriptor.width;
        var screenH = renderingData.cameraData.cameraTargetDescriptor.height;
        cmd.GetTemporaryRT(cbTempBuffer1, screenW, screenH, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, true);
        cmd.GetTemporaryRT(cbTempBuffer2, screenW, screenH, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, true);

        var textureSize = new Vector4(screenW, screenH, 1f / screenW, 1f / screenH);

        Blit(cmd, _source, cbTempBuffer2);
        for (int i = 0; i < _iteration; ++i)
        {
            DoGaussianHorizontal(cmd, cbTempBuffer2, cbTempBuffer1, textureSize);
            DoGussianVertical(cmd, cbTempBuffer1, cbTempBuffer2, textureSize);
        }
        Blit(cmd, cbTempBuffer2, _source);
        cmd.ReleaseTemporaryRT(cbTempBuffer1);
        cmd.ReleaseTemporaryRT(cbTempBuffer2);
    }

    private void DoGaussianHorizontal(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier tar, Vector4 textureSize)
    {
        _DoGaussionCB(kernelHorizotnal, cmd, src, tar, textureSize);
    }

    private void DoGussianVertical(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier tar, Vector4 textureSize)
    {
        _DoGaussionCB(kernelVertical, cmd, src, tar, textureSize);
    }

    private void _DoGaussionCB(string kernelStr,CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier tar, Vector4 textureSize)
    {
        var kernel = computeShader.FindKernel(kernelStr);
        computeShader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
        cmd.SetComputeTextureParam(computeShader, kernel, cbInputBufferName, src);
        cmd.SetComputeTextureParam(computeShader, kernel, cbOutputBufferName, tar);
        cmd.SetComputeFloatParam(computeShader, cdBlurRadius, _blurRadius);
        cmd.SetComputeVectorParam(computeShader, cbTexSize, textureSize);
        cmd.DispatchCompute(computeShader, kernel, Mathf.CeilToInt((float)textureSize.x / x), Mathf.CeilToInt((float)textureSize.y / y), 1);

    }



    private class SamplerIDs
    {
        public int downVertical;
        public int downHorizontal;
        public int upVertical;
        public int upHorizontal;
    }
}
