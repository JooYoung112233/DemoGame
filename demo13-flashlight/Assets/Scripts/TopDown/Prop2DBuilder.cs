using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prop2DDefinition → 실제 GameObject(SpriteRenderer + Collider2D) 생성.
/// 런타임 배치와 에디터 미리보기/베이크가 같은 경로를 쓰도록 static.
/// </summary>
public static class Prop2DBuilder
{
    /// <summary>정의로부터 새 프롭 GameObject를 만든다(SpriteRenderer + 콜라이더).</summary>
    public static GameObject Build(Prop2DDefinition def)
    {
        string name = !string.IsNullOrEmpty(def.displayName) ? def.displayName
                    : !string.IsNullOrEmpty(def.propId) ? $"Prop_{def.propId}" : "Prop";
        var go = new GameObject(name);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = def.sprite;
        if (def.material != null) sr.sharedMaterial = def.material;
        if (!string.IsNullOrEmpty(def.sortingLayer))
            sr.sortingLayerName = def.sortingLayer;
        sr.sortingOrder = def.sortingOffset;

        // Draw mode (벽 타일링 등). Tiled/Sliced는 size 지정.
        sr.drawMode = def.drawMode;
        if (def.drawMode != SpriteDrawMode.Simple)
        {
            sr.tileMode = def.tileMode;
            sr.size = def.tiledSize == Vector2.zero ? Vector2.one : def.tiledSize;
        }

        ApplyColliders(go, def);
        return go;
    }

    /// <summary>
    /// 기존 GameObject의 Collider2D를 모두 제거하고 정의대로 다시 붙인다.
    /// (에디터 재적용·런타임 둘 다 안전.)
    /// </summary>
    public static void ApplyColliders(GameObject go, Prop2DDefinition def)
    {
        if (go == null || def == null) return;

        foreach (var existing in go.GetComponents<Collider2D>())
            DestroySafe(existing);

        switch (def.colliderMode)
        {
            case Prop2DDefinition.ColliderMode.None:
                break;

            case Prop2DDefinition.ColliderMode.Box:
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = def.isTrigger;
                if (def.drawMode != SpriteDrawMode.Simple)
                {
                    // Tiled/Sliced: 콜라이더를 실제 렌더 크기(tiledSize)에 맞춤 (벽 길이 전체).
                    var s = def.tiledSize == Vector2.zero ? Vector2.one : def.tiledSize;
                    box.size = Vector2.Scale(s, def.boxSizeScale);
                    box.offset = def.boxOffset;
                }
                else if (def.sprite != null)
                {
                    var b = def.sprite.bounds; // 월드 단위(PixelsPerUnit 반영)
                    box.size = Vector2.Scale(b.size, def.boxSizeScale);
                    box.offset = (Vector2)b.center + def.boxOffset;
                }
                break;
            }

            case Prop2DDefinition.ColliderMode.Polygon:
            {
                var poly = go.AddComponent<PolygonCollider2D>();
                poly.isTrigger = def.isTrigger;
                FitPolygonToSprite(poly, def.sprite);
                break;
            }

            case Prop2DDefinition.ColliderMode.Composite:
            {
                if (def.compositeBoxes != null)
                {
                    foreach (var cb in def.compositeBoxes)
                    {
                        var box = go.AddComponent<BoxCollider2D>();
                        box.isTrigger = def.isTrigger;
                        box.offset = cb.center;
                        box.size = cb.size == Vector2.zero ? Vector2.one : cb.size;
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// 스프라이트의 physics shape(외곽선)을 읽어 PolygonCollider2D path로 채운다.
    /// 다중 path(각진/오목/구멍) 지원. physics shape가 없으면 스프라이트 사각형으로 폴백.
    /// </summary>
    static void FitPolygonToSprite(PolygonCollider2D poly, Sprite sprite)
    {
        if (sprite == null) { poly.pathCount = 0; return; }

        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount > 0)
        {
            poly.pathCount = shapeCount;
            var pts = new List<Vector2>();
            for (int i = 0; i < shapeCount; i++)
            {
                sprite.GetPhysicsShape(i, pts);
                poly.SetPath(i, pts.ToArray());
            }
            return;
        }

        // 폴백: 스프라이트 사각형 외곽
        var b = sprite.bounds;
        Vector2 c = b.center, h = (Vector2)b.extents;
        poly.pathCount = 1;
        poly.SetPath(0, new[]
        {
            c + new Vector2(-h.x, -h.y),
            c + new Vector2( h.x, -h.y),
            c + new Vector2( h.x,  h.y),
            c + new Vector2(-h.x,  h.y),
        });
    }

    static void DestroySafe(Object o)
    {
        if (o == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { Object.DestroyImmediate(o); return; }
#endif
        Object.Destroy(o);
    }
}
