using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        EnsureSingleton<QuestManager>("QuestManager");
        EnsureSingleton<NPCRelationshipManager>("NPCRelationshipManager");
        EnsureSingleton<PostRaidEventManager>("PostRaidEventManager");
    }

    static void EnsureSingleton<T>(string name) where T : MonoBehaviour
    {
        if (Object.FindFirstObjectByType<T>() != null) return;
        var go = new GameObject($"[{name}]");
        go.AddComponent<T>();
        Object.DontDestroyOnLoad(go);
    }
}
