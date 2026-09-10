#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **안쪽을 보는 법선을 임포트 시점에 뒤집어 바로잡는다.**
///
/// 왜 필요한가 — 2026-09-10 플레이어의 어깨(`Hero_Sleeve_1/-1`)가 어떤 조명에서도 새까맣게
/// 나왔다. 원인을 단계별로 좁힌 결과:
///   · 알베도만(언릿) 렌더 → 정상(올리브 0.27)      · 외곽선 끔 → 여전히 검정
///   · 태양 그림자 끔 → 변화 없음                    · 머티리얼 → 옆 파트와 **동일 인스턴스**
/// 남은 입력이 법선뿐이었고, 실측에서 `Hero_Sleeve`는 정점 428개 **전부**가 안쪽을 보고 있었다.
/// 셰이딩이 "모든 광원을 등진 면"으로 계산되니 램프 최하단, 즉 검정으로 떨어진다.
/// **이건 셰이더와 무관하다** — URP/Lit으로 렌더되던 시점에도 어깨는 똑같이 검었다.
///
/// 왜 여기서 고치나 — 근본 수정은 블렌더(Recalculate Normals Outside)지만, 이 프로젝트의 캐릭터는
/// `tools/*.py`로 **수시로 재익스포트**된다. 임포트 후처리로 두면 FBX를 다시 뽑아도 계속 유효하고,
/// 같은 결함이 있는 다른 모델도 자동으로 걸린다.
///
/// 판정 — **메시 중심에서 바깥으로 향하는 방향과 정점 법선을 비교**한다. 실측값이 확실히 갈린다:
///   정상 재킷 +0.833 / 롤소매 +0.804 / 팔뚝 +0.605  vs  결함 소매 −0.879(100% 안쪽).
///
/// ⚠️ 와인딩(면 감김)과 비교하는 판정을 먼저 넣었다가 뺐다. 임포트 시점에는 인덱스 버퍼를 읽을 수
///    없어 `GetTriangles`가 0개를 돌려주는 메시가 대부분이라, 그 조건을 먼저 검사하면 중심 판정에
///    도달하지도 못한다(실제로 후처리기가 아무것도 못 잡았다). 지표로만 남겨 둔다.
///
/// ⚠️ 법선만 뒤집고 와인딩은 건드리지 않는다. 이 파트는 알베도만 렌더하면 **모양이 정상**이라
///    감김은 맞다는 뜻이고, 확인할 수 없는 것을 같이 뒤집으면 멀쩡한 면이 컬링된다.
/// </summary>
public sealed class MeshNormalRepair : AssetPostprocessor
{
    /// <summary>바깥향 평균이 이 값보다 작으면 뒤집힌 것으로 본다.
    /// 챙처럼 오목한 파트(모자 +0.444)를 오판하지 않도록 넉넉히 음수 쪽에 둔다.</summary>
    const float InwardMean = -0.50f;

    /// <summary>이 판정은 부피가 있어야 의미가 있다. 가장 짧은 변이 가장 긴 변의 이 비율 미만이면
    /// 판때기로 보고 건드리지 않는다(라펠은 정점 5개짜리 평면이라 중심 기준 판정이 무의미).</summary>
    const float MinThickness = 0.08f;

    /// <summary>정점이 이보다 적으면 판정이 흔들려서 제외.</summary>
    const int MinVertices = 24;

