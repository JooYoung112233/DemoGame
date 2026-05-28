using UnityEngine;

/// <summary>
/// 벽 뒤에 가려진 유닛의 아웃라인 표시.
/// SpriteRenderer 기반 캐릭터에 대응 — 동일 스프라이트 텍스처를 사용하여
/// OcclusionOutline 셰이더(ZTest Greater)로 벽 뒤에서만 외곽선 렌더링.
///
/// 자동 셋업: outlineRenderer가 없으면 Awake에서 Quad 메쉬를 자동 생성.
/// 매 프레임 소스 SpriteRenderer의 _CurrentFrame을 아웃라인에 동기화.
/// </summary>
public class WallOcclusionOutline : MonoBehaviour
{
    [Header("소스 (자동 탐색)")]
    [Tooltip("아웃라인 원본 SpriteRenderer (없으면 자식에서 탐색)")]
    [SerializeField] SpriteRenderer sourceSpriteRenderer;

    [Header("아웃라인 렌더러")]
    [Tooltip("아웃라인용 MeshRenderer (없으면 자동 생성)")]
    [SerializeField] MeshRenderer outlineRenderer;

    [Header("스타일")]
    [Tooltip("아웃라인 색상")]
    [SerializeField] Color outlineColor = new Color(1f, 1f, 1f, 0.7f);

    [Tooltip("아웃라인 두께 (텍셀 단위)")]
    [SerializeField] float outlineWidth = 2f;

    [Header("감지")]
    [Tooltip("레이캐스트 시작점 오프셋 (캐릭터 중심)")]
    [SerializeField] Vector3 centerOffset = new Vector3(0, 0.5f, 0);

    [Tooltip("벽 레이어 마스크")]
    [SerializeField] LayerMask wallLayerMask = ~0;

    [Tooltip("자기 레이어 자동 제외")]
    [SerializeField] bool autoExcludeOwnLayer = true;

    Camera mainCam;
    MaterialPropertyBlock mpb;
    Material outlineMat;
    bool wasOccluded;
    Transform outlineTransform;

    // 셰이더 프로퍼티 ID 캐시
    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static readonly int ID_OutlineWidth = Shader.PropertyToID("_OutlineWidth");
    static readonly int ID_CurrentFrame = Shader.PropertyToID("_CurrentFrame");
    static readonly int ID_MainTex = Shader.PropertyToID("_MainTex");
    static readonly int ID_Columns = Shader.PropertyToID("_Columns");
    static readonly int ID_Rows = Shader.PropertyToID("_Rows");
    static readonly int ID_Cutoff = Shader.PropertyToID("_Cutoff");

