Shader "Unlit/DualKawaseBlur"
{
    HLSLINCLUDE

    uniform half _Offset;
    float4 _Source_TexelSize;

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    TEXTURE2D(_Source);
    SAMPLER(sampler_Source);

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f_DownSample
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 uv01: TEXCOORD1;
		float4 uv23: TEXCOORD2;
    };

    struct v2f_UpSample
    {
    	float4 vertex: SV_POSITION;
		float4 uv01: TEXCOORD0;
		float4 uv23: TEXCOORD1;
		float4 uv45: TEXCOORD2;
		float4 uv67: TEXCOORD3;
    };
    
    ENDHLSL
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Tags{"RenderPipeline" = "UniversalRenderPipeline"}
        LOD 100
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert_DownSample
            #pragma fragment Frag_DownSample

            v2f_DownSample Vert_DownSample(appdata v)
            {
                v2f_DownSample o;
                o.uv = v.uv;
                _Source_TexelSize *= 0.5;
                half offset = float2(1 + _Offset, 1 + _Offset);
                o.uv01.xy = o.uv - _Source_TexelSize * offset;//右上
                o.uv01.zw = o.uv + _Source_TexelSize * offset;//左下
                o.uv23.xy = o.uv - float2(_Source_TexelSize.x, -_Source_TexelSize.y) * offset;//左上
                o.uv23.zw = o.uv + float2(_Source_TexelSize.x, -_Source_TexelSize.y) * offset;//右下
                o.vertex = TransformObjectToHClip(v.vertex);
                return o;
            }

            half4 Frag_DownSample(v2f_DownSample i) : SV_Target
            {
                half4 res = SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv) * 4;
                res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv01.xy);
                res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv01.zw);
                res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv23.xy);
                res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv23.zw);
                res *= 0.125;
                return res;
            }
            
            ENDHLSL
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert_UpSample
            #pragma fragment Frag_UpSample
            v2f_UpSample Vert_UpSample(appdata v)
            {
                v2f_UpSample o;
                o.vertex = TransformObjectToHClip(v.vertex);
                _Source_TexelSize *= 0.5;
                half offset = float2(1 + _Offset, 1 + _Offset);
                float2 uv = v.uv;
                o.uv01.xy = uv + float2(-_Source_TexelSize.x * 2, 0) * offset;
		        o.uv01.zw = uv + float2(-_Source_TexelSize.x, _Source_TexelSize.y) * offset;
		        o.uv23.xy = uv + float2(0, _Source_TexelSize.y * 2) * offset;
		        o.uv23.zw = uv + _Source_TexelSize * offset;
		        o.uv45.xy = uv + float2(_Source_TexelSize.x * 2, 0) * offset;
		        o.uv45.zw = uv + float2(_Source_TexelSize.x, -_Source_TexelSize.y) * offset;
		        o.uv67.xy = uv + float2(0, -_Source_TexelSize.y * 2) * offset;
		        o.uv67.zw = uv - _Source_TexelSize * offset;
                return o;
            }

            half4 Frag_UpSample(v2f_UpSample i) : SV_Target
            {
                half4 res = SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv01.xy);
				res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv01.zw) * 2;
				res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv23.xy);
				res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv23.zw) * 2;
				res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv45.xy);
				res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv45.zw) * 2;
				res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv67.xy);
				res += SAMPLE_TEXTURE2D(_Source, sampler_Source, i.uv67.zw) * 2;
            	return res * 0.0833;
            }
            
            ENDHLSL
        }
    }
}
