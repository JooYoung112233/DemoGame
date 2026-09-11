using UnityEngine;

/// <summary>
/// 캐릭터 경험치/레벨 진행 싱글톤 (lazy 자가 부트 — Systems 씬 배치 불필요).
/// 설계: docs/traits.md §2 (2026-07-06 확정) — PP 주 공급원.
///  - 레이드 종료 정산 XP(RaidManager)가 GrantXp로 들어옴.
///  - 다음 레벨 필요 XP = GameTuning.xpPerLevelBase × 현재 레벨 (선형).
///  - 레벨업마다 TraitManager.GrantPP(xpPpPerLevel) + 토스트.
///  - 세이브: level/xp 영속 (GameSaveData.progress — SaveManager 배선).
/// </summary>
public class PlayerProgress : MonoBehaviour
{
    static PlayerProgress instance;
    public static PlayerProgress Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("PlayerProgress");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<PlayerProgress>();
            }
            return instance;
        }
    }

    int level = 1;
    int xp;   // 현재 레벨에서 쌓은 XP (누적 아님)

    public int Level => level;
    public int Xp => xp;

    /// <summary>다음 레벨까지 필요한 총 XP (= base × 현재 레벨, 선형).</summary>
    public int XpToNext
    {
        get
        {
            int baseXp = GameTuning.Instance != null ? GameTuning.Instance.xpPerLevelBase : 100;
            return Mathf.Max(1, baseXp * level);
        }
    }

    static int PpPerLevel => GameTuning.Instance != null ? GameTuning.Instance.xpPpPerLevel : 1;

    /// <summary>레벨/XP 변동 시 발생 (UI 갱신용).</summary>
    public event System.Action OnProgressChanged;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    /// <summary>XP 지급 + 레벨업 처리(여러 레벨 한 번에 가능). 레벨업마다 PP 지급 + 토스트.</summary>
    public void GrantXp(int amount)
    {
        if (amount <= 0) return;
        xp += amount;

        int levelsGained = 0;
        while (xp >= XpToNext)   // XpToNext는 level에 따라 증가 — 루프마다 재평가
        {
            xp -= XpToNext;
            level++;
            levelsGained++;
        }

        if (levelsGained > 0)
        {
            int pp = PpPerLevel * levelsGained;
            if (pp > 0)
            {
                if (TraitManager.Instance != null) TraitManager.Instance.GrantPP(pp);
                else Debug.LogWarning($"[PlayerProgress] TraitManager 없음 — 레벨업 PP {pp} 소실 (부트 순서 확인)");
            }
            ToastManager.Show($"레벨 {level} 달성!  PP +{pp}", ToastManager.ToastType.Success, 3f);
            Debug.Log($"[PlayerProgress] 레벨업 ×{levelsGained} → Lv.{level} (PP +{pp})");
        }

        OnProgressChanged?.Invoke();
    }

    // ═══════════════════════════
    //  세이브 (SaveManager 배선)
    // ═══════════════════════════

    [System.Serializable]
    public class ProgressSaveData
    {
        public int level = 1;
        public int xp;
    }

    public ProgressSaveData GetSaveData() => new ProgressSaveData { level = level, xp = xp };

    public void LoadSaveData(ProgressSaveData data)
    {
        if (data == null) return;
        level = Mathf.Max(1, data.level);
        xp = Mathf.Max(0, data.xp);
        OnProgressChanged?.Invoke();
    }
}
