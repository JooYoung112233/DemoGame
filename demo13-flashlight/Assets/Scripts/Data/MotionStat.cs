using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모션(애니) 1개의 튜닝값 — 재생 속도 배율 + 이동/거리(해당 모션만).
/// PlayerStatData/UnitStatData의 `motions` 리스트에 들어가 **논리 모션 키**(idle/walk/run/attack/roll …)로 조회.
/// (스켈레톤 실제 애니 이름이 변형(attack1 등)이라도, 코드가 논리 키로 조회하므로 무관.)
///
/// 규칙(플레이어·적·NPC 동일): 모든 모션은 애니 속도를 따로 조절,
/// 움직이거나 거리가 있는 모션(run/roll 등)은 distance도 따로 둔다.
/// </summary>
[System.Serializable]
public class MotionStat
{
    [Tooltip("논리 모션 키: idle/walk/run/crouch/crouch_walk/attack/roll/hit/death 등. (스켈레톤 애니 이름과 별개)")]
    public string anim = "";
    [Tooltip("애니 재생 속도 배율. 1=기본, >1 빠르게, <1 느리게.")]
    public float animSpeed = 1f;
    [Tooltip("이동/거리(해당 모션만, m). 0=미사용(코드 기본값 사용). 예: run=최대 달리기 거리, roll=구르기 거리.")]
    public float distance = 0f;

    // ── 조회 헬퍼 (리스트에서 키로 찾기) ──
    public static MotionStat Find(List<MotionStat> list, string key)
    {
        if (list == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].anim == key) return list[i];
        return null;
    }

    /// <summary>키의 애니 속도(없으면 fallback).</summary>
    public static float SpeedOf(List<MotionStat> list, string key, float fallback = 1f)
    {
        var m = Find(list, key);
        return m != null ? m.animSpeed : fallback;
    }

    /// <summary>키의 거리(0이면 미설정으로 보고 fallback 반환).</summary>
    public static float DistanceOf(List<MotionStat> list, string key, float fallback = 0f)
    {
        var m = Find(list, key);
        return (m != null && m.distance > 0f) ? m.distance : fallback;
    }
}
