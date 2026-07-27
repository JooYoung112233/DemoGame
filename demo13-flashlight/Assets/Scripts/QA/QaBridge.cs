using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QA ↔ Claude 브리지 — **파일 기반 요청/응답 채널**. (docs/qa.md §브리지)
///
/// 설계 원칙: **게임 빌드에 API 키를 넣지 않는다.** 게임이 직접 Anthropic API를 부르면
/// 키가 빌드에 박히고(추출 가능) 비용·지연·오프라인 문제가 생긴다. 대신
///   게임(QA 봇) ──파일──▶ 개발 머신의 Claude Code ──파일──▶ 게임
/// 형태로 주고받는다. QA 머신이 여러 대여도 같은 폴더/공유 경로만 보면 된다.
///
/// 파일 (모두 `Application.persistentDataPath`, 경로는 Console에 찍힌다):
///   • `qa-session-&lt;stamp&gt;.json` — 매 런 종료 시 기계가 읽는 전체 결과(지표·이상·환경)
///   • `qa-blocked.json`           — 봇이 **막혔을 때** 쓰고 대기(재현 정보 포함)
///   • `qa-resume.json`            — Claude/사람이 답을 넣으면 봇이 읽고 진행
///
/// 대기(handshake)는 옵트인이다(`QaScenarioDef` 없이도 동작). 응답이 없으면 타임아웃 후
/// `skip`으로 간주하고 계속 — 무인 실행이 멈추지 않게.
/// </summary>
public static class QaBridge
{
    public const string BlockedFile = "qa-blocked.json";
    public const string ResumeFile  = "qa-resume.json";

    public static string Dir => Application.persistentDataPath;
    public static string PathOf(string file) => System.IO.Path.Combine(Dir, file);

    // ── 세션 리포트(기계 판독용) ─────────────────────────────────────

    [System.Serializable]
    public class SessionJson
    {
        public string instance;
        public string commandId;
        public string scenario;
        public int seed;
        public int cyclesPlanned;
        public int cyclesCompleted;
        public string unityVersion;
        public string platform;
        public bool isEditor;
        public string startedAt;
        public string endedAt;
        public float durationSec;

        /// <summary>PASS / FAIL — GUI 통계의 기본 단위.</summary>
        public string verdict = "PASS";
        /// <summary>판정 근거(왜 FAIL인지).</summary>
        public string verdictReason = "";
        /// <summary>세부 판정 — 항목별 통과/실패(GUI에서 막대로 표시).</summary>
        public List<CheckJson> checks = new List<CheckJson>();

        public int errorCount;
        public int warnCount;
        public List<QaCycleMetrics> cycles = new List<QaCycleMetrics>();
        public List<AnomalyJson> anomalies = new List<AnomalyJson>();
        /// <summary>공간 셀 데이터 — "어디서" 분석용(파밍효율·스턱·미방문).</summary>
        public List<QaHeatmap.CellJson> cells = new List<QaHeatmap.CellJson>();
    }

    /// <summary>항목별 통과 판정 — "루프가 도는가", "루팅이 나오는가" 같은 체크 하나.</summary>
    [System.Serializable]
    public class CheckJson
    {
        public string name;
        public bool passed;
        public string detail;
    }

    [System.Serializable]
    public class AnomalyJson
    {
        public string level;    // Info/Warn/Error
        public string step;
        public string kind;     // STUCK, ITEM_LOST, EXCEPTION …
        public string msg;
        public float atSec;
        public string scene;
        public string playerPos;
    }

    /// <summary>런 종료 시 기계 판독용 JSON 저장. 반환 = 경로.</summary>
    public static string WriteSession(SessionJson s)
    {
        try
        {
            string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string path = PathOf($"qa-session-{stamp}.json");
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(s, true));
            // 최신본 고정 이름으로도 남긴다 — Claude가 항상 같은 경로를 보면 되게.
            System.IO.File.WriteAllText(PathOf("qa-session-latest.json"), JsonUtility.ToJson(s, true));
            Debug.Log($"[QA] 세션 JSON 저장: {path}\n[QA] 최신본: {PathOf("qa-session-latest.json")}");
            return path;
        }
        catch (System.Exception e) { Debug.LogError($"[QA] 세션 저장 실패: {e.Message}"); return null; }
    }

    // ── 막힘 요청/응답 ───────────────────────────────────────────────

    [System.Serializable]
    public class BlockedJson
    {
        public string kind;         // 왜 막혔나
        public string step;
        public string message;
        public string scene;
        public string playerPos;
        public int cycle;
        public string scenario;
        public int seed;
        public string askedAt;
        /// <summary>봇이 스스로 시도해 본 것 — Claude가 중복 제안 안 하게.</summary>
        public string tried;
        /// <summary>가능한 응답 값 안내.</summary>
        public string expect = "qa-resume.json 에 {\"action\":\"retry|skip|abort\",\"note\":\"...\"} 를 쓰세요";
    }

    [System.Serializable]
    public class ResumeJson
    {
        public string action;   // retry | skip | abort
        public string note;
    }

    /// <summary>막힘을 알리고 응답을 기다린다. 타임아웃이면 skip.
    /// (게임을 멈추지 않기 위해 코루틴으로 폴링 — 무인 실행에서도 영원히 안 멈춘다.)</summary>
    public static IEnumerator RequestHelp(BlockedJson req, float timeoutSec, System.Action<ResumeJson> onAnswer)
    {
        string blockedPath = PathOf(BlockedFile);
        string resumePath  = PathOf(ResumeFile);

        // 이전 응답 잔재 제거 — 지난 런의 답을 잘못 집어오지 않게.
        try { if (System.IO.File.Exists(resumePath)) System.IO.File.Delete(resumePath); } catch { }

        req.askedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        try { System.IO.File.WriteAllText(blockedPath, JsonUtility.ToJson(req, true)); }
        catch (System.Exception e) { Debug.LogError($"[QA] 막힘 파일 쓰기 실패: {e.Message}"); }

        Debug.LogWarning($"[QA] ⛔ 막힘 — Claude 응답 대기 (최대 {timeoutSec:0}초)\n" +
                         $"  요청: {blockedPath}\n  응답: {resumePath}\n" +
                         $"  Claude Code에서 `/qa` 실행하면 진단 후 응답을 씁니다.");

        float t = 0f;
        ResumeJson answer = null;
        while (t < timeoutSec)
        {
            if (System.IO.File.Exists(resumePath))
            {
                // 쓰는 도중 읽는 경합 방지 — 실패하면 다음 프레임 재시도.
                try
                {
                    string txt = System.IO.File.ReadAllText(resumePath);
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        answer = JsonUtility.FromJson<ResumeJson>(txt);
                        if (answer != null && !string.IsNullOrEmpty(answer.action)) break;
                    }
                }
                catch { }
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (answer == null)
        {
            answer = new ResumeJson { action = "skip", note = "타임아웃 — 응답 없음" };
            Debug.LogWarning("[QA] 응답 타임아웃 → skip으로 계속 진행");
        }
        else Debug.Log($"[QA] ✅ 응답 수신: action={answer.action} note={answer.note}");

        try { if (System.IO.File.Exists(blockedPath)) System.IO.File.Delete(blockedPath); } catch { }
        onAnswer?.Invoke(answer);
    }
}
