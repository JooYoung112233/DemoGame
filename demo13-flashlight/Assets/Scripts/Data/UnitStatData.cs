using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적별 전리품 드랍 한 줄 — itemId × 확률 × 수량범위. UnitStatData.drops 리스트에 포함.
/// 밸런스 에디터(Control Panel ▸ StatDB ▸ Units ▸ 전리품 드랍)에서 직접 편집.
/// </summary>
[System.Serializable]
public class EnemyDropEntry
{
    [Tooltip("드랍 아이템 itemId (ItemDatabase 기준).")]
    public string itemId;
    [Tooltip("드랍 확률 0~1.")]
    [Range(0f, 1f)] public float chance = 0.5f;
    [Tooltip("최소 수량.")]
    [Min(1)] public int minQty = 1;
    [Tooltip("최대 수량.")]
    [Min(1)] public int maxQty = 1;
}

/// <summary>
/// 유닛(적/NPC) 통합 스탯. StatDB.units 리스트에 포함.
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

    // ===== 전리품 드랍 (적별 전용 테이블) =====
    [Header("전리품 드랍 (비우면 지역 루트로 폴백)")]
    [Tooltip("적 처치 시 굴리는 드랍 항목. 비면 지역(Ground) 루트로 폴백, 채우면 이 테이블이 우선. EnemyController.DropLoot가 읽음.")]
    public List<EnemyDropEntry> drops = new List<EnemyDropEntry>();

    // ===== 모션(애니 속도/거리) — 플레이어와 동일 규칙 =====
    // ⚠️ 데이터만 준비됨. 적/NPC 애니가 아직 없어 와이어링 안 됨(3D 적 애니 도입 시 적용 — docs/3d-migration.md Stage 4).
    [Header("모션(애니 속도/거리)")]
    [Tooltip("모션별 애니 재생 속도 + 이동거리. 키: idle/walk/attack/hit/death 등. (3D 적 애니 도입 시 EnemyController가 사용)")]
    public List<MotionStat> motions = new List<MotionStat>
    {
        new MotionStat { anim = "idle",   animSpeed = 1f },
        new MotionStat { anim = "walk",   animSpeed = 1f },
        new MotionStat { anim = "attack", animSpeed = 1f },
        new MotionStat { anim = "hit",    animSpeed = 1f },
        new MotionStat { anim = "death",  animSpeed = 1f },
    };

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
