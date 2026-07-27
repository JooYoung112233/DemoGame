using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 공간 텔레메트리 — **"어디서"** 데이터를 뽑는다. (docs/qa.md §공간 분석)
///
/// 사용자 요구: *"어디서 반복하면 재미없다 / 어디서 파밍하면 재미없다 / 어디서 멈춘다 / 어디선 진행이 안 된다"*
/// 사이클 총합(소지금·XP)만으론 이 질문에 답이 안 나온다 → 맵을 격자로 쪼개 셀마다 누적한다.
///
/// 셀 누적: 방문 횟수 · 체류 시간 · 획득 가치/개수 · 스턱 · 사망 · 처음 밟은 사이클
/// 분석 산출:
///   ① 파밍 효율 랭킹 (가치/분) — 낮은 곳 = "여기 파밍하면 재미없다"
///   ② 신규성 곡선 (사이클별 처음 밟는 셀 수) — 0으로 수렴 = "N판째부터 새로운 게 없다"
///   ③ 스턱 핫스팟 (같은 셀 반복) — 산발이 아니면 지오메트리 버그
///   ④ 커버리지 + ASCII 히트맵 — 아무도 안 가는 공간이 어디인지 눈으로
/// </summary>
public class QaHeatmap
{
    public float CellSize = 16f;   // 격자 한 칸(m). Zone1이 330×344이므로 ~21×22 격자.

    class Cell
    {
        public int gx, gy;
        public int visits;          // 다른 셀에서 넘어온 횟수
        public float dwellSec;
        public int lootValue, lootCount;
        public int stuck, deaths, unreachable;
        public int firstCycle = -1;
        public int lastCycle = -1;
    }

    class SceneHeat
    {
        public readonly Dictionary<long, Cell> cells = new Dictionary<long, Cell>();
        public int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        public readonly Dictionary<int, int> newCellsPerCycle = new Dictionary<int, int>();
        public readonly Dictionary<int, int> samplesPerCycle = new Dictionary<int, int>();
        public readonly Dictionary<int, int> revisitPerCycle = new Dictionary<int, int>();
    }

    readonly Dictionary<string, SceneHeat> _scenes = new Dictionary<string, SceneHeat>();
    long _lastKey = long.MinValue;
    string _lastScene = "";

    static long Key(int gx, int gy) => ((long)gx << 32) ^ (uint)gy;

    SceneHeat Heat(string scene)
    {
        if (!_scenes.TryGetValue(scene, out var h)) { h = new SceneHeat(); _scenes[scene] = h; }
        return h;
    }

    Cell At(string scene, Vector2 pos, int cycle)
    {
        var h = Heat(scene);
        int gx = Mathf.FloorToInt(pos.x / CellSize);
        int gy = Mathf.FloorToInt(pos.y / CellSize);
        long k = Key(gx, gy);

        if (!h.cells.TryGetValue(k, out var c))
        {
            c = new Cell { gx = gx, gy = gy, firstCycle = cycle };
            h.cells[k] = c;
            if (cycle > 0) h.newCellsPerCycle[cycle] = h.newCellsPerCycle.TryGetValue(cycle, out var n) ? n + 1 : 1;
        }
        else if (cycle > 0 && c.lastCycle != cycle)
        {
            // 이전 사이클에 이미 밟았던 셀을 다시 밟음 = 반복
            h.revisitPerCycle[cycle] = h.revisitPerCycle.TryGetValue(cycle, out var r) ? r + 1 : 1;
        }

        c.lastCycle = cycle;
        h.minX = Mathf.Min(h.minX, gx); h.maxX = Mathf.Max(h.maxX, gx);
        h.minY = Mathf.Min(h.minY, gy); h.maxY = Mathf.Max(h.maxY, gy);
        return c;
    }

    // ── 수집 ─────────────────────────────────────────────────────────

    /// <summary>주기적 위치 샘플(체류·이동 추적). dt = 직전 샘플로부터 경과.</summary>
    public void Sample(string scene, Vector2 pos, float dt, int cycle)
    {
        var c = At(scene, pos, cycle);
        c.dwellSec += dt;
        var h = Heat(scene);
        h.samplesPerCycle[cycle] = h.samplesPerCycle.TryGetValue(cycle, out var s) ? s + 1 : 1;

        long k = Key(c.gx, c.gy);
        if (k != _lastKey || scene != _lastScene) { c.visits++; _lastKey = k; _lastScene = scene; }
    }

    public void AddLoot(string scene, Vector2 pos, int value, int count, int cycle)
    { var c = At(scene, pos, cycle); c.lootValue += value; c.lootCount += count; }

    public void AddStuck(string scene, Vector2 pos, int cycle) { At(scene, pos, cycle).stuck++; }
    public void AddDeath(string scene, Vector2 pos, int cycle) { At(scene, pos, cycle).deaths++; }
    public void AddUnreachable(string scene, Vector2 pos, int cycle) { At(scene, pos, cycle).unreachable++; }

