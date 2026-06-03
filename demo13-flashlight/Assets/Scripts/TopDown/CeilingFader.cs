using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 천장(지붕) 컷어웨이 — 플레이어가 건물(트리거) 안에 들어오면 지붕 스프라이트 알파를 부드럽게 뺀다(좀보이드/타르코프식).
///
/// 동작:
///  - 자기 트리거(빌더가 footprint 크기로 부착)에 "Player" 태그가 들어오면 _playerInside.
///  - 같은 groupId 끼리 한 건물로 묶여, 그룹 중 한 조각에만 플레이어가 들어와도 그룹 전체가 함께 페이드.
///  - groupId가 비면 이 조각 단독 판정.
///
/// 부착: Prop2DBuilder가 Ceiling 카테고리 프롭에 트리거 콜라이더 + 이 컴포넌트를 자동으로 붙인다.
/// 에디트 모드에선 Update가 안 돌아 지붕이 항상 보임(편집용). 런타임에서만 페이드.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CeilingFader : MonoBehaviour
{
    [Tooltip("건물 그룹 ID — 같은 ID끼리 함께 페이드. 비우면 단독.")]
    public string groupId = "";
    [Range(0f, 1f)]
    [Tooltip("플레이어가 안에 있을 때 알파(0=투명, 0.3=반투명).")]
    public float hiddenAlpha = 0f;
    [Tooltip("페이드 속도(알파/초).")]
    public float fadeSpeed = 6f;

    // groupId → 해당 그룹의 모든 페이더 (그룹 통합 판정용)
    static readonly Dictionary<string, List<CeilingFader>> _groups = new();

    bool _playerInside;
    SpriteRenderer[] _renderers;
    float _alpha = 1f;

    void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _alpha = 1f;     // 시작은 항상 불투명(에디터 반투명 프리뷰가 플레이로 새는 것 방지)
        ApplyAlpha();
    }

    void OnEnable()
    {
        if (string.IsNullOrEmpty(groupId)) return;
        if (!_groups.TryGetValue(groupId, out var list))
        {
            list = new List<CeilingFader>();
            _groups[groupId] = list;
        }
        if (!list.Contains(this)) list.Add(this);
    }

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(groupId) && _groups.TryGetValue(groupId, out var list))
            list.Remove(this);
    }

    void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) _playerInside = true; }
    void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) _playerInside = false; }

    /// <summary>이 조각(또는 그룹) 안에 플레이어가 있나.</summary>
    bool AnyInsideGroup()
    {
        if (string.IsNullOrEmpty(groupId)) return _playerInside;
        if (_groups.TryGetValue(groupId, out var list))
            foreach (var f in list)
                if (f != null && f._playerInside) return true;
        return false;
    }

    void Update()
    {
        float target = AnyInsideGroup() ? hiddenAlpha : 1f;
        if (Mathf.Approximately(_alpha, target)) return;
        _alpha = Mathf.MoveTowards(_alpha, target, Mathf.Max(0.1f, fadeSpeed) * Time.deltaTime);
        ApplyAlpha();
    }

    void ApplyAlpha()
    {
        if (_renderers == null) return;
        foreach (var sr in _renderers)
        {
            if (sr == null) continue;
            var c = sr.color;
            c.a = _alpha;
            sr.color = c;
        }
    }

    // 컷어웨이 감지 영역(트리거)을 씬에서 시각화 — 선택 시 청록 박스. 입구/밑둥까지 덮였는지 보고 조절.
    void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Vector3 c = transform.TransformPoint(box.offset);
        Vector3 s = Vector3.Scale((Vector3)box.size, transform.lossyScale);
        Gizmos.matrix = Matrix4x4.TRS(c, transform.rotation, Vector3.one);
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.12f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(s.x, s.y, 0.01f));
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(s.x, s.y, 0.01f));
    }
}
