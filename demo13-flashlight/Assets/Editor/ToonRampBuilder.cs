#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 카툰 **라이트 램프** 텍스처를 만든다 — `BRB/Toon`의 `_RampTex`가 읽는다.
///
/// 램프가 왜 필요한가: 밴딩은 "밝기"만 계단으로 자른다. 그래서 그림자가 **같은 색의 어두움**이
/// 되고, 그건 그냥 어두운 3D지 카툰이 아니다. 램프는 밝기 축(가로)마다 **색을 따로** 지정한다 —
/// 밝은 쪽은 따뜻하게, 어두운 쪽은 차갑게(보랏빛). 이 색 전이가 Flat Kit류 룩의 실제 정체다.
///
/// 가로 = 어두움(왼쪽) → 밝음(오른쪽). 세로는 의미 없음(4px).
/// 결과: `Assets/Resources/Shaders/ToonRamp.png` — Resources라 런타임에 이름으로 불러 쓴다.
///
/// 값을 바꾸고 싶으면 아래 Stops를 고치고 메뉴를 다시 실행하면 된다.
/// 포토샵에서 직접 그린 램프로 갈아 끼워도 된다(그게 원래 쓰는 방식이다).
/// </summary>
public static class ToonRampBuilder
{
    const string Dir  = "Assets/Resources/Shaders";
    const string Path = Dir + "/ToonRamp.png";
    const int Width = 256, Height = 4;

    /// <summary>램프의 색 구간. (경계 위치 0~1, 그 구간의 색).
    /// 3단 — 그림자(차가운 보랏빛) / 중간(중성) / 하이라이트(따뜻함).
    /// 경계는 <see cref="EdgeSoft"/>만큼만 풀어 카툰의 딱 떨어지는 단을 유지한다.</summary>
    static readonly (float at, Color color)[] Stops =
    {
        // "어두운 붕괴된 사회" 톤 — 그늘은 차갑고 깊게, 빛은 희지 않고 탁하게(나트륨등/먼지).
        // 밝은 카툰이 아니라 **어둠 속에서 형태만 읽히는** 룩이 목표다.
        // ⚠️ 그늘 값의 하한이 중요하다. 0.13까지 떨어뜨렸더니 **원래 어두운 재질**(소매 0.27,
        //    방망이 0.34)이 곱해져 0.2 밑으로 가라앉아, 밝은 면 옆에서 통째로 검게 읽혔다
        //    (사용자 지적: "팔뚝이 검정색으로 꽉 찬다"). 어둡되 **형태는 읽히는** 선이 0.30 부근이다.
        (0.00f, new Color(0.30f, 0.32f, 0.38f)),   // 깊은 그늘 — 차가운 남색, 바닥은 있다
        (0.38f, new Color(0.55f, 0.56f, 0.55f)),   // 중간 — 바랜 회녹색(부패)
        (0.70f, new Color(0.98f, 0.93f, 0.82f)),   // 빛 — 탁한 온백색, 순백 아님
    };
    const float EdgeSoft = 0.02f;   // 단 경계 폭(0이면 완전히 각짐 → 곡면에서 지글거린다)

    [MenuItem("Tools/TopDown/개발/카툰 램프 텍스처 생성")]
    public static void Build()
    {
        var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true);
        var row = new Color[Width];
        for (int x = 0; x < Width; x++)
        {
            float t = x / (float)(Width - 1);
            row[x] = Sample(t);
        }
        for (int y = 0; y < Height; y++) tex.SetPixels(0, y, Width, 1, row);
        tex.Apply();

        System.IO.Directory.CreateDirectory(Dir);
        System.IO.File.WriteAllBytes(Path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);

        // 임포트 설정이 중요하다 — 밉맵·압축·반복이 켜져 있으면 램프가 뭉개지거나
        // 양 끝이 반대편으로 감겨 그림자와 하이라이트가 섞인다.
        var imp = (TextureImporter)AssetImporter.GetAtPath(Path);
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        Debug.Log($"[ToonRamp] 생성 — {Path} ({Width}×{Height}, 구간 {Stops.Length}단)");
        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("카툰 램프", $"{Path}\n{Stops.Length}단 램프 생성 완료.", "확인");
    }

    static Color Sample(float t)
    {
        // 마지막 구간부터 거꾸로 훑으며, 경계에서만 부드럽게 섞는다.
        Color c = Stops[0].color;
        for (int i = 1; i < Stops.Length; i++)
        {
            float k = Mathf.SmoothStep(Stops[i].at - EdgeSoft, Stops[i].at + EdgeSoft, t);
            c = Color.Lerp(c, Stops[i].color, k);
        }
        return c;
    }
}
#endif
