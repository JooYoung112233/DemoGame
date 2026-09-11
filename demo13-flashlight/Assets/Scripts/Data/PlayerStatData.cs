using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 전투/이동 스탯. StatDB에 포함.
/// </summary>
[System.Serializable]
public class PlayerStatData
{
    [Header("체력")]
    public float maxHp = 100f;

    [Header("이동")]
    public float moveSpeed = 1.2f;
    public float sprintSpeedMultiplier = 3f;
    public float crouchSpeedMultiplier = 0.5f;

    [Header("이동 감각 (걷는 느낌)")]
    [Tooltip("가속도(유닛/초²). 작을수록 묵직, 0=즉시(미끄럼 느낌).")]
    public float moveAccel = 45f;
    [Tooltip("감속도(유닛/초²). 클수록 정지가 또렷, 0=즉시 정지.")]
    public float moveDecel = 80f;

    [Header("달리기")]
    public float sprintStaminaCost = 12f;
    public float sprintMinStamina = 10f;

    [Header("모션(애니 속도/거리)")]
    [Tooltip("모션별 애니 재생 속도 + 이동거리. 키: idle/walk/run/crouch/crouch_walk/attack/roll. " +
             "run.distance=최대 달리기 거리(0=무제한), roll.distance=구르기 거리(0=구르기 기본값 dodgeDistance 사용).")]
    public List<MotionStat> motions = new List<MotionStat>
    {
        new MotionStat { anim = "idle",        animSpeed = 1f },
        new MotionStat { anim = "walk",        animSpeed = 1f },
        new MotionStat { anim = "run",         animSpeed = 1f, distance = 0f },
        new MotionStat { anim = "crouch",      animSpeed = 1f },
        new MotionStat { anim = "crouch_walk", animSpeed = 1f },
        new MotionStat { anim = "attack",      animSpeed = 1f },
        new MotionStat { anim = "roll",        animSpeed = 1f, distance = 0f },
    };

    [Header("약공격")]
    public float lightDamage = 8f;
    public float lightRange = 2f;
    public float lightStaminaCost = 6f;
    public float lightGroggy = 5f;
    public float lightCooldown = 0.4f;
    public int lightComboMax = 3;
    public float lightComboWindow = 0.6f;
    public float lightCombo2Damage = 9f;
    public float lightCombo2Groggy = 6f;
    public float lightCombo3Damage = 12f;
    public float lightCombo3Groggy = 8f;
    public float lightCombo3StaminaCost = 8f;

    [Header("강공격")]
    public float heavyDamage = 20f;
    public float heavyRange = 2.5f;
    public float heavyStaminaCost = 22f;
    public float heavyGroggy = 25f;
    public float heavyChargeTime = 0.6f;
    public float heavyMaxCharge = 1.5f;
    public float heavyFullDamage = 32f;
    public float heavyFullStaminaCost = 35f;
    public float heavyFullGroggy = 45f;
    public float heavyCooldown = 0.8f;

    [Header("구르기")]
    public float dodgeStaminaCost = 15f;
    public float dodgeDistance = 3f;
    public float dodgeDuration = 0.3f;
    public float dodgeInvincibleDuration = 0.2f;
    public float dodgeCooldown = 0.5f;

    [Header("스태미너")]
    public float maxStamina = 100f;
    public float staminaRegen = 15f;
    public float staminaRegenDelay = 1.0f;
    public float exhaustionDuration = 0.8f;
}
