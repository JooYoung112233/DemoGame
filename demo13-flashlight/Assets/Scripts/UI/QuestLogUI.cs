using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 전체화면 "메신저(디스코드형) 의뢰/퀘스트 UI" (코드 생성 uGUI).
/// docs/quest.md §7-4 참조: 좌측 = 의뢰인/NPC 목록(아바타+이름, 신규/진행/완료 뱃지),
/// 우측 = 대화 스레드(의뢰 내용·목표 체크리스트·보상이 메시지 말풍선 형태 + 첨부 자리,
/// 하단에 수락/보고/추적 액션). 통신/메신저 톤.
///
/// 데이터 소스: QuestManager(활성/완료/진행도) + Resources/Data/Quests·DailyQuests(전체 의뢰)
/// + Resources/Data/NPC(npcId→displayName). 데이터가 비면 플레이스홀더 의뢰로 레이아웃 시연.
///
/// 공개 API: Show() / Hide() / IsShowing / ShowPreview(). 'J' 토글, ESC 닫기.
/// self-spawn DontDestroyOnLoad 싱글톤 (RadioUI 패턴).
/// </summary>
public class QuestLogUI : MonoBehaviour
{
    static QuestLogUI instance;
    static GameObject uiRoot;
    static bool isShowing;
    public static bool IsShowing => isShowing;

    // ── 한글 폰트 로더 ──
    static Font _kr;
    static Font KR
    {
        get
        {
            if (_kr == null)
                _kr = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Arial" }, 16);
            return _kr != null ? _kr : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    // ── 색 팔레트 (어두운 전술 톤) ──
    static readonly Color CDim = UITheme.Backdrop;
    static readonly Color CPanel = UITheme.Panel;
    static readonly Color CPanel2 = UITheme.PanelAlt;
    static readonly Color CGold = UITheme.Gold;
    static readonly Color CText = UITheme.TextBright;
    static readonly Color CFaint = UITheme.TextMuted;
    static readonly Color CLine = UITheme.Divider;
    static readonly Color CButton = UITheme.Cell;
    static readonly Color CDanger = UITheme.Danger;
    static readonly Color CGood = UITheme.Positive;
    static readonly Color CWarn = UITheme.Negative;

    // ── 한 발신자(의뢰인)의 스레드 묶음 ──
    class Sender
    {
        public string npcId;
        public string displayName;
        public string role;
        public List<Entry> entries = new List<Entry>();
        public int newCount;        // 신규(수락 가능) 의뢰 수
        public bool hasReportable;  // 보고 가능
    }

    // ── 한 의뢰 항목 (활성 인스턴스 또는 정의만 있는 가용 의뢰) ──
    class Entry
    {
        public QuestData data;
        public QuestInstance instance;   // null이면 미수락(가용/완료 정의)
        public QuestState state;         // 표시용 상태
        public bool placeholder;
        public string phTitle;
        public string phDesc;
        public string phSender;
        public string[] phObjectives;
        public string phReward;
    }

    readonly List<Sender> senders = new List<Sender>();
    Sender selected;
    RectTransform threadContent;       // 우측 스레드 메시지 컨테이너
    RectTransform sidebarContent;      // 좌측 목록 컨테이너
    RectTransform actionBar;           // 하단 액션 버튼 영역

    // ═══════════════════════════
    //  공개 API
    // ═══════════════════════════

    public static void Show()
    {
        if (isShowing) { Hide(); return; }

        if (instance == null)
        {
            var go = new GameObject("QuestLogUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<QuestLogUI>();
        }
        instance.BuildUI();
        isShowing = true;
    }

    public static void Hide()
    {
        isShowing = false;
        if (uiRoot != null) { Destroy(uiRoot); uiRoot = null; }
    }

    /// <summary>게이트 없는 미리보기 진입 (= Show).</summary>
    public static void ShowPreview() => Show();

    void Update()
    {
        // J 토글. 다른 UI가 떠 있으면 겹쳐 열지 않음(닫기는 항상 허용).
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isShowing) Hide();
            else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen()) Show();
        }
        if (!isShowing) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Hide();
    }

