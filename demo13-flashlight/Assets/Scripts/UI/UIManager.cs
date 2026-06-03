using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// UI 매니저 싱글톤 (DontDestroyOnLoad).
/// 모든 게임 UI 패널을 중앙 관리.
/// 어떤 씬에서 Play 해도 자동 생성됨 (RuntimeInitializeOnLoadMethod).
/// </summary>
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

        // 안전가옥: 게임 시간 정지 (낮밤, 배터리 소모 등 멈춤)
        // 전투 지역: 정상 흐름
        isSafehouse = (scene.name == "Safehouse");
        Time.timeScale = isSafehouse ? 0f : 1f;

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
            esGO.AddComponent<StandaloneInputModule>();
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
    }

    void Update()
    {
        // ESC 키: 열린 UI 닫기 (우선순위: 캐릭터패널 > 맵선택 > 정산)
        // 각 패널이 자체 Update()에서도 ESC 처리하지만,
        // UIManager가 중앙에서 한 번 더 잡아주면 누락 없이 안전.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (craftingUI != null && craftingUI.IsShowing)
            {
                // CraftingUI.Update()가 처리
            }
            else if (characterPanelUI != null && characterPanelUI.IsShowing)
            {
                // CharacterPanelUI.Update()가 처리 — 여기선 스킵
            }
            else if (mapSelectUI != null && mapSelectUI.IsShowing)
            {
                mapSelectUI.Hide();
            }
            // RaidResultUI는 showTimer > 1f 조건이 있으므로 자체 처리에 맡김
        }

        // Tab 키: 캐릭터 패널 토글 (제작 UI 열림 시 무시)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (craftingUI != null && craftingUI.IsShowing)
                return;

            if (characterPanelUI != null)
            {
                if (characterPanelUI.IsShowing)
                    characterPanelUI.Hide();
                else
                    ShowCharacterPanel();
            }
        }
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

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        var inv = playerGO != null ? playerGO.GetComponent<PlayerInventory>() : null;
        shopUI.Open(shop, inv);
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
        return false;
    }
}
