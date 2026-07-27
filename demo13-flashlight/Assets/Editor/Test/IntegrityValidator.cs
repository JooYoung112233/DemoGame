#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬·데이터 정합성 검증기(QA) — "컴파일·플레이 전엔 안 보이는 배선 버그"를 메뉴 한 번으로 잡는다.
/// (이번 지역1 작업에서 사람이 놓쳤던 부류: 스폰/탈출 미주입, StatDB에 없는 유닛키, 빌드세팅 누락,
///  MapSpawnProfile 참조 결손 등.)
///
/// 데이터 검사(씬 불필요) + 씬 검사(빌드세팅 씬을 Additive로 하나씩 열어 조사 → 닫기, 현재 씬 유지).
/// Console에 [QA] ERROR/WARN 상세 + 마지막 요약 다이얼로그.
///
/// 메뉴: Tools ▸ TopDown ▸ QA ▸ 정합성 검증
/// </summary>
public static class IntegrityValidator
{
    static int _err, _warn, _ok;

    // 게임플레이(레이드/내부/허브) 씬 = 스폰·탈출·유닛 배선을 검사. Systems는 매니저 씬이라 제외.
    static readonly HashSet<string> SkipScenes = new HashSet<string> { "Systems" };

    [MenuItem("Tools/TopDown/QA/정합성 검증", priority = -50)]
    public static void Run()
    {
        // 플레이 중엔 OpenScene이 예외를 던진다.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("QA 정합성 검증", "플레이 모드에서는 실행할 수 없습니다.\n(씬을 여는 검사라 Play를 멈추고 실행하세요.)", "확인");
            return;
        }
        _err = _warn = _ok = 0;
        Debug.Log("<b>[QA] 정합성 검증 시작</b> ───────────────");

        var statUnitIds = LoadStatUnitIds();
        var buildScenes = BuildSceneNames();

        ValidateData(statUnitIds, buildScenes);
        ValidateScenes(statUnitIds, buildScenes);

