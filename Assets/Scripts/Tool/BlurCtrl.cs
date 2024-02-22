using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlurCtrl : MonoBehaviour
{
    public ForwardRendererData rendererData;

    public enum BlurType
    {
        NONE,
        Gaussian_Common,
        Gaussian_CB,
        Dual,
        TiltShift,
        Radial
    }

    private BlurType curType;
    private Dictionary<BlurType, ScriptableRendererFeature> dict;

    private void Start()
    {
        curType = BlurType.NONE;
        dict = new Dictionary<BlurType, ScriptableRendererFeature>();
        dict.Add(BlurType.NONE, null);
        dict.Add(BlurType.Gaussian_Common, rendererData.rendererFeatures.OfType<DualGaussianRenderFeature>().FirstOrDefault());
        dict.Add(BlurType.Gaussian_CB, rendererData.rendererFeatures.OfType<DualGaussianRenderFeature>().FirstOrDefault());
        dict.Add(BlurType.Dual, rendererData.rendererFeatures.OfType<DualKawaseBlurRenderFeature>().FirstOrDefault());
        dict.Add(BlurType.TiltShift, rendererData.rendererFeatures.OfType<TiltShiftBlurFeature>().FirstOrDefault());
        dict.Add(BlurType.Radial, rendererData.rendererFeatures.OfType<RadialBlurRenderFeature>().FirstOrDefault());
    }

    public void ClickNone()
    {
        Click(BlurType.NONE);
    }

    public void ClickGuassainCommon()
    {
        Click(BlurType.Gaussian_Common);
    }

    public void ClickGuassian_CB()
    {
        Click(BlurType.Gaussian_CB);
    }

    public void ClickDual()
    {
        Click(BlurType.Dual);
    }

    public void ClickTiltShift()
    {
        Click(BlurType.TiltShift);
    }

    public void ClickRadial()
    {
        Click(BlurType.Radial);
    }

    public void Click(BlurType type)
    {
        if(type != curType)
        {
            Switch(type);
            curType = type;
        }
    }

    private void Switch(BlurType type)
    {
        dict[curType]?.SetActive(false);
        dict[type]?.SetActive(true);
        if(type == BlurType.Gaussian_CB)
        {
            (dict[type] as DualGaussianRenderFeature)._settings.useCS = true;
        }
        if (type == BlurType.Gaussian_Common)
        {
            (dict[type] as DualGaussianRenderFeature)._settings.useCS = false;
        }
    }
}
