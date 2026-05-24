Shader "InkCity/Prop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _LightBoost ("Light Boost", Range(1, 5)) = 2.0

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1.0

        [Header(Tiling)]
        _TileScaleX ("Tile Scale X", Range(0.1, 10)) = 1
        _TileScaleY ("Tile Scale Y", Range(0.1, 10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Prop"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                float _BumpScale;
                float4 _Color;
                float _Cutoff;
                float _Brightness;
                float _LightBoost;
                float _TileScaleX;
                float _TileScaleY;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                #ifdef _NORMALMAP
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                #endif
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * float2(_TileScaleX, _TileScaleY);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                #ifdef _NORMALMAP
                output.tangentWS = normalize(TransformObjectToWorldDir(input.tangentOS.xyz));
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                clip(mainTex.a - _Cutoff);

                half3 color = mainTex.rgb * _Color.rgb * _Brightness;

                // normal map
                float3 N = normalize(input.normalWS);
                #ifdef _NORMALMAP
                    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                    float3 T = normalize(input.tangentWS);
                    float3 B = normalize(input.bitangentWS);
                    N = normalize(T * normalTS.x + B * normalTS.y + N * normalTS.z);
                #endif

                // lighting: no NdotL (billboard mesh)
                half3 baseColor = color;
                half3 litColor = mainTex.rgb * _Color.rgb;
                half3 ambient = baseColor * 0.4;

                Light mainLight = GetMainLight();
                color = ambient + baseColor * mainLight.color;

                // additional lights (flashlight)
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    color += litColor * addAtten * addLight.color * _LightBoost;
                }
                #endif

                return half4(color, mainTex.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
