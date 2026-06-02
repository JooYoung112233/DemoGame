using UnityEngine;

/// <summary>
/// 그림자 방향의 공용 기준(보통 플레이어). FlatShadow가 매 프레임 참조한다.
/// 맵이 먼저 생기고 플레이어(TopDownPlayer)가 늦게 스폰돼도,
/// TopDownPlayer.Instance를 지연 참조하므로 자동으로 연결된다(폴링/FindObjects 없음).
///
/// 우선순위: 명시 등록(SetSource) > TopDownPlayer.Instance > null.
/// null이면 FlatShadow는 manualDirection으로 임시 처리하다가 소스가 생기면 따라간다.
/// </summary>
public static class FlatShadowDirector
{
    static Transform _explicit;

    /// <summary>그림자 기준을 수동 지정(예: 특정 횃불/차량 등). null로 호출하면 해제.</summary>
    public static void SetSource(Transform t) => _explicit = t;

    /// <summary>등록한 소스가 t와 같을 때만 해제(소유자 파괴 시 안전).</summary>
    public static void ClearSource(Transform t) { if (_explicit == t) _explicit = null; }

    static TopDownPlayer _cached;

    /// <summary>현재 그림자 방향 기준 Transform. 없으면 null.</summary>
    public static Transform Source
    {
        get
        {
            if (_explicit != null) return _explicit;
            if (TopDownPlayer.Instance != null) return TopDownPlayer.Instance.transform;
            // 에디터 미리보기/플레이어 미스폰 폴백: 씬에서 직접 찾기(캐시)
            if (_cached == null)
                _cached = Object.FindFirstObjectByType<TopDownPlayer>(FindObjectsInactive.Include);
            return _cached != null ? _cached.transform : null;
        }
    }
}
