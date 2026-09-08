using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전체화면 전제로 만들어진 시설 UI를 **좁은 도크 폭에 맞게 재배치**한다.
///
/// 이 프로젝트 UI는 전부 1920×1080 기준이라 도크에 그냥 넣으면 가로로 터진다.
/// 실측(2026-09-08):
/// <list type="bullet">
/// <item>작업대 `CraftRoot` 1920 → 진짜 내용은 `CenterPanel` 520×480 (껍데기만 전체화면)</item>
/// <item>침대 `Panel` 1920 → `Window` 720×460</item>
/// <item>파견 `Body` 1840 = `LeftPanel` 360(조작부) + `MapArea` 1464(지도)</item>
/// <item>라디오 `Root` 1920 = Header + TuneBar + Body(LeftPanel 540 + RightPanel 1276)</item>
/// <item>창고 `PanelRoot` 1920 = 643 + 556 + 556 3열</item>
/// </list>
///
/// 그래서 네 단계로 손본다.
/// <list type="number">
/// <item><b>내려가기</b> — 전체화면 껍데기를 지나 진짜 내용까지. 작업대·침대는 이것만으로 끝난다.</item>
/// <item><b>덜어내기</b> — 도크에서 자리만 잡아먹는 것(파견 지도, 패널 자체 닫기 버튼)을 감춘다.</item>
/// <item><b>세로로 쌓기</b> — 도크보다 넓은 노드는 자식을 한 줄로 쌓는다(웹의 다단→단일 컬럼과 같다).
///       그래도 안 줄어드는 것(고정 크기 그리드 등)만 마지막에 균일 축소한다.</item>
/// <item><b>스크롤</b> — 쌓고 나서 세로로 넘치면 스크롤로 감싼다.</item>
/// </list>
///
/// ⚠️ <b>남의 패널을 건드리므로 바꾼 것은 전부 되돌려야 한다.</b> 안 되돌리면 은신처 밖
/// (레이드 중 인벤토리 등)에서 그 UI가 찌그러진 채로 뜬다. <see cref="Session"/>이 바꾼 값을
/// 들고 있다가 <see cref="Session.Restore"/>에서 원래대로 돌린다.
///
/// 설계: docs/hideout-3d.md
/// </summary>
public static class DockedLayout
{
    /// <summary>쌓을 때 항목 사이 간격(px, 1920 기준).</summary>
    const float Gap = 12f;

    /// <summary>줄일 때의 하한. 이보다 작아지면 글씨를 못 읽는다 — 그럴 바엔 스크롤이 낫다.</summary>
    const float MinScale = 0.72f;

    /// <summary>이 배수까지는 스크롤 대신 살짝 줄여 한 화면에 담는다(하한 축소율 ≈ 0.87).</summary>
    const float SquashLimit = 1.15f;


    /// <summary>쌓기 재귀 깊이 한계. 이 아래로는 축소로 처리한다.</summary>
    const int MaxDepth = 3;

    // ─────────────────────────────────────────────────────────────
    // 되돌리기
    // ─────────────────────────────────────────────────────────────

    /// <summary>한 번의 입양 동안 건드린 것 전부. <see cref="Restore"/>로 원상복구한다.</summary>
    public class Session
    {
        struct Saved
        {
            public RectTransform rt;
            public Transform parent;
            public int sibling;
            public Vector2 aMin, aMax, pivot, offMin, offMax;
            public Vector3 scale;
            public bool active;
        }

        readonly List<Saved> _saved = new List<Saved>();
        readonly List<GameObject> _created = new List<GameObject>();

        /// <summary>UI가 스스로 켜고 끄는 노드. 도크를 닫을 때 이걸 꺼야 UI가 닫힌 것이 된다.</summary>
        public RectTransform Root { get; internal set; }

        /// <summary>도크 안에 실제로 넣을 노드.</summary>
        public RectTransform Placed { get; internal set; }

        /// <summary>스크롤로 감쌌는가 — 그렇다면 도크 영역을 꽉 채워야 한다.</summary>
        public bool Scrolled { get; internal set; }

        /// <summary>스크롤 안 내용의 폭. 도크가 옆으로 넓어도 이 폭까지만 차지한다.</summary>
        public float ContentWidth { get; internal set; }

        /// <summary>도크가 늘리지 말고 **제 크기·제 배율 그대로** 가운데 두어야 하는가.
        /// 가로 유지형(창고)은 이미 균일 축소로 맞춰 놨으므로 다시 늘리면 배치가 깨진다.</summary>
        public bool KeepsOwnSize { get; internal set; }

        internal void Record(RectTransform rt)
        {
            if (rt == null) return;
            for (int i = 0; i < _saved.Count; i++)
                if (_saved[i].rt == rt) return;          // 처음 상태만 남긴다

            _saved.Add(new Saved
            {
                rt = rt, parent = rt.parent, sibling = rt.GetSiblingIndex(),
                aMin = rt.anchorMin, aMax = rt.anchorMax, pivot = rt.pivot,
                offMin = rt.offsetMin, offMax = rt.offsetMax,
                scale = rt.localScale, active = rt.gameObject.activeSelf,
            });
        }

