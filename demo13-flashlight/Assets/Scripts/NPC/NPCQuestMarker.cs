using UnityEngine;

/// <summary>
/// NPC 머리 위 퀘스트/대화 마커.
/// QuestManager 이벤트를 구독하여 자동 갱신.
///
/// 상태 우선순위:
/// 1. ReadyToReport (❗ 느낌표) — 완료한 퀘스트 보고 가능
/// 2. Available (❓ 물음표) — 수주 가능한 퀘스트 있음
/// 3. InProgress (…) — 진행 중인 퀘스트 있음 (반투명)
/// 4. Story (💬) — 스토리 씬 대기 중
/// 5. Talk (일반 대화 가능) — 작은 말풍선
/// 6. None — 숨김
/// </summary>
public class NPCQuestMarker : MonoBehaviour
{
    public enum MarkerState
    {
        None,
        Talk,           // 일반 대화 가능
        Story,          // 스토리 씬 대기
        InProgress,     // 퀘스트 진행 중
        Available,      // 퀘스트 수주 가능
        ReadyToReport,  // 퀘스트 보고 가능
    }

    [Header("설정")]
    [Tooltip("NPC 머리 위 높이 오프셋")]
    [SerializeField] float heightOffset = 2.2f;

    [Tooltip("마커 크기")]
    [SerializeField] float markerScale = 0.4f;

    [Tooltip("마커 흔들림(위아래) 속도")]
    [SerializeField] float bobSpeed = 2f;

    [Tooltip("마커 흔들림 진폭")]
    [SerializeField] float bobAmplitude = 0.12f;

    [Header("색상")]
    [SerializeField] Color exclamationColor = new Color(1f, 0.85f, 0.1f); // 금색 ❗
    [SerializeField] Color questionColor = new Color(1f, 0.85f, 0.1f);    // 금색 ❓
    [SerializeField] Color inProgressColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    [SerializeField] Color storyColor = new Color(0.3f, 0.8f, 1f);        // 하늘색 💬
    [SerializeField] Color talkColor = new Color(0.8f, 0.8f, 0.8f, 0.5f); // 회색 말풍선

    // 런타임
    NPCController npcController;
    NPCData npcData;
    MarkerState currentState = MarkerState.None;

    // 3D 마커 오브젝트들
    GameObject markerRoot;
    GameObject exclamationMark;    // ❗
    GameObject questionMark;       // ❓
    GameObject progressMark;       // …
    GameObject storyMark;          // 💬
    GameObject talkMark;           // 작은 말풍선

    // 빌보드
    Transform camTransform;

    void Start()
    {
        npcController = GetComponent<NPCController>();
        if (npcController != null)
            npcData = npcController.Data;

        CreateMarkerVisuals();
        camTransform = Camera.main?.transform;

        // QuestManager 이벤트 구독
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestChanged;
            QuestManager.Instance.OnObjectiveUpdated += OnObjectiveChanged;
        }

