// Stage 0/1 스파이크 — 카메라 오클루전 처리. 프로덕션 셰이더 아님(조명은 최소 구현).
//
// 규약: 컷어웨이 파라미터는 **전역 유니폼**이다(Properties에 없음).
//   Shader.SetGlobalVector("_CutCenter", ...) 한 번이면 이 셰이더를 쓰는 모든 건물이
//   같이 동작한다 — 재질별 배선이 필요 없다.
//
// 판정은 두 조건의 교집합이다:
//   ① 화면상 플레이어 위치에서 반경 안  (월드 거리로 하면 건물 표면이 멀어 안 잘린다)
//   ② 프래그먼트가 플레이어보다 카메라에 가까움 (뒤에 있는 건물까지 뚫리면 안 된다)
Shader "Spike/OccluderFX"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.46, 0.45, 0.44, 1)
        _Mode      ("Mode (0=solid 1=cutaway 2=dither)", Float) = 1
        _Alpha     ("Dither Alpha", Range(0,1)) = 0.25
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
            CBUFFER_END

            // ── 전역(재질별 아님) ──
            // ⚠️ **월드 좌표를 받는다.** 화면 좌표를 CPU에서 계산해 넘기면 렌더 타겟에 따라
            //    Y가 뒤집혀(UNITY_UV_STARTS_AT_TOP / _ProjectionParams.x) 구멍이 어긋난다.
            //    프래그먼트와 중심을 **같은 행렬로** 투영하면 뒤집힘과 무관해진다.
            float4 _CutCenter;   // xyz = 플레이어 월드 좌표(가슴 높이)
            float  _CutRadius;   // 화면 높이 대비 비율 (예 0.13)
            float  _CutSoft;     // 화면 높이 대비 비율 (예 0.05)
            float  _CutEnabled;  // 0/1

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  viewDepth  : TEXCOORD2;
                float2 fragNDC    : TEXCOORD3;   // -1~1
                float3 cutNDCDepth: TEXCOORD4;   // xy = 중심 NDC, z = 중심 뷰 깊이
            };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                o.viewDepth  = -TransformWorldToView(p.positionWS).z;   // 카메라로부터의 거리
                o.fragNDC    = p.positionCS.xy / max(p.positionCS.w, 1e-6);
                float4 cutCS = TransformWorldToHClip(_CutCenter.xyz);   // 프래그먼트와 동일 경로
                o.cutNDCDepth = float3(cutCS.xy / max(cutCS.w, 1e-6),
                                       -TransformWorldToView(_CutCenter.xyz).z);
                return o;
            }

            // 4x4 Bayer — 스크린도어. 깊이를 유지하므로 반투명 정렬 문제가 없다.
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
                float dither = Bayer4(IN.positionCS.xy);

                if (_Mode > 1.5)
                {
                    clip(_Alpha - dither);                       // 통짜 디더 페이드
                }
                else if (_Mode > 0.5 && _CutEnabled > 0.5)
                {
                    // ② 플레이어보다 뒤면 건드리지 않는다 (여유 0.5m)
                    if (IN.viewDepth < IN.cutNDCDepth.z - 0.5)
                    {
                        // ① 화면상 플레이어 주변 반경. NDC는 세로 -1~1(=2)이므로 반경을 2배로.
                        //    종횡비 보정해 원형을 유지한다.
                        float aspect = _ScreenParams.x / _ScreenParams.y;
                        float2 off = (IN.fragNDC - IN.cutNDCDepth.xy) * float2(aspect, 1.0);
                        float d = length(off);
                        float t = saturate((d - _CutRadius * 2.0) / max(_CutSoft * 2.0, 0.001));
                        clip(t - dither * 0.999);
                    }
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
            CBUFFER_END

            struct SAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SVaryings   { float4 positionCS : SV_POSITION; };

            // 그림자는 구멍과 무관하게 그대로 드리운다 — 건물이 사라진 것처럼 보이면 안 된다.
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
