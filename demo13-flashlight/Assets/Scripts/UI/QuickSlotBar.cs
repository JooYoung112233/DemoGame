using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 퀵슬롯 바 (1~6). 소비/사용 아이템을 슬롯에 등록 → 숫자키(또는 슬롯 클릭)로 즉시 사용.
/// 등록: 인벤 우클릭 "퀵슬롯"(토글). 사용: 게임플레이 중 1~6 키.
/// 자가 부트스트랩(부팅 시 생성). 플레이어 존재 시 표시, 모달 UI 없을 때만 입력.
/// (슬롯은 itemId만 보관 — 런타임 전용, 세이브는 후속.)
/// </summary>
public class QuickSlotBar : MonoBehaviour
{
    public static QuickSlotBar Instance { get; private set; }
    const int SlotCount = 6;

    readonly string[] _slotIds = new string[SlotCount];

    PlayerInventory inventory;
    GameObject playerGO;

    Canvas canvas;
    GameObject barRoot;
    Image[] slotBgs = new Image[SlotCount];
    Image[] slotIcons = new Image[SlotCount];
    Text[] slotNames = new Text[SlotCount];
    Text[] slotCounts = new Text[SlotCount];
    RectTransform staminaFillRT;
    Image staminaFill;
    Font koreanFont;

    const float CELL = 56f, GAP = 6f;
    const int SortingOrder = 30;   // HUD 층 (CharacterPanel 40 아래 → 모달 열리면 가려짐)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[QuickSlotBar]");
        DontDestroyOnLoad(go);
        go.AddComponent<QuickSlotBar>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        koreanFont = LoadKoreanFont();
        GenerateUI();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>아이템을 퀵슬롯에 등록(토글: 이미 있으면 해제 / 없으면 첫 빈 슬롯 / 다 차면 1번 교체).</summary>
    public void Assign(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        for (int i = 0; i < SlotCount; i++)
            if (_slotIds[i] == itemId) { _slotIds[i] = null; Refresh(); ToastManager.Show("퀵슬롯 해제", ToastManager.ToastType.Info); return; }
        for (int i = 0; i < SlotCount; i++)
            if (string.IsNullOrEmpty(_slotIds[i])) { _slotIds[i] = itemId; Refresh(); ToastManager.Show($"퀵슬롯 {i + 1} 등록", ToastManager.ToastType.Info); return; }
        _slotIds[0] = itemId; Refresh();
        ToastManager.Show("퀵슬롯 1 교체", ToastManager.ToastType.Info);
    }

    /// <summary>세이브용 — 슬롯 itemId 6개(빈 칸은 "").</summary>
    public System.Collections.Generic.List<string> GetSlotIds()
    {
        var list = new System.Collections.Generic.List<string>(SlotCount);
        for (int i = 0; i < SlotCount; i++) list.Add(_slotIds[i] ?? "");
        return list;
    }

    /// <summary>로드용 — 저장된 itemId를 슬롯에 복원.</summary>
    public void LoadSlotIds(System.Collections.Generic.List<string> ids)
    {
        if (ids == null) return;
        for (int i = 0; i < SlotCount; i++)
            _slotIds[i] = (i < ids.Count && !string.IsNullOrEmpty(ids[i])) ? ids[i] : null;
        Refresh();
    }

    void Update()
    {
        ResolveRefs();
        if (barRoot != null) barRoot.SetActive(playerGO != null);
        if (playerGO == null) return;

        RefreshCounts();
        UpdateStamina();

        // 모달 UI 열려 있으면 입력만 차단(표시는 유지하되 모달이 위를 덮음)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        for (int i = 0; i < SlotCount; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                UseSlot(i);
    }

    void ResolveRefs()
    {
        if (playerGO == null)
        {
            playerGO = GameObject.FindGameObjectWithTag("Player");
            inventory = playerGO != null ? playerGO.GetComponent<PlayerInventory>() : null;
        }
        else if (inventory == null)
        {
            inventory = playerGO.GetComponent<PlayerInventory>();
        }
    }

    void UseSlot(int i)
    {
        if (i < 0 || i >= SlotCount) return;
        string id = _slotIds[i];
        if (string.IsNullOrEmpty(id) || inventory == null) return;
        if (inventory.CountItemAll(id) <= 0)
        {
            ToastManager.Show("아이템 없음", ToastManager.ToastType.Warning);
            return;
        }
        inventory.UseItemById(id);
        RefreshCounts();
    }

    // ── UI ────────────────────────────────────────────────────────────

