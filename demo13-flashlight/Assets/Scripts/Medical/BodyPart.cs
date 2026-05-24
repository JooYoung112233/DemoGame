using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 신체 부위 열거형 + 부상 종류.
/// </summary>
public enum BodyPartType
{
    Head,       // 머리 — 시야/판정 디버프
    Torso,      // 몸통 — HP 최대치/스태미너 회복 저하
    Arms,       // 양팔 — 공격속도/차징 저하
    LeftLeg,    // 왼다리 — 이동속도 감소
    RightLeg,   // 오른다리 — 이동속도 감소
}

public enum InjuryType
{
    Bleeding,   // 출혈 — 시간당 HP 감소 (DoT)
    Fracture,   // 골절 — 해당 부위 기능 대폭 저하, 자연치유 없음
    Pain,       // 통증 — 스태미너 회복 저하, 시간 경과로 서서히 감소
}

/// <summary>
/// 개별 부상 인스턴스.
/// 하나의 부위에 여러 부상이 동시에 존재할 수 있음.
/// </summary>
[System.Serializable]
public class Injury
{
    public InjuryType type;
    public float severity;      // 0~1 (심각도: 1이 최대)
    public float duration;      // 남은 시간 (Pain만 사용, 0이면 영구)
    public float maxDuration;   // 최초 지속 시간

    public Injury(InjuryType type, float severity, float duration = 0f)
    {
        this.type = type;
        this.severity = Mathf.Clamp01(severity);
        this.duration = duration;
        this.maxDuration = duration;
    }

    /// <summary>시간 감소가 있는 부상인지 (Pain만 자연 감소)</summary>
    public bool IsTimeBased => type == InjuryType.Pain && maxDuration > 0;
}

/// <summary>
/// 개별 신체 부위 상태.
/// </summary>
[System.Serializable]
public class BodyPart
{
    public BodyPartType partType;
    public List<Injury> injuries = new List<Injury>();

    public BodyPart(BodyPartType type)
    {
        partType = type;
    }

    /// <summary>특정 부상이 있는지</summary>
    public bool HasInjury(InjuryType type)
    {
        for (int i = 0; i < injuries.Count; i++)
            if (injuries[i].type == type) return true;
        return false;
    }

    /// <summary>특정 부상의 최대 심각도</summary>
    public float GetSeverity(InjuryType type)
    {
        float max = 0;
        for (int i = 0; i < injuries.Count; i++)
            if (injuries[i].type == type && injuries[i].severity > max)
                max = injuries[i].severity;
        return max;
    }

    /// <summary>부상 추가</summary>
    public void AddInjury(Injury injury)
    {
        // 같은 종류가 이미 있으면 심각도만 갱신 (더 높은 값으로)
        for (int i = 0; i < injuries.Count; i++)
        {
            if (injuries[i].type == injury.type)
            {
                if (injury.severity > injuries[i].severity)
                    injuries[i].severity = injury.severity;
                // Pain은 지속시간도 리셋
                if (injury.type == InjuryType.Pain)
                    injuries[i].duration = Mathf.Max(injuries[i].duration, injury.duration);
                return;
            }
        }
        injuries.Add(injury);
    }

    /// <summary>특정 부상 제거</summary>
    public bool RemoveInjury(InjuryType type)
    {
        for (int i = injuries.Count - 1; i >= 0; i--)
        {
            if (injuries[i].type == type)
            {
                injuries.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>모든 부상 제거 (침대 완치)</summary>
    public void ClearAll()
    {
        injuries.Clear();
    }

    /// <summary>부상이 하나라도 있는지</summary>
    public bool IsInjured => injuries.Count > 0;
}
