#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// tools/traits.csv → TraitData SO를 **AssetDatabase로 직접 생성**(손저작 YAML 대체).
/// Unity가 직접 SO를 만들어 import·GUID·YAML 문제를 원천 차단한다.
/// 기존 Traits/*.asset(손저작 깨진 것 포함)을 전부 지우고 41개 재생성.
/// 메뉴: Tools/TopDown/Data/특성 SO 재생성(CSV→AssetDatabase)
/// </summary>
public static class TraitDataImporter
{
    const string CsvRel = "tools/traits.csv";
    const string OutDir = "Assets/Resources/Data/Traits";

    // 메뉴 폐지 — 밸런스·컨트롤 패널 ▸ 도구·검증 탭에서 호출.
    public static void Generate()
    {
        string csvPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", CsvRel));
        if (!File.Exists(csvPath)) { Debug.LogError($"[TraitDataImporter] CSV 없음: {csvPath}"); return; }
        if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

        // 1) 기존 .asset 전부 삭제(깨진 손저작 에셋 포함) — DeleteAsset이 .meta도 제거
        foreach (var f in Directory.GetFiles(OutDir, "*.asset"))
            AssetDatabase.DeleteAsset(f.Replace('\\', '/'));

        // 2) CSV → CreateInstance + CreateAsset
        var lines = File.ReadAllLines(csvPath);
        int created = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 1; i < lines.Length; i++)   // 헤더 스킵
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;
                var c = line.Split(',');
                if (c.Length < 9) { Debug.LogWarning($"[TraitDataImporter] 컬럼 부족(행 {i + 1}): {line}"); continue; }

                var t = ScriptableObject.CreateInstance<TraitData>();
                t.traitId       = c[0].Trim();
                t.category      = (TraitCategory)ParseInt(c[1]);
                t.tier          = (TraitTier)ParseInt(c[2]);
                t.ppCost        = ParseInt(c[3]);
                t.prereqTraitId = c[4].Trim();
                t.tradeoff      = ParseInt(c[5]) != 0;
                t.displayName   = c[6].Trim();
                t.description   = "";
                t.effectSummary = c[7].Trim();
                t.effects       = ParseEffects(c[8].Trim());

                AssetDatabase.CreateAsset(t, $"{OutDir}/{ToPascal(t.traitId)}.asset");
                created++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ [TraitDataImporter] 특성 SO {created}개 생성 → {OutDir}");
    }

    static int ParseInt(string s) { int.TryParse(s.Trim(), out var v); return v; }

    static List<TraitEffect> ParseEffects(string s)
    {
        var list = new List<TraitEffect>();
        if (string.IsNullOrEmpty(s)) return list;
        foreach (var part in s.Split(';'))
        {
            var p = part.Trim();
            if (p.Length == 0) continue;
            var kv = p.Split(':');
            if (kv.Length < 3) continue;
            float.TryParse(kv[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var val);
            list.Add(new TraitEffect { effectKey = kv[0].Trim(), op = kv[1].Trim(), value = val });
        }
        return list;
    }

    static string ToPascal(string id)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var p in id.Split('_'))
            if (p.Length > 0) sb.Append(char.ToUpper(p[0])).Append(p.Substring(1));
        return sb.ToString();
    }
}
#endif
