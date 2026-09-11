using System.Collections.Generic;
using UnityEngine;

public enum HitboxShape { Box, Circle }

/// <summary>
/// 공격 1종(콤보 1단계)의 프레임 기반 히트박스 타임라인.
/// 총 totalFrames 프레임(fps 기준)으로 재생. 각 HitWindow가 프레임 구간에서 활성.
/// </summary>
[CreateAssetMenu(menuName = "Top-Down Combat/Attack Data", fileName = "Attack_")]
public class AttackData : ScriptableObject
{
    [Tooltip("공격 식별자 (light1, heavy, enemy_basic 등)")]
    public string attackId = "light1";

    [Header("타이밍 (프레임)")]
    [Tooltip("초당 프레임 — 히트박스 타이밍 해상도")]
    [Min(1)] public int fps = 12;
    [Tooltip("공격 전체 프레임 수")]
    [Min(1)] public int totalFrames = 6;
    [Tooltip("이 프레임부터 다음 콤보로 캔슬(연속 입력) 허용. -1이면 캔슬 불가")]
    public int cancelFromFrame = 3;

    [Header("데미지")]
    public float damage = 8f;
    public float groggy = 5f;

    [Header("타격감")]
    [Tooltip("적중 시 히트스탑(짧은 전역 정지). 강공에 권장")]
    public bool hitstop = false;
    [Tooltip("히트스탑 지속(초) — 0.04~0.06 권장")]
    [Min(0f)] public float hitstopDuration = 0.05f;

    public List<HitWindow> windows = new List<HitWindow> { new HitWindow() };

    /// <summary>공격 전체 길이(초).</summary>
    public float Duration => totalFrames / Mathf.Max(1f, fps);

    /// <summary>경과 시간(초) → 현재 프레임.</summary>
    public int FrameAtTime(float t) => Mathf.Clamp(Mathf.FloorToInt(t * fps), 0, totalFrames);
}

/// <summary>
/// 히트박스 활성 구간 1개. 프레임 [startFrame, endFrame] 동안 켜짐.
/// offset은 캐릭터 정면(facing) 기준 로컬 — x=전방, y=좌측.
/// </summary>
[System.Serializable]
public class HitWindow
{
    public string label = "hit";

    [Header("타이밍 (프레임)")]
    [Min(0)] public int startFrame = 1;
    [Min(0)] public int endFrame   = 3;

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

    public bool IsActiveAtFrame(int frame) => frame >= startFrame && frame <= endFrame;
}
