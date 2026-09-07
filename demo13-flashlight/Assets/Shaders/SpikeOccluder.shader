// Stage 0 스파이크 전용 — 오클루전 처리 3종 비교용. 프로덕션 셰이더 아님.
// _Mode 0 = 디더 페이드(스크린도어) / 1 = 구형 컷어웨이(플레이어 주변만 뚫음)
Shader "Spike/OccluderFX"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.46, 0.45, 0.44, 1)
        _Mode      ("Mode (0=dither 1=cutaway)", Float) = 0
        _Alpha     ("Dither Alpha", Range(0,1)) = 0.25
        _PlayerPos ("Player World Pos", Vector) = (0,0,0,0)
        _Radius    ("Cutaway Radius", Float) = 3.0
        _Soft      ("Cutaway Soft Edge", Float) = 1.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Mode;
                float  _Alpha;
                float4 _PlayerPos;
                float  _Radius;
                float  _Soft;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }

            // 4x4 Bayer — 스크린도어 투명(깊이를 유지해 정렬 문제가 없다)
            float Bayer4(float2 sp)
            {
                const float m[16] = {
                     0.0625, 0.5625, 0.1875, 0.6875,
                     0.8125, 0.3125, 0.9375, 0.4375,
                     0.2500, 0.7500, 0.1250, 0.6250,
                     1.0000, 0.5000, 0.8750, 0.3750
                };
                int2 c = int2(fmod(sp, 4.0));
                return m[c.y * 4 + c.x];
            }

            half4 frag (Varyings IN) : SV_Target
            {
                if (_Mode < 0.5)
                {
                    // 디더 페이드 — 알파만큼 픽셀을 규칙적으로 버린다
                    clip(_Alpha - Bayer4(IN.positionCS.xy));
                }
                else
                {
                    // 컷어웨이 — 화면상 플레이어 위치 주변에 구멍을 뚫는다.
                    // 월드 거리로 하면 건물 표면이 플레이어에서 멀어 아무것도 안 잘린다 —
                    // 가리는 것을 걷어내는 일이므로 판정은 시선 축(=화면 공간)에서 해야 한다.
                    // _PlayerPos.xy = 픽셀 좌표(위에서 아래), _Radius/_Soft = 픽셀.
                    float d = length(IN.positionCS.xy - _PlayerPos.xy);
                    float t = saturate((d - _Radius) / max(_Soft, 0.001));  // 0=중심 1=바깥
                    clip(t - Bayer4(IN.positionCS.xy) * 0.999);
                }

                float3 N = normalize(IN.normalWS);
                float4 sc = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(sc);
                float ndl = saturate(dot(N, mainLight.direction));
                float3 direct = mainLight.color * ndl * mainLight.shadowAttenuation;
                float3 ambient = SampleSH(N);
                return half4(_BaseColor.rgb * (direct + ambient), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Mode;
                float  _Alpha;
                float4 _PlayerPos;
                float  _Radius;
                float  _Soft;
            CBUFFER_END

            struct SAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SVaryings   { float4 positionCS : SV_POSITION; };

            SVaryings shadowVert (SAttributes IN)
            {
                SVaryings o;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, _MainLightPosition.xyz));
                return o;
            }

            half4 shadowFrag (SVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
