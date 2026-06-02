using UnityEngine;

/// <summary>
/// 무기별 전투 데이터. ItemData(Weapon)가 참조.
/// 장착 시 TopDownPlayer의 약공 콤보·강공·이동/스태미너 보정을 이 무기 것으로 교체.
/// 비어있는 항목은 맨손(기본) 값을 사용.
/// </summary>
[CreateAssetMenu(menuName = "Top-Down Combat/Weapon Data", fileName = "Weapon_")]
public class WeaponData : ScriptableObject
{
    [Tooltip("무기 식별자 (pipe, knife, bat 등)")]
    public string weaponId;

    [Header("공격 (비우면 맨손 기본)")]
    [Tooltip("약공격 콤보 체인")]
    public AttackComboData lightCombo;
    public AttackData heavyAttack;
    public AttackData heavyFullAttack;

    [Header("보정")]
    [Tooltip("이동속도 배율 (무거운 무기 = 낮게)")]
    [Range(0.5f, 1.3f)] public float moveSpeedMult = 1f;
    [Tooltip("스태미너 소모 배율 (무거운 무기 = 높게)")]
    [Range(0.5f, 2f)] public float staminaCostMult = 1f;
}
