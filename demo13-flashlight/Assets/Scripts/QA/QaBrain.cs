using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 봇의 **판단** — 정해진 순서를 재생하는 대신 상황을 보고 목표를 고른다.
///
/// <para><b>왜 유틸리티(점수) 방식인가.</b> Claude가 교정하려면 **왜 그렇게 정했는지 읽혀야** 한다.
/// 신경망이면 사람도 Claude도 못 고친다. 그래서 모든 결정을 후보·점수·선택으로 남기고,
/// 1·2위 점수가 붙으면(<see cref="Policy.ambiguousMargin"/>) **스스로 애매하다고 신고**한다.</para>
///
/// <para><b>학습 = 교사 교정.</b> 경사하강이 아니다.
/// <c>qa-policy.json</c>(가중치·임계) + <c>qa-cases.json</c>(조건→정답 행동)을 읽어 판단하고,
/// Claude가 그 두 파일을 고쳐주면 다음 런부터 반영된다. 같은 헛짓을 반복하지 않게 된다.</para>
/// </summary>
public class QaBrain
{
    public enum Goal { Explore, LootCrate, LootCorpse, FightEnemy, EnterBuilding, LeaveBuilding, Extract, Flee, ClearPassage }

    // ── 정책(가중치·임계) — Claude가 고치는 파일 ─────────────────────────
    [System.Serializable]
    public class Policy
    {
        public float fleeHp = 0.35f;          // 이 아래면 무조건 도주
        public float fightHp = 0.5f;          // 이 아래면 교전 회피
        public float extractWeight = 0.9f;    // 무게 이 비율 넘으면 탈출 압력
        public float extractTime = 0.8f;      // 레이드 시간 이만큼 쓰면 탈출 압력
        public float wLoot = 1.0f;
        public float wCorpse = 1.1f;          // 시체는 눈앞의 확정 이득 — 상자보다 우선
        public float wFight = 0.6f;
        public float wExplore = 0.35f;
        public float wExtract = 0.9f;
        public float wLeaveWhenDone = 0.8f;   // 실내에 볼 일이 끝났을 때 '나가기' 점수(탐색보다 높아야 한다)
        public float wEnterBuilding = 0.75f;  // 건물 진입 — 2026-07-28 커밋 b2b3937로 **루트가 실내로 옮겨졌다**.
                                              // 밖만 돌면 파밍 자체가 불가능하므로 탐색보다 높아야 한다.
        public float wClearPassage = 0.65f;   // 막힌 통로 — "빠른 길이지만 시끄럽다" 대 "돌아간다"의 선택.
                                              // 평소엔 낮게 깔아두고, 실제로 가려던 방향이 막혔을 때만 확 올린다.
        public float ambiguousMargin = 0.06f; // 1·2위 차가 이보다 작으면 '애매하다'고 신고
    }

    // ── 사례(조건 → 정답) — Claude가 추가하는 파일 ───────────────────────
    [System.Serializable]
    public class Case
    {
        public string id;
        public string doGoal;      // Goal 이름
        public string why;
        public float hpMax = -1f;        // 조건: 체력 <= (미사용 -1)
        public float weightMin = -1f;    // 조건: 무게 >=
        public float exitDistMax = -1f;  // 조건: 탈출구 거리 <=
        public float timeMin = -1f;      // 조건: 시간 소진율 >=
        public int knownCratesMax = -1;  // 조건: 아는 상자 <=
        public string inScene;           // 조건: 씬 이름(부분 일치)
    }

    [System.Serializable] public class CaseFile { public List<Case> cases = new List<Case>(); }

    /// <summary>판단에 쓰는 상태 — 전부 AI가 **알 수 있는** 값만.</summary>
    public struct State
    {
        public float hp;                // 0~1
        public float weight;            // 0~1 (현재/최대)
        public int knownCrates;         // 아직 안 연, **본 적 있는** 상자
        public float crateDist;         // -1 = 없음
        public float corpseDist;        // -1 = 없음
        public float enemyDist;         // -1 = 안 보임
        public float exitDist;          // -1 = 모름
        public float doorDist;          // -1 = 아는 건물 입구 없음 (실외에서만 의미)
        public float passageDist;       // -1 = 아는 미해결 막힌 통로 없음
        public bool blockedRecently;    // 최근 이동이 막혔는가(moveFailStreak > 0)
        public float timeUsed;          // 0~1
        public bool inInterior;
        public string scene;
    }