        string summary = $"[QA] 검증 완료 — 오류 {_err} · 경고 {_warn} · 통과 {_ok}";
        if (_err > 0) Debug.LogError($"<b>{summary}</b> (Console의 [QA] ERROR 확인)");
        else if (_warn > 0) Debug.LogWarning($"<b>{summary}</b>");
        else Debug.Log($"<b><color=#5c5>{summary}</color></b>");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("QA 정합성 검증",
                $"오류 {_err} · 경고 {_warn} · 통과 {_ok}\n\n" +
                (_err > 0 ? "❌ 오류가 있습니다. Console에서 [QA] ERROR 확인." :
                 _warn > 0 ? "⚠️ 경고가 있습니다. Console 확인." : "✅ 이상 없음."),
                "확인");
    }

    // ── 결과 헬퍼 ────────────────────────────────────────────────────
    static void Err(string ctx, string msg)  { _err++;  Debug.LogError($"[QA] ERROR · {ctx} — {msg}"); }
    static void Warn(string ctx, string msg) { _warn++; Debug.LogWarning($"[QA] WARN · {ctx} — {msg}"); }
    static void Ok()                          { _ok++; }

    // ── 데이터 검사 (씬 불필요) ──────────────────────────────────────
    static void ValidateData(HashSet<string> statUnitIds, HashSet<string> buildScenes)
    {
        // StatDB 유닛 id 공백/중복
        if (statUnitIds == null)
            Warn("StatDB", "Resources/Data/StatDB.asset 로드 실패 — 유닛키 검증 스킵");
        else if (statUnitIds.Count == 0)
            Warn("StatDB", "유닛이 하나도 없음");
        else Ok();

        // MapSpawnProfile: profileId 공백/중복
        var profIds = new Dictionary<string, string>();
        foreach (var guid in AssetDatabase.FindAssets("t:MapSpawnProfile"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var p = AssetDatabase.LoadAssetAtPath<MapSpawnProfile>(path);
            if (p == null) continue;
            if (string.IsNullOrEmpty(p.profileId)) { Warn("MapSpawnProfile", $"{path}: profileId 비어 있음"); continue; }
            if (profIds.TryGetValue(p.profileId, out var other))
                Err("MapSpawnProfile", $"profileId '{p.profileId}' 중복: {path} ↔ {other}");
            else { profIds[p.profileId] = path; Ok(); }
        }

        // WorldRegionCatalog: sceneName이 채워진 지역은 빌드세팅에 등록돼야 함(빈 것 = 미구현, 검사 제외)
        foreach (var r in WorldRegionCatalog.All)
        {
            if (string.IsNullOrEmpty(r.sceneName)) continue;   // 미구현 지역은 정상
            if (!buildScenes.Contains(r.sceneName))
                Err("WorldRegionCatalog", $"지역 '{r.regionId}'의 sceneName '{r.sceneName}'이 빌드세팅에 없음 → 출전 시 로드 실패");
            else Ok();
        }
    }

    // ── 씬 검사 (빌드세팅 씬을 Additive로 열어 조사) ─────────────────
    static void ValidateScenes(HashSet<string> statUnitIds, HashSet<string> buildScenes)
    {
        foreach (var bs in EditorBuildSettings.scenes)
        {
            if (!bs.enabled) continue;
            string name = System.IO.Path.GetFileNameWithoutExtension(bs.path);
            if (SkipScenes.Contains(name)) continue;
            if (!System.IO.File.Exists(bs.path)) { Err("빌드세팅", $"등록됐지만 파일 없음: {bs.path}"); continue; }

            // ★ 사용자가 계층에 올려둔 씬은 **로드 여부와 무관하게** 손대지 않는다.
            //   (언로드 상태 씬을 열었다 CloseScene(removeScene:true)로 닫으면 계층 구성이 바뀐다.)
            var scene = SceneManager.GetSceneByPath(bs.path);
            bool userHasIt = scene.IsValid();
            if (userHasIt && !scene.isLoaded)
            { Warn(name, "계층에 언로드 상태로 있어 검사 생략(구성 보존) — 로드 후 다시 실행하세요"); continue; }

            if (!userHasIt) scene = EditorSceneManager.OpenScene(bs.path, OpenSceneMode.Additive);

            try { ValidateOneScene(scene, name, statUnitIds, buildScenes); }
            finally { if (!userHasIt && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
        }
    }

    static void ValidateOneScene(Scene scene, string name, HashSet<string> statUnitIds, HashSet<string> buildScenes)
    {
        var roots = scene.GetRootGameObjects();

        var spawns    = Collect<SpawnPoint>(roots);
        var exitsIO   = Collect<InteractableObject>(roots).Where(i => i.Type == InteractableObject.InteractType.ExitPoint).ToList();
        var entrances = Collect<BuildingEntrance>(roots);
        var zones     = Collect<SpawnZone>(roots);
        var controllers = Collect<MapSpawnController>(roots);
        var anchors   = Collect<ItemSpawnPoint>(roots);

        // 1) SpawnPoint pointId 공백
        foreach (var sp in spawns)
        {
            if (string.IsNullOrEmpty(sp.PointId))
                Err(name, $"SpawnPoint '{sp.name}'의 pointId가 비어 있음 → 복귀 라우팅 실패");
            else Ok();
        }

        // 2) ExitPoint(InteractableObject) targetScene 유효
        foreach (var io in exitsIO)
            CheckTargetScene(name, $"ExitPoint '{io.name}'", io.TargetScene, buildScenes);

        // 3) BuildingEntrance targetScene 유효 (진입/출구 공통)
        foreach (var be in entrances)
            CheckTargetScene(name, $"BuildingEntrance '{be.name}'", be.TargetScene, buildScenes);

        // 4) SpawnZone.UnitKey가 StatDB에 실재
        foreach (var z in zones)
        {
            if (string.IsNullOrEmpty(z.UnitKey))
                Warn(name, $"SpawnZone '{z.name}'의 unitKey가 비어 있음 → EnemySpawner 기본키 폴백");
            else if (statUnitIds != null && !statUnitIds.Contains(z.UnitKey))
                Err(name, $"SpawnZone '{z.name}'의 unitKey '{z.UnitKey}'가 StatDB에 없음 → 적이 인스펙터 기본 스탯으로 폴백");
            else Ok();
        }

        // 5) MapSpawnController가 있으면 profile 참조 + 앵커 존재
        foreach (var c in controllers)
        {
            var so = new SerializedObject(c);
            var prof = so.FindProperty("profile");
            if (prof == null || prof.objectReferenceValue == null)
                Err(name, $"MapSpawnController '{c.name}'의 profile 미지정 → 예산제 무동작(루팅 0)");
            else Ok();

            if (anchors.Count == 0)
                Warn(name, $"MapSpawnController가 있지만 ItemSpawnPoint 앵커가 0개 → 분배할 대상 없음");
        }

        // 6) 레이드/내부 씬 스모크 — 스폰이 하나도 없으면 진입 후 플레이어가 어디 설지 모호
        bool looksPlayable = name == "Zone1" || name.StartsWith("Int_") || name == "ScrapMarket_GB";
        if (looksPlayable && spawns.Count == 0)
            Warn(name, "게임플레이 씬인데 SpawnPoint가 0개 → 진입 스폰 불명확");
    }

    static void CheckTargetScene(string ctx, string who, string target, HashSet<string> buildScenes)
    {
        if (string.IsNullOrEmpty(target))
            Err(ctx, $"{who}의 targetScene이 비어 있음 → 전환 무동작");
        else if (!buildScenes.Contains(target))
            Err(ctx, $"{who}의 targetScene '{target}'이 빌드세팅에 없음 → LoadSceneAsync 실패");
        else Ok();
    }

    // ── 로더 ─────────────────────────────────────────────────────────
    static HashSet<string> LoadStatUnitIds()
    {
        var db = Resources.Load<StatDB>("Data/StatDB");
        if (db == null) return null;
        return new HashSet<string>(db.GetAllUnitIds());
    }

    static HashSet<string> BuildSceneNames()
    {
        var set = new HashSet<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) set.Add(System.IO.Path.GetFileNameWithoutExtension(s.path));
        return set;
    }

    static List<T> Collect<T>(GameObject[] roots) where T : Component
    {
        var list = new List<T>();
        foreach (var r in roots) list.AddRange(r.GetComponentsInChildren<T>(true));
        return list;
    }
}
#endif
