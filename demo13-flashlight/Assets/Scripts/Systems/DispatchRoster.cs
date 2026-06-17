using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 파견 인력(회수꾼) 데이터 싱글톤. (그레이박스 — 런타임 전용, 세이브 미연동)
///  - <b>회수꾼 랜덤 풀</b>: 매번 랜덤 생성된 고용 후보 3명. RefreshPool()로 갱신.
///  - <b>내 고정 파티(계약)</b>: Hire(poolIndex)로 풀에서 계약하면 스크랩 소비 후 roster로 이동.
/// 설계: docs/safehouse-intel.md §2-A(인력 등급·특기), §7-B(파견 UI). 비용은 그레이박스 가안.
/// </summary>
public class DispatchRoster : MonoBehaviour
{
    static DispatchRoster instance;
    public static DispatchRoster Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("DispatchRoster");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<DispatchRoster>();
                instance.Init();
            }
            return instance;
        }
    }

    // ── 등급/특기 (§2-A) ──
    public enum Grade { Apprentice, Veteran, Elite }   // 견습 / 숙련 / 베테랑
    public enum Specialty { Carry, Scout, Forage, Rescue } // 운반 / 정찰 / 채집 / 구조

    public class Unit
    {
        public string name;
        public Grade grade;
        public Specialty specialty;
        public bool contracted;
    }

    // ── 등급별 계약 비용(스크랩, 그레이박스 가안) ──
    public static int HireCost(Grade g) => g switch
    {
        Grade.Apprentice => 500,
        Grade.Veteran    => 1500,
        Grade.Elite      => 4000,
        _                => 500,
    };

    public static string GradeName(Grade g) => g switch
    {
        Grade.Apprentice => "견습",
        Grade.Veteran    => "숙련",
        Grade.Elite      => "베테랑",
        _                => "견습",
    };

    public static string SpecialtyName(Specialty s) => s switch
    {
        Specialty.Carry  => "운반",
        Specialty.Scout  => "정찰",
        Specialty.Forage => "채집",
        Specialty.Rescue => "구조",
        _                => "운반",
    };

    // ── 이름 풀(랜덤 생성용) ──
    static readonly string[] NamePool = {
        "노을", "재인", "도윤", "한별", "서리", "강우", "민혁", "유나",
        "정우", "하린", "태경", "소율", "건우", "예린", "찬", "이안",
    };

    const int PoolSize = 3;

    readonly List<Unit> pool = new List<Unit>();    // 회수꾼 랜덤 풀
    readonly List<Unit> roster = new List<Unit>();  // 내 고정 파티(계약됨)
    bool initialized;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;
        RefreshPool();
    }

    // ─────────────────────────────────────────────
    //  조회 API
    // ─────────────────────────────────────────────

    /// <summary>회수꾼 랜덤 풀(읽기용 — 수정 금지).</summary>
    public IReadOnlyList<Unit> Pool => pool;

    /// <summary>내 고정 파티(계약된 인력).</summary>
    public IReadOnlyList<Unit> Roster => roster;

    /// <summary>풀을 새 랜덤 후보 3명으로 갱신.</summary>
    public void RefreshPool()
    {
        pool.Clear();
        for (int i = 0; i < PoolSize; i++)
            pool.Add(GenerateRandomUnit());
    }

    Unit GenerateRandomUnit()
    {
        return new Unit
        {
            name      = NamePool[Random.Range(0, NamePool.Length)],
            grade     = (Grade)Random.Range(0, System.Enum.GetValues(typeof(Grade)).Length),
            specialty = (Specialty)Random.Range(0, System.Enum.GetValues(typeof(Specialty)).Length),
            contracted = false,
        };
    }

    // ─────────────────────────────────────────────
    //  계약(고용)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 풀의 poolIndex 유닛을 계약한다. 스크랩(등급별 비용) 소비 후 roster로 이동, 풀에서 제거.
    /// </summary>
    /// <returns>성공 여부 + 사유 메시지(토스트용).</returns>
    public (bool ok, string reason) Hire(int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= pool.Count)
            return (false, "잘못된 대상");

        var unit = pool[poolIndex];
        int cost = HireCost(unit.grade);

        var cur = CurrencyManager.Instance;
        if (cur == null)
            return (false, "화폐 시스템 없음");
        if (!cur.CanAfford(cost))
            return (false, $"스크랩 부족 (필요 {cost:N0})");

        if (!cur.Spend(cost, $"인력 계약: {unit.name}"))
            return (false, $"스크랩 부족 (필요 {cost:N0})");

        unit.contracted = true;
        pool.RemoveAt(poolIndex);
        roster.Add(unit);
        return (true, $"{unit.name}({GradeName(unit.grade)}·{SpecialtyName(unit.specialty)}) 계약 완료");
    }
}
