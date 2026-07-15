using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 도감(Codex) — "첫 획득" 발견 기록 보유·영속화. (docs/items.md §아이템 도감)
///
/// 1차 범위 = 기록·열람만(보상 없음). 발견 = itemId set.
/// 훅: `PlayerInventory`가 자기 3격자(가방/주머니/보안)의 `InventoryGrid.OnItemPlaced`를
///     구독해 `Discover(itemId)` 호출 → 줍기·루팅 드래그·자동배치·구매·제작 등 모든 획득 경로를 한 지점에서 포착.
///
/// 자가부트 싱글턴(QuickSlotBar/PlayerNoise 패턴) + DontDestroyOnLoad.
/// </summary>
public class CodexManager : MonoBehaviour
{
    public static CodexManager Instance { get; private set; }

    readonly HashSet<string> _discovered = new HashSet<string>();

    /// <summary>발견 시 호출(itemId) — UI 갱신용.</summary>
    public event System.Action<string> OnDiscovered;

    /// <summary>true면 발견은 기록하되 토스트를 띄우지 않는다.
    /// 세이브 로드 구간에서 켠다 — 격자 복원(AppendSaveData→TryPlace)이 발견 훅을 발화시키므로,
    /// 특히 discoveredItems가 없는 **구 세이브**를 처음 열 때 가방 아이템 수만큼 토스트가 쏟아지는 것을 막는다.</summary>
    public bool SilentDiscovery { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;   // 맵툴 씬에선 비활성(QuickSlotBar/PlayerNoise와 동일 가드)
        if (Instance != null) return;
        if (FindFirstObjectByType<CodexManager>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject("[CodexManager]");
        DontDestroyOnLoad(go);
        go.AddComponent<CodexManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ── 발견 ─────────────────────────────────────────────────────────

    public bool IsDiscovered(string itemId) => !string.IsNullOrEmpty(itemId) && _discovered.Contains(itemId);

    /// <summary>첫 획득 발견 처리. 새로 발견했으면 true(토스트 1회).</summary>
    public bool Discover(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (!_discovered.Add(itemId)) return false;   // 이미 발견

        if (!SilentDiscovery)
        {
            var data = ItemDatabase.Get(itemId);
            string name = data != null ? data.displayName : itemId;
            ToastManager.Show($"도감 발견 — {name}", ToastManager.ToastType.Info);
        }
        OnDiscovered?.Invoke(itemId);
        return true;
    }

    public int DiscoveredCount => _discovered.Count;

    // ── 세이브 ───────────────────────────────────────────────────────

    public List<string> GetSaveData() => new List<string>(_discovered);

    public void LoadSaveData(List<string> ids)
    {
        _discovered.Clear();
        if (ids == null) return;
        foreach (var id in ids)
            if (!string.IsNullOrEmpty(id)) _discovered.Add(id);
    }

    /// <summary>새 게임 리셋 — 발견 기록 비움.</summary>
    public void ResetForNewGame() => _discovered.Clear();

    // ── 디버그(F1) ───────────────────────────────────────────────────

    /// <summary>전체 아이템 발견 처리(토스트 없이). OnDiscovered는 발행하지 않는다 —
    /// itemId null 발행은 구독자 계약 위반. 도감 UI는 열 때 Rebuild하므로 갱신에 문제 없음.</summary>
    public void DebugDiscoverAll()
    {
        var all = ItemDatabase.GetAll();
        if (all != null)
            foreach (var d in all)
                if (d != null && !string.IsNullOrEmpty(d.itemId)) _discovered.Add(d.itemId);
    }
}
