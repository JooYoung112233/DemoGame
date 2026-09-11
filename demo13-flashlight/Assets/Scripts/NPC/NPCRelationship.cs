using System.Collections.Generic;

[System.Serializable]
public class NPCRelationship
{
    public string npcId;
    public int affinity;
    public int trust;
    public int fear;
    public HashSet<string> completedEvents = new HashSet<string>();
    public Dictionary<string, bool> flags = new Dictionary<string, bool>();

    public NPCRelationship(string npcId, int initialAffinity = 20, int initialTrust = 10)
    {
        this.npcId = npcId;
        this.affinity = initialAffinity;
        this.trust = initialTrust;
        this.fear = 0;
    }
}
