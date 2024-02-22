using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualKawaseBlurRenderFeature : ScriptableRendererFeature
{
    [Serializable]
    public class DualKawaseBlurSetting
    {
        [Range(0.0f, 15.0f)]
        public float blurRadius = 2.0f;

        [Range(1,10)]
        public int iteration = 2;

        [Range(1.0f, 10.0f)]
        public float rtDowmScaling = 2.0f;
    }

    [SerializeField] private DualKawaseBlurSetting _setting;
    [SerializeField] private Shader _shader;
    private DualKawaseBlurRenderPass _renderPass;
    public override void Create()
    {
        _renderPass = new DualKawaseBlurRenderPass(_shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _renderPass.Setup(_setting, renderer.cameraColorTarget);
        renderer.EnqueuePass(_renderPass);
    }
}
