using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 영속 "Systems" 부트 씬 생성기. (Tools ▸ TopDown ▸ Build ▸ Systems Scene)
///
/// 한 번에 Assets/Scenes/Systems.unity 를 만들고 다음을 배치한다:
///  • [매니저들] Quest/NPC/PostRaidEvent/Toast/Narration/Tutorial/ScreenEffect/DailyQuest/
///    Achievement/Currency/Save/StoryLocale/StoryPlayer/StoryTrigger + SceneTransitionManager
///  • UIManager (+ GameHUD/RaidResult/MapSelect/CharacterPanel/Crafting/Shop/Dialogue/PostRaidEvent/Quest)
///    → 상점(ShopUI) 포함 모든 UI가 씬에 GameObject로 배치되어 에디터에서 보임
///  • PlayerRig 프리팹 인스턴스 (카메라 + 라이트 + 후처리 Volume)
///  • Global Light2D (어두운 밤) + DayNightCycle + SystemsSceneEnforcer
///  • GameBoot (Systems 단독 진입 시 기본 게임플레이 씬 additive 로드)
///
/// 그리고 빌드세팅에 Systems(맨 앞) + Safehouse/InGameScene/CombatSandbox 를 등록한다.
///
/// 런타임: 게임플레이 씬은 이 Systems 위에 additive로 교체 로드됨(SceneTransitionManager).
/// </summary>
public static class SystemsSceneBuilder
{
    const string SCENE_PATH = "Assets/Scenes/Systems.unity";

    // 빌드세팅 등록 대상(존재하는 것만, Systems 맨 앞)
    static readonly string[] BuildScenes =
    {
        "Assets/Scenes/Systems.unity",
        "Assets/Scenes/Safehouse.unity",
        "Assets/Scenes/InGameScene.unity",
        "Assets/Scenes/CombatSandbox.unity",
    };

    // [매니저들] — GameBootstrap이 코드 스폰하던 싱글톤 + 씬 전환 매니저
    static readonly System.Type[] ManagerTypes =
    {
        typeof(QuestManager),
        typeof(NPCRelationshipManager),
        typeof(PostRaidEventManager),
        typeof(ToastManager),
        typeof(NarrationUI),
        typeof(NoteUI),
        typeof(TutorialPrompt),
        typeof(ScreenEffectManager),
        typeof(DailyQuestManager),
        typeof(AchievementManager),
        typeof(CurrencyManager),
        typeof(ReputationManager),
        typeof(HideoutModuleManager),
        typeof(MainStash),
        typeof(SaveManager),
        typeof(StoryLocale),
        typeof(StoryPlayer),
        typeof(StoryTriggerManager),
        typeof(RaidMapManager),        // 내비게이션: 레이드 맵 상태 + 지역별 영속 지식(지도판 누적)
        typeof(SceneTransitionManager),
    };

    // UIManager 직렬화 필드 ↔ UI 컴포넌트 타입
    static readonly (string field, System.Type type)[] UiPanels =
    {
        ("gameHUD",          typeof(GameHUD)),
        ("raidResultUI",     typeof(RaidResultUI)),
        ("mapSelectUI",      typeof(MapSelectUI)),
        ("characterPanelUI", typeof(CharacterPanelUI)),
        ("craftingUI",       typeof(CraftingUI)),
        ("shopUI",           typeof(ShopUI)),
        ("dialogueUI",       typeof(DialogueUI)),
        ("postRaidEventUI",  typeof(PostRaidEventUI)),
        ("questHUD",         typeof(QuestHUD)),
        ("navigationHUD",    typeof(NavigationHUD)),   // 시계 나침반 + 미니맵 (UI)
    };

    [MenuItem("Tools/TopDown/빌드/시스템 씬")]
    public static void BuildSystemsScene()
    {
        // additive로 만들어 현재 열린 씬을 닫지 않으므로 저장 프롬프트 불필요.
        var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);  // 현재 씬 유지(폴더에만 생성)

        // ── [매니저들] (각각 루트 GO — DontDestroyOnLoad는 루트 GO에서만 동작) ──
        foreach (var t in ManagerTypes)
        {
            var go = new GameObject(t.Name);
            go.AddComponent(t);
        }

