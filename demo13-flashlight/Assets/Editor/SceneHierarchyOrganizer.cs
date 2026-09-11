#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// **씬 하이어라키를 기능별로 묶는다.** 규약: docs/architecture.md §씬 하이어라키 규약(2026-09-11).
///
/// 왜 이 방식인가 — 씬 대부분이 빌더로 생성된다. 손으로 정리하면 다음 재빌드에 날아간다.
/// 그래서 정리를 **도구 하나**로 만들고 모든 빌더가 저장 직전에 부른다(EditorSceneBuildUtil.SaveAndClose,
/// Safehouse3DLayout·Hideout3DLayout·LookDevScene). 기존 씬은 메뉴로 한 번 돌린다.
///
/// 왜 이름이 아니라 컴포넌트로 가르나 — 이름 접두사는 뜻이 섞여 있다. 실측: Zone1의 `SP_`는
/// 소품 188 + 루트 상자 47 + 스폰 지점 5가 한 접두사다. 컴포넌트는 거짓말을 안 한다.
/// 이름 표는 **지형(Environment) 안을 구조별로 나눌 때만** 쓴다.
///
/// 몇 번 돌려도 결과가 같다 — 이미 폴더에 든 것도 다시 판정해 제자리로 보내고, 빈 폴더는 지운다.
/// </summary>
public static class SceneHierarchyOrganizer
{
    const string MapRoot = "Map";

    // 폴더 순서 = 하이어라키에 보이는 순서.
    static readonly string[] MapFolders = { "Environment", "Lighting", "Gameplay", "Loot", "Enemies", "NPCs", "Controllers" };
    static readonly string[] EnvFolders = { "Ground", "Structures", "Roads", "Scatter", "Misc" };
    static readonly string[] GenericFolders = { "Cameras", "Characters", "Environment", "Lighting", "Gameplay", "Loot", "Enemies", "NPCs", "Controllers" };
    static readonly string[] SystemsFolders = { "Core", "World", "Progress", "Story", "UI", "Player", "Misc" };

    // ── Systems 씬 — 매니저 이름 → 폴더. 빌더(SystemsSceneBuilder)가 타입 이름으로 오브젝트를 만든다.
    //    표에 없는 루트는 Misc로 보내고 경고한다 — 새 매니저가 생기면 여기 한 줄을 더할 것.
    static readonly Dictionary<string, string> SystemsTable = new Dictionary<string, string>
    {
        { "GameBoot", "Core" }, { "SaveManager", "Core" }, { "SceneTransitionManager", "Core" },
        { "ScreenEffectManager", "Core" }, { "SystemsSceneEnforcer", "Core" },
        { "DayNightCycle", "World" }, { "RaidManager", "World" }, { "RaidMapManager", "World" },
        { "HideoutModuleManager", "World" },
        { "QuestManager", "Progress" }, { "DailyQuestManager", "Progress" }, { "AchievementManager", "Progress" },
        { "TraitManager", "Progress" }, { "ReputationManager", "Progress" }, { "NPCRelationshipManager", "Progress" },
        { "CurrencyManager", "Progress" }, { "MainStash", "Progress" }, { "PostRaidEventManager", "Progress" },
        { "StoryLocale", "Story" }, { "StoryPlayer", "Story" }, { "StoryTriggerManager", "Story" },
        { "NarrationUI", "Story" }, { "NoteUI", "Story" }, { "TutorialPrompt", "Story" },
        { "UIManager", "UI" }, { "ToastManager", "UI" },
        { "PlayerRig", "Player" },
    };

    // ── 지형 이름 표 — 먼저 맞는 줄. 실측 이름(Zone1·실내·안전가옥·하이드아웃) 기준.
    static readonly (Regex re, string bucket)[] EnvTable =
    {
        (new Regex(@"^(Floor|Ground|Road|LotF|Lotf|Rug)"), "Ground"),
        (new Regex(@"^(OB_|MD_|RB_|Edge|JX|Median|Barricade)"), "Roads"),
        (new Regex(@"^(SP_|SZ_|P_|Dressing|Board|Prop|Crate|Shelf)"), "Scatter"),
        (new Regex(@"^(Bldg|Apt|Tower|CollapsedMall|Pharmacy|DomeCore|Yard|Park|Lot|GH_|UR|PA_|AW|Wall|W_|Rib|Backdrop|Container|Gate|Village|Safehouse|Pawnshop|Furniture|Repair|Medical|BlackMarket|Med_Gate)"), "Structures"),
    };
    // 지오메트리뿐이어도 이름이 이러면 게임플레이 표식이다(출구 메시 등).
    static readonly Regex GameplayName = new Regex(@"^(Exit|Spawn|Entrance|Door)");
    static readonly Regex LightName = new Regex(@"^(Sun|Bulb|CeilingLight|Lamp|Light)");

