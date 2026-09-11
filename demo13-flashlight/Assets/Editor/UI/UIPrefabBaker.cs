#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 코드 생성 UI → 프리팹 베이크 (ui-prefab-plan.md '프리팹 우선' 전환).
/// 각 패널의 EditorBake()로 계층을 1회 생성 → SaveAsPrefabAsset로 Resources/UI/에 저장.
/// 런타임 부트스트랩은 이 프리팹을 Instantiate(프리팹 없으면 코드 생성 폴백).
///
/// 메뉴를 패널별로 추가하며 점진 전환. (PoC: ItemDetail)
/// </summary>
public static class UIPrefabBaker
{
    const string OutDir = "Assets/Resources/UI";

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/ItemDetail")]
    public static void BakeItemDetail()
    {
        Bake<ItemDetailUI>("ItemDetailUI", c => c.EditorBake());
    }

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/NoteUI")]
    public static void BakeNoteUI()
    {
        Bake<NoteUI>("NoteUI", c => c.EditorBake());
    }

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/NarrationUI")]
    public static void BakeNarrationUI() => Bake<NarrationUI>("NarrationUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/TutorialPrompt")]
    public static void BakeTutorialPrompt() => Bake<TutorialPrompt>("TutorialPrompt", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/RaidResultUI")]
    public static void BakeRaidResultUI() => Bake<RaidResultUI>("RaidResultUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/PostRaidEventUI")]
    public static void BakePostRaidEventUI() => Bake<PostRaidEventUI>("PostRaidEventUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/PauseMenu")]
    public static void BakePauseMenu() => Bake<PauseMenu>("PauseMenu", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/GroundPickupUI")]
    public static void BakeGroundPickupUI() => Bake<GroundPickupUI>("GroundPickupUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/ToastManager")]
    public static void BakeToastManager() => Bake<ToastManager>("ToastManager", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/GameHUD")]
    public static void BakeGameHUD() => Bake<GameHUD>("GameHUD", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/QuestHUD")]
    public static void BakeQuestHUD() => Bake<QuestHUD>("QuestHUD", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/QuickSlotBar")]
    public static void BakeQuickSlotBar() => Bake<QuickSlotBar>("QuickSlotBar", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/NavigationHUD")]
    public static void BakeNavigationHUD() => Bake<NavigationHUD>("NavigationHUD", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/MapSelectUI")]
    public static void BakeMapSelectUI() => Bake<MapSelectUI>("MapSelectUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/CraftingUI")]
    public static void BakeCraftingUI() => Bake<CraftingUI>("CraftingUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/DialogueUI")]
    public static void BakeDialogueUI() => Bake<DialogueUI>("DialogueUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/CharacterPanelUI")]
    public static void BakeCharacterPanelUI() => Bake<CharacterPanelUI>("CharacterPanelUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/ShopUI")]
    public static void BakeShopUI() => Bake<ShopUI>("ShopUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/QuestLogUI")]
    public static void BakeQuestLogUI() => Bake<QuestLogUI>("QuestLogUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/RaidMapUI")]
    public static void BakeRaidMapUI() => Bake<RaidMapUI>("RaidMapUI", c => c.EditorBake());


    [MenuItem("Tools/TopDown/UI/프리팹 베이크/RadioUI")]
    public static void BakeRadioUI() => Bake<RadioUI>("RadioUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/HideoutUI")]
    public static void BakeHideoutUI() => Bake<HideoutUI>("HideoutUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/SleepUI")]
    public static void BakeSleepUI() => Bake<SleepUI>("SleepUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/TitleScreen")]
    public static void BakeTitleScreen() => Bake<TitleScreen>("TitleScreen", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/TraitPanelUI")]
    public static void BakeTraitPanelUI() => Bake<TraitPanelUI>("TraitPanelUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/SettingsUI")]
    public static void BakeSettingsUI() => Bake<SettingsUI>("SettingsUI", c => c.EditorBake());

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/── 전부 ──", priority = 100)]
    public static void BakeAll()
    {
        BakeItemDetail(); BakeNoteUI(); BakeNarrationUI(); BakeTutorialPrompt();
        BakeRaidResultUI(); BakePostRaidEventUI(); BakePauseMenu(); BakeGroundPickupUI();
        BakeToastManager(); BakeGameHUD(); BakeQuestHUD(); BakeQuickSlotBar();
        BakeNavigationHUD(); BakeMapSelectUI(); BakeCraftingUI(); BakeDialogueUI();
        BakeCharacterPanelUI(); BakeShopUI();
        BakeQuestLogUI(); BakeRaidMapUI(); BakeRadioUI();
        BakeHideoutUI(); BakeSleepUI(); BakeTitleScreen(); BakeTraitPanelUI(); BakeSettingsUI();
        Debug.Log("[UIPrefabBaker] 전체 베이크 완료. (Systems 씬 재빌드로 프리팹 인스턴스 반영)");
    }

    // ── 공통 베이크 ────────────────────────────────────────
    static void Bake<T>(string assetName, System.Action<T> build) where T : Component
    {
        EnsureDir();
        var go = new GameObject(assetName);
        try
        {
            var comp = go.AddComponent<T>();   // 에디터 모드 → Awake 미호출, EditorBake가 명시 생성
            build(comp);
            string path = $"{OutDir}/{assetName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log($"[UIPrefabBaker] 베이크 완료 → {path}");
        }
        finally
        {
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
        }
    }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutDir))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
    }
}
#endif