    public bool HasData
    {
        get { foreach (var kv in _scenes) if (kv.Value.cells.Count > 0) return true; return false; }
    }

    // ── 분석 ─────────────────────────────────────────────────────────

    /// <summary>이상 신호를 리포트에 올린다(사용자가 못 보는 것을 대신 짚어주는 부분).</summary>
    public void Analyze(QaReport rep)
    {
        foreach (var kv in _scenes)
        {
            string scene = kv.Key;
            var h = kv.Value;
            if (h.cells.Count == 0) continue;

            // ① 파밍 효율 — 오래 머물렀는데 소득이 없는 셀
            var dwelt = new List<Cell>();
            foreach (var c in h.cells.Values) if (c.dwellSec >= 3f) dwelt.Add(c);
            if (dwelt.Count >= 3)
            {
                dwelt.Sort((a, b) => Rate(a).CompareTo(Rate(b)));
                var worst = dwelt[0];
                float worstRate = Rate(worst);
                float total = 0f; foreach (var c in dwelt) total += Rate(c);
                float avg = total / dwelt.Count;

                if (worst.dwellSec >= 8f && worst.lootValue == 0)
                    rep.Warn("map", "DEAD_ZONE",
                        $"[{scene}] {Pos(worst)} 근처에서 {worst.dwellSec:0}초 머물렀는데 소득 0 — 파밍 가치 없는 구역(루트 앵커 추가 or 동선에서 제외 검토)");

                if (avg > 0f && worstRate < avg * 0.25f && worst.dwellSec >= 5f)
                    rep.Info("map", "LOW_YIELD",
                        $"[{scene}] {Pos(worst)} 효율 {worstRate:0.#}/분 (맵 평균 {avg:0.#}/분의 {(worstRate / avg * 100):0}%)");
            }

            // ② 신규성 — 사이클이 늘수록 새로 밟는 셀이 줄어드는가
            if (h.newCellsPerCycle.Count >= 2)
            {
                var cyclesSorted = new List<int>(h.newCellsPerCycle.Keys); cyclesSorted.Sort();
                int firstC = cyclesSorted[0], lastC = cyclesSorted[cyclesSorted.Count - 1];
                int firstN = h.newCellsPerCycle[firstC], lastN = h.newCellsPerCycle[lastC];

                if (lastN == 0)
                    rep.Warn("map", "NOVELTY_ZERO",
                        $"[{scene}] 사이클 {lastC}에서 **처음 밟는 구역이 0** — 이 판부터는 새로 볼 게 없음(반복 지루함 시작점)");
                else if (firstN > 0 && lastN < firstN * 0.25f)
                    rep.Warn("map", "NOVELTY_DECAY",
                        $"[{scene}] 신규 구역 {firstN}→{lastN}칸으로 급감(사이클 {firstC}→{lastC}) — 반복성 한계 근접");
            }

            // ③ 스턱 핫스팟 — 같은 셀에 몰리면 지오메트리 문제
            foreach (var c in h.cells.Values)
            {
                if (c.stuck >= 2)
                    rep.Error("map", "STUCK_HOTSPOT",
                        $"[{scene}] {Pos(c)} 에서 스턱 {c.stuck}회 반복 — 콜라이더 구멍·끼임 지점 의심(좌표 확인 필요)");
                if (c.unreachable >= 2)
                    rep.Error("map", "BLOCKED_HOTSPOT",
                        $"[{scene}] {Pos(c)} 부근에서 목표 도달 실패 {c.unreachable}회 — 길이 막혀 진행 불가한 구간");
                if (c.deaths >= 2)
                    rep.Warn("map", "DEATH_HOTSPOT", $"[{scene}] {Pos(c)} 에서 {c.deaths}회 사망 — 난이도 스파이크");
            }

            // ④ 커버리지 — 돌아다닌 범위 대비 실제 밟은 칸
            int spanX = h.maxX - h.minX + 1, spanY = h.maxY - h.minY + 1;
            int box = Mathf.Max(1, spanX * spanY);
            float cov = h.cells.Count * 100f / box;
            rep.Metric($"[{scene}] 방문 칸 수", h.cells.Count);
            rep.Metric($"[{scene}] 이동 범위 커버리지(%)", cov);
            if (cov < 35f && box >= 20)
                rep.Warn("map", "SPARSE_PATH",
                    $"[{scene}] 이동 범위({spanX}×{spanY}칸) 중 {cov:0}%만 밟음 — 동선이 한 줄로 쏠렸거나 맵이 과대(빈 공간 과다)");
        }
    }

    static float Rate(Cell c) => c.dwellSec > 0f ? c.lootValue / (c.dwellSec / 60f) : 0f;
    string Pos(Cell c) => $"({c.gx * CellSize:0}~{(c.gx + 1) * CellSize:0}, {c.gy * CellSize:0}~{(c.gy + 1) * CellSize:0})";

