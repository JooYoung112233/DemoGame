using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestHUD : MonoBehaviour
{
    bool townMapVisible;
    Vector2 originalPanelOffsetMax;
    Vector2 originalPanelOffsetMin;
    public void SetTownMapVisible(bool visible)
    {
        if (panelRoot == null || visible == townMapVisible) return;
        var rt = panelRoot.GetComponent<RectTransform>();
        if (visible)
        {
            originalPanelOffsetMax = rt.offsetMax;
            originalPanelOffsetMin = rt.offsetMin;
            float shift = Mathf.Min(0f, -354f - rt.offsetMax.y);
            rt.offsetMin += new Vector2(0, shift);
            rt.offsetMax = new Vector2(rt.offsetMax.x, Mathf.Min(rt.offsetMax.y, -354f));
        }
        else
        {
            rt.offsetMin = originalPanelOffsetMin;
            rt.offsetMax = originalPanelOffsetMax;
        }
        townMapVisible = visible;
    }
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Text questListText;
    [SerializeField] Text notificationText;
    float notificationTimer;

    public bool IsGenerated => canvas != null;

    void Start()
    {
        if (!IsGenerated) GenerateUI();
        BindEvents();
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
            QuestManager.Instance.OnQuestAccepted -= OnQuestAccepted;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    void Update()
    {
        UpdateQuestList();

        if (notificationTimer > 0)
        {
            notificationTimer -= Time.unscaledDeltaTime;
            if (notificationTimer <= 0)
                notificationText.gameObject.SetActive(false);
            else
            {
                float alpha = Mathf.Clamp01(notificationTimer);
                var c = notificationText.color;
                c.a = alpha;
                notificationText.color = c;
            }
        }
    }

    void UpdateQuestList()
    {
        if (QuestManager.Instance == null || questListText == null) return;

        var quests = QuestManager.Instance.ActiveQuests;
        if (quests.Count == 0)
        {
            panelRoot.SetActive(false);
            return;
        }

        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);
        string text = "";
        int shown = 0;

        foreach (var q in quests)
        {
            if (shown >= 3) break;
            if (q.state == QuestState.Failed) continue;

            text += $"<color=#FFD700>■</color> {q.data.title}";
            if (q.state == QuestState.ReadyToReport)
            {
                text += " <color=#00FF88>[완료]</color>";
            }
            text += "\n";

            for (int i = 0; i < q.data.objectives.Length; i++)
            {
                var obj = q.data.objectives[i];
                int cur = q.progress.ContainsKey(i) ? q.progress[i] : 0;
                bool done = cur >= obj.requiredCount;
                string color = done ? "#88FF88" : "#CCCCCC";
                text += $"  <color={color}>{obj.description} ({cur}/{obj.requiredCount})</color>\n";
            }
            shown++;
        }

        questListText.text = text.TrimEnd('\n');
    }

    void OnObjectiveUpdated(QuestInstance quest, int objIndex)
    {
        var obj = quest.data.objectives[objIndex];
        if (quest.IsObjectiveComplete(objIndex))
            ShowNotification($"목표 달성: {obj.description}");
        else
            ShowNotification($"{obj.description} ({quest.progress[objIndex]}/{obj.requiredCount})");
    }

    void OnQuestAccepted(QuestInstance quest)
    {
        ShowNotification($"퀘스트 수주: {quest.data.title}");
    }

    void OnQuestCompleted(QuestInstance quest)
    {
        ShowNotification($"퀘스트 완료: {quest.data.title}");
    }

    void ShowNotification(string msg)
    {
        notificationText.text = msg;
        notificationText.color = new Color(1f, 0.9f, 0.3f, 1f);
        notificationText.gameObject.SetActive(true);
        notificationTimer = 3f;
    }

    public void BindEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
            QuestManager.Instance.OnQuestAccepted += OnQuestAccepted;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
        }

        // 패널 클릭 → 퀘스트 로그 열기 onClick 재부착 — 빌더에서만 붙이면 프리팹 경로에서
        // 클릭 무반응(§6.5, 전체 검수 2026-07-07). panelRoot는 직렬화 ref라 양 경로 커버.
        var openBtn = panelRoot != null ? panelRoot.GetComponent<UnityEngine.UI.Button>() : null;
        if (openBtn != null)
        {
            openBtn.onClick.RemoveAllListeners();
            openBtn.onClick.AddListener(QuestLogUI.Show);
        }
    }

    public void GenerateUI()
    {
        var canvasGO = new GameObject("QuestHUD_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 우측 퀘스트 패널
        panelRoot = new GameObject("QuestPanel");
        panelRoot.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 0.5f);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 1);
        panelRT.offsetMin = new Vector2(-320, 0);
        panelRT.offsetMax = new Vector2(-10, -80);

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.4f);

        // 패널 클릭 → 전체화면 의뢰/통신 로그(QuestLogUI) 열기 (J 키와 동일 진입점)
        var openBtn = panelRoot.AddComponent<Button>();
        openBtn.targetGraphic = bg;
        openBtn.onClick.AddListener(QuestLogUI.Show);

        questListText = MakeText(panelRoot.transform, "QuestList", "", 14, TextAnchor.UpperLeft);
        var listRT = questListText.GetComponent<RectTransform>();
        listRT.anchorMin = Vector2.zero;
        listRT.anchorMax = Vector2.one;
        listRT.offsetMin = new Vector2(8, 4);
        listRT.offsetMax = new Vector2(-8, -4);
        questListText.color = UITheme.TextBright;
        questListText.raycastTarget = false;   // 클릭이 패널 버튼으로 통과되게

        // 알림 텍스트 (상단 중앙)
        notificationText = MakeText(canvasGO.transform, "Notification", "", 20, TextAnchor.MiddleCenter);
        var notifRT = notificationText.GetComponent<RectTransform>();
        notifRT.anchorMin = new Vector2(0.5f, 1);
        notifRT.anchorMax = new Vector2(0.5f, 1);
        notifRT.pivot = new Vector2(0.5f, 1);
        notifRT.anchoredPosition = new Vector2(0, -50);
        notifRT.sizeDelta = new Vector2(500, 35);
        notificationText.fontStyle = FontStyle.Bold;
        notificationText.gameObject.SetActive(false);

        panelRoot.SetActive(false);
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        GenerateUI();
    }
#endif

    public void ClearGeneratedUI()
    {
        var child = transform.Find("QuestHUD_Canvas");
        if (child != null)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        canvas = null;
        panelRoot = null;
        questListText = null;
        notificationText = null;
    }

    Text MakeText(Transform parent, string name, string content, int fontSize, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.text = content;
        txt.alignment = align;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.supportRichText = true;
        return txt;
    }
}
