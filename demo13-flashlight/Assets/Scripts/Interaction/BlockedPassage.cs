using UnityEngine;

/// <summary>
/// 막힌 통로 — "못 가는 곳 / 돌아가야 하는 곳"을 만드는 레벨 인터랙션.
/// (docs/level-scrapmarket.md 2026-07-11 — 사용자 요청 "못 가는 지역이나 돌아가야 하는 등 인터랙티브한 것")
///
/// 길목에 **솔리드 콜라이더**로 서서 통행을 막고, 모드에 따라 열리는 방법이 다르다.
///   • Permanent — 영구 차단. 열 방법 없음(지역 확장 예정지·연출). 안내 토스트만.
///   • Clearable — 철거. E 홀드 채널(<see cref="UseActionManager"/>) + **완료 시 큰 소음**.
///                 → "빠른 길이지만 시끄럽다" 대 "조용히 돌아간다"의 선택을 만든다.
///   • Locked    — 열쇠/도구 아이템 필요. 없으면 **강제 돌파**로 폴백(더 오래 + 더 시끄럽게).
///   • NightOnly — 낮엔 잠김, 밤에만 통행(DayNightCycle).
///
/// ★ Locked의 강제 돌파 폴백이 중요하다: 이 저장소의 열쇠 사슬(key_apt_admin 등)은
///   **아직 ItemData가 없다.** 폴백이 없으면 존재하지 않는 열쇠 때문에 맵이 영구 소프트락된다.
///   나중에 열쇠 아이템이 생기면 열쇠 경로가 저절로 살아난다(코드 수정 불필요).
///
/// 부착: 같은 GO에 `InteractableObject`(type = Passage) + 솔리드 Collider2D.
/// </summary>
[RequireComponent(typeof(InteractableObject))]
public class BlockedPassage : MonoBehaviour
{
    public enum Mode { Permanent, Clearable, Locked, NightOnly, Code }

    [Header("── 방식 ──")]
    [SerializeField] Mode mode = Mode.Clearable;
    [Tooltip("표시 이름 (토스트/프롬프트)")]
    [SerializeField] string label = "무너진 잔해";

    [Header("Locked — 필요한 아이템")]
    [Tooltip("ItemData.itemId. 비었거나 DB에 없으면 강제 돌파만 가능.")]
    [SerializeField] string requiredItemId;
    [SerializeField] bool consumeItem = true;

    [Header("Code — 알아내야 하는 지식")]
    [Tooltip("PlayerKnowledge 플래그 id (예: code_jewelry_vault). 쪽지·퀘스트로 획득.\n" +
             "아이템이 아니라 '지식'이라 죽어도 잃지 않는다.")]
    [SerializeField] string requiredKnowledgeId;
    [Tooltip("아직 모를 때 주는 힌트 한 줄 (어디서 알아낼 수 있는지).")]
    [SerializeField] string codeHint = "번호를 모른다. 어딘가에 적어 뒀을 텐데.";

    [Header("시간 (0 = GameTuning 기본값)")]
    [SerializeField] float clearSeconds;
    [SerializeField] float breachSeconds;

    [Header("참조 (비우면 자동)")]
    [Tooltip("통행을 막는 솔리드 콜라이더. 열리면 비활성화된다.")]
    [SerializeField] Collider2D blocker;
    [Tooltip("열릴 때 숨길 비주얼(잔해 스프라이트 등)")]
    [SerializeField] GameObject visual;

    bool _open;
    InteractableObject _io;

    public bool IsOpen => _open;
    public Mode PassageMode => mode;

    float ClearSecs  => clearSeconds  > 0f ? clearSeconds
                      : (GameTuning.Instance != null ? GameTuning.Instance.barricadeClearSeconds : 3.5f);
    float BreachSecs => breachSeconds > 0f ? breachSeconds
                      : (GameTuning.Instance != null ? GameTuning.Instance.barricadeBreachSeconds : 7f);

    void Awake()
    {
        _io = GetComponent<InteractableObject>();

        if (blocker == null)
        {
            // 솔리드(트리거 아님) 우선 — 진입 발판(트리거)과 같은 GO에 있을 수 있다.
            var cols = GetComponentsInChildren<Collider2D>();
            foreach (var c in cols) { if (!c.isTrigger) { blocker = c; break; } }
        }
    }

    void Start() => RefreshPrompt();

