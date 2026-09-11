using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연속 공격(콤보) 체인. 순서대로 이어지는 AttackData 단계들.
/// 한 단계 공격 중 cancelFromFrame 이후 입력하면 다음 단계로 캔슬 연결.
/// </summary>
[CreateAssetMenu(menuName = "Top-Down Combat/Attack Combo", fileName = "Combo_")]
public class AttackComboData : ScriptableObject
{
    public string comboId = "light";

    [Tooltip("순서대로 이어지는 공격 단계 (0=1타, 1=2타, ...)")]
    public List<AttackData> steps = new List<AttackData>();

    [Tooltip("선입력 버퍼(초) — 캔슬 가능 전에 누른 입력도 이 시간만큼 기억해 다음 단계 발동")]
    public float bufferTime = 0.25f;

    public int StepCount => steps != null ? steps.Count : 0;
    public AttackData GetStep(int i) => (steps != null && i >= 0 && i < steps.Count) ? steps[i] : null;
}
