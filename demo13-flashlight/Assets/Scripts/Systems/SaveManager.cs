using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 세이브/로드 매니저.
/// 모든 게임 상태를 JSON으로 직렬화하여 파일에 저장.
/// 자동 저장: 안전가옥 진입 시, 휴식 시, 레이드 귀환 시.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    const string SAVE_FILE = "save.json";   // 레거시 단일 파일(슬롯0로 마이그레이션)
    const int SAVE_VERSION = 1;
    public const int SlotCount = 3;

    int currentSlot = 0;
    public int CurrentSlot => currentSlot;

    /// <summary>활성 슬롯 지정(0..SlotCount-1). 새 게임/이어하기 슬롯 선택 시 호출.</summary>
    public void SetSlot(int slot)
    {
        currentSlot = Mathf.Clamp(slot, 0, SlotCount - 1);
        Debug.Log($"[Save] 활성 슬롯 = {currentSlot}");
    }

    static string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    string SavePath => SlotPath(currentSlot);
    string LegacyPath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HierarchyFolder.Persist(gameObject);
        MigrateLegacySave();
    }

    /// <summary>구 단일 save.json이 있고 슬롯0가 비어 있으면 슬롯0로 이관(기존 사용자 세이브 보존).</summary>
    void MigrateLegacySave()
    {
        try
        {
            if (File.Exists(LegacyPath) && !File.Exists(SlotPath(0)))
            {
                File.Move(LegacyPath, SlotPath(0));
                Debug.Log("[Save] 레거시 save.json → 슬롯0 이관");
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[Save] 레거시 이관 실패: {e.Message}"); }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════
    //  저장
    // ═══════════════════════════

    /// <summary>
    /// 전체 게임 상태를 파일에 저장.
    /// (= WriteToDisk(BuildSaveData()) — 기존 호출부 호환용 래퍼)
    /// </summary>
    /// <summary>true인 동안 디스크 쓰기를 건너뛴다(인메모리 진행은 그대로).
    /// QA 자동 플레이가 켜는 스위치 — 봇이 준 테스트 아이템·비운 상자·XP가
    /// **플레이어의 진짜 세이브를 덮어쓰지 않게** 한다. (게임 코드는 QA를 참조하지 않는다 — QA가 이 값을 세팅)</summary>
    public static bool SuppressWrites;

    public void Save()
    {
        if (SuppressWrites) { Debug.Log("[Save] SuppressWrites=true — 디스크 쓰기 생략(QA 런)"); return; }
        WriteToDisk(BuildSaveData());
    }

    /// <summary>
    /// 현재 게임 상태를 스냅샷(GameSaveData)으로 수집한다. 디스크에 쓰지 않는다.
    /// SaveCheckpoints의 인메모리 스냅샷(Record) 및 일반 저장(Commit) 양쪽이 이걸 쓴다.
    /// </summary>
    public GameSaveData BuildSaveData()
    {
        var data = new GameSaveData();
        data.version = SAVE_VERSION;
        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 퀘스트
        if (QuestManager.Instance != null)
        {
            data.completedQuests = QuestManager.Instance.CompletedQuestIds.ToList();
            data.flags = new List<FlagEntry>();
            foreach (var kvp in QuestManager.Instance.GetAllFlags())
            {
                data.flags.Add(new FlagEntry { key = kvp.Key, value = kvp.Value });
            }

            data.activeQuests = new List<ActiveQuestEntry>();
            foreach (var q in QuestManager.Instance.ActiveQuests)
            {
                var entry = new ActiveQuestEntry
                {
                    questId = q.data.questId,
                    state = (int)q.state,
                    progress = new List<ProgressEntry>()
                };
                foreach (var kvp in q.progress)
                    entry.progress.Add(new ProgressEntry { index = kvp.Key, count = kvp.Value });
                data.activeQuests.Add(entry);
            }
        }

        // NPC 관계
        if (NPCRelationshipManager.Instance != null)
        {
            data.relationships = NPCRelationshipManager.Instance.GetAllSaveData();
        }

        // 업적
        if (AchievementManager.Instance != null)
        {
            data.achievements = AchievementManager.Instance.GetSaveData();
        }

        // 화폐(스크랩)
        if (CurrencyManager.Instance != null)
        {
            data.currency = CurrencyManager.Instance.GetSaveData();
        }

        // 평판(전역 명성)
        if (ReputationManager.Instance != null)
        {
            data.reputation = ReputationManager.Instance.GetSaveData();
        }

        // 하이드아웃 모듈 레벨
        if (HideoutModuleManager.Instance != null)
        {
            data.hideoutModules = HideoutModuleManager.Instance.GetSaveData();
        }

        // 위탁(전당포) 슬롯
        data.consignSlots = ShopUI.GetConsignSave();

        // 메인 창고(보관함)
        if (MainStash.Instance != null)
        {
            data.mainStash = MainStash.Instance.GetSaveData();
        }

        // 상점 판매 트레이(런타임 격자) — 올려둔 채 저장되면 창고·트레이 어디에도 없어 크래시 시 소실.
        // 스냅샷에 포함하고 로드 시 창고로 복구(RestoreSellTrayToStash).
        data.sellTray = ShopUI.GetSellTraySave();

        // 인벤토리(가방) + 장착 무기
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            var inv = playerGO.GetComponent<PlayerInventory>();
            if (inv != null && inv.Grid != null) data.bagItems = inv.Grid.GetSaveData();
            if (inv != null && inv.PocketsGrid != null) data.pocketItems = inv.PocketsGrid.GetSaveData();
            if (inv != null && inv.SecureGrid != null) data.secureItems = inv.SecureGrid.GetSaveData();
            var eq = playerGO.GetComponent<PlayerEquipment>();
            if (eq != null)
            {
                data.equippedWeapon = eq.GetSaveData();
                data.equippedWeaponAttachments = eq.GetEquippedWeaponAttachments();
            }
            var survival = SurvivalStats.Get();
            if (survival != null) data.survival = survival.GetSaveData();
        }

        if (QuickSlotBar.Instance != null) data.quickSlots = QuickSlotBar.Instance.GetSlotIds();

        if (TraitManager.Instance != null) data.traits = TraitManager.Instance.GetSaveData();

        data.progress = PlayerProgress.Instance.GetSaveData();   // 레벨/XP (lazy 싱글톤 — 접근이 곧 생성)

        // 창고 (안전가옥 가구 — static 목록)
        data.storageUnits = new List<StorageUnitEntry>();
        foreach (var f in SafehouseStorage.AllFurniture)
        {
            if (f == null || f.grid == null) continue;
            data.storageUnits.Add(new StorageUnitEntry { uid = f.uid, items = f.grid.GetSaveData() });
        }

        // 일일 의뢰
        if (DailyQuestManager.Instance != null)
        {
            data.dailyQuest = DailyQuestManager.Instance.GetSaveData();
        }

        // 튜토리얼
        if (TutorialPrompt.Instance != null)
        {
            data.shownTutorials = TutorialPrompt.Instance.GetShownIds().ToList();
        }

        // 스토리 재생 기록
        if (StoryPlayer.Instance != null)
        {
            data.playedScenes = StoryPlayer.Instance.GetPlayedScenes().ToList();
        }

        // 도감 발견 기록
        if (CodexManager.Instance != null)
            data.discoveredItems = CodexManager.Instance.GetSaveData();

        // 레이드 지도 지식(발견 존·통로)
        if (RaidMapManager.InstanceIfExists != null)
            data.raidMap = RaidMapManager.InstanceIfExists.GetSaveData();

        return data;
    }

    /// <summary>GameSaveData → JSON 문자열 (직렬화만; 디스크 X). 스냅샷 보관용.</summary>
    public string ToJson(GameSaveData data)
    {
        return JsonUtility.ToJson(data, true);
    }

    /// <summary>스냅샷(GameSaveData)을 직렬화하여 디스크에 기록.</summary>
    public void WriteToDisk(GameSaveData data)
    {
        WriteJson(ToJson(data));
    }

    /// <summary>이미 직렬화된 JSON 문자열을 그대로 디스크에 기록.
    /// (크래시 복구 등에서 보관해 둔 스냅샷 JSON을 재수집 없이 그대로 커밋할 때 사용)</summary>
    public void WriteJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        WriteAtomic(SavePath, json);
        Debug.Log($"[Save] 저장 완료: {SavePath}");
    }

    /// <summary>원자적 파일 쓰기 — 임시파일에 먼저 쓰고 교체(File.Replace).
    /// 쓰는 도중 크래시/스팀 클라우드 동기화가 겹쳐도 반쪽 파일이 남지 않음(세이브 파손 방지).
    /// 스팀 클라우드(Auto-Cloud)가 이 파일을 그대로 동기화하므로 원자성이 특히 중요. (save.md §10)</summary>
    static void WriteAtomic(string path, string content)
    {
        string tmp = path + ".tmp";
        try
        {
            File.WriteAllText(tmp, content);
            if (File.Exists(path)) File.Replace(tmp, path, null);   // 동일 볼륨 원자 교체
            else File.Move(tmp, path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Save] 원자적 쓰기 실패({e.Message}) → 직접 쓰기 폴백");
            try { File.WriteAllText(path, content); } catch (System.Exception e2) { Debug.LogError($"[Save] 저장 실패: {e2.Message}"); }
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    // ═══════════════════════════
    //  로드
    // ═══════════════════════════

    /// <summary>
    /// 세이브 파일에서 게임 상태 복원.
    /// </summary>
    public bool Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[Save] 세이브 파일 없음. 새 게임.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<GameSaveData>(json);

        if (data == null || data.version != SAVE_VERSION)
        {
            Debug.LogWarning("[Save] 세이브 버전 불일치. 새 게임으로 시작.");
            return false;
        }

        // 퀘스트
        if (QuestManager.Instance != null)
        {
            // 완료 퀘스트 복원
            foreach (var qid in data.completedQuests)
                QuestManager.Instance.CompletedQuestIds.Add(qid);

            // 플래그 복원
            if (data.flags != null)
            {
                foreach (var f in data.flags)
                    QuestManager.Instance.SetFlag(f.key, f.value);
            }

            // 진행 중 퀘스트는 아래 playedScenes 복원 후 처리
        }

        // NPC 관계
        if (NPCRelationshipManager.Instance != null && data.relationships != null)
        {
            NPCRelationshipManager.Instance.LoadAllSaveData(data.relationships);
        }

        // 업적
        if (AchievementManager.Instance != null && data.achievements != null)
        {
            AchievementManager.Instance.LoadSaveData(data.achievements);
        }

        // 화폐(스크랩)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.LoadSaveData(data.currency);
        }

        // 평판(전역 명성)
        if (ReputationManager.Instance != null)
        {
            ReputationManager.Instance.LoadSaveData(data.reputation);
        }

        // 하이드아웃 모듈 레벨
        if (HideoutModuleManager.Instance != null)
        {
            HideoutModuleManager.Instance.LoadSaveData(data.hideoutModules);
        }

        // 위탁(전당포) 슬롯
        ShopUI.LoadConsignSave(data.consignSlots);

        // 아르바이트 보드 — 런타임 전용 상태라 로드 시 초기화(슬롯 간 이월 방지)
        // 의뢰 게시판(BD) — 동일 원칙 (오늘의 의뢰는 일자 시드라 재추첨돼도 같은 2장)
        QuestBoard.ResetRuntime();

        // 도감 발견 기록 — ★반드시 플레이어 격자 복원(아래)보다 먼저.
        // 격자 복원은 TryPlace를 타 OnItemPlaced(도감 발견 훅)를 발화시키는데, 발견 set이 먼저 채워져 있어야
        // Discover가 "이미 발견"으로 조용히 무시된다. 순서가 뒤바뀌면 로드마다 전 아이템 발견 토스트가 쏟아짐.
        // + 로드 구간 내내 SilentDiscovery — discoveredItems가 없는 구 세이브를 처음 열 때
        //   가방 내용물이 새로 발견 처리되며 토스트가 쏟아지는 것을 막는다(기록은 정상적으로 남음).
        if (CodexManager.Instance != null)
        {
            CodexManager.Instance.SilentDiscovery = true;
            CodexManager.Instance.LoadSaveData(data.discoveredItems);
        }

        // 레이드 지도 지식 복원 — 안전가옥 지도판에 누적 표시된다.
        RaidMapManager.InstanceIfExists?.LoadSaveData(data.raidMap);

        // 메인 창고(보관함)
        if (data.mainStash != null)
        {
            MainStash.Ensure().LoadSaveData(data.mainStash);
        }

        // 상점 판매 트레이에 걸려 있던 아이템을 창고로 복구(반드시 mainStash 로드 뒤).
        ShopUI.RestoreSellTrayToStash(data.sellTray);

        // 인벤토리(가방) + 장착 무기
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            // ★ 장비(가방)를 먼저 복원해야 가방 격자가 그 크기로 확장된다.
            //   (가방 아이템을 먼저 넣으면 0x0 격자라 공간 부족으로 사라지는 버그.)
            var eq = playerGO.GetComponent<PlayerEquipment>();
            if (eq != null)
            {
                eq.LoadSaveData(data.equippedWeapon);
                eq.SetEquippedWeaponAttachments(data.equippedWeaponAttachments);
            }

            var inv = playerGO.GetComponent<PlayerInventory>();
            if (inv != null && inv.Grid != null && data.bagItems != null)
                inv.Grid.LoadSaveData(data.bagItems);
            if (inv != null && inv.PocketsGrid != null && data.pocketItems != null)
                inv.PocketsGrid.LoadSaveData(data.pocketItems);
            if (inv != null && inv.SecureGrid != null && data.secureItems != null)
                inv.SecureGrid.LoadSaveData(data.secureItems);

            if (data.survival != null) SurvivalStats.Get()?.LoadSaveData(data.survival);
        }

        if (data.quickSlots != null && QuickSlotBar.Instance != null)
            QuickSlotBar.Instance.LoadSlotIds(data.quickSlots);

        if (data.traits != null && TraitManager.Instance != null)
            TraitManager.Instance.LoadSaveData(data.traits);

        if (data.progress != null)
            PlayerProgress.Instance.LoadSaveData(data.progress);

        // 창고 (uid 매칭)
        if (data.storageUnits != null)
        {
            foreach (var e in data.storageUnits)
            {
                var f = SafehouseStorage.AllFurniture.FirstOrDefault(x => x != null && x.uid == e.uid);
                if (f != null && f.grid != null) f.grid.LoadSaveData(e.items);
            }
        }

        // 일일 의뢰
        if (DailyQuestManager.Instance != null && data.dailyQuest != null)
        {
            DailyQuestManager.Instance.LoadSaveData(data.dailyQuest);
        }

        // 튜토리얼
        if (TutorialPrompt.Instance != null && data.shownTutorials != null)
        {
            TutorialPrompt.Instance.SetShownIds(new HashSet<string>(data.shownTutorials));
        }

        // 스토리 재생 기록
        if (StoryPlayer.Instance != null && data.playedScenes != null)
        {
            StoryPlayer.Instance.SetPlayedScenes(new HashSet<string>(data.playedScenes));
        }

        // 진행 중 퀘스트 복원
        if (QuestManager.Instance != null && data.activeQuests != null)
        {
            foreach (var entry in data.activeQuests)
            {
                var questData = Resources.Load<QuestData>($"Data/Quests/{entry.questId}");
                if (questData == null)   // 게시판 BD는 Board/ 서브폴더 — Resources.Load는 서브폴더 미탐색이라 폴백
                    questData = Resources.Load<QuestData>($"Data/Quests/Board/{entry.questId}");
                if (questData == null) continue;

                if (QuestManager.Instance.AcceptQuest(questData))
                {
                    var active = QuestManager.Instance.ActiveQuests
                        .Find(q => q.data.questId == entry.questId);
                    if (active != null)
                    {
                        active.state = (QuestState)entry.state;
                        if (entry.progress != null)
                        {
                            foreach (var p in entry.progress)
                                active.progress[p.index] = p.count;
                        }
                    }
                }
            }
        }

        // 로드 완료 — 이후 획득부터는 정상적으로 발견 토스트를 띄운다.
        if (CodexManager.Instance != null) CodexManager.Instance.SilentDiscovery = false;

        Debug.Log($"[Save] 로드 완료. 저장 시각: {data.saveTime}");
        return true;
    }

    /// <summary>
    /// 세이브 파일 존재 여부.
    /// </summary>
    public bool HasSave() => File.Exists(SavePath);
    public bool HasSave(int slot) => File.Exists(SlotPath(slot));

    /// <summary>슬롯 중 하나라도 세이브가 있는가 — 타이틀 '이어하기' 버튼 게이팅용
    /// (currentSlot 기준 HasSave()는 부팅 직후 슬롯0만 봐서 슬롯1/2 전용 세이브를 놓친다).</summary>
    public bool HasAnySave()
    {
        for (int i = 0; i < SlotCount; i++)
            if (File.Exists(SlotPath(i))) return true;
        return false;
    }

    /// <summary>슬롯 카드 요약 — 전체 로드/월드 적용 없이 파일만 역직렬화해 표시용 값만 뽑는다.</summary>
    public struct SlotSummary
    {
        public bool exists;
        public int level;
        public int traitCount;
        public int currency;
        public string saveTime;
    }

    public SlotSummary PeekSlot(int slot)
    {
        var s = new SlotSummary { exists = false, level = 1, traitCount = 0, currency = 0, saveTime = "" };
        try
        {
            string path = SlotPath(slot);
            if (!File.Exists(path)) return s;
            var data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
            if (data == null) return s;
            s.exists = true;
            s.level = data.progress != null ? Mathf.Max(1, data.progress.level) : 1;
            s.traitCount = data.traits != null && data.traits.unlocked != null ? data.traits.unlocked.Count : 0;
            s.currency = data.currency;
            s.saveTime = data.saveTime ?? "";
        }
        catch (System.Exception e) { Debug.LogWarning($"[Save] 슬롯{slot} 요약 실패: {e.Message}"); }
        return s;
    }

    /// <summary>
    /// 세이브 파일 삭제 (새 게임 시작).
    /// </summary>
    public void DeleteSave() => DeleteSave(currentSlot);

    public void DeleteSave(int slot)
    {
        string path = SlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[Save] 슬롯{slot} 세이브 삭제.");
        }
    }

    /// <summary>새 게임 시작 — 세이브가 없어도 이전 세션의 인메모리 상태가 이월되지 않도록
    /// 모든 영속(DontDestroyOnLoad) 시스템을 기본값으로 초기화한다. 씬-로컬(안전가옥 가구 등)은
    /// 씬 재로드로 자연 초기화되므로 제외. TitleScreen.OnNewGame에서 DeleteSave 뒤 호출.
    /// (Load()가 복원하는 시스템 목록과 1:1 대응 — 새 항목 추가 시 여기도 갱신.)</summary>
    public void ResetToNewGame()
    {
        // ── 스칼라 / 진행도 ──
        CurrencyManager.Instance?.LoadSaveData(0);
        ReputationManager.Instance?.LoadSaveData(0);
        PlayerProgress.Instance.LoadSaveData(new PlayerProgress.ProgressSaveData());   // Lv1/XP0
        TraitManager.Instance?.LoadSaveData(new TraitSaveData());                      // 해금·PP 초기화

        // ── 컬렉션형 매니저 ──
        NPCRelationshipManager.Instance?.ResetForNewGame();
        AchievementManager.Instance?.ResetForNewGame();
        HideoutModuleManager.Instance?.LoadSaveData(null);   // levels.Clear (전 모듈 Lv0)
        DailyQuestManager.Instance?.ResetForNewGame();
        QuestManager.Instance?.ResetForNewGame();            // 활성/완료 퀘스트 + 플래그
        StoryPlayer.Instance?.SetPlayedScenes(null);         // 재생 기록 초기화
        CodexManager.Instance?.ResetForNewGame();            // 도감 발견 기록 초기화
        RaidMapManager.InstanceIfExists?.ResetForNewGame();  // 레이드 지도 지식 초기화
        StoryTriggerManager.Instance?.ResetForNewGame();     // 프롤로그 재생 플래그 초기화(같은 세션 재시작 시 프롤로그 재생)
        TutorialPrompt.Instance?.SetShownIds(null);          // 튜토 1회성 기록 초기화

        // ── 게시판 런타임 상태 ──
        QuestBoard.ResetRuntime();

        // ── 창고 / 상점 / 안전가옥 가구 ──
        MainStash.Ensure()?.GetGrid()?.Clear();
        ShopUI.LoadConsignSave(null);   // 위탁 슬롯 비움
        ShopUI.ClearSellTray();         // 판매 트레이 비움
        SafehouseStorage.ResetForNewGame();   // 가구 격자 = static 리스트라 씬 재로드로 안 비워짐(검수 반영)

        // ── 플레이어(영속 PlayerRig): 장비 해제 → 격자 클리어 → 생존/체력/의료 복귀 ──
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerGO.GetComponent<PlayerEquipment>()?.ResetForNewGame();
            var inv = playerGO.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.Grid?.Clear();
                inv.PocketsGrid?.Clear();
                inv.SecureGrid?.Clear();
            }
            playerGO.GetComponent<SurvivalStats>()?.ResetForNewGame();
            playerGO.GetComponent<Health>()?.FullHeal();
        }
        else
        {
            // 정석 Systems-씬 빌드에선 PlayerRig가 영속이라 항상 존재. 없으면(폴백 부트 미완)
            // 인벤/장비 리셋이 스킵돼 이월될 수 있음 — 경고만(다음 안전가옥 진입 시 세이브 없음이라 실피해 낮음).
            Debug.LogWarning("[SaveManager] ResetToNewGame: Player 없음 — 인벤/장비 리셋 스킵(폴백 부트?)");
        }

        QuickSlotBar.Instance?.LoadSlotIds(new List<string>());   // 퀵슬롯 비움

        Debug.Log("[SaveManager] 새 게임 — 영속 상태 전체 초기화 완료");
    }

    // ═══════════════════════════
    //  자동 저장 트리거
    // ═══════════════════════════

    /// <summary>
    /// 안전가옥 진입, 휴식, 레이드 귀환 시 호출.
    /// </summary>
    public void AutoSave()
    {
        Save();
    }

}

