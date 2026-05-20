Shader "Custom/SpriteSheet"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FrameCount ("Frame Count", Float) = 1
        _CurrentFrame ("Current Frame", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "SpriteSheet"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend One Zero
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _FrameCount;
                float _CurrentFrame;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // UV를 현재 프레임으로 슬라이싱
                float frameH = 1.0 / _FrameCount;
                float frameY = 1.0 - (_CurrentFrame + 1.0) * frameH;
                output.uv = float2(input.uv.x, input.uv.y * frameH + frameY);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col *= _Color;
                clip(col.a - _Cutoff);
                return col;
            }
            ENDHLSL
        }
    }
}
