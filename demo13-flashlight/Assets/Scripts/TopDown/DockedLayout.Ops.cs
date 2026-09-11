using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재배치 **실행부** — 껍데기 통과, 제목 제거·끌어올리기, 다단→단일 컬럼 쌓기,
/// 넘치는 세로를 축소 또는 스크롤로 처리.
///
/// 규칙(무엇을 감출지)은 Rules에, 여기서는 그 규칙을 화면에 적용하는 방법만 다룬다.
/// </summary>
public static partial class DockedLayout
{
    /// <summary>제목 줄을 감추고 **그 아래 것들을 그만큼 끌어올린다.**
    ///
    /// 감추기만 하면 위쪽에 빈 띠만 남아 내용이 좁은 채로 그대로다. 제목 띠가 있던 자리를
    /// 아래 내용이 이어받도록, 띠 바로 밑 요소가 띠의 맨 위로 올라오는 만큼 전부 옮긴다.
    /// 세로로 늘어나는 요소(목록·본문)는 옮기는 대신 **위로 자라게** 한다.</summary>
    static void StripHeader(RectTransform node, string[] names, Session s)
    {
        if (names == null || names.Length == 0) return;

        // ① 감출 제목 띠를 찾는다 (바로 아래 자식만).
        float bandTop = float.NegativeInfinity, bandBottom = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < node.childCount; i++)
        {
            var c = node.GetChild(i) as RectTransform;
            if (c == null || !c.gameObject.activeSelf) continue;
            if (System.Array.IndexOf(names, c.name) < 0) continue;

            bandTop    = Mathf.Max(bandTop,    c.offsetMax.y);
            bandBottom = Mathf.Min(bandBottom, c.offsetMin.y);
            s.Record(c);
            c.gameObject.SetActive(false);
            found = true;
        }
        if (!found) return;

        // ② 띠보다 완전히 아래에 있는 것들 중 가장 위 = 이어받을 요소.
        //    (비활성 형제도 포함한다 — 탭 전환으로 나중에 켜지는 패널이 어긋나면 안 된다)
        float nextTop = float.NegativeInfinity;
        for (int i = 0; i < node.childCount; i++)
        {
            var c = node.GetChild(i) as RectTransform;
            if (c == null || System.Array.IndexOf(names, c.name) >= 0) continue;
            if (c.offsetMax.y > bandBottom + 0.5f) continue;      // 띠와 겹치거나 위 → 건드리지 않는다
            nextTop = Mathf.Max(nextTop, c.offsetMax.y);
        }
        if (float.IsNegativeInfinity(nextTop)) return;

        float lift = bandTop - nextTop;
        if (lift <= 0.5f) return;

