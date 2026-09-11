using System.Collections.Generic;
using UnityEngine;

/// <summary>플레이어 전역 평판 등급. quests-region1.md §9.2 / tools/balance/reputation_tiers.csv.</summary>
public enum ReputationTier { F, E, D, C, B, A }

/// <summary>
/// 평판 등급 순수 로직 — 임계값·자릿세 할인·라벨. MonoBehaviour 비의존(에디터/자가검증에서 직접 호출 가능).
/// 진실원: tools/balance/reputation_tiers.csv. (I29 BalanceConfig 런타임 로더가 추후 외부화 — 그 전까진 이 표가 하드 미러.)
/// </summary>
public static class ReputationTiers
{
    public struct Info
    {
        public ReputationTier tier;
        public int min;            // 이 등급 진입 최소 평판
        public int rentDiscount;   // 자릿세 할인 %
        public string name;        // 거리 호칭
    }

    // 내림차순(높은 등급 먼저) — GetTier가 첫 매치를 최고 등급으로 반환.
    static readonly Info[] Table =
    {
        new Info { tier = ReputationTier.A, min = 150, rentDiscount = 80, name = "이름" },
        new Info { tier = ReputationTier.B, min = 100, rentDiscount = 60, name = "베테랑" },
        new Info { tier = ReputationTier.C, min = 60,  rentDiscount = 40, name = "회수꾼" },
        new Info { tier = ReputationTier.D, min = 30,  rentDiscount = 20, name = "숙련" },
        new Info { tier = ReputationTier.E, min = 10,  rentDiscount = 0,  name = "견습" },
        new Info { tier = ReputationTier.F, min = 0,   rentDiscount = 0,  name = "신참" },
    };

    public const int Max = 9999;

    public static ReputationTier GetTier(int rep)
    {
        foreach (var i in Table)
            if (rep >= i.min) return i.tier;
        return ReputationTier.F;
    }

    public static Info GetInfo(ReputationTier t)
    {
        foreach (var i in Table)
            if (i.tier == t) return i;
        return Table[Table.Length - 1];
    }

    public static int RentDiscountPercent(int rep) => GetInfo(GetTier(rep)).rentDiscount;
    public static string Name(ReputationTier t) => GetInfo(t).name;

    /// <summary>다음 등급까지 남은 평판 (A 등급이면 0).</summary>
    public static int ToNextTier(int rep)
    {
        var cur = GetTier(rep);
        if (cur == ReputationTier.A) return 0;
        var next = (ReputationTier)((int)cur + 1);   // F→E→D→C→B→A
        return Mathf.Max(0, GetInfo(next).min - rep);
    }
}

/// <summary>
/// 행동별 평판 적립값. 진실원: tools/balance/reputation.csv.
/// BD_general·DQ_general 등 미정의 키 = 0(평판 미부여).
/// </summary>
public static class ReputationActions
{
    static readonly Dictionary<string, int> Values = new Dictionary<string, int>
    {
        { "MQ-001_report", 10 },
        { "SQ-002_done", 5 },
        { "MQ-002_done", 15 },
        { "SQ-001_rescue", 5 },
        { "BQ-E_done", 1 },
        { "BQ-D_done", 2 },
        { "BQ-C_done", 3 },
        { "BQ-B_done", 4 },
        { "BQ-A_done", 5 },
        { "rudi_deliver", 1 },
    };

    public static int Value(string actionKey)
        => (actionKey != null && Values.TryGetValue(actionKey, out var v)) ? v : 0;
}