// ═══════════════════════════
//  세이브 데이터 구조
// ═══════════════════════════

[System.Serializable]
public class GameSaveData
{
    public int version;
    public string saveTime;

    // 퀘스트
    public List<string> completedQuests = new List<string>();
    public List<FlagEntry> flags = new List<FlagEntry>();
    public List<ActiveQuestEntry> activeQuests = new List<ActiveQuestEntry>();

    // NPC 관계
    public List<NPCRelationshipSaveEntry> relationships;

    // 업적
    public AchievementManager.AchievementSaveData achievements;

    // 화폐(스크랩)
    public int currency;

    // 평판(전역 명성)
    public int reputation;

    // 하이드아웃 모듈 레벨
    public List<HideoutModuleSaveEntry> hideoutModules;

    // 위탁(전당포) 슬롯
    public List<ShopUI.ConsignSave> consignSlots;

    // 메인 창고(보관함)
    public List<GridItemEntry> mainStash;

    // 상점 판매 트레이(크래시 시 창고로 복구) — 비었으면 null
    public List<GridItemEntry> sellTray;

    // 인벤토리(가방) + 장착 무기
    public List<GridItemEntry> bagItems = new List<GridItemEntry>();
    public List<GridItemEntry> pocketItems = new List<GridItemEntry>();   // 주머니 4칸
    public List<GridItemEntry> secureItems = new List<GridItemEntry>();   // 보안 컨테이너 3×3
    public string equippedWeapon;
    public string[] equippedWeaponAttachments;   // 장착 주무기 부착물 itemId[4]