        // ③ 실제로 끌어올린다.
        for (int i = 0; i < node.childCount; i++)
        {
            var c = node.GetChild(i) as RectTransform;
            if (c == null || System.Array.IndexOf(names, c.name) >= 0) continue;
            if (c.offsetMax.y > bandBottom + 0.5f) continue;

            s.Record(c);
            bool stretchesY = c.anchorMax.y - c.anchorMin.y > 0.01f;
            if (stretchesY)
                c.offsetMax = new Vector2(c.offsetMax.x, c.offsetMax.y + lift);   // 위로 자란다
            else
                c.offsetMin = new Vector2(c.offsetMin.x, c.offsetMin.y + lift);   // 통째로 올라간다
            if (!stretchesY)
                c.offsetMax = new Vector2(c.offsetMax.x, c.offsetMax.y + lift);
        }
    }

    /// <summary>전체화면 껍데기를 지나 진짜 내용까지 내려간다.
    /// 자식이 하나뿐이고 그 자식이 눈에 띄게 작으면 껍데기로 본다.</summary>
    static RectTransform Descend(RectTransform node, float areaW, Session s)
    {
        for (int guard = 0; guard < 4; guard++)
        {
            if (node.rect.width <= areaW + 0.5f) break;    // 이미 들어간다

            RectTransform only = null;
            int count = 0;
            foreach (var c in ActiveKids(node)) { count++; only = c; }

            if (count != 1 || only.rect.width >= node.rect.width * 0.95f) break;

            // ⚠️ 지나친 껍데기는 **계속 켜져 있다**(UI가 스스로 켜고 끄는 노드라서).
            // 그 위에 전체화면 암막 Image가 붙어 있으면 방과 캐릭터가 통째로 어두워지고,
            // 투명하더라도 raycastTarget이 남아 소품 클릭을 먹는다. 그림을 꺼둔다.
            MuteGraphics(node, s);
            node = only;
        }
        return node;
    }

    /// <summary>이 노드 자신에게 붙은 그림(배경 Image 등)만 끈다. 자식은 건드리지 않는다.</summary>
    static void MuteGraphics(RectTransform node, Session s)
    {
        foreach (var g in node.GetComponents<Graphic>())
        {
            if (!g.enabled) continue;
            s.RecordGraphic(g);
            g.enabled = false;
        }
    }

    /// <summary><paramref name="node"/>를 <paramref name="areaW"/> 폭에 앉힌다.
    /// 반환값은 그때 차지하는 높이(축소했다면 축소 후 높이).</summary>
    /// <param name="nativeW">부모가 폭을 건드리기 **전**의 원래 폭. 자식이 stretch 앵커면
    /// 부모를 줄인 뒤 읽으면 이미 줄어들어 있어서, 넓었다는 사실을 놓친다.</param>
    static float Fit(RectTransform node, float nativeW, float areaW, Session s, int depth)
    {
        if (nativeW <= areaW + 0.5f) return node.rect.height;   // 들어간다 → 손대지 않는다

        var kids = ActiveKids(node).ToList();

        // 더 나눌 자식이 없거나 너무 깊다 → 통째로 균일 축소.
        // 고정 크기 인벤토리 그리드처럼 재배치로는 안 줄어드는 것들이 여기로 온다.
        if (kids.Count == 0 || depth >= MaxDepth)
        {
            s.Record(node);
            float k = Mathf.Clamp(areaW / nativeW, MinScale, 1f);
            node.localScale = new Vector3(k, k, 1f);
            return node.rect.height * k;
        }

        // 화면에 놓인 좌→우 순서로 쌓아야 읽는 순서가 유지된다(계층 순서와 다를 수 있다).
        // 같은 열(세로로 이미 쌓여 있는 것)은 x가 비슷하므로 계층 순서가 그대로 남는다.
        var ordered = kids.OrderBy(k => Mathf.Round(k.position.x / 40f)).ToList();

        // ⚠️ 부모 폭을 바꾸기 전에 자식들의 원래 크기를 읽어둔다.
        var natW = ordered.Select(k => k.rect.width).ToArray();
        var natH = ordered.Select(k => k.rect.height).ToArray();

        s.Record(node);
        SetBox(node, areaW, node.rect.height);

        float y = 0f;
        for (int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            s.Record(c);

            StretchTop(c, y, natH[i]);                        // 가로 꽉, 세로는 원래 높이
            float h = Fit(c, natW[i], areaW, s, depth + 1);   // 원래 넓던 자식만 다시 손본다
            if (!Mathf.Approximately(h, natH[i])) StretchTop(c, y, h);

            y += h + Gap;
        }

        float total = Mathf.Max(0f, y - Gap);
        SetBox(node, areaW, total);
        return total;
    }

    /// <summary>세로 넘침 처리.
    /// 들어가면 그대로 두고(도크 쪽에서 <b>꽉 채워</b> 늘린다), 넘치면 스크롤로 감싼다.
    ///
    /// ⚠️ 예전엔 "조금 넘치면 통째로 축소"를 했는데, 창고(1.51배)·파견(1.58배)이 여기 걸려
    ///    0.63~0.66배까지 줄어 글씨를 못 읽었다. 줄이는 대신 스크롤이 낫다.</summary>
    static RectTransform WrapIfTall(RectTransform node, Vector2 area, Session s)
    {
        float contentH = node.rect.height;
        float contentW = node.rect.width;
        if (contentH <= area.y + 0.5f) return node;
        // 조금 넘치는 정도면 스크롤 대신 **살짝 줄여** 한 화면에 담는다.
        // 창고 격자는 928인데 도크가 856이라 8%만 넘친다 — 그만큼 때문에 스크롤을 붙이면
        // 격자 아래 한 줄 보자고 스크롤해야 해서 오히려 불편하다.
        if (contentH <= area.y * SquashLimit)
        {
            s.Record(node);
            float k = area.y / contentH;
            node.localScale = new Vector3(k, k, 1f);
            s.KeepsOwnSize = true;            // 줄여 놨으니 도크가 다시 늘리면 안 된다
            return node;
        }


        var scrollGO = new GameObject("DockScroll", typeof(RectTransform), typeof(ScrollRect));
        s.Created(scrollGO);
        var scroll = (RectTransform)scrollGO.transform;

        var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        var vp = (RectTransform)vpGO.transform;
        vp.SetParent(scroll, false);
        vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one;
        vp.offsetMin = new Vector2(0f, 0f);
        vp.offsetMax = new Vector2(-14f, 0f);          // 오른쪽에 스크롤바 자리

        node.SetParent(vp, false);
        node.anchorMin = new Vector2(0f, 1f);
        node.anchorMax = new Vector2(1f, 1f);
        node.pivot     = new Vector2(0.5f, 1f);
        node.offsetMin = new Vector2(0f, -contentH);
        node.offsetMax = Vector2.zero;

        var bar = MakeScrollbar(scroll);

        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.viewport = vp;
        sr.content  = node;
        sr.horizontal = false;
        sr.vertical   = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;
        sr.verticalScrollbar = bar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        s.Scrolled = true;
        s.ContentWidth = contentW;
        return scroll;
    }

    static Scrollbar MakeScrollbar(RectTransform parent)
    {
        var go = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(-10f, 0f);
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handle = (RectTransform)handleGO.transform;
        handle.SetParent(rt, false);
        handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one;
        handle.offsetMin = Vector2.zero; handle.offsetMax = Vector2.zero;
        handleGO.GetComponent<Image>().color = new Color(0.86f, 0.82f, 0.60f, 0.55f);

        var bar = go.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;
        bar.handleRect = handle;
        bar.targetGraphic = handleGO.GetComponent<Image>();
        return bar;
    }

    // ─────────────────────────────────────────────────────────────
    // 도우미
    // ─────────────────────────────────────────────────────────────

    static IEnumerable<RectTransform> ActiveKids(RectTransform node)
    {
        for (int i = 0; i < node.childCount; i++)
        {
            var c = node.GetChild(i) as RectTransform;
            if (c != null && c.gameObject.activeSelf) yield return c;
        }
    }

    /// <summary>이름이 같은 자손을 감춘다(깊이 무관, 여러 개면 전부).</summary>
    static void HideByName(RectTransform root, string name, Session s)
    {
        foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (t.name != name) continue;
            s.Record(t);
            t.gameObject.SetActive(false);
        }
    }

    /// <summary>좌상단 고정 앵커 + 지정 크기. 부모가 이후에 다시 앉히더라도
    /// 이 시점의 폭이 자식 stretch 계산의 기준이 된다.</summary>
    static void SetBox(RectTransform rt, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(0f, -h);
        rt.offsetMax = new Vector2(w, 0f);
    }

    /// <summary>부모 폭을 꽉 채우고, 위에서 <paramref name="y"/>만큼 내려 높이 <paramref name="h"/>로 앉힌다.</summary>
    static void StretchTop(RectTransform rt, float y, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -(y + h));
        rt.offsetMax = new Vector2(0f, -y);
    }
}
