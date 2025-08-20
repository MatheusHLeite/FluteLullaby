Shader"Hidden/Custom/GammaCorrection"
{
    Properties
    {
        _Gamma("Gamma", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
Name"GammaCorrectionPass"
            ZTest
Always Cull
Off ZWrite
Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Em Unity 6 / URP 17 a entrada é _CameraColorTexture
            TEXTURE2D(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);
float _Gamma;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

Varyings Vert(Attributes input)
{
    Varyings output;
    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

half4 Frag(Varyings input) : SV_Target
{
    float3 color = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, input.uv).rgb;
    color = pow(color, 1.0 / max(_Gamma, 0.001)); // evita divisão por zero
    return half4(color, 1);
}
            ENDHLSL
        }
    }
}
