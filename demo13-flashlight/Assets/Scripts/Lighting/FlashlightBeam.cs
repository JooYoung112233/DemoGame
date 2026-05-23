using UnityEngine;

/// <summary>
/// 손전등 빔 시각 효과 — 바닥에 납작하게 깔리는 반투명 원뿔 메시.
/// FlashlightPivot 자식으로 배치하되, 회전은 Y축(yaw)만 따라감.
/// 피벗의 pitch/roll 기울임은 무시하여 항상 바닥에 깔림.
/// </summary>
public class FlashlightBeam : MonoBehaviour
{
    [Header("Beam Shape")]
    [SerializeField] float beamLength = 12f;
    [SerializeField] float beamEndWidth = 6f;
    [SerializeField] float beamStartWidth = 0.3f;
    [SerializeField] float beamHeight = 0.08f; // 바닥 위 살짝 띄움

    [Header("Visual")]
    [SerializeField] Color beamColor = new Color(1f, 0.95f, 0.8f, 0.2f);
    [SerializeField] Material beamMaterial;

    [Header("Segments")]
    [SerializeField] int segments = 12;

    GameObject beamGO;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh beamMesh;

    FlashlightController flashCtrl;
    Transform pivotTransform;

    void Start()
    {
        flashCtrl = GetComponentInParent<FlashlightController>();
        pivotTransform = transform;
        CreateBeamMesh();
    }

    void CreateBeamMesh()
    {
        // 피벗 자식이 아닌 루트 레벨에 생성 — 피벗 회전 영향 안 받음
        beamGO = new GameObject("BeamMesh");
        beamGO.transform.position = pivotTransform.position;

        meshFilter = beamGO.AddComponent<MeshFilter>();
        meshRenderer = beamGO.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        if (beamMaterial != null)
            meshRenderer.sharedMaterial = beamMaterial;
        else
        {
            var shader = Shader.Find("InkCity/FlashlightBeam");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_Color", beamColor);
                meshRenderer.sharedMaterial = mat;
            }
        }

        BuildMesh();
    }

    void BuildMesh()
    {
        beamMesh = new Mesh();
        beamMesh.name = "FlashlightBeamMesh";

        int vertCount = (segments + 1) * 2;
        var verts = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var tris = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float z = t * beamLength;
            float halfW = Mathf.Lerp(beamStartWidth * 0.5f, beamEndWidth * 0.5f, t);

            int idx = i * 2;
            // XZ 평면에 납작하게 (Y = 0, 로컬)
            verts[idx]     = new Vector3(-halfW, 0, z);
            verts[idx + 1] = new Vector3( halfW, 0, z);

            uvs[idx]     = new Vector2(0, t);
            uvs[idx + 1] = new Vector2(1, t);
        }

        for (int i = 0; i < segments; i++)
        {
            int idx = i * 6;
            int v = i * 2;
            tris[idx]     = v;
            tris[idx + 1] = v + 2;
            tris[idx + 2] = v + 1;
            tris[idx + 3] = v + 1;
            tris[idx + 4] = v + 2;
            tris[idx + 5] = v + 3;
        }

        beamMesh.vertices = verts;
        beamMesh.uv = uvs;
        beamMesh.triangles = tris;
        beamMesh.RecalculateNormals();
        beamMesh.RecalculateBounds();

        meshFilter.mesh = beamMesh;
    }

    void LateUpdate()
    {
        if (beamGO == null || pivotTransform == null) return;

        // 손전등 꺼지면 빔도 숨김
        bool show = flashCtrl == null || flashCtrl.IsOn;
        if (meshRenderer.enabled != show)
            meshRenderer.enabled = show;

        if (!show) return;

        // 위치: 피벗 위치에서 바닥 높이(beamHeight)
        Vector3 pos = pivotTransform.position;
        pos.y = beamHeight;
        beamGO.transform.position = pos;

        // 회전: 피벗의 Y축 회전(yaw)만 사용 — 바닥에 납작하게 유지
        float yaw = pivotTransform.eulerAngles.y;
        beamGO.transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    void OnDestroy()
    {
        if (beamGO != null)
            Destroy(beamGO);
    }
}
