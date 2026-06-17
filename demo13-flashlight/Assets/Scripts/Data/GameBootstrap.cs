using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // Systems 씬이 매니저를 전부 공급하면(=정상 플레이) 코드 스폰 폴백을 건너뛴다.
        // 맵툴 씬 / Systems 미빌드 시에는 ProvidesSystems=false → 기존대로 코드 스폰.
        if (SystemsScene.ProvidesSystems) return;

        // 기존 시스템
        EnsureSingleton<QuestManager>("QuestManager");
        EnsureSingleton<NPCRelationshipManager>("NPCRelationshipManager");
        EnsureSingleton<PostRaidEventManager>("PostRaidEventManager");

        // 스토리/UI 시스템
        EnsureSingleton<ToastManager>("ToastManager");
        EnsureSingleton<NarrationUI>("NarrationUI");
        EnsureSingleton<NoteUI>("NoteUI");
        EnsureSingleton<TutorialPrompt>("TutorialPrompt");
        EnsureSingleton<ScreenEffectManager>("ScreenEffectManager");

        // 반복 콘텐츠 시스템
        EnsureSingleton<DailyQuestManager>("DailyQuestManager");
        EnsureSingleton<AchievementManager>("AchievementManager");

        // 경제 시스템
        EnsureSingleton<CurrencyManager>("CurrencyManager");

        // 내비게이션 (레이드 맵 상태 + 지역별 영속 지식; NavigationHUD는 자체 RuntimeInit 폴백)
        EnsureSingleton<RaidMapManager>("RaidMapManager");

        // 세이브 시스템
        EnsureSingleton<SaveManager>("SaveManager");
        EnsureSingleton<SaveCheckpoints>("SaveCheckpoints");
        EnsureSingleton<CombatStateTracker>("CombatStateTracker");

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
