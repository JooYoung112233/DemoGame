using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 스크롤뷰 content에 부착. 아이템 클릭/드래그가 부모 ScrollRect 스크롤을 유발하지 않도록
/// 드래그 이벤트를 소비(아무것도 안 함)한다. 스크롤은 휠(IScrollHandler 전달)과 스크롤바로만.
/// CharacterPanelUI의 격자 클릭/드래그는 Input 폴링으로 별도 처리되므로 막아도 무방.
///
/// ⚠ 이 클래스는 프리팹 베이크 대상(CharacterPanelUI가 ContainerGrid에 부착)이므로
///   반드시 파일명=클래스명인 '독립 파일'에 있어야 한다. 다른 .cs(예: CharacterPanelUI.cs)
///   안에 두면 Unity가 MonoScript 에셋을 만들지 않아 SaveAsPrefabAsset이 m_Script를
///   fileID:0(스크립트 유실 = 런타임 미동작)으로 굽는다. 절대 다시 합치지 말 것.
/// (ShopUI는 런타임 AddComponent라 영향 없지만, 여기도 같은 타입을 참조하므로 독립 파일 유지 필수.)
/// </summary>
public class ScrollDragBlocker : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public ScrollRect targetScroll;

    // 드래그 이벤트를 여기서 받아 소비 → 부모 ScrollRect로 전파되지 않음(스크롤 점프 방지).
    public void OnBeginDrag(PointerEventData e) { }
    public void OnDrag(PointerEventData e) { }
    public void OnEndDrag(PointerEventData e) { }

    // 휠 스크롤은 부모 ScrollRect로 전달해 정상 동작 유지.
    public void OnScroll(PointerEventData e)
    {
        if (targetScroll != null)
            targetScroll.OnScroll(e);
    }
}
