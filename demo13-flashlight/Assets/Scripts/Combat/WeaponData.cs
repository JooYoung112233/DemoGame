using UnityEngine;

/// <summary>
/// 무기 하나의 전투 데이터 — **총 수치의 유일한 출처**(2026-09-11 사용자 "한 곳으로 전부", docs/combat.md §무기 구성 결정).
/// ItemData(Weapon)가 참조하고, 총기 밴딧도 StatDB 유닛에서 같은 에셋을 참조한다(적 전용 배율만 StatDB).
/// 근접 무기면 근접 칸만, 총이면 총 칸만 쓴다 — 인스펙터(Editor/WeaponDataEditor)도 해당 칸만 보여 준다.
/// 탄창 장탄수는 탄창 아이템, 탄 스펙(피해·관통·탄속·퍼짐 배율)은 탄약 아이템에 있다 — 총과 따로 바꿔 끼우는 물건이라서.
/// </summary>
[CreateAssetMenu(menuName = "Top-Down Combat/Weapon Data", fileName = "Weapon_")]
public class WeaponData : ScriptableObject
{
    [Header("공통")]
    [Tooltip("무기 식별자 (pipe, knife, bat 등)")]
    public string weaponId;

    [Tooltip("켜면 이 무기는 총이다. 좌클릭=사격 / 우클릭=조준 / R=장전.\n끄면 근접 — 좌클릭 약공 / 우클릭 홀드 차징 강공.")]
    public bool isRanged = false;

    [Tooltip("이동속도 배율 (무거운 무기 = 낮게)")]
    [Range(0.5f, 1.3f)] public float moveSpeedMult = 1f;

    // ── 근접 ─────────────────────────────────────────────────────────────
    [Header("── 근접 ──")]
    [Tooltip("장검 양손 걷기·베기 모션을 사용합니다.")]
    public bool useTwoHandSwordAnimations;

    [Tooltip("양손 방망이 잡기와 1타 스윙 모션을 사용합니다.")]
    public bool useTwoHandBatAnimations;

    [Tooltip("약공격 콤보 체인. 비우면 StatDB 플레이어 기본 근접 공격(맨손 공격은 2026-09-11 폐기 — 무기가 없으면 공격 없음).")]
    public AttackComboData lightCombo;
    [Tooltip("비우면 StatDB 플레이어 기본 강공격.")]
    public AttackData heavyAttack;
    public AttackData heavyFullAttack;

    [Tooltip("스태미너 소모 배율 (무거운 무기 = 높게). 총은 스태미너를 쓰지 않는다.")]
    [Range(0.5f, 2f)] public float staminaCostMult = 1f;

    // ── 총 (2026-07-29) ──────────────────────────────────────────────────
    public enum FireMode { Single, Auto, Burst }
    public enum FirearmStance { Pistol, AssaultRifle }

    [Header("── 총 ──")]
    [Tooltip("총 종류 — 플레이어 3D 총기 모양·모션(Resources/Characters/Firearms/Pistol|Rifle)과\n총기 밴딧 모델(Characters/BanditPistol|BanditRifle)을 고른다.")]
    public FirearmStance firearmStance = FirearmStance.Pistol;

    [Tooltip("구경(예: 9x19). **탄창의 magCaliber와 같아야** 장착된다.")]
    public string caliber = "9x19";

    [Tooltip("격발 방식. Single=클릭마다 1발, Auto=누르는 동안 연사. (Burst는 아직 Single로 동작)")]
    public FireMode fireMode = FireMode.Single;

    [Tooltip("분당 발사수(RPM). 600 = 초당 10발.")]
    public float rpm = 400f;

    [Tooltip("탄 1발 기본 데미지. 탄종 배율(ammoDamageMult)이 곱해진다. 총기 밴딧은 StatDB rangedDamageMult가 곱해진다.")]
    public float damage = 18f;

    [Tooltip("적중 시 그로기 누적치.")]
    public float groggy = 12f;

    [Tooltip("총알 속도(m/s). 탑다운에서 눈에 보여야 하므로 실제보다 훨씬 느리게 잡는다. 총기 밴딧은 StatDB rangedBulletSpeedMult가 곱해진다.")]
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
