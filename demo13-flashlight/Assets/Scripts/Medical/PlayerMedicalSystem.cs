using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 의료 시스템.
/// 5부위 부상 관리, 디버프 적용, 치료 처리.
/// Player 오브젝트에 부착.
/// </summary>
public class PlayerMedicalSystem : MonoBehaviour
{
    #region 설정

    [Header("출혈 설정")]
    [Tooltip("출혈 1.0 심각도 기준 초당 HP 감소량")]
    [SerializeField] float bleedDamagePerSec = 3f;

    [Header("골절 설정")]
    [Tooltip("다리 골절 시 이동속도 감소 비율 (0.5 = 50% 감소)")]
    [SerializeField] float legFractureMoveDebuff = 0.5f;

    [Tooltip("팔 골절 시 공격속도 감소 비율")]
    [SerializeField] float armFractureAtkDebuff = 0.4f;

    [Header("통증 설정")]
    [Tooltip("통증 1.0 심각도 기준 스태미너 회복 감소 비율")]
    [SerializeField] float painStaminaDebuff = 0.5f;

    [Tooltip("통증 기본 지속시간 (초)")]
    [SerializeField] float painDefaultDuration = 60f;

    [Header("치료")]
    [Tooltip("치료 중 이동 불가")]
    [SerializeField] bool immobileDuringHeal = true;

    #endregion

    #region 상태

    // 5부위
    public BodyPart Head { get; private set; }
    public BodyPart Torso { get; private set; }
    public BodyPart Arms { get; private set; }
    public BodyPart LeftLeg { get; private set; }
    public BodyPart RightLeg { get; private set; }

    BodyPart[] allParts;

    // 치료 진행
    bool isHealing;
    float healTimer;
    float healDuration;
    BodyPart healTarget;
    InjuryType healInjuryType;
    float healAmount;
    System.Action onHealComplete;

    // 디버프 캐시 (매 프레임 계산 안 하도록)
    float cachedMoveSpeedMult = 1f;
    float cachedAtkSpeedMult = 1f;
    float cachedStaminaRegenMult = 1f;
    float debuffRecalcTimer;

    // 레퍼런스
    Health health;
    // TODO(TopDownPlayer): PlayerController playerController;

    #endregion

    #region 프로퍼티

    /// <summary>이동속도 배율 (1.0 = 정상)</summary>
    public float MoveSpeedMultiplier => cachedMoveSpeedMult;

    /// <summary>공격속도 배율 (1.0 = 정상)</summary>
    public float AttackSpeedMultiplier => cachedAtkSpeedMult;

    /// <summary>스태미너 회복 배율 (1.0 = 정상)</summary>
    public float StaminaRegenMultiplier => cachedStaminaRegenMult;

    /// <summary>치료 중인지</summary>
    public bool IsHealing => isHealing;
    public bool BlocksMovementWhileHealing => immobileDuringHeal && isHealing;

    /// <summary>치료 진행률 (0~1)</summary>
    public float HealProgress => isHealing ? Mathf.Clamp01(healTimer / healDuration) : 0f;

    /// <summary>어디든 부상이 있는지</summary>
    public bool HasAnyInjury
    {
        get
        {
            for (int i = 0; i < allParts.Length; i++)
                if (allParts[i].IsInjured) return true;
            return false;
        }
    }

    #endregion

    #region 초기화

    void Awake()
    {
        health = GetComponent<Health>();
        // TODO(TopDownPlayer): playerController = GetComponent<PlayerController>();

        // 5부위 생성
        Head = new BodyPart(BodyPartType.Head);
        Torso = new BodyPart(BodyPartType.Torso);
        Arms = new BodyPart(BodyPartType.Arms);
        LeftLeg = new BodyPart(BodyPartType.LeftLeg);
        RightLeg = new BodyPart(BodyPartType.RightLeg);

        allParts = new BodyPart[] { Head, Torso, Arms, LeftLeg, RightLeg };
    }

    #endregion

    #region 업데이트

    void Update()
    {
        float dt = Time.deltaTime;

        // 출혈 DoT 처리
        UpdateBleeding(dt);

        // 통증 시간 감소
        UpdatePainDecay(dt);

        // 디버프 재계산 (0.5초마다)
        debuffRecalcTimer -= dt;
        if (debuffRecalcTimer <= 0)
        {
            RecalculateDebuffs();
            debuffRecalcTimer = 0.5f;
        }

        // 치료 진행
        if (isHealing)
            UpdateHealing(dt);
    }

