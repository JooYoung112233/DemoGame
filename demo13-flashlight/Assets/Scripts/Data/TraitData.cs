using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특성(퍽) 카테고리. traits.md §3 트리 6분류.
/// </summary>
public enum TraitCategory
{
    Combat,      // 전투
    Survival,    // 생존·신체
    Scavenging,  // 회수·파밍
    Stealth,     // 잠행·기동
    Social,      // 사회
    Anomaly,     // ★ 현상 (세계관 고유)
}

/// <summary>
/// 특성 티어. 양수 = 비용 티어(T1~T3), Negative = 부정 특성(PP 환급).
/// </summary>
public enum TraitTier
{
    Negative = 0, // 부정 특성(낙인) — ppCost 음수(환급)
    T1 = 1,
    T2 = 2,
    T3 = 3,
}

/// <summary>
/// 단순 효과 모디파이어 한 줄. (런타임 미연동 — 구조만 보유)
/// 정밀 수치/StatDB 적용은 별도 작업.
/// </summary>
[System.Serializable]
public class TraitEffect
{
    [Tooltip("효과 키 (스탯/플래그 식별자, 예: stamina_max, dodge_iframe)")]
    public string effectKey;

    [Tooltip("연산자 (예: add, mul, set, flag)")]
    public string op;

    [Tooltip("수치 (op에 따라 의미 다름)")]
    public float value;
}

/// <summary>
/// 캐릭터 특성(퍽) 정의 ScriptableObject. traits.md §1~§3.
/// 진행형 퍽 트리: 레이드·평판으로 PP를 벌어 해금. 부정 특성은 PP 환급(ppCost 음수).
/// Assets/Resources/Data/Traits/ 에 SO로 관리.
///
/// TODO(런타임): StatDB/PlayerStatData 연동(효과 적용)·UI 트리 뷰는 이번 범위 아님.
///               effects 리스트는 구조만 — 정밀 수치는 traits.md §4(TBD)·balance-tuner.
/// </summary>
[CreateAssetMenu(fileName = "NewTrait", menuName = "Dev Tools/Trait/Trait Data")]
public class TraitData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 ID (코드 참조용 영문 슬러그, 예: combat_cold_hands)")]
    public string traitId;

    [Tooltip("표시 이름")]
    public string displayName;

    [Tooltip("설명 (서사/플레이버)")]
    [TextArea(2, 4)]
    public string description;

    [Header("분류")]
    public TraitCategory category;
    public TraitTier tier;

    [Header("포인트(PP)")]
    [Tooltip("PP 비용. 부정 특성은 음수 = 환급 (예: -2 = +2 PP 환급)")]
    public int ppCost;

    [Header("선행/관계")]
    [Tooltip("선행 특성 traitId (없으면 빈값)")]
    public string prereqTraitId;

    [Tooltip("⚖ 트레이드오프 퍽 여부 (강효과 + 내장 대가)")]
    public bool tradeoff;

    [Header("효과")]
    [Tooltip("효과 요약 문구 (traits.md 효과 문구 그대로)")]
    [TextArea(1, 3)]
    public string effectSummary;

    [Tooltip("효과 모디파이어 (런타임 미연동 — 구조용)")]
    public List<TraitEffect> effects = new List<TraitEffect>();
}
