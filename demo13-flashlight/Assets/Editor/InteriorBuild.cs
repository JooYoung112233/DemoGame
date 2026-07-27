#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 건물 '내부' 씬 공용 빌더 — 전당포(`PawnshopGreyboxLayout`) 패턴의 재사용 헬퍼.
/// (docs/level-scrapmarket.md 2026-07-11 "건물 모델 = 전당포식 씬 전환")
///
/// 모델: 외부 맵(Zone1)의 건물은 **껍데기(외벽+문)** 뿐 → 문의 `BuildingEntrance` 트리거로 이 내부 씬 진입 →
///       내부 출구 트리거로 외부 복귀(`Zone1` / `from_&lt;건물&gt;` 스폰). 내부는 콘텐츠 전용
///       (Systems 씬이 카메라·조명·매니저·플레이어 공급).
///
/// 좌표 = 로컬 (0,0)~(w,h). 각 건물 빌더는 이 헬퍼로 바닥/벽/스폰/출구/루트/적만 얹으면 된다.
/// </summary>
public static class InteriorBuild
{
    /// <summary>내부 씬 시작 — 빈 씬 + 루트 오브젝트 반환.</summary>
    public static GameObject Begin(out UnityEngine.SceneManagement.Scene scene)
        => GreyboxBuild.BeginScene(out scene);

    /// <summary>바닥 + 외벽 4면(두께 1m). 문 갭은 벽 세그먼트를 나눠서 만든다.</summary>
    public static int Shell(GameObject m, float w, float h)
    {
        int n = 0;
        n += GreyboxBuild.Floor(m, "Floor", w * 0.5f, h * 0.5f, w, h);
        n += GreyboxBuild.WallSeg(m, "W_S", 0f, 0f, w, 1f);
        n += GreyboxBuild.WallSeg(m, "W_N", 0f, h - 1f, w, h);
        n += GreyboxBuild.WallSeg(m, "W_W", 0f, 0f, 1f, h);
        n += GreyboxBuild.WallSeg(m, "W_E", w - 1f, 0f, w, h);
        return n;
    }

    /// <summary>남쪽 벽에 문 갭을 뚫는다(Shell 뒤에 호출 — W_S를 지우고 좌/우로 재생성).</summary>
    public static int DoorGapSouth(GameObject m, float w, float doorX, float doorW = 2f)
    {
        var old = m.transform.Find("W_S");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        int n = 0;
        float x0 = Mathf.Max(0f, doorX - doorW * 0.5f);
        float x1 = Mathf.Min(w, doorX + doorW * 0.5f);
        if (x0 > 0.01f) n += GreyboxBuild.WallSeg(m, "W_S_a", 0f, 0f, x0, 1f);
        if (x1 < w - 0.01f) n += GreyboxBuild.WallSeg(m, "W_S_b", x1, 0f, w, 1f);
        return n;
    }

