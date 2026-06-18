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
    float[] SpotWeights => T != null ? T.anomalySpotCountWeights : null;

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

    /// <summary>호환 별칭 — 한 번의 발생 웨이브.</summary>
    public void TryTriggerRandom() => TriggerWave();

    /// <summary>발생 '웨이브' — 가중치(anomalySpotCountWeights)로 "몇 군데" 나올지 정하고,
    /// 그만큼 잠복 존을 SelectionWeight 가중 비복원 추출로 골라 동시에 발생시킨다.
    /// 동시 활성 상한(MaxConcurrent)·잠복 존 수로 클램프.</summary>
    public void TriggerWave()
    {
        int free = MaxConcurrent - ActiveCount();
        if (free <= 0) return;

        var candidates = new List<AnomalyZone>();
        var all = AnomalyZone.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].CanTrigger && all[i].SelectionWeight > 0f) candidates.Add(all[i]);
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[AnomalyManager] 발생 시도했지만 잠복 중인 AnomalyZone이 없음. (Tools▸TopDown▸Build▸Anomaly Zone로 배치)");
            return;
        }

        int cap = Mathf.Min(free, candidates.Count);
        int count = PickSpotCount(cap);
        for (int k = 0; k < count && candidates.Count > 0; k++)
        {
            int idx = WeightedPick(candidates);
            candidates[idx].Trigger();
            candidates.RemoveAt(idx);
        }
    }

    /// <summary>가중치 배열로 "몇 군데"(1..cap) 결정. index i → (i+1)군데. 비었거나 합 0이면 1.</summary>
    int PickSpotCount(int cap)
    {
        var w = SpotWeights;
        if (w == null || w.Length == 0) return 1;
        int usable = Mathf.Min(w.Length, cap);
        float total = 0f;
        for (int i = 0; i < usable; i++) total += Mathf.Max(0f, w[i]);
        if (total <= 0f) return 1;
        float r = Random.value * total, c = 0f;
        for (int i = 0; i < usable; i++)
        {
            c += Mathf.Max(0f, w[i]);
            if (r <= c) return i + 1;
        }
        return usable;
    }

    /// <summary>SelectionWeight 가중 랜덤으로 후보 1개의 인덱스 선택.</summary>
    static int WeightedPick(List<AnomalyZone> list)
    {
        float total = 0f;
        for (int i = 0; i < list.Count; i++) total += list[i].SelectionWeight;
        if (total <= 0f) return Random.Range(0, list.Count);
        float r = Random.value * total, c = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            c += list[i].SelectionWeight;
            if (r <= c) return i;
        }
        return list.Count - 1;
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