    // ═══════════════════════════
    //  데이터 수집
    // ═══════════════════════════

    void GatherData()
    {
        senders.Clear();
        selected = null;

        // npcId → displayName/role 매핑 (Resources/Data/NPC)
        var npcDefs = Resources.LoadAll<NPCData>("Data/NPC");
        var npcMap = new Dictionary<string, NPCData>();
        if (npcDefs != null)
            foreach (var n in npcDefs)
                if (n != null && !string.IsNullOrEmpty(n.npcId) && !npcMap.ContainsKey(n.npcId))
                    npcMap[n.npcId] = n;

        // 전체 의뢰 정의 (메인/사이드 + 데일리)
        var allQuests = new List<QuestData>();
        var q1 = Resources.LoadAll<QuestData>("Data/Quests");
        var q2 = Resources.LoadAll<QuestData>("Data/DailyQuests");
        if (q1 != null) allQuests.AddRange(q1);
        if (q2 != null) allQuests.AddRange(q2);

        var qm = QuestManager.Instance;
        var active = qm != null ? qm.ActiveQuests : new List<QuestInstance>();
        var completed = qm != null ? qm.CompletedQuestIds : new HashSet<string>();

        // 활성/완료가 표시할 항목을 만든다. 같은 questId 중복 방지.
        var seen = new HashSet<string>();

        // 1) 활성 인스턴스 (진행중 / 보고가능)
        foreach (var inst in active)
        {
            if (inst == null || inst.data == null) continue;
            if (inst.state == QuestState.Failed) continue;
            AddEntry(npcMap, inst.data, inst, inst.state);
            seen.Add(inst.data.questId);
        }

        // 2) 정의된 의뢰들: 완료/가용 표시
        foreach (var def in allQuests)
        {
            if (def == null || string.IsNullOrEmpty(def.questId)) continue;
            if (seen.Contains(def.questId)) continue;

            QuestState st;
            if (completed.Contains(def.questId)) st = QuestState.Completed;
            else st = QuestState.Available;

            AddEntry(npcMap, def, null, st);
            seen.Add(def.questId);
        }

        // 3) 데이터가 전혀 없으면 플레이스홀더 시연
        if (senders.Count == 0)
            BuildPlaceholders();

        // 발신자 정렬: 보고가능 → 신규 → 그 외
        senders.Sort((a, b) =>
        {
            int sa = (a.hasReportable ? 2 : 0) + (a.newCount > 0 ? 1 : 0);
            int sb = (b.hasReportable ? 2 : 0) + (b.newCount > 0 ? 1 : 0);
            return sb.CompareTo(sa);
        });

        if (senders.Count > 0) selected = senders[0];
    }

    void AddEntry(Dictionary<string, NPCData> npcMap, QuestData data, QuestInstance inst, QuestState st)
    {
        string nid = string.IsNullOrEmpty(data.giverNpcId) ? "_board" : data.giverNpcId;
        string dispName, role;
        if (nid == "_board")
        {
            dispName = "게시판 의뢰";
            role = "익명 의뢰";
        }
        else if (npcMap.TryGetValue(nid, out var nd) && nd != null)
        {
            dispName = string.IsNullOrEmpty(nd.displayName) ? nid : nd.displayName;
            role = nd.role;
        }
        else
        {
            dispName = nid;
            role = "";
        }

        var sender = senders.Find(s => s.npcId == nid);
        if (sender == null)
        {
            sender = new Sender { npcId = nid, displayName = dispName, role = role };
            senders.Add(sender);
        }

        sender.entries.Add(new Entry { data = data, instance = inst, state = st });
        if (st == QuestState.Available) sender.newCount++;
        if (st == QuestState.ReadyToReport) sender.hasReportable = true;
    }

