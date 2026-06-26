#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI 시안 스프라이트(`Assets/Resources/UI/Image/`)를 Sprite + 9-slice 보더로 일괄 셋업. (ui-prefab-plan §4-B 스킨)
/// 패널/버튼이 9-slice로 늘어나도 종이 테두리가 보존되도록. 보더는 시작값 — Sprite Editor에서 미세조정 가능.
/// 스킨 입히기 전에 1회 실행. (UISkin.Slice가 Resources.Load<Sprite>로 읽음)
/// </summary>
public static class UIAssetSetup
{
    const string ImageDir = "Assets/Resources/UI/Image";

    // Unity spriteBorder = Vector4(Left, Bottom, Right, Top).
    // 찢긴 종이/큰 프레임은 보더를 크게 잡아 모서리 찢김이 늘어나지 않게(이미지 크기 비례).
    static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "item",            new Vector4(90, 90, 90, 90) },  // 440x650 ITEM NAME 큰 크림 종이 — 모서리 찢김 보존
        { "storage",         new Vector4(90, 90, 90, 90) },  // 516x730 STORAGE 큰 어두운 프레임
        { "box",             new Vector4(26, 26, 26, 26) },  // 76x76  소형 패널 프레임(자원바 배경 등)
        { "itembox",         new Vector4(30, 34, 30, 34) },  // 90x100 아이콘 박스
        { "storage_box",     new Vector4(26, 26, 26, 26) },  // 92x92  격자 셀
        { "btn",             new Vector4(28, 18, 28, 18) },  // 232x50 PRIMARY 버튼(가로 9-slice)
        { "btnb",            new Vector4(28, 18, 28, 18) },  // 232x50 SECONDARY 버튼
        { "name",            new Vector4(34, 16, 34, 16) },  // 118x50 제목/자원 태그(찢긴 종이, 가로 신축)
        { "name_icon",       new Vector4(36, 36, 36, 36) },  // 168x166 아이콘 태그
        { "title",           new Vector4(40, 18, 40, 18) },  // 234x50 제목 배너
        { "title2",          new Vector4(34, 20, 34, 20) },  // 170x60 제목 배너2
        { "storage_btn_on",  new Vector4(20, 18, 20, 18) },  // 70x54 탭 on
        { "storage_btn_off", new Vector4(20, 18, 20, 18) },  // 70x54 탭 off
    };

    [MenuItem("Tools/TopDown/UI/9-slice 자산 셋업")]
    public static void Setup()
    {
        int done = 0, miss = 0;
        foreach (var kv in Borders)
        {
            string path = $"{ImageDir}/{kv.Key}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning($"[UIAssetSetup] 스킵(임포터 없음): {path}"); miss++; continue; }

            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.textureType = TextureImporterType.Sprite;
            s.spriteMode = (int)SpriteImportMode.Single;
            s.spriteMeshType = SpriteMeshType.FullRect;   // 9-slice는 FullRect 필요
            s.spritePixelsPerUnit = 100f;
            s.alphaIsTransparency = true;
            s.mipmapEnabled = false;
            s.wrapMode = TextureWrapMode.Clamp;
            s.filterMode = FilterMode.Bilinear;
            imp.SetTextureSettings(s);

            imp.spriteBorder = kv.Value;
            imp.textureCompression = TextureImporterCompression.Uncompressed; // UI 선명도

            EditorUtility.SetDirty(imp);
            imp.SaveAndReimport();
            done++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[UIAssetSetup] 9-slice/스프라이트 설정 완료 — 적용 {done}, 누락 {miss}. 보더는 Sprite Editor에서 미세조정 가능.");
    }
}
#endif
