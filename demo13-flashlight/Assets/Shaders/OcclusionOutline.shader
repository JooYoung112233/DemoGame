Shader "Custom/OcclusionOutline"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _Color ("Outline Color", Color) = (1.0, 1.0, 1.0, 0.7)
        _OutlineWidth ("Outline Expand (px)", Range(0.5, 6)) = 2.0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Sprite Sheet)]
        _Columns ("Columns", Float) = 2
        _Rows ("Rows", Float) = 8
        _CurrentFrame ("Current Frame", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "OcclusionOutline"

            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float _OutlineWidth;
                float _Cutoff;
                float _Columns;
                float _Rows;
                float _CurrentFrame;
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

            float2 calcSheetUV(float2 uv)
            {
                uint frame = (uint)_CurrentFrame;
                uint cols = (uint)_Columns;
                uint col = frame % cols;
                uint row = frame / cols;

                float cellW = 1.0 / _Columns;
                float cellH = 1.0 / _Rows;

                float u = (col + uv.x) * cellW;
                float v = 1.0 - (row + 1.0 - uv.y) * cellH;
                return float2(u, v);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = calcSheetUV(input.uv);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 본체 알파가 충분하면 아웃라인 색으로 채움 (실루엣)
                float centerAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                if (centerAlpha >= _Cutoff)
                    return half4(_Color.rgb, _Color.a * 0.3); // 실루엣 (옅게)

                // 주변 샘플링 — 인접 픽셀에 불투명이 있으면 외곽선
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;
                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                    float2(-1, -1), float2(-1, 1), float2(1, -1), float2(1, 1)
                };

                float maxAlpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUV = input.uv + offsets[i] * texelSize;
                    maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, sampleUV, 0).a);
                }

                if (maxAlpha >= _Cutoff)
                    return _Color; // 외곽선

                clip(-1);
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
