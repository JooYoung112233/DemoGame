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
    public void TransitionTo(string sceneName, string spawnPointId = "")
    {
        if (isTransitioning) return;
        // timeScale=0(안전가옥)에서 출전 시 복구
        Time.timeScale = 1f;
        PendingSpawnPointId = spawnPointId;
        transitionCoroutine = StartCoroutine(TransitionRoutine(sceneName, 0f));
    }

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

    IEnumerator TransitionRoutine(string sceneName, float waitTime)
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

        // 페이드 아웃
        yield return StartCoroutine(FadeRoutine(0f, 1f));

        // 씬 로드
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op != null)
        {
            while (!op.isDone)
                yield return null;
        }

        // 페이드 인 (OnSceneLoaded에서 스폰 처리 후)
        yield return new WaitForSeconds(0.1f); // 씬 초기화 대기
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

        var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        foreach (var sp in spawnPoints)
        {
            if (sp.PointId == PendingSpawnPointId)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    playerGO.transform.position = sp.transform.position;

                    Debug.Log($"[SceneTransition] 스폰: {PendingSpawnPointId} → {sp.transform.position}");
                }
                break;
            }
        }

        PendingSpawnPointId = "";
    }
}