    // ── 리포트 ───────────────────────────────────────────────────────

    public string BuildReport()
    {
        var sb = new StringBuilder();
        foreach (var kv in _scenes)
        {
            var h = kv.Value;
            if (h.cells.Count == 0) continue;
            sb.AppendLine($"── 공간 분석: {kv.Key} (셀 {CellSize:0}m) ──");
            sb.AppendLine(Ascii(h));
            sb.AppendLine(Ranking(h));
            sb.AppendLine(Novelty(h));
        }
        return sb.ToString();
    }

    /// <summary>ASCII 히트맵 — 어디를 안 가는지 한눈에.</summary>
    string Ascii(SceneHeat h)
    {
        var sb = new StringBuilder();
        sb.AppendLine("  범례: ' '미방문  '.'스침  ':'짧게  '+'보통  '#'오래   X=스턱  !=도달실패  $=고수익  †=사망");
        for (int gy = h.maxY; gy >= h.minY; gy--)
        {
            sb.Append($"  y{gy * CellSize,5:0} |");
            for (int gx = h.minX; gx <= h.maxX; gx++)
            {
                h.cells.TryGetValue(Key(gx, gy), out var c);
                sb.Append(c == null ? ' ' : Glyph(c));
            }
            sb.AppendLine("|");
        }
        sb.AppendLine($"          x{h.minX * CellSize:0} → x{(h.maxX + 1) * CellSize:0}");
        return sb.ToString();
    }

    static char Glyph(Cell c)
    {
        if (c.stuck > 0) return 'X';
        if (c.unreachable > 0) return '!';
        if (c.deaths > 0) return '†';
        if (c.lootValue >= 300) return '$';
        if (c.dwellSec >= 10f) return '#';
        if (c.dwellSec >= 4f) return '+';
        if (c.dwellSec >= 1.5f) return ':';
        return '.';
    }

    string Ranking(SceneHeat h)
    {
        var list = new List<Cell>();
        foreach (var c in h.cells.Values) if (c.dwellSec >= 3f) list.Add(c);
        if (list.Count == 0) return "  (파밍 효율 표본 부족)";

        list.Sort((a, b) => Rate(b).CompareTo(Rate(a)));
        var sb = new StringBuilder();
        sb.AppendLine("  파밍 효율 (가치/분) — 상위 3 / 하위 3");
        for (int i = 0; i < list.Count && i < 3; i++) sb.AppendLine($"    ▲ {Pos(list[i]),-26} {Rate(list[i]),8:0.#}/분  체류 {list[i].dwellSec:0}s · 획득 {list[i].lootValue}");
        for (int i = Mathf.Max(3, list.Count - 3); i < list.Count; i++) sb.AppendLine($"    ▼ {Pos(list[i]),-26} {Rate(list[i]),8:0.#}/분  체류 {list[i].dwellSec:0}s · 획득 {list[i].lootValue}");
        return sb.ToString();
    }

    string Novelty(SceneHeat h)
    {
        if (h.newCellsPerCycle.Count == 0) return "";
        var keys = new List<int>(h.newCellsPerCycle.Keys); keys.Sort();
        var sb = new StringBuilder();
        sb.AppendLine("  반복성 — 사이클별 '처음 밟는 칸' (0에 수렴 = 새로울 게 없음)");
        foreach (var cy in keys)
        {
            int nw = h.newCellsPerCycle[cy];
            h.revisitPerCycle.TryGetValue(cy, out int rv);
            sb.AppendLine($"    사이클 {cy}: 신규 {nw,3}칸 · 재방문 {rv,4}회  {new string('█', Mathf.Min(40, nw))}");
        }
        return sb.ToString();
    }

    // ── JSON 내보내기 (Claude가 읽음) ────────────────────────────────

    [System.Serializable]
    public class CellJson
    {
        public string scene;
        public float x, y;          // 셀 중심 월드 좌표
        public int visits, lootValue, lootCount, stuck, deaths, unreachable, firstCycle;
        public float dwellSec, valuePerMin;
    }

    public List<CellJson> Export()
    {
        var list = new List<CellJson>();
        foreach (var kv in _scenes)
            foreach (var c in kv.Value.cells.Values)
                list.Add(new CellJson
                {
                    scene = kv.Key,
                    x = (c.gx + 0.5f) * CellSize, y = (c.gy + 0.5f) * CellSize,
                    visits = c.visits, dwellSec = c.dwellSec,
                    lootValue = c.lootValue, lootCount = c.lootCount,
                    stuck = c.stuck, deaths = c.deaths, unreachable = c.unreachable,
                    firstCycle = c.firstCycle, valuePerMin = Rate(c),
                });
        return list;
    }
}
