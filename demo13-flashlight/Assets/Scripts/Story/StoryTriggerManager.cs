using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 이벤트 → 스토리 플래그 → 자동 트리거 연결 매니저.
/// 씬 전환, 퀘스트 완료, 첫 행동 등에 반응해 StoryPlayer를 가동.
///
/// GameBootstrap에서 싱글톤으로 생성.
/// </summary>
public class StoryTriggerManager : MonoBehaviour
{
    public static StoryTriggerManager Instance { get; private set; }

    // ── 씬 이름 상수 ──
    const string SCENE_SAFEHOUSE = "Safehouse";
    const string SCENE_HIDEOUT = "Hideout";
    const string SCENE_SYSTEMS = "Systems";

    // ── 첫 행동 추적 (세이브 로드 시 복원은 플래그로 대체) ──
    bool prologuePlayed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════
    //  씬 로드 훅
    // ═══════════════════════════

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 약간의 딜레이 — 씬 오브젝트 초기화 대기
        StartCoroutine(DelayedSceneInit(scene.name));
    }

    System.Collections.IEnumerator DelayedSceneInit(string sceneName)
    {
        yield return null; // 1프레임 대기 (Start() 호출 보장)

        if (sceneName == SCENE_SAFEHOUSE || sceneName.Contains("Safehouse"))
        {
            OnSafehouseLoaded();
        }
        // 안전 실내 씬(은신처/전당포)은 레이드가 아님 — 레이드 스토리 트리거 제외.
        else if (sceneName != SCENE_SYSTEMS && sceneName != SCENE_HIDEOUT && sceneName != "Pawnshop")
        {
            OnRaidSceneLoaded();
        }

        // 범용 자동 트리거 체크
        CheckAutoTriggers();
    }

    // ═══════════════════════════
    //  안전가옥 진입
    // ═══════════════════════════

    void OnSafehouseLoaded()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        // 안전가옥 진입 시 자동 트리거 체크(귀환 보고 S-007, 거처 S-010 등)는
        // DelayedSceneInit 끝의 CheckAutoTriggers()에서 일괄 처리.
        if (!qm.GetFlag("safehouse_arrived"))
            qm.SetFlag("safehouse_arrived");
    }

    // ═══════════════════════════
    //  레이드 씬 진입
    // ═══════════════════════════

    void OnRaidSceneLoaded()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        // 낮/밤 판별
        bool isNight = false;
        if (RegionTimeManager.Instance != null)
        {
            string activeId = RegionTimeManager.Instance.ActiveRegionId;
            if (!string.IsNullOrEmpty(activeId))
            {
                var rt = RegionTimeManager.Instance.GetRegion(activeId);
                if (rt != null) isNight = rt.isNight;
            }
        }
        else
        {
            var dnc = FindFirstObjectByType<DayNightCycle>();
            if (dnc != null) isNight = dnc.IsNight;
        }

        if (isNight)
        {
            if (!qm.GetFlag("entered_night_ever"))
            {
                qm.SetFlag("entered_ruined_mall_night");
                qm.SetFlag("entered_night_ever");
            }
        }
        else
        {
            if (!qm.GetFlag("entered_day_ever"))
            {
                qm.SetFlag("entered_ruined_mall_day");
                qm.SetFlag("entered_day_ever");
            }
        }
    }

    // ═══════════════════════════
    //  프롤로그 (게임 최초 시작)
    // ═══════════════════════════

    /// <summary>
    /// 게임 최초 시작 시 호출. 프롤로그 씬 S-000 재생.
    /// 게임 시작 스크립트에서 직접 호출.
    /// </summary>
    /// <summary>
    /// 자동 프롤로그 (GameStartHandler에서 호출, 1회만).
    /// </summary>
    /// <summary>새 게임 리셋 — 프롤로그 재생 플래그 초기화(같은 세션 재시작 시 프롤로그 다시 재생).</summary>
    public void ResetForNewGame() { prologuePlayed = false; }

    /// <returns>실제로 프롤로그를 시작했으면 true(이미 재생됐거나 StoryPlayer 없음이면 false).</returns>
    public bool PlayPrologueAuto()
    {
        if (prologuePlayed) return false;
        if (StoryPlayer.Instance == null || StoryPlayer.Instance.IsPlaying) return false;
        prologuePlayed = true;

        PlayPrologue();
        return true;
    }

    /// <summary>
    /// 프롤로그 재생. H키 튜토리얼 패널에서도 호출 가능.
    /// S-000(깨어남) → S-001(골목 첫 이동) 연쇄 후 H키 안내.
    /// </summary>
    public void PlayPrologue()
    {
        if (StoryPlayer.Instance == null) return;
        if (StoryPlayer.Instance.IsPlaying) return;

        StoryPlayer.Instance.PlayScene("S-000", () =>
        {
            StoryPlayer.Instance.PlayScene("S-001", () =>
            {
                // 프롤로그 후 H키 안내 프롬프트 표시
                if (TutorialPrompt.Instance != null)
                {
                    string helpText = StoryLocale.Instance != null
                        ? StoryLocale.Instance.Get("TUT_HELP")
                        : "H — 조작법 보기";
                    TutorialPrompt.Instance.Show(helpText, 5f, null);
                }
            });
        });
    }

    // ═══════════════════════════
    //  NPC 대화 — 스토리 씬 우선 체크
    // ═══════════════════════════

    /// <summary>
    /// NPC 상호작용 전에 호출.
    /// 스토리 씬이 있으면 재생하고 true 반환. 없으면 false (일반 대화 진행).
    /// </summary>
    public bool TryPlayNPCStoryScene(string npcId, System.Action onComplete = null)
    {
        var qm = QuestManager.Instance;
        var sp = StoryPlayer.Instance;
        if (qm == null || sp == null || sp.IsPlaying) return false;

        // 전당포 주인 — 스토리 비활성 (그레이박스 테스트: 바로 일반 대화로)

        // 베테랑 회수꾼 — 암구호 '불씨' 확보 후 최초 대화
        // S-003(소개·정보) → S-004(게시판 의뢰 MQ-001 수령 + 장비 지급) 연쇄.
        if (npcId == "veteran_scavenger" && qm.GetFlag("got_password_ember")
            && !qm.GetFlag("veteran_intro_done") && sp.HasScene("S-003"))
        {
            sp.PlayScene("S-003", () =>
            {
                sp.PlayScene("S-004", () =>
                {
                    qm.SetFlag("veteran_intro_done");
                    onComplete?.Invoke();
                });
            });
            return true;
        }

        // 떠돌이 상인 — 최초 조우 (보급)
        if (npcId == "merchant" && !qm.GetFlag("merchant_met") && sp.HasScene("S-012"))
        {
            sp.PlayScene("S-012", () => onComplete?.Invoke());
            return true;
        }

        // 회수꾼 — 짙은 현상 귀환 보고 (S-016, MQ-002)
        if (npcId == "veteran_scavenger" && qm.GetFlag("night_raid_returned_with_rudi")
            && !qm.GetFlag("mq002_veteran_reported") && sp.HasScene("S-016"))
        {
            sp.PlayScene("S-016", () => onComplete?.Invoke());
            return true;
        }

        // 전당포 — 루디 납품 스토리 비활성 (그레이박스 테스트)

        // 밴딧 협상꾼 — 최초 조우
        if (npcId == "bandit_negotiator" && !qm.GetFlag("negotiator_met") && sp.HasScene("SX-002"))
        {
            sp.PlayScene("SX-002", () => onComplete?.Invoke());
            return true;
        }

        return false;
    }

    // ═══════════════════════════
    //  침대 휴식
    // ═══════════════════════════

    /// <summary>
    /// 침대 상호작용 시 호출.
    /// 페이드 아웃 → HP 회복 → 페이드 인 → 스토리 체크.
    /// </summary>
    public void OnBedRest()
    {
        StartCoroutine(BedRestRoutine());
    }

    System.Collections.IEnumerator BedRestRoutine()
    {
        var qm = QuestManager.Instance;

        // 페이드 아웃
        if (ScreenEffectManager.Instance != null)
            yield return ScreenEffectManager.Instance.FadeOut(0.8f);
        else
            yield return new WaitForSecondsRealtime(0.5f);

        // HP 회복
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var health = player.GetComponent<Health>();
            if (health != null)
                health.Heal(health.MaxHp);
        }

        // 시간 경과 (짧은 대기)
        yield return new WaitForSecondsRealtime(0.5f);

        // 페이드 인
        if (ScreenEffectManager.Instance != null)
            yield return ScreenEffectManager.Instance.FadeIn(1.0f);

        // 첫 수면 플래그 — 거처(컨테이너) 획득 후 첫 휴식 (S-011_WAKE: 생존 스탯 안내)
        if (qm != null && !qm.GetFlag("first_sleep_done") && qm.GetFlag("home_unlocked"))
        {
            qm.SetFlag("first_sleep_done");
        }

        // MQ-002 완료 후 첫 휴식 → 에필로그
        if (qm != null && qm.CompletedQuestIds.Contains("MQ-002") && !qm.GetFlag("mq002_complete_rested"))
        {
            qm.SetFlag("mq002_complete_rested");
        }

        // 오토세이브
        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSave();

        // 자동 트리거 체크
        CheckAutoTriggers();
    }

    // ═══════════════════════════
    //  첫 파밍
    // ═══════════════════════════

    /// <summary>
    /// 파밍/줍기 성공 시 호출 (첫 1회만 스토리 트리거).
    /// </summary>
    public void OnItemLooted()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        if (!qm.GetFlag("first_loot_done"))
        {
            qm.SetFlag("first_loot_done");
            CheckAutoTriggers();
        }
    }

    // ═══════════════════════════
    //  쪽지 발견
    // ═══════════════════════════

    /// <summary>
    /// 쪽지 오브젝트 상호작용 시 호출.
    /// storySceneId가 있으면 해당 스토리 씬 재생, 없으면 DialogueUI로 표시.
    /// </summary>
    public void OnNoteRead(string noteContent, string storySceneId = null)
    {
        if (!string.IsNullOrEmpty(storySceneId) && StoryPlayer.Instance != null)
        {
            // 단서 씬(예: S-005_WAREHOUSE 민이 흔적, S-009_BOX 철제 상자)을 재생.
            // 진행 플래그(mq001_trace_found, sq002_box_found 등)는 각 씬 내부 system 노드에서 설정.
            StoryPlayer.Instance.PlayScene(storySceneId);
            return;
        }

        // 일반 쪽지 → 전체 화면 노트 UI(없으면 자동 생성).
        NoteUI.Ensure().Show(noteContent);
    }

    // ═══════════════════════════
    //  전투 (첫 전투)
    // ═══════════════════════════

    /// <summary>
    /// 적대 탐색꾼 조우 시 호출 (첫 1회만 S-005_COMBAT 전투 튜토리얼 재생).
    /// </summary>
    public void OnFirstCombatEncounter()
    {
        var qm = QuestManager.Instance;
        if (qm == null || qm.GetFlag("first_combat_done")) return;

        qm.SetFlag("first_combat_done");

        if (StoryPlayer.Instance != null)
            StoryPlayer.Instance.PlayScene("S-005_COMBAT");
    }

    // ═══════════════════════════
    //  루디 획득
    // ═══════════════════════════

    /// <summary>
    /// 루디 아이템 획득 시 호출.
    /// 첫 획득이면 시계 이상현상 이벤트 트리거.
    /// </summary>
    public void OnRudiPickup()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        if (!qm.GetFlag("first_rudi_pickup"))
        {
            qm.SetFlag("first_rudi_pickup");
            CheckAutoTriggers();
        }
    }

    // ═══════════════════════════
    //  탈출/귀환
    // ═══════════════════════════

    /// <summary>
    /// 레이드 탈출 성공 시 호출. RaidManager.OnExtractSuccess 에서 호출.
    /// 현재 상태에 따라 적절한 플래그 설정.
    /// </summary>
    public void OnRaidExtract(bool wasNight, bool hasRudi)
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        // 업적
        AchievementManager.Instance?.AddStat("raids_complete", 1);
        if (wasNight)
            AchievementManager.Instance?.AddStat("night_raids", 1);

        // 낮 레이드 첫 귀환 → 회수꾼 보고 (S-007, MQ-001)
        if (!wasNight && !qm.GetFlag("day_raid_returned") && qm.GetFlag("entered_day_ever"))
        {
            qm.SetFlag("day_raid_returned");
        }

        // SQ-002: 약국에서 철제 상자 회수 후 귀환 → 상자 전달 & 거처 알선 (S-010)
        if (!wasNight && qm.GetFlag("sq002_box_found") && !qm.GetFlag("sq002_returned"))
        {
            qm.SetFlag("sq002_returned");
        }

        // 짙은 현상 레이드 + 루디 소지 귀환 → 회수꾼 보고 대기 (S-016, MQ-002)
        if (wasNight && hasRudi && !qm.GetFlag("night_raid_returned_with_rudi"))
        {
            qm.SetFlag("night_raid_returned_with_rudi");
        }

        // 오토세이브는 Safehouse 로드 시 처리
    }

    // ═══════════════════════════
    //  밤 출전 게이트
    // ═══════════════════════════

    /// <summary>
    /// 짙은 현상 출격 게이트 확인 시 호출.
    /// S-013 브리핑 후 S-014(회수꾼 말풍선) → 폐상가 로드 시 S-015(탐지등 튜토리얼).
    /// </summary>
    public void OnNightGateSelected(System.Action onComplete)
    {
        var qm = QuestManager.Instance;
        if (qm != null)
            qm.SetFlag("gate_anomaly_depart");
        CheckAutoTriggers();
        onComplete?.Invoke();
    }

    // ═══════════════════════════
    //  지하창고 진입
    // ═══════════════════════════

    /// <summary>
    /// 지하창고 영역 진입 시 호출.
    /// </summary>
    public void OnBasementEnter()
    {
        var qm = QuestManager.Instance;
        if (qm == null || qm.GetFlag("basement_entered")) return;

        qm.SetFlag("enter_basement");
        qm.SetFlag("basement_entered");
        CheckAutoTriggers();
    }

    // ═══════════════════════════
    //  NPC 퀘스트 마커 지원
    // ═══════════════════════════

    /// <summary>
    /// 해당 NPC에 아직 재생되지 않은 스토리 씬이 있는지 확인.
    /// NPCQuestMarker에서 호출하여 스토리 아이콘 표시 여부 결정.
    /// </summary>
    public bool HasPendingNPCScene(string npcId)
    {
        var qm = QuestManager.Instance;
        if (qm == null) return false;

        // 베테랑 회수꾼 — 암구호 확보 후 소개 미완료
        if (npcId == "veteran_scavenger" && qm.GetFlag("got_password_ember")
            && !qm.GetFlag("veteran_intro_done"))
            return true;

        // 떠돌이 상인 — 미조우
        if (npcId == "merchant" && !qm.GetFlag("merchant_met"))
            return true;

        // 회수꾼 — 짙은 현상 귀환 보고 대기
        if (npcId == "veteran_scavenger" && qm.GetFlag("night_raid_returned_with_rudi")
            && !qm.GetFlag("mq002_veteran_reported"))
            return true;

        // 밴딧 협상꾼 — 미조우
        if (npcId == "bandit_negotiator" && !qm.GetFlag("negotiator_met"))
            return true;

        return false;
    }

    // ═══════════════════════════
    //  유틸리티
    // ═══════════════════════════

    void CheckAutoTriggers()
    {
        if (StoryPlayer.Instance != null && !StoryPlayer.Instance.IsPlaying)
            StoryPlayer.Instance.CheckAutoTriggers();
    }
}
