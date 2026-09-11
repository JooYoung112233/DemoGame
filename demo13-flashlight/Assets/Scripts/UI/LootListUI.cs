using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>루팅 목록 — 필드 상자·시체를 열면 뜨는 목록(2026-09-11, docs/region-loot.md §루팅 정리 결정).
/// 처음 여는 상자는 **옛 수색 연출**(2026-09-09 볼륨 축소 때 뺀 것을 사용자 요청으로 되살림): 칸이 '?'로 가려져
/// 있다가 위에서부터 하나씩 드러난다 — 희귀할수록 오래 걸린다(GameTuning.searchSec* × searchSpeedMult).
/// 다 드러나면 [전부 가져가기] / 하나씩(클릭·E). **가치(◈) 표기는 넣지 않는다**(사용자).
/// [클릭/E] 하나 · [F] 전부 · [Tab] 자세히(캐릭터 패널) · [Esc] 닫기. 멀어지면(3m) 닫힌다 — 수색 진행은 상자에 남는다.
/// 가져오기는 LootTake(정산 TrackLoot · 수집 퀘스트)를 탄다. 창고·보관함은 캐릭터 패널. 자가 생성(씬 배치 불필요).
/// (구 CorpseLootUI — 시체 전용이던 것을 모든 필드 상자로 넓힘)</summary>
[DefaultExecutionOrder(60)]   // InteractionSystem(0)보다 늦게 — 연 프레임의 E를 다시 처리하지 않게
public class LootListUI : MonoBehaviour
{
    public static LootListUI Instance { get; private set; }
    public static bool IsShowing => Instance != null && Instance._showing;
    public static void Hide() { if (Instance != null) Instance.Close(); }

    const int SortingOrder = 108;   // GroundPickupUI와 같은 층
    const float PanelWidth = 340f, RowHeight = 32f, RowGap = 3f, CloseDistance = 3f;

    bool _showing, _dirty;
    int _openFrame = -1, _selected;
    float _revealLeft;   // 다음 칸이 드러날 때까지 남은 시간
    LootContainer _box;
    GameObject _player;
    readonly List<InventoryGrid.PlacedItem> _rows = new List<InventoryGrid.PlacedItem>();

    Canvas _canvas;
    GameObject _root, _takeAllBtn, _detailBtn;
    RectTransform _panel, _list;
    Text _header, _hint;
    Font _font;

