using UnityEngine;
using UnityEngine.UI;

/// <summary>화면 픽셀 두께가 일정한 원 — 조준원(<see cref="AimReticle"/>)용.
/// 원 이미지를 늘려 쓰면 선도 같이 굵어지고(큰 원), 작은 원에선 선이 사라진다 — 그래서 메시로 직접 그린다.
/// thickness ≥ radius×2 면 속이 찬 점이 된다.
/// ⚠️ CanvasRenderer가 없으면 크기·위치는 멀쩡한데 **화면에 아무것도 안 그려진다**(2026-09-11 실측 —
///    이 버전에선 런타임 AddComponent로 Graphic을 붙여도 CanvasRenderer가 자동으로 안 따라왔다).</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ReticleRing : MaskableGraphic
{
    [SerializeField] float radius = 20f;     // px
    [SerializeField] float thickness = 2f;   // px
    const int Segments = 48;

    public void Set(float r, float t)
    {
        if (Mathf.Approximately(r, radius) && Mathf.Approximately(t, thickness)) return;
        radius = r; thickness = t;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float r0 = Mathf.Max(0f, radius - thickness * 0.5f);
        float r1 = radius + thickness * 0.5f;
        Color32 c = color;
        for (int i = 0; i <= Segments; i++)
        {
            float a = i * Mathf.PI * 2f / Segments;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            vh.AddVert(d * r0, c, Vector2.zero);
            vh.AddVert(d * r1, c, Vector2.zero);
            if (i == 0) continue;
            int k = i * 2;
            vh.AddTriangle(k - 2, k - 1, k + 1);
            vh.AddTriangle(k - 2, k + 1, k);
        }
    }
}
