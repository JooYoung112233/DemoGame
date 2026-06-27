using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재사용 가능한 "격자 패널" — 인벤토리(CharacterPanelUI)와 동일한 슬롯+footprint 렌더 + 드래그.
/// 상점/창고/트레이 등 여러 격자를 같은 동작으로 다루기 위한 공용 컴포넌트.
/// 한 캔버스에 GridDragManager(싱글톤) 하나 + 여러 GridPanel을 등록해서 쓴다.
/// 드래그: 셀을 눌러 들고(고스트=픽셀 잡기 오프셋) 다른 패널 칸에 놓으면 onDrop 콜백.
/// 우클릭: onRightClick 콜백(컨테이너 열기 등).
/// </summary>
public class GridPanel
{
    public InventoryGrid grid;
    public RectTransform slotRoot;   // 빈 슬롯
    public RectTransform itemRoot;   // 아이템 오버레이
    public RectTransform hitRoot;    // 좌표 변환 기준(보통 slotRoot)

    public const int CELL = 48, GAP = 2;
    public static int CellTotal => CELL + GAP;

    /// <summary>이 패널에 item을 받을 수 있는지(카테고리 게이트 등). null=모두 허용.</summary>
    public System.Func<ItemInstance, bool> canAccept;
    /// <summary>다른 패널/자기 칸에 놓였을 때. (item, 출발패널, 목표x, 목표y) → 처리(소비)했으면 true.
    /// false면 매니저가 아이템을 출발지로 되돌린다(분실 방지).</summary>
    public System.Func<ItemInstance, GridPanel, int, int, bool> onDrop;
    /// <summary>셀 우클릭. (placed)</summary>
    public System.Action<InventoryGrid.PlacedItem> onRightClick;
    /// <summary>Ctrl+좌클릭(즉시 이동). (placed)</summary>
    public System.Action<InventoryGrid.PlacedItem> onCtrlClick;
    /// <summary>아이템 셀 배경색(희귀도 등). null이면 기본.</summary>
    public System.Func<ItemInstance, bool, Color> cellColor;

    Image[,] slots;

    public bool Accepts(ItemInstance item) => canAccept == null || canAccept(item);

    /// <summary>슬롯 + 아이템 다시 그림.</summary>
    public void Refresh()
    {
        if (slotRoot == null || itemRoot == null || grid == null) return;
        Clear(slotRoot); Clear(itemRoot);

        int w = grid.width, h = grid.height;
        slots = new Image[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var s = NewImg(slotRoot, $"S_{x}_{y}", UITheme.PanelAlt);
                UISkin.Cell(s);   // 시안: storage_box 빈 격자 셀(폴백: 색 유지)
                Place(s.rectTransform, x * CellTotal, -y * CellTotal, CELL, CELL);
                slots[x, y] = s;
            }

        // content 높이(스크롤)
        var content = slotRoot.parent as RectTransform;
        if (content != null) content.sizeDelta = new Vector2(w * CellTotal + 16, h * CellTotal + 16);

        foreach (var p in grid.GetAll())
            if (p?.item?.data != null) AddCell(p);
    }

