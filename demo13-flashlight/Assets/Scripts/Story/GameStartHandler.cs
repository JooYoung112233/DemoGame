using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 게임 최초 시작 처리: 안전가옥 첫 진입 시 **세이브 로드(이어하기)** 또는 **프롤로그(새 게임)**.
///
/// ★ 영속 자가 부트스트랩(DontDestroyOnLoad)으로 동작한다.
///   - 과거엔 Safehouse 씬에 직접 배치했으나, 그레이박스 빌더가 씬을 '맵만'으로 재생성하면
///     이 컴포넌트가 사라져 **이어하기 시 Load()가 호출되지 않아 아이템/스크랩이 날아가는 버그**가 있었다.
///   - 이제 씬 배치에 의존하지 않고 런타임에 1개를 보장하며, 안전가옥 로드 시 1회 init한다.
///   - 세션 가드(sessionInitialized)로 **레이드 귀환 등 재진입 때는 다시 로드하지 않는다**(진행 손실 방지).
/// </summary>
public class GameStartHandler : MonoBehaviour
{
    const string SCENE_SAFEHOUSE = "Safehouse";

    static GameStartHandler instance;
    static bool sessionInitialized;   // 한 세션(게임 실행)당 1회만 게임 시작 분기

    [Tooltip("프롤로그 전 대기 시간(초) — GameTuning.prologueStartDelay가 있으면 그 값 우선(폴백용).")]
    [SerializeField] float delayBeforePrologue = 0.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;          // 맵툴 씬에선 비활성
        if (instance != null) return;
        if (FindFirstObjectByType<GameStartHandler>(FindObjectsInactive.Include) != null) return; // 씬 배치본이 있으면 양보

        var go = new GameObject("[GameStartHandler]");
        DontDestroyOnLoad(go);
        go.AddComponent<GameStartHandler>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 부트스트랩 시점에 이미 안전가옥이 활성일 수도 있으니 한 번 확인.
        TryInit(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryInit(scene.name);

    /// <summary>타이틀로 돌아갈 때(새 게임/이어하기 누를 때) 세션 리셋 → 안전가옥 진입 시 다시 1회 init.</summary>
    public static void ResetSession() => sessionInitialized = false;

    void TryInit(string sceneName)
    {
        if (sessionInitialized) return;
        if (sceneName != SCENE_SAFEHOUSE) return;   // 게임 시작 분기는 안전가옥에서만
        sessionInitialized = true;
        StartCoroutine(InitGame());
    }

    IEnumerator InitGame()
    {
        // 싱글톤 초기화 + 플레이어 리그 스폰 대기 (GameBootstrap이 AfterSceneLoad에서 실행)
        yield return null;

        // 세이브 로드 시도 (이어하기)
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            bool loaded = SaveManager.Instance.Load();
            if (loaded && MainStash.Ensure().GrantStarterArmory()) SaveManager.Instance.Save();
            Debug.Log("[GameStart] 세이브 로드 완료. 정상 진행.");

            // 자동 트리거 체크 (로드된 플래그 기반)
            if (StoryPlayer.Instance != null)
                StoryPlayer.Instance.CheckAutoTriggers();

            yield break;
        }

        // ── 새 게임: 프롤로그 재생 ──
        // 새 게임은 타이틀에서 이미 스토리 페이드 오버레이(#2, sortingOrder 999)로 화면을 덮은 채 진입한다.
        // 여기서 #2를 유지(CoverInstant)하고 전환 커버(#1=OnGUI)를 제거해, 로드~프롤로그까지 검정이 끊기지 않게 한다.
        // 프롤로그 S-000이 같은 #2를 fade_in으로 드러내므로 seam/깜박임이 없다.
        Debug.Log("[GameStart] 새 게임. 프롤로그 시작.");

        // 새 게임 시작 상태(Lv1·돈0·특성 없음)를 슬롯에 즉시 기록 → 저장 슬롯 카드에 바로 뜬다.
        // 위 HasSave 분기를 이미 지난 뒤라 프롤로그 재생엔 영향 없음(타이틀에서 미리 저장하면 HasSave=true라 프롤로그 스킵됨).
        MainStash.Ensure().GrantStarterArmory();
        SaveManager.Instance?.Save();

        var stm  = SceneTransitionManager.Instance;
        var sem  = ScreenEffectManager.Instance;
        if (sem != null) sem.CoverInstant();   // #2 즉시 검정(이미 덮여 있으면 유지 — lerp 없이 깜박임 0)
        if (stm != null) stm.ClearCover();      // #1(OnGUI 커버) 제거 — #2가 덮고 있어 화면 변화 없음

        float startDelay = GameTuning.Instance != null ? GameTuning.Instance.prologueStartDelay : delayBeforePrologue;
        yield return new WaitForSecondsRealtime(startDelay);

        bool started = false;
        if (StoryTriggerManager.Instance != null)
            started = StoryTriggerManager.Instance.PlayPrologueAuto();
        if (!started && StoryPlayer.Instance != null)
        {
            StoryPlayer.Instance.PlayScene("S-000");
            started = StoryPlayer.Instance.IsPlaying;
        }

        // 안전망: 프롤로그가 실제로 시작되지 않았다면(중복 방지 플래그/씬 누락) 검은 화면에 갇히지 않게 드러낸다.
        if (!started)
        {
            if (sem != null) yield return sem.FadeIn(0.5f);
            else if (stm != null) stm.ClearCover();
        }
    }
}
