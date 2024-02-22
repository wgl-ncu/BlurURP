using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public enum RadialBlurQuality
{
    RadialBlur_4Tap_Fatest = 4,
    RadialBlur_6Tap = 6,
    RadialBlur_8Tap_Balance = 8,
    RadialBlur_10Tap = 10,
    RadialBlur_12Tap = 12,
    RadialBlur_20Tap_Quality = 20,
    RadialBlur_30Tap_Extreme = 30,
}
public class RadialBlurRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private RadialBlurQuality quality = RadialBlurQuality.RadialBlur_4Tap_Fatest;
    [Range(0f,1f)]
    [SerializeField] private float centerX = 0.5f;
    [Range(0f,1f)]
    [SerializeField] private float centerY = 0.5f;
    [Range(-1f,1f)]
    [SerializeField] private float blurRadius = 0.0f;
    [SerializeField] private Shader shader;
    private RadialBlurRenderPass m_renderPass;
    public override void Create()
    {
        m_renderPass = new RadialBlurRenderPass(shader);
    }
    

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_renderPass.SetUp(quality, centerX, centerY, blurRadius, renderer.cameraColorTarget);
        renderer.EnqueuePass(m_renderPass);
    }
}