        internal void Created(GameObject go) { if (go != null) _created.Add(go); }

        readonly List<Graphic> _muted = new List<Graphic>();

        /// <summary>지나친 껍데기의 배경 그림. 끈 것만 기억했다가 되돌린다.</summary>
        internal void RecordGraphic(Graphic g) { if (g != null) _muted.Add(g); }

        public void Restore()
        {
            // 나중에 건드린 것부터 되돌린다 — 부모를 되돌리기 전에 자식을 먼저 빼야 하는 경우가 있다.
            for (int i = _saved.Count - 1; i >= 0; i--)
            {
                var s = _saved[i];
                if (s.rt == null) continue;

                if (s.rt.parent != s.parent && s.parent != null)
                {
                    s.rt.SetParent(s.parent, false);
                    s.rt.SetSiblingIndex(s.sibling);
                }
                s.rt.anchorMin = s.aMin;
                s.rt.anchorMax = s.aMax;
                s.rt.pivot     = s.pivot;
                // offsetMin/Max는 앵커가 무엇이든 위치·크기를 함께 결정한다 — 이것만 되돌리면 된다.
                s.rt.offsetMin = s.offMin;
                s.rt.offsetMax = s.offMax;
                s.rt.localScale = s.scale;
                s.rt.gameObject.SetActive(s.active);
            }
            _saved.Clear();

            foreach (var go in _created)
                if (go != null) Object.Destroy(go);
            _created.Clear();

            foreach (var g in _muted) if (g != null) g.enabled = true;
            _muted.Clear();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 시설별 규칙
    // ─────────────────────────────────────────────────────────────

    /// <summary>도크에서 감출 자식 이름들.
    /// 지도처럼 넓기만 한 것, 도크의 '닫기'와 겹치는 패널 자체 닫기 버튼 등.</summary>
    static string[] HideFor(string moduleKey) => moduleKey switch
    {
        // 지도는 폭 1464를 먹는데, 실제 조작(인력·목적지·보내기)은 전부 왼쪽 폼(360)에 있다.
        "dispatch" => new[] { "MapArea" },
        // 도크에 이미 '닫기'가 있다. 패널 자체 닫기는 중복이고 도크 밖으로 삐져나간다.
        "radio"    => new[] { "Close" },
        // 창고는 말 그대로 창고 격자만 있으면 된다. 캐릭터 상태·장비/소지품은
        // Tab 인벤토리에서 보는 것이고, 여기 같이 띄우면 정작 창고가 좁아진다.
        "stash"    => new[] { "CloseBtn", "CharacterPanel", "RightPanel" },
        "bed"      => new[] { "Close" },
        // 제작창 자체 닫기(X)도 도크의 X와 겹친다.
        "workbench" or "cooking" or "medical" => new[] { "CloseBtn", "Close" },
        _          => null,
    };


    // ─────────────────────────────────────────────────────────────
    // 본체
    // ─────────────────────────────────────────────────────────────

    /// <summary><paramref name="root"/>(방금 켜진 패널)을 <paramref name="area"/> 크기의
    /// 도크에 들어가도록 재배치한다. 결과는 <see cref="Session.Placed"/>.</summary>
    public static Session Prepare(RectTransform root, string moduleKey, Vector2 area)
    {
        var s = new Session { Root = root };
        if (root == null || area.x <= 1f) { s.Placed = root; return s; }

        var hide = HideFor(moduleKey);
        if (hide != null)
            foreach (var name in hide) HideByName(root, name, s);

        var node = Descend(root, area.x, s);


        Fit(node, node.rect.width, area.x, s, 0);

        s.Record(node);                       // 도크로 옮기기 전 원래 자리를 기억
        // 도크보다 훨씬 좁으면 도크 폭까지 넓힌다. 세로로 긴 폼(파견 조작부 360×964)은
        // 높이 때문에 축소가 걸리는데, 미리 넓혀두면 같은 축소율에서도 훨씬 크게 보인다.
        // stretch 앵커를 쓰는 자식은 따라 넓어지고, 고정 폭 자식은 그대로 남는다(빈자리만 생긴다).
        // ⚠️ 세로가 넘치는 것에만 적용한다. 높이에 여유가 있는 패널(제작 520×480)은
        //    넓히는 대신 통째로 키우는 편이 낫다 — 내부 2열 배치를 흔들지 않는다.
        //    창고 격자(643)처럼 이미 도크 폭의 8할을 쓰는 것도 건드리면 안 된다 — 격자 칸이
        //    고정 크기라 늘려봐야 오른쪽에 빈자리만 생긴다. 확실히 좁은 것(7할 미만)만 넓힌다.
        if (node.rect.height > area.y && node.rect.width < area.x * 0.7f)
        {
            s.Record(node);
            SetBox(node, area.x, node.rect.height);
        }

        s.Placed = WrapIfTall(node, area, s);
        return s;
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