    void AddCell(InventoryGrid.PlacedItem p)
    {
        int cw = p.EffectiveWidth, ch = p.EffectiveHeight;
        var cell = NewImg(itemRoot, $"I_{p.item.uid}",
            cellColor != null ? cellColor(p.item, false) : UITheme.RarityBg(p.item.data.rarity));
        Place(cell.rectTransform, p.gridX * CellTotal, -p.gridY * CellTotal,
              cw * CELL + (cw - 1) * GAP, ch * CELL + (ch - 1) * GAP);

        if (p.item.data.icon != null)
        {
            var ic = NewImg(cell.rectTransform, "Icon", Color.white);
            ic.sprite = p.item.data.icon; ic.preserveAspect = true;
            var irt = ic.rectTransform; irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(4, 4); irt.offsetMax = new Vector2(-4, -4);
        }
        else
        {
            var t = NewText(cell.rectTransform, p.item.data.displayName, 11);
            var trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(2, 2); trt.offsetMax = new Vector2(-2, -2);
        }

        // 내구도 바(있으면) / 스택 수 — 인벤과 동일
        if (p.item.HasDurability)
        {
            float ratio = p.item.DurabilityRatio;
            var durBg = NewImg(cell.rectTransform, "DurBg", new Color(0, 0, 0, 0.6f));
            var dbrt = durBg.rectTransform;
            dbrt.anchorMin = new Vector2(0, 0); dbrt.anchorMax = new Vector2(1, 0); dbrt.pivot = new Vector2(0, 0);
            dbrt.anchoredPosition = new Vector2(2, 2); dbrt.sizeDelta = new Vector2(-4, 6);
            var durFill = NewImg(durBg.rectTransform, "DurFill",
                ratio > 0.5f ? new Color(0.3f, 0.9f, 0.4f) : ratio > 0.2f ? new Color(0.9f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f));
            var dfrt = durFill.rectTransform;
            dfrt.anchorMin = Vector2.zero; dfrt.anchorMax = new Vector2(ratio, 1f);
            dfrt.offsetMin = Vector2.zero; dfrt.offsetMax = Vector2.zero;
        }
        else if (p.item.stackCount > 1)
        {
            var c = NewText(cell.rectTransform, $"x{p.item.stackCount}", 11);
            c.alignment = TextAnchor.LowerRight; c.fontStyle = FontStyle.Bold;
            c.gameObject.AddComponent<Shadow>().effectColor = Color.black;
            var crt = c.rectTransform; crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(0, 0); crt.offsetMax = new Vector2(-3, -2);
        }

        // 점유 칸 색 → 그리드선(아이템 뒤로 비치게)
        if (slots != null)
            for (int gx = p.gridX; gx < p.gridX + cw && gx < grid.width; gx++)
                for (int gy = p.gridY; gy < p.gridY + ch && gy < grid.height; gy++)
                    if (gx >= 0 && gy >= 0 && slots[gx, gy] != null) slots[gx, gy].color = UITheme.Gridline;
    }

    /// <summary>마우스가 이 패널 격자의 어느 칸인지.</summary>
    public bool CellAtMouse(out int gx, out int gy)
    {
        gx = gy = -1;
        var root = hitRoot != null ? hitRoot : slotRoot;
        if (root == null || grid == null || !root.gameObject.activeInHierarchy) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, GameInput.mousePosition, null, out var lp)) return false;
        gx = Mathf.FloorToInt(lp.x / CellTotal);
        gy = Mathf.FloorToInt(-lp.y / CellTotal);
        return gx >= 0 && gx < grid.width && gy >= 0 && gy < grid.height;
    }

    // ── 소형 uGUI 헬퍼 ──
    static void Clear(RectTransform rt) { for (int i = rt.childCount - 1; i >= 0; i--) Object.Destroy(rt.GetChild(i).gameObject); }
    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
    }
    static Image NewImg(Transform parent, string name, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.color = c; return img;
    }
    static Text NewText(Transform parent, string s, int size)
    {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.raycastTarget = false;
        t.text = s;   // ← 누락돼 있던 줄: 이름/스택 수가 안 보이던 원인
        return t;
    }
}

/// <summary>
/// 등록된 GridPanel들 사이의 드래그를 조율(픽셀 잡기 오프셋 고스트 → 칸 스냅 → onDrop).
/// 한 UI 패널당 1개 생성(예: ShopUI가 자기 캔버스에 붙임). GameInput 사용.
/// </summary>
public class GridDragManager : MonoBehaviour
{
    readonly List<GridPanel> panels = new List<GridPanel>();
    RectTransform canvasRT;
    bool active;

    GridPanel srcPanel;
    InventoryGrid.PlacedItem dragPlaced;
    bool dragRotated;
    Vector2 grabPixelOffset;
    GameObject ghost; RectTransform ghostRT;
    GameObject highlightGO; RectTransform highlightRT; Image highlightImage;   // 배치 미리보기(초록/빨강)

    public bool IsDragging => active;
    public bool suspended;            // true면 픽업/우클릭 무시(컨텍스트 메뉴 등 떠 있을 때)
    public System.Action onChanged;   // 드롭/이동 후 호출(소비측 갱신)

