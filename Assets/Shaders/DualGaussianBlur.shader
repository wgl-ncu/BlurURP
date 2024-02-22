Shader "Custom/DualGaussianBlur"
{
    HLSLINCLUDE
    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 uv01 : TEXCOORD1;
        float4 uv23 : TEXCOORD2;
        float4 uv45 : TEXCOORD3;
    };

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    uniform float4 _BlurOffset;
    TEXTURE2D(_Source);
    SAMPLER(sampler_Source);
    
    v2f vert(appdata i)
    {
        v2f o;
        o.uv = i.uv;
        
		o.uv01 = o.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1);
		o.uv23 = o.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1) * 2.0;
		o.uv45 = o.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1) * 6.0;
        o.vertex = TransformObjectToHClip(i.vertex);
        return o;
    }
    
    ENDHLSL
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        
        Pass
        {
            Tags{"RenderPipeline" = "UniversalRenderPipeline"}
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            half4 frag(v2f i) : SV_Target
            {
                half4 color = half4(0,0,0,0);

                color += 0.40 * SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv);
                color += 0.15 * SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv01.xy);
                color += 0.15 * SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv01.zw);
                color += 0.10 * SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv23.xy);
                color += 0.10 * SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv23.zw);
                color += 0.05 * SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv45.xy);
                color += 0.05 * SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv45.zw);

                return color;
            }
            
            ENDHLSL
        }
        
        Pass
        {
            Tags{"RenderPipeline" = "UniversalRenderPipeline"}
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            half4 frag(v2f i):SV_Target
            {
                return SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv);
            }
            ENDHLSL
        }
    }
}
