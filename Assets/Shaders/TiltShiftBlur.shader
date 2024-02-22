Shader "Unlit/TiltShiftBlur"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    uniform  float3 _Gradient;
	uniform  float4 _GoldenRot;
	uniform  float4 _Params;

    #define _Offset _Gradient.x
	#define _Area _Gradient.y
	#define _Spread _Gradient.z
	#define _Iteration _Params.x
	#define _Radius _Params.y
	#define _PixelSize _Params.zw

    TEXTURE2D(_Source);
    SAMPLER(sampler_Source);

    struct appdata
    {
		float4 vertex : POSITION;
    	float2 uv : TEXCOORD0;
    };

    struct v2f
    {
    	float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
    };                                                                                                                                                                                                                                                                                                                                                                                                        
    
	v2f Vert(appdata i)
	{
		v2f o;
		o.uv = i.uv;
		o.vertex = TransformObjectToHClip(i.vertex);
		return o;
	}

    float TiltShiftMask(float2 uv)
	{
		float centerY = uv.y * 2.0 - 1.0 + _Offset; // [0,1] -> [-1,1]
		return pow(abs(centerY * _Area), _Spread);
	}
	
	half4 Frag(v2f i):SV_Target
    {
    	half2x2 rot = half2x2(_GoldenRot);
    	half4 accumulator = 0.0;
    	half4 divisor = 0.0;
    	half r = 2.0;

    	half2 angle = half2(0.0, _Radius * saturate(TiltShiftMask(i.uv)));

    	for (int j = 0; j < _Iteration; ++j)
    	{
    		r  += 1.0 / r;
    		angle = mul(rot, angle);
    		half4 boken = SAMPLE_TEXTURE2D(_Source, sampler_Source, float2(i.uv + _PixelSize * (r - 1.0) * angle));
    		accumulator += boken * boken;
    		divisor += boken;
    	}
    	return accumulator / divisor;
	}

	half4 FragPreview(v2f i):SV_Target
	{
		return TiltShiftMask(i.uv);
	}                                                                                                                                                                                                                                                                                                                                                                                                              
                                                                                                                                                                                                                                                                                                                      
    
    ENDHLSL
    SubShader
    {
    	Cull Off ZWrite Off ZTest Always
        Tags{"RenderPipeline" = "UniversalRenderPipeline"}
        LOD 100

        Pass
        {
        	HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
        	
        	ENDHLSL
        }
        
        Pass
        {
	        HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragPreview
        	
        	ENDHLSL
        }
    }
}
