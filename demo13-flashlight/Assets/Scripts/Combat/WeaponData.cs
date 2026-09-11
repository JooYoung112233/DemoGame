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

    [Header("3D 모션")]
    [Tooltip("장검 양손 걷기·베기 모션을 사용합니다. 다른 무기와 맨손에는 적용하지 않습니다.")]
    public bool useTwoHandSwordAnimations;

    [Tooltip("양손 방망이 잡기와 1타 스윙 모션을 사용합니다.")]
    public bool useTwoHandBatAnimations;

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

    // ── 총기 (2026-07-29) ────────────────────────────────────────────────
    //  근접 필드와 같은 SO에 둔다 — 무기 하나가 근접이면서 총인 일은 없으므로 타입을 쪼갤 이유가 없다.
    //  isRanged가 켜지면 TopDownPlayer의 좌클릭이 '휘두르기' 대신 '사격'으로 갈린다.
    public enum FireMode { Single, Auto, Burst }

    [Header("── 총기 ──")]
    [Tooltip("켜면 이 무기는 총이다. 좌클릭=사격 / 우클릭=조준 / R=장전.")]
    public bool isRanged = false;

    public enum FirearmStance { Pistol, AssaultRifle }
    [Tooltip("3D 총기 모션/모델. 기존 권총은 기본값 Pistol을 사용합니다.")]
    public FirearmStance firearmStance = FirearmStance.Pistol;

    [Tooltip("구경(예: 9x19). **탄창의 magCaliber와 같아야** 장착된다.")]
    public string caliber = "9x19";

    [Tooltip("격발 방식. Single=클릭마다 1발, Auto=누르는 동안 연사.")]
    public FireMode fireMode = FireMode.Single;

    [Tooltip("분당 발사수(RPM). 600 = 초당 10발.")]
    public float rpm = 400f;

    [Tooltip("탄 1발 기본 데미지. 탄종 배율(ammoDamageMult)이 곱해진다.")]
    public float damage = 18f;

    [Tooltip("적중 시 그로기 누적치.")]
    public float groggy = 12f;

    [Tooltip("총알 속도(m/s). 탑다운에서 눈에 보여야 하므로 실제보다 훨씬 느리게 잡는다.")]
    public float projectileSpeed = 42f;

    [Tooltip("유효사거리(m). 넘으면 총알이 사라진다. 조준경 partRangeBonus가 더해진다.")]
    public float effectiveRange = 16f;

    [Tooltip("허리쏴 탄퍼짐(도, 반각). 조준하면 adsSpreadDeg로 좁아진다.")]
    public float hipSpreadDeg = 7f;
    [Tooltip("조준 시 탄퍼짐(도, 반각).")]
    public float adsSpreadDeg = 1.6f;

    [Tooltip("한 발 쏠 때마다 누적되는 탄퍼짐(도). 연사하면 벌어진다.")]
    public float recoilPerShot = 2.2f;
    [Tooltip("초당 회복되는 누적 탄퍼짐(도). 손을 떼면 다시 모인다.")]
    public float recoilRecover = 9f;
    [Tooltip("누적 탄퍼짐 상한(도).")]
    public float recoilMax = 11f;

    [Tooltip("장전(탄창 교체)에 걸리는 시간(초).")]
    public float reloadSeconds = 2.2f;

    [Tooltip("조준 중 이동속도 배율.")]
    [Range(0.2f, 1f)] public float adsMoveMult = 0.55f;
}
