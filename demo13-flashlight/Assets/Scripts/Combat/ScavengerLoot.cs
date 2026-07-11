using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 약탈자 회수 루프 (docs/raid.md — ⑧ 「시간 잔상」 대체, 2026-07-11 사용자 결정).
/// 플레이어 사망 시 잃은 소지품(보안 제외)을 담아뒀다가, **다음 레이드에 스폰된 적 몇 마리를
/// '약탈자'로 지정해 그들에게 분배**한다. 그 적을 잡아 시체를 뒤지면 회수. 못 챙기고 그 레이드를
/// 벗어나면 **영구 소실(한 번의 기회)** — 분배 시점에 대기열을 비운다.
/// 세션 런타임(세이브 미포함 — 죽고 그 세션 내 재출격 루프 대응. 크로스세션 영속은 후속).
/// </summary>
public static class ScavengerLoot
{
    static readonly List<ItemInstance> _pending = new List<ItemInstance>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { _pending.Clear(); }

    /// <summary>회수 대기 중인 잃은 물품이 있는가(다음 레이드에서 약탈자에게 분배될 것).</summary>
    public static bool HasPending => _pending.Count > 0;

    /// <summary>대기 물품 수(디버그/HUD용).</summary>
    public static int PendingCount => _pending.Count;

    /// <summary>사망 시 잃은 소지품을 회수 대기열에 담는다(다음 레이드 약탈자에게 분배).</summary>
    public static void Capture(IEnumerable<ItemInstance> items)
    {
        if (items == null) return;
        foreach (var it in items)
            if (it != null && it.data != null) _pending.Add(it);
    }

    /// <summary>새 레이드 스폰 직후 호출 — 대기 물품을 약탈자 적 몇 마리에 분배하고 비운다(한 번의 기회).</summary>
    public static void Distribute(List<EnemyController> enemies)
    {
        if (_pending.Count == 0 || enemies == null || enemies.Count == 0) return;

        int maxScav = GameTuning.Instance != null ? Mathf.Max(1, GameTuning.Instance.scavengerCount) : 3;
        int k = Mathf.Min(maxScav, enemies.Count);

        // 적 셔플 — 앞에서 k마리 선택(사망마다 랜덤, 결정론 불필요).
        for (int i = 0; i < k; i++)
        {
            int j = Random.Range(i, enemies.Count);
            var tmp = enemies[i]; enemies[i] = enemies[j]; enemies[j] = tmp;
        }

        // 물품을 k마리에 라운드로빈 분배.
        var buckets = new List<ItemInstance>[k];
        for (int i = 0; i < k; i++) buckets[i] = new List<ItemInstance>();
        for (int i = 0; i < _pending.Count; i++) buckets[i % k].Add(_pending[i]);

        int assigned = 0;
        for (int i = 0; i < k; i++)
            if (buckets[i].Count > 0 && enemies[i] != null) { enemies[i].MakeScavenger(buckets[i]); assigned++; }

        _pending.Clear();   // 한 번의 기회 — 이 레이드로 소비(회수 못하면 영구 소실)
        if (assigned > 0)
            Debug.Log($"[ScavengerLoot] 약탈자 {assigned}기에 잃은 물품 분배(회수 기회 1회).");
    }
}