    void UpdateBleeding(float dt)
    {
        if (health == null) return;

        float totalBleedDmg = 0f;

        for (int i = 0; i < allParts.Length; i++)
        {
            float severity = allParts[i].GetSeverity(InjuryType.Bleeding);
            if (severity > 0)
                totalBleedDmg += bleedDamagePerSec * severity * dt;
        }

        if (totalBleedDmg > 0)
        {
            health.TakeDamage(totalBleedDmg, gameObject, true); // silent damage (no knockback)
        }
    }

    void UpdatePainDecay(float dt)
    {
        for (int i = 0; i < allParts.Length; i++)
        {
            var injuries = allParts[i].injuries;
            for (int j = injuries.Count - 1; j >= 0; j--)
            {
                if (injuries[j].IsTimeBased)
                {
                    injuries[j].duration -= dt;
                    // 시간에 따라 심각도도 서서히 감소
                    if (injuries[j].maxDuration > 0)
                        injuries[j].severity = Mathf.Clamp01(injuries[j].duration / injuries[j].maxDuration);

                    if (injuries[j].duration <= 0)
                        injuries.RemoveAt(j);
                }
            }
        }
    }

    void UpdateHealing(float dt)
    {
        healTimer += dt;

        if (healTimer >= healDuration)
        {
            // 치료 완료
            CompleteHealing();
        }
    }

    #endregion

    #region 디버프 계산

    void RecalculateDebuffs()
    {
        float moveMult = 1f;
        float atkMult = 1f;
        float staminaMult = 1f;

        // 다리 골절 → 이동속도
        if (LeftLeg.HasInjury(InjuryType.Fracture))
            moveMult -= legFractureMoveDebuff * LeftLeg.GetSeverity(InjuryType.Fracture);
        if (RightLeg.HasInjury(InjuryType.Fracture))
            moveMult -= legFractureMoveDebuff * RightLeg.GetSeverity(InjuryType.Fracture);

        // 팔 골절 → 공격속도
        if (Arms.HasInjury(InjuryType.Fracture))
            atkMult -= armFractureAtkDebuff * Arms.GetSeverity(InjuryType.Fracture);

        // 통증 → 스태미너 회복 (모든 부위의 통증 합산)
        float totalPain = 0f;
        for (int i = 0; i < allParts.Length; i++)
            totalPain += allParts[i].GetSeverity(InjuryType.Pain);
        totalPain = Mathf.Clamp01(totalPain); // 최대 1.0으로 캡
        staminaMult -= painStaminaDebuff * totalPain;

        cachedMoveSpeedMult = Mathf.Max(0.2f, moveMult);   // 최소 20%
        cachedAtkSpeedMult = Mathf.Max(0.3f, atkMult);     // 최소 30%
        cachedStaminaRegenMult = Mathf.Max(0.2f, staminaMult); // 최소 20%
    }

    #endregion

    #region 외부 API — 부상 추가

    /// <summary>
    /// 부상 추가 (전투 시스템에서 호출).
    /// </summary>
    public void InflictInjury(BodyPartType part, InjuryType injuryType, float severity = 0.5f)
    {
        BodyPart bp = GetPart(part);
        if (bp == null) return;

        float duration = (injuryType == InjuryType.Pain) ? painDefaultDuration : 0f;
        bp.AddInjury(new Injury(injuryType, severity, duration));

        Debug.Log($"[Medical] {part}에 {injuryType} 발생 (심각도: {severity:F1})");
    }

    /// <summary>랜덤 부위에 부상 추가 (일반 피격용)</summary>
    public void InflictRandomInjury(InjuryType injuryType, float severity = 0.5f)
    {
        // 가중치: 몸통 > 팔/다리 > 머리
        float roll = Random.value;
        BodyPartType part;

        if (roll < 0.1f) part = BodyPartType.Head;
        else if (roll < 0.35f) part = BodyPartType.Arms;
        else if (roll < 0.55f) part = BodyPartType.LeftLeg;
        else if (roll < 0.75f) part = BodyPartType.RightLeg;
        else part = BodyPartType.Torso;

        InflictInjury(part, injuryType, severity);
    }

