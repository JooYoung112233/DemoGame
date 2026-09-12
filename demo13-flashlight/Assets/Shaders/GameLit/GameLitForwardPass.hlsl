#ifndef BRB_GAMELIT_FORWARD_INCLUDED
#define BRB_GAMELIT_FORWARD_INCLUDED

// BRB/GameLit forward pass — matte lighting for painted Comic B textures (2026-09-13).
//
// 조명 계산은 URP의 UniversalFragmentPBR에 맡긴다. 그러면 옛 셰이더가 빠졌던 함정 두 개가 구조적으로 사라진다:
//   ① 추가 광원 그림자 — URP 내부가 그림자를 받는 GetAdditionalLight 오버로드를 쓴다(2인자 함정 없음).
//   ② Forward+ — URP 내부가 LIGHT_LOOP_BEGIN/END(_CLUSTER_LIGHT_LOOP)로 돈다(개수 0 함정 없음).
// Painted mode preserves texture ink and hue; legacy grime/desaturation/rim remain opt-in.

// Specular workflow lets painted surfaces set reflectance to zero without replacing
// URP's light loops. Shadows, light cookies, Forward+ and additional lights stay native.
#define _SPECULAR_SETUP 1
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 룩 A/B 비교용 전역 스위치 — Shader.SetGlobalFloat("_GameLitLookOff", 1)이면 때·그늘 채도·림을 끄고 순수 PBR만 남긴다.
// 머티리얼을 건드리지 않고 켜고 끌 수 있게 전역이다(기본 0 = 룩 켜짐 — 설정 안 한 전역 float는 0이다).
float _GameLitLookOff;

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3  normalWS   : TEXCOORD2;
    half4  tangentWS  : TEXCOORD3;   // xyz = 접선, w = 부호
    float3 positionOS : TEXCOORD4;   // 캐릭터의 때는 몸에 붙어 따라다녀야 한다(월드 좌표면 걸을 때 흐른다)
    half   fogFactor  : TEXCOORD5;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings GameLitVertex(Attributes input)
{
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    o.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    o.positionWS = pos.positionWS;
    o.positionOS = input.positionOS.xyz;
    o.normalWS = nrm.normalWS;
    o.tangentWS = half4(nrm.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    o.fogFactor = ComputeFogFactor(pos.positionCS.z);
    o.positionCS = pos.positionCS;
    return o;
}

// ── 때(grime) — 절차적 먼지. 텍스처가 없어도 "낡은 세계"가 읽히게 ─────────────────
float GameHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float GameNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(GameHash(i + float3(0, 0, 0)), GameHash(i + float3(1, 0, 0)), f.x),
                     lerp(GameHash(i + float3(0, 1, 0)), GameHash(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(GameHash(i + float3(0, 0, 1)), GameHash(i + float3(1, 0, 1)), f.x),
                     lerp(GameHash(i + float3(0, 1, 1)), GameHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

/// 0~1. 얼룩 무늬가 진한 곳 · 벽 밑동(바닥 가까운 **세운 면**)일수록 더럽다.
/// ⚠️ 2026-09-11 1차 값(낮은 주파수 + 바닥·윗면 가산)은 바닥 전체에 큰 얼룩이 깔려 **흙이 아니라 구름·안개**처럼 보였다.
///    → 무늬를 잘게(두 옥타브 모두 고주파), 바닥 가산은 세운 면(벽 밑동)에만, 윗면 가산은 약하게, 문턱을 올렸다.
half GameGrime(float3 positionWS, float3 positionOS, half3 normalWS)
{
    if (_GrimeStrength <= 0.0h) return 0.0h;
    float3 p = (_GrimeObjectSpace > 0.5h ? positionOS : positionWS) * _GrimeScale;
    float n = GameNoise(p) * 0.55 + GameNoise(p * 2.7 + 7.3) * 0.45;
    half vertical = 1.0h - saturate(abs(normalWS.y));   // 세운 면일수록 1
    half ground = (1.0h - saturate(positionWS.y / max(_GroundGrimeHeight, 0.01h))) * vertical;
    half up = saturate(normalWS.y);
    return saturate((n - 0.45h) * 1.6h + ground * 0.45h + up * 0.08h);
}

half4 GameLitFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.uv;
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    half3 albedo = albedoAlpha.rgb * _BaseColor.rgb;
    half comic = saturate(_ComicLighting);

    half3 normalWS = normalize(input.normalWS);
    #if defined(_NORMALMAP)
        half bump = lerp(_BumpScale, min(_BumpScale, 0.12h), comic);
        half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), bump);
        half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
        normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
    #endif

    // ① 때 — 색은 흙빛으로, 표면은 무광으로
    half look = (1.0h - saturate(_GameLitLookOff)) * (1.0h - comic);
    half grime = GameGrime(input.positionWS, input.positionOS, normalWS) * _GrimeStrength * look;
    albedo = lerp(albedo, albedo * _GrimeColor.rgb, grime);

    half occlusion = 1.0h;
    #if defined(_OCCLUSIONMAP)
        half aoStrength = lerp(_OcclusionStrength, min(_OcclusionStrength, 0.35h), comic);
        occlusion = lerp(1.0h, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g, aoStrength);
    #endif

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    inputData.fogCoord = input.fogFactor;
    inputData.bakedGI = SampleSH(normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1, 1, 1, 1);

    SurfaceData surface = (SurfaceData)0;
    surface.albedo = albedo;
    surface.alpha = alpha;
    surface.metallic = _Metallic;
    surface.smoothness = _Smoothness * (1.0h - grime * 0.7h);
    // Explicit opt-in keeps legacy materials unchanged, including materials that
    // still carry an unused URP metallic-map keyword from an earlier conversion.
    #if defined(_GAMELIT_PACKED_MASK)
        half4 packedSurface = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
        surface.metallic *= packedSurface.r;
        surface.smoothness *= packedSurface.a;
    #endif
    // Preserve the old metallic material's reflectance at zero strength, then remove
    // reflective highlights for inked textures. No emission/ambient floor is added:
    // an unlit area remains dark, including the backpack and nighttime interiors.
    surface.specular = lerp(lerp(half3(0.04h, 0.04h, 0.04h), albedo, surface.metallic), half3(0, 0, 0), comic);
    surface.smoothness *= 1.0h - comic;
    surface.normalTS = half3(0, 0, 1);
    surface.occlusion = occlusion;
    #if defined(_EMISSION)
        surface.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    #endif

    // ② 조명 — URP 그대로(주광·추가 광원 그림자·Forward+·SSAO·반사 프로브)
    half4 color = UniversalFragmentPBR(inputData, surface);

    // ③ 빛이 닿는 곳만 살아난다 — 받은 빛이 적을수록 채도를 뺀다(어둠은 잿빛, 빛 속은 제 색)
    half albedoLum = max(Luminance(albedo), 0.02h);
    half lit = saturate(Luminance(color.rgb) / albedoLum);
    color.rgb = lerp(color.rgb, Luminance(color.rgb).xxx, saturate(_ShadowDesaturation * (1.0h - lit)) * look);

    // ④ 실루엣 림 — 어둠 속에서도 형태가 읽히게(캐릭터용, 환경은 0)
    half rim = pow(1.0h - saturate(dot(normalWS, inputData.viewDirectionWS)), 4.0h) * _RimStrength * look;
    color.rgb += rim * _RimColor.rgb;

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = alpha;
    return color;
}

#endif
