Shader "BRB/AnomalyHazePrototype"
{
    // 짙은 현상 "볼류메트릭 헤이즈" 프로토 — 기존 BRB/AnomalyFog보다 깊이감/뭉게짐/광선결을 강화한 실험판.
    // URP 2D 렌더러용 전체화면 오버레이(카메라 앞 풀스크린 쿼드). depth 버퍼 불필요.
    //  · 도메인 워프 fbm → 연기처럼 뭉게지는 결(평면 노이즈 X).
    //  · 원근 2레이어(near/far가 카메라 이동에 다른 속도) → "부피" 착시.
    //  · 방향성 광선결(라이트 샤프트 흉내) + 상단 소프트 라이트 그라데이션.
    //  · 플레이어 주변 소프트 클리어 버블(내 시야 확보), 멀수록 짙게(depth bias).
    Properties
    {
        _FogColor    ("Fog Color (어두운 안개)", Color) = (0.035, 0.070, 0.045, 1)
        _LightColor  ("Light Color (병든 빛결)", Color) = (0.30, 0.52, 0.26, 1)
        _Density     ("Density (0~1 강도)", Range(0,1)) = 0.6
        _HazeStrength("Haze Strength (최대 불투명)", Range(0,1)) = 0.94
        _Coverage    ("Coverage (덮는 정도)", Range(0,1)) = 0.6
        _Scale       ("Noise Scale (결 크기)", Range(0.5, 8)) = 2.6
        _Speed       ("Drift Speed (흐름)", Range(0, 1)) = 0.02
        _Warp        ("Warp (뭉게짐)", Range(0, 2)) = 1.0
        _ShaftStrength("Light Shaft (광선결)", Range(0, 1)) = 0.35
        _ShaftAngle  ("Shaft Angle (deg)", Range(0, 360)) = 115
        _DepthBias   ("Depth Bias (멀수록 짙게)", Range(0, 1)) = 0.6
        _TopLight    ("Top Light (상단 밝기)", Range(0, 1)) = 0.25
        _NearClear   ("Near Clear (클리어 거리)", Range(0,1.5)) = 0.28
        _FarFull     ("Far Full (완전안개 거리)", Range(0.3,3)) = 1.5
        _ClearCenter ("Clear Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect      ("Aspect", Float) = 1.78
        _WorldAnchor ("World Anchor", Range(0,1)) = 1
        _WorldOffset ("World Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }

        Pass
        {
            Name "AnomalyHaze2D"
            Tags { "LightMode"="Universal2D" }
            Cull Off  ZWrite Off  ZTest Always  Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 2D 렌더러가 패스를 인식하도록 shape-light 키워드 포함(조명은 안 씀 — 언릿 오버레이).
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _LightColor;
                float _Density, _HazeStrength, _Coverage;
                float _Scale, _Speed, _Warp;
                float _ShaftStrength, _ShaftAngle, _DepthBias, _TopLight;
                float _NearClear, _FarFull;
                float4 _ClearCenter;
                float _Aspect, _WorldAnchor;
                float4 _WorldOffset;
            CBUFFER_END

            float Hash21(float2 p) { p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1,0)), c = Hash21(i + float2(0,1)), d = Hash21(i + float2(1,1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p) { float s=0, a=0.5; [unroll] for (int i=0;i<5;i++){ s+=a*VNoise(p); p*=2.02; a*=0.5; } return s; }

            // 도메인 워프 fbm — 연기처럼 뭉게지는 결
            float WarpFbm(float2 p, float t)
            {
                float2 q = float2(Fbm(p + float2(0.0, t)), Fbm(p + float2(5.2, 1.3 - t)));
                float2 r = float2(Fbm(p + _Warp * q + float2(1.7, 9.2) + t * 0.5),
                                  Fbm(p + _Warp * q + float2(8.3, 2.8) - t * 0.4));
                return Fbm(p + _Warp * r);
            }

            half4 ComputeFog(float2 uv)
            {
                float t = _Time.y * _Speed;
                float2 anchor = _WorldOffset.xy * _WorldAnchor;

                // 원근 2레이어(near는 앵커 강하게 = 카메라 따라 많이, far는 약하게) → 부피 착시
                float2 nearP = uv * _Scale        + anchor * 1.00;
                float2 farP  = uv * _Scale * 0.55 + anchor * 0.45 + 17.3;
                float nearN = WarpFbm(nearP, t);
                float farN  = WarpFbm(farP, t * 0.7);
                float fog = saturate(farN * 0.55 + nearN * 0.6);

                // 방향성 광선결(라이트 샤프트 흉내) — shaft 축 투영에 노이즈 결
                float ang = radians(_ShaftAngle);
                float2 dir = float2(cos(ang), sin(ang));
                float along = dot(uv - 0.5, dir);
                float shaft = Fbm(float2(along * 6.0 + t * 1.5, dot(uv - 0.5, float2(-dir.y, dir.x)) * 2.0));
                shaft = saturate(shaft * shaft) * _ShaftStrength;

                // 플레이어 주변 소프트 클리어(노이즈로 경계 깸 → 원 안 보이게)
                float2 pc = uv - _ClearCenter.xy; pc.x *= _Aspect;
                float dist = length(pc) * 2.0;
                float clearBubble = 1.0 - smoothstep(_NearClear, _FarFull, dist);
                clearBubble = saturate(clearBubble * (0.55 + 0.85 * fog));

                // 멀수록 짙게(depth bias) + 상단 소프트 라이트
                float depth = lerp(1.0 - _DepthBias, 1.0, saturate(dist * 0.55));
                float topLight = _TopLight * smoothstep(0.2, 1.0, uv.y);

                float density = saturate(_Density);
                float cover = lerp(1.0 - _Coverage, 1.0, fog);   // Coverage=덮는 하한
                float amt = density * cover * depth * (1.0 - clearBubble);

                // 색: 어두운 베이스 + 빛결(wisp)·샤프트·상단광을 병든 빛색으로
                float lit = saturate(fog * 0.6 + shaft + topLight);
                half3 col = lerp(_FogColor.rgb, _LightColor.rgb, lit * 0.6);
                half a = saturate(amt * _HazeStrength);
                return half4(col, a);
            }

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }
            half4 frag(Varyings input) : SV_Target { return ComputeFog(input.uv); }
            ENDHLSL
        }

        // 폴백(3D/프리뷰 인스펙터) — 동일 출력
        Pass
        {
            Name "AnomalyHazeForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off  ZWrite Off  ZTest Always  Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _LightColor;
                float _Density, _HazeStrength, _Coverage;
                float _Scale, _Speed, _Warp;
                float _ShaftStrength, _ShaftAngle, _DepthBias, _TopLight;
                float _NearClear, _FarFull;
                float4 _ClearCenter;
                float _Aspect, _WorldAnchor;
                float4 _WorldOffset;
            CBUFFER_END

            float Hash21(float2 p) { p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1,0)), c = Hash21(i + float2(0,1)), d = Hash21(i + float2(1,1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p) { float s=0, a=0.5; [unroll] for (int i=0;i<5;i++){ s+=a*VNoise(p); p*=2.02; a*=0.5; } return s; }
            float WarpFbm(float2 p, float t)
            {
                float2 q = float2(Fbm(p + float2(0.0, t)), Fbm(p + float2(5.2, 1.3 - t)));
                float2 r = float2(Fbm(p + _Warp * q + float2(1.7, 9.2) + t * 0.5),
                                  Fbm(p + _Warp * q + float2(8.3, 2.8) - t * 0.4));
                return Fbm(p + _Warp * r);
            }
            half4 ComputeFog(float2 uv)
            {
                float t = _Time.y * _Speed;
                float2 anchor = _WorldOffset.xy * _WorldAnchor;
                float2 nearP = uv * _Scale        + anchor * 1.00;
                float2 farP  = uv * _Scale * 0.55 + anchor * 0.45 + 17.3;
                float fog = saturate(WarpFbm(farP, t * 0.7) * 0.55 + WarpFbm(nearP, t) * 0.6);
                float ang = radians(_ShaftAngle);
                float2 dir = float2(cos(ang), sin(ang));
                float along = dot(uv - 0.5, dir);
                float shaft = saturate(pow(Fbm(float2(along * 6.0 + t * 1.5, dot(uv - 0.5, float2(-dir.y, dir.x)) * 2.0)), 2.0)) * _ShaftStrength;
                float2 pc = uv - _ClearCenter.xy; pc.x *= _Aspect;
                float dist = length(pc) * 2.0;
                float clearBubble = saturate((1.0 - smoothstep(_NearClear, _FarFull, dist)) * (0.55 + 0.85 * fog));
                float depth = lerp(1.0 - _DepthBias, 1.0, saturate(dist * 0.55));
                float topLight = _TopLight * smoothstep(0.2, 1.0, uv.y);
                float amt = saturate(_Density) * lerp(1.0 - _Coverage, 1.0, fog) * depth * (1.0 - clearBubble);
                float lit = saturate(fog * 0.6 + shaft + topLight);
                half3 col = lerp(_FogColor.rgb, _LightColor.rgb, lit * 0.6);
                return half4(col, saturate(amt * _HazeStrength));
            }
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }
            half4 frag(Varyings input) : SV_Target { return ComputeFog(input.uv); }
            ENDHLSL
        }
    }
    FallBack Off
}
