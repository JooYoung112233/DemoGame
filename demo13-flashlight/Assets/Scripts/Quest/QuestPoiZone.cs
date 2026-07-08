using UnityEngine;

/// <summary>
/// 탐색형 의뢰(ReachPoint) POI 존 — 플레이어가 들어오면 `poiId` 목표를 1 진행시킨다.
/// 게시판 탐색 의뢰(BD-12/13/14/16/18, BQ-C02/D03/E03/A01)의 `poi_*` 대상과 연결.
///
/// 설계(2026-07-08):
///  - **입장 시 진행 + 퇴장 시 재무장**(oneShot 아님). 의뢰 수주 *전에* 지나가면 소비돼
///    나중에 완수 못 하는 버그를 피하려 debounce만 하고 영구 소비하지 않는다.
///  - `QuestManager.UpdateObjective`가 매칭 활성 의뢰의 미완 목표만 증가(requiredCount 상한,
///    완료 목표 스킵)하므로 반복 입장은 목표를 채우기만 하고 무한 누적 없음.
///  - 요구 수량 2(POI 2곳)는 **서로 다른 존 2개**로 채우는 설계 — 같은 존 재입장 반복도
///    수량을 채우긴 하나(그레이박스 허용), 정상 플레이는 서로 다른 지점 방문.
///  - 2D 트리거(Collider2D isTrigger). 좀보이드식 top-down 2D 규약.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuestPoiZone : MonoBehaviour
{
    [Tooltip("탐색 의뢰가 참조하는 POI 식별자 (예: poi_warehouse_noise). 의뢰 SO의 ReachPoint targetId와 일치해야 함.")]
    public string poiId;

    [Tooltip("도달 시 표시 이름(토스트/로그용). 비우면 poiId 사용.")]
    public string displayName;

    bool _inside;   // debounce — 존 안에 머무는 동안 중복 발화 방지(퇴장 시 재무장)

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_inside || !other.CompareTag("Player")) return;
        _inside = true;
        TryProgress();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) _inside = false;
    }

    void TryProgress()
    {
        if (string.IsNullOrEmpty(poiId) || QuestManager.Instance == null) return;

        // 이 POI를 필요로 하는 활성 의뢰가 있을 때만 진행/피드백(무관하면 조용히 무시 = 미래 수주 대비 재무장 유지).
        bool wanted = QuestManager.Instance.HasActiveObjective(ObjectiveType.ReachPoint, poiId);
        QuestManager.Instance.UpdateObjective(ObjectiveType.ReachPoint, poiId, 1);

        if (wanted)
        {
            string label = string.IsNullOrEmpty(displayName) ? poiId : displayName;
            ToastManager.Show($"정찰: {label} 확인", ToastManager.ToastType.Success);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        var col = GetComponent<Collider2D>();
        if (col != null) Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.bounds.size);
    }
}
