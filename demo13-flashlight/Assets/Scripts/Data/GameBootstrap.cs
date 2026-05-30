using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // 기존 시스템
        EnsureSingleton<QuestManager>("QuestManager");
        EnsureSingleton<NPCRelationshipManager>("NPCRelationshipManager");
        EnsureSingleton<PostRaidEventManager>("PostRaidEventManager");

        // 스토리/UI 시스템
        EnsureSingleton<ToastManager>("ToastManager");
        EnsureSingleton<NarrationUI>("NarrationUI");
        EnsureSingleton<TutorialPrompt>("TutorialPrompt");
        EnsureSingleton<ScreenEffectManager>("ScreenEffectManager");

        // 반복 콘텐츠 시스템
        EnsureSingleton<DailyQuestManager>("DailyQuestManager");
        EnsureSingleton<AchievementManager>("AchievementManager");

        // 경제 시스템
        EnsureSingleton<CurrencyManager>("CurrencyManager");

        // 세이브 시스템
        EnsureSingleton<SaveManager>("SaveManager");

        // 스토리 시스템
        EnsureSingleton<StoryLocale>("StoryLocale");
        EnsureSingleton<StoryPlayer>("StoryPlayer");
        EnsureSingleton<StoryTriggerManager>("StoryTriggerManager");
    }

    static void EnsureSingleton<T>(string name) where T : MonoBehaviour
    {
        if (Object.FindFirstObjectByType<T>() != null) return;
        var go = new GameObject($"[{name}]");
        go.AddComponent<T>();
        Object.DontDestroyOnLoad(go);
    }
}