        // ── UIManager (+ 모든 UI 패널을 자식 GO로 배치) ──
        var uiGO = new GameObject("UIManager");
        var ui = uiGO.AddComponent<UIManager>();
        var uiSo = new SerializedObject(ui);
        foreach (var (field, type) in UiPanels)
        {
            var child = new GameObject(type.Name);
            child.transform.SetParent(uiGO.transform, false);
            var comp = child.AddComponent(type);
            var p = uiSo.FindProperty(field);
            if (p != null) p.objectReferenceValue = comp;
            else Debug.LogWarning($"[SystemsScene] UIManager 필드 못 찾음: {field}");
        }
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        // ── PlayerRig (카메라 + 라이트 + 후처리) ──
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PlayerRig.prefab");
        if (rigPrefab != null)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rig.name = "PlayerRig";
        }
        else
        {
            Debug.LogWarning("[SystemsScene] Resources/PlayerRig.prefab 없음 — " +
                             "'Tools/TopDown/Build/Player Rig'로 먼저 만든 뒤 다시 실행하세요.");
        }

        // ── 글로벌 조명(어두운 밤) + DayNightCycle + Enforcer ──
        var lightGO = new GameObject("Global Light 2D (Dark)");
        var global = lightGO.AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.intensity = 0.22f;
        global.color = new Color(0.45f, 0.5f, 0.68f);
        SceneLightingBuilder.ApplyAllSortingLayers(global);

        var dn = lightGO.AddComponent<DayNightCycle>();
        WireRef(dn, "globalLight", global);

        var enforcerGO = new GameObject("SystemsSceneEnforcer");
        var enforcer = enforcerGO.AddComponent<SystemsSceneEnforcer>();
        WireRef(enforcer, "ownedGlobal", global);

        // ── GameBoot (기본 게임플레이 씬 진입) ──
        var bootGO = new GameObject("GameBoot");
        var boot = bootGO.AddComponent<GameBoot>();
        var bootSo = new SerializedObject(boot);
        SetString(bootSo, "defaultScene", "Safehouse");
        SetString(bootSo, "defaultSpawn", "default");
        bootSo.ApplyModifiedPropertiesWithoutUndo();

        // ── 저장 ── (저장 후 닫기 — 현재 씬 유지)
        bool saved = EditorSceneBuildUtil.SaveAndClose(scene, SCENE_PATH, prevActive);

        if (!saved)
        {
            Debug.LogError("[SystemsScene] 씬 저장 실패: " + SCENE_PATH);
            return;
        }

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log("<color=cyan>[SystemsScene]</color> 생성 완료: " + SCENE_PATH +
                  "\n  • 매니저 " + ManagerTypes.Length + "개 + UIManager(+UI " + UiPanels.Length + "개, 상점·나침반 포함)" +
                  (rigPrefab != null ? " + PlayerRig" : " (PlayerRig 누락!)") +
                  " + 글로벌조명 + GameBoot" +
                  "\n  • 빌드세팅 등록(Systems 맨 앞). 게임플레이 씬은 additive로 교체 로드됩니다.");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Systems Scene",
                "Assets/Scenes/Systems.unity 생성 완료.\n\n" +
                "▶ 전체 게임을 테스트하려면 Systems 씬을 열고 Play 하세요.\n" +
                "   (GameBoot이 Safehouse를 additive로 엽니다.)\n\n" +
                "▶ 게임플레이 씬(Safehouse/InGameScene)에서 바로 Play해도\n" +
                "   Systems가 자동으로 additive 로드됩니다.\n\n" +
                (rigPrefab == null
                    ? "⚠ PlayerRig 프리팹이 없어 플레이어가 빠졌습니다.\n   'Tools/TopDown/Build/Player Rig' 먼저 실행 후 다시 빌드하세요."
                    : "상점(ShopUI) 포함 모든 UI가 UIManager 아래에 배치되어 있습니다."),
                "확인");
    }

    static void WireRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        else Debug.LogWarning($"[SystemsScene] 직렬화 필드 못 찾음: {target.GetType().Name}.{field}");
    }

    static void SetString(SerializedObject so, string field, string value)
    {
        var p = so.FindProperty(field);
        if (p != null) p.stringValue = value;
    }

    /// <summary>빌드세팅에 Systems(맨 앞) + 게임플레이 씬을 등록. 옛 SafehouseScene 엔트리는 제거.</summary>
    static void EnsureBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>();

        // 원하는 씬을 순서대로(존재하는 것만)
        foreach (var path in BuildScenes)
            if (System.IO.File.Exists(path))
                list.Add(new EditorBuildSettingsScene(path, true));

        // 기존 등록분 중 위에 없고, 옛 SafehouseScene이 아닌 것만 보존
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (System.Array.IndexOf(BuildScenes, s.path) >= 0) continue;
            if (s.path.EndsWith("/SafehouseScene.unity") || s.path.EndsWith("\\SafehouseScene.unity")) continue;
            // 중복 방지
            bool dup = false;
            foreach (var e in list) if (e.path == s.path) { dup = true; break; }
            if (!dup) list.Add(s);
        }

        EditorBuildSettings.scenes = list.ToArray();
    }
}
