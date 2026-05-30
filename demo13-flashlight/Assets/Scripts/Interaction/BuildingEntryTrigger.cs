using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 건물 입장 트리거.
/// 플레이어가 트리거 영역에 들어오면 프롬프트를 표시하고,
/// 상호작용(E키) 시 지정된 씬/스폰 포인트로 전환.
///
/// 모드:
/// - SceneTransition: 다른 씬으로 전환 (건물 내부 등)
/// - LocalTeleport: 같은 씬 내 다른 위치로 이동
/// - StoryTrigger: 스토리 씬 재생
/// - CustomEvent: customData 기반 커스텀 이벤트
///
/// MapObjectSpawner에서 MapObjectType.Trigger로 자동 생성.
/// </summary>
public class BuildingEntryTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        SceneTransition,  // 다른 씬으로 전환
        LocalTeleport,    // 같은 씬 내 이동
        StoryTrigger,     // 스토리 씬 재생
        CustomEvent       // 커스텀 이벤트 (customData 기반)
    }

    [Header("트리거 설정")]
    [SerializeField] TriggerMode mode = TriggerMode.SceneTransition;
    [SerializeField] string promptText = "E — 입장";
    [SerializeField] bool autoEnter;              // true면 E키 없이 자동 입장
    [SerializeField] float transitionDelay;        // 0이면 즉시 전환

    [Header("씬 전환 (SceneTransition)")]
    [SerializeField] string targetScene;
    [SerializeField] string targetSpawnId;

    [Header("로컬 이동 (LocalTeleport)")]
    [SerializeField] Vector3 teleportPosition;
    [SerializeField] float teleportYRotation;

    [Header("스토리 (StoryTrigger)")]
    [SerializeField] string storySceneId;

    [Header("커스텀")]
    [SerializeField] string customData;

    [Header("조건")]
    [SerializeField] bool oneShot;                 // 1회만 발동
    [SerializeField] string requiredKeyId;         // 필요 아이템 (비어있으면 무조건)
    [SerializeField] string requiredQuestId;       // 완료 필요 퀘스트

    [Header("콜라이더")]
    [SerializeField] Vector3 triggerSize = new Vector3(1.5f, 2f, 1.5f);
    [SerializeField] Vector3 triggerCenter = new Vector3(0, 1f, 0);

    // 런타임 상태
    bool playerInside;
    bool triggered;
    PlayerController cachedPlayer;
    GameObject promptUI;
    Text promptUIText;
    Canvas promptCanvas;

    void Start()
    {
        // 트리거 콜라이더 확인/생성
        var col = GetComponent<BoxCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = triggerCenter;
            col.size = triggerSize;
        }
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        BuildPromptUI();
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot) return;
        if (!other.CompareTag("Player")) return;

        cachedPlayer = other.GetComponent<PlayerController>();
        if (cachedPlayer == null)
            cachedPlayer = other.GetComponentInParent<PlayerController>();
        if (cachedPlayer == null) return;

        if (!CheckConditions()) return;

        playerInside = true;

        if (autoEnter)
        {
            Execute();
        }
        else
        {
            ShowPrompt(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        ShowPrompt(false);
    }

    void Update()
    {
        if (!playerInside || autoEnter) return;
        if (triggered && oneShot) return;

        // UI가 열려있으면 입력 무시
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Execute();
        }
    }

    bool CheckConditions()
    {
        // 아이템 조건
        if (!string.IsNullOrEmpty(requiredKeyId))
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var inv = player.GetComponent<PlayerInventory>();
                if (inv == null || inv.Grid == null || inv.Grid.FindItem(requiredKeyId) == null)
                {
                    return false;
                }
            }
        }

        // 퀘스트 조건
        if (!string.IsNullOrEmpty(requiredQuestId))
        {
            if (QuestManager.Instance == null ||
                !QuestManager.Instance.CompletedQuestIds.Contains(requiredQuestId))
            {
                return false;
            }
        }

        return true;
    }

    void Execute()
    {
        triggered = true;
        ShowPrompt(false);

        switch (mode)
        {
            case TriggerMode.SceneTransition:
                ExecuteSceneTransition();
                break;
            case TriggerMode.LocalTeleport:
                ExecuteLocalTeleport();
                break;
            case TriggerMode.StoryTrigger:
                ExecuteStoryTrigger();
                break;
            case TriggerMode.CustomEvent:
                ExecuteCustomEvent();
                break;
        }
    }

    void ExecuteSceneTransition()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning($"[BuildingEntryTrigger] targetScene이 비어있음: {gameObject.name}");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[BuildingEntryTrigger] SceneTransitionManager가 없습니다.");
            return;
        }

        // RaidManager 추출 성공 처리 (필드→안전가옥 복귀 시)
        if (RaidManager.Instance != null)
            RaidManager.Instance.OnExtractSuccess();

        if (transitionDelay > 0f)
        {
            SceneTransitionManager.Instance.TransitionWithDelay(
                targetScene, targetSpawnId, transitionDelay, transform);
        }
        else
        {
            SceneTransitionManager.Instance.TransitionTo(targetScene, targetSpawnId);
        }
    }

    void ExecuteLocalTeleport()
    {
        if (cachedPlayer == null) return;

        var agent = cachedPlayer.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(teleportPosition);
        }
        else
        {
            cachedPlayer.transform.position = teleportPosition;
        }

        if (teleportYRotation != 0f)
            cachedPlayer.transform.rotation = Quaternion.Euler(0, teleportYRotation, 0);

        Debug.Log($"[BuildingEntryTrigger] 로컬 텔레포트: {teleportPosition}");
    }

    void ExecuteStoryTrigger()
    {
        if (string.IsNullOrEmpty(storySceneId))
        {
            Debug.LogWarning("[BuildingEntryTrigger] storySceneId가 비어있음");
            return;
        }

        if (StoryPlayer.Instance != null)
        {
            StoryPlayer.Instance.PlayScene(storySceneId);
        }
        else
        {
            Debug.LogWarning("[BuildingEntryTrigger] StoryPlayer가 없습니다.");
        }
    }

    void ExecuteCustomEvent()
    {
        // customData를 스토리 씬 ID로 해석 시도
        if (StoryPlayer.Instance != null && !string.IsNullOrEmpty(customData))
        {
            StoryPlayer.Instance.PlayScene(customData);
        }

        Debug.Log($"[BuildingEntryTrigger] 커스텀 이벤트 발동: {customData}");
    }

    // ═══════════════════════════════
    //  프롬프트 UI
    // ═══════════════════════════════

    void BuildPromptUI()
    {
        var go = new GameObject("EntryPrompt");
        go.transform.SetParent(transform);
        go.transform.localPosition = triggerCenter + Vector3.up * (triggerSize.y * 0.5f + 0.3f);

        // World Space Canvas
        promptCanvas = go.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.sortingOrder = 50;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);
        rt.localScale = Vector3.one * 0.01f;

        // 배경
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

        // 텍스트
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(8, 4);
        txtRt.offsetMax = new Vector2(-8, -4);
        promptUIText = txtGo.AddComponent<Text>();
        promptUIText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptUIText.fontSize = 24;
        promptUIText.color = new Color(1f, 0.9f, 0.6f);
        promptUIText.alignment = TextAnchor.MiddleCenter;
        promptUIText.text = promptText;

        promptUI = go;
        go.SetActive(false);
    }

    void ShowPrompt(bool show)
    {
        if (promptUI != null)
            promptUI.SetActive(show);
    }

    void LateUpdate()
    {
        // 프롬프트가 항상 카메라를 향하도록 (빌보드)
        if (promptUI != null && promptUI.activeSelf && Camera.main != null)
        {
            promptUI.transform.forward = Camera.main.transform.forward;
        }
    }

    // ═══════════════════════════════
    //  설정 API (MapObjectSpawner용)
    // ═══════════════════════════════

    /// <summary>외부에서 트리거 설정</summary>
    public void Configure(TriggerMode triggerMode, string prompt, bool auto,
        float delay, Vector3 size, Vector3 center)
    {
        mode = triggerMode;
        if (!string.IsNullOrEmpty(prompt)) promptText = prompt;
        autoEnter = auto;
        transitionDelay = delay;
        triggerSize = size;
        triggerCenter = center;
    }

    public void SetSceneTarget(string scene, string spawnId)
    {
        targetScene = scene;
        targetSpawnId = spawnId;
    }

    public void SetTeleportTarget(Vector3 pos, float yRot)
    {
        teleportPosition = pos;
        teleportYRotation = yRot;
    }

    public void SetStoryTarget(string sceneId)
    {
        storySceneId = sceneId;
    }

    public void SetCustomData(string data)
    {
        customData = data;
    }

    public void SetConditions(bool once, string keyId, string questId)
    {
        oneShot = once;
        requiredKeyId = keyId;
        requiredQuestId = questId;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(triggerCenter, triggerSize);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(triggerCenter, triggerSize);

        // 방향 화살표
        Gizmos.color = Color.yellow;
        Vector3 arrowStart = triggerCenter + Vector3.up * (triggerSize.y * 0.5f + 0.2f);
        Gizmos.DrawLine(arrowStart, arrowStart + Vector3.up * 0.5f);
    }
#endif
}
