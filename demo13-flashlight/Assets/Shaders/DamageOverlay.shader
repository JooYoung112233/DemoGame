Shader "BRB/DamageOverlay"
{
    // 파괴/폐허 오버레이 — 베이스 스프라이트 위에 덧씌우는 자식 렌더러용.
    // 베이스 셰이더(WallPixel/PropPixel/FloorPixel/스프라이트-Lit 등)를 전혀 건드리지 않으므로
    // "모든 셰이더에 같이" 쓸 수 있다. Breakable.cs가 _Damage(0~1)를 단계별로 올린다.
    // 균열(veins) + 그을음/때(grime)를 절차적으로 생성(아트 불필요), 베이스 알파로 마스킹(실루엣 밖 안 그림).
    // Light2D 반응. Transparent. 픽셀화로 도트 일관.
    Properties
    {
        _MainTex ("Base Sprite (mask)", 2D) = "white" {}
        _Damage ("Damage (0~1)", Range(0, 1)) = 0
        _Intensity ("Intensity", Range(0, 2)) = 1.0
        _CrackColor ("Crack Color", Color) = (0.02, 0.02, 0.03, 1)
        _GrimeColor ("Grime Color", Color) = (0.15, 0.13, 0.10, 1)
        _CrackScale ("Crack Scale", Range(1, 32)) = 9
        _CrackSharp ("Crack Sharpness", Range(1, 12)) = 5
        _GrimeScale ("Grime Scale", Range(1, 32)) = 4
        _GrimeStrength ("Grime Strength", Range(0, 1)) = 0.55

        [Header(Lighting)]
        [Toggle(_LIT_ON)] _LitToggle ("Light2D 반응 ON", Float) = 1

        [Header(Pixelation)]
        [Toggle(_PIXELATE_ON)] _PixelateToggle ("픽셀화 ON", Float) = 1
        _PixelDensity ("Pixel Density (격자 수)", Range(8, 1024)) = 64
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
        }

        // ============ 3D URP Forward 패스 (폴백/프리뷰) ============
        Pass
        {
            Name "DamageOverlayForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off  ZWrite Off  Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _PIXELATE_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Damage;
                float _Intensity;
                float4 _CrackColor;
                float4 _GrimeColor;
                float _CrackScale;
                float _CrackSharp;
                float _GrimeScale;
                float _GrimeStrength;
                float _PixelDensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++) { s += a * VNoise(p); p *= 2.0; a *= 0.5; }
                return s;
            }
            float Ridged(float2 p) { return 1.0 - abs(2.0 * Fbm(p) - 1.0); }

            // 반환: rgb = 손상 색, a = 덮임(coverage)
            float4 ComputeDamage(float2 uv)
            {
                half baseA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                float2 puv = uv;
                #ifdef _PIXELATE_ON
                    float2 grid = float2(_PixelDensity, _PixelDensity);
                    puv = (floor(uv * grid) + 0.5) / grid;
                #endif
                float d = saturate(_Damage);
                float veins = pow(saturate(Ridged(puv * _CrackScale)), _CrackSharp);
                float thr = lerp(0.92, 0.30, d);
                float crack = smoothstep(thr, thr + 0.06, veins);
                float g = Fbm(puv * _GrimeScale);
                float grime = saturate(g - 0.35) * _GrimeStrength * d;
                float coverage = saturate(crack + grime) * baseA * _Intensity * step(0.0001, d);
                half3 col = lerp(_GrimeColor.rgb, _CrackColor.rgb, crack);
                return float4(col, coverage);
            }

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float3 positionWS : TEXCOORD1; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 dmg = ComputeDamage(input.uv);
                clip(dmg.a - 0.001);
                #ifdef _LIT_ON
                    half3 ambient = dmg.rgb * 0.4;
                    Light mainLight = GetMainLight();
                    half3 outc = ambient + dmg.rgb * mainLight.color;
                    return half4(outc, dmg.a * input.color.a);
                #else
                    return half4(dmg.rgb, dmg.a * input.color.a);
                #endif
            }
            ENDHLSL
        }

        // ============ URP 2D 렌더러 패스 (Light2D 반응) ============
        Pass
        {
            Name "DamageOverlay2D"
            Tags { "LightMode"="Universal2D" }
            Cull Off  ZWrite Off  Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert2d
            #pragma fragment frag2d
            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _PIXELATE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Damage;
                float _Intensity;
                float4 _CrackColor;
                float4 _GrimeColor;
                float _CrackScale;
                float _CrackSharp;
                float _GrimeScale;
                float _GrimeStrength;
                float _PixelDensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++) { s += a * VNoise(p); p *= 2.0; a *= 0.5; }
                return s;
            }
            float Ridged(float2 p) { return 1.0 - abs(2.0 * Fbm(p) - 1.0); }

            float4 ComputeDamage(float2 uv)
            {
                half baseA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                float2 puv = uv;
                #ifdef _PIXELATE_ON
                    float2 grid = float2(_PixelDensity, _PixelDensity);
                    puv = (floor(uv * grid) + 0.5) / grid;
                #endif
                float d = saturate(_Damage);
                float veins = pow(saturate(Ridged(puv * _CrackScale)), _CrackSharp);
                float thr = lerp(0.92, 0.30, d);
                float crack = smoothstep(thr, thr + 0.06, veins);
                float g = Fbm(puv * _GrimeScale);
                float grime = saturate(g - 0.35) * _GrimeStrength * d;
                float coverage = saturate(crack + grime) * baseA * _Intensity * step(0.0001, d);
                half3 col = lerp(_GrimeColor.rgb, _CrackColor.rgb, crack);
                return float4(col, coverage);
            }

            struct Attributes2D { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings2D { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; half2 lightingUV : TEXCOORD1; };

            Varyings2D vert2d(Attributes2D input)
            {
                Varyings2D o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                return o;
            }

            half4 frag2d(Varyings2D input) : SV_Target
            {
                float4 dmg = ComputeDamage(input.uv);
                clip(dmg.a - 0.001);
                half outA = dmg.a * input.color.a;
                #ifdef _LIT_ON
                    SurfaceData2D sd;
                    InputData2D id;
                    InitializeSurfaceData(dmg.rgb, outA, half4(1,1,1,1), sd);
                    InitializeInputData(input.uv, input.lightingUV, id);
                    return CombinedShapeLightShared(sd, id);
                #else
                    return half4(dmg.rgb, outA);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
