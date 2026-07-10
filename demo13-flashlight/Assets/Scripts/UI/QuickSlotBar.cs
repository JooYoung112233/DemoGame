using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 퀵슬롯 바 (1~6 — docs/inventory.md 2026-07-10: 4칸으로 줄였다가 사용자 요청으로 6칸 복구).
/// 소모품(의료·음식)을 슬롯에 등록 → 숫자키(또는 슬롯 클릭)로 즉시 사용.
/// 등록: 인벤 드래그로 슬롯에 놓기(해당 슬롯 지정) 또는 우클릭 "퀵슬롯"(첫 빈 칸 토글). 의료·음식만 등록 가능.
/// 자가 부트스트랩(부팅 시 생성). 플레이어 존재 시 표시, 모달 UI 없을 때만 입력.
/// 슬롯은 itemId만 보관 — SaveManager가 저장/복원(quickSlots).
/// </summary>
public class QuickSlotBar : MonoBehaviour
{
    public static QuickSlotBar Instance { get; private set; }
    const int SlotCount = 6;

    readonly string[] _slotIds = new string[SlotCount];

    PlayerInventory inventory;
    GameObject playerGO;

    // uGUI (프리팹 베이크 시 직렬화 보존). 슬롯은 고정 6개 — 1회 생성 후 갱신만 하므로 직렬화 OK.
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject barRoot;
    [SerializeField] Image[] slotBgs = new Image[SlotCount];
    [SerializeField] Image[] slotIcons = new Image[SlotCount];
    [SerializeField] Text[] slotNames = new Text[SlotCount];
    [SerializeField] Text[] slotCounts = new Text[SlotCount];
    [SerializeField] RectTransform staminaFillRT;
    [SerializeField] Image staminaFill;
    Font koreanFont;   // 런타임 동적 OS 폰트 — 직렬화 안 함(Instantiate 후 재바인딩)

    const float CELL = 56f, GAP = 6f;
    const int SortingOrder = 30;   // HUD 층 (CharacterPanel 40 아래 → 모달 열리면 가려짐)

    bool IsGenerated => canvas != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
        var prefab = Resources.Load<GameObject>("UI/QuickSlotBar");
        GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("[QuickSlotBar]");
        go.name = "[QuickSlotBar]";
        if (prefab == null) go.AddComponent<QuickSlotBar>();   // 폴백: Awake가 GenerateUI
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        koreanFont = LoadKoreanFont();
        if (!IsGenerated) GenerateUI();   // 폴백: 프리팹 없이 코드로 생성
        else ApplyFonts();                // 프리팹 인스턴스: 동적 폰트 재바인딩
        WireEvents();                     // onClick은 프리팹에 직렬화 안 됨(§6.5) — 슬롯 클릭 재부착
    }

    /// <summary>슬롯 셀 onClick 재부착 — 빌더에서만 붙이면 프리팹 경로에서 클릭 무반응(§6.5).</summary>
    void WireEvents()
    {
        if (slotBgs == null) return;
        for (int i = 0; i < slotBgs.Length; i++)
        {
            if (slotBgs[i] == null) continue;
            var btn = slotBgs[i].GetComponent<UnityEngine.UI.Button>();
            if (btn == null) continue;
            int captured = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => UseSlot(captured));
        }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>퀵슬롯 등록 가능 여부 — 소모품(의료·음식)만 (docs/inventory.md 2026-07-10).</summary>
    public static bool IsAssignable(ItemData data)
        => data != null && (data.category == ItemCategory.Medical || data.category == ItemCategory.Consumable);

    /// <summary>드래그 드롭 등록 — 화면 좌표가 슬롯 위면 그 슬롯에 지정 등록(교체). 반환 = 바 위였는지(핸들 여부).
    /// 아이템 이동이 아니라 id 등록 — 호출자(CharacterPanelUI)가 드래그를 원위치 복귀시킨다.</summary>
    public bool TryAssignAtScreenPoint(Vector2 screenPos, string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || slotBgs == null) return false;
        for (int i = 0; i < SlotCount && i < slotBgs.Length; i++)
        {
            if (slotBgs[i] == null) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)slotBgs[i].transform, screenPos))
                continue;

            var data = ItemDatabase.Get(itemId);
            if (!IsAssignable(data))
            {
                ToastManager.Show("의료·음식만 퀵슬롯에 등록할 수 있다", ToastManager.ToastType.Warning);
                return true;   // 바 위 드롭이긴 함 — 제스처는 소비(원위치 복귀는 호출자)
            }
            _slotIds[i] = itemId;
            Refresh();
            ToastManager.Show($"퀵슬롯 {i + 1} 등록", ToastManager.ToastType.Info);
            return true;
        }
        return false;
    }

    /// <summary>아이템을 퀵슬롯에 등록(토글: 이미 있으면 해제 / 없으면 첫 빈 슬롯 / 다 차면 1번 교체). 의료·음식만.</summary>
    public void Assign(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (!IsAssignable(ItemDatabase.Get(itemId)))
        {
            ToastManager.Show("의료·음식만 퀵슬롯에 등록할 수 있다", ToastManager.ToastType.Warning);
            return;
        }
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
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (barRoot != null) barRoot.SetActive(playerGO != null);
        if (playerGO == null) return;

        RefreshCounts();
        UpdateStamina();

        // 모달 UI 열려 있으면 입력만 차단(표시는 유지하되 모달이 위를 덮음)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        for (int i = 0; i < SlotCount; i++)
            if (GameInput.GetKeyDown(KeyCode.Alpha1 + i) || GameInput.GetKeyDown(KeyCode.Keypad1 + i))
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

    /// <summary>프리팹 인스턴스화 시 동적 OS 폰트를 직렬화된 Text 참조에 재바인딩.</summary>
    void ApplyFonts()
    {
        // 전체 자식 Text 일괄 재바인딩 — 개별 ref 방식은 미직렬화 숫자 라벨("Num" 1~6)을 놓쳐
        // 프리팹 경로에서 슬롯 번호가 전부 안 보였다(전체 검수 2026-07-07). 이 바의 모든 텍스트 = koreanFont.
        var f = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var t in GetComponentsInChildren<Text>(true))
            if (t != null) t.font = f;
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        koreanFont = LoadKoreanFont();
        GenerateUI();
    }
#endif

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