    public void Init(RectTransform canvas) { canvasRT = canvas; }
    public void Register(GridPanel p) { if (p != null && !panels.Contains(p)) panels.Add(p); }
    public void Clear() { panels.Clear(); CancelDrag(); }

    void Update()
    {
        if (panels.Count == 0) return;
        if (suspended && !active) return;   // 메뉴 떠 있을 땐 픽업 금지(진행 중 드래그는 계속)

        if (active)
        {
            UpdateGhost();
            UpdateHighlight();
            if (GameInput.GetMouseButtonDown(1)) { CancelDrag(); return; }
            if (GameInput.GetMouseButtonUp(0)) Drop();
            return;
        }

        if (GameInput.GetMouseButtonDown(0))
        {
            if (GameInput.GetKey(KeyCode.LeftControl) || GameInput.GetKey(KeyCode.RightControl)) TryCtrlClick();
            else TryPickup();
        }
        else if (GameInput.GetMouseButtonDown(1)) TryRightClick();
    }

    void TryCtrlClick()
    {
        foreach (var p in panels)
        {
            if (!p.CellAtMouse(out int gx, out int gy)) continue;
            var placed = p.grid.GetAt(gx, gy);
            if (placed != null) p.onCtrlClick?.Invoke(placed);
            return;
        }
    }

    void TryPickup()
    {
        foreach (var p in panels)
        {
            if (!p.CellAtMouse(out int gx, out int gy)) continue;
            var placed = p.grid.GetAt(gx, gy);
            if (placed == null) return;   // 빈 칸
            BeginDrag(p, placed, gx, gy);
            return;
        }
    }

    void TryRightClick()
    {
        foreach (var p in panels)
        {
            if (!p.CellAtMouse(out int gx, out int gy)) continue;
            var placed = p.grid.GetAt(gx, gy);
            if (placed != null) p.onRightClick?.Invoke(placed);
            return;
        }
    }

