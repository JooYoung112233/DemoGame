using UnityEngine;

/// <summary>
/// 치료 아이템 정의 (ScriptableObject).
/// Assets/Data/MedicalItems/ 에 생성.
/// </summary>
[CreateAssetMenu(fileName = "NewMedicalItem", menuName = "Dev Tools/Item/Medical Item")]
public class MedicalItemData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("아이템 ID (인벤토리 연동용)")]
    public string itemId;

    [Tooltip("표시 이름")]
    public string displayName;

    [Tooltip("설명")]
    [TextArea(2, 4)]
    public string description;

    [Header("치료 대상")]
    [Tooltip("이 아이템이 치료할 수 있는 부상 종류")]
    public InjuryType[] targetInjuries;

    [Tooltip("true면 어떤 부위든 사용 가능 (구급상자)")]
    public bool isUniversal = false;

    [Header("효과")]
    [Tooltip("치료 시간 (초). 0이면 즉시")]
    public float useTime = 2f;

    [Tooltip("심각도 감소량 (0~1). 1이면 완전 치료")]
    public float healAmount = 1f;

    [Tooltip("일시적 효과 지속시간 (진통제 등). 0이면 영구 치료")]
    public float effectDuration = 0f;

    /// <summary>특정 부상을 치료할 수 있는지</summary>
    public bool CanTreat(InjuryType injury)
    {
        for (int i = 0; i < targetInjuries.Length; i++)
            if (targetInjuries[i] == injury) return true;
        return false;
    }
}
