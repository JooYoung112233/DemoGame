using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **외곽선용 부드러운 법선**을 구워 넣는다.
///
/// 왜 필요한가 — 인버티드 헐 외곽선은 정점을 법선 방향으로 밀어 만든다. 그런데 이 프로젝트의
/// 캐릭터 모델은 거의 전부 **하드 엣지**다(실측: `Bandit_Belt`는 정점 144개가 고유 위치 12개에
/// 몰려 있고, 위치를 공유하는 정점의 83~98%가 법선이 20° 넘게 갈린다). 같은 자리에 있는 정점이
/// 면마다 다른 방향으로 밀리면 테두리가 **이어지지 않고 조각조각 찢어진다.** 실제로 그렇게 보였다.
///
/// 해결: **위치가 같은 정점들의 법선을 평균**내어 하나의 방향을 만들고, 그걸 밀기용으로 쓴다.
/// 저장 자리는 `tangent.xyz` — 이 모델들은 노멀맵을 쓰지 않아 탄젠트가 비어 있고, 무엇보다
/// **스키닝이 탄젠트도 같이 회전**시켜 준다(UV 채널에 넣으면 애니메이션 중에 방향이 안 따라온다).
///
/// 원본 에셋은 건드리지 않는다 — 메시를 복제해 캐시하고 렌더러에 갈아 끼운다.
/// (에디터에서 sharedMesh를 직접 고치면 임포트된 FBX 메시가 오염된다.)
/// </summary>
public static class OutlineNormals
{
    // 키는 **메시 참조 자체**. Unity 6.6에서 GetInstanceID()도, EntityId→int 캐스트도
    // obsolete-as-error라 정수 ID를 쓸 수 없다.
    static readonly Dictionary<Mesh, Mesh> _cache = new Dictionary<Mesh, Mesh>();

    /// <summary>이 렌더러(와 자식)의 메시를 부드러운 법선이 구워진 복제본으로 교체한다.</summary>
    public static void Apply(GameObject root)
    {
        if (root == null) return;
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null) smr.sharedMesh = GetSmoothed(smr.sharedMesh);
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) mf.sharedMesh = GetSmoothed(mf.sharedMesh);
    }

    /// <summary>부드러운 법선이 탄젠트에 들어간 복제 메시(같은 원본은 한 번만 만든다).</summary>
    public static Mesh GetSmoothed(Mesh src)
    {
        if (src == null) return null;
        if (_cache.TryGetValue(src, out var hit) && hit != null) return hit;

        var m = Object.Instantiate(src);
        m.name = src.name + "_SmoothOutline";

        var verts = m.vertices;
        var norms = m.normals;
        if (verts == null || norms == null || verts.Length != norms.Length)
        {
            _cache[src] = m;
            return m;
        }

        // 위치를 1mm 격자로 반올림해 묶는다 — FBX 왕복에서 좌표가 미세하게 어긋나 있어
        // 정확히 같은 값으로는 안 묶인다.
        var sum = new Dictionary<Vector3Int, Vector3>(verts.Length);
        var keys = new Vector3Int[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            var k = new Vector3Int(Mathf.RoundToInt(verts[i].x * 1000f),
                                   Mathf.RoundToInt(verts[i].y * 1000f),
                                   Mathf.RoundToInt(verts[i].z * 1000f));
            keys[i] = k;
            sum[k] = sum.TryGetValue(k, out var acc) ? acc + norms[i] : norms[i];
        }

        var tans = new Vector4[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            var n = sum[keys[i]];
            if (n.sqrMagnitude < 1e-8f) n = norms[i];      // 서로 상쇄된 경우(양면 판때기 등)
            n.Normalize();
            tans[i] = new Vector4(n.x, n.y, n.z, 1f);
        }
        m.tangents = tans;

        _cache[src] = m;
        return m;
    }
}
