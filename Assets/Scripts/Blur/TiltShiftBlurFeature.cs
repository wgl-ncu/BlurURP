using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TiltShiftBlurFeature : ScriptableRendererFeature
{
    [Serializable]
    public class TiltShiftBlurSettings
    {
        [Range(0f, 3f)] public float blurRaius = 1f;

        [Range(8, 128)] public int iteration = 32;

        [Range(-1f, 1f)] public float centerOffset = 0f;

        [Range(0f, 20f)] public float areaSize = 1f;

        [Range(1f, 20f)] public float areaSmooth = 1.3f;

        public bool showPriview = false;
    }

    [SerializeField] private TiltShiftBlurSettings _settings;
    [SerializeField] private Shader _shader;
    private TiltShiftBlurRenderPass _renderPass;
    public override void Create()
    {
        _renderPass = new TiltShiftBlurRenderPass(_shader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _renderPass.Setup(_settings, renderer.cameraColorTarget);
        renderer.EnqueuePass(_renderPass);
    }
}
