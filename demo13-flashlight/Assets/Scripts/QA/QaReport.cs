using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// QA 리포트 — 이상 항목 수집 + 파일 출력. (docs/qa.md)
/// 빌드/에디터 공용. 봇이 도는 동안 Unity 로그(Error/Exception)도 자동 수집한다.
/// </summary>
public class QaReport
{
    public enum Level { Info, Warn, Error }

    public struct Entry
    {
        public Level level;
        public string step;     // 어느 단계에서
        public string kind;     // 이상 종류(STUCK, ITEM_LOST, EXCEPTION …)
        public string msg;
        public float time;
    }

    readonly List<Entry> _entries = new List<Entry>();
    readonly Dictionary<string, float> _metrics = new Dictionary<string, float>();

    public int ErrorCount { get; private set; }
    public int WarnCount  { get; private set; }
    public IReadOnlyList<Entry> Entries => _entries;

    public void Add(Level lv, string step, string kind, string msg)
    {
        _entries.Add(new Entry { level = lv, step = step, kind = kind, msg = msg, time = Time.realtimeSinceStartup });
        if (lv == Level.Error) ErrorCount++;
        else if (lv == Level.Warn) WarnCount++;

        string line = $"[QA:{lv}] {step} · {kind} — {msg}";
        if (lv == Level.Error) Debug.LogError(line);
        else if (lv == Level.Warn) Debug.LogWarning(line);
        else Debug.Log(line);
    }

    public void Info(string step, string kind, string msg)  => Add(Level.Info, step, kind, msg);
    public void Warn(string step, string kind, string msg)  => Add(Level.Warn, step, kind, msg);
    public void Error(string step, string kind, string msg) => Add(Level.Error, step, kind, msg);

    /// <summary>밸런스/성능 지표 기록(리포트 하단에 표로 출력).</summary>
    public void Metric(string key, float value) => _metrics[key] = value;

    /// <summary>기록된 지표 — 결과 JSON에 실어야 밖(마더·자동루프)에서 단계 판정을 할 수 있다.
    /// 2026-07-28까지 직렬화에서 빠져 있어 '발견율' 같은 값이 밖에서 항상 0으로 보였다.</summary>
    public IReadOnlyDictionary<string, float> Metrics => _metrics;

    public string Build(string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine($" {title}");
        sb.AppendLine($" 시각: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}   유니티: {Application.unityVersion}");
        sb.AppendLine($" 플랫폼: {Application.platform}   에디터: {Application.isEditor}");
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine($" 결과: 오류 {ErrorCount} · 경고 {WarnCount} · 전체 {_entries.Count}건");
        sb.AppendLine();

        if (_metrics.Count > 0)
        {
            sb.AppendLine("── 지표 ──");
            foreach (var kv in _metrics) sb.AppendLine($"  {kv.Key,-34} {kv.Value:0.###}");
            sb.AppendLine();
        }

        sb.AppendLine("── 로그 ──");
        foreach (var e in _entries)
            sb.AppendLine($"  [{e.time,7:0.0}s] {e.level,-5} {e.step,-14} {e.kind,-16} {e.msg}");

        return sb.ToString();
    }

    /// <summary>리포트 파일 저장. 반환 = 저장 경로(실패 시 null).</summary>
    public string Save(string title, string fileName)
    {
        string text = Build(title);
        try
        {
            string dir = Application.persistentDataPath;
            string path = System.IO.Path.Combine(dir, fileName);
            System.IO.File.WriteAllText(path, text);
            Debug.Log($"[QA] 리포트 저장: {path}");
            return path;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[QA] 리포트 저장 실패: {ex.Message}");
            return null;
        }
    }
}