    public Policy Pol = new Policy();
    public readonly List<Case> Cases = new List<Case>();

    // ── 지속 학습 (런 중에 배운다) ───────────────────────────────────────
    //
    // 예전엔 런이 끝나야 Claude가 고쳐주고 다음 판에 반영됐다. 그러면 한 판 내내
    // 같은 헛짓을 반복한다. 그래서 **결과를 즉시 반영**한다:
    //   • 목표를 실행한 결과(성공/실패)로 그 목표의 신뢰도를 깎거나 올린다.
    //   • 실패가 쌓인 목표는 점수가 눌려 자연스럽게 다른 선택지로 넘어간다.
    //   • 성공하면 회복한다(영구 봉인 금지 — 상황이 바뀌면 다시 유효하다).
    readonly Dictionary<Goal, float> _trust = new Dictionary<Goal, float>();
    readonly Dictionary<Goal, int> _fails = new Dictionary<Goal, int>();
    readonly Dictionary<Goal, int> _wins = new Dictionary<Goal, int>();

    float Trust(Goal g) => _trust.TryGetValue(g, out float t) ? t : 1f;

    /// <summary>목표 실행 결과를 즉시 반영한다. 실행부가 매번 불러야 한다.</summary>
    public void Outcome(Goal g, bool success, string detail = null)
    {
        float t = Trust(g);
        if (success)
        {
            _wins[g] = _wins.TryGetValue(g, out int w) ? w + 1 : 1;
            t = Mathf.Min(1f, t + 0.25f);          // 회복은 빠르게
        }
        else
        {
            _fails[g] = _fails.TryGetValue(g, out int f) ? f + 1 : 1;
            t = Mathf.Max(0.15f, t * 0.55f);       // 실패는 강하게 눌러 다른 선택지로
        }
        _trust[g] = t;
        _learn.Add($"{{\"t\":{Time.realtimeSinceStartup:0.0},\"goal\":\"{g}\",\"ok\":{(success ? "true" : "false")}," +
                   $"\"trust\":{t:0.##}" + (detail != null ? $",\"why\":\"{Escape(detail)}\"" : "") + "}");
    }

    public string TrustSummary()
    {
        var sb = new StringBuilder();
        foreach (var kv in _trust)
            sb.Append($"{kv.Key} {kv.Value:0.##}(승{(_wins.TryGetValue(kv.Key, out int w) ? w : 0)}/패{(_fails.TryGetValue(kv.Key, out int f) ? f : 0)}) ");
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "학습 없음";
    }

    static string Escape(string s) => s == null ? "" : s.Replace("\\", "/").Replace("\"", "'");

    readonly List<string> _learn = new List<string>();

    // ── 교사 교정 실시간 반영 ────────────────────────────────────────────
    string _polPath, _casePath;
    long _polStamp, _caseStamp;
    float _reloadTimer;

    /// <summary>정책·사례 파일이 바뀌면 **런 도중에도** 다시 읽는다.
    /// Claude가 플레이 중에 고쳐 넣으면 그 판부터 바로 적용된다(다음 판까지 기다리지 않는다).</summary>
    public bool PollReload(float dt)
    {
        _reloadTimer += dt;
        if (_reloadTimer < 2f) return false;
        _reloadTimer = 0f;

        bool changed = false;
        try
        {
            if (_polPath != null && System.IO.File.Exists(_polPath))
            {
                long st = System.IO.File.GetLastWriteTimeUtc(_polPath).Ticks;
                if (st != _polStamp)
                {
                    _polStamp = st;
                    var p = JsonUtility.FromJson<Policy>(System.IO.File.ReadAllText(_polPath));
                    if (p != null) { Pol = p; changed = true; }
                }
            }
            if (_casePath != null && System.IO.File.Exists(_casePath))
            {
                long st = System.IO.File.GetLastWriteTimeUtc(_casePath).Ticks;
                if (st != _caseStamp)
                {
                    _caseStamp = st;
                    var cf = JsonUtility.FromJson<CaseFile>(System.IO.File.ReadAllText(_casePath));
                    Cases.Clear();
                    if (cf?.cases != null) Cases.AddRange(cf.cases);
                    changed = true;
                }
            }
        }
        catch { }
        return changed;
    }