    /// <summary>피격 시 자동 부상 판정 (데미지 기반)</summary>
    public void OnDamageTaken(float damage, float maxHp)
    {
        float ratio = damage / maxHp;

        // 항상 통증
        InflictRandomInjury(InjuryType.Pain, Mathf.Clamp(ratio * 2f, 0.2f, 0.8f));

        // 15% 이상 데미지: 출혈 확률
        if (ratio > 0.15f && Random.value < 0.6f)
            InflictRandomInjury(InjuryType.Bleeding, Mathf.Clamp(ratio * 1.5f, 0.3f, 1f));

        // 25% 이상 데미지: 골절 확률
        if (ratio > 0.25f && Random.value < 0.3f)
            InflictRandomInjury(InjuryType.Fracture, Mathf.Clamp(ratio, 0.5f, 1f));
    }

    #endregion

    #region 외부 API — 치료

    /// <summary>
    /// 치료 시작.
    /// </summary>
    /// <param name="part">치료할 부위</param>
    /// <param name="injuryType">치료할 부상 종류</param>
    /// <param name="item">사용할 치료 아이템</param>
    /// <returns>치료 시작 성공 여부</returns>
    public bool StartHealing(BodyPartType part, InjuryType injuryType, MedicalItemData item)
    {
        if (isHealing) return false; // 이미 치료 중

        BodyPart bp = GetPart(part);
        if (bp == null || !bp.HasInjury(injuryType)) return false;

        if (!item.CanTreat(injuryType)) return false;

        isHealing = true;
        healTimer = 0f;
        healDuration = item.useTime;
        healTarget = bp;
        healInjuryType = injuryType;
        healAmount = item.healAmount;

        Debug.Log($"[Medical] 치료 시작: {part} {injuryType} ({item.displayName}, {item.useTime}초)");
        return true;
    }

    /// <summary>치료 취소 (이동 등으로 중단)</summary>
    public void CancelHealing()
    {
        if (!isHealing) return;
        isHealing = false;
        healTimer = 0;
        Debug.Log("[Medical] 치료 취소됨");
    }

    void CompleteHealing()
    {
        isHealing = false;

        if (healTarget != null)
        {
            if (healAmount >= 1f)
            {
                // 완전 치료
                healTarget.RemoveInjury(healInjuryType);
            }
            else
            {
                // 부분 치료 (심각도 감소)
                for (int i = 0; i < healTarget.injuries.Count; i++)
                {
                    if (healTarget.injuries[i].type == healInjuryType)
                    {
                        healTarget.injuries[i].severity -= healAmount;
                        if (healTarget.injuries[i].severity <= 0)
                            healTarget.injuries.RemoveAt(i);
                        break;
                    }
                }
            }

            Debug.Log($"[Medical] 치료 완료: {healTarget.partType} {healInjuryType}");
        }

        healTarget = null;
        onHealComplete?.Invoke();
        onHealComplete = null;
    }

    #endregion

    #region 외부 API — 전체 치료 (침대)

    /// <summary>모든 부상 제거 (안전가옥 침대)</summary>
    public void HealAll()
    {
        for (int i = 0; i < allParts.Length; i++)
            allParts[i].ClearAll();

        RecalculateDebuffs();
        Debug.Log("[Medical] 전체 치료 완료 (침대 휴식)");
    }

    #endregion

    #region 유틸리티

    public BodyPart GetPart(BodyPartType type)
    {
        switch (type)
        {
            case BodyPartType.Head: return Head;
            case BodyPartType.Torso: return Torso;
            case BodyPartType.Arms: return Arms;
            case BodyPartType.LeftLeg: return LeftLeg;
            case BodyPartType.RightLeg: return RightLeg;
            default: return null;
        }
    }

    public BodyPart[] GetAllParts() => allParts;

    /// <summary>전체 부상 목록 (UI용)</summary>
    public List<(BodyPartType part, Injury injury)> GetAllInjuries()
    {
        var result = new List<(BodyPartType, Injury)>();
        for (int i = 0; i < allParts.Length; i++)
            for (int j = 0; j < allParts[i].injuries.Count; j++)
                result.Add((allParts[i].partType, allParts[i].injuries[j]));
        return result;
    }

    #endregion
}
