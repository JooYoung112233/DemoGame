using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 씬 전환 매니저.
/// 페이드 인/아웃 + 스폰 포인트 + 로딩.
/// DontDestroyOnLoad 싱글톤.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade")]
    [Tooltip("페이드 인/아웃 소요 시간(초)")]
    [SerializeField] float fadeDuration = 0.5f;

    [Tooltip("페이드 색상 (보통 검정)")]
    [SerializeField] Color fadeColor = Color.black;

    // 다음 씬에서 플레이어를 배치할 스폰 포인트 ID
    public static string PendingSpawnPointId { get; private set; }

    float fadeAlpha;
    bool isFading;
    Texture2D fadeTex;
    bool isTransitioning;

    // 탈출 카운트���운 UI
    float exitCountdown;
    float exitCountdownMax;
    bool isCountingDown;

    // 탈출 취소용
    Transform exitSource;       // 탈출구 Transform
    float exitCancelRange;      // 이 거리 이상 벗어나면 취소
    Coroutine transitionCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // Systems 씬이 SceneTransitionManager를 배치 공급하면 코드 스폰 폴백을 건너뛴다.
        if (SystemsScene.ProvidesSystems) return;
        if (Instance != null) return;
        if (FindFirstObjectByType<SceneTransitionManager>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[SceneTransitionManager]");
        go.AddComponent<SceneTransitionManager>();
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

        fadeTex = new Texture2D(1, 1);
        fadeTex.SetPixel(0, 0, Color.white);
        fadeTex.Apply();

        // 씬 로드 시 스폰 포인트 처리
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>씬 전환 요청 (즉시).</summary>
    /// <param name="instantCover">true면 페이드 없이 화면을 즉시 검게 덮은 채 로드(부팅/새게임 셋업 중 HUD 깜빡임 차단). 로드 후 reveal.</param>
    /// <param name="keepCovered">true면 로드 후 reveal(페이드 인)을 생략하고 검게 덮은 채 둔다.
    /// 새 게임처럼 곧바로 프롤로그가 자체 페이드로 화면을 이어받는 경우 — 중간에 안전가옥이 번쩍이는 것을 막는다.
    /// 이때 커버 해제는 호출측(GameStartHandler)이 ClearCover/RevealRoutine으로 책임진다.</param>
    public void TransitionTo(string sceneName, string spawnPointId = "", bool instantCover = false, bool keepCovered = false)
    {
        if (isTransitioning) return;
        // timeScale=0(안전가옥)에서 출전 시 복구
        Time.timeScale = 1f;
        PendingSpawnPointId = spawnPointId;
        if (instantCover) fadeAlpha = 1f;
        transitionCoroutine = StartCoroutine(TransitionRoutine(sceneName, 0f, instantCover, keepCovered));
    }

    /// <summary>즉시 화면을 검게 덮는다(페이드 없이). 부팅/새게임 셋업 중 HUD(HP바 등) 깜빡임 차단용.</summary>
    public void CoverInstant() { fadeAlpha = 1f; }

    /// <summary>즉시 커버를 제거한다(페이드 없이). 다른 오버레이(스토리 페이드)가 이미 화면을 덮고 있어 번쩍임 없이 넘길 때.</summary>
    public void ClearCover() { fadeAlpha = 0f; isFading = false; }

    /// <summary>덮인 화면을 페이드로 드러낸다(reveal). 셋업 완료 후 호출.</summary>
    public IEnumerator RevealRoutine() { yield return StartCoroutine(FadeRoutine(fadeAlpha, 0f)); }

    /// <summary>씬 전환 요청 (대기 시간 포함, 탈출구용).</summary>
    /// <param name="source">탈출구 Transform. 플레이어가 여기서 cancelRange 이상 벗어나면 취소</param>
    /// <param name="cancelRange">취소 거리 (기본 3m)</param>
    public void TransitionWithDelay(string sceneName, string spawnPointId, float waitTime, Transform source = null, float cancelRange = 3f)
    {
        if (isTransitioning) return;
        PendingSpawnPointId = spawnPointId;
        exitSource = source;
        exitCancelRange = cancelRange;
        transitionCoroutine = StartCoroutine(TransitionRoutine(sceneName, waitTime));
    }

    /// <summary>탈출 취소 (거리 이탈 또는 외부 호출)</summary>
    public void CancelTransition()
    {
        if (!isCountingDown) return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        isCountingDown = false;
        isTransitioning = false;
        exitSource = null;
        transitionCoroutine = null;
        Debug.Log("[SceneTransition] 탈출 취소됨");
    }

    IEnumerator TransitionRoutine(string sceneName, float waitTime, bool startCovered = false, bool keepCovered = false)
    {
        isTransitioning = true;

        // 탈출 대기 (카운트다운 UI 표시 + 거리 이탈 시 취소)
        if (waitTime > 0)
        {
            Debug.Log($"[SceneTransition] {waitTime}초 대기 중...");
            exitCountdownMax = waitTime;
            exitCountdown = waitTime;
            isCountingDown = true;

            Transform playerT = GameObject.FindGameObjectWithTag("Player")?.transform;

            while (exitCountdown > 0)
            {
                // 거리 체크 — 탈출구에서 너무 멀어지면 취소
                if (exitSource != null && playerT != null)
                {
                    float dist = Vector3.Distance(playerT.position, exitSource.position);
                    if (dist > exitCancelRange)
                    {
                        CancelTransition();
                        yield break;
                    }
                }

                exitCountdown -= Time.deltaTime;
                yield return null;
            }

            isCountingDown = false;
            exitSource = null;
        }

        // 페이드 아웃 (이미 즉시 커버됐으면 건너뜀 — HUD 깜빡임 없이 바로 검정)
        if (startCovered)
            fadeAlpha = 1f;
        else
            yield return StartCoroutine(FadeRoutine(0f, 1f));

        // 씬 로드
        if (SystemsScene.Available)
        {
            // Systems(부트) 씬은 유지하고, 게임플레이 콘텐츠 씬만 additive로 교체한다.
            // 1) 현재 게임플레이 씬 기억 → 2) 새 씬 additive 로드 → 3) 새 씬을 Active로 →
            // 4) 이전 게임플레이 씬 언로드 (Systems는 절대 언로드 안 함).
            Scene prev = SceneManager.GetActiveScene();
            bool prevIsGameplay = SystemsScene.IsGameplayScene(prev) && prev.isLoaded;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op != null)
            {
                while (!op.isDone)
                    yield return null;
            }

            Scene loaded = SceneManager.GetSceneByName(sceneName);
            if (loaded.IsValid())
                SceneManager.SetActiveScene(loaded);

            if (prevIsGameplay && prev.IsValid() && prev != loaded)
            {
                AsyncOperation un = SceneManager.UnloadSceneAsync(prev);
                if (un != null)
                {
                    while (!un.isDone)
                        yield return null;
                }
            }
        }
        else
        {
            // 폴백(Systems 미빌드): 기존 단일(Single) 로드 — DontDestroyOnLoad로 영속 객체 유지.
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                    yield return null;
            }
        }

        // 페이드 인 (OnSceneLoaded에서 스폰 처리 후)
        yield return new WaitForSeconds(0.1f); // 씬 초기화 대기
        // keepCovered면 reveal 생략 — 곧바로 이어질 프롤로그 페이드가 화면을 넘겨받는다(안전가옥 번쩍임 방지).
        if (!keepCovered)
            yield return StartCoroutine(FadeRoutine(1f, 0f));

        isTransitioning = false;
    }

    IEnumerator FadeRoutine(float from, float to)
    {
        isFading = true;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Lerp(from, to, timer / fadeDuration);
            yield return null;
        }

        fadeAlpha = to;
        isFading = to > 0; // 완전 투명이면 fading 종료
    }

    void OnGUI()
    {
        // 탈출 카운트다운 UI
        if (isCountingDown)
            DrawExitCountdown();

        // 페이드 오버레이
        if (fadeAlpha <= 0) return;

        Color prev = GUI.color;
        GUI.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTex);
        GUI.color = prev;
    }

    void DrawExitCountdown()
    {
        float remaining = Mathf.Max(0f, exitCountdown);
        float progress = 1f - (remaining / exitCountdownMax);

        // --- 바 배경 (화면 중앙 하단) ---
        float barWidth = 300f;
        float barHeight = 24f;
        float barX = (Screen.width - barWidth) * 0.5f;
        float barY = Screen.height * 0.75f;

        // 배경 (어두운 박스)
        Rect bgRect = new Rect(barX - 4, barY - 4, barWidth + 8, barHeight + 8);
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(bgRect, fadeTex);

        // 프로그레스 바 (채워지는 형태)
        Rect fillRect = new Rect(barX, barY, barWidth * progress, barHeight);
        GUI.color = Color.Lerp(new Color(1f, 0.6f, 0.1f), new Color(0.2f, 1f, 0.4f), progress);
        GUI.DrawTexture(fillRect, fadeTex);

        // 테두리
        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        GUI.Box(new Rect(barX, barY, barWidth, barHeight), GUIContent.none);

        // 텍스트
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        string text = $"탈출 중... {remaining:F1}초";
        Rect textRect = new Rect(barX, barY - 30, barWidth, 28);
        GUI.Label(textRect, text, style);

        GUI.color = Color.white;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 스폰 포인트로 플레이어 이동
        if (string.IsNullOrEmpty(PendingSpawnPointId)) return;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) { PendingSpawnPointId = ""; return; }

        // ★ 방금 로드된 '목적지 씬'의 스폰만 후보로 한정한다.
        //   (건물 전환 중 옛 씬이 아직 안 내려가 같은 id "default"가 둘 존재하면
        //    엉뚱한 씬의 스폰으로 순간이동하던 버그 방지.)
        var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        SpawnPoint match = null, fallbackDefault = null, anyInScene = null;
        foreach (var sp in spawnPoints)
        {
            if (sp == null) continue;
            if (sp.gameObject.scene != scene) continue;   // 목적지 씬만
            if (anyInScene == null) anyInScene = sp;
            if (sp.PointId == PendingSpawnPointId) { match = sp; break; }
            if (sp.PointId == "default") fallbackDefault = sp;
        }

        var target = match ?? fallbackDefault ?? anyInScene;
        if (target != null)
        {
            playerGO.transform.position = target.transform.position;
            CameraFollow.Instance?.SnapToTarget();   // 카메라도 즉시 스냅 → 슬라이드 방지
            if (match == null)
                Debug.LogWarning($"[SceneTransition] 스폰 '{PendingSpawnPointId}' 미발견 → 대체 '{target.PointId}' 사용 (씬 {scene.name}).");
            else
                Debug.Log($"[SceneTransition] 스폰: {PendingSpawnPointId} → {target.transform.position}");
        }
        else
        {
            Debug.LogWarning($"[SceneTransition] 씬 '{scene.name}'에 스폰포인트 없음 — 플레이어 위치 유지.");
        }

        PendingSpawnPointId = "";
    }
}