    readonly List<string> _decisions = new List<string>();
    readonly List<string> _asks = new List<string>();
    public int AskCount => _asks.Count;
    public int DecisionCount => _decisions.Count;

    // ── 파일 IO ──────────────────────────────────────────────────────────
    public void Load(string policyPath, string casesPath)
    {
        _polPath = policyPath;
        _casePath = casesPath;
        try
        {
            if (System.IO.File.Exists(policyPath)) _polStamp = System.IO.File.GetLastWriteTimeUtc(policyPath).Ticks;
            if (System.IO.File.Exists(casesPath)) _caseStamp = System.IO.File.GetLastWriteTimeUtc(casesPath).Ticks;
        }
        catch { }

        try
        {
            if (System.IO.File.Exists(policyPath))
            {
                var p = JsonUtility.FromJson<Policy>(System.IO.File.ReadAllText(policyPath));
                if (p != null) Pol = p;
            }
            else System.IO.File.WriteAllText(policyPath, JsonUtility.ToJson(Pol, true));  // 기본값을 심어둔다
        }
        catch (System.Exception e) { Debug.LogWarning($"[QaBrain] 정책 로드 실패: {e.Message}"); }

        try
        {
            if (System.IO.File.Exists(casesPath))
            {
                var cf = JsonUtility.FromJson<CaseFile>(System.IO.File.ReadAllText(casesPath));
                if (cf?.cases != null) Cases.AddRange(cf.cases);
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[QaBrain] 사례 로드 실패: {e.Message}"); }
    }

    /// <summary>기록을 파일로 — 런 도중에도 주기적으로 부른다(끝나야 보이면 실시간 교정을 못 한다).</summary>
    public void Flush(string decisionsPath, string asksPath, string learnPath = null)
    {
        try { System.IO.File.WriteAllText(decisionsPath, string.Join("\n", _decisions)); } catch { }
        try
        {
            if (_asks.Count > 0)
                System.IO.File.WriteAllText(asksPath, "{\"asks\":[" + string.Join(",", _asks) + "]}");
        }
        catch { }
        try
        {
            if (learnPath != null && _learn.Count > 0)
                System.IO.File.WriteAllText(learnPath, string.Join("\n", _learn));
        }
        catch { }
    }

    // ── 판단 ─────────────────────────────────────────────────────────────

    /// <summary>사례가 맞으면 그 행동을 그대로 쓴다(Claude가 준 정답이 점수보다 우선).</summary>
    Case MatchCase(in State s)
    {
        foreach (var c in Cases)
        {
            if (c == null || string.IsNullOrEmpty(c.doGoal)) continue;
            if (c.hpMax >= 0f && s.hp > c.hpMax) continue;
            if (c.weightMin >= 0f && s.weight < c.weightMin) continue;
            if (c.exitDistMax >= 0f && (s.exitDist < 0f || s.exitDist > c.exitDistMax)) continue;
            if (c.timeMin >= 0f && s.timeUsed < c.timeMin) continue;
            if (c.knownCratesMax >= 0 && s.knownCrates > c.knownCratesMax) continue;
            if (!string.IsNullOrEmpty(c.inScene) && (s.scene == null || !s.scene.Contains(c.inScene))) continue;
            return c;
        }
        return null;
    }

    public Goal Decide(in State s, out string reason, out bool ambiguous)
    {
        var scores = new Dictionary<Goal, float>();

        // 생존이 먼저 — 체력이 바닥이면 다른 건 의미 없다
        scores[Goal.Flee] = s.hp <= Pol.fleeHp ? 2f : 0f;

        // 탈출 압력: 무게가 차거나 시간이 다하면 급등(사람이 그렇게 판단한다)
        float pressure = Mathf.Max(
            s.weight >= Pol.extractWeight ? (s.weight - Pol.extractWeight) / Mathf.Max(0.01f, 1f - Pol.extractWeight) : 0f,
            s.timeUsed >= Pol.extractTime ? (s.timeUsed - Pol.extractTime) / Mathf.Max(0.01f, 1f - Pol.extractTime) : 0f);
        scores[Goal.Extract] = s.exitDist >= 0f ? Pol.wExtract * pressure : 0f;

        // 건물 안에서 나가는 이유는 둘이다.
        //   ① 탈출해야 하는데 실내엔 탈출구가 없다(압력 전달)
        //   ② **볼 일이 끝났다** — 열 상자도, 시체도, 적도 없다. 사람은 그러면 나온다.
        // ②가 없어서 2026-07-28 AI가 실내에서 벽만 보고 돌았다(문이 멀쩡히 있는데도).
        float indoorDone = (s.inInterior && s.knownCrates == 0 && s.corpseDist < 0f && s.enemyDist < 0f)
            ? Pol.wLeaveWhenDone : 0f;
        scores[Goal.LeaveBuilding] = s.inInterior
            ? Mathf.Max(Pol.wExtract * pressure * 0.99f, indoorDone) : 0f;

        // 시체 = 눈앞의 확정 이득
        scores[Goal.LootCorpse] = s.corpseDist >= 0f
            ? Pol.wCorpse * Falloff(s.corpseDist, 12f) * (1f - s.weight) : 0f;

        // 상자 = 아는 것만. 무게가 찰수록 매력 감소
        scores[Goal.LootCrate] = (s.knownCrates > 0 && s.crateDist >= 0f)
            ? Pol.wLoot * Falloff(s.crateDist, 25f) * (1f - s.weight) : 0f;

        // 교전은 체력이 받쳐줄 때만. 멀면 굳이 찾아가지 않는다
        scores[Goal.FightEnemy] = (s.enemyDist >= 0f && s.hp > Pol.fightHp)
            ? Pol.wFight * Falloff(s.enemyDist, 10f) : 0f;

        // 건물 진입 — 2026-07-28 커밋 b2b3937로 **루트가 실내로 옮겨졌다**.
        // 밖만 돌면 파밍이 원천 불가능하므로, 밖에 열 상자가 없을수록 들어갈 이유가 커진다.
        scores[Goal.EnterBuilding] = (!s.inInterior && s.doorDist >= 0f && pressure < 0.5f)
            ? Pol.wEnterBuilding * Falloff(s.doorDist, 30f) * (1f - s.weight)
              * (s.knownCrates > 0 ? 0.5f : 1f)
            : 0f;

        // 막힌 통로 — 콘텐츠 선택지다("빠른 길이지만 시끄럽다" vs "조용히 돌아간다").
        // 평소엔 낮게 깔려 있다가, 실제로 가려던 방향이 막혔을 때(blockedRecently) 확 오른다.
        scores[Goal.ClearPassage] = s.passageDist >= 0f
            ? Pol.wClearPassage * Falloff(s.passageDist, 20f) * (s.blockedRecently ? 1f : 0.3f)
            : 0f;

        // 탐색 — 아는 게 없을 때의 유일한 수단. 이미 아는 상자가 많으면 낮춘다
        scores[Goal.Explore] = Pol.wExplore * (s.knownCrates > 0 ? 0.5f : 1f);

        // ★ 런 중 학습 반영 — 방금 실패한 목표는 눌리고, 통한 목표는 살아난다.
        //   생존(Flee)은 학습으로 깎지 않는다(죽으면 배울 기회도 없다).
        var keys = new List<Goal>(scores.Keys);
        foreach (var g in keys)
            if (g != Goal.Flee) scores[g] *= Trust(g);

        // 1·2위
        Goal best = Goal.Explore; float b1 = -1f, b2 = -1f;
        foreach (var kv in scores)
        {
            if (kv.Value > b1) { b2 = b1; b1 = kv.Value; best = kv.Key; }
            else if (kv.Value > b2) b2 = kv.Value;
        }

        float margin = b1 - Mathf.Max(0f, b2);
        ambiguous = margin < Pol.ambiguousMargin && b1 > 0f;

        var hit = MatchCase(s);
        if (hit != null && System.Enum.TryParse(hit.doGoal, out Goal forced))
        {
            reason = $"사례 '{hit.id}' 적용 — {hit.why}";
            Log(s, scores, forced, margin, ambiguous: false, caseId: hit.id);
            return forced;
        }

        reason = $"{best} 점수 {b1:0.00} (2위와 차 {margin:0.00})";
        Log(s, scores, best, margin, ambiguous, null);
        if (ambiguous) Ask(s, scores, best, margin);
        return best;
    }

    /// <summary>가까울수록 1에 가깝고 멀수록 0으로 — 거리를 매력으로 바꾼다.</summary>
    static float Falloff(float dist, float span) => Mathf.Clamp01(1f - dist / Mathf.Max(1f, span));

    // ── 기록 ─────────────────────────────────────────────────────────────
    static string Json(in State s, Dictionary<Goal, float> sc, Goal chose, float margin)
    {
        var sb = new StringBuilder();
        sb.Append("{\"scene\":\"").Append(s.scene).Append("\",\"state\":{")
          .Append("\"hp\":").Append(s.hp.ToString("0.##"))
          .Append(",\"weight\":").Append(s.weight.ToString("0.##"))
          .Append(",\"knownCrates\":").Append(s.knownCrates)
          .Append(",\"crateDist\":").Append(s.crateDist.ToString("0.#"))
          .Append(",\"corpseDist\":").Append(s.corpseDist.ToString("0.#"))
          .Append(",\"enemyDist\":").Append(s.enemyDist.ToString("0.#"))
          .Append(",\"exitDist\":").Append(s.exitDist.ToString("0.#"))
          .Append(",\"doorDist\":").Append(s.doorDist.ToString("0.#"))
          .Append(",\"passageDist\":").Append(s.passageDist.ToString("0.#"))
          .Append(",\"blockedRecently\":").Append(s.blockedRecently ? "true" : "false")
          .Append(",\"timeUsed\":").Append(s.timeUsed.ToString("0.##"))
          .Append(",\"inInterior\":").Append(s.inInterior ? "true" : "false")
          .Append("},\"scores\":{");
        bool first = true;
        foreach (var kv in sc)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(kv.Key).Append("\":").Append(kv.Value.ToString("0.###"));
        }
        sb.Append("},\"chose\":\"").Append(chose).Append("\",\"margin\":").Append(margin.ToString("0.###"));
        return sb.ToString();
    }

    void Log(in State s, Dictionary<Goal, float> sc, Goal chose, float margin, bool ambiguous, string caseId)
    {
        var line = Json(s, sc, chose, margin)
                 + ",\"ambiguous\":" + (ambiguous ? "true" : "false")
                 + (caseId != null ? ",\"case\":\"" + caseId + "\"" : "")
                 + ",\"t\":" + Time.realtimeSinceStartup.ToString("0.0") + "}";
        _decisions.Add(line);
        if (_decisions.Count > 2000) _decisions.RemoveAt(0);
    }

    void Ask(in State s, Dictionary<Goal, float> sc, Goal chose, float margin)
    {
        _asks.Add(Json(s, sc, chose, margin)
                + ",\"question\":\"1·2위 점수가 붙어 어느 쪽이 맞는지 모르겠다. 이 상황의 정답 행동과 이유를 qa-cases.json에 추가해줘.\"}");
    }

    /// <summary>결과 요약(리포트용).</summary>
    public string Summary()
    {
        var count = new Dictionary<string, int>();
        foreach (var d in _decisions)
        {
            int i = d.IndexOf("\"chose\":\"");
            if (i < 0) continue;
            int a = i + 9, b = d.IndexOf('"', a);
            if (b < 0) continue;
            string g = d.Substring(a, b - a);
            count[g] = count.TryGetValue(g, out int n) ? n + 1 : 1;
        }
        var sb = new StringBuilder();
        foreach (var kv in count) sb.Append(kv.Key).Append(' ').Append(kv.Value).Append(" · ");
        return sb.Length > 0 ? sb.ToString(0, sb.Length - 3) : "결정 없음";
    }
}