        // 초기 상태 갱신
        RefreshState();
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted -= OnQuestChanged;
            QuestManager.Instance.OnObjectiveUpdated -= OnObjectiveChanged;
        }
    }

    void OnQuestChanged(QuestInstance _) => RefreshState();
    void OnObjectiveChanged(QuestInstance _, int __) => RefreshState();

    void Update()
    {
        if (markerRoot == null) return;

        // 위아래 흔들림
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        markerRoot.transform.localPosition = new Vector3(0, heightOffset + bob, 0);

        // 카메라 빌보드 (Y축만)
        if (camTransform != null)
        {
            Vector3 dir = camTransform.position - markerRoot.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                markerRoot.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    /// <summary>현재 NPC의 퀘스트 상태를 평가하고 마커를 갱신</summary>
    public void RefreshState()
    {
        if (npcData == null)
        {
            SetState(MarkerState.None);
            return;
        }

        string npcId = npcData.npcId;

        // 1. 보고 가능한 퀘스트?
        if (QuestManager.Instance != null)
        {
            var reportable = QuestManager.Instance.GetReportableQuest(npcId);
            if (reportable != null)
            {
                SetState(MarkerState.ReadyToReport);
                return;
            }
        }

        // 2. 수주 가능한 퀘스트?
        if (QuestManager.Instance != null && npcData.availableQuests != null && npcData.availableQuests.Length > 0)
        {
            var available = QuestManager.Instance.GetAvailableQuests(npcId, npcData.availableQuests);
            if (available.Count > 0)
            {
                SetState(MarkerState.Available);
                return;
            }
        }

        // 3. 진행 중 퀘스트?
        if (QuestManager.Instance != null)
        {
            foreach (var q in QuestManager.Instance.ActiveQuests)
            {
                if (q.data.giverNpcId == npcId && q.state == QuestState.Active)
                {
                    SetState(MarkerState.InProgress);
                    return;
                }
            }
        }

        // 4. 스토리 씬 대기?
        if (StoryTriggerManager.Instance != null)
        {
            string storyNpcId = npcId;
            // NPCController의 storyNpcId도 체크
            var field = typeof(NPCController).GetField("storyNpcId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && npcController != null)
            {
                string overrideId = (string)field.GetValue(npcController);
                if (!string.IsNullOrEmpty(overrideId))
                    storyNpcId = overrideId;
            }

            if (StoryTriggerManager.Instance.HasPendingNPCScene(storyNpcId))
            {
                SetState(MarkerState.Story);
                return;
            }
        }

        // 5. 이벤트 대화 또는 일반 대화 가능?
        bool hasDialogue = (npcData.defaultDialogues != null && npcData.defaultDialogues.Length > 0)
                        || (npcData.eventDialogues != null && npcData.eventDialogues.Length > 0);
        if (hasDialogue)
        {
            SetState(MarkerState.Talk);
            return;
        }

        SetState(MarkerState.None);
    }

    void SetState(MarkerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        UpdateVisuals();
    }

    /// <summary>외부에서 강제 상태 설정 (스토리 이벤트 등)</summary>
    public void ForceState(MarkerState state)
    {
        currentState = state;
        UpdateVisuals();
    }

    public MarkerState CurrentState => currentState;

    void UpdateVisuals()
    {
        if (markerRoot == null) return;

        exclamationMark.SetActive(currentState == MarkerState.ReadyToReport);
        questionMark.SetActive(currentState == MarkerState.Available);
        progressMark.SetActive(currentState == MarkerState.InProgress);
        storyMark.SetActive(currentState == MarkerState.Story);
        talkMark.SetActive(currentState == MarkerState.Talk);

        markerRoot.SetActive(currentState != MarkerState.None);
    }

    // ── 마커 비주얼 생성 ──

    void CreateMarkerVisuals()
    {
        markerRoot = new GameObject("QuestMarker");
        markerRoot.transform.SetParent(transform);
        markerRoot.transform.localPosition = new Vector3(0, heightOffset, 0);

        // ❗ 느낌표 (ReadyToReport)
        exclamationMark = CreateTextMarker("Exclamation", "!", exclamationColor, 1.2f);

        // ❓ 물음표 (Available)
        questionMark = CreateTextMarker("Question", "?", questionColor, 1.2f);

        // … 진행 중 (InProgress)
        progressMark = CreateTextMarker("Progress", "...", inProgressColor, 0.8f);

        // 💬 스토리 (Story)
        storyMark = CreateIconMarker("Story", storyColor, MarkerShape.Diamond);

        // 말풍선 (Talk)
        talkMark = CreateIconMarker("Talk", talkColor, MarkerShape.Dot);

        // 전부 비활성
        exclamationMark.SetActive(false);
        questionMark.SetActive(false);
        progressMark.SetActive(false);
        storyMark.SetActive(false);
        talkMark.SetActive(false);
        markerRoot.SetActive(false);
    }

    GameObject CreateTextMarker(string name, string text, Color color, float sizeMultiplier)
    {
        var go = new GameObject(name);
        go.transform.SetParent(markerRoot.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * markerScale * sizeMultiplier;

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;

        // 그림자용 뒤쪽 텍스트
        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(go.transform, false);
        shadow.transform.localPosition = new Vector3(0.02f, -0.02f, 0.01f);
        shadow.transform.localScale = Vector3.one;

        var shadowTm = shadow.AddComponent<TextMesh>();
        shadowTm.text = text;
        shadowTm.fontSize = 64;
        shadowTm.characterSize = 0.1f;
        shadowTm.anchor = TextAnchor.MiddleCenter;
        shadowTm.alignment = TextAlignment.Center;
        shadowTm.color = new Color(0, 0, 0, 0.6f);
        shadowTm.fontStyle = FontStyle.Bold;

        return go;
    }

    enum MarkerShape { Dot, Diamond }

    GameObject CreateIconMarker(string name, Color color, MarkerShape shape)
    {
        var go = new GameObject(name);
        go.transform.SetParent(markerRoot.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * markerScale;

        if (shape == MarkerShape.Diamond)
        {
            // 다이아몬드 (45도 회전 큐브)
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(go.transform, false);
            cube.transform.localScale = Vector3.one * 0.35f;
            cube.transform.localRotation = Quaternion.Euler(0, 0, 45);

            var col = cube.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = cube.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
        else
        {
            // 작은 점
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.2f;

            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = sphere.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        return go;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * heightOffset, 0.2f);
    }
}
