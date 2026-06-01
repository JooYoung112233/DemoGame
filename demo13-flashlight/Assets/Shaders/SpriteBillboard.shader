Shader "BRB/SpriteBillboard"
{
    // SpriteRenderer 전용 빌보드 셰이더.
    // 3D 큐브 벽과 깊이 처리(ZWrite On + AlphaCutout).
    // 추가조명(손전등) 반응. 밤에도 최소 밝기 보장.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _AmbientMin ("Min Brightness (Night)", Range(0,1)) = 0.15

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineToggle ("Outline", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0.02, 0.02, 0.02, 1)
        _OutlineSize ("Outline Thickness", Range(0, 5)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "SpriteBillboard"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float4 _Flip;
                float _Cutoff;
                float _AmbientMin;
                float4 _OutlineColor;
                float _OutlineSize;
                float _EnableExternalAlpha;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 vertexColor : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.vertexColor = input.color;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                return output;
            }

            half3 CalcLighting(float3 posWS)
            {
                // 메인 라이트
                float4 shadowCoord = TransformWorldToShadowCoord(posWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lighting = mainLight.color * mainLight.shadowAttenuation;

                // 추가 라이트 (손전등 등)
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, posWS);
                    float atten = addLight.distanceAttenuation * addLight.shadowAttenuation;
                    lighting += addLight.color * atten;
                }
                #endif

                // 최소 밝기 보장 (밤에도 실루엣 보임)
                lighting = max(lighting, half3(_AmbientMin, _AmbientMin, _AmbientMin));

                return lighting;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 col = tex * _Color * input.vertexColor;

                // 보이는 픽셀
                if (col.a >= _Cutoff)
                {
                    col.rgb *= CalcLighting(input.positionWS);
                    return col;
                }

                // 아웃라인
                #ifdef _OUTLINE_ON
                    float2 texelSize = _MainTex_TexelSize.xy * _OutlineSize;
                    float2 offsets[8] = {
                        float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                        float2(-1,-1), float2(-1, 1), float2(1, -1), float2(1, 1)
                    };

                    float maxAlpha = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        float2 sampleUV = input.uv + offsets[j] * texelSize;
                        maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, sampleUV, 0).a);
                    }

                    if (maxAlpha >= _Cutoff)
                    {
                        half4 outline = _OutlineColor;
                        outline.rgb *= CalcLighting(input.positionWS);
                        return outline;
                    }
                #endif

                clip(-1);
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }

        // Shadow caster — 3D 벽 그림자에 스프라이트도 참여
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float4 _Flip;
                float _Cutoff;
                float _AmbientMin;
                float4 _OutlineColor;
                float _OutlineSize;
                float _EnableExternalAlpha;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return posCS;
            }

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(col.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