    public static void Show(LootContainer box, GameObject player)
    {
        if (box == null || player == null) return;
        if (Instance == null)
        {
            var go = new GameObject("[LootListUI]");
            go.AddComponent<LootListUI>();
            DontDestroyOnLoad(go);
        }
        Instance.Open(box, player);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _font = LoadKoreanFont();
        Build();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    bool Revealing => _box != null && !_box.HasBeenSearched && _box.RevealedCount < _rows.Count;

    void Open(LootContainer box, GameObject player)
    {
        if (_showing) Close();
        _box = box;
        _player = player;
        _selected = 0;
        _showing = true;
        _openFrame = Time.frameCount;
        _root.SetActive(true);
        if (_box.Grid != null) _box.Grid.OnChanged += OnGridChanged;
        RefreshRows();
        if (_box.HasBeenSearched || _rows.Count == 0) _box.HasBeenSearched = true;
        else _revealLeft = RevealDelay(_box.RevealedCount);
        Rebuild();
    }

    public void Close()
    {
        if (!_showing) return;
        _showing = false;
        if (_box != null)
        {
            if (_box.Grid != null) _box.Grid.OnChanged -= OnGridChanged;
            _box.Close();   // 비었으면 루팅 완료 처리
        }
        _box = null;
        _player = null;
        if (_root != null) _root.SetActive(false);
    }

    void OnGridChanged() { _dirty = true; }   // 다른 경로(자세히 창)로 빠져도 목록이 맞게 — 다음 프레임에 다시 그린다

    void Update()
    {
        if (!_showing) return;
        if (_box == null || _player == null) { Close(); return; }
        if (Vector3.Distance(_player.transform.position, _box.transform.position) > CloseDistance) { Close(); return; }
        if (_dirty) { _dirty = false; RefreshRows(); Rebuild(); }
        if (Time.frameCount == _openFrame) return;   // 연 프레임의 입력 무시
        if (GameInput.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        // 수색 중 — 하나씩 드러난다. 가져오기·자세히는 다 드러난 뒤에.
        if (Revealing)
        {
            _revealLeft -= Time.deltaTime;
            if (_revealLeft <= 0f)
            {
                _box.RevealedCount++;
                if (_box.RevealedCount >= _rows.Count) _box.HasBeenSearched = true;
                else _revealLeft = RevealDelay(_box.RevealedCount);
                Rebuild();
            }
            return;
        }

        if (_rows.Count == 0) { Close(); return; }
        if (GameInput.GetKeyDown(KeyCode.Tab)) { OpenDetail(); return; }

        float wheel = GameInput.mouseScrollDelta.y;
        if (wheel > 0.01f || GameInput.GetKeyDown(KeyCode.UpArrow)) Move(-1);
        else if (wheel < -0.01f || GameInput.GetKeyDown(KeyCode.DownArrow)) Move(1);

        if (GameInput.GetKeyDown(KeyCode.F)) { TakeAll(); return; }
        if (GameInput.GetKeyDown(KeyCode.E) || GameInput.GetKeyDown(KeyCode.Return) || GameInput.GetKeyDown(KeyCode.KeypadEnter))
            TakeAt(_selected);
    }

    /// <summary>index번째 칸이 드러나는 데 걸리는 시간 — 희귀할수록 길게(옛 수색 딜레이 값 그대로).</summary>
    float RevealDelay(int index)
    {
        var it = index >= 0 && index < _rows.Count ? _rows[index]?.item : null;
        var gt = GameTuning.Instance;
        float sec = 0.4f;
        if (it?.data != null)
            sec = it.data.rarity switch
            {
                ItemRarity.Uncommon  => gt != null ? gt.searchSecUncommon  : 0.6f,
                ItemRarity.Rare      => gt != null ? gt.searchSecRare      : 0.9f,
                ItemRarity.Epic      => gt != null ? gt.searchSecEpic      : 1.3f,
                ItemRarity.Legendary => gt != null ? gt.searchSecLegendary : 1.8f,
                _                    => gt != null ? gt.searchSecCommon    : 0.4f,
            };
        float mult = gt != null ? gt.searchSpeedMult : 1f;
        return sec / Mathf.Max(0.05f, mult);
    }

    void Move(int dir)
    {
        if (_rows.Count == 0) return;
        _selected = (_selected + dir + _rows.Count) % _rows.Count;
        Highlight();
    }

    /// <summary>한 줄 가져오기. 성공하면 true. 실패(공간·가방)는 토스트로 알린다.</summary>
    public bool TakeAt(int index)
    {
        if (Revealing || _box == null || _player == null || index < 0 || index >= _rows.Count) return false;
        var inv = _player.GetComponent<PlayerInventory>();
        if (!LootTake.Take(inv, _box.Grid, _rows[index])) return false;
        _dirty = true;
        return true;
    }

    /// <summary>위에서부터 전부 — 공간이 모자라면 거기서 멈춘다(나머지는 남긴다).</summary>
    public int TakeAll()
    {
        if (Revealing) return 0;
        int taken = 0;
        while (_rows.Count > 0 && _box != null)
        {
            if (!TakeAt(0)) break;
            taken++;
            RefreshRows();
        }
        _selected = 0;
        _dirty = true;
        return taken;
    }

    void OpenDetail()
    {
        var box = _box;
        var player = _player;
        Close();
        if (box == null) return;
        box.Open(player);
        if (UIManager.Instance != null) UIManager.Instance.ShowCharacterPanelWithContainer(box);
    }

    void RefreshRows()
    {
        _rows.Clear();
        if (_box != null && _box.Grid != null) _rows.AddRange(_box.Grid.GetAll());
        if (_selected >= _rows.Count) _selected = Mathf.Max(0, _rows.Count - 1);
    }

    // ── UI ────────────────────────────────────────────────────────────

    static string Label(ItemInstance it)
    {
        if (it == null || it.data == null) return "(사라짐)";
        // DisplayName엔 이미 수량이 붙어 있어 "x2 x2"가 됐다 — 원래 이름에 한 번만 붙인다.
        return it.stackCount > 1 ? $"{it.data.displayName} x{it.stackCount}" : it.data.displayName;
    }

    void Rebuild()
    {
        for (int i = _list.childCount - 1; i >= 0; i--) Destroy(_list.GetChild(i).gameObject);

        bool revealing = Revealing;
        int shown = _box != null ? (_box.HasBeenSearched ? _rows.Count : Mathf.Min(_box.RevealedCount, _rows.Count)) : 0;
        string name = _box != null ? _box.ContainerName : "상자";
        _header.text = revealing ? $"{name} — 뒤지는 중… ({shown}/{_rows.Count})"
                     : _rows.Count == 0 ? $"{name} — 비어 있다" : $"{name} — {_rows.Count}개";
        _hint.text = revealing ? "[Esc] 닫기 (진행은 남는다)" : "[클릭/E] 하나씩   [휠/↑↓] 선택   [Esc] 닫기";
        _takeAllBtn.SetActive(!revealing && _rows.Count > 0);
        _detailBtn.SetActive(!revealing && _rows.Count > 0);

        float listH = _rows.Count * (RowHeight + RowGap);
        _panel.sizeDelta = new Vector2(PanelWidth, 44f + listH + 44f + 26f);
        _list.sizeDelta = new Vector2(-16f, listH);

        for (int i = 0; i < _rows.Count; i++)
        {
            bool hidden = i >= shown;
            var it = _rows[i]?.item;
            Color rarity = hidden ? UITheme.TextDim : (it != null && it.data != null ? it.data.RarityColor : Color.gray);
            int captured = i;

            var row = new GameObject($"Row_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(_list, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, RowHeight);
            rt.anchoredPosition = new Vector2(0, -i * (RowHeight + RowGap));
            row.GetComponent<Image>().color = RowColor(!revealing && i == _selected);
            var btn = row.GetComponent<Button>();
            btn.interactable = !hidden && !revealing;
            btn.onClick.AddListener(() => { _selected = captured; TakeAt(captured); });

            var sw = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
            sw.transform.SetParent(row.transform, false);
            var swRT = (RectTransform)sw.transform;
            swRT.anchorMin = swRT.anchorMax = new Vector2(0, 0.5f); swRT.pivot = new Vector2(0, 0.5f);
            swRT.anchoredPosition = new Vector2(8, 0); swRT.sizeDelta = new Vector2(12, 12);
            sw.GetComponent<Image>().color = rarity;

            var label = MakeText(row.transform, "Name", 15, hidden ? FontStyle.Normal : FontStyle.Bold, rarity, TextAnchor.MiddleLeft);
            label.text = hidden ? "? ? ?" : Label(it);
            Stretch((RectTransform)label.transform, 28, -8);
        }
    }

    static void Stretch(RectTransform rt, float left, float right)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, 0); rt.offsetMax = new Vector2(right, 0);
    }

    void Highlight()
    {
        for (int i = 0; i < _list.childCount; i++)
        {
            var img = _list.GetChild(i).GetComponent<Image>();
            if (img != null) img.color = RowColor(i == _selected);
        }
    }

    static Color RowColor(bool sel) => sel
        ? new Color(UITheme.Accent.r, UITheme.Accent.g, UITheme.Accent.b, 0.95f)
        : new Color(UITheme.Cell.r, UITheme.Cell.g, UITheme.Cell.b, 0.9f);

    void Build()
    {
        var cgo = new GameObject("LootListUI_Canvas");
        cgo.transform.SetParent(transform, false);
        _canvas = cgo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        _root = new GameObject("Root", typeof(RectTransform));
        _root.transform.SetParent(cgo.transform, false);
        Stretch((RectTransform)_root.transform, 0, 0);

        // 화면 오른쪽 — 가운데를 가리지 않게(배그 루팅 목록처럼). 어둡게 덮지 않는다.
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_root.transform, false);
        _panel = (RectTransform)panel.transform;
        _panel.anchorMin = _panel.anchorMax = new Vector2(1f, 0.5f);
        _panel.pivot = new Vector2(1f, 0.5f);
        _panel.anchoredPosition = new Vector2(-80f, 0f);
        _panel.sizeDelta = new Vector2(PanelWidth, 200f);
        panel.GetComponent<Image>().color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, 0.96f);