    void BuildPlaceholders()
    {
        var ph = new[]
        {
            new Entry
            {
                placeholder = true, state = QuestState.ReadyToReport,
                phSender = "회수꾼", phTitle = "민이를 찾아 주세요",
                phDesc = "폐상가 교역 지구(낮)에서 '민이'의 흔적을 찾아 보고하라. 위험하니 야간엔 피해.",
                phObjectives = new[] { "폐상가 창고에서 흔적 발견 (1/1)" },
                phReward = "크레딧 ×10"
            },
            new Entry
            {
                placeholder = true, state = QuestState.Active,
                phSender = "회수꾼", phTitle = "붕대를 모아 와",
                phDesc = "의무실 재고가 바닥났다. 붕대 3개만 구해다 주면 사례하지.",
                phObjectives = new[] { "붕대 수집 (1/3)" },
                phReward = "크레딧 ×25, 진통제 ×1"
            },
            new Entry
            {
                placeholder = true, state = QuestState.Available,
                phSender = "구역 관리인", phTitle = "정찰 보고",
                phDesc = "구역3 북측 통신탑 상태를 확인하고 와. 접근만 하면 된다.",
                phObjectives = new[] { "통신탑 지점 도달 (0/1)" },
                phReward = "신뢰 +5"
            },
        };

        foreach (var e in ph)
        {
            var sender = senders.Find(s => s.displayName == e.phSender);
            if (sender == null)
            {
                sender = new Sender { npcId = e.phSender, displayName = e.phSender, role = "의뢰인" };
                senders.Add(sender);
            }
            sender.entries.Add(e);
            if (e.state == QuestState.Available) sender.newCount++;
            if (e.state == QuestState.ReadyToReport) sender.hasReportable = true;
        }
    }

    // ═══════════════════════════
    //  UI 구축
    // ═══════════════════════════

    void BuildUI()
    {
        GatherData();

        if (uiRoot != null) Destroy(uiRoot);

        uiRoot = new GameObject("QuestLogUI_Canvas");
        uiRoot.transform.SetParent(transform, false);
        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 58;
        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        uiRoot.AddComponent<GraphicRaycaster>();

        var rootRT = uiRoot.GetComponent<RectTransform>();

        // 전체화면 딤 배경
        var dim = MakeStretch(uiRoot.transform, "Dim");
        dim.AddComponent<Image>().color = CDim;

        // ── 헤더바 (전폭, 높이 64) ──
        var header = MakeChild(dim.transform, "Header");
        var hRT = header.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
        hRT.pivot = new Vector2(0.5f, 1);
        hRT.offsetMin = new Vector2(0, -64); hRT.offsetMax = new Vector2(0, 0);
        header.AddComponent<Image>().color = CPanel;

        MakeLabel(header.transform, "Title", "의뢰 · 통신", 26, CGold, TextAnchor.MiddleLeft,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 0), new Vector2(-200, 0), true);

        MakeLabel(header.transform, "Sub", "수신함 — 의뢰인 통신 채널", 14, CFaint, TextAnchor.MiddleLeft,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(190, -2), new Vector2(-200, 0), false);

