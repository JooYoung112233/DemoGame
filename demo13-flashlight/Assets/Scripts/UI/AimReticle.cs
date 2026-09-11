using UnityEngine;

/// <summary>총 조준원 — 마우스 위치에 **퍼짐만큼 벌어지는 원** (docs/combat.md §총격전, 2026-09-11 사용자 결정 "커서 조준원").
///
/// 원의 크기 = 플레이어→커서 거리에서 지금 탄퍼짐(<see cref="PlayerGun.CurrentSpreadDeg"/>)이 만드는 반경을 화면 픽셀로 바꾼 것.
/// 그래서 연사하면 벌어지고(퍼짐 누적), 우클릭 조준하면 좁아진다 — "원 안 어딘가로 간다"가 곧 규칙이다.
/// 총이 입력을 받는 동안(총 장착 + UI 닫힘)만, 달리기(총 수납) 중엔 숨긴다. 보이는 동안 OS 커서는 숨긴다.
/// PlayerGun이 붙인다 — 씬에 따로 배치하지 않는다.</summary>
[DisallowMultipleComponent]
public class AimReticle : MonoBehaviour
{
    PlayerGun _gun;
    TopDownPlayer _player;
    Camera _cam;
    Canvas _canvas;
    ReticleRing _outline, _ring, _dotOutline, _dot;
    bool _shown;

    /// <summary>원이 너무 작아지면 점과 겹쳐 안 읽힌다 — 최소 반경(px).</summary>
    const float MinRadiusPx = 7f;

    void Awake()
    {
        _gun = GetComponent<PlayerGun>();
        _player = GetComponent<TopDownPlayer>();
    }

    Camera Cam()
    {
        if (_cam != null) return _cam;
        if (CameraFollow.Instance != null) _cam = CameraFollow.Instance.GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        return _cam;
    }

    void Build()
    {
        var go = new GameObject("AimReticleCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 900;
        // 밝은 바닥에서도 읽히게 — 어두운 테두리 위에 흰 선.
        _outline = Ring(go.transform, "Outline", new Color(0f, 0f, 0f, 0.6f));
        _ring = Ring(go.transform, "Ring", Color.white);
        _dotOutline = Ring(go.transform, "DotOutline", new Color(0f, 0f, 0f, 0.6f));
        _dot = Ring(go.transform, "Dot", Color.white);
        _dotOutline.Set(0f, 6f);
        _dot.Set(0f, 3f);
    }

    static ReticleRing Ring(Transform parent, string name, Color c)
    {
        // CanvasRenderer를 처음부터 같이 만든다 — 없으면 그려지지 않는다(ReticleRing 주석).
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<ReticleRing>();
        r.color = c;
        r.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;   // 좌하단 기준 = 화면 픽셀 좌표 그대로
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        return r;
    }

    void LateUpdate()
    {
        // 2026-09-11: 조준(우클릭) 중에만 — 평소엔 몸이 이동 방향을 보고 탄도 그쪽으로 나가므로, 커서에 원을 띄우면 거짓말이 된다.
        bool want = _gun != null && _gun.ReticleActive && _gun.IsAiming && _player != null && !_player.IsSprinting && Cam() != null;
        if (want != _shown)
        {
            _shown = want;
            if (want && _canvas == null) Build();
            if (_canvas != null) _canvas.enabled = want;
            Cursor.visible = !want;
        }
        if (!want) return;

        // 반경: 플레이어→커서 거리 × tan(퍼짐)을 커서 지점에서 옆으로 재 화면 픽셀로 바꾼다.
        Vector3 aim = _player.MouseWorldPos;
        Vector2 d = Plan3D.ToPlan(aim - _player.transform.position);
        float dist = d.magnitude;
        float spread = Mathf.Clamp(_gun.CurrentSpreadDeg, 0f, 60f) * Mathf.Deg2Rad;
        float rWorld = dist * Mathf.Tan(spread);
        Vector2 perp = dist > 0.001f ? new Vector2(-d.y, d.x) / dist : Vector2.right;
        Vector3 center = _cam.WorldToScreenPoint(aim);
        Vector3 side = _cam.WorldToScreenPoint(aim + Plan3D.ToWorld(perp) * rWorld);
        float rPx = Mathf.Max(MinRadiusPx, Vector2.Distance(center, side));

        // 마우스면 커서 자리 그대로(지면 역투영 오차 없이), 패드면 조준 지점 투영.
        Vector2 pos = GameInput.PadActive ? (Vector2)center : (Vector2)GameInput.mousePosition;
        foreach (var r in new[] { _outline, _ring, _dotOutline, _dot }) r.rectTransform.anchoredPosition = pos;
        _outline.Set(rPx, 4f);
        _ring.Set(rPx, 2f);

        // 장전 중엔 흐리게 — 지금은 못 쏜다.
        var c = _gun.IsReloading ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        _ring.color = c;
        _dot.color = c;
    }

    void OnDisable()
    {
        if (!_shown) return;
        _shown = false;
        if (_canvas != null) _canvas.enabled = false;
        Cursor.visible = true;
    }
}
