using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 전투/유닛 스탯 중앙 데이터베이스.
/// Resources/Data/StatDB.asset 에 저장. 키 기반으로 데이터 접근.
/// </summary>
[CreateAssetMenu(fileName = "StatDB", menuName = "Dev Tools/Data/Stat DB")]
public class StatDB : ScriptableObject
{
    [Header("Player")]
    public PlayerStatData playerStat = new PlayerStatData();

    [Header("Units")]
    public List<UnitStatData> units = new List<UnitStatData>();

    // ===== 캐시 =====
    Dictionary<string, UnitStatData> unitMap;

    void BuildCache()
    {
        unitMap = new Dictionary<string, UnitStatData>();
        foreach (var u in units)
        {
            if (!string.IsNullOrEmpty(u.id) && !unitMap.ContainsKey(u.id))
                unitMap[u.id] = u;
        }
    }

    /// <summary>유닛 키로 스탯 조회. 없으면 null.</summary>
    public UnitStatData GetUnit(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (unitMap == null) BuildCache();
        unitMap.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>캐시 강제 갱신 (에디터에서 데이터 변경 후)</summary>
    public void RefreshCache() => unitMap = null;

    /// <summary>모든 유닛 ID 목록</summary>
    public List<string> GetAllUnitIds()
    {
        var ids = new List<string>();
        foreach (var u in units)
            if (!string.IsNullOrEmpty(u.id))
                ids.Add(u.id);
        return ids;
    }

    // ===== 글로벌 접근 =====
    static StatDB instance;

    public static StatDB Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<StatDB>("Data/StatDB");
                if (instance == null)
                    Debug.LogWarning("[StatDB] Resources/Data/StatDB.asset 를 찾을 수 없습니다.");
            }
            return instance;
        }
    }

    /// <summary>에디터용: 직접 인스턴스 지정</summary>
    public static void SetInstance(StatDB db) => instance = db;
}
