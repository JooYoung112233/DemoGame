Shader "BRB/PlayerSprite"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Columns ("Columns", Float) = 2
        _Rows ("Rows", Float) = 8
        _CurrentFrame ("Current Frame", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineToggle ("Outline", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0.02, 0.02, 0.02, 1)
        _OutlineSize ("Outline Size", Range(0, 5)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "PlayerSprite"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float _Columns;
                float _Rows;
                float _CurrentFrame;
                float _Cutoff;
                float4 _OutlineColor;
                float _OutlineSize;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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
                float2 rawUV : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
            };

            // sprite sheet UV calculation
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
                output.rawUV = input.uv;
                output.uv = calcSheetUV(input.uv);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col *= _Color;

                // lighting: no NdotL (billboard sprite)
                Light mainLight = GetMainLight();
                half3 lighting = half3(0.4, 0.4, 0.4) + mainLight.color;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    lighting += addAtten * addLight.color;
                }
                #endif

                // visible pixel: apply lighting and return
                if (col.a >= _Cutoff)
                {
                    col.rgb *= lighting;
                    return col;
                }

                // outline check
                #ifdef _OUTLINE_ON
                    float2 texelSize = _MainTex_TexelSize.xy * _OutlineSize;
                    float2 offsets[8] = {
                        float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                        float2(-1, -1), float2(-1, 1), float2(1, -1), float2(1, 1)
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
                        outline.rgb *= lighting;
                        return outline;
                    }
                #endif

                clip(-1);
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }

        // ============ URP 2D 렌더러 패스 (Light2D 반응) ============
        Pass
        {
            Name "PlayerSprite2D"
            Tags { "LightMode"="Universal2D" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert2d
            #pragma fragment frag2d
            #pragma shader_feature_local _OUTLINE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float _Columns;
                float _Rows;
                float _CurrentFrame;
                float _Cutoff;
                float4 _OutlineColor;
                float _OutlineSize;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes2D
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings2D
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half2 lightingUV : TEXCOORD1;
            };

            float2 calcSheetUV2D(float2 uv)
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

            Varyings2D vert2d(Attributes2D input)
            {
                Varyings2D o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = calcSheetUV2D(input.uv);
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                return o;
            }

            half4 light2d(half3 albedo, half alpha, float2 uv, half2 lightingUV)
            {
                SurfaceData2D sd;
                InputData2D id;
                InitializeSurfaceData(albedo, alpha, half4(1,1,1,1), sd);
                InitializeInputData(uv, lightingUV, id);
                return CombinedShapeLightShared(sd, id);
            }

            half4 frag2d(Varyings2D input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                if (col.a >= _Cutoff)
                    return light2d(col.rgb, col.a, input.uv, input.lightingUV);

                #ifdef _OUTLINE_ON
                    float2 texelSize = _MainTex_TexelSize.xy * _OutlineSize;
                    float2 offsets[8] = {
                        float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                        float2(-1, -1), float2(-1, 1), float2(1, -1), float2(1, 1)
                    };
                    float maxAlpha = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        float2 sampleUV = input.uv + offsets[j] * texelSize;
                        maxAlpha = max(maxAlpha, SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, sampleUV, 0).a);
                    }
                    if (maxAlpha >= _Cutoff)
                        return light2d(_OutlineColor.rgb, _OutlineColor.a, input.uv, input.lightingUV);
                #endif

                clip(-1);
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
