using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 짙은현상 오케스트레이터 (씬 스코프, RaidManager 패턴). Phase 1 기반.
/// 레이드 중 랜덤 간격으로 잠복 중인 AnomalyZone을 발생시킴(동시 개수 제한).
/// 존만 배치하면 AnomalyZone.OnEnable이 이 매니저를 자동 생성.
/// 디버그: K키 = 즉시 랜덤 발생, 좌상단 상태 HUD.
/// 설계: docs/anomaly.md
/// </summary>
public class AnomalyManager : MonoBehaviour
{
    public static AnomalyManager Instance { get; private set; }

    [SerializeField] bool showDebugHud = true;
    [SerializeField] KeyCode forceTriggerKey = KeyCode.K;

    float _nextTriggerTime;

    GameTuning T => GameTuning.Instance;
    float IntervalMin => T != null ? T.anomalyIntervalMin : 90f;
    float IntervalMax => T != null ? T.anomalyIntervalMax : 180f;
    int MaxConcurrent => T != null ? Mathf.Max(1, T.anomalyMaxConcurrent) : 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start() => ScheduleNext();

    void ScheduleNext() => _nextTriggerTime = Time.time + Random.Range(IntervalMin, IntervalMax);

    void Update()
    {
        // 디버그 강제 발생(레이드/간격 무시) — 어디서든 테스트
        if (Input.GetKeyDown(forceTriggerKey)) { TryTriggerRandom(); ScheduleNext(); }

        // 자동 발생은 레이드 중에만
        if (RaidManager.Instance == null || !RaidManager.Instance.IsRaidActive) return;
        if (Time.time < _nextTriggerTime) return;
        TryTriggerRandom();
        ScheduleNext();
    }

    int ActiveCount()
    {
        int n = 0;
        var all = AnomalyZone.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].State != AnomalyState.Dormant) n++;
        return n;
    }

    /// <summary>잠복 중 존 하나를 랜덤 발생(동시 개수 여유 있을 때).</summary>
    public void TryTriggerRandom()
    {
        if (ActiveCount() >= MaxConcurrent) return;
        var candidates = new List<AnomalyZone>();
        var all = AnomalyZone.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].CanTrigger) candidates.Add(all[i]);
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[AnomalyManager] 발생 시도했지만 잠복 중인 AnomalyZone이 없음. (Tools▸TopDown▸Build▸Anomaly Zone로 배치)");
            return;
        }
        candidates[Random.Range(0, candidates.Count)].Trigger();
    }

    void OnGUI()
    {
        if (!showDebugHud) return;
        var all = AnomalyZone.All;
        int active = ActiveCount();
        if (active == 0) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        float y = 70f;
        GUI.color = new Color(0.85f, 0.7f, 1f);
        GUI.Label(new Rect(12, y, 360, 20), $"■ 짙은현상 {active}개", style);
        y += 20f;
        for (int i = 0; i < all.Count; i++)
        {
            var z = all[i];
            if (z == null || z.State == AnomalyState.Dormant) continue;
            string info = z.IsLootablePhase ? $"{z.State}  남은 {z.TimeLeft:F0}s" : z.State.ToString();
            GUI.color = z.State == AnomalyState.Waning ? new Color(1f, 0.6f, 0.2f)
                       : z.State == AnomalyState.Collapsing ? new Color(1f, 0.4f, 0.4f)
                       : new Color(0.8f, 0.8f, 1f);
            GUI.Label(new Rect(20, y, 360, 18), $"· {z.name}: {info}", style);
            y += 18f;
        }
        GUI.color = Color.white;
    }
}
