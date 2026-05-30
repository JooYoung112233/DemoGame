using UnityEngine;

/// <summary>
/// 건물 내부 진입/퇴장 시 벽 페이드 + 내부 프랍 가시성 통합 관리.
/// 건물 오브젝트에 Box Collider (Trigger) 와 함께 배치.
///
/// 사용법:
/// 1. 건물에 빈 자식 GO 생성, Box Collider (Is Trigger) 설정
/// 2. BuildingInterior 컴포넌트 추가
/// 3. fadeRenderers 에 앞면 벽/천장 Renderer 할당
/// 4. interiorPropRoot 에 내부 프랍 루트 Transform 할당 (비우면 자동 탐색)
/// 5. Player 태그가 트리거에 진입하면:
///    - 벽/천장 → 투명 (페이드 아웃)
///    - 내부 프랍 → 표시 (페이드 인)
/// </summary>
public class BuildingInterior : MonoBehaviour
{
    [Header("벽/천장 페이드 (진입 시 투명)")]
    [Tooltip("진입 시 투명해질 Renderer 목록 (앞면 벽, 천장 등)")]
    [SerializeField] Renderer[] fadeRenderers;

    [Header("내부 프랍 (진입 시 표시)")]
    [Tooltip("내부 프랍 루트 Transform. 하위 Renderer 전체를 관리. 비우면 자동 탐색")]
    [SerializeField] Transform interiorPropRoot;

    [Tooltip("내부 프랍 자동 수집 (interiorPropRoot 하위 + 태그/레이어 기반)")]
    [SerializeField] bool autoCollectProps = true;

    [Header("설정")]
    [Tooltip("벽 페이드 목표 알파 (0=완전 투명, 0.15=약간 보임)")]
    [SerializeField] float fadeAlpha = 0.15f;

    [Tooltip("페이드 속도")]
    [SerializeField] float fadeSpeed = 5f;

    [Tooltip("페이드에 사용할 셰이더 프로퍼티 이름")]
    [SerializeField] string alphaProperty = "_Alpha";

    [Tooltip("시작 시 프랍 숨김 상태 (외부에서 건물 내부가 안 보임)")]
    [SerializeField] bool startPropsHidden = true;

    [Tooltip("숨김 시 프랍 콜라이더도 비활성화")]
    [SerializeField] bool disablePropColliders = false;

    // MaterialPropertyBlock 으로 인스턴스별 프로퍼티 (material 복사 방지)
    MaterialPropertyBlock _mpb;
    int _alphaId;
    float _wallAlpha = 1f;
    float _wallTargetAlpha = 1f;
    float _propAlpha;
    float _propTargetAlpha;
    int _playerInsideCount; // 중첩 트리거 대응

    // 프랍 캐시
    Renderer[] _propRenderers;
    Collider[] _propColliders;
    bool _propCacheValid;

    /// <summary>현재 플레이어가 건물 내부에 있는지</summary>
    public bool IsPlayerInside => _playerInsideCount > 0;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _alphaId = Shader.PropertyToID(alphaProperty);

        // 내부 프랍 루트 자동 탐색
        if (interiorPropRoot == null && autoCollectProps)
            interiorPropRoot = FindInteriorPropRoot();

        // 초기 프랍 상태
        _propAlpha = startPropsHidden ? 0f : 1f;
        _propTargetAlpha = _propAlpha;

        RebuildPropCache();
        ApplyPropAlpha(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInsideCount++;

        // 벽 → 투명, 프랍 → 표시
        _wallTargetAlpha = fadeAlpha;
        _propTargetAlpha = 1f;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInsideCount = Mathf.Max(0, _playerInsideCount - 1);

        if (_playerInsideCount == 0)
        {
            // 벽 → 불투명, 프랍 → 숨김
            _wallTargetAlpha = 1f;
            _propTargetAlpha = 0f;
        }
    }

    void Update()
    {
        float dt = fadeSpeed * Time.deltaTime;
        bool changed = false;

        // 벽 알파
        if (!Mathf.Approximately(_wallAlpha, _wallTargetAlpha))
        {
            _wallAlpha = Mathf.MoveTowards(_wallAlpha, _wallTargetAlpha, dt);
            ApplyWallAlpha();
            changed = true;
        }

        // 프랍 알파
        if (!Mathf.Approximately(_propAlpha, _propTargetAlpha))
        {
            _propAlpha = Mathf.MoveTowards(_propAlpha, _propTargetAlpha, dt);
            ApplyPropAlpha(false);
            changed = true;
        }
    }

