Shader "Custom/RadialBlur"
{
    HLSLINCLUDE
    uniform half3 _Params;
    uniform float2 _Params2;
    #define _RadialCenter _Params.yz
    #define _BlurRadius _Params.x
    #define _Quality _Params2.x
	#define _Average _Params2.y

     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_Source);
    SAMPLER(sampler_Source);
    
    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    v2f DefaultVert(appdata v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex);
        o.uv = v.uv;
        return o;
    }

    half4 RadialBlur(float2 texcoord)
    {
        float2 uv = texcoord - _RadialCenter;
        half scale = 1;
        half4 color = half4(0,0,0,0);
        for(int i = 0; i < (int)_Quality; ++i)
        {
            scale = _BlurRadius * i + 1;
            color += SAMPLE_TEXTURE2D(_Source, sampler_Source, uv * scale + _RadialCenter);
        }
        return color * _Average;
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

            #pragma vertex DefaultVert
            #pragma fragment  frag

            half4 frag (v2f i) : SV_Target
            {
                return RadialBlur(i.uv);
            }
            
            ENDHLSL
        }
    }
}
