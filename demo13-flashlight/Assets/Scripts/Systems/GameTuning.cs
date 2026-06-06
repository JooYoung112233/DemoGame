using UnityEngine;

/// <summary>
/// 게임 전역 튜닝 값 한 곳(밸런스/타이밍 단일 소스). `Resources/Data/GameTuning.asset`.
/// 각 시스템은 `GameTuning.Instance.X`를 읽되, 에셋이 없으면 자체 기본값으로 폴백(크래시 없음).
/// 에디터에서 'Tools ▸ TopDown ▸ Control Panel'로 보고 튜닝.
///
/// 값 추가법: 아래에 필드 하나 추가 → 쓰는 시스템에서 GameTuning.Instance.필드 읽기 → 패널에 자동 노출.
/// </summary>
[CreateAssetMenu(menuName = "BRB/Game Tuning", fileName = "GameTuning")]
public class GameTuning : ScriptableObject
{
    static GameTuning _instance;

    /// <summary>없으면 null 반환 → 각 시스템이 자체 기본값 사용. (런타임/에디터 공용)</summary>
    public static GameTuning Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<GameTuning>("Data/GameTuning");
            return _instance;
        }
    }

    // ── 수색 (루팅 상자 아이템 공개 속도) ──────────────────────────────
    [Header("수색 (루팅 상자)")]
    [Tooltip("아이템 공개 딜레이 배율. >1 느리게, <1 빠르게. 희귀도별 기본 딜레이에 곱함.")]
    [Range(0.25f, 5f)] public float searchSpeedMult = 1f;

    // ── 시간 / 현상 (지역 낮·밤 길이) ─────────────────────────────────
    [Header("시간 / 현상 (지역 낮·밤, 초)")]
    [Tooltip("낮 지속 시간(초). RegionTimeManager가 읽음.")]
    public float dayDuration = 120f;
    [Tooltip("밤(짙은 현상) 지속 시간(초). 1회 현상 지속 ≈ 10분.")]
    public float nightDuration = 600f;

    // ── 레이드 ───────────────────────────────────────────────────────
    [Header("레이드")]
    [Tooltip("레이드 제한 시간(초). 0=무제한. RaidManager가 읽음.")]
    public float raidDuration = 1200f;

    // ── 드랍 ─────────────────────────────────────────────────────────
    [Header("드랍 (맵 전체)")]
    [Tooltip("지역 루트 수량 배율. 1=기본, 0.5=절반, 2=두 배. RegionLootBootstrap가 읽음.")]
    [Range(0f, 3f)] public float lootCountMult = 1f;
}
