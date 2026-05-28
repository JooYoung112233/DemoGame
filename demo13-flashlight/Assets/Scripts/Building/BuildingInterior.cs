using UnityEngine;

/// <summary>
/// 건물 내부 진입 시 앞면 벽 + 천장 알파 페이드.
/// 건물 오브젝트에 Box Collider (Trigger) 와 함께 배치.
///
/// 사용법:
/// 1. 건물에 빈 자식 GO 생성, Box Collider (Is Trigger) 설정
/// 2. BuildingInterior 컴포넌트 추가
/// 3. fadeRenderers 에 앞면 벽/천장 Renderer 할당
/// 4. Player 태그가 트리거에 진입하면 자동 페이드
/// </summary>
public class BuildingInterior : MonoBehaviour
{
    [Header("페이드 대상")]
    [Tooltip("진입 시 투명해질 Renderer 목록 (앞면 벽, 천장 등)")]
    [SerializeField] Renderer[] fadeRenderers;

    [Header("설정")]
    [Tooltip("페이드 목표 알파 (0=완전 투명, 0.15=약간 보임)")]
    [SerializeField] float fadeAlpha = 0.15f;

    [Tooltip("페이드 속도")]
    [SerializeField] float fadeSpeed = 5f;

    [Tooltip("페이드에 사용할 셰이더 프로퍼티 이름")]
    [SerializeField] string alphaProperty = "_Alpha";

    // MaterialPropertyBlock 으로 인스턴스별 프로퍼티 (material 복사 방지)
    MaterialPropertyBlock mpb;
    int alphaId;
    float currentAlpha = 1f;
    float targetAlpha = 1f;
    int playerInsideCount; // 중첩 트리거 대응

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        alphaId = Shader.PropertyToID(alphaProperty);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInsideCount++;
        targetAlpha = fadeAlpha;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInsideCount = Mathf.Max(0, playerInsideCount - 1);
        if (playerInsideCount == 0)
            targetAlpha = 1f;
    }

    void Update()
    {
        if (fadeRenderers == null || fadeRenderers.Length == 0) return;
        if (Mathf.Approximately(currentAlpha, targetAlpha)) return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        ApplyAlpha();
    }

    void ApplyAlpha()
    {
        for (int i = 0; i < fadeRenderers.Length; i++)
        {
            var r = fadeRenderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(mpb);
            mpb.SetFloat(alphaId, currentAlpha);
            r.SetPropertyBlock(mpb);

            // 완전 투명이면 렌더러 끄기 (성능)
            r.enabled = currentAlpha > 0.01f;
        }
    }

    /// <summary>에디터에서 트리거 영역 시각화</summary>
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
