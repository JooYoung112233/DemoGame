using System.Collections.Generic;
using UnityEngine;

public enum HitboxShape { Box, Circle }

/// <summary>
/// 공격 1종의 히트박스 타임라인.
/// duration(초) 동안 정규화 시간(0~1)에 따라 각 HitWindow가 활성/비활성.
/// 캐릭터(플레이어/적)의 공격마다 하나씩.
/// </summary>
[CreateAssetMenu(menuName = "Top-Down Combat/Attack Data", fileName = "Attack_")]
public class AttackData : ScriptableObject
{
    [Tooltip("공격 식별자 (light1, light2, heavy, enemy_basic 등)")]
    public string attackId = "light1";

    [Tooltip("공격 전체 길이(초). 히트 윈도우는 이 길이에 대한 비율(0~1)로 정의")]
    [Min(0.05f)] public float duration = 0.4f;

    [Tooltip("기본 데미지 (윈도우별 damageMult로 배율 적용)")]
    public float damage = 8f;

    [Tooltip("기본 그로기 (윈도우별 groggyMult로 배율 적용)")]
    public float groggy = 5f;

    public List<HitWindow> windows = new List<HitWindow> { new HitWindow() };
}

/// <summary>
/// 히트박스 활성 구간 1개. 정규화 시간 [startNorm, endNorm] 동안 켜짐.
/// offset은 캐릭터 정면(facing) 기준 로컬 좌표 — x=전방, y=좌측.
/// </summary>
[System.Serializable]
public class HitWindow
{
    public string label = "hit";

    [Header("타이밍 (공격 길이 대비 0~1)")]
    [Range(0f, 1f)] public float startNorm = 0.2f;
    [Range(0f, 1f)] public float endNorm   = 0.5f;

    [Header("형태 / 위치 (facing 기준 로컬)")]
    public HitboxShape shape = HitboxShape.Box;
    [Tooltip("x=전방 거리, y=좌측 오프셋")]
    public Vector2 offset = new Vector2(1f, 0f);
    [Tooltip("Box 크기 (가로=전방폭, 세로=측면폭)")]
    public Vector2 boxSize = new Vector2(1.2f, 0.9f);
    [Tooltip("Box 추가 회전(도). facing에 더해짐")]
    [Range(-180f, 180f)] public float angle = 0f;
    [Tooltip("Circle 반경")]
    public float radius = 0.6f;

    [Header("배율")]
    public float damageMult = 1f;
    public float groggyMult = 1f;

    public bool IsActiveAt(float norm) => norm >= startNorm && norm <= endNorm;
}
