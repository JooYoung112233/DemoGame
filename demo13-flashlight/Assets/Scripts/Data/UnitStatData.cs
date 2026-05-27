using UnityEngine;

/// <summary>
/// 유닛(적/NPC) 통합 스탯. StatDB.units 리스트에 포함.
/// 기존 CombatData.EnemyStats + EnemyData 통합.
/// id(키)로 조회: StatDB.Instance.GetUnit("bandit_melee")
/// </summary>
[System.Serializable]
public class UnitStatData
{
    // ===== 식별 =====
    [Header("ID")]
    public string id = "new_unit";
    public string displayName = "New Unit";

    // ===== 비주얼 =====
    [Header("비주얼")]
    public float scale = 2f;
    public Color tintColor = new Color(1f, 0.7f, 0.7f, 1f);
    public Color shadowColor = new Color(0f, 0f, 0f, 0.4f);
    public bool useGlow = false;
    public Color glowColor = new Color(1f, 0.3f, 0.3f);
    public float glowIntensity = 3f;
    public float glowRange = 3f;
    public bool useTrailParticle = false;
    public Color trailColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    [Header("프리팹")]
    public GameObject generatedPrefab;

    // ===== 전투 =====
    [Header("체력")]
    public float maxHp = 60f;

    [Header("공격")]
    public float attackDamage = 15f;
    public float attackRange = 1.5f;
    public float attackSpeed = 0.67f;
    public float attackWindup = 0.8f;
    public bool canBeCancelled = true;

    // ===== 그로기 =====
    [Header("그로기")]
    public float maxGroggy = 100f;
    public float groggyDecay = 8f;
    public float groggyStunDuration = 2.0f;

    // ===== 이동 =====
    [Header("이동")]
    public float moveSpeed = 2.5f;
    public float patrolSpeed = 1.2f;

    // ===== 감지 =====
    [Header("감지")]
    public float detectRange = 8f;
    public float loseRange = 12f;
    public float patrolRadius = 5f;

    // ===== AI 행동 =====
    [Header("AI 행동")]
    public float patrolWaitTime = 2f;
    public float hitStunDuration = 0.3f;

    // ===== 보상 =====
    [Header("보상")]
    public int expReward = 10;
    public int goldReward = 5;

    // ===== 계산 프로퍼티 =====
    public float DPS => attackDamage * attackSpeed;
    public float AttackCooldown => 1f / Mathf.Max(attackSpeed, 0.1f);
    public float EffectiveHP => maxHp;

    /// <summary>프리셋: 근접 밴딧</summary>
    public static UnitStatData PresetMelee()
    {
        return new UnitStatData
        {
            id = "bandit_melee",
            displayName = "Bandit Melee",
            tintColor = new Color(0.9f, 0.7f, 0.6f),
            maxHp = 40f, attackDamage = 10f, attackRange = 1.5f, attackSpeed = 0.8f,
            attackWindup = 0.7f,
            moveSpeed = 2.5f, patrolSpeed = 1f,
            detectRange = 7f, loseRange = 11f, patrolRadius = 4f,
            maxGroggy = 80f, groggyDecay = 10f, groggyStunDuration = 2.5f,
        };
    }

    /// <summary>프리셋: 원거리 밴딧</summary>
    public static UnitStatData PresetRanged()
    {
        return new UnitStatData
        {
            id = "bandit_ranged",
            displayName = "Bandit Ranged",
            tintColor = new Color(0.6f, 0.8f, 0.9f),
            maxHp = 30f, attackDamage = 12f, attackRange = 6f, attackSpeed = 0.5f,
            attackWindup = 1.0f,
            moveSpeed = 2f, patrolSpeed = 0.8f,
            detectRange = 10f, loseRange = 14f, patrolRadius = 3f,
            maxGroggy = 60f, groggyDecay = 8f, groggyStunDuration = 2f,
        };
    }

    /// <summary>프리셋: 탱크</summary>
    public static UnitStatData PresetTank()
    {
        return new UnitStatData
        {
            id = "bandit_tank",
            displayName = "Bandit Tank",
            tintColor = new Color(0.5f, 0.5f, 0.6f),
            scale = 2.5f,
            maxHp = 150f, attackDamage = 22f, attackRange = 1.8f, attackSpeed = 0.4f,
            attackWindup = 1.2f, canBeCancelled = false,
            moveSpeed = 1.5f, patrolSpeed = 0.6f,
            detectRange = 8f, loseRange = 12f, patrolRadius = 4f,
            maxGroggy = 200f, groggyDecay = 5f, groggyStunDuration = 1.5f,
        };
    }

    /// <summary>프리셋: 보스</summary>
    public static UnitStatData PresetBoss()
    {
        return new UnitStatData
        {
            id = "boss_01",
            displayName = "Boss",
            tintColor = new Color(1f, 0.3f, 0.3f),
            scale = 3f,
            useGlow = true, glowColor = new Color(1f, 0.2f, 0.2f), glowIntensity = 5f, glowRange = 4f,
            maxHp = 300f, attackDamage = 30f, attackRange = 2.5f, attackSpeed = 0.5f,
            attackWindup = 0.8f, canBeCancelled = false,
            moveSpeed = 2f, patrolSpeed = 1f,
            detectRange = 12f, loseRange = 18f, patrolRadius = 6f,
            maxGroggy = 300f, groggyDecay = 3f, groggyStunDuration = 1.2f,
            expReward = 100, goldReward = 50,
        };
    }
}
