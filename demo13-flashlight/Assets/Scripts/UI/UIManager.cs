using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// UI 매니저 싱글톤 (DontDestroyOnLoad).
/// 모든 게임 UI 패널을 중앙 관리.
/// 어떤 씬에서 Play 해도 자동 생성됨 (RuntimeInitializeOnLoadMethod).
/// </summary>
[DefaultExecutionOrder(-50)]   // ESC를 per-UI보다 먼저 잡아 중첩/일시정지 충돌 방지
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("메인 게임 HUD (HP/스태미너/부상)")]
    [SerializeField] GameHUD gameHUD;

    [Tooltip("귀환 정산 UI")]
    [SerializeField] RaidResultUI raidResultUI;

    [Tooltip("지역 선택 UI (지도판)")]
    [SerializeField] MapSelectUI mapSelectUI;

    [Tooltip("통합 캐릭터 패널 (인벤토리/의료/정보 탭)")]
    [SerializeField] CharacterPanelUI characterPanelUI;

    [Tooltip("제작/수리 UI (작업대/의료대/조리대)")]
    [SerializeField] CraftingUI craftingUI;
    [SerializeField] ShopUI shopUI;

    [Tooltip("NPC 대화 UI")]
    [SerializeField] DialogueUI dialogueUI;

    [Tooltip("레이드 후 이벤트 UI")]
    [SerializeField] PostRaidEventUI postRaidEventUI;

    [Tooltip("퀘스트 HUD")]
    [SerializeField] QuestHUD questHUD;

    [Tooltip("내비게이션 HUD (시계 나침반 + 미니맵)")]
    [SerializeField] NavigationHUD navigationHUD;

    /// <summary>현재 안전가옥인지 (timeScale=0 상태)</summary>
    public bool IsSafehouse => isSafehouse;
    bool isSafehouse;

    /// <summary>
    /// 어떤 씬에서 Play 해도 자동 부트스트래핑.
    /// 씬에 UIManager가 있으면 그걸 사용, 없으면 코드로 생성.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // 맵툴 씬에선 게임 HUD(체력/재화 등) 자동 생성 안 함 — 맵 편집/미리보기 전용.
        if (MapToolScene.IsActive) return;

        // Systems 씬이 UIManager(+모든 UI)를 공급하면 코드 스폰 폴백을 건너뛴다.
        if (SystemsScene.ProvidesSystems) return;

        // 씬에 배치된 게 Awake에서 이미 Instance가 됐으면 스킵
        if (Instance != null) return;

        // 씬에 비활성 상태로라도 있는지 한번 더 확인
        var existing = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (existing != null) return;

        // 없으면 자동 생성
        var go = new GameObject("[UIManager]");
        go.AddComponent<UIManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureEventSystem();
        EnsureChildren();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 전환 후 EventSystem이 없으면 다시 확보
        EnsureEventSystem();

        // 안전가옥도 '걸어다니는 허브' → 물리/이동(FixedUpdate)을 위해 timeScale=1 유지.
        //   (timeScale=0이면 Rigidbody2D 이동이 얼어 맵보드/침대까지 못 감)
        //   낮/밤·지역시계는 아래 ActiveRegionId=null + 이벤트 기반(T키)이라 timeScale과 무관하게 정지됨.
        isSafehouse = (scene.name == "Safehouse");
        Time.timeScale = 1f;

        // 안전가옥 귀환 시 활성 지역 해제
        if (isSafehouse && RegionTimeManager.Instance != null)
            RegionTimeManager.Instance.ActiveRegionId = null;

        // 캐릭터 패널 레퍼런스 리셋 (Player가 새로 로드되므로)
        if (characterPanelUI != null)
        {
            characterPanelUI.ResetRefs();
            characterPanelUI.Hide();
        }

        if (craftingUI != null)
        {
            craftingUI.ResetRefs();
            craftingUI.Hide();
        }
    }

    /// <summary>EventSystem이 씬에 없으면 자동 생성 (uGUI 클릭 필수)</summary>
    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("[EventSystem]");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            DontDestroyOnLoad(esGO);
        }
    }

    /// <summary>필요한 자식 UI 컴포넌트 자동 생성 (인스펙터 미할당 시에만)</summary>
    void EnsureChildren()
    {
        // GameHUD — 씬/인스펙터에서 할당했으면 그대로 사용
        if (gameHUD == null)
            gameHUD = GetComponentInChildren<GameHUD>(true);
        if (gameHUD == null)
        {
            var hud = new GameObject("GameHUD");
            hud.transform.SetParent(transform);
            gameHUD = hud.AddComponent<GameHUD>();
        }

        // RaidResultUI
        if (raidResultUI == null)
            raidResultUI = GetComponentInChildren<RaidResultUI>(true);
        if (raidResultUI == null)
        {
            var raid = new GameObject("RaidResultUI");
            raid.transform.SetParent(transform);
            raidResultUI = raid.AddComponent<RaidResultUI>();
        }

        // MapSelectUI
        if (mapSelectUI == null)
            mapSelectUI = GetComponentInChildren<MapSelectUI>(true);
        if (mapSelectUI == null)
        {
            var map = new GameObject("MapSelectUI");
            map.transform.SetParent(transform);
            mapSelectUI = map.AddComponent<MapSelectUI>();
        }

        // CharacterPanelUI
        if (characterPanelUI == null)
            characterPanelUI = GetComponentInChildren<CharacterPanelUI>(true);
        if (characterPanelUI == null)
        {
            var charPanel = new GameObject("CharacterPanelUI");
            charPanel.transform.SetParent(transform);
            characterPanelUI = charPanel.AddComponent<CharacterPanelUI>();
        }

        // CraftingUI
        if (craftingUI == null)
            craftingUI = GetComponentInChildren<CraftingUI>(true);
        if (craftingUI == null)
        {
            var craftGO = new GameObject("CraftingUI");
            craftGO.transform.SetParent(transform);
            craftingUI = craftGO.AddComponent<CraftingUI>();
        }

        // ShopUI
        if (shopUI == null)
            shopUI = GetComponentInChildren<ShopUI>(true);
        if (shopUI == null)
        {
            var shopGO = new GameObject("ShopUI");
            shopGO.transform.SetParent(transform);
            shopUI = shopGO.AddComponent<ShopUI>();
        }

        // DialogueUI
        if (dialogueUI == null)
            dialogueUI = GetComponentInChildren<DialogueUI>(true);
        if (dialogueUI == null)
        {
            var dlgGO = new GameObject("DialogueUI");
            dlgGO.transform.SetParent(transform);
            dialogueUI = dlgGO.AddComponent<DialogueUI>();
        }

        // PostRaidEventUI
        if (postRaidEventUI == null)
            postRaidEventUI = GetComponentInChildren<PostRaidEventUI>(true);
        if (postRaidEventUI == null)
        {
            var preGO = new GameObject("PostRaidEventUI");
            preGO.transform.SetParent(transform);
            postRaidEventUI = preGO.AddComponent<PostRaidEventUI>();
        }

        // QuestHUD
        if (questHUD == null)
            questHUD = GetComponentInChildren<QuestHUD>(true);
        if (questHUD == null)
        {
            var qhGO = new GameObject("QuestHUD");
            qhGO.transform.SetParent(transform);
            questHUD = qhGO.AddComponent<QuestHUD>();
        }

        // NavigationHUD (시계 나침반 + 미니맵)
        if (navigationHUD == null)
            navigationHUD = GetComponentInChildren<NavigationHUD>(true);
        if (navigationHUD == null)
        {
            var navGO = new GameObject("NavigationHUD");
            navGO.transform.SetParent(transform);
            navigationHUD = navGO.AddComponent<NavigationHUD>();
        }
    }

    void Update()
    {
        // ── ESC: 맨 위(가장 최근) UI 1개만 닫기(LIFO). 없으면 일시정지 (중앙 권위) ──
        if (GameInput.GetKeyDown(KeyCode.Escape))
        {
            // 아이템 사용(채널) 중이면 ESC = 사용 취소. 설정/일시정지 창은 열지 않는다.
            if (UseActionManager.Instance != null && UseActionManager.Instance.IsBusy)
            {
                UseActionManager.Instance.Cancel();
                ToastManager.Show("사용 취소", ToastManager.ToastType.Info);
                return;
            }
            if (CloseTopmost()) return;
            if (!HideoutController.IsActive) PauseMenu.Show();   // 하이드아웃에선 HideoutController가 ESC=나가기확인 처리
            return;
        }

        // ── Tab: 캐릭터 패널 토글 — 다른 UI 열려있으면 무시(중첩 금지) ──
        if (GameInput.GetKeyDown(KeyCode.Tab))
        {
            if (characterPanelUI != null && characterPanelUI.IsShowing)
                characterPanelUI.Hide();
            else if (!IsAnyUIOpen() && characterPanelUI != null)
                ShowCharacterPanel();
        }

        // 하이드아웃 모듈 업그레이드 UI(HideoutUI)는 전역 단축키로 열지 않는다.
        // → 하이드아웃 실내(HideoutController)의 '시설 관리' 버튼으로만 진입(하이드아웃 안에서만).
    }

    /// <summary>캐릭터 패널 표시 (인벤토리/의료/정보 탭)</summary>
    public void ShowCharacterPanel()
    {
        if (characterPanelUI != null)
            characterPanelUI.Show();
    }

    /// <summary>캐릭터 패널 + 루팅 상자 표시</summary>
    public void ShowCharacterPanelWithContainer(LootContainer container)
    {
        if (characterPanelUI != null)
            characterPanelUI.ShowWithContainer(container);
    }

    /// <summary>캐릭터 패널 + 안전가옥 창고 표시</summary>
    public void ShowCharacterPanelWithStorage(SafehouseStorage storage)
    {
        if (characterPanelUI != null)
            characterPanelUI.ShowWithStorage(storage);
    }

    /// <summary>캐릭터 패널 + 메인 창고(보관함) 표시 — 하이드아웃 창고 시설용</summary>
    public void ShowCharacterPanelWithStash()
    {
        if (characterPanelUI != null)
            characterPanelUI.ShowWithStash();
    }

    /// <summary>귀환 정산 UI 표시</summary>
    public void ShowRaidResult()
    {
        if (raidResultUI != null)
            raidResultUI.Show();
    }

    /// <summary>지역 선택 UI 표시 (지도판)</summary>
    public void ShowMapSelect()
    {
        if (mapSelectUI != null)
            mapSelectUI.Show();
    }

    /// <summary>제작 UI 표시 (작업대/의료대/조리대)</summary>
    public void ShowCrafting(CraftingStation station)
    {
        if (characterPanelUI != null && characterPanelUI.IsShowing)
            characterPanelUI.Hide();

        if (craftingUI != null)
            craftingUI.Show(station);
    }

    /// <summary>상점(전당포) 거래 UI 열기.</summary>
    public void ShowShop(ShopData shop)
    {
        if (shop == null || shopUI == null) return;
        if (dialogueUI != null && dialogueUI.IsShowing) dialogueUI.Hide();
        if (NarrationUI.Instance != null && NarrationUI.Instance.IsShowing) NarrationUI.Instance.Dismiss();  // 하단 독백 잔류 방지

        shopUI.Open(shop);
    }

    /// <summary>
    /// 맨 위(가장 최근에 열린) UI 1개만 닫는다 — Esc 한 번에 하나씩(LIFO).
    /// 위에 뜨는 팝업 → 캐릭터 패널(내부 레이어 위임) → 기타 base 패널 → 모달 순.
    /// 닫을 게 있으면 true.
    /// </summary>
    bool CloseTopmost()
    {
        // 1) base 패널 위에 뜨는 독립 팝업 (최상위부터)
        if (SettingsUI.IsShowing) { SettingsUI.Hide(); return true; }   // 일시정지 위 레이어 — PauseMenu보다 먼저
        if (ItemDetailUI.IsShowing) { ItemDetailUI.Hide(); return true; }
        if (NoteUI.Instance != null && NoteUI.Instance.IsShowing) { NoteUI.Instance.Close(); return true; }
        if (GroundPickupUI.IsShowing) { GroundPickupUI.Hide(); return true; }
        if (TraitPanelUI.IsShowing) { TraitPanelUI.Hide(); return true; }

        // 2) 캐릭터 패널 — 내부 레이어(컨텍스트/사용/컨테이너팝업/드래그)부터 LIFO로 자체 처리
        if (characterPanelUI != null && characterPanelUI.IsShowing)
            return characterPanelUI.HandleEscape();

        // 3) 기타 base 패널 (보통 동시에 하나만 열림)
        if (shopUI != null && shopUI.IsShowing) { shopUI.Close(); return true; }
        if (craftingUI != null && craftingUI.IsShowing) { craftingUI.Hide(); return true; }
        if (mapSelectUI != null && mapSelectUI.IsShowing) { mapSelectUI.Hide(); return true; }
        if (dialogueUI != null && dialogueUI.IsShowing) { dialogueUI.Hide(); return true; }
        if (RadioUI.IsShowing) { RadioUI.Hide(); return true; }
        if (DispatchUI.IsShowing) { DispatchUI.Hide(); return true; }
        if (QuestLogUI.IsShowing) { QuestLogUI.Hide(); return true; }
        if (RaidMapUI.IsShowing) { RaidMapUI.Hide(); return true; }
        if (HideoutUI.Instance != null && HideoutUI.Instance.IsShowing) { HideoutUI.Instance.Close(); return true; }
        if (SleepUI.Instance != null && SleepUI.Instance.IsShowing) { SleepUI.Instance.Close(); return true; }
        // PostRaidEventUI는 선택지로만 닫히는 모달 → Esc 대상 아님(원래 CloseAll에도 없음).
        if (raidResultUI != null && raidResultUI.IsShowing) { raidResultUI.Hide(); return true; }

        // 4) 일시정지 메뉴
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsShowing) { PauseMenu.Instance.Hide(); return true; }
        return false;
    }

    /// <summary>모든 UI 닫기</summary>
    public void CloseAll()
    {
        if (raidResultUI != null)
            raidResultUI.Hide();
        if (mapSelectUI != null)
            mapSelectUI.Hide();
        if (characterPanelUI != null)
            characterPanelUI.Hide();
        if (craftingUI != null)
            craftingUI.Hide();
        if (shopUI != null)
            shopUI.Close();
        if (dialogueUI != null)
            dialogueUI.Hide();
        if (PauseMenu.Instance != null)
            PauseMenu.Instance.Hide();
        if (HideoutUI.Instance != null)
            HideoutUI.Instance.Close();
        if (SleepUI.Instance != null)
            SleepUI.Instance.Close();
        if (RadioUI.IsShowing) RadioUI.Hide();
        if (DispatchUI.IsShowing) DispatchUI.Hide();
        if (QuestLogUI.IsShowing) QuestLogUI.Hide();
        if (RaidMapUI.IsShowing) RaidMapUI.Hide();
        if (GroundPickupUI.IsShowing) GroundPickupUI.Hide();
        if (ItemDetailUI.IsShowing) ItemDetailUI.Hide();
        if (SettingsUI.IsShowing) SettingsUI.Hide();
    }

    /// <summary>현재 어떤 UI든 열려있는지</summary>
    public bool IsAnyUIOpen()
    {
        if (raidResultUI != null && raidResultUI.IsShowing) return true;
        if (mapSelectUI != null && mapSelectUI.IsShowing) return true;
        if (characterPanelUI != null && characterPanelUI.IsShowing) return true;
        if (craftingUI != null && craftingUI.IsShowing) return true;
        if (shopUI != null && shopUI.IsShowing) return true;
        if (dialogueUI != null && dialogueUI.IsShowing) return true;
        if (postRaidEventUI != null && postRaidEventUI.IsShowing) return true;
        if (NoteUI.Instance != null && NoteUI.Instance.IsShowing) return true;
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsShowing) return true;
        if (HideoutUI.Instance != null && HideoutUI.Instance.IsShowing) return true;
        if (SleepUI.Instance != null && SleepUI.Instance.IsShowing) return true;
        if (RadioUI.IsShowing) return true;
        if (DispatchUI.IsShowing) return true;
        if (QuestLogUI.IsShowing) return true;
        if (RaidMapUI.IsShowing) return true;
        if (GroundPickupUI.IsShowing) return true;
        if (ItemDetailUI.IsShowing) return true;
        if (TraitPanelUI.IsShowing) return true;
        if (SettingsUI.IsShowing) return true;
        return false;
    }
}
