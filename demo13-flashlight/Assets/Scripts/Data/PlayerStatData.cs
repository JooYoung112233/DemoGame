using UnityEngine;

/// <summary>
/// 플레이어 전투/이동 스탯. StatDB에 포함.
/// 기존 CombatData.PlayerStats를 대체.
/// </summary>
[System.Serializable]
public class PlayerStatData
{
    [Header("체력")]
    public float maxHp = 100f;

    [Header("이동")]
    public float moveSpeed = 5f;
    public float sprintSpeedMultiplier = 1.6f;
    public float crouchSpeedMultiplier = 0.5f;

    [Header("달리기")]
    public float sprintStaminaCost = 12f;
    public float sprintMinStamina = 10f;

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
