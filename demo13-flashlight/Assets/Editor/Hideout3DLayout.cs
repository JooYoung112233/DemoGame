#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 은신처(컨테이너) — **3D 쿼터뷰 버전**. 2D `HideoutGreyboxLayout`의 3D 대응.
///
/// 좌표 규약(docs/3d-migration.md): 월드는 **XZ 평면**, 위쪽 +Y, 1u = 1m.
/// 2D 시절 좌표 (x, y)를 그대로 (x, 0, y)로 옮겼다 — 시설 배치가 2D판과 1:1로 대응한다.
///
/// 방 = 14 × 9 m 컨테이너 내부.
/// ★ **천장을 만들지 않는다** — pitch 55° 쿼터뷰에서 지붕이 있으면 내부가 아예 안 보인다.
///   컨테이너 "안"이라는 느낌은 4면 벽 + 어두운 조명으로 낸다.
///
/// 메뉴: Tools ▸ TopDown ▸ 빌드 ▸ 은신처 (3D)
/// </summary>
public static class Hideout3DLayout
{
    const string ScenePath = "Assets/Scenes/Hideout3D.unity";

    // 방 규격 — 2D판(MapW 14, MapH 9)과 동일
    const float RoomW = 14f, RoomD = 9f;
    const float WallH = 2.6f;    // 컨테이너 내부 높이(실측 약 2.4~2.7m)
    const float WallT = 0.3f;

    [MenuItem("Tools/TopDown/빌드/은신처 (3D)", priority = -98)]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = new GameObject("Map");
        int n = 0;

        // ── 재질 팔레트 (텍스처 없는 단색 — 3D 아트 규약) ──
        var matFloor = Mat(new Color(0.28f, 0.28f, 0.30f), 0.05f);
        var matWall  = Mat(new Color(0.38f, 0.40f, 0.38f), 0.08f);   // 골판 금속 컨테이너
        var matRib   = Mat(new Color(0.33f, 0.35f, 0.33f), 0.10f);

        // ── 바닥 ──
        n += Box(map, "Floor", new Vector3(RoomW * .5f, -0.05f, RoomD * .5f),
                 new Vector3(RoomW, 0.1f, RoomD), matFloor);

        // ── 외벽 4면 (천장 없음) ──
        float cx = RoomW * .5f, cz = RoomD * .5f, hy = WallH * .5f;
        n += Box(map, "Wall_S", new Vector3(cx, hy, -WallT * .5f),          new Vector3(RoomW + WallT * 2f, WallH, WallT), matWall);
        n += Box(map, "Wall_N", new Vector3(cx, hy, RoomD + WallT * .5f),   new Vector3(RoomW + WallT * 2f, WallH, WallT), matWall);
        n += Box(map, "Wall_W", new Vector3(-WallT * .5f, hy, cz),          new Vector3(WallT, WallH, RoomD), matWall);
        n += Box(map, "Wall_E", new Vector3(RoomW + WallT * .5f, hy, cz),   new Vector3(WallT, WallH, RoomD), matWall);

        // 골판 리브 — 컨테이너 느낌. 남·북 벽에 세로 홈을 얕게 덧붙인다(콜라이더 없음).
        for (float x = 0.6f; x < RoomW; x += 1.2f)
        {
            n += Deco(map, "Rib_S", new Vector3(x, hy, 0.02f),          new Vector3(0.18f, WallH * 0.92f, 0.10f), matRib);
            n += Deco(map, "Rib_N", new Vector3(x, hy, RoomD - 0.02f),  new Vector3(0.18f, WallH * 0.92f, 0.10f), matRib);
        }

        // ── 시설 ── 2D판과 같은 (x, y) → (x, 0, y)
        n += Facility(map, "Bed_BoxCot",       new Vector3(5f,  0f, 6.5f), new Vector3(2.0f, 0.55f, 1.0f), new Color(0.45f, 0.38f, 0.32f));
        n += Facility(map, "Workbench_Broken", new Vector3(9f,  0f, 6.5f), new Vector3(1.8f, 0.95f, 0.8f), new Color(0.50f, 0.45f, 0.38f));
        n += Facility(map, "Stash_창고",        new Vector3(11f, 0f, 6.5f), new Vector3(1.2f, 1.4f,  0.8f), new Color(0.30f, 0.62f, 0.55f));
        n += Facility(map, "Radio_라디오",      new Vector3(7f,  0f, 6.5f), new Vector3(0.8f, 0.6f,  0.6f), new Color(0.30f, 0.45f, 0.72f));
        n += Facility(map, "CookingBench",     new Vector3(11f, 0f, 2.5f), new Vector3(1.6f, 0.9f,  0.8f), new Color(0.60f, 0.45f, 0.30f));
        n += Facility(map, "MedicalBench",     new Vector3(5f,  0f, 2.5f), new Vector3(1.6f, 0.9f,  0.8f), new Color(0.70f, 0.70f, 0.72f));
        n += Facility(map, "Dispatch_파견",     new Vector3(8f,  0f, 2.5f), new Vector3(1.0f, 1.6f,  0.3f), new Color(0.66f, 0.42f, 0.24f));
        n += Facility(map, "Generator_발전기",  new Vector3(9f,  0f, 4.5f), new Vector3(1.0f, 1.0f,  1.0f), new Color(0.62f, 0.55f, 0.30f));

        // ── 진입 스폰 (2D판과 동일 위치) ──
        var spawn = new GameObject("default");
        spawn.transform.SetParent(map.transform, false);
        spawn.transform.position = new Vector3(3f, 0f, 4.5f);
        spawn.AddComponent<SpawnPoint>();
        n++;

        // ── 조명 ── 컨테이너 내부: 약한 천창 + 매달린 전구 한 개
        var sunGO = new GameObject("KeyLight");
        sunGO.transform.SetParent(map.transform, false);
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.55f;
        sun.color = new Color(0.80f, 0.86f, 1f);      // 위에서 새어드는 찬 빛
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(62f, -30f, 0f);

        var bulbGO = new GameObject("Bulb");
        bulbGO.transform.SetParent(map.transform, false);
        bulbGO.transform.position = new Vector3(7f, 2.3f, 4.5f);
        var bulb = bulbGO.AddComponent<Light>();
        bulb.type = LightType.Point;
        bulb.range = 11f;
        bulb.intensity = 3.2f;
        bulb.color = new Color(1f, 0.82f, 0.58f);      // 백열 전구
        bulb.shadows = LightShadows.Soft;
        n += 2;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.20f, 0.22f, 0.26f);
        RenderSettings.ambientEquatorColor = new Color(0.15f, 0.15f, 0.17f);
        RenderSettings.ambientGroundColor  = new Color(0.09f, 0.09f, 0.10f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[Hideout3D]</color> 생성 완료: {ScenePath} — 오브젝트 {n}개. " +
                  "천장 없음(쿼터뷰에서 내부가 보이도록). 시설 좌표는 2D판과 1:1.");
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────
    static Material Mat(Color c, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static int Box(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return 1;
    }

    /// <summary>순수 장식 — 콜라이더를 떼서 이동·판정에 관여하지 않게 한다.</summary>
    static int Deco(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return 1;
    }

    /// <summary>시설 — 바닥에 놓이도록 y를 높이 절반만큼 띄운다.</summary>
    static int Facility(GameObject parent, string name, Vector3 floorPos, Vector3 size, Color c)
    {
        var center = new Vector3(floorPos.x, size.y * .5f, floorPos.z);
        return Box(parent, name, center, size, Mat(c, 0.06f));
    }
}
#endif
