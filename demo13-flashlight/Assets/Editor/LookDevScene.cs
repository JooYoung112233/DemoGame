#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// **룩 확인용 더미 씬** — 캐릭터를 정면에서 크게 보고 셰이더·외곽선·음영을 판단한다.
///
/// 왜 필요한가: 인게임은 55° 내려다보는 오소 쿼터뷰라 캐릭터가 작고 각도도 눕는다.
/// 그 화면으로 "외곽선이 굵은가 / 셀 밴딩이 몇 단으로 보이는가"를 판단할 수 없다
/// (2026-09-10 사용자 제안: "정면 샷으로 더미 씬에서 체크하자").
///
/// 씬 이름에 **MapTool**이 들어간다 = <see cref="MapToolScene"/>가 잡아내어
/// Systems·플레이어·UI 부트스트랩이 전부 건너뛴다. 순수하게 모델과 조명만 남는다.
///
/// 쓰는 법: `Tools ▸ TopDown ▸ 개발 ▸ 룩 체크 씬` → 씬이 열린다 → Play → 스크린샷.
/// </summary>
public static class LookDevScene
{
    const string ScenePath = "Assets/Scenes/MapTool_LookDev.unity";

    [MenuItem("Tools/TopDown/개발/룩 체크 씬 (캐릭터 정면)")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── 바닥 ── 캐릭터가 배경에서 어떻게 분리되는지 보려면 바닥이 있어야 한다.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        var groundMat = new Material(Shader.Find("BRB/Stylized"));
        groundMat.SetColor("_BaseColor", new Color(0.38f, 0.38f, 0.40f));
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // ── 뒷벽 ── 실루엣이 밝은 배경/어두운 배경에서 각각 어떻게 읽히는지 같이 본다.
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "BackWall";
        wall.transform.position = new Vector3(0f, 2.5f, 3.2f);
        wall.transform.localScale = new Vector3(14f, 5f, 0.3f);
        var wallMat = new Material(Shader.Find("BRB/Stylized"));
        wallMat.SetColor("_BaseColor", new Color(0.22f, 0.23f, 0.27f));
        wall.GetComponent<Renderer>().sharedMaterial = wallMat;

        // ── 조명 ── 야외 프리셋과 같은 성격(정면 3/4에서 들어오는 키라이트).
        var sunGo = new GameObject("Sun");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(1f, 0.96f, 0.89f);
        sun.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(38f, -40f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.52f, 0.56f, 0.64f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.43f, 0.47f);
        RenderSettings.ambientGroundColor  = new Color(0.24f, 0.24f, 0.26f);

        // ── 모델 두 개 ── 플레이어와 밴딧을 나란히. 같은 셰이더로 갈아끼워 **같은 조건**에서 본다.
        int placed = 0;
        var toon = Shader.Find("BRB/Toon");
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PlayerRig.prefab");
        GameObject heroPrefab = null;
        RuntimeAnimatorController heroCtrl = null;
        if (rig != null)
        {
            var tp = rig.GetComponentInChildren<TopDownPlayer>(true);
            if (tp != null)
            {
                var so = new SerializedObject(tp);
                heroPrefab = so.FindProperty("character3DPrefab")?.objectReferenceValue as GameObject;
                heroCtrl   = so.FindProperty("character3DController")?.objectReferenceValue as RuntimeAnimatorController;
            }
        }
        // 배율은 **인게임과 같은 값**으로 세운다 — 여기서 크기 비율을 보고 조절할 수 있어야 하므로
        //   (2026-09-10 사용자: "저 화면에서 캐릭터간의 크기도 조절할 거니까"). 값의 출처:
        //   · 플레이어 = PlayerRig의 character3DScale
        //   · 밴딧 일반/중장 = StatDB의 unit.scale ÷ 2 (EnemyController.ApplyUnitLook과 같은 식)
        float heroScale = 1f;
        if (rig != null)
        {
            var tp2 = rig.GetComponentInChildren<TopDownPlayer>(true);
            if (tp2 != null)
            {
                var so2 = new SerializedObject(tp2);
                var sp = so2.FindProperty("character3DScale");
                if (sp != null && sp.floatValue > 0.01f) heroScale = sp.floatValue;
            }
        }
        float meleeScale = UnitScale("bandit_melee_1"), tankScale = UnitScale("bandit_tank");

        if (heroPrefab != null) { Place(heroPrefab, heroCtrl, new Vector3(-1.5f, 0f, 0f), "Player", toon, heroScale); placed++; }
        else Debug.LogWarning("[LookDev] PlayerRig에서 캐릭터 프리팹을 못 찾음 — 플레이어 생략.");

        var bandit = Resources.Load<GameObject>("Characters/Bandit01");
        if (bandit != null)
        {
            Place(bandit, null, new Vector3(0f, 0f, 0f),    "Bandit_일반", toon, meleeScale);
            Place(bandit, null, new Vector3(1.6f, 0f, 0f),  "Bandit_중장", toon, tankScale);
            placed += 2;
        }
        else Debug.LogWarning("[LookDev] Resources/Characters/Bandit01 없음 — 밴딧 생략.");

        // 키 기준봉 1.8m — 눈으로 "사람 키"를 대볼 기준이 없으면 비율 판단이 안 된다.
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "기준봉 1.8m";
        pole.transform.position = new Vector3(-2.6f, 0.9f, 0f);
        pole.transform.localScale = new Vector3(0.06f, 1.8f, 0.06f);
        var poleMat = new Material(Shader.Find("BRB/Stylized"));
        poleMat.SetColor("_BaseColor", new Color(0.85f, 0.72f, 0.25f));
        pole.GetComponent<Renderer>().sharedMaterial = poleMat;

        // ── 카메라 3대 ────────────────────────────────────────────────
        //  정면·측면은 **형태를 보는 눈**이고, 쿼터뷰는 **실제 게임에서 어떻게 보이는가**다.
        //  룩을 정면에서만 맞추면 인게임(작고 눕은 각도)에서 전혀 다르게 보인다 —
        //  그래서 게임 설정(오소·pitch 55°·yaw 0°·size 3.6)을 그대로 가진 카메라를 같이 둔다.
        //  한 번에 하나만 켠다. 숫자 1/2/3으로 전환(LookDevCameras).
        MakeCam("Cam_1_정면",  new Vector3(0f, 1.15f, -6.4f),  Quaternion.Euler(4f, 0f, 0f),    false, 0f, true);
        MakeCam("Cam_2_측면",  new Vector3(-6.4f, 1.15f, 0f),  Quaternion.Euler(4f, 90f, 0f),   false, 0f, false);
        MakeCam("Cam_3_쿼터뷰(게임)", QuarterPos(), Quaternion.Euler(GamePitch, GameYaw, 0f),   true,  GameOrtho, false);

        BuildLampLane(toon, meleeScale);
        // 레인은 **옆에서** 본다 — 등 뒤에서 보면 빔이 어디를 비추는지는 보여도 콘 모양이 안 보인다.
        MakeCam("Cam_4_랜턴레인", new Vector3(-6f, 2.2f, LampLaneZ + 3f), Quaternion.Euler(13f, 85f, 0f), false, 0f, false);

        var switcher = new GameObject("LookDevCameras");
        switcher.AddComponent<LookDevCameraSwitcher>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Debug.Log($"[LookDev] 룩 체크 씬 생성 — 모델 {placed}개 · {ScenePath}\n"
                  + "  Play를 누르면 부트스트랩 없이 모델만 뜬다(씬 이름에 MapTool 포함).");
        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("룩 체크 씬", $"모델 {placed}개 배치 완료.\nPlay → 정면 샷으로 확인.", "확인");
    }