        _header = MakeText(panel.transform, "Header", 17, FontStyle.Bold, UITheme.Gold, TextAnchor.MiddleCenter);
        var hRT = (RectTransform)_header.transform;
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1); hRT.pivot = new Vector2(0.5f, 1f);
        hRT.offsetMin = new Vector2(8, -34); hRT.offsetMax = new Vector2(-8, -6);

        var listGO = new GameObject("List", typeof(RectTransform));
        listGO.transform.SetParent(panel.transform, false);
        _list = (RectTransform)listGO.transform;
        // ⚠️ 가로로 늘인 앵커에서 sizeDelta.x는 **부모 폭에 더하는 값**이다(-16 = 좌우 8px 여백).
        //    offsetMin/Max를 위치 뒤에 설정하면 위쪽 가장자리가 부모 꼭대기로 돌아가 헤더를 덮는다(2026-09-11 실측).
        _list.anchorMin = new Vector2(0, 1); _list.anchorMax = new Vector2(1, 1); _list.pivot = new Vector2(0.5f, 1f);
        _list.sizeDelta = new Vector2(-16f, 0f);
        _list.anchoredPosition = new Vector2(0, -40);

        // 하단 버튼 — 다 드러난 뒤에만 보인다
        _takeAllBtn = MakeButton(panel.transform, "TakeAll", "전부 가져가기 [F]", new Vector2(0.02f, 0f), new Vector2(0.62f, 0f), () => TakeAll());
        _detailBtn  = MakeButton(panel.transform, "Detail", "자세히 [Tab]", new Vector2(0.64f, 0f), new Vector2(0.98f, 0f), OpenDetail);

        _hint = MakeText(panel.transform, "Hint", 12, FontStyle.Italic, UITheme.TextMuted, TextAnchor.MiddleCenter);
        var hintRT = (RectTransform)_hint.transform;
        hintRT.anchorMin = new Vector2(0, 0); hintRT.anchorMax = new Vector2(1, 0); hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.offsetMin = new Vector2(6, 4); hintRT.offsetMax = new Vector2(-6, 22);

        _root.SetActive(false);
    }

    GameObject MakeButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = new Vector2(aMax.x, 0f); rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(0, 26); rt.offsetMax = new Vector2(0, 60);
        go.GetComponent<Image>().color = new Color(UITheme.Accent.r, UITheme.Accent.g, UITheme.Accent.b, 0.85f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        var t = MakeText(go.transform, "Label", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        t.text = label;
        Stretch((RectTransform)t.transform, 0, 0);
        return go;
    }

    Text MakeText(Transform parent, string name, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Font LoadKoreanFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Arial" }, 18);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
