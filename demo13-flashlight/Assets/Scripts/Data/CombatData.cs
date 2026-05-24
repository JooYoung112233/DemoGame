using UnityEngine;

/// <summary>
/// 전투 밸런스 데이터. ScriptableObject로 에디터에서 실시간 조절 가능.
/// </summary>
[CreateAssetMenu(fileName = "CombatData", menuName = "Demo/Combat Data")]
public class CombatData : ScriptableObject
{
    [System.Serializable]
    public class PlayerStats
    {
        [Header("체력")]
        public float maxHp = 100f;

        [Header("약공격")]
        public float lightDamage = 8f;
        public float lightRange = 2f;
        public float lightStaminaCost = 6f;
        public float lightGroggy = 5f;
        public float lightCooldown = 0.4f;        // 약공격 간 간격
        public int lightComboMax = 3;              // 최대 콤보 수
        public float lightComboWindow = 0.6f;      // 콤보 입력 허용 시간
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
        public float heavyChargeTime = 0.6f;       // 최소 차징 시간
        public float heavyMaxCharge = 1.5f;         // 풀차지 시간
        public float heavyFullDamage = 32f;         // 풀차지 피해
        public float heavyFullStaminaCost = 35f;
        public float heavyFullGroggy = 45f;
        public float heavyCooldown = 0.8f;

        [Header("구르기")]
        public float dodgeStaminaCost = 15f;
        public float dodgeDistance = 3f;
        public float dodgeDuration = 0.3f;          // 구르기 소요 시간
        public float dodgeInvincibleDuration = 0.2f; // 무적 시간
        public float dodgeCooldown = 0.5f;

        [Header("스태미너")]
        public float maxStamina = 100f;
        public float staminaRegen = 15f;            // 초당 회복
        public float staminaRegenDelay = 1.0f;      // 소모 후 회복 시작 딜레이
        public float exhaustionDuration = 0.8f;     // 탈진 상태 지속 시간

        [Header("이동")]
        public float moveSpeed = 5f;

        [Header("구버전 호환 (사용하지 않음)")]
        public float attackDamage = 25f;
        public float attackRange = 3f;
        public float attackSpeed = 1.2f;
    }

    [System.Serializable]
    public class EnemyStats
    {
        [Header("체력")]
        public float maxHp = 60f;

        [Header("공격")]
        public float attackDamage = 15f;
        public float attackRange = 1.5f;
        public float attackSpeed = 0.67f;

        [Header("공격 예고")]
        public float attackWindup = 0.8f;           // 예비동작 시간
        public bool canBeCancelled = true;          // 강공격으로 캔슬 가능 여부

        [Header("그로기")]
        public float maxGroggy = 100f;
        public float groggyDecay = 8f;              // 초당 그로기 감소
        public float groggyStunDuration = 2.0f;     // 그로기 상태 지속 시간

        [Header("이동")]
        public float moveSpeed = 2.5f;
        public float patrolSpeed = 1.2f;

        [Header("감지")]
        public float detectRange = 8f;
        public float loseRange = 12f;
        public float patrolRadius = 5f;
    }

    public PlayerStats player = new PlayerStats();
    public EnemyStats enemy = new EnemyStats();
}
