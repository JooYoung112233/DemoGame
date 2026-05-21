using UnityEngine;

/// <summary>
/// 적 타입별 개별 데이터. ScriptableObject로 저장/관리.
/// 예: SkeletonSoldier, SkeletonArcher, SkeletonBoss 등
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Demo/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string displayName = "New Enemy";
    public float scale = 2f;

    [Header("비주얼")]
    public Color tintColor = new Color(1f, 0.7f, 0.7f, 1f);
    public Color shadowColor = new Color(0f, 0f, 0f, 0.4f);
    public bool useGlow = false;
    public Color glowColor = new Color(1f, 0.3f, 0.3f);
    public float glowIntensity = 3f;
    public float glowRange = 3f;
    public bool useTrailParticle = false;
    public Color trailColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    [Header("프리팹")]
    public GameObject generatedPrefab; // 자동 생성된 프리팹 참조

    [Header("체력")]
    public float maxHp = 60f;

    [Header("공격")]
    public float attackDamage = 15f;
    public float attackRange = 1.5f;
    public float attackSpeed = 0.67f; // 초당 공격 횟수

    [Header("이동")]
    public float moveSpeed = 2.5f;
    public float patrolSpeed = 1.2f;

    [Header("감지")]
    public float detectRange = 8f;
    public float loseRange = 12f;
    public float patrolRadius = 5f;

    [Header("AI 행동")]
    public float patrolWaitTime = 2f;
    public float hitStunDuration = 0.3f;

    [Header("보상")]
    public int expReward = 10;
    public int goldReward = 5;

    // ===== 계산 프로퍼티 =====
    public float DPS => attackDamage * attackSpeed;
    public float AttackCooldown => 1f / Mathf.Max(attackSpeed, 0.1f);
    public float EffectiveHP => maxHp; // 나중에 방어력 추가 시 확장

    /// <summary>CombatData.EnemyStats 형식으로 변환 (하위호환)</summary>
    public CombatData.EnemyStats ToCombatEnemyStats()
    {
        return new CombatData.EnemyStats
        {
            maxHp = this.maxHp,
            attackDamage = this.attackDamage,
            attackRange = this.attackRange,
            attackSpeed = this.attackSpeed,
            moveSpeed = this.moveSpeed,
            patrolSpeed = this.patrolSpeed,
            detectRange = this.detectRange,
            loseRange = this.loseRange,
            patrolRadius = this.patrolRadius,
        };
    }
}
