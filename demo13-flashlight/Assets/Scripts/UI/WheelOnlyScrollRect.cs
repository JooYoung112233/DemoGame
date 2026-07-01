using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>드래그로는 스크롤되지 않고 **마우스 휠로만** 스크롤되는 ScrollRect.
/// (아이템을 들고 격자 위를 드래그할 때 창고가 멋대로 스크롤되던 문제 방지 — 휠 전용.)
/// OnScroll(휠)은 base 그대로 동작, 드래그 핸들러만 무효화.
///
/// ⚠ 이 클래스는 프리팹 베이크 대상(CharacterPanelUI가 LeftViewport에 부착)이므로
///   반드시 파일명=클래스명인 '독립 파일'에 있어야 한다. 다른 .cs(예: CharacterPanelUI.cs)
///   안에 두면 Unity가 MonoScript 에셋을 만들지 않아 SaveAsPrefabAsset이 스크립트 참조를
///   직렬화하지 못하고, 베이크된 프리팹에서 베이스 UnityEngine.UI.ScrollRect로 격하된다
///   (→ 드래그 무효화 오버라이드 소실). 절대 다시 합치지 말 것.</summary>
public class WheelOnlyScrollRect : UnityEngine.UI.ScrollRect
{
    public override void OnBeginDrag(PointerEventData e) { }
    public override void OnDrag(PointerEventData e) { }
    public override void OnEndDrag(PointerEventData e) { }

    /// <summary>휠 1 notch당 pixelsPerNotch만큼 세로 스크롤(내용 높이와 무관하게 일정 속도).
    /// content.anchoredPosition을 직접 이동(ScrollRect 내부 bounds/normalizedPosition 비의존 — 더 견고) + 스크롤바 수동 연동.
    /// content는 top pivot(0,1) 가정(창고/상점 모두 해당): y∈[0,max], 0=맨 위.</summary>
    public static void WheelStep(UnityEngine.UI.ScrollRect sr, float wheelY, float pixelsPerNotch = 170f)
    {
        if (sr == null || sr.content == null) return;
        var vp = sr.viewport != null ? sr.viewport : sr.transform as RectTransform;
        if (vp == null) return;
        float maxScroll = sr.content.rect.height - vp.rect.height;
        if (maxScroll <= 1f) return;
        var p = sr.content.anchoredPosition;
        p.y = Mathf.Clamp(p.y - Mathf.Sign(wheelY) * pixelsPerNotch, 0f, maxScroll);
        sr.content.anchoredPosition = p;
        sr.velocity = Vector2.zero;
        if (sr.verticalScrollbar != null)
            sr.verticalScrollbar.SetValueWithoutNotify(1f - p.y / maxScroll);   // top=1
    }
}
