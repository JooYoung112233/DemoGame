using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    bool isShowing;
    NPCData currentNPC;
    GameObject currentPlayerGO;

    // 대화 상태
    string[] currentLines;
    int lineIndex;
    bool waitingForChoice;
    DialogueChoice[] currentChoices;
    int choiceHighlight;
    System.Action<int> choiceCallback;

    // 타이핑 연출
    Coroutine typingCoroutine;
    bool isTyping;
    bool _skipTyping;
    string fullLineText;

    // uGUI
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Text nameText;
    [SerializeField] Text dialogueText;
    [SerializeField] Text relationLabel;
    [SerializeField] GameObject choicePanel;
    [SerializeField] Button[] choiceButtons;
    [SerializeField] Text[] choiceTexts;
    [SerializeField] Text continueHint;

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
        // choice button onClick are wired dynamically in ShowChoices/ShowStoryChoices
    }

    public void StartDialogue(NPCData npc, GameObject playerGO)
    {
        currentNPC = npc;
        currentPlayerGO = playerGO;

        var rel = NPCRelationshipManager.Instance != null
            ? NPCRelationshipManager.Instance.GetRelationship(npc.npcId)
            : null;

        // 1. 보고 가능한 퀘스트 체크
        if (QuestManager.Instance != null)
        {
            var reportable = QuestManager.Instance.GetReportableQuest(npc.npcId);
            if (reportable != null)
            {
                ShowQuestReport(reportable);
                return;
            }
        }

        // 2. 이벤트 대화 체크
        var eventDlg = FindEventDialogue(npc, rel);
        if (eventDlg != null)
        {
            ShowEventDialogue(eventDlg, rel);
            return;
        }

        // 3. 신규 퀘스트 체크
        if (QuestManager.Instance != null && npc.availableQuests != null)
        {
            var available = QuestManager.Instance.GetAvailableQuests(npc.npcId, npc.availableQuests);
            if (available.Count > 0)
            {
                ShowQuestOffer(available[0]);
                return;
            }
        }

        // 4. 일반 대화
        var defaultDlg = FindDefaultDialogue(npc, rel);
        if (defaultDlg != null)
        {
            ShowLines(npc.displayName, defaultDlg.lines, rel);
        }
        else
        {
            ShowLines(npc.displayName, new[] { "..." }, rel);
        }
    }

    EventDialogue FindEventDialogue(NPCData npc, NPCRelationship rel)
    {
        if (npc.eventDialogues == null) return null;

        foreach (var evt in npc.eventDialogues)
        {
            if (evt.oneShot && rel != null && rel.completedEvents.Contains(evt.id))
                continue;
            if (!CheckCondition(evt.triggerCondition, rel))
                continue;
            return evt;
        }
        return null;
    }

    DialogueEntry FindDefaultDialogue(NPCData npc, NPCRelationship rel)
    {
        if (npc.defaultDialogues == null || npc.defaultDialogues.Length == 0)
            return null;

        DialogueEntry best = null;
        int bestPriority = -1;
        var candidates = new List<DialogueEntry>();

        foreach (var d in npc.defaultDialogues)
        {
            if (!CheckCondition(d.conditions, rel)) continue;
            if (d.priority > bestPriority)
            {
                bestPriority = d.priority;
                candidates.Clear();
                candidates.Add(d);
            }
            else if (d.priority == bestPriority)
            {
                candidates.Add(d);
            }
        }

        if (candidates.Count > 0)
            best = candidates[Random.Range(0, candidates.Count)];

        return best;
    }

    bool CheckCondition(DialogueCondition cond, NPCRelationship rel)
    {
        if (cond == null) return true;
        if (rel == null) return cond.minAffinity <= 0 && cond.minTrust <= 0 && cond.minFear <= 0;

        if (rel.affinity < cond.minAffinity) return false;
        if (rel.trust < cond.minTrust) return false;
        if (rel.fear < cond.minFear) return false;

        if (!string.IsNullOrEmpty(cond.requiredQuest))
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.CompletedQuestIds.Contains(cond.requiredQuest))
                return false;
        }

        if (!string.IsNullOrEmpty(cond.requiredFlag))
        {
            if (rel.flags == null || !rel.flags.ContainsKey(cond.requiredFlag) || !rel.flags[cond.requiredFlag])
                return false;
        }

        return true;
    }

    void ShowEventDialogue(EventDialogue evt, NPCRelationship rel)
    {
        ShowLines(currentNPC.displayName, evt.npcLines, rel, () =>
        {
            if (evt.choices != null && evt.choices.Length > 0)
            {
                ShowChoices(evt.choices, (choiceIdx) =>
                {
                    var choice = evt.choices[choiceIdx];
                    ApplyChoiceEffects(choice, rel);

                    if (evt.oneShot && rel != null)
                        rel.completedEvents.Add(evt.id);

                    if (TryOpenShop(choice)) return; // 상점 진입 시 대화 종료
                    ShowLines(currentNPC.displayName, choice.resultLines, rel);
                });
            }
        });
    }

    /// <summary>선택지가 상점 진입이면 대화를 닫고 상점을 연다. (열었으면 true)</summary>
    bool TryOpenShop(DialogueChoice choice)
    {
        if (choice == null || !choice.openShop) return false;
        if (currentNPC == null || currentNPC.shopData == null) return false;
        var shop = currentNPC.shopData;
        Hide();
        if (UIManager.Instance != null) UIManager.Instance.ShowShop(shop);
        return true;
    }

    void ApplyChoiceEffects(DialogueChoice choice, NPCRelationship rel)
    {
        // 전역 평판 (NPCRelationshipManager 유무와 무관하게 적용)
        if (choice.reputationChange != 0 && ReputationManager.Instance != null)
            ReputationManager.Instance.Add(choice.reputationChange,
                currentNPC != null ? $"{currentNPC.displayName} 대화" : "대화");

        if (NPCRelationshipManager.Instance == null || currentNPC == null) return;

        if (choice.affinityChange != 0)
            NPCRelationshipManager.Instance.ModifyAffinity(currentNPC.npcId, choice.affinityChange);
        if (choice.trustChange != 0)
            NPCRelationshipManager.Instance.ModifyTrust(currentNPC.npcId, choice.trustChange);
        if (choice.fearChange != 0)
            NPCRelationshipManager.Instance.ModifyFear(currentNPC.npcId, choice.fearChange);

        if (!string.IsNullOrEmpty(choice.setFlag) && rel != null)
            rel.flags[choice.setFlag] = true;

        if (!string.IsNullOrEmpty(choice.triggerQuest) && QuestManager.Instance != null)
        {
            if (currentNPC.availableQuests != null)
            {
                foreach (var q in currentNPC.availableQuests)
                {
                    if (q.questId == choice.triggerQuest)
                    {
                        QuestManager.Instance.AcceptQuest(q);
                        break;
                    }
                }
            }
        }
    }

    void ShowQuestOffer(QuestData quest)
    {
        var rel = NPCRelationshipManager.Instance?.GetRelationship(currentNPC.npcId);
        string[] offerLines = new[]
        {
            $"의뢰가 있어.",
            $"<color=#FFD700>{quest.title}</color>",
            quest.description
        };

        ShowLines(currentNPC.displayName, offerLines, rel, () =>
        {
            var choices = new DialogueChoice[]
            {
                new DialogueChoice { text = "수락한다", resultLines = new[] { "좋아, 기다리고 있지." } },
                new DialogueChoice { text = "거절한다", resultLines = new[] { "...그래, 어쩔 수 없지." } },
            };

            ShowChoices(choices, (idx) =>
            {
                if (idx == 0 && QuestManager.Instance != null)
                    QuestManager.Instance.AcceptQuest(quest);

                ShowLines(currentNPC.displayName, choices[idx].resultLines, rel);
            });
        });
    }

    void ShowQuestReport(QuestInstance quest)
    {
        var rel = NPCRelationshipManager.Instance?.GetRelationship(currentNPC.npcId);
        string[] reportLines = new[]
        {
            $"<color=#FFD700>{quest.data.title}</color> — 완료했구나.",
            "수고했어. 여기 보상이야."
        };

        ShowLines(currentNPC.displayName, reportLines, rel, () =>
        {
            QuestManager.Instance.CompleteQuest(quest.data.questId);

            string rewardText = "보상 수령 완료.";
            foreach (var r in quest.data.rewards)
            {
                switch (r.type)
                {
                    case QuestRewardType.Item:
                        var item = ItemDatabase.Get(r.itemId);
                        rewardText += $"\n  ✦ {(item != null ? item.displayName : r.itemId)} x{r.amount}";
                        break;
                    case QuestRewardType.Currency:
                        rewardText += $"\n  ✦ {r.amount} 스크랩";
                        break;
                    case QuestRewardType.Affinity:
                        rewardText += $"\n  ✦ 호감도 +{r.amount}";
                        break;
                    case QuestRewardType.Trust:
                        rewardText += $"\n  ✦ 신뢰도 +{r.amount}";
                        break;
                    case QuestRewardType.Recipe:
                        rewardText += $"\n  ✦ 레시피 해금: {r.itemId}";
                        break;
                }
            }
            ShowLines(currentNPC.displayName, new[] { rewardText }, rel);
        });
    }

    // ════════════════════════════════════════
    //  Lines & Choices
    // ════════════════════════════════════════

    void ShowLines(string npcName, string[] lines, NPCRelationship rel, System.Action onComplete = null)
    {
        isShowing = true;
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);
        choicePanel.SetActive(false);

        nameText.text = npcName;

        if (rel != null && NPCRelationshipManager.Instance != null)
        {
            string label = NPCRelationshipManager.Instance.GetAffinityLabel(rel.affinity);
            relationLabel.text = $"[{label}]";
            relationLabel.gameObject.SetActive(true);
        }
        else
        {
            relationLabel.gameObject.SetActive(false);
        }

        currentLines = lines;
        lineIndex = 0;
        waitingForChoice = false;
        choiceCallback = null;

        ShowCurrentLine(onComplete);
    }

    void ShowCurrentLine(System.Action onComplete)
    {
        if (lineIndex >= currentLines.Length)
        {
            if (onComplete != null)
                onComplete();
            else
                Hide();
            return;
        }

        fullLineText = currentLines[lineIndex];
        continueHint.gameObject.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullLineText, onComplete));
    }

    IEnumerator TypeText(string text, System.Action onComplete)
    {
        isTyping = true;
        _skipTyping = false;
        dialogueText.supportRichText = true;
        dialogueText.text = "";

        float typeSpeed = GameTuning.Instance != null ? GameTuning.Instance.dialogueTypingSpeed : 0.03f;
        bool inTag = false;
        string visibleText = "";

        for (int i = 0; i < text.Length; i++)
        {
            if (_skipTyping) { dialogueText.text = text; break; }   // 스킵 → 전체 즉시 표시(코루틴은 계속 → 입력 대기로)

            char c = text[i];

            if (c == '<') inTag = true;
            if (inTag)
            {
                visibleText += c;
                if (c == '>') inTag = false;
            }
            else
            {
                visibleText += c;
            }

            dialogueText.text = visibleText;

            if (!inTag)
                yield return new WaitForSecondsRealtime(typeSpeed);
        }

        isTyping = false;
        continueHint.gameObject.SetActive(true);

        // 클릭/키 대기
        yield return WaitForInput();
        lineIndex++;
        ShowCurrentLine(onComplete);
    }

    IEnumerator WaitForInput()
    {
        yield return null;
        while (true)
        {
            if (GameInput.GetKeyDown(KeyCode.E))   // 대화 진행 = E 단일키
                yield break;
            yield return null;
        }
    }

    void ShowChoices(DialogueChoice[] choices, System.Action<int> callback)
    {
        waitingForChoice = true;
        currentChoices = choices;
        choiceCallback = callback;
        choicePanel.SetActive(true);
        continueHint.gameObject.SetActive(false);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceTexts[i].text = $"  {i + 1}. {choices[i].text}";
                int idx = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(idx));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        choiceHighlight = 0;
        UpdateChoiceHighlight();
    }

    void OnChoiceSelected(int index)
    {
        waitingForChoice = false;
        choicePanel.SetActive(false);
        choiceCallback?.Invoke(index);
    }

    /// <summary>현재 하이라이트된 선택지를 강조(색+▶). 키보드 W/S 이동, E 확정용.</summary>
    void UpdateChoiceHighlight()
    {
        if (currentChoices == null || choiceButtons == null) return;
        int n = Mathf.Min(currentChoices.Length, choiceButtons.Length);
        if (n <= 0) return;
        choiceHighlight = Mathf.Clamp(choiceHighlight, 0, n - 1);

        for (int i = 0; i < n; i++)
        {
            bool sel = (i == choiceHighlight);
            var img = choiceButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = sel ? UITheme.Accent
                                : UITheme.PanelAlt;
            if (choiceTexts[i] != null)
                choiceTexts[i].text = $"  {(sel ? "▶" : "  ")} {i + 1}. {currentChoices[i].text}";
        }
    }

    void Update()
    {
        if (!isShowing) return;

        // 타이핑 중 E → 즉시 완성
        if (isTyping && GameInput.GetKeyDown(KeyCode.E))
        {
            _skipTyping = true;   // 코루틴 유지(전체 표시 후 입력 대기로 이어짐) — StopCoroutine 시 E 먹통 버그 수정
        }

        // 선택지: W/S 이동 · E 확정 (대화 진행과 동일한 E)
        if (waitingForChoice && currentChoices != null)
        {
            int n = Mathf.Min(currentChoices.Length, choiceButtons.Length);
            if (n > 0)
            {
                if (GameInput.GetKeyDown(KeyCode.W))
                { choiceHighlight = (choiceHighlight - 1 + n) % n; UpdateChoiceHighlight(); }
                else if (GameInput.GetKeyDown(KeyCode.S))
                { choiceHighlight = (choiceHighlight + 1) % n; UpdateChoiceHighlight(); }
                else if (GameInput.GetKeyDown(KeyCode.E))
                { OnChoiceSelected(choiceHighlight); }
            }
        }
        // ESC(닫기)는 UIManager가 전역 처리(CloseAll) — 여기선 안 잡음
    }

    public void Hide()
    {
        isShowing = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        panelRoot.SetActive(false);
    }

    // ════════════════════════════════════════
    //  스토리 모드 (StoryPlayer 연동)
    // ════════════════════════════════════════

    System.Action storyOnComplete;

    /// <summary>
    /// StoryPlayer에서 호출. NPC 대사를 표시. NPCData 없이 직접 텍스트 전달.
    /// </summary>
    public void ShowStoryDialogue(string speakerName, string[] lines, System.Action onComplete)
    {
        currentNPC = null;
        currentPlayerGO = null;
        storyOnComplete = onComplete;

        isShowing = true;
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);
        choicePanel.SetActive(false);

        nameText.text = speakerName ?? "";
        relationLabel.gameObject.SetActive(false);

        currentLines = lines;
        lineIndex = 0;
        waitingForChoice = false;
        choiceCallback = null;

        ShowCurrentLine(() =>
        {
            Hide();
            var cb = storyOnComplete;
            storyOnComplete = null;
            cb?.Invoke();
        });
    }

    /// <summary>
    /// StoryPlayer에서 호출. 선택지만 표시하고 선택 콜백 반환.
    /// </summary>
    public void ShowStoryChoices(string[] choiceTextsArray, System.Action<int> onChoice)
    {
        isShowing = true;
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);
        choicePanel.SetActive(true);
        continueHint.gameObject.SetActive(false);

        // 대사 영역 숨기기
        dialogueText.text = "";
        nameText.text = "";
        relationLabel.gameObject.SetActive(false);

        waitingForChoice = true;

        // DialogueChoice 배열로 변환
        var tempChoices = new DialogueChoice[choiceTextsArray.Length];
        for (int i = 0; i < choiceTextsArray.Length; i++)
        {
            tempChoices[i] = new DialogueChoice { text = choiceTextsArray[i] };
        }
        currentChoices = tempChoices;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choiceTextsArray.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                this.choiceTexts[i].text = $"  {i + 1}. {choiceTextsArray[i]}";
                int idx = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() =>
                {
                    waitingForChoice = false;
                    choicePanel.SetActive(false);
                    Hide();
                    onChoice?.Invoke(idx);
                });
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        choiceHighlight = 0;
        UpdateChoiceHighlight();

        choiceCallback = (idx) =>
        {
            waitingForChoice = false;
            choicePanel.SetActive(false);
            Hide();
            onChoice?.Invoke(idx);
        };
    }

    // ════════════════════════════════════════
    //  UI 구축
    // ════════════════════════════════════════

    public void GenerateUI()
    {
        var canvasGO = new GameObject("Dialogue_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasGO.transform, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // 하단 대화창 배경
        var dialogueBG = new GameObject("DialogueBG");
        dialogueBG.transform.SetParent(panelRoot.transform, false);
        var bgRT = dialogueBG.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0);
        bgRT.anchorMax = new Vector2(1, 0);
        bgRT.pivot = new Vector2(0.5f, 0);
        bgRT.offsetMin = new Vector2(60, 20);
        bgRT.offsetMax = new Vector2(-60, 260);
        var bgImg = dialogueBG.AddComponent<Image>();
        bgImg.color = UITheme.Panel;   // 불투명 — 뒤 맵 글자 비침 방지

        // NPC 이름
        nameText = MakeText(dialogueBG.transform, "NPCName", "", 20, TextAnchor.MiddleLeft);
        var nameRT = nameText.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(0.5f, 1);
        nameRT.pivot = new Vector2(0, 1);
        nameRT.offsetMin = new Vector2(20, -40);
        nameRT.offsetMax = new Vector2(300, -8);
        nameText.color = UITheme.Gold;
        nameText.fontStyle = FontStyle.Bold;

        // 관계 라벨
        relationLabel = MakeText(dialogueBG.transform, "Relation", "", 16, TextAnchor.MiddleLeft);
        var relRT = relationLabel.GetComponent<RectTransform>();
        relRT.anchorMin = new Vector2(0, 1);
        relRT.anchorMax = new Vector2(1, 1);
        relRT.pivot = new Vector2(0, 1);
        relRT.offsetMin = new Vector2(220, -40);
        relRT.offsetMax = new Vector2(400, -12);
        relationLabel.color = UITheme.Positive;

        // 대사 텍스트
        dialogueText = MakeText(dialogueBG.transform, "Dialogue", "", 18, TextAnchor.UpperLeft);
        var dlgRT = dialogueText.GetComponent<RectTransform>();
        dlgRT.anchorMin = new Vector2(0, 0);
        dlgRT.anchorMax = new Vector2(1, 1);
        dlgRT.offsetMin = new Vector2(24, 40);
        dlgRT.offsetMax = new Vector2(-24, -48);
        dialogueText.color = UITheme.TextBright;

        // 계속 힌트
        continueHint = MakeText(dialogueBG.transform, "ContinueHint", "[E] 계속 ▶", 14, TextAnchor.MiddleRight);
        var hintRT = continueHint.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(1, 0);
        hintRT.anchorMax = new Vector2(1, 0);
        hintRT.pivot = new Vector2(1, 0);
        hintRT.offsetMin = new Vector2(-160, 8);
        hintRT.offsetMax = new Vector2(-16, 32);
        continueHint.color = UITheme.TextMuted;

        // 선택지 패널
        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(panelRoot.transform, false);
        var cpRT = choicePanel.AddComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0, 0);
        cpRT.anchorMax = new Vector2(1, 0);
        cpRT.pivot = new Vector2(0.5f, 0);
        cpRT.offsetMin = new Vector2(60, 270);
        cpRT.offsetMax = new Vector2(-60, 430);
        var cpBg = choicePanel.AddComponent<Image>();
        cpBg.color = UITheme.PanelAlt;   // 불투명

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
            btnRT.offsetMin = new Vector2(12, -(i + 1) * 48);
            btnRT.offsetMax = new Vector2(-12, -i * 48 - 8);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = UITheme.PanelAlt;

            choiceButtons[i] = btnGO.AddComponent<Button>();
            var colors = choiceButtons[i].colors;
            colors.highlightedColor = UITheme.CellHover;
            colors.pressedColor = UITheme.CellPressed;
            choiceButtons[i].colors = colors;

            choiceTexts[i] = MakeText(btnGO.transform, "Text", "", 16, TextAnchor.MiddleLeft);
            var txtRT = choiceTexts[i].GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8, 0);
            txtRT.offsetMax = new Vector2(-8, 0);
            choiceTexts[i].color = UITheme.TextBright;
        }

        choicePanel.SetActive(false);
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
        var child = transform.Find("Dialogue_Canvas");
        if (child != null)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        canvas = null;
        panelRoot = null;
        nameText = null;
        dialogueText = null;
        relationLabel = null;
        choicePanel = null;
        choiceButtons = null;
        choiceTexts = null;
        continueHint = null;
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
