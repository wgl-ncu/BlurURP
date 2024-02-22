using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class PostProcessShaderData
{
    /// <summary>
    /// 径向模糊
    /// </summary>
    [Reload("Shaders/RadialBlur.shader")]
    public static Shader radialBlurShader;
}
