Shader "BRB/SpineLitURP"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Shadow Alpha Cutoff", Range(0,1)) = 0.1

        [Header(Lighting)]
        _AmbientMin ("Ambient Min (Night)", Range(0, 1)) = 0.4
        _LightInfluence ("Light Influence", Range(0, 1)) = 1.0

        [Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 1

        [Header(Occlusion Outline)]
        _OcclusionColor ("Occlusion Color", Color) = (0.3, 0.8, 1.0, 0.5)
        _OcclusionOutlineWidth ("Outline Width", Range(0, 8)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "SpineLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _STRAIGHT_ALPHA_INPUT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Cutoff;
                float _AmbientMin;
                float _LightInfluence;
                float4 _OcclusionColor;
                float _OcclusionOutlineWidth;
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
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Spine vertex color
                half4 col = tex * input.color * _Color;

                #ifdef _STRAIGHT_ALPHA_INPUT
                    col.rgb *= col.a;
                #endif

                if (col.a < 0.01)
                    discard;

                // ---- lighting (no NdotL - Spine meshes are flat) ----
                Light mainLight = GetMainLight();

                // main light contribution (no NdotL for flat Spine mesh)
                half3 lightColor = mainLight.color;

                // additional lights (flashlight) — distance attenuation only
                half3 addLightColor = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    addLightColor += addLight.color * addAtten;
                }
                #endif

                // final lighting: ambient + (main + additional) * influence
                half3 totalLight = lightColor + addLightColor;
                half3 lighting = max(_AmbientMin, totalLight * _LightInfluence + _AmbientMin);

                col.rgb *= lighting;

                return col;
            }
            ENDHLSL
        }

        // ---- Occlusion Outline Pass ----
        // Renders only where depth test FAILS (player behind something)
        // Samples neighboring texels to find edge → draws outline only
        Pass
        {
            Name "OcclusionOutline"

            ZTest Greater
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vertOcc
            #pragma fragment fragOcc
            #pragma shader_feature_local _STRAIGHT_ALPHA_INPUT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Cutoff;
                float _AmbientMin;
                float _LightInfluence;
                float4 _OcclusionColor;
                float _OcclusionOutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

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
                float4 color : COLOR;
            };

            Varyings vertOcc(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 fragOcc(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float alpha = tex.a * input.color.a;

                // inside the character: draw semi-transparent fill
                if (alpha > _Cutoff)
                {
                    half4 fill = _OcclusionColor;
                    fill.a *= 0.15; // very subtle inner fill
                    return fill;
                }

                // outside: check if we're near the edge → outline
                float2 texelSize = _MainTex_TexelSize.xy * _OcclusionOutlineWidth;
                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                    float2(-1, -1), float2(-1, 1), float2(1, -1), float2(1, 1)
                };

                float maxAlpha = 0;
                for (int j = 0; j < 8; j++)
                {
                    float2 sampleUV = input.uv + offsets[j] * texelSize;
                    float sA = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, sampleUV, 0).a;
                    maxAlpha = max(maxAlpha, sA * input.color.a);
                }

                if (maxAlpha > _Cutoff)
                    return _OcclusionColor; // outline pixel

                discard;
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
