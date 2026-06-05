using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 시계 나침반이 가리킬 수 있는 월드 타겟. 우선순위 기반(Tutorial > Quest > Story > Exit).
/// 튜토리얼 단계/퀘스트 목표/스토리 지점에 부착하거나 런타임 생성 후 Configure().
/// 설계: docs/navigation.md §1.1
/// </summary>
public class CompassTarget : MonoBehaviour
{
    public static readonly List<CompassTarget> All = new List<CompassTarget>();

    [SerializeField] CompassPriority priority = CompassPriority.Quest;
    [SerializeField] string label = "목표";
    [Tooltip("끄면 나침반 후보에서 제외.")]
    [SerializeField] bool active = true;

    public CompassPriority Priority => priority;
    public string Label => label;
    public bool Active { get => active; set => active = value; }
    public Vector2 Position => transform.position;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    /// <summary>런타임에서 타겟 설정(예: 퀘스트 목표 마커 스폰).</summary>
    public void Configure(CompassPriority p, string l, bool isActive = true)
    {
        priority = p;
        label = l;
        active = isActive;
    }
}