    void BeginDrag(GridPanel p, InventoryGrid.PlacedItem placed, int gx, int gy)
    {
        active = true; srcPanel = p; dragPlaced = placed; dragRotated = placed.rotated;

        var root = p.hitRoot != null ? p.hitRoot : p.slotRoot;
        grabPixelOffset = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, GameInput.mousePosition, null, out var lp))
            grabPixelOffset = lp - new Vector2(placed.gridX * GridPanel.CellTotal, -placed.gridY * GridPanel.CellTotal);

        p.grid.Remove(placed);
        CreateGhost(placed.item);
        p.Refresh();
        onChanged?.Invoke();
    }

    void Drop()
    {
        var item = dragPlaced.item;
        // 마우스 아래 패널 찾기
        foreach (var p in panels)
        {
            if (!p.CellAtMouse(out int gx, out int gy)) continue;
            if (!p.Accepts(item)) { CancelDrag(); return; }
            int ox = OriginCell(p, true), oy = OriginCell(p, false);
            bool consumed = p.onDrop != null && p.onDrop(item, srcPanel, ox, oy);
            EndGhost();
            active = false;
            if (!consumed && srcPanel != null)   // 처리 안 됐으면 출발지로 복귀
            {
                if (!srcPanel.grid.TryPlace(item, dragPlaced.gridX, dragPlaced.gridY, dragRotated))
                    srcPanel.grid.TryAutoPlace(item);
                srcPanel.Refresh();
            }
            srcPanel = null; dragPlaced = null;
            onChanged?.Invoke();
            return;
        }
        CancelDrag();   // 격자 밖 → 원위치
    }

    int OriginCell(GridPanel p, bool xAxis)
    {
        var root = p.hitRoot != null ? p.hitRoot : p.slotRoot;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, GameInput.mousePosition, null, out var lp)) return 0;
        var tl = lp - grabPixelOffset;
        return xAxis ? Mathf.RoundToInt(tl.x / GridPanel.CellTotal) : Mathf.RoundToInt(-tl.y / GridPanel.CellTotal);
    }

    public void CancelDrag()
    {
        if (active && srcPanel != null && dragPlaced != null)
        {
            if (!srcPanel.grid.TryPlace(dragPlaced.item, dragPlaced.gridX, dragPlaced.gridY, dragRotated))
                srcPanel.grid.TryAutoPlace(dragPlaced.item);
            srcPanel.Refresh();
        }
        EndGhost();
        active = false; srcPanel = null; dragPlaced = null;
        onChanged?.Invoke();
    }

    // ── 고스트 ──
    void CreateGhost(ItemInstance item)
    {
        EndGhost();
        if (canvasRT == null || item?.data == null) return;
        ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        ghost.transform.SetParent(canvasRT, false);
        ghostRT = ghost.GetComponent<RectTransform>();
        ghostRT.anchorMin = ghostRT.anchorMax = new Vector2(0.5f, 0.5f); ghostRT.pivot = new Vector2(0, 1);
        int w = dragRotated ? item.data.gridHeight : item.data.gridWidth;
        int h = dragRotated ? item.data.gridWidth : item.data.gridHeight;
        ghostRT.sizeDelta = new Vector2(w * GridPanel.CELL + (w - 1) * GridPanel.GAP, h * GridPanel.CELL + (h - 1) * GridPanel.GAP);
        var img = ghost.GetComponent<Image>();
        var c = item.data.RarityColor; img.color = new Color(c.r, c.g, c.b, 0.7f); img.raycastTarget = false;
        if (item.data.icon != null)
        {
            var ico = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            ico.transform.SetParent(ghost.transform, false);
            var irt = ico.GetComponent<RectTransform>(); irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = new Vector2(3, 3); irt.offsetMax = new Vector2(-3, -3);
            var im = ico.GetComponent<Image>(); im.sprite = item.data.icon; im.preserveAspect = true; im.raycastTarget = false;
        }
        UpdateGhost();
    }
    void UpdateGhost()
    {
        if (ghost == null || canvasRT == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, GameInput.mousePosition, null, out var lp))
            ghostRT.anchoredPosition = lp - grabPixelOffset;
    }
    void EndGhost()
    {
        if (ghost != null) { Destroy(ghost); ghost = null; ghostRT = null; }
        if (highlightGO != null) highlightGO.SetActive(false);
    }

    // ── 배치 미리보기 하이라이트(인벤과 동일: 배치가능=초록 / 불가=빨강) ──
    void UpdateHighlight()
    {
        GridPanel hover = null; int ox = 0, oy = 0;
        foreach (var p in panels)
        {
            if (!p.CellAtMouse(out int gx, out int gy)) continue;
            hover = p; ox = OriginCell(p, true); oy = OriginCell(p, false); break;
        }
        if (hover == null || dragPlaced == null)
        {
            if (highlightGO != null) highlightGO.SetActive(false);
            return;
        }

        var item = dragPlaced.item;
        EnsureHighlight(hover);
        highlightGO.SetActive(true);

        int w = dragRotated ? item.data.gridHeight : item.data.gridWidth;
        int h = dragRotated ? item.data.gridWidth : item.data.gridHeight;
        bool canPlace = hover.Accepts(item) && hover.grid.CanPlace(item, ox, oy, dragRotated);

        int ct = GridPanel.CellTotal;
        highlightRT.anchoredPosition = new Vector2(ox * ct, -oy * ct);
        highlightRT.sizeDelta = new Vector2(w * GridPanel.CELL + (w - 1) * GridPanel.GAP,
                                            h * GridPanel.CELL + (h - 1) * GridPanel.GAP);
        highlightImage.color = canPlace ? new Color(0.2f, 0.8f, 0.3f, 0.35f) : new Color(0.9f, 0.2f, 0.2f, 0.35f);
    }

    void EnsureHighlight(GridPanel p)
    {
        var root = p.itemRoot != null ? p.itemRoot : p.slotRoot;
        if (highlightGO == null)
        {
            highlightGO = new GameObject("DragHighlight", typeof(RectTransform), typeof(Image));
            highlightRT = highlightGO.GetComponent<RectTransform>();
            highlightRT.anchorMin = new Vector2(0, 1); highlightRT.anchorMax = new Vector2(0, 1); highlightRT.pivot = new Vector2(0, 1);
            highlightImage = highlightGO.GetComponent<Image>();
            highlightImage.raycastTarget = false;
        }
        if (highlightGO.transform.parent != root) highlightGO.transform.SetParent(root, false);
        highlightGO.transform.SetAsLastSibling();
    }
}
