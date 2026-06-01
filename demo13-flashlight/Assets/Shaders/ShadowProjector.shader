Shader "BRB/ShadowProjector"
{
    Properties
    {
        _MainTex ("Sprite (모듈과 동일 텍스처)", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
        _ShadowStrength ("Shadow Strength (진하기)", Range(0, 1)) = 0.55
        _TipFade ("Tip Fade (끝 흐려짐)", Range(0, 1)) = 0.35
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.1
        [Toggle] _FlipV ("Flip V (위아래 반대로 투영)", Float) = 0

        // 아래 둘은 보통 스크립트(FlatShadow.cs)가 채움 — 직접 만져도 됨
        _ShadowDirWS ("Shadow Dir WS (xy)", Vector) = (1, 0, 0, 0)
        _ShadowLength ("Shadow Length", Range(0, 5)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FlatShadow"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ShadowColor;
                float _ShadowStrength;
                float _TipFade;
                float _Cutoff;
                float _FlipV;
                float4 _ShadowDirWS;   // .xy = 화면 XY 평면 방향
                float _ShadowLength;
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
                float height : TEXCOORD1; // 0=바닥접지, 1=꼭대기
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                // 텍스처 세로 = 오브젝트 높이. 바닥(접지선)은 고정, 위쪽일수록 투영
                float h = (_FlipV > 0.5) ? (1.0 - input.uv.y) : input.uv.y;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                // 빛 반대 방향(=_ShadowDirWS)으로 화면 XY 평면에서 늘림
                posWS.x += _ShadowDirWS.x * h * _ShadowLength;
                posWS.y += _ShadowDirWS.y * h * _ShadowLength;

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.height = h;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(a - _Cutoff);

                // 끝으로 갈수록 옅게
                float fade = lerp(1.0, _TipFade, saturate(input.height));
                return half4(_ShadowColor.rgb, a * _ShadowStrength * fade);
            }
            ENDHLSL
        }

        // ---- URP 2D 렌더러(Renderer2D)용 패스 (동일 로직) ----
        Pass
        {
            Name "FlatShadow2D"
            Tags { "LightMode"="Universal2D" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ShadowColor;
                float _ShadowStrength;
                float _TipFade;
                float _Cutoff;
                float _FlipV;
                float4 _ShadowDirWS;
                float _ShadowLength;
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
                float height : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float h = (_FlipV > 0.5) ? (1.0 - input.uv.y) : input.uv.y;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS.x += _ShadowDirWS.x * h * _ShadowLength;
                posWS.y += _ShadowDirWS.y * h * _ShadowLength;

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.height = h;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(a - _Cutoff);
                float fade = lerp(1.0, _TipFade, saturate(input.height));
                return half4(_ShadowColor.rgb, a * _ShadowStrength * fade);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