    /// <summary>진입 스폰(외부에서 들어왔을 때 서는 자리). pointId 주입.</summary>
    public static int Spawn(GameObject m, string pointId, float x, float y)
    {
        if (GreyboxBuild.Marker(m, "gb_spawn", $"Spawn_{pointId}", x, y) == 0) return 0;
        var t = Find(m.transform, $"Spawn_{pointId}");
        var sp = t != null ? t.GetComponentInChildren<SpawnPoint>() : null;
        if (sp != null)
        {
            var so = new SerializedObject(sp);
            var p = so.FindProperty("pointId");
            if (p != null) { p.stringValue = pointId; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    /// <summary>출구 트리거 — 밟으면 외부 맵으로 복귀(BuildingEntrance, isExit). 전당포 Exit 패턴.</summary>
    public static int Exit(GameObject m, string name, float x, float y, string targetScene, string spawnId,
                           float w = 2f, float h = 1f)
    {
        if (GreyboxBuild.Marker(m, "gb_exit", name, x, y) == 0) return 0;
        var t = Find(m.transform, name);
        if (t == null) return 0;
        var go = t.gameObject;

        // gb_exit의 InteractableObject(ExitPoint, E키)는 트리거 전환과 중복 → 제거.
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null) Object.DestroyImmediate(io);

        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        box.isTrigger = true;
        box.size = new Vector2(w, h);

        var be = go.GetComponent<BuildingEntrance>();
        if (be == null) be = go.AddComponent<BuildingEntrance>();
        be.Configure(targetScene, spawnId, true);
        return 1;
    }

    /// <summary>바닥 루트 앵커(ItemSpawnPoint.Ground) — 예산제가 분배.</summary>
    public static int GroundLoot(GameObject m, string name, float x, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(m.transform, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        SetType(go.AddComponent<ItemSpawnPoint>(), 0);
        return 1;
    }

    /// <summary>상자(gb_crate) + Container 앵커(linkedContainer 주입).</summary>
    public static int Crate(GameObject m, string name, float x, float y)
    {
        if (GreyboxBuild.Marker(m, "gb_crate", name, x, y) == 0) return 0;
        var t = Find(m.transform, name);
        if (t == null) return 0;
        var lc = t.GetComponentInChildren<LootContainer>();
        var sp = t.gameObject.AddComponent<ItemSpawnPoint>();
        SetType(sp, 1);
        if (lc != null)
        {
            var so = new SerializedObject(sp);
            var lk = so.FindProperty("linkedContainer");
            if (lk != null) { lk.objectReferenceValue = lc; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    /// <summary>적 스폰 존.</summary>
    public static int Enemy(GameObject m, string name, float cx, float cy, float w, float h, string unitKey, int count)
    {
        var go = new GameObject(name);
        go.transform.SetParent(m.transform, false);
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.AddComponent<SpawnZone>().Setup(new Vector3(w, 0f, h), count, unitKey);
        return 1;
    }

    /// <summary>루팅 예산 분배기 — 내부 씬도 제 예산을 갖는다(프로파일 경로 지정).</summary>
    public static int Controller(GameObject m, string profilePath, string regionId)
    {
        var go = new GameObject("MapSpawnController");
        go.transform.SetParent(m.transform, false);
        var c = go.AddComponent<MapSpawnController>();
        var profile = AssetDatabase.LoadAssetAtPath<MapSpawnProfile>(profilePath);
        if (profile == null) Debug.LogWarning($"[InteriorBuild] 프로파일 로드 실패: {profilePath}");
        var so = new SerializedObject(c);
        var p  = so.FindProperty("profile");              if (p  != null) p.objectReferenceValue = profile;
        var r  = so.FindProperty("regionIdOverride");     if (r  != null) r.stringValue = regionId;
        var fb = so.FindProperty("fallbackToRegionLoot"); if (fb != null) fb.boolValue = true;
        // 내부 씬 표시 — 런타임에 GameTuning.interiorLootBudgetMult가 곱해진다(맵 전체 예산 그대로 쓰면 과다).
        //   루트 **테이블**은 지역 확률(regionId) 그대로 → 내부 파밍도 지역 확률에 맞춰 나온다.
        var it = so.FindProperty("isInterior");           if (it != null) it.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    /// <summary>씬 저장 + 빌드세팅 등록(안 하면 진입 시 LoadSceneAsync 실패).</summary>
    public static void End(UnityEngine.SceneManagement.Scene scene, string scenePath, int n, string label)
    {
        GreyboxBuild.EndScene(scene, scenePath, n, label);
        AddToBuildSettings(scenePath);
        AssetDatabase.SaveAssets();
    }

    public static void AddToBuildSettings(string scenePath)
    {
        var cur = EditorBuildSettings.scenes;
        foreach (var s in cur) if (s.path == scenePath) return;
        var arr = new EditorBuildSettingsScene[cur.Length + 1];
        System.Array.Copy(cur, arr, cur.Length);
        arr[cur.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = arr;
        Debug.Log($"[InteriorBuild] 빌드세팅 등록: {scenePath}");
    }

    static void SetType(ItemSpawnPoint sp, int type)
    {
        var so = new SerializedObject(sp);
        var t = so.FindProperty("spawnType");
        if (t != null) { t.enumValueIndex = type; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    public static Transform Find(Transform t, string n)
    {
        if (t.name == n) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = Find(t.GetChild(i), n);
            if (r != null) return r;
        }
        return null;
    }
}
#endif
