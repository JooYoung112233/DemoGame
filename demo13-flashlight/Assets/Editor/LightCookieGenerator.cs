#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// URP 2D <b>Sprite(쿠키) 라이트</b>용 텍스쳐를 절차적으로 생성한다.
/// Point/Spot 파라미터 라이트는 경계가 수학적(딱딱)이라 다크우드 느낌이 안 남 →
/// 부드러운 그라데이션 쿠키를 Sprite 라이트에 물려 "텍스쳐 라이트"로 쓴다.
///
/// 생성물(흰색 RGB + 알파=빛 모양, 빛 색은 Light2D.color로 틴트):
///   • Resources/LightCookies/cookie_radial.png — 원형 풀(전구·램프·창문·플레이어 주변광). 피벗 중앙.
///   • Resources/LightCookies/cookie_cone.png   — 부채꼴(플레이어 시야·스탠드·스포트). +Y 방향, 피벗 하단중앙(=원점이 콘 꼭지).
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Generate Light Cookies
/// </summary>
public static class LightCookieGenerator
{
    const int   COOKIE_SIZE = 256;
    const float NOISE       = 0.12f;   // 살짝 결 입히기(0=매끈)

    const string DIR          = "Assets/Resources/LightCookies";
    public const string RadialPath = DIR + "/cookie_radial.png";
    public const string ConePath   = DIR + "/cookie_cone.png";
    // Resources.Load 용 경로(확장자·Resources 접두 제외)
    public const string RadialResource = "LightCookies/cookie_radial";
    public const string ConeResource   = "LightCookies/cookie_cone";

    [MenuItem("Tools/TopDown/맵/조명 쿠키 생성")]
    public static void Generate()
    {
        int S = COOKIE_SIZE;
        WriteCookie("cookie_radial", RadialPixels(S, NOISE), S, new Vector2(0.5f, 0.5f), custom: false);
        WriteCookie("cookie_cone",   ConePixels(S, NOISE),   S, new Vector2(0.5f, 0f),   custom: true);
        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan>[LightCookie]</color> cookie_radial / cookie_cone 생성 → {DIR}\n" +
                  "Sprite 라이트(Light2D)에 물려 부드러운 텍스쳐 라이트로 사용. 빛 색은 Light2D.color로.");
    }

    /// <summary>쿠키가 없으면 생성(플레이어/프롭 빌더가 먼저 호출).</summary>
    public static void EnsureCookies()
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(RadialPath) != null &&
            AssetDatabase.LoadAssetAtPath<Sprite>(ConePath) != null) return;
        Generate();
    }

    // ── 픽셀 생성 ──────────────────────────────────────────────────────

    /// <summary>원형: 중앙 밝고 가장자리로 부드럽게 사라짐(smoothstep) + 살짝 노이즈.</summary>
    static Color[] RadialPixels(int S, float noiseAmt)
    {
        var px = new Color[S * S];
        float nf = 4f / S;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float u = (x + 0.5f) / S * 2f - 1f;
            float v = (y + 0.5f) / S * 2f - 1f;
            float r = Mathf.Sqrt(u * u + v * v);
            float t = Mathf.Clamp01(1f - r);
            float a = t * t * (3f - 2f * t);                 // smoothstep
            if (noiseAmt > 0f)
            {
                float n = Mathf.PerlinNoise(x * nf + 3.1f, y * nf + 1.7f);
                a *= Mathf.Lerp(1f, n, noiseAmt);
            }
            px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        }
        return px;
    }

    /// <summary>부채꼴: 하단중앙(꼭지)에서 +Y로 퍼지는 콘. 각도·길이 falloff + 꼭지 전구광 + 노이즈.</summary>
    static Color[] ConePixels(int S, float noiseAmt)
    {
        var px = new Color[S * S];
        float nf = 4f / S;
        const float innerHalf = 10f, outerHalf = 34f;        // 콘 반각(도)
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dx = (x + 0.5f) / S - 0.5f;                // -0.5..0.5
            float dy = (y + 0.5f) / S;                       // 0(하단=꼭지)..1
            float a = 0f;
            if (dy > 1e-4f)
            {
                float ang  = Mathf.Atan2(Mathf.Abs(dx), dy) * Mathf.Rad2Deg;   // 0=정면(+Y)
                float angT = 1f - Mathf.Clamp01((ang - innerHalf) / (outerHalf - innerHalf));
                float angA = angT * angT * (3f - 2f * angT);
                float lenT = 1f - Mathf.Clamp01((dy - 0.2f) / 0.8f);            // 0.2까지 풀, 끝으로 fade
                float lenA = lenT * lenT * (3f - 2f * lenT);
                a = angA * lenA;
            }
            float dApex = Mathf.Sqrt(dx * dx + dy * dy);                       // 꼭지 전구광
            float g = Mathf.Clamp01(1f - dApex / 0.14f);
            g = g * g * (3f - 2f * g);
            a = Mathf.Max(a, g * 0.7f);
            if (noiseAmt > 0f)
            {
                float n = Mathf.PerlinNoise(x * nf + 5.2f, y * nf + 2.3f);
                a *= Mathf.Lerp(1f, n, noiseAmt);
            }
            px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        }
        return px;
    }

    // ── 저장 + 임포트 설정 ─────────────────────────────────────────────

    static void WriteCookie(string fileName, Color[] pixels, int S, Vector2 pivot, bool custom)
    {
        EnsureFolder(DIR);
        string path = $"{DIR}/{fileName}.png";

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType         = TextureImporterType.Sprite;
        ti.spriteImportMode    = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = S;                          // 256px = 1 월드 유닛 → 스케일로 크기 조절
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled       = false;
        ti.wrapMode            = TextureWrapMode.Clamp;
        ti.filterMode          = FilterMode.Bilinear;
        ti.textureCompression  = TextureImporterCompression.Uncompressed;     // 그라데이션 깨짐 방지

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)(custom ? SpriteAlignment.Custom : SpriteAlignment.Center);
        s.spritePivot     = pivot;
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf   = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