    // 인게임 카메라 설정 — PlayerRig의 값과 같아야 의미가 있다(바뀌면 여기도 같이).
    const float GamePitch = 55f, GameYaw = 0f, GameOrtho = 3.6f;

    /// <summary>랜턴 검증 레인의 z 시작점 — 캐릭터 전시 구역(z=0)과 겹치지 않게 뒤로 뺀다.</summary>
    const float LampLaneZ = 14f;

    /// <summary>**랜턴 검증 레인** — "진짜 손전등처럼 보이는가"를 눈으로 확인하는 구역.
    ///
    /// 빔은 허공에선 안 보인다. 맞는 면이 있어야 거리별 감쇠·그림자·콘 가장자리가 드러난다.
    /// 그래서 전방 2·4·6·8m에 상자를 세우고 10m에 벽을 둔다. 상자는 그림자도 던진다 —
    /// 손전등처럼 보이는 데 **그림자가 절반**이다(빛만 있고 그림자가 없으면 조명판이 된다).
    /// 등을 든 사람은 카메라 반대쪽(+Z)을 본다 = 관찰자 시점에서 빔의 옆면을 본다.</summary>
    static void BuildLampLane(Shader toon, float scale)
    {
        var lane = new GameObject("랜턴 검증 레인").transform;
        lane.position = new Vector3(0f, 0f, LampLaneZ);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Lane_Floor";
        floor.transform.SetParent(lane, false);
        floor.transform.localPosition = new Vector3(0f, 0f, 5f);
        floor.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
        floor.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.34f, 0.34f, 0.36f));

        // 거리 표식 겸 그림자 시험대
        float[] zs = { 2f, 4f, 6f, 8f };
        for (int i = 0; i < zs.Length; i++)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"Lane_Box_{zs[i]:0}m";
            box.transform.SetParent(lane, false);
            box.transform.localPosition = new Vector3(i % 2 == 0 ? -0.9f : 0.9f, 0.4f, zs[i]);
            box.transform.localScale = Vector3.one * 0.8f;
            box.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.55f, 0.52f, 0.48f));
        }

        var endWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        endWall.name = "Lane_Wall_10m";
        endWall.transform.SetParent(lane, false);
        endWall.transform.localPosition = new Vector3(0f, 1.5f, 10f);
        endWall.transform.localScale = new Vector3(6f, 3f, 0.3f);
        endWall.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.30f, 0.31f, 0.35f));

        // 등을 찬 사람 — 실제 게임과 같은 높이·각도로 WornLamp를 단다.
        var bandit = Resources.Load<GameObject>("Characters/Bandit01");
        Transform holder;
        if (bandit != null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(bandit);
            go.name = "LampHolder";
            go.transform.SetParent(lane, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;   // +Z(레인 안쪽)를 본다
            go.transform.localScale = Vector3.one * scale;
            ApplyToon(go, toon, "LampHolder");
            holder = go.transform;
        }
        else
        {
            holder = new GameObject("LampHolder").transform;
            holder.SetParent(lane, false);
        }

        var lampGo = new GameObject("WornLamp");
        lampGo.transform.SetParent(holder, false);
        lampGo.transform.localPosition = new Vector3(0f, 1.15f, 0.12f);   // 가슴 높이
        lampGo.transform.localRotation = Quaternion.Euler(16f, 0f, 0f);   // WornLamp.TiltDown과 같은 값
        lampGo.AddComponent<Light>();
        lampGo.AddComponent<WornLamp>();
    }

    static Material Mat(Color c)
    {
        var m = new Material(Shader.Find("BRB/Stylized"));
        m.SetColor("_BaseColor", c);
        return m;
    }

    static void ApplyToon(GameObject go, Shader toon, string name)
    {
        // ⚠️ 외곽선용 평균 법선은 **런타임에** 굽는다(LookDevCameraSwitcher.Start).
        //    여기서 구우면 복제 메시가 에셋이 아니라서 씬을 저장하는 순간 참조가 깨진다
        //    — 실제로 캐릭터가 통째로 청록색 덩어리로 렌더링됐다.
        if (toon == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var src = r.sharedMaterials;
            var dst = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                Color c = Color.white;
                if (src[i] != null)
                    c = src[i].HasProperty("_BaseColor") ? src[i].GetColor("_BaseColor") : src[i].color;
                dst[i] = new Material(toon) { name = name + "_Toon_" + i };
                dst[i].SetColor("_BaseColor", c);
            }
            r.sharedMaterials = dst;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }

    /// <summary>StatDB의 유닛 배율 → 화면 배율(EnemyController.ApplyUnitLook과 같은 식: scale ÷ 2).</summary>
    static float UnitScale(string key)
    {
        var db = AssetDatabase.LoadAssetAtPath<StatDB>("Assets/Resources/Data/StatDB.asset");
        if (db == null) return 1f;
        var u = db.GetUnit(key);
        return u == null ? 1f : Mathf.Clamp(u.scale / 2f, 0.5f, 3f);
    }

    /// <summary>쿼터뷰 카메라 위치 — 캐릭터 가운데(높이 1m)를 8m 뒤에서 내려다본다.</summary>
    static Vector3 QuarterPos()
    {
        var look = new Vector3(0f, 1f, 0f);
        var dir = Quaternion.Euler(GamePitch, GameYaw, 0f) * Vector3.forward;
        return look - dir * 8f;
    }

    static void MakeCam(string name, Vector3 pos, Quaternion rot, bool ortho, float size, bool on)
    {
        var go = new GameObject(name);
        var c = go.AddComponent<Camera>();
        c.orthographic = ortho;
        if (ortho) c.orthographicSize = size; else c.fieldOfView = 32f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.10f, 0.10f, 0.12f);
        c.nearClipPlane = 0.05f;
        c.farClipPlane = 60f;
        go.transform.SetPositionAndRotation(pos, rot);
        go.tag = "MainCamera";
        c.enabled = on;
    }

    /// <summary>모델 하나를 세우고, 셰이더를 카툰으로 갈아끼운다(색은 원본 유지).
    /// 배율은 **인게임과 같은 값**을 넣는다 — 여기서 크기 비율을 보고 판단하기 위해서다.</summary>
    static void Place(GameObject prefab, RuntimeAnimatorController ctrl, Vector3 pos, string name, Shader toon, float scale)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // 카메라(−Z)를 바라보게
        go.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);

        var an = go.GetComponentInChildren<Animator>(true);
        if (an != null)
        {
            if (ctrl != null) an.runtimeAnimatorController = ctrl;
            an.applyRootMotion = false;
            an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        ApplyToon(go, toon, name);
    }

    static void AddToBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