    void OnPostprocessModel(GameObject root)
    {
        var done = new HashSet<Mesh>();
        var log = new List<string>();

        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null && done.Add(smr.sharedMesh)) Try(smr.sharedMesh, log);

        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null && done.Add(mf.sharedMesh)) Try(mf.sharedMesh, log);

        if (log.Count > 0)
            Debug.Log($"[법선 보정] {assetPath} — 안쪽을 보던 메시 {log.Count}개 뒤집음: "
                    + string.Join(", ", log));
    }

    static void Try(Mesh m, List<string> log)
    {
        if (!IsInverted(m, out float outward, out _, out _)) return;
        int tris = Flip(m);
        log.Add($"{m.name}({outward:F2}, 삼각형 {tris})");
    }

    /// <summary>법선·탄젠트를 반전하고 **면 감김도 뒤집는다**. 임포트 중인 메시에만 쓸 것.
    /// 돌려주는 값은 감김을 뒤집은 삼각형 수(0이면 인덱스를 못 읽었다는 뜻).
    ///
    /// ⚠️ 감김까지 뒤집어야 하는 이유 — 이 메시들은 법선만이 아니라 **안팎이 통째로** 뒤집혀 있다.
    ///    그러면 `Cull Back`인 본체 패스는 안쪽면(먼 쪽)을, `Cull Front`인 외곽선 패스는
    ///    바깥면(가까운 쪽)을 그린다. 외곽선이 **항상 앞**이라 깊이를 아무리 밀어도(Offset,
    ///    시선방향 밀기) 본체를 덮는다 — 실제로 어깨가 통째로 검게 나온 마지막 원인이 이것이었다.
    ///    법선만 뒤집으면 라이팅은 맞아 보여도 우리가 보는 면은 여전히 뒷면이다.</summary>
    public static int Flip(Mesh mesh)
    {
        var n = mesh.normals;
        for (int i = 0; i < n.Length; i++) n[i] = -n[i];
        mesh.normals = n;

        // 탄젠트도 같은 기준으로 만들어진 값이라 같이 뒤집는다.
        var t = mesh.tangents;
        if (t != null && t.Length == n.Length)
        {
            for (int i = 0; i < t.Length; i++) t[i] = new Vector4(-t[i].x, -t[i].y, -t[i].z, -t[i].w);
            mesh.tangents = t;
        }

        int flipped = 0;
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            if (mesh.GetTopology(sub) != MeshTopology.Triangles) continue;
            int[] tri;
            try { tri = mesh.GetTriangles(sub); } catch { continue; }
            if (tri == null || tri.Length == 0) continue;
            for (int i = 0; i + 2 < tri.Length; i += 3)
                (tri[i + 1], tri[i + 2]) = (tri[i + 2], tri[i + 1]);
            mesh.SetTriangles(tri, sub);
            flipped += tri.Length / 3;
        }
        return flipped;
    }

    /// <summary>지표를 재고 뒤집혔는지 돌려준다. 아무것도 바꾸지 않는다.</summary>
    /// <param name="outward">중심 기준 바깥향 평균. 음수일수록 안쪽을 본다.</param>
    /// <param name="inwardRatio">법선이 안쪽을 보는 정점 비율.</param>
    /// <param name="windingDisagree">와인딩과 반대인 삼각형 비율. 인덱스를 못 읽으면 −1(참고용).</param>
    public static bool IsInverted(Mesh mesh, out float outward, out float inwardRatio, out float windingDisagree)
    {
        outward = 0f; inwardRatio = 0f; windingDisagree = -1f;
        if (mesh == null) return false;

        Vector3[] verts, norms;
        try { verts = mesh.vertices; norms = mesh.normals; }
        catch { return false; }
        if (verts == null || norms == null || verts.Length < MinVertices || norms.Length != verts.Length)
            return false;

        Vector3 centre = Vector3.zero;
        foreach (var p in verts) centre += p;
        centre /= verts.Length;

        double sum = 0; int used = 0, inward = 0;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 d = verts[i] - centre;
            if (d.sqrMagnitude < 1e-10f) continue;
            double dot = Vector3.Dot(norms[i].normalized, d.normalized);
            sum += dot; used++;
            if (dot < 0) inward++;
        }
        if (used == 0) return false;
        outward = (float)(sum / used);
        inwardRatio = inward / (float)used;

        // 참고 지표 — 읽을 수 있을 때만.
        int against = 0, counted = 0;
        try
        {
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                var tri = mesh.GetTriangles(s);
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    int a = tri[i], b = tri[i + 1], c = tri[i + 2];
                    Vector3 geo = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                    if (geo.sqrMagnitude < 1e-12f) continue;
                    Vector3 avg = norms[a] + norms[b] + norms[c];
                    if (avg.sqrMagnitude < 1e-12f) continue;
                    counted++;
                    if (Vector3.Dot(geo.normalized, avg.normalized) < 0f) against++;
                }
            }
        }
        catch { counted = 0; }
        if (counted > 0) windingDisagree = against / (float)counted;

        var size = mesh.bounds.size;
        float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        float shortest = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        if (longest <= 1e-5f || shortest / longest < MinThickness) return false;   // 판때기는 판정 불가

        return outward <= InwardMean;
    }

    // ── 감사용: 아무것도 바꾸지 않고 현재 상태만 뽑는다 ──────────────────────────
    [MenuItem("Tools/TopDown/개발/법선 뒤집힘 검사 (선택한 모델)")]
    static void AuditSelection()
    {
        var sb = new StringBuilder();
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path)) sb.Append(Report(path));
        }
        Debug.Log(sb.Length == 0 ? "[법선 검사] 모델을 선택하고 다시 실행할 것." : sb.ToString());
    }

    /// <summary>한 모델 에셋의 메시별 지표를 표로. 판정 근거를 눈으로 확인하는 용도.</summary>
    public static string Report(string assetPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### " + assetPath);
        int bad = 0, total = 0;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (!(asset is Mesh m)) continue;
            total++;
            bool inv = IsInverted(m, out float outward, out float inwardRatio, out float wd);
            if (inv) bad++;
            if (inv || outward < 0.25f)
                sb.AppendLine(string.Format("  {0}  {1,-26} 바깥향 {2,7:F3}  안쪽향 {3,4:F0}%  와인딩반대 {4}",
                    inv ? "뒤집힘" : "  주의", m.name, outward, inwardRatio * 100f,
                    wd < 0f ? "읽기불가" : (wd * 100f).ToString("F1") + "%"));
        }
        sb.AppendLine($"  → 메시 {total}개 중 뒤집힘 {bad}개");
        return sb.ToString();
    }
}
#endif
