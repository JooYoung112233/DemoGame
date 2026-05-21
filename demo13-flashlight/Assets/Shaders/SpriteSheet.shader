Shader "Custom/SpriteSheet"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Columns ("Columns", Float) = 2
        _Rows ("Rows", Float) = 8
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
                float _Columns;
                float _Rows;
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

                // 그리드 기반 UV 슬라이싱
                int frame = (int)_CurrentFrame;
                int col = frame % (int)_Columns;
                int row = frame / (int)_Columns;

                float cellW = 1.0 / _Columns;
                float cellH = 1.0 / _Rows;

                // 왼쪽→오른쪽, 위→아래 순서
                float u = (col + input.uv.x) * cellW;
                float v = 1.0 - (row + 1.0 - input.uv.y) * cellH;

                output.uv = float2(u, v);
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
