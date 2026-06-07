Shader "BRB/AnomalyFog"
{
    // 짙은 현상(시간 무관 침식) 전체화면 오버레이 — 카메라 앞 풀스크린 쿼드(DenseAnomalyController가 생성).
    // 낮/밤 글로벌 Light2D를 안 건드리고 최종 화면 위에 어두운 안개를 알파로 덧씌움.
    //  · 노이즈 주도 패치 안개(움직임) — 방사형 원이 아니라 불규칙 결.
    //  · 플레이어(중심) 주변은 소프트 클리어 + 노이즈로 경계를 깨 "원"처럼 안 보이게.
    //  · 어두운 톤(밝은 원반 X) + 미세한 밝은 wisp 결로 밤에도 움직임 인지.
    Properties
    {
        _FogColor ("Fog Color (어두운 안개)", Color) = (0.06, 0.07, 0.10, 1)
        _Density ("Density (0~1)", Range(0,1)) = 0
        _HazeStrength ("Haze Strength (최대 불투명)", Range(0,1)) = 0.9
        _HazeMin ("Haze Min (옅은 곳 바닥)", Range(0,1)) = 0.3
        _WispBright ("Wisp Brightness (밝은 결)", Range(1,4)) = 2.5
        _NearClear ("Near Clear (플레이어 클리어 거리)", Range(0,1.5)) = 0.25
        _FarFull ("Far Full (완전 안개 거리)", Range(0.3,3)) = 1.4
        _NoiseScale ("Noise Scale (패치 크기)", Range(0.5, 8)) = 3.0
        _NoiseSpeed ("Noise Speed (드리프트)", Range(0, 1)) = 0.06
        _ClearCenter ("Clear Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.78
        _WorldAnchor ("World Anchor", Range(0,1)) = 1
        _WorldOffset ("World Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }

        Pass
        {
            Name "AnomalyFog2D"
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
                float _Density;
                float _HazeStrength;
                float _HazeMin;
                float _WispBright;
                float _NearClear;
                float _FarFull;
                float _NoiseScale;
                float _NoiseSpeed;
                float4 _ClearCenter;
                float _Aspect;
                float _WorldAnchor;
                float4 _WorldOffset;
            CBUFFER_END

            float Hash21(float2 p) { p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1,0)), c = Hash21(i + float2(0,1)), d = Hash21(i + float2(1,1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p) { float s=0, a=0.5; [unroll] for (int i=0;i<5;i++){ s+=a*VNoise(p); p*=2; a*=0.5; } return s; }

            half4 ComputeFog(float2 uv)
            {
                // 움직이는 패치 안개(노이즈 주도 — 방사형 원이 아님)
                float2 b = uv * _NoiseScale + _WorldOffset.xy * _WorldAnchor;
                float n1 = Fbm(b + float2(_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.6));
                float n2 = Fbm(b * 2.13 + float2(-_Time.y * _NoiseSpeed * 0.8, _Time.y * _NoiseSpeed) + 11.3);
                float fog = saturate(n1 * 0.65 + n2 * 0.45);

                // 플레이어 주변 소프트 클리어 — 노이즈로 경계를 깨 "원"처럼 안 보이게
                float2 p = uv - _ClearCenter.xy; p.x *= _Aspect;
                float dist = length(p) * 2.0;
                float clearBubble = 1.0 - smoothstep(_NearClear, _FarFull, dist);   // 1 근처 → 0 먼 곳
                clearBubble = saturate(clearBubble * (0.55 + 0.85 * fog));          // 노이즈로 울퉁불퉁

                float density = saturate(_Density);
                float distBias = lerp(0.75, 1.0, saturate(dist * 0.6));             // 멀수록 살짝 더
                float amt = density * lerp(_HazeMin, 1.0, fog) * (1.0 - clearBubble) * distBias;

                // 어두운 안개 톤 + 미세한 밝은 wisp 결(밝은 원반 X, 밤엔 움직임으로 인지)
                half3 col = _FogColor.rgb * (1.0 + fog * (_WispBright - 1.0));
                half a = saturate(amt * _HazeStrength);
                return half4(col, a);
            }

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

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

        // 폴백(3D/프리뷰) — 동일 출력
        Pass
        {
            Name "AnomalyFogForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off  ZWrite Off  ZTest Always  Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Density;
                float _HazeStrength;
                float _HazeMin;
                float _WispBright;
                float _NearClear;
                float _FarFull;
                float _NoiseScale;
                float _NoiseSpeed;
                float4 _ClearCenter;
                float _Aspect;
                float _WorldAnchor;
                float4 _WorldOffset;
            CBUFFER_END

            float Hash21(float2 p) { p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1,0)), c = Hash21(i + float2(0,1)), d = Hash21(i + float2(1,1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p) { float s=0, a=0.5; [unroll] for (int i=0;i<5;i++){ s+=a*VNoise(p); p*=2; a*=0.5; } return s; }

            half4 ComputeFog(float2 uv)
            {
                float2 b = uv * _NoiseScale + _WorldOffset.xy * _WorldAnchor;
                float n1 = Fbm(b + float2(_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.6));
                float n2 = Fbm(b * 2.13 + float2(-_Time.y * _NoiseSpeed * 0.8, _Time.y * _NoiseSpeed) + 11.3);
                float fog = saturate(n1 * 0.65 + n2 * 0.45);
                float2 p = uv - _ClearCenter.xy; p.x *= _Aspect;
                float dist = length(p) * 2.0;
                float clearBubble = 1.0 - smoothstep(_NearClear, _FarFull, dist);
                clearBubble = saturate(clearBubble * (0.55 + 0.85 * fog));
                float density = saturate(_Density);
                float distBias = lerp(0.75, 1.0, saturate(dist * 0.6));
                float amt = density * lerp(_HazeMin, 1.0, fog) * (1.0 - clearBubble) * distBias;
                half3 col = _FogColor.rgb * (1.0 + fog * (_WispBright - 1.0));
                half a = saturate(amt * _HazeStrength);
                return half4(col, a);
            }

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

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
