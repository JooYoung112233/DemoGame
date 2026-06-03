using UnityEngine;

/// <summary>
/// BoxCollider2D를 같은 오브젝트의 SpriteRenderer에 지속적으로 맞춘다(씬에서 스프라이트·Tiled 크기를 바꾸면 콜라이더가 따라옴).
/// Tiled/Sliced면 SpriteRenderer.size, Simple이면 sprite.bounds 기준 + sizeScale·offset.
/// (Prop2DBuilder가 빌드 시 적용하던 박스 산출을 "라이브 SpriteRenderer" 기준으로 재현 — 맵툴 배치본·씬 수정본 모두.)
/// 변경 감지 시에만 갱신. 에디트 모드 위주(런타임도 안전, 변경 없으면 비용 0).
/// Polygon/Composite 콜라이더는 대상 아님(Box 전용) — 그건 스프라이트 physics shape를 직접 편집.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class ColliderAutoFit : MonoBehaviour
{
    [Tooltip("스프라이트 크기 대비 배율(1,1=그림 크기 그대로).")]
    public Vector2 sizeScale = Vector2.one;
    [Tooltip("콜라이더 중심 오프셋(로컬 단위).")]
    public Vector2 offset = Vector2.zero;

    SpriteRenderer _sr;
    BoxCollider2D _box;

    // 변경 감지 캐시
    Sprite _cSprite; SpriteDrawMode _cDraw; Vector2 _cSize; Vector2 _cScale; Vector2 _cOffset;

    void OnEnable()
    {
        Cache(); Fit(true);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorTick;
        UnityEditor.EditorApplication.update += EditorTick;   // 매 에디터 틱 확실히
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorTick;
#endif
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorTick;
#endif
    }

    void OnValidate() { Fit(true); }                 // 인스펙터에서 scale/offset 바꾸면 즉시
    void Update() { if (Application.isPlaying) Fit(false); }   // 런타임만(에디트는 EditorTick)

#if UNITY_EDITOR
    void EditorTick()
    {
        if (this == null) { UnityEditor.EditorApplication.update -= EditorTick; return; }
        if (Application.isPlaying || !isActiveAndEnabled) return;
        Fit(false);
    }
#endif

    void Cache()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_box == null) _box = GetComponent<BoxCollider2D>();
    }

    /// <summary>현재 sizeScale/offset 기준으로 즉시 재맞춤(코드로 값 세팅 후 호출).</summary>
    public void Refit() => Fit(true);

    void Fit(bool force)
    {
        Cache();
        if (_sr == null || _box == null || _sr.sprite == null) return;

        bool changed = force
            || _cSprite != _sr.sprite || _cDraw != _sr.drawMode || _cSize != _sr.size
            || _cScale != sizeScale || _cOffset != offset;
        if (!changed) return;

        if (_sr.drawMode != SpriteDrawMode.Simple)
        {
            // Tiled/Sliced: 렌더 크기(size)에 맞춤. 중심은 스프라이트 피벗만큼 보정(Prop2DBuilder와 동일).
            Vector2 s = _sr.size;
            _box.size = Vector2.Scale(s, sizeScale);
            var sb = _sr.sprite.bounds;
            Vector2 frac = new(
                sb.size.x > 1e-5f ? sb.center.x / sb.size.x : 0f,
                sb.size.y > 1e-5f ? sb.center.y / sb.size.y : 0f);
            _box.offset = Vector2.Scale(s, frac) + offset;
        }
        else
        {
            var b = _sr.sprite.bounds;   // 월드 단위(PixelsPerUnit 반영)
            _box.size = Vector2.Scale(b.size, sizeScale);
            _box.offset = (Vector2)b.center + offset;
        }

        _cSprite = _sr.sprite; _cDraw = _sr.drawMode; _cSize = _sr.size;
        _cScale = sizeScale; _cOffset = offset;
    }
}
