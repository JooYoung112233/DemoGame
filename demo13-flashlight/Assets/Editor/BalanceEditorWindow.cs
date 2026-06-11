using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 밸런스/콘텐츠 CSV를 유니티 안에서 격자로 편집 (Tools ▸ TopDown ▸ 밸런스 ▸ 밸런스 에디터).
/// tools/ 의 items·quests·region_loot + tools/balance/*.csv 를 같은 파일로 읽고 쓴다 → 엑셀과 양방향.
/// ⚠️ 셀 안에 콤마(,) 금지(구분자). 복수값은 ; / | / · 사용.
/// 편집 후 실제 SO 반영은 생성기(ItemPrices/UpdateItemPrices·GenerateRegionItems·ApplyBalance) 별도 실행.
/// </summary>
public class BalanceEditorWindow : EditorWindow
{
    static string ToolsDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../tools"));

    List<string> files;
    string current;
    List<string[]> rows;   // rows[0] = 헤더
    Vector2 scrollV, scrollH;
    bool dirty;
    float colW = 120f;

    [MenuItem("Tools/TopDown/밸런스/밸런스 에디터")]
    static void Open() => GetWindow<BalanceEditorWindow>("밸런스 에디터").minSize = new Vector2(760, 500);

    void OnEnable() => RefreshFiles();

    void RefreshFiles()
    {
        files = new List<string>();
        foreach (var f in new[] { "items.csv", "quests.csv", "region_loot.csv", "region_items.csv" })
        {
            var p = Path.Combine(ToolsDir, f);
            if (File.Exists(p)) files.Add(p);
        }
        var bdir = Path.Combine(ToolsDir, "balance");
        if (Directory.Exists(bdir))
            files.AddRange(Directory.GetFiles(bdir, "*.csv").OrderBy(x => x));
    }

    void Load(string path)
    {
        current = path;
        rows = new List<string[]>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 && rows.Count > 0) continue; // 빈 줄 스킵(헤더 뒤)
            rows.Add(line.Split(','));
        }
        int cols = rows.Count > 0 ? rows[0].Length : 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Length == cols) continue;
            var a = new string[cols];
            for (int c = 0; c < cols; c++) a[c] = c < rows[i].Length ? rows[i][c] : "";
            rows[i] = a;
        }
        dirty = false;
    }

    void Save()
    {
        var sb = new StringBuilder();
        foreach (var r in rows) sb.AppendLine(string.Join(",", r));
        File.WriteAllText(current, sb.ToString());
        dirty = false;
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("저장됨"));
    }

    void OnGUI()
    {
        if (files == null) RefreshFiles();
        EditorGUILayout.BeginHorizontal();

        // 좌: 파일 목록
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        EditorGUILayout.LabelField("CSV", EditorStyles.boldLabel);
        foreach (var f in files)
        {
            var name = Path.GetFileName(f);
            bool sel = current == f;
            if (GUILayout.Toggle(sel, name, "Button") && !sel)
            {
                if (dirty && !EditorUtility.DisplayDialog("저장 안 함", "변경을 버리고 이동할까요?", "버리기", "취소")) { }
                else Load(f);
            }
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("파일 새로고침")) RefreshFiles();
        EditorGUILayout.HelpBox("셀에 콤마 금지.\n복수값=; | ·\n저장 후 생성기로 SO 반영.", MessageType.None);
        EditorGUILayout.EndVertical();

        // 우: 격자
        EditorGUILayout.BeginVertical();
        if (rows == null)
        {
            EditorGUILayout.HelpBox("좌측에서 CSV를 선택하세요.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Path.GetFileName(current) + (dirty ? "  *" : ""), EditorStyles.boldLabel);
            colW = EditorGUILayout.Slider(colW, 60f, 240f, GUILayout.Width(200));
            GUI.enabled = dirty;
            if (GUILayout.Button("저장", GUILayout.Width(60))) Save();
            GUI.enabled = true;
            if (GUILayout.Button("되돌리기", GUILayout.Width(70))) Load(current);
            if (GUILayout.Button("행 추가", GUILayout.Width(70))) { rows.Add(new string[rows[0].Length]); dirty = true; }
            EditorGUILayout.EndHorizontal();

            scrollV = EditorGUILayout.BeginScrollView(scrollV);
            for (int r = 0; r < rows.Count; r++)
            {
                EditorGUILayout.BeginHorizontal();
                bool header = r == 0;
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (header)
                    {
                        EditorGUILayout.LabelField(rows[r][c], EditorStyles.boldLabel, GUILayout.Width(colW));
                    }
                    else
                    {
                        var nv = EditorGUILayout.TextField(rows[r][c], GUILayout.Width(colW));
                        if (nv != rows[r][c]) { rows[r][c] = nv; dirty = true; }
                    }
                }
                if (!header && GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    rows.RemoveAt(r); dirty = true; GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
}
