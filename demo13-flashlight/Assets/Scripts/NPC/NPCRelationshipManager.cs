using UnityEngine;
using System.Collections.Generic;

public class NPCRelationshipManager : MonoBehaviour
{
    public static NPCRelationshipManager Instance { get; private set; }

    Dictionary<string, NPCRelationship> relationships = new Dictionary<string, NPCRelationship>();

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
        if (Instance == this)
            Instance = null;
    }

    public NPCRelationship GetRelationship(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;
        if (!relationships.ContainsKey(npcId))
            relationships[npcId] = new NPCRelationship(npcId);
        return relationships[npcId];
    }

    /// <summary>관계가 아직 없을 때만 초기값으로 생성(첫 대면 시드). 이미 있으면(세이브 로드 등) 무시.</summary>
    public void SeedRelationship(string npcId, int affinity, int trust, int fear)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        if (relationships.ContainsKey(npcId)) return;
        var rel = new NPCRelationship(npcId, Mathf.Clamp(affinity, 0, 100), Mathf.Clamp(trust, 0, 100));
        rel.fear = Mathf.Clamp(fear, 0, 100);
        relationships[npcId] = rel;
    }

    public void ModifyAffinity(string npcId, int delta)
    {
        var rel = GetRelationship(npcId);
        rel.affinity = Mathf.Clamp(rel.affinity + delta, 0, 100);
    }

    public void ModifyTrust(string npcId, int delta)
    {
        var rel = GetRelationship(npcId);
        rel.trust = Mathf.Clamp(rel.trust + delta, 0, 100);
    }

    public void ModifyFear(string npcId, int delta)
    {
        var rel = GetRelationship(npcId);
        rel.fear = Mathf.Clamp(rel.fear + delta, 0, 100);
    }

    public string GetAffinityLabel(int affinity)
    {
        if (affinity >= 80) return "신뢰";
        if (affinity >= 60) return "친밀";
        if (affinity >= 40) return "호의";
        if (affinity >= 20) return "중립";
        return "경계";
    }

    // ═══════════════════════════
    //  세이브/로드
    // ═══════════════════════════

    public List<NPCRelationshipSaveEntry> GetAllSaveData()
    {
        var list = new List<NPCRelationshipSaveEntry>();
        foreach (var kvp in relationships)
        {
            var rel = kvp.Value;
            var entry = new NPCRelationshipSaveEntry
            {
                npcId = rel.npcId,
                affinity = rel.affinity,
                trust = rel.trust,
                fear = rel.fear,
                completedEvents = new List<string>(rel.completedEvents),
                flags = new List<FlagEntry>(),
            };
            foreach (var f in rel.flags)
                entry.flags.Add(new FlagEntry { key = f.Key, value = f.Value });
            list.Add(entry);
        }
        return list;
    }

    public void LoadAllSaveData(List<NPCRelationshipSaveEntry> data)
    {
        if (data == null) return;
        relationships.Clear();
        foreach (var entry in data)
        {
            var rel = new NPCRelationship(entry.npcId, entry.affinity, entry.trust);
            rel.fear = entry.fear;
            if (entry.completedEvents != null)
                rel.completedEvents = new HashSet<string>(entry.completedEvents);
            if (entry.flags != null)
                foreach (var f in entry.flags)
                    rel.flags[f.key] = f.value;
            relationships[entry.npcId] = rel;
        }
    }
}
