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

    /// <summary>현재 안전가옥인지 (timeScale=0 상태)</summary>
    public bool IsSafehouse => isSafehouse;
    bool isSafehouse;

    // 향후 추가될 UI들
    // [SerializeField] InventoryUI inventoryUI;
    // [SerializeField] MedicalUI medicalUI;

    /// <summary>
    /// 어떤 씬에서 Play 해도 자동 부트스트래핑.
    /// 씬에 UIManager가 있으면 그걸 사용, 없으면 코드로 생성.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // 씬에 배치된 게 Awake에서 이미 Instance가 됐으면 스킵
        if (Instance != null) return;

        // 씬에 비활성 상태로라도 있는지 한번 더 확인
        var existing = FindObjectOfType<UIManager>(true);
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
    }

    /// <summary>EventSystem이 씬에 없으면 자동 생성 (uGUI 클릭 필수)</summary>
    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
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

    /// <summary>모든 UI 닫기</summary>
    public void CloseAll()
    {
        if (raidResultUI != null)
            raidResultUI.Hide();
        if (mapSelectUI != null)
            mapSelectUI.Hide();
    }

    /// <summary>현재 어떤 UI든 열려있는지</summary>
    public bool IsAnyUIOpen()
    {
        if (raidResultUI != null && raidResultUI.IsShowing) return true;
        if (mapSelectUI != null && mapSelectUI.IsShowing) return true;
        return false;
    }
}
