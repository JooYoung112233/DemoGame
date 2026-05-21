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

        [Header("공격")]
        public float attackDamage = 25f;
        public float attackRange = 3f;
        public float attackSpeed = 1.2f; // 초당 공격 횟수

        [Header("이동")]
        public float moveSpeed = 5f;
    }

    [System.Serializable]
    public class EnemyStats
    {
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
    }

    public PlayerStats player = new PlayerStats();
    public EnemyStats enemy = new EnemyStats();
}
