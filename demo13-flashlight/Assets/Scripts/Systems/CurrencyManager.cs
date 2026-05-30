using UnityEngine;

/// <summary>
/// 화폐(루디) 중앙 관리 싱글톤.
/// 루디는 짙은 현상 지역에서만 얻는 핵심 자원이자 화폐.
/// 퀘스트/업적/레이드 이벤트 보상, 상점 거래 등 모든 화폐 흐름이 여기를 거친다.
/// GameBootstrap에서 자동 생성, SaveManager가 잔액을 영속화.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] int balance = 0;

    /// <summary>현재 루디 잔액.</summary>
    public int Balance => balance;

    /// <summary>잔액 변동 이벤트 (newBalance, delta). UI/토스트가 구독.</summary>
    public event System.Action<int, int> OnBalanceChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>루디 추가 (보상). reason은 로그용.</summary>
    public void Add(int amount, string reason = null)
    {
        if (amount <= 0) return;
        balance += amount;
        Debug.Log($"[Currency] +{amount} 루디 ({reason ?? "보상"}) → 잔액 {balance}");
        OnBalanceChanged?.Invoke(balance, amount);
        ToastManager.Show($"◈ +{amount:N0} 루디", ToastManager.ToastType.Success);
    }

    /// <summary>지불 가능 여부.</summary>
    public bool CanAfford(int amount) => balance >= amount;

    /// <summary>루디 차감. 잔액 부족 시 false 반환하고 차감하지 않음.</summary>
    public bool Spend(int amount, string reason = null)
    {
        if (amount <= 0) return true;
        if (balance < amount)
        {
            Debug.Log($"[Currency] 잔액 부족: {amount} 필요, {balance} 보유");
            return false;
        }
        balance -= amount;
        Debug.Log($"[Currency] -{amount} 루디 ({reason ?? "지출"}) → 잔액 {balance}");
        OnBalanceChanged?.Invoke(balance, -amount);
        return true;
    }

    /// <summary>잔액을 강제로 깎되, 0 밑으로는 내려가지 않음 (페널티용).</summary>
    public int Lose(int amount, string reason = null)
    {
        if (amount <= 0) return 0;
        int actual = Mathf.Min(amount, balance);
        balance -= actual;
        Debug.Log($"[Currency] -{actual} 루디 손실 ({reason ?? "페널티"}) → 잔액 {balance}");
        OnBalanceChanged?.Invoke(balance, -actual);
        if (actual > 0)
            ToastManager.Show($"◈ -{actual:N0} 루디", ToastManager.ToastType.Warning);
        return actual;
    }

    // ═══════════════════════════
    //  세이브/로드
    // ═══════════════════════════

    public int GetSaveData() => balance;

    public void LoadSaveData(int saved)
    {
        balance = Mathf.Max(0, saved);
        OnBalanceChanged?.Invoke(balance, 0);
    }
}
