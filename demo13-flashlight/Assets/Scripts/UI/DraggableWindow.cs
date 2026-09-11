using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 헤더에 붙여 드래그하면 target 창(RectTransform)을 이동시키는 간단한 핸들.
/// uGUI EventSystem 드래그 사용 — 인벤토리의 수동 Input 드래그(아이템)와 분리되어 충돌 없음.
/// </summary>
public class DraggableWindow : MonoBehaviour, IDragHandler
{
    public RectTransform target;

    public void OnDrag(PointerEventData e)
    {
        if (target == null) return;
        var canvas = target.GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        if (scale <= 0f) scale = 1f;
        target.anchoredPosition += e.delta / scale;
    }
}