    // ══════════════════════════════════
    //  벽/천장 페이드
    // ══════════════════════════════════

    void ApplyWallAlpha()
    {
        if (fadeRenderers == null) return;

        for (int i = 0; i < fadeRenderers.Length; i++)
        {
            var r = fadeRenderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_alphaId, _wallAlpha);
            r.SetPropertyBlock(_mpb);

            // 완전 투명이면 렌더러 끄기 (성능)
            r.enabled = _wallAlpha > 0.01f;
        }
    }

    // ══════════════════════════════════
    //  내부 프랍 가시성
    // ══════════════════════════════════

    void ApplyPropAlpha(bool immediate)
    {
        if (_propRenderers == null || _propRenderers.Length == 0) return;

        bool visible = _propAlpha > 0.01f;

        for (int i = 0; i < _propRenderers.Length; i++)
        {
            var r = _propRenderers[i];
            if (r == null) continue;

            r.enabled = visible;

            if (visible && r.sharedMaterial != null)
            {
                r.GetPropertyBlock(_mpb);

                if (r.sharedMaterial.HasProperty(_alphaId))
                {
                    _mpb.SetFloat(_alphaId, _propAlpha);
                }
                else
                {
                    // _Color.a 또는 _BaseColor.a 폴백
                    int colorId = r.sharedMaterial.HasProperty("_BaseColor")
                        ? Shader.PropertyToID("_BaseColor")
                        : Shader.PropertyToID("_Color");

                    if (r.sharedMaterial.HasProperty(colorId))
                    {
                        Color c = r.sharedMaterial.GetColor(colorId);
                        c.a = _propAlpha;
                        _mpb.SetColor(colorId, c);
                    }
                }
                r.SetPropertyBlock(_mpb);
            }
        }

        // 콜라이더 토글
        if (disablePropColliders && _propColliders != null)
        {
            for (int i = 0; i < _propColliders.Length; i++)
            {
                if (_propColliders[i] != null)
                    _propColliders[i].enabled = visible;
            }
        }
    }

    // ══════════════════════════════════
    //  캐시 & 탐색
    // ══════════════════════════════════

    /// <summary>프랍 렌더러/콜라이더 캐시 재구축 (프랍 동적 추가 시 호출)</summary>
    public void RebuildPropCache()
    {
        if (interiorPropRoot != null)
        {
            _propRenderers = interiorPropRoot.GetComponentsInChildren<Renderer>(true);
            if (disablePropColliders)
                _propColliders = interiorPropRoot.GetComponentsInChildren<Collider>(true);
        }
        else
        {
            _propRenderers = System.Array.Empty<Renderer>();
            _propColliders = System.Array.Empty<Collider>();
        }
        _propCacheValid = true;
    }

    /// <summary>외부에서 프랍 표시/숨기기 강제</summary>
    public void ForcePropsVisible(bool visible, bool immediate = false)
    {
        _propTargetAlpha = visible ? 1f : 0f;
        if (immediate)
        {
            _propAlpha = _propTargetAlpha;
            ApplyPropAlpha(true);
        }
    }

    /// <summary>내부 프랍 루트를 자동으로 찾기</summary>
    Transform FindInteriorPropRoot()
    {
        // 부모에서 "Interior" / "Props" / "Furniture" 이름을 가진 자식 탐색
        Transform search = transform.parent != null ? transform.parent : transform;

        string[] names = { "Interior", "interior", "Props", "props", "Furniture", "furniture", "Inside", "inside" };

        for (int i = 0; i < search.childCount; i++)
        {
            var child = search.GetChild(i);
            foreach (var n in names)
            {
                if (child.name.Contains(n))
                    return child;
            }
        }
        return null;
    }

    /// <summary>에디터에서 트리거 영역 + 프랍 영역 시각화</summary>
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        // 트리거 영역 (파란색)
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // 프랍 루트 하위 렌더러 영역 (주황색)
        Transform root = interiorPropRoot;
        if (root == null && autoCollectProps)
            root = FindInteriorPropRoot();

        if (root != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.3f);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
    }
}