    void Awake()
    {
        mpb = new MaterialPropertyBlock();

        // 소스 SpriteRenderer 탐색
        if (sourceSpriteRenderer == null)
            sourceSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 자기 레이어 제외
        if (autoExcludeOwnLayer)
            wallLayerMask &= ~(1 << gameObject.layer);

        // 아웃라인 렌더러 자동 생성
        if (outlineRenderer == null)
            outlineRenderer = FindOrCreateOutlineRenderer();

        if (outlineRenderer != null)
        {
            outlineRenderer.enabled = false;
            outlineTransform = outlineRenderer.transform;
        }
    }

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam == null || outlineRenderer == null || sourceSpriteRenderer == null) return;

        bool occluded = CheckOcclusion();

        if (occluded != wasOccluded)
        {
            outlineRenderer.enabled = occluded;
            wasOccluded = occluded;
        }

        if (occluded)
            SyncOutline();
    }

    bool CheckOcclusion()
    {
        Vector3 camPos = mainCam.transform.position;
        Vector3 targetPos = transform.position + centerOffset;
        Vector3 dir = targetPos - camPos;
        float dist = dir.magnitude;

        return Physics.Raycast(camPos, dir.normalized, dist - 0.3f, wallLayerMask);
    }

    /// <summary>소스 스프라이트의 프레임/텍스처를 아웃라인에 동기화</summary>
    void SyncOutline()
    {
        // 위치/회전을 소스 스프라이트와 동일하게
        if (outlineTransform.parent != sourceSpriteRenderer.transform.parent)
            outlineTransform.SetParent(sourceSpriteRenderer.transform.parent, false);

        outlineTransform.localPosition = sourceSpriteRenderer.transform.localPosition;
        outlineTransform.localRotation = sourceSpriteRenderer.transform.localRotation;
        outlineTransform.localScale = sourceSpriteRenderer.transform.localScale;

        // MaterialPropertyBlock으로 프레임 동기화
        sourceSpriteRenderer.GetPropertyBlock(mpb);
        float frame = mpb.GetFloat(ID_CurrentFrame);

        outlineRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(ID_CurrentFrame, frame);
        mpb.SetColor(ID_Color, outlineColor);
        mpb.SetFloat(ID_OutlineWidth, outlineWidth);
        outlineRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>색상 변경</summary>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
    }

    /// <summary>자식에서 기존 아웃라인 렌더러 탐색, 없으면 Quad 자동 생성</summary>
    MeshRenderer FindOrCreateOutlineRenderer()
    {
        // 기존 아웃라인 탐색
        var existing = transform.Find("OcclusionOutlineQuad");
        if (existing != null)
        {
            var mr = existing.GetComponent<MeshRenderer>();
            if (mr != null) return mr;
        }

        // 소스가 없으면 생성 불가
        if (sourceSpriteRenderer == null) return null;

        // Quad 메쉬 생성
        var quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGO.name = "OcclusionOutlineQuad";

        // 콜라이더 제거 (Quad는 기본 MeshCollider 포함)
        var col = quadGO.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        // 소스 스프라이트의 부모(Root)에 배치
        quadGO.transform.SetParent(sourceSpriteRenderer.transform.parent, false);
        quadGO.transform.localPosition = sourceSpriteRenderer.transform.localPosition;
        quadGO.transform.localRotation = sourceSpriteRenderer.transform.localRotation;
        quadGO.transform.localScale = sourceSpriteRenderer.transform.localScale;

        // 머티리얼 생성
        var shader = Shader.Find("Custom/OcclusionOutline");
        if (shader == null)
        {
            Debug.LogWarning("[WallOcclusionOutline] Custom/OcclusionOutline 셰이더를 찾을 수 없음");
            Destroy(quadGO);
            return null;
        }

        outlineMat = new Material(shader);

        // 소스 스프라이트에서 텍스처/시트 정보 복사
        var srcMat = sourceSpriteRenderer.sharedMaterial;
        if (srcMat != null)
        {
            if (srcMat.HasTexture(ID_MainTex))
                outlineMat.SetTexture(ID_MainTex, srcMat.GetTexture(ID_MainTex));
            if (srcMat.HasFloat(ID_Columns))
                outlineMat.SetFloat(ID_Columns, srcMat.GetFloat(ID_Columns));
            if (srcMat.HasFloat(ID_Rows))
                outlineMat.SetFloat(ID_Rows, srcMat.GetFloat(ID_Rows));
            if (srcMat.HasFloat(ID_Cutoff))
                outlineMat.SetFloat(ID_Cutoff, srcMat.GetFloat(ID_Cutoff));
        }

        outlineMat.SetColor(ID_Color, outlineColor);
        outlineMat.SetFloat(ID_OutlineWidth, outlineWidth);

        var mr2 = quadGO.GetComponent<MeshRenderer>();
        mr2.sharedMaterial = outlineMat;
        mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr2.receiveShadows = false;

        return mr2;
    }

    void OnValidate()
    {
        if (outlineRenderer != null && mpb != null)
        {
            outlineRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(ID_Color, outlineColor);
            mpb.SetFloat(ID_OutlineWidth, outlineWidth);
            outlineRenderer.SetPropertyBlock(mpb);
        }
    }
}