        // 닫기 버튼 (48x40, 우측 끝)
        var close = MakeButton(header.transform, "Close", "✕", CDanger, 20, Hide);
        var cRT = close.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1, 0.5f); cRT.anchorMax = new Vector2(1, 0.5f);
        cRT.pivot = new Vector2(1, 0.5f);
        cRT.anchoredPosition = new Vector2(-12, 0);
        cRT.sizeDelta = new Vector2(48, 40);

        // 헤더 아래 1px 구분선
        var hLine = MakeChild(dim.transform, "HeaderLine");
        var hlRT = hLine.AddComponent<RectTransform>();
        hlRT.anchorMin = new Vector2(0, 1); hlRT.anchorMax = new Vector2(1, 1);
        hlRT.pivot = new Vector2(0.5f, 1);
        hlRT.offsetMin = new Vector2(0, -65); hlRT.offsetMax = new Vector2(0, -64);
        hLine.AddComponent<Image>().color = CLine;

        // ── 본문 영역 (헤더 아래 ~ 하단, 좌우 여백 40) ──
        var body = MakeChild(dim.transform, "Body");
        var bRT = body.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 1);
        bRT.offsetMin = new Vector2(40, 30); bRT.offsetMax = new Vector2(-40, -80);

        // 좌측 사이드바 (폭 300, 패널2 색)
        var sidebar = MakeChild(body.transform, "Sidebar");
        var sbRT = sidebar.AddComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0, 0); sbRT.anchorMax = new Vector2(0, 1);
        sbRT.pivot = new Vector2(0, 0.5f);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(300, 0);
        sidebar.AddComponent<Image>().color = CPanel2;

        MakeLabel(sidebar.transform, "SbTitle", "의뢰인", 16, CFaint, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -10), new Vector2(-16, -34), true);

        sidebarContent = MakeScrollView(sidebar.transform, "SbScroll",
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(6, 8), new Vector2(-6, -40));

        // 우측 본문 패널 (스레드)
        var rightPanel = MakeChild(body.transform, "Right");
        var rpRT = rightPanel.AddComponent<RectTransform>();
        rpRT.anchorMin = new Vector2(0, 0); rpRT.anchorMax = new Vector2(1, 1);
        rpRT.offsetMin = new Vector2(312, 0); rpRT.offsetMax = new Vector2(0, 0);
        rightPanel.AddComponent<Image>().color = CPanel;

        // 스레드 헤더 (선택 발신자명)
        var threadHeader = MakeChild(rightPanel.transform, "ThreadHeader");
        var thRT = threadHeader.AddComponent<RectTransform>();
        thRT.anchorMin = new Vector2(0, 1); thRT.anchorMax = new Vector2(1, 1);
        thRT.pivot = new Vector2(0.5f, 1);
        thRT.offsetMin = new Vector2(0, -50); thRT.offsetMax = new Vector2(0, 0);
        threadHeader.AddComponent<Image>().color = CPanel2;
        threadHeaderText = MakeLabel(threadHeader.transform, "ThName", "", 18, CText, TextAnchor.MiddleLeft,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 0), new Vector2(-18, 0), true);

        // 스레드 메시지 스크롤 (세로 레이아웃 — 말풍선 자동 스택)
        threadContent = MakeScrollView(rightPanel.transform, "ThreadScroll",
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 70), new Vector2(-8, -54));
        var threadVlg = threadContent.gameObject.AddComponent<VerticalLayoutGroup>();
        threadVlg.padding = new RectOffset(8, 8, 12, 12);
        threadVlg.spacing = 12f;
        threadVlg.childControlWidth = true;
        threadVlg.childControlHeight = true;
        threadVlg.childForceExpandWidth = true;
        threadVlg.childForceExpandHeight = false;
        var threadFitter = threadContent.gameObject.AddComponent<ContentSizeFitter>();
        threadFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        threadFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 하단 액션바
        actionBar = MakeChild(rightPanel.transform, "ActionBar").AddComponent<RectTransform>();
        actionBar.anchorMin = new Vector2(0, 0); actionBar.anchorMax = new Vector2(1, 0);
        actionBar.pivot = new Vector2(0.5f, 0);
        actionBar.offsetMin = new Vector2(0, 0); actionBar.offsetMax = new Vector2(0, 62);
        actionBar.gameObject.AddComponent<Image>().color = CPanel2;

        RebuildSidebar();
        RebuildThread();
    }

    Text threadHeaderText;

    // ── 좌측 발신자 목록 ──
    void RebuildSidebar()
    {
        if (sidebarContent == null) return;
        foreach (Transform c in sidebarContent) Destroy(c.gameObject);

        float y = -4f;
        foreach (var s in senders)
        {
            var sCap = s;
            var item = MakeChild(sidebarContent, "Sender_" + s.npcId);
            var itRT = item.AddComponent<RectTransform>();
            itRT.anchorMin = new Vector2(0, 1); itRT.anchorMax = new Vector2(1, 1);
            itRT.pivot = new Vector2(0.5f, 1);
            itRT.anchoredPosition = new Vector2(0, y);
            itRT.sizeDelta = new Vector2(-8, 64);
            bool isSel = s == selected;
            var img = item.AddComponent<Image>();
            img.color = isSel ? UITheme.Accent : CButton;
            var btn = item.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { selected = sCap; RebuildSidebar(); RebuildThread(); });

            // 아바타 자리 (44x44 사각 플레이스홀더 + 이니셜)
            var av = MakeChild(item.transform, "Avatar");
            var avRT = av.AddComponent<RectTransform>();
            avRT.anchorMin = new Vector2(0, 0.5f); avRT.anchorMax = new Vector2(0, 0.5f);
            avRT.pivot = new Vector2(0, 0.5f);
            avRT.anchoredPosition = new Vector2(10, 0);
            avRT.sizeDelta = new Vector2(44, 44);
            av.AddComponent<Image>().color = UITheme.Cell;
            string initial = string.IsNullOrEmpty(s.displayName) ? "?" : s.displayName.Substring(0, 1);
            MakeLabel(av.transform, "Init", initial, 20, CGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);

            // 이름
            MakeLabel(item.transform, "Name", s.displayName, 15, CText, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(64, 6), new Vector2(-10, -8), true);

            // 한줄 미리보기 (최근 의뢰 제목)
            string preview = s.entries.Count > 0 ? EntryTitle(s.entries[s.entries.Count - 1]) : "(의뢰 없음)";
            MakeLabel(item.transform, "Preview", preview, 12, CFaint, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(64, 8), new Vector2(-10, -34), false);

            // 뱃지 (신규/보고)
            if (s.hasReportable || s.newCount > 0)
            {
                string badgeTxt = s.hasReportable ? "보고" : "신규 " + s.newCount;
                Color badgeColor = s.hasReportable ? CGood : CWarn;
                var badge = MakeChild(item.transform, "Badge");
                var bgRT = badge.AddComponent<RectTransform>();
                bgRT.anchorMin = new Vector2(1, 1); bgRT.anchorMax = new Vector2(1, 1);
                bgRT.pivot = new Vector2(1, 1);
                bgRT.anchoredPosition = new Vector2(-8, -8);
                bgRT.sizeDelta = new Vector2(54, 20);
                var bImg = badge.AddComponent<Image>();
                bImg.color = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.22f);
                MakeLabel(badge.transform, "Txt", badgeTxt, 11, badgeColor, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            }

            y -= 68f;
        }

        sidebarContent.sizeDelta = new Vector2(sidebarContent.sizeDelta.x, Mathf.Max(10f, -y + 8f));
    }

    // ── 우측 스레드 ──
    void RebuildThread()
    {
        if (threadContent == null) return;
        foreach (Transform c in threadContent) Destroy(c.gameObject);
        foreach (Transform c in actionBar) Destroy(c.gameObject);

        if (selected == null)
        {
            if (threadHeaderText != null) threadHeaderText.text = "";
            MakeLabel(threadContent, "Empty", "수신된 의뢰가 없습니다.", 16, CFaint, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -50), new Vector2(-16, -16), false);
            return;
        }

        if (threadHeaderText != null)
        {
            string roleSuffix = string.IsNullOrEmpty(selected.role) ? "" : "  <size=12><color=#8A8270>· " + selected.role + "</color></size>";
            threadHeaderText.text = "@ " + selected.displayName + roleSuffix;
        }

        foreach (var e in selected.entries)
            AddMessageBubble(e);

        BuildActionBar();
    }

    // 메시지 말풍선 1개(의뢰 1건) — 제목/상태/설명/목표 체크리스트/보상/첨부 자리.
    // VerticalLayoutGroup(threadContent) 안에 추가 → 높이는 ContentSizeFitter가 자동 산출.
    void AddMessageBubble(Entry e)
    {
        // 본문 텍스트 합성 (rich text)
        string title = EntryTitle(e);
        string desc = e.placeholder ? e.phDesc : (e.data != null ? e.data.description : "");

        var sb = new System.Text.StringBuilder();
        sb.Append("<b><color=#DBBC6B>").Append(title).Append("</color></b>  ");
        sb.Append(StateTag(e.state)).Append("\n");
        if (!string.IsNullOrEmpty(desc))
            sb.Append("<color=#EBE5D6>").Append(desc).Append("</color>\n");

        // 목표 체크리스트
        sb.Append("\n<color=#8A8270>목표</color>\n");
        foreach (var line in ObjectiveLines(e))
            sb.Append(line).Append("\n");

        // 보상
        sb.Append("\n<color=#8A8270>보상</color>\n<color=#EBE5D6>")
          .Append(RewardLine(e)).Append("</color>");

        // 첨부 자리 (지도/아이템 첨부 플레이스홀더)
        if (HasAttachment(e))
            sb.Append("\n\n<color=#665F52>[첨부] ").Append(AttachmentLabel(e)).Append("</color>");

        // 말풍선 박스: 높이를 내용에 맞게 자동(ContentSizeFitter)
        var bubble = MakeChild(threadContent, "Msg");
        bubble.AddComponent<RectTransform>();
        var bImg = bubble.AddComponent<Image>();
        bImg.color = UITheme.PanelAlt;
        var fitter = bubble.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        // VerticalLayoutGroup이 좌우 폭을 채우고, 내부 패딩으로 본문 영역 확보
        var inner = bubble.AddComponent<VerticalLayoutGroup>();
        inner.padding = new RectOffset(20, 16, 12, 12);
        inner.childControlWidth = true;
        inner.childControlHeight = true;
        inner.childForceExpandWidth = true;
        inner.childForceExpandHeight = false;

        // 좌측 상태 색 스트립
        var strip = MakeChild(bubble.transform, "Strip");
        var stRT = strip.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(0, 1);
        stRT.pivot = new Vector2(0, 0.5f);
        stRT.anchoredPosition = Vector2.zero;
        stRT.sizeDelta = new Vector2(4, 0);
        strip.AddComponent<Image>().color = StateColor(e.state);
        // 스트립은 레이아웃 흐름에서 제외
        strip.AddComponent<LayoutElement>().ignoreLayout = true;

        // 본문 텍스트 (레이아웃이 폭/높이 제어, preferredHeight 자동)
        var txtGO = MakeChild(bubble.transform, "Body");
        txtGO.AddComponent<RectTransform>();
        var txt = txtGO.AddComponent<Text>();
        txt.font = KR;
        txt.fontSize = 15;
        txt.color = CText;
        txt.alignment = TextAnchor.UpperLeft;
        txt.supportRichText = true;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.text = sb.ToString();
    }

    // 하단 액션 버튼: 상태별 (수락 / 추적 / 보고)
    void BuildActionBar()
    {
        if (actionBar == null || selected == null) return;

        // 대표 상태 = 보고가능 > 신규 > 진행중 > 완료
        Entry rep = null;
        foreach (var e in selected.entries)
        {
            if (e.state == QuestState.ReadyToReport) { rep = e; break; }
        }
        if (rep == null) foreach (var e in selected.entries) if (e.state == QuestState.Available) { rep = e; break; }
        if (rep == null) foreach (var e in selected.entries) if (e.state == QuestState.Active) { rep = e; break; }
        if (rep == null && selected.entries.Count > 0) rep = selected.entries[0];
        if (rep == null) return;

        var repCap = rep;

        float x = 16f;
        if (rep.state == QuestState.Available)
        {
            x = AddActionButton("수락", UITheme.Buy, x, () => OnAccept(repCap));
            x = AddActionButton("거절", CButton, x, () => ToastManager.Show("의뢰를 보류했습니다.", ToastManager.ToastType.Info));
        }
        else if (rep.state == QuestState.ReadyToReport)
        {
            x = AddActionButton("보고", UITheme.Accent, x, () => OnReport(repCap));
            x = AddActionButton("추적", CButton, x, () => OnTrack(repCap));
        }
        else if (rep.state == QuestState.Active)
        {
            x = AddActionButton("추적", UITheme.Accent, x, () => OnTrack(repCap));
        }
        else // Completed
        {
            AddActionLabel("완료된 의뢰", CGood, 16f);
        }
    }

    void OnAccept(Entry e)
    {
        if (e.placeholder || e.data == null)
        {
            ToastManager.Show("(데모) 의뢰 수락", ToastManager.ToastType.Success);
            return;
        }
        var qm = QuestManager.Instance;
        if (qm == null) { ToastManager.Show("QuestManager 없음", ToastManager.ToastType.Warning); return; }
        if (qm.AcceptQuest(e.data))
        {
            ToastManager.Show("의뢰 수락: " + e.data.title, ToastManager.ToastType.Success);
            BuildUI(); // 데이터/뱃지 갱신
        }
        else ToastManager.Show("이미 수주했거나 수락 불가", ToastManager.ToastType.Warning);
    }

    void OnReport(Entry e)
    {
        if (e.placeholder || e.data == null)
        {
            ToastManager.Show("(데모) 의뢰 보고 완료", ToastManager.ToastType.Success);
            return;
        }
        var qm = QuestManager.Instance;
        if (qm == null) { ToastManager.Show("QuestManager 없음", ToastManager.ToastType.Warning); return; }
        if (qm.CompleteQuest(e.data.questId))
        {
            ToastManager.Show("의뢰 보고 완료: " + e.data.title, ToastManager.ToastType.Success);
            BuildUI();
        }
        else ToastManager.Show("아직 보고할 수 없습니다.", ToastManager.ToastType.Warning);
    }

    void OnTrack(Entry e)
    {
        string t = EntryTitle(e);
        ToastManager.Show("추적: " + t, ToastManager.ToastType.Info);
    }

    // ═══════════════════════════
    //  표시 헬퍼
    // ═══════════════════════════

    string EntryTitle(Entry e)
    {
        if (e.placeholder) return e.phTitle;
        return e.data != null && !string.IsNullOrEmpty(e.data.title) ? e.data.title : "(제목 없음)";
    }

    string StateTag(QuestState st) => st switch
    {
        QuestState.Available => "<color=#FF8C73>● 신규</color>",
        QuestState.Active => "<color=#F2D94C>● 진행중</color>",
        QuestState.ReadyToReport => "<color=#73D973>● 보고 가능</color>",
        QuestState.Completed => "<color=#8A8270>● 완료</color>",
        QuestState.Failed => "<color=#FF7373>● 실패</color>",
        _ => ""
    };

    Color StateColor(QuestState st) => st switch
    {
        QuestState.Available => CWarn,
        QuestState.Active => CGold,
        QuestState.ReadyToReport => CGood,
        QuestState.Completed => CFaint,
        QuestState.Failed => CDanger,
        _ => CFaint
    };

    IEnumerable<string> ObjectiveLines(Entry e)
    {
        if (e.placeholder)
        {
            if (e.phObjectives != null)
                foreach (var o in e.phObjectives)
                    yield return "  <color=#CCCCCC>☐ " + o + "</color>";
            yield break;
        }

        if (e.data == null || e.data.objectives == null || e.data.objectives.Length == 0)
        {
            yield return "  <color=#8A8270>(목표 없음)</color>";
            yield break;
        }

        for (int i = 0; i < e.data.objectives.Length; i++)
        {
            var obj = e.data.objectives[i];
            int cur = 0;
            if (e.instance != null && e.instance.progress.ContainsKey(i))
                cur = e.instance.progress[i];
            bool done = cur >= obj.requiredCount;
            // 완료 처리된 의뢰는 전부 달성으로 표시
            if (e.state == QuestState.Completed) { done = true; cur = obj.requiredCount; }
            string box = done ? "<color=#73D973>☑</color>" : "<color=#CCCCCC>☐</color>";
            string color = done ? "#88FF88" : "#CCCCCC";
            string descTxt = string.IsNullOrEmpty(obj.description) ? ObjectiveTypeLabel(obj.type) : obj.description;
            yield return $"  {box} <color={color}>{descTxt} ({cur}/{obj.requiredCount})</color>";
        }
    }

    string ObjectiveTypeLabel(ObjectiveType t) => t switch
    {
        ObjectiveType.CollectItem => "아이템 수집",
        ObjectiveType.KillEnemy => "적 처치",
        ObjectiveType.ReachPoint => "지점 도달",
        ObjectiveType.TalkToNPC => "NPC 대화",
        _ => "목표"
    };

    string RewardLine(Entry e)
    {
        if (e.placeholder) return string.IsNullOrEmpty(e.phReward) ? "(없음)" : e.phReward;
        if (e.data == null || e.data.rewards == null || e.data.rewards.Length == 0) return "(없음)";

        var parts = new List<string>();
        foreach (var r in e.data.rewards)
        {
            switch (r.type)
            {
                case QuestRewardType.Item:
                    parts.Add((string.IsNullOrEmpty(r.itemId) ? "아이템" : r.itemId) + " ×" + r.amount);
                    break;
                case QuestRewardType.Currency:
                    parts.Add("크레딧 ×" + r.amount);
                    break;
                case QuestRewardType.Affinity:
                    parts.Add("호감 +" + r.amount);
                    break;
                case QuestRewardType.Trust:
                    parts.Add("신뢰 +" + r.amount);
                    break;
                case QuestRewardType.Recipe:
                    parts.Add("레시피 해금: " + (string.IsNullOrEmpty(r.itemId) ? "?" : r.itemId));
                    break;
            }
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "(없음)";
    }

    bool HasAttachment(Entry e)
    {
        if (e.placeholder)
            return e.phObjectives != null && e.phObjectives.Length > 0;
        if (e.data == null || e.data.objectives == null) return false;
        foreach (var o in e.data.objectives)
            if (o.type == ObjectiveType.ReachPoint || o.type == ObjectiveType.CollectItem)
                return true;
        return false;
    }

    string AttachmentLabel(Entry e)
    {
        if (e.placeholder) return "현장 좌표 · 지도";
        if (e.data != null && !string.IsNullOrEmpty(e.data.region))
            return "지역: " + e.data.region + " · 지도 첨부";
        return "지도 첨부";
    }

    // ═══════════════════════════
    //  액션바 버튼
    // ═══════════════════════════

    float AddActionButton(string label, Color color, float x, UnityEngine.Events.UnityAction onClick)
    {
        var go = MakeButton(actionBar, "Act_" + label, label, color, 16, onClick);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(140, 40);
        return x + 152f;
    }

    void AddActionLabel(string label, Color color, float x)
    {
        MakeLabel(actionBar, "ActLbl", label, 16, color, TextAnchor.MiddleLeft,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(x, 0), new Vector2(-16, 0), true);
    }

    // ═══════════════════════════
    //  UI 빌더 유틸
    // ═══════════════════════════

    GameObject MakeChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    GameObject MakeStretch(Transform parent, string name)
    {
        var go = MakeChild(parent, name);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    /// <summary>앵커/오프셋 기반 라벨 생성.</summary>
    Text MakeLabel(Transform parent, string name, string content, int fontSize, Color color, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool bold)
    {
        var go = MakeChild(parent, name);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var txt = go.AddComponent<Text>();
        txt.font = KR;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.text = content;
        txt.alignment = align;
        txt.supportRichText = true;
        txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    GameObject MakeButton(Transform parent, string name, string label, Color color, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        var go = MakeChild(parent, name);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        if (onClick != null) btn.onClick.AddListener(onClick);

        var lbl = MakeChild(go.transform, "Label");
        var lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var t = lbl.AddComponent<Text>();
        t.font = KR;
        t.fontSize = fontSize;
        t.text = label;
        t.color = CText;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.supportRichText = true;
        return go;
    }

    /// <summary>
    /// 스크롤뷰(세로) 생성 후 content RectTransform 반환.
    /// content는 anchor top-stretch, pivot(0.5,1), 높이는 호출측에서 설정.
    /// </summary>
    RectTransform MakeScrollView(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var viewport = MakeChild(parent, name);
        var vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = anchorMin; vpRT.anchorMax = anchorMax;
        vpRT.offsetMin = offsetMin; vpRT.offsetMax = offsetMax;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.001f); // 거의 투명, raycast target용
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var content = MakeChild(viewport.transform, "Content");
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.offsetMin = new Vector2(0, 0); cRT.offsetMax = new Vector2(0, 0);
        cRT.sizeDelta = new Vector2(0, 10);

        scroll.viewport = vpRT;
        scroll.content = cRT;
        return cRT;
    }
}
