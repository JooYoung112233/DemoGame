using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 사이클별 밸런스 지표 수집 + **추세 이상 감지**. (docs/qa.md)
/// 한 판만 봐선 안 보이고 여러 사이클을 쌓아야 드러나는 것들 — 돈 인플레, 루팅 가뭄,
/// 레벨 정체, 사이클 시간 폭주 — 을 잡는다.
/// </summary>
[System.Serializable]
public class QaCycleMetrics
{
    public int cycle;
    public int moneyStart, moneyEnd;
    public int levelStart, levelEnd;
    public int xpGained;
    public int itemsHeldStart, itemsHeldEnd;
    public int lootValue;          // 이번 사이클 레이드에서 주운 것의 판매가 합
    public int spent, earned;      // 상점 구매/판매
    public int cratesOpened;
    public int questsAccepted;
    public int deaths;
    public float durationSec;
    public bool raidCompleted;     // 탈출까지 성공했나
}

public class QaTelemetry
{
    readonly List<QaCycleMetrics> _cycles = new List<QaCycleMetrics>();
    QaCycleMetrics _cur;

    public IReadOnlyList<QaCycleMetrics> Cycles => _cycles;
    public QaCycleMetrics Current => _cur;

    public void BeginCycle(int index)
    {
        _cur = new QaCycleMetrics
        {
            cycle = index,
            moneyStart = Money(),
            levelStart = Level(),
            itemsHeldStart = ItemsHeld(),
            durationSec = Time.realtimeSinceStartup,   // 임시로 시작 시각 보관
        };
    }

    public void EndCycle()
    {
        if (_cur == null) return;
        _cur.moneyEnd = Money();
        _cur.levelEnd = Level();
        _cur.itemsHeldEnd = ItemsHeld();
        _cur.durationSec = Time.realtimeSinceStartup - _cur.durationSec;
        _cycles.Add(_cur);
        _cur = null;
    }

    // ── 게임 상태 스냅샷 ─────────────────────────────────────────────
    public static int Money() => CurrencyManager.Instance != null ? CurrencyManager.Instance.Balance : 0;
    public static int Level() => PlayerProgress.Instance != null ? PlayerProgress.Instance.Level : 0;

    public static int ItemsHeld()
    {
        var p = TopDownPlayer.Instance;
        if (p == null) return 0;
        var inv = p.GetComponent<PlayerInventory>();
        if (inv == null) return 0;
        int n = 0;
        if (inv.Grid != null) n += inv.Grid.ItemCount;
        if (inv.PocketsGrid != null) n += inv.PocketsGrid.ItemCount;
        if (inv.SecureGrid != null) n += inv.SecureGrid.ItemCount;
        return n;
    }

    /// <summary>소지품 판매가 총합(루팅 가치 계산용).</summary>
    public static int HeldValue()
    {
        var p = TopDownPlayer.Instance;
        if (p == null) return 0;
        var inv = p.GetComponent<PlayerInventory>();
        if (inv == null) return 0;
        int v = 0;
        foreach (var g in new[] { inv.Grid, inv.PocketsGrid, inv.SecureGrid })
        {
            if (g == null) continue;
            foreach (var it in g.GetAll())
                if (it?.item?.data != null) v += it.item.data.sellPrice * it.item.stackCount;
        }
        return v;
    }

    // ── 추세 이상 감지 ───────────────────────────────────────────────

    /// <summary>사이클 지표를 훑어 밸런스 이상을 리포트에 올린다.</summary>
    public void Analyze(QaReport rep)
    {
        if (_cycles.Count == 0) { rep.Warn("balance", "NO_CYCLE", "완료된 사이클이 없어 추세 분석 불가"); return; }

        int n = _cycles.Count;
        float avgLoot = 0f, avgDur = 0f;
        int completed = 0, deaths = 0;
        foreach (var c in _cycles) { avgLoot += c.lootValue; avgDur += c.durationSec; if (c.raidCompleted) completed++; deaths += c.deaths; }
        avgLoot /= n; avgDur /= n;

        rep.Metric("사이클 수", n);
        rep.Metric("레이드 완주율(%)", completed * 100f / n);
        rep.Metric("사이클 평균 루팅가치", avgLoot);
        rep.Metric("사이클 평균 시간(초)", avgDur);
        rep.Metric("총 사망", deaths);

        var first = _cycles[0];
        var last = _cycles[n - 1];
        rep.Metric("소지금 시작→끝", last.moneyEnd - first.moneyStart);
        rep.Metric("레벨 시작→끝", last.levelEnd - first.levelStart);

        // 1) 루팅 가뭄 — 레이드는 돌았는데 얻은 가치가 0
        if (completed > 0 && avgLoot <= 0f)
            rep.Error("balance", "LOOT_DROUGHT", "레이드를 완주했는데 사이클 평균 루팅가치가 0 — 예산제/루트테이블 미동작 의심");

        // 2) 돈 인플레 — 매 사이클 순증가 + 총증가가 시작 자산의 5배 초과
        bool allUp = true;
        foreach (var c in _cycles) if (c.moneyEnd <= c.moneyStart) { allUp = false; break; }
        int gain = last.moneyEnd - first.moneyStart;
        if (allUp && first.moneyStart > 0 && gain > first.moneyStart * 5)
            rep.Warn("balance", "MONEY_INFLATION", $"모든 사이클에서 소지금 순증가 + 총 {gain} 증가(시작 {first.moneyStart}) — 소모처 부족 의심");

        // 3) 레벨 정체 — XP를 벌었는데 레벨이 그대로
        int xpTotal = 0; foreach (var c in _cycles) xpTotal += c.xpGained;
        if (xpTotal > 0 && last.levelEnd == first.levelStart && n >= 3)
            rep.Warn("balance", "LEVEL_STALL", $"{n}사이클 동안 XP {xpTotal}을 벌었는데 레벨 변화 없음 — 곡선이 너무 가파른지 확인");

        // 4) 사이클 시간 폭주 — 마지막이 첫 사이클의 3배 초과
        if (n >= 3 && first.durationSec > 1f && last.durationSec > first.durationSec * 3f)
            rep.Warn("balance", "CYCLE_SLOWDOWN", $"사이클 시간이 {first.durationSec:0}s → {last.durationSec:0}s로 증가 — 누수/스턱 의심");

        // 5) 완주 실패율
        if (completed < n)
            rep.Warn("balance", "RAID_INCOMPLETE", $"{n}사이클 중 {n - completed}회 레이드 미완주(탈출 실패/막힘)");
    }

    public string BuildTable()
    {
        var sb = new StringBuilder();
        sb.AppendLine("── 사이클별 지표 ──");
        sb.AppendLine($"  {"#",-3}{"소지금",12}{"레벨",6}{"XP",7}{"루팅가치",10}{"구매",8}{"판매",8}{"상자",6}{"시간(s)",9}  완주");
        foreach (var c in _cycles)
            sb.AppendLine($"  {c.cycle,-3}{c.moneyStart + "→" + c.moneyEnd,12}{c.levelStart + "→" + c.levelEnd,6}" +
                          $"{c.xpGained,7}{c.lootValue,10}{c.spent,8}{c.earned,8}{c.cratesOpened,6}{c.durationSec,9:0.0}  {(c.raidCompleted ? "O" : "X")}");
        return sb.ToString();
    }

    public List<QaCycleMetrics> Snapshot() => new List<QaCycleMetrics>(_cycles);
}
