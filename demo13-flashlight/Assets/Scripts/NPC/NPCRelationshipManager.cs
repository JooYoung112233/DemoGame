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
}