    /// <summary>빌더/런타임 설정. Locked면 keyOrKnowledgeId = 아이템 id, Code면 지식 id.</summary>
    public void Configure(Mode m, string displayLabel, string keyOrKnowledgeId = null, string hint = null)
    {
        mode = m;
        label = displayLabel;
        if (m == Mode.Code) requiredKnowledgeId = keyOrKnowledgeId;
        else                requiredItemId      = keyOrKnowledgeId;
        if (!string.IsNullOrEmpty(hint)) codeHint = hint;
    }

    /// <summary>E 상호작용 진입점 — `InteractableObject`가 호출.</summary>
    public void TryPass(GameObject playerGO)
    {
        if (_open) return;

        switch (mode)
        {
            case Mode.Permanent:
                Toast($"{label} — 이쪽은 완전히 막혔다. 돌아가야 한다.", ToastManager.ToastType.Warning);
                return;

            case Mode.NightOnly:
                if (IsNight()) { Open($"{label} — 지나간다."); return; }
                Toast($"{label} — 지금은 지날 수 없다. 어두워지면 다시 와 보자.", ToastManager.ToastType.Warning);
                return;

            case Mode.Locked:
                TryUnlockOrBreach(playerGO);
                return;

            case Mode.Code:
                if (PlayerKnowledge.IsKnown(requiredKnowledgeId))
                {
                    Open($"{label} — 번호를 입력했다. 열렸다.");
                    return;
                }
                Toast($"{label} — {codeHint}", ToastManager.ToastType.Warning);
                // 번호를 몰라도 **부술 수는 있다**(오래·시끄럽게). 알아낸 쪽이 항상 이득.
                BeginChannel(BreachSecs, $"{label} 강제 개방 중");
                return;

            default:
                BeginChannel(ClearSecs, $"{label} 치우는 중");
                return;
        }
    }

    void TryUnlockOrBreach(GameObject playerGO)
    {
        // ① 열쇠/도구가 실제로 존재하고 보유 중이면 즉시 통과.
        var data = string.IsNullOrEmpty(requiredItemId) ? null : ItemDatabase.Get(requiredItemId);
        if (data != null)
        {
            var inv = playerGO != null ? playerGO.GetComponent<PlayerInventory>() : null;
            if (inv != null && inv.CountItemAll(requiredItemId) > 0)
            {
                if (consumeItem) inv.ConsumeItemAll(requiredItemId, 1);
                Open($"{data.displayName}(으)로 {label}을(를) 열었다.");
                return;
            }
            Toast($"{data.displayName}이(가) 없다 — 부수고 들어간다(오래 걸리고 시끄럽다).", ToastManager.ToastType.Warning);
        }
        // ② 열쇠가 없거나 아직 미구현 → 강제 돌파(느리고 시끄럽다). 소프트락 방지.
        BeginChannel(BreachSecs, $"{label} 강제 돌파 중");
    }

    void BeginChannel(float seconds, string text)
    {
        var mgr = UseActionManager.Instance;
        if (mgr == null) { Open($"{label} — 치웠다."); return; }   // 채널 시스템이 없으면 즉시 처리(폴백)
        if (mgr.IsBusy) return;
        // 예전엔 치우는 소음으로 주변 적이 몰려왔으나, 소음 시스템 폐기(2026-09-09)로 대가는 시간뿐이다.
        mgr.Begin(text, seconds, () => Open($"{label} — 치웠다."));
    }

    void Open(string msg)
    {
        _open = true;
        if (blocker != null) blocker.enabled = false;
        if (visual != null) visual.SetActive(false);
        else
        {
            // 비주얼 참조가 없으면 스프라이트만 흐리게 — "치운 자리"가 눈에 남게.
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                var c = sr.color; c.a *= 0.28f; sr.color = c;
            }
        }
        if (_io != null) _io.SetInteractable(false);
        Toast(msg, ToastManager.ToastType.Info);
    }

    void RefreshPrompt()
    {
        if (_io == null) return;
        string p = mode switch
        {
            Mode.Permanent => $"{label} (막힘)",
            Mode.NightOnly => $"{label} (밤에만)",
            Mode.Locked    => $"{label} 열기",
            Mode.Code      => $"{label} (번호)",
            _              => $"{label} 치우기",
        };
        _io.SetPrompt(p);
    }

    static bool IsNight()
    {
        var dn = FindFirstObjectByType<DayNightCycle>();
        return dn != null && dn.IsNight;
    }

    static void Toast(string msg, ToastManager.ToastType t)
    {
        ToastManager.Show(msg, t);
        Debug.Log($"[BlockedPassage] {msg}");
    }
}
