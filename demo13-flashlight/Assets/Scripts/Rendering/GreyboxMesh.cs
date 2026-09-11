using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임 그레이박스 상자 — 색만 다른 단색 큐브를 싸게 찍어내는 공용 헬퍼.
///
/// 맵은 <c>Greybox3D</c>(에디터)가 상자로 세웠지만, <b>런타임에 생기는 것</b>(적 몸통·무기·
/// 떨어진 아이템)은 아직 스프라이트다. 그것들을 3D로 옮기면서 매번 큐브 메시를 만들고
/// 머티리얼을 새로 찍으면 적 44기 × 부위 6개마다 드로우콜이 따로 잡힌다 — 여기서
/// <b>메시 하나와 색별 머티리얼</b>을 공유한다.
///
/// ⚠️ 색을 <b>바꿀</b> 때는 <see cref="Tint"/>를 써라. `renderer.material.color`를 건드리면
///    Unity가 그 순간 머티리얼을 인스턴스화해 공유가 깨지고, 오브젝트마다 하나씩 샌다.
///
/// 설계: docs/3d-migration.md Stage 4
/// </summary>
public static class GreyboxMesh
{
    static Mesh _cube;
    static readonly Dictionary<Color, Material> _mats = new();
    static MaterialPropertyBlock _mpb;
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int ColorID     = Shader.PropertyToID("_Color");

    /// <summary>원점 중심 1m 정육면체(공유). 프리미티브를 만들었다 지우는 방식이라 첫 호출만 비싸다.</summary>
    public static Mesh Cube
    {
        get
        {
            if (_cube != null) return _cube;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cube = tmp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(tmp); else Object.DestroyImmediate(tmp);
            return _cube;
        }
    }

    /// <summary>색만 다른 단색 머티리얼(색별 공유).
    ///
    /// 게임 전용 셰이더 <c>BRB/GameLit</c>(어두운 사실풍 — 때·그늘 채도, docs/rendering.md §게임 전용 셰이더)을 쓴다 —
    /// 떨어진 아이템·런타임 상자가 맵과 같은 룩으로 서야 한 화면으로 읽힌다.
    /// ⚠️ 셰이더를 못 찾으면 URP/Lit으로 — null이면 맵이 통째로 마젠타로 뜬다(2026-09-10 실측).</summary>
    public static Material Material(Color c)
    {
        if (_mats.TryGetValue(c, out var cached) && cached != null) return cached;

        var sh = Shader.Find("BRB/GameLit")
              ?? Shader.Find("Universal Render Pipeline/Lit")
              ?? Shader.Find("Standard");
        var m = new Material(sh) { name = $"gb_{ColorUtility.ToHtmlStringRGB(c)}" };
        if (m.HasProperty(BaseColorID)) m.SetColor(BaseColorID, c);
        if (m.HasProperty(ColorID))     m.SetColor(ColorID, c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        _mats[c] = m;
        return m;
    }

    /// <summary>상자 하나를 만들어 <paramref name="parent"/> 밑에 붙인다.
    /// 위치·크기는 <b>부모 로컬</b> 기준이며, 메시가 1m 큐브라 <paramref name="size"/>가 곧 스케일이다.</summary>
    public static MeshRenderer Box(Transform parent, string name, Vector3 localPos, Vector3 size, Color color,
                                   bool castShadow = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;

        go.AddComponent<MeshFilter>().sharedMesh = Cube;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Material(color);
        mr.shadowCastingMode = castShadow
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = castShadow;
        return mr;
    }

    /// <summary>렌더러 색을 바꾼다 — 머티리얼을 인스턴스화하지 않는다(공유 유지).</summary>
    public static void Tint(Renderer r, Color c)
    {
        if (r == null) return;
        _mpb ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, c);
        _mpb.SetColor(ColorID, c);
        r.SetPropertyBlock(_mpb);
    }
}