    // 퀵슬롯(1~6) — itemId 6개(빈 칸은 "")
    public List<string> quickSlots;

    // 생존 스탯 (수분/포만감)
    public SurvivalSaveData survival;

    // 특성(퍽) — PP·해금 목록
    public TraitSaveData traits;

    // 캐릭터 레벨/XP (traits.md §2 — PP 주 공급원)
    public PlayerProgress.ProgressSaveData progress;

    // 창고 (안전가옥 가구)
    public List<StorageUnitEntry> storageUnits = new List<StorageUnitEntry>();

    // 일일 의뢰
    public DailyQuestManager.DailyQuestSaveData dailyQuest;

    // 튜토리얼
    public List<string> shownTutorials = new List<string>();

    // 스토리
    public List<string> playedScenes = new List<string>();

    // 도감 — 발견한 itemId (docs/items.md §아이템 도감)
    public List<string> discoveredItems = new List<string>();

    // 레이드 지도 — 지역별 발견 존·통로 (docs/navigation.md §3.3 "안전가옥 지도판 누적")
    public RaidMapSaveData raidMap = new RaidMapSaveData();
}

[System.Serializable]
public class GridItemEntry
{
    public string itemId;
    public int count;
    public float durability;
    public int x;
    public int y;
    public bool rotated;
    public string[] attachments;   // 무기 부착물(파츠) itemId[4] — 없으면 null
    public int ammoCount;          // 탄창=든 탄 / 총기=장착 탄창에 남은 탄 (2026-07-29 총기)
    public string ammoItemId;      // 담긴 탄종. ammoCount가 0이면 무의미
    public List<GridItemEntry> containerItems;   // 보관함(컨테이너 아이템) 내부 격자 — 컨테이너만, 없으면 null
}

[System.Serializable]
public class StorageUnitEntry
{
    public int uid;
    public List<GridItemEntry> items = new List<GridItemEntry>();
}

[System.Serializable]
public class FlagEntry
{
    public string key;
    public bool value;
}

[System.Serializable]
public class ActiveQuestEntry
{
    public string questId;
    public int state;
    public List<ProgressEntry> progress = new List<ProgressEntry>();
}

[System.Serializable]
public class ProgressEntry
{
    public int index;
    public int count;
}

[System.Serializable]
public class NPCRelationshipSaveEntry
{
    public string npcId;
    public int affinity;
    public int trust;
    public int fear;
    public List<string> completedEvents = new List<string>();
    public List<FlagEntry> flags = new List<FlagEntry>();
}
