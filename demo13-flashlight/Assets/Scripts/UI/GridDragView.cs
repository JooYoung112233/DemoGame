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

    public const int CELL = 72, GAP = 2;   // 인벤(CharacterPanelUI.CELL_SIZE)과 동일 — 가로 7칸 통일
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

    /// <summary>이 패널의 세로 스크롤(있으면). 드래그 중 휠 폴링·속도 제어용. 없으면 null.</summary>
    public ScrollRect scrollRect;

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
