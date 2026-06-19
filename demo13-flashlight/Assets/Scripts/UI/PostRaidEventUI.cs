using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PostRaidEventUI : MonoBehaviour
{
    public static PostRaidEventUI Instance { get; private set; }

    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    bool isShowing;
    PostRaidEventData currentEvent;
    System.Action onDismissed;

    // uGUI
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Text titleText;
    [SerializeField] Text descText;
    [SerializeField] GameObject choicePanel;
    [SerializeField] Button[] choiceButtons;
    [SerializeField] Text[] choiceTexts;

    // 결과 표시
    [SerializeField] GameObject resultPanel;
    [SerializeField] Text resultText;
    [SerializeField] Text rewardText;
    [SerializeField] Text continueLabel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!IsGenerated) GenerateUI();
        BindEvents();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BindEvents()
    {
        // Choice button onClick listeners are set dynamically in ShowEvent()
    }

    public void ShowEvent(PostRaidEventData evt, System.Action onDone = null)
    {
        if (evt == null)
        {
            onDone?.Invoke();
            return;
        }

        currentEvent = evt;
        onDismissed = onDone;
        isShowing = true;

        titleText.text = evt.title;
        descText.text = evt.description;

        choicePanel.SetActive(true);
        resultPanel.SetActive(false);
        panelRoot.SetActive(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < evt.choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceTexts[i].text = $"  ▸ {evt.choices[i].text}";
                int idx = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(idx));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnChoiceSelected(int index)
    {
        var choice = currentEvent.choices[index];

        ApplyRewards(choice);
        ApplyPenalties(choice);

        choicePanel.SetActive(false);
        resultPanel.SetActive(true);

        resultText.text = choice.resultText;

        string rewardStr = "";
        if (choice.rewards != null)
        {
            foreach (var r in choice.rewards)
            {
                switch (r.type)
                {
                    case EventRewardType.Item:
                        var item = ItemDatabase.Get(r.itemId);
                        rewardStr += $"\n  ✦ 획득: {(item != null ? item.displayName : r.itemId)} x{r.amount}";
                        break;
                    case EventRewardType.Currency:
                        rewardStr += $"\n  ✦ {r.amount} 스크랩";
                        break;
                    case EventRewardType.Heal:
                        rewardStr += $"\n  ✦ HP +{r.amount}";
                        break;
                    case EventRewardType.Affinity:
                        rewardStr += $"\n  ✦ 호감도 +{r.amount}";
                        break;
                    case EventRewardType.Trust:
                        rewardStr += $"\n  ✦ 신뢰도 +{r.amount}";
                        break;
                }
            }
        }
        if (choice.penalties != null)
        {
            foreach (var p in choice.penalties)
            {
                switch (p.type)
                {
                    case EventPenaltyType.Damage:
                        rewardStr += $"\n  ▾ HP -{p.amount}";
                        break;
                    case EventPenaltyType.LoseCurrency:
                        rewardStr += $"\n  ▾ -{p.amount} 스크랩";
                        break;
                    case EventPenaltyType.LoseItem:
                        rewardStr += $"\n  ▾ 아이템 일부 손실";
                        break;
                }
            }
        }
        rewardText.text = rewardStr;
    }

    void ApplyRewards(EventChoice choice)
    {
        if (choice.rewards == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");

        foreach (var r in choice.rewards)
        {
            switch (r.type)
            {
                case EventRewardType.Item:
                    if (player != null)
                    {
                        var data = ItemDatabase.Get(r.itemId);
                        if (data != null)
                        {
                            var inv = player.GetComponent<PlayerInventory>();
                            if (inv != null)
                                inv.TryPickup(new ItemInstance(data, r.amount));
                        }
                    }
                    break;
                case EventRewardType.Heal:
                    if (player != null)
                    {
                        var health = player.GetComponent<Health>();
                        if (health != null) health.Heal(r.amount);
                    }
                    break;
                case EventRewardType.Affinity:
                    if (NPCRelationshipManager.Instance != null)
                        NPCRelationshipManager.Instance.ModifyAffinity(r.npcId, r.amount);
                    break;
                case EventRewardType.Trust:
                    if (NPCRelationshipManager.Instance != null)
                        NPCRelationshipManager.Instance.ModifyTrust(r.npcId, r.amount);
                    break;
                case EventRewardType.Currency:
                    if (CurrencyManager.Instance != null)
                        CurrencyManager.Instance.Add(r.amount, "레이드 후 이벤트");
                    break;
            }
        }
    }

    void ApplyPenalties(EventChoice choice)
    {
        if (choice.penalties == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");

        foreach (var p in choice.penalties)
        {
            switch (p.type)
            {
                case EventPenaltyType.Damage:
                    if (player != null)
                    {
                        var health = player.GetComponent<Health>();
                        if (health != null) health.TakeDamage(p.amount);
                    }
                    break;
                case EventPenaltyType.LoseCurrency:
                    if (CurrencyManager.Instance != null)
                        CurrencyManager.Instance.Lose(p.amount, "레이드 후 이벤트");
                    break;
                case EventPenaltyType.LoseItem:
                    if (player != null)
                    {
                        var inv = player.GetComponent<PlayerInventory>();
                        if (inv != null)
                        {
                            var items = inv.Grid.GetAll();
                            if (items.Count > 0)
                            {
                                int idx = Random.Range(0, items.Count);
                                inv.Grid.Remove(items[idx]);
                            }
                        }
                    }
                    break;
            }
        }
    }

    void OnContinue()
    {
        isShowing = false;
        panelRoot.SetActive(false);
        onDismissed?.Invoke();
    }

    void Update()
    {
        if (!isShowing) return;

        // 결과 화면에서 Enter/클릭으로 닫기
        if (resultPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
                OnContinue();
        }

        // 선택지에서 숫자키
        if (choicePanel.activeSelf && currentEvent != null)
        {
            for (int i = 0; i < currentEvent.choices.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    OnChoiceSelected(i);
                    break;
                }
            }
        }
    }

    public void GenerateUI()
    {
        var canvasGO = new GameObject("PostRaidEvent_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 105;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasRT = canvasGO.GetComponent<RectTransform>();

        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasRT, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // 어두운 배경
        var dim = panelRoot.AddComponent<Image>();
        dim.color = UITheme.Backdrop;

        // 중앙 패널
        var panel = new GameObject("CenterPanel");
        panel.transform.SetParent(panelRoot.transform, false);
        var pRT = panel.AddComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.5f, 0.5f);
        pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(520, 420);
        var pImg = panel.AddComponent<Image>();
        pImg.color = UITheme.Panel;

        // 제목
        titleText = MakeText(panel.transform, "Title", "", 22, TextAnchor.MiddleCenter);
        var titleRT = titleText.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.offsetMin = new Vector2(16, -50);
        titleRT.offsetMax = new Vector2(-16, -12);
        titleText.color = UITheme.Gold;
        titleText.fontStyle = FontStyle.Bold;

        // 구분선
        MakeLine(panel.transform, new Vector2(0, -55), new Vector2(460, 1));

        // 설명
        descText = MakeText(panel.transform, "Desc", "", 17, TextAnchor.UpperLeft);
        var descRT = descText.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 1);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.pivot = new Vector2(0.5f, 1);
        descRT.offsetMin = new Vector2(24, -180);
        descRT.offsetMax = new Vector2(-24, -65);
        descText.color = UITheme.TextBright;

        // 선택지 패널
        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(panel.transform, false);
        var cpRT = choicePanel.AddComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0, 0);
        cpRT.anchorMax = new Vector2(1, 0.45f);
        cpRT.offsetMin = new Vector2(16, 16);
        cpRT.offsetMax = new Vector2(-16, 0);

        choiceButtons = new Button[3];
        choiceTexts = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var btnGO = new GameObject($"Choice{i}");
            btnGO.transform.SetParent(choicePanel.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 1);
            btnRT.anchorMax = new Vector2(1, 1);
            btnRT.pivot = new Vector2(0.5f, 1);
            btnRT.offsetMin = new Vector2(4, -(i + 1) * 52);
            btnRT.offsetMax = new Vector2(-4, -i * 52 - 4);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = UITheme.Cell;

            choiceButtons[i] = btnGO.AddComponent<Button>();
            var colors = choiceButtons[i].colors;
            colors.highlightedColor = UITheme.CellHover;
            colors.pressedColor = UITheme.CellPressed;
            choiceButtons[i].colors = colors;

            choiceTexts[i] = MakeText(btnGO.transform, "Text", "", 16, TextAnchor.MiddleLeft);
            var txtRT = choiceTexts[i].GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(12, 0);
            txtRT.offsetMax = new Vector2(-12, 0);
            choiceTexts[i].color = UITheme.TextBright;
        }

        // 결과 패널
        resultPanel = new GameObject("ResultPanel");
        resultPanel.transform.SetParent(panel.transform, false);
        var rpRT = resultPanel.AddComponent<RectTransform>();
        rpRT.anchorMin = new Vector2(0, 0);
        rpRT.anchorMax = new Vector2(1, 0.55f);
        rpRT.offsetMin = new Vector2(16, 16);
        rpRT.offsetMax = new Vector2(-16, 0);

        resultText = MakeText(resultPanel.transform, "ResultText", "", 17, TextAnchor.UpperLeft);
        var rRT = resultText.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0, 0.4f);
        rRT.anchorMax = new Vector2(1, 1);
        rRT.offsetMin = new Vector2(8, 0);
        rRT.offsetMax = new Vector2(-8, -4);
        resultText.color = UITheme.TextBright;

        rewardText = MakeText(resultPanel.transform, "RewardText", "", 15, TextAnchor.UpperLeft);
        var rwRT = rewardText.GetComponent<RectTransform>();
        rwRT.anchorMin = new Vector2(0, 0.05f);
        rwRT.anchorMax = new Vector2(1, 0.4f);
        rwRT.offsetMin = new Vector2(8, 0);
        rwRT.offsetMax = new Vector2(-8, 0);
        rewardText.color = UITheme.Positive;

        // 계속 버튼 (텍스트)
        continueLabel = MakeText(resultPanel.transform, "Continue", "[ 계속 ]", 14, TextAnchor.MiddleCenter);
        var clRT = continueLabel.GetComponent<RectTransform>();
        clRT.anchorMin = new Vector2(0.5f, 0);
        clRT.anchorMax = new Vector2(0.5f, 0);
        clRT.pivot = new Vector2(0.5f, 0);
        clRT.anchoredPosition = new Vector2(0, 4);
        clRT.sizeDelta = new Vector2(200, 28);
        continueLabel.color = UITheme.TextMuted;

        resultPanel.SetActive(false);
        panelRoot.SetActive(false);
    }

    public void ClearGeneratedUI()
    {
        var child = transform.Find("PostRaidEvent_Canvas");
        if (child != null)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        canvas = null;
        panelRoot = null;
        titleText = null;
        descText = null;
        choicePanel = null;
        choiceButtons = null;
        choiceTexts = null;
        resultPanel = null;
        resultText = null;
        rewardText = null;
        continueLabel = null;
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

    void MakeLine(Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = UITheme.Divider;
    }
}