    void GenerateUI()
    {
        var canvasGO = new GameObject("QuickSlot_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        barRoot = new GameObject("Bar", typeof(RectTransform));
        barRoot.transform.SetParent(canvas.transform, false);
        var barRT = barRoot.GetComponent<RectTransform>();
        barRT.anchorMin = barRT.anchorMax = barRT.pivot = new Vector2(0.5f, 0f);
        float totalW = SlotCount * CELL + (SlotCount - 1) * GAP;
        barRT.sizeDelta = new Vector2(totalW, CELL);
        barRT.anchoredPosition = new Vector2(0, 18);

        for (int i = 0; i < SlotCount; i++)
        {
            int captured = i;
            var cell = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            cell.transform.SetParent(barRT, false);
            var rt = cell.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(CELL, CELL);
            rt.anchoredPosition = new Vector2(i * (CELL + GAP), 0);
            var bg = cell.GetComponent<Image>();
            bg.color = UITheme.Cell;
            slotBgs[i] = bg;
            cell.GetComponent<Button>().onClick.AddListener(() => UseSlot(captured));

            // 숫자 라벨 (좌상단)
            var num = MakeText(rt, "Num", 13, UITheme.TextMuted, TextAnchor.UpperLeft);
            num.text = (i + 1).ToString();
            var nRT = (RectTransform)num.transform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(0, 1); nRT.pivot = new Vector2(0, 1);
            nRT.anchoredPosition = new Vector2(4, -2); nRT.sizeDelta = new Vector2(16, 16);

            // 아이콘
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(rt, false);
            var icRT = iconGO.GetComponent<RectTransform>();
            icRT.anchorMin = Vector2.zero; icRT.anchorMax = Vector2.one;
            icRT.offsetMin = new Vector2(6, 6); icRT.offsetMax = new Vector2(-6, -6);
            var icon = iconGO.GetComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false; icon.enabled = false;
            slotIcons[i] = icon;

            // 아이콘 없는 아이템 이름 폴백
            var nameTxt = MakeText(rt, "Name", 11, UITheme.TextBright, TextAnchor.MiddleCenter);
            var nmRT = (RectTransform)nameTxt.transform;
            nmRT.anchorMin = Vector2.zero; nmRT.anchorMax = Vector2.one;
            nmRT.offsetMin = new Vector2(3, 3); nmRT.offsetMax = new Vector2(-3, -3);
            slotNames[i] = nameTxt;

            // 개수 (우하단)
            var cnt = MakeText(rt, "Count", 13, Color.white, TextAnchor.LowerRight);
            cnt.fontStyle = FontStyle.Bold;
            var cRT = (RectTransform)cnt.transform;
            cRT.anchorMin = new Vector2(1, 0); cRT.anchorMax = new Vector2(1, 0); cRT.pivot = new Vector2(1, 0);
            cRT.anchoredPosition = new Vector2(-3, 2); cRT.sizeDelta = new Vector2(36, 16);
            cnt.gameObject.AddComponent<Shadow>().effectColor = Color.black;
            slotCounts[i] = cnt;
        }

        // 스태미너 바 (슬롯 위 얇은 바)
        float totalW2 = SlotCount * CELL + (SlotCount - 1) * GAP;
        var stamBg = new GameObject("StaminaBar", typeof(RectTransform), typeof(Image));
        stamBg.transform.SetParent(barRoot.transform, false);
        var sbRT = stamBg.GetComponent<RectTransform>();
        sbRT.anchorMin = sbRT.anchorMax = sbRT.pivot = new Vector2(0.5f, 0f);
        sbRT.sizeDelta = new Vector2(totalW2, 10f);
        sbRT.anchoredPosition = new Vector2(0, CELL + 8f);
        stamBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        stamBg.GetComponent<Image>().raycastTarget = false;

        var stamFillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        stamFillGO.transform.SetParent(sbRT, false);
        staminaFillRT = stamFillGO.GetComponent<RectTransform>();
        staminaFillRT.anchorMin = new Vector2(0, 0); staminaFillRT.anchorMax = new Vector2(1, 1);
        staminaFillRT.offsetMin = new Vector2(1, 1); staminaFillRT.offsetMax = new Vector2(-1, -1);
        staminaFill = stamFillGO.GetComponent<Image>();
        staminaFill.raycastTarget = false;
        staminaFill.color = UITheme.Positive;

        Refresh();
        barRoot.SetActive(false);
    }

    void UpdateStamina()
    {
        if (staminaFillRT == null) return;
        var p = TopDownPlayer.Instance;
        float pct = p != null ? Mathf.Clamp01(p.StaminaPercent) : 1f;
        staminaFillRT.anchorMax = new Vector2(pct, 1f);
        staminaFill.color = pct > 0.5f ? UITheme.Positive
            : pct > 0.2f ? new Color(0.9f, 0.8f, 0.3f)
            : new Color(0.9f, 0.35f, 0.3f);
    }

    /// <summary>슬롯 아이콘/이름 갱신(등록 변경 시).</summary>
    void Refresh()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var data = string.IsNullOrEmpty(_slotIds[i]) ? null : ItemDatabase.Get(_slotIds[i]);
            if (data != null && data.icon != null)
            {
                slotIcons[i].sprite = data.icon; slotIcons[i].enabled = true;
                slotNames[i].text = "";
            }
            else
            {
                slotIcons[i].enabled = false;
                slotNames[i].text = data != null ? data.displayName : "";
                if (slotNames[i].text != "") slotNames[i].color = data.RarityColor;
            }
        }
        RefreshCounts();
    }

    /// <summary>보유 개수 표시 + 0이면 슬롯 흐리게.</summary>
    void RefreshCounts()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            string id = _slotIds[i];
            if (string.IsNullOrEmpty(id))
            {
                slotCounts[i].text = "";
                slotBgs[i].color = UITheme.PanelAlt;
                continue;
            }
            int n = inventory != null ? inventory.CountItemAll(id) : 0;
            slotCounts[i].text = n > 0 ? n.ToString() : "";
            slotBgs[i].color = n > 0 ? UITheme.Cell : new Color(UITheme.Cell.r, UITheme.Cell.g, UITheme.Cell.b, 0.4f);
            float a = n > 0 ? 1f : 0.35f;
            var ic = slotIcons[i].color; slotIcons[i].color = new Color(ic.r, ic.g, ic.b, a);
        }
    }

    Text MakeText(Transform parent, string name, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.color = color; t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Font LoadKoreanFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Batang", "Arial" }, 14);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
