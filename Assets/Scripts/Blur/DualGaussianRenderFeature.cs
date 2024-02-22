using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualGaussianRenderFeature : ScriptableRendererFeature
{
    [Serializable]
    public class DualGaussianSettings
    {
        [Range(0f, 15.0f)]
        [SerializeField] public float blurRadius = 5.0f;
        [Range(1,8)]
        [SerializeField] public int iteration = 4;
        [Range(1.0f, 10.0f)]
        [SerializeField] public float rtDownScaling = 2.0f;

        [SerializeField] public Shader shader;

        [SerializeField] public ComputeShader compute;

        [SerializeField] public bool useCS = false;
    }

    [SerializeField] public DualGaussianSettings _settings;
    private DualGaussianRenderPass _renderPass;
    public override void Create()
    {
        _renderPass = new DualGaussianRenderPass(_settings.shader, _settings.compute);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _renderPass.Setup(_settings, renderer.cameraColorTarget);
        renderer.EnqueuePass(_renderPass);
    }
}