    static readonly System.Type[] ControllerTypes =
        { typeof(MapSpawnController), typeof(RaidManager), typeof(NavGrid),
          typeof(HideoutController), typeof(HideoutDiorama), typeof(HideoutDockPanel) };
    static readonly System.Type[] EnemyTypes = { typeof(SpawnZone), typeof(EnemyController), typeof(EnemySpawner) };
    static readonly System.Type[] LootTypes = { typeof(LootContainer), typeof(ItemSpawnPoint), typeof(WorldItem) };
    static readonly System.Type[] GameplayTypes =
        { typeof(SpawnPoint), typeof(SceneDoor3D), typeof(BuildingEntrance),
          typeof(BlockedPassage), typeof(QuestPoiZone), typeof(StoryAreaTrigger), typeof(InteractableObject) };

    // ── 메뉴 ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/TopDown/개발/하이어라키 정리 (열린 씬)")]
    static void OrganizeOpenScenes()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            int moved = Organize(s, sb);
            if (moved > 0) EditorSceneManager.MarkSceneDirty(s);
        }
        Debug.Log("[하이어라키 정리] 열린 씬\n" + sb);
    }

    [MenuItem("Tools/TopDown/개발/하이어라키 정리 (빌드세팅 전체 씬)")]
    public static void OrganizeBuildScenes()
    {
        if (EditorSceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveOpenScenes();
        string current = EditorSceneManager.GetActiveScene().path;

        var sb = new StringBuilder();
        int sceneCount = 0;
        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (string.IsNullOrEmpty(entry.path) || !System.IO.File.Exists(entry.path)) continue;
            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            int moved = Organize(scene, sb);
            if (moved > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sceneCount++;
            }
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        Debug.Log($"[하이어라키 정리] 빌드세팅 씬 중 {sceneCount}개 정리·저장\n" + sb);
        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("하이어라키 정리", $"씬 {sceneCount}개 정리·저장.\n자세한 내용은 콘솔.", "확인");
    }

    // ── 본체 ─────────────────────────────────────────────────────────────
    /// <summary>씬 하나를 규약대로 묶는다. 옮긴 오브젝트 수를 돌려준다(0이면 이미 정리돼 있음).
    /// 빌더는 저장 직전에 이걸 부른다 — 로그가 필요 없으면 sb는 null.</summary>
    public static int Organize(Scene scene, StringBuilder sb = null)
    {
        if (!scene.IsValid() || !scene.isLoaded) return 0;
        var moved = new Dictionary<string, int>();
        int total;

        if (scene.name == SystemsScene.SceneName) total = OrganizeSystems(scene, moved);
        else
        {
            var map = scene.GetRootGameObjects().FirstOrDefault(g => g.name == MapRoot);
            total = map != null ? OrganizeMap(scene, map.transform, moved) : OrganizeGeneric(scene, moved);
        }

        sb?.AppendLine($"  {scene.name}: {total}개 이동" +
                       (moved.Count > 0 ? " — " + string.Join(", ", moved.Select(kv => $"{kv.Key} {kv.Value}")) : ""));
        return total;
    }

    // Systems: 루트를 표대로 루트 폴더에. 폴더는 detachOnPersist — 매니저의 DDOL을 살린다.
    static int OrganizeSystems(Scene scene, Dictionary<string, int> moved)
    {
        int n = 0;
        foreach (var go in Candidates(scene, null).ToList())   // 순회 중에 루트를 옮기므로 목록을 먼저 고정
        {
            if (!SystemsTable.TryGetValue(go.name, out var folder))
            {
                folder = "Misc";
                Debug.LogWarning($"[하이어라키 정리] Systems 표에 없는 루트 '{go.name}' → Misc. " +
                                 "SceneHierarchyOrganizer.SystemsTable에 한 줄 추가할 것.");
            }
            n += MoveTo(go, EnsureFolder(scene, null, folder, detachOnPersist: true), folder, moved);
        }
        Tidy(scene, null, SystemsFolders);
        return n;
    }

    // 맵: Map 아래를 기능별로. 씬 루트의 떠돌이(Sun3D 등)도 Map 안으로 들인다 — 단 싱글톤은 제외.
    static int OrganizeMap(Scene scene, Transform map, Dictionary<string, int> moved)
    {
        int n = 0;
        var list = Candidates(scene, map).ToList();
        foreach (var root in scene.GetRootGameObjects())
            if (root.transform != map && !IsFolder(root.transform) && !IsSingletonRoot(root)) list.Add(root);

        foreach (var go in list)
        {
            var (folder, sub) = Classify(go, generic: false);
            var f = EnsureFolder(scene, map, folder, false);
            if (sub != null) f = EnsureFolder(scene, f, sub, false);
            n += MoveTo(go, f, sub != null ? $"{folder}/{sub}" : folder, moved);
        }
        Tidy(scene, map, MapFolders);
        var env = map.Find("Environment");
        if (env != null) Tidy(scene, env, EnvFolders);
        return n;
    }

    // Map이 없는 씬(룩 체크 등): 같은 판정을 루트 폴더로.
    static int OrganizeGeneric(Scene scene, Dictionary<string, int> moved)
    {
        int n = 0;
        foreach (var go in Candidates(scene, null).ToList())
        {
            if (IsSingletonRoot(go)) continue;   // 루트여서 DDOL이 먹던 것 — 폴더에 넣으면 끊긴다
            var (folder, sub) = Classify(go, generic: true);
            var f = EnsureFolder(scene, null, folder, false);
            if (sub != null) f = EnsureFolder(scene, f, sub, false);
            n += MoveTo(go, f, sub != null ? $"{folder}/{sub}" : folder, moved);
        }
        Tidy(scene, null, GenericFolders);
        var env = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Environment" && IsFolder(g.transform));
        if (env != null) Tidy(scene, env.transform, EnvFolders);
        return n;
    }

    // ── 판정 ─────────────────────────────────────────────────────────────
    static (string folder, string sub) Classify(GameObject go, bool generic)
    {
        if (generic && go.GetComponent<Camera>() != null) return ("Cameras", null);
        if (go.GetComponent<Light>() != null || go.GetComponent<UnityEngine.Rendering.Volume>() != null) return ("Lighting", null);
        if (LightName.IsMatch(go.name) && go.GetComponentInChildren<Light>(true) != null && !HasAny(go, GameplayTypes))
            return ("Lighting", null);   // 천장등 묶음처럼 자식에 광원만 든 컨테이너
        if (HasAny(go, ControllerTypes) || IsControllerLike(go)) return ("Controllers", null);
        if (HasAny(go, EnemyTypes)) return ("Enemies", null);
        if (HasAny(go, LootTypes)) return ("Loot", null);            // 루팅 상자는 InteractableObject도 있다 — Gameplay보다 먼저
        if (go.GetComponent<NPCController>() != null) return ("NPCs", null);
        if (HasAny(go, GameplayTypes) || GameplayName.IsMatch(go.name)) return ("Gameplay", null);
        if (generic && go.GetComponentInChildren<Animator>(true) != null) return ("Characters", null);
        // 컴포넌트도 자식도 없는 빈 오브젝트 = 위치 표식(RoomCenter 등).
        if (go.GetComponents<Component>().Length == 1 && go.transform.childCount == 0) return ("Gameplay", null);
        return ("Environment", EnvBucket(go.name));
    }

    static string EnvBucket(string name)
    {
        foreach (var (re, bucket) in EnvTable) if (re.IsMatch(name)) return bucket;
        return "Misc";
    }

    static bool HasAny(GameObject go, System.Type[] types)
    {
        // 컴포넌트가 아닌 타입(정적 클래스 등)이 섞여도 멈추지 않게 — BuildingReturn이 static class라 한 번 멈췄다.
        foreach (var t in types) if (typeof(Component).IsAssignableFrom(t) && go.GetComponent(t) != null) return true;
        return false;
    }

    /// <summary>렌더러 없이 이름이 ~Controller/~Manager/~Director인 스크립트만 붙은 것 = 제어 오브젝트.</summary>
    static bool IsControllerLike(GameObject go)
    {
        if (go.GetComponent<Renderer>() != null) return false;
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            var n = mb.GetType().Name;
            if (n.EndsWith("Controller") || n.EndsWith("Manager") || n.EndsWith("Director")) return true;
        }
        return false;
    }

    /// <summary>정적 <c>Instance</c>를 가진 스크립트가 붙은 루트 — 대개 Awake에서 DDOL을 건다.
    /// 폴더에 넣으면 DDOL이 끊기므로 맵·일반 씬에선 루트에 그대로 둔다(Systems는 Persist로 해결).</summary>
    static bool IsSingletonRoot(GameObject go)
    {
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            var t = mb.GetType();
            if (t.GetProperty("Instance", F) != null || t.GetField("Instance", F) != null) return true;
        }
        return false;
    }

    // ── 폴더 조작 ────────────────────────────────────────────────────────
    static bool IsFolder(Transform t) => t != null && t.GetComponent<HierarchyFolder>() != null;

    /// <summary>parent(null이면 씬 루트) 바로 아래의 비-폴더 오브젝트 + 폴더 속을 재귀로. 이미 정리된 것도 다시 판정한다.</summary>
    static IEnumerable<GameObject> Candidates(Scene scene, Transform parent)
    {
        var level = parent == null
            ? scene.GetRootGameObjects().Select(g => g.transform).ToList()
            : Enumerable.Range(0, parent.childCount).Select(parent.GetChild).ToList();
        foreach (var t in level)
        {
            if (IsFolder(t)) { foreach (var c in Candidates(scene, t)) yield return c; }
            else if (CanMove(t.gameObject)) yield return t.gameObject;
        }
    }

    /// <summary>프리팹 인스턴스 **내부**는 옮길 수 없다(바깥 루트만 된다).</summary>
    static bool CanMove(GameObject go) =>
        !PrefabUtility.IsPartOfPrefabInstance(go) || PrefabUtility.IsOutermostPrefabInstanceRoot(go);

    static Transform EnsureFolder(Scene scene, Transform parent, string name, bool detachOnPersist)
    {
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name && IsFolder(c)) { c.GetComponent<HierarchyFolder>().detachOnPersist = detachOnPersist; return c; }
            }
        }
        else
        {
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == name && IsFolder(r.transform)) { r.GetComponent<HierarchyFolder>().detachOnPersist = detachOnPersist; return r.transform; }
        }

        var go = new GameObject(name);
        go.AddComponent<HierarchyFolder>().detachOnPersist = detachOnPersist;
        // ⚠️ new GameObject는 **활성 씬**에 생긴다. 빌더는 대상 씬을 활성으로 두지 않으므로 옮겨야 한다.
        if (go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    static int MoveTo(GameObject go, Transform folder, string label, Dictionary<string, int> moved)
    {
        if (go.transform.parent == folder) return 0;
        go.transform.SetParent(folder, true);   // 월드 위치 유지 — 폴더는 항등이지만 혹시를 대비
        moved[label] = moved.TryGetValue(label, out var c) ? c + 1 : 1;
        return 1;
    }

    /// <summary>빈 폴더를 지우고 순서를 규약대로 맞춘다.</summary>
    static void Tidy(Scene scene, Transform parent, string[] order)
    {
        var folders = parent == null
            ? scene.GetRootGameObjects().Select(g => g.transform).Where(IsFolder).ToList()
            : Enumerable.Range(0, parent.childCount).Select(parent.GetChild).Where(IsFolder).ToList();

        foreach (var f in folders.Where(f => f.childCount == 0).ToList())
        { folders.Remove(f); Object.DestroyImmediate(f.gameObject); }

        int idx = 0;
        foreach (var name in order)
        {
            var f = folders.FirstOrDefault(x => x.name == name);
            if (f == null) continue;
            if (parent != null) f.SetSiblingIndex(idx);
            else f.SetSiblingIndex(idx);
            idx++;
        }
    }
}
#endif
