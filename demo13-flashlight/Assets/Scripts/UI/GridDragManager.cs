using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


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
    Vector2 grabPixelOffset;
    GameObject ghost; RectTransform ghostRT;
    GameObject highlightGO; RectTransform highlightRT; Image highlightImage;   // 배치 미리보기(초록/빨강)

    public bool IsDragging => active;
    public bool suspended;            // true면 픽업/우클릭 무시(컨텍스트 메뉴 등 떠 있을 때)
    public System.Action onChanged;   // 드롭/이동 후 호출(소비측 갱신)

    public void Init(RectTransform canvas) { canvasRT = canvas; }
    public void Register(GridPanel p) { if (p != null && !panels.Contains(p)) panels.Add(p); }
    public void Clear() { panels.Clear(); CancelDrag(); }

    /// <summary>마우스 아래 패널을 휠로 스크롤(EventSystem 비의존, 직접 폴링).</summary>
    void PollWheel()
    {
        float wheelY = GameInput.mouseScrollDelta.y;
        if (Mathf.Abs(wheelY) <= 0.01f) return;
        for (int i = 0; i < panels.Count; i++)
            if (panels[i].scrollRect != null && panels[i].CellAtMouse(out _, out _))
            { WheelOnlyScrollRect.WheelStep(panels[i].scrollRect, wheelY); break; }
    }

    void Update()
    {
        if (panels.Count == 0) return;

        // 휠은 EventSystem(InputSystem 액션) 전달에 의존하지 않고 직접 폴링한다(드래그/비드래그 모두).
        // EventSystem 휠은 중복/오동작 방지로 항상 끔(sensitivity=0). 컨텍스트 메뉴(suspended·비드래그) 땐 휠 무시.
        for (int i = 0; i < panels.Count; i++)
            if (panels[i].scrollRect != null)
                panels[i].scrollRect.scrollSensitivity = 0f;
        if (!suspended || active)
            PollWheel();

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
        active = true; srcPanel = p; dragPlaced = placed;

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

            // 같은 격자 안에서 다른 아이템 위에 떨굼 → 스택/스왑(부분 중첩 허용). 빈 칸이면 onDrop이 배치.
            bool consumed;
            if (p == srcPanel && TryInternalSwap(p, item, ox, oy, out bool swapped))
            {
                consumed = swapped;
                if (swapped) p.Refresh();
            }
            else
            {
                consumed = p.onDrop != null && p.onDrop(item, srcPanel, ox, oy);
            }
            EndGhost();
            active = false;
            if (!consumed && srcPanel != null)   // 처리 안 됐으면 출발지로 복귀
            {
                if (!srcPanel.grid.TryPlace(item, dragPlaced.gridX, dragPlaced.gridY))
                    srcPanel.grid.TryAutoPlace(item);
                srcPanel.Refresh();
            }
            srcPanel = null; dragPlaced = null;
            onChanged?.Invoke();
            return;
        }
        CancelDrag();   // 격자 밖 → 원위치
    }

    /// <summary>같은 격자 안에서 다른 아이템 위에 떨굼 → 스택 또는 A↔B 1:1 스왑.
    /// 반환 true = 이 메서드가 드롭을 책임짐(스왑/스택 완료 또는 실패→호출부가 출발지로 bounce).
    /// 반환 false = 빈 칸 등 → 호출부가 onDrop으로 처리.
    /// 드래그 footprint와 겹치는 단일 아이템을 타겟으로 잡으므로 세로/가로 부분 중첩도 스왑됨.</summary>
    bool TryInternalSwap(GridPanel p, ItemInstance item, int ox, int oy, out bool placed)
    {
        placed = false;
        var g = p.grid;
        if (g == null || item?.data == null) return false;
        if (g.CanPlace(item, ox, oy)) return false;   // 빈 칸 → onDrop이 배치

        const int w = 1, h = 1;   // 슬롯 1칸 고정(2026-09-09 격자 폐기)

        // footprint와 겹치는 '단일' 아이템 검출(부분 중첩 허용)
        InventoryGrid.PlacedItem target = null;
        for (int gx = ox; gx < ox + w; gx++)
            for (int gy = oy; gy < oy + h; gy++)
            {
                var pp = g.GetAt(gx, gy);
                if (pp == null) continue;
                if (target == null) target = pp;
                else if (pp != target) return false;   // 둘 이상 겹침 → onDrop(보통 실패→bounce)
            }
        if (target == null) return false;

        // 같은 아이템이면 스택 우선
        if (target.item != null && target.item.CanStackWith(item))
        {
            int remaining = target.item.TryStack(item);
            g.NotifyChanged();
            placed = remaining <= 0;   // 완전 흡수=소비, 일부면 나머지 bounce
            return true;
        }

        // 1:1 스왑: A는 B 자리(oldX,oldY)에, B는 A 출발지(dragPlaced)에. 둘 다 맞을 때만(아니면 원복).
        var oldItem = target.item;
        int oldX = target.gridX, oldY = target.gridY;
        g.Remove(target);

        bool aFits = p.Accepts(item) && g.CanPlace(item, oldX, oldY);
        bool bFits = g.CanPlace(oldItem, dragPlaced.gridX, dragPlaced.gridY);
        if (aFits && bFits)
        {
            g.TryPlace(item, oldX, oldY);
            g.TryPlace(oldItem, dragPlaced.gridX, dragPlaced.gridY);
            placed = true;
            return true;
        }

        g.TryPlace(oldItem, oldX, oldY);   // 스왑 불가 → B 원복, A는 호출부가 출발지로 bounce
        placed = false;
        return true;
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
            if (!srcPanel.grid.TryPlace(dragPlaced.item, dragPlaced.gridX, dragPlaced.gridY))
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
        const int w = 1, h = 1;   // 슬롯 1칸 고정(2026-09-09 격자 폐기)
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

        const int w = 1, h = 1;   // 슬롯 1칸 고정(2026-09-09 격자 폐기)
        bool canPlace = hover.Accepts(item) && hover.grid.CanPlace(item, ox, oy);

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
