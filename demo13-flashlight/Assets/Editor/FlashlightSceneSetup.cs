using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class FlashlightSceneSetup : EditorWindow
{
    const float TILE_SIZE = 1f;

    [MenuItem("Tools/Setup Flashlight Prototype Scene")]
    static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene",
            "3D 쿼터뷰 손전등 프로토타입 씬을 생성합니다.\n(기존 씬 오브젝트를 먼저 삭제해주세요)\n\n계속?",
            "Yes", "Cancel"))
            return;

        // ===== Directional Light (낮밤용) =====
        var dirLightGO = new GameObject("Directional Light");
        var dirLight = dirLightGO.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 0f; // 밤: 완전 깜깜
        dirLight.color = Color.black;
        dirLight.shadows = LightShadows.Soft;
        dirLight.shadowStrength = 0.8f;
        dirLightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        Undo.RegisterCreatedObjectUndo(dirLightGO, "Create DirLight");

        // ===== Player =====
        var playerGO = new GameObject("Player");
        playerGO.transform.position = new Vector3(6, 0, 4);
        var playerCC = playerGO.AddComponent<CharacterController>();
        playerCC.radius = 0.3f;
        playerCC.height = 0.1f;
        playerCC.center = new Vector3(0, 0.05f, 0);
        var playerCtrl = playerGO.AddComponent<PlayerController>();
        Undo.RegisterCreatedObjectUndo(playerGO, "Create Player");

        // Player visual (빌보드 Quad)
        var playerVisual = CreateBillboardQuad("PlayerSprite", CreateCharacterTexture(), 0.6f, 0.9f);
        playerVisual.transform.SetParent(playerGO.transform);
        playerVisual.transform.localPosition = new Vector3(0, 0.45f, 0);
        var billboard = playerVisual.AddComponent<BillboardSprite>();

        // ===== Flashlight Pivot =====
        var flashPivot = new GameObject("FlashlightPivot");
        flashPivot.transform.SetParent(playerGO.transform);
        flashPivot.transform.localPosition = new Vector3(0, 0.6f, 0);

        // SpotLight (손전등 콘)
        var spotGO = new GameObject("SpotLight");
        spotGO.transform.SetParent(flashPivot.transform);
        spotGO.transform.localPosition = Vector3.zero;
        var spot = spotGO.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 90f;
        spot.innerSpotAngle = 50f;
        spot.range = 25f;
        spot.intensity = 15f;
        spot.color = new Color(1f, 0.93f, 0.75f);
        spot.shadows = LightShadows.Hard;
        spot.shadowStrength = 0.95f;
        spot.shadowNearPlane = 0.1f;

        // PointLight (기본 글로우)
        var glowGO = new GameObject("AmbientGlow");
        glowGO.transform.SetParent(playerGO.transform);
        glowGO.transform.localPosition = new Vector3(0, 0.5f, 0);
        var glow = glowGO.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 6f;
        glow.intensity = 2.5f;
        glow.color = new Color(0.6f, 0.65f, 0.8f);
        glow.shadows = LightShadows.Soft;

        // Wire FlashlightController
        var flashCtrl = playerGO.AddComponent<FlashlightController>();
        var flashSO = new SerializedObject(flashCtrl);
        flashSO.FindProperty("spotLight").objectReferenceValue = spot;
        flashSO.FindProperty("ambientGlow").objectReferenceValue = glow;
        flashSO.ApplyModifiedProperties();

        var playerSO = new SerializedObject(playerCtrl);
        playerSO.FindProperty("flashlightPivot").objectReferenceValue = flashPivot.transform;
        playerSO.ApplyModifiedProperties();

        // ===== Camera =====
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 7;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(6, 10, 4 - 7); // player(6,0,4) + offset(0,10,-7)
        cam.transform.rotation = Quaternion.Euler(55, 0, 0);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;

        // 포스트프로세싱 활성화
        var camData = cam.gameObject.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        var camFollow = cam.gameObject.AddComponent<CameraFollow>();
        var camSO = new SerializedObject(camFollow);
        camSO.FindProperty("target").objectReferenceValue = playerGO.transform;
        camSO.ApplyModifiedProperties();

        // ===== 폐허 분위기: 포스트프로세싱 Volume =====
        CreateAtmosphere();

        // ===== 먼지 파티클 (플레이어 주변) =====
        CreateDustParticles(playerGO);

        // ===== DayNightCycle =====
        var dnGO = new GameObject("DayNightCycle");
        var dnCycle = dnGO.AddComponent<DayNightCycle>();
        var dnSO = new SerializedObject(dnCycle);
        dnSO.FindProperty("directionalLight").objectReferenceValue = dirLight;
        dnSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        dnSO.ApplyModifiedProperties();

        // ===== HUD =====
        var hudGO = new GameObject("PrototypeHUD");
        var hud = hudGO.AddComponent<PrototypeHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        hudSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        hudSO.ApplyModifiedProperties();

        // ===== 환경 조명 (밤에 윤곽만 살짝 보이게) =====
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.06f); // 미세한 푸른빛
        RenderSettings.ambientSkyColor = new Color(0.03f, 0.03f, 0.05f);
        RenderSettings.ambientEquatorColor = new Color(0.02f, 0.02f, 0.03f);
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.skybox = null;
        RenderSettings.subtractiveShadowColor = Color.black;

        // 안개 (거리감 + 폐허 분위기, 밀도 약간 낮춤)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
        RenderSettings.fogDensity = 0.04f;

        // ===== 건물 생성 =====
        CreateBuilding();

        // ===== 네온사인 (입구 위) =====
        var neonGO = CreateNeonSign(dnCycle);

        // ===== 지붕 (건물 위) =====
        var roofGO = CreateRoof(playerGO);

        // ===== 벽 뒤 아웃라인 =====
        CreateWallOutline(playerGO, playerVisual);

        Debug.Log("[Flashlight Prototype] 3D 쿼터뷰 씬 생성 완료!");
    }

    static void CreateAtmosphere()
    {
        var volumeGO = new GameObject("PostProcessVolume");
        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1;
        Undo.RegisterCreatedObjectUndo(volumeGO, "Create PostProcess");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        // 비네트 — 화면 가장자리 어둡게 (터널 비전)
        var vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.3f);
        vignette.color.Override(Color.black);

        // 색 보정 — 탈색된 차가운 톤 (폐허)
        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.saturation.Override(-30f);
        colorAdj.contrast.Override(15f);
        colorAdj.colorFilter.Override(new Color(0.85f, 0.9f, 1f));

        // 블룸 — 손전등 빛번짐
        var bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(0.5f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(0.9f, 0.85f, 0.7f));

        // 필름 그레인 — 거친 질감
        var grain = profile.Add<FilmGrain>();
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.intensity.Override(0.4f);
        grain.response.Override(0.6f);

        // 리프트 감마 게인 — 어두운 부분 더 짙게, 밝은 부분 약간 노란
        var lgg = profile.Add<LiftGammaGain>();
        lgg.active = true;
        lgg.lift.Override(new Vector4(-0.05f, -0.05f, -0.02f, 0f));
        lgg.gamma.Override(new Vector4(0f, -0.02f, 0.02f, 0f));
        lgg.gain.Override(new Vector4(0.05f, 0.02f, -0.03f, 0f));
    }

    static void CreateDustParticles(GameObject player)
    {
        var dustGO = new GameObject("DustParticles");
        dustGO.transform.SetParent(player.transform);
        dustGO.transform.localPosition = new Vector3(0, 2f, 0);
        Undo.RegisterCreatedObjectUndo(dustGO, "Create Dust");

        var ps = dustGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = 80;
        main.startLifetime = 6f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new Color(0.7f, 0.65f, 0.55f, 0.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 3f, 8f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.7f, 0.65f, 0.55f), 0f),
                new GradientColorKey(new Color(0.7f, 0.65f, 0.55f), 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.3f, 0.3f),
                new GradientAlphaKey(0.3f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = dustGO.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.material.SetColor("_BaseColor", new Color(0.7f, 0.65f, 0.55f, 0.3f));
    }

    static GameObject CreateNeonSign(DayNightCycle dnCycle)
    {
        // 입구 위쪽에 네온사인 Quad
        var signGO = new GameObject("NeonSign");
        signGO.transform.position = new Vector3(6.5f, 2.8f, 0.6f);
        Undo.RegisterCreatedObjectUndo(signGO, "Create NeonSign");

        var signQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        signQuad.name = "SignFace";
        signQuad.transform.SetParent(signGO.transform);
        signQuad.transform.localPosition = Vector3.zero;
        signQuad.transform.localScale = new Vector3(2.5f, 0.6f, 1);
        Object.DestroyImmediate(signQuad.GetComponent<MeshCollider>());

        // 네온 머테리얼 (Emission)
        var neonMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        neonMat.name = "NeonMat";
        Color neonColor = new Color(1f, 0.15f, 0.25f);
        neonMat.SetColor("_BaseColor", neonColor);
        neonMat.SetColor("_EmissionColor", neonColor * 2f);
        neonMat.EnableKeyword("_EMISSION");
        neonMat.SetFloat("_Smoothness", 0.9f);
        signQuad.GetComponent<MeshRenderer>().sharedMaterial = neonMat;
        signQuad.GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        // 두번째 Quad — 글자 "OPEN" 표현
        var textQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        textQuad.name = "SignText";
        textQuad.transform.SetParent(signGO.transform);
        textQuad.transform.localPosition = new Vector3(0, 0, -0.01f);
        textQuad.transform.localScale = new Vector3(2f, 0.4f, 1);
        Object.DestroyImmediate(textQuad.GetComponent<MeshCollider>());
        var textMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        textMat.name = "NeonTextMat";
        textMat.mainTexture = CreateNeonTexture();
        textMat.SetColor("_EmissionColor", Color.white * 3f);
        textMat.EnableKeyword("_EMISSION");
        textMat.SetFloat("_Surface", 1);
        textMat.SetFloat("_AlphaClip", 1);
        textMat.SetFloat("_Cutoff", 0.5f);
        textMat.EnableKeyword("_ALPHATEST_ON");
        textMat.renderQueue = 2460;
        textQuad.GetComponent<MeshRenderer>().sharedMaterial = textMat;
        textQuad.GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        // 네온 PointLight (붉은 조명)
        var neonLightGO = new GameObject("NeonLight");
        neonLightGO.transform.SetParent(signGO.transform);
        neonLightGO.transform.localPosition = new Vector3(0, -0.5f, -1.5f);
        var neonLight = neonLightGO.AddComponent<Light>();
        neonLight.type = LightType.Point;
        neonLight.range = 8f;
        neonLight.intensity = 8f;
        neonLight.color = new Color(1f, 0.2f, 0.3f);
        neonLight.shadows = LightShadows.Soft;

        // NeonSign 컴포넌트
        var neonComp = signGO.AddComponent<NeonSign>();
        var neonSO = new SerializedObject(neonComp);
        neonSO.FindProperty("neonLight").objectReferenceValue = neonLight;
        neonSO.FindProperty("signRenderer").objectReferenceValue =
            signQuad.GetComponent<MeshRenderer>();
        neonSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        neonSO.FindProperty("neonColor").colorValue = new Color(1f, 0.15f, 0.25f);
        neonSO.FindProperty("baseIntensity").floatValue = 8f;
        neonSO.ApplyModifiedProperties();

        return signGO;
    }

    static Texture2D CreateNeonTexture()
    {
        int w = 128, h = 32;
        var tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color glow = new Color(1f, 1f, 1f, 1f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        // "OPEN" 글자를 점으로 찍기 (단순 픽셀 폰트)
        int[,] letters = {
            // O (x=10~22, y=8~24)
            {12,8},{13,8},{14,8},{15,8},{16,8},{17,8},{18,8},{19,8},{20,8},
            {11,9},{12,9},{20,9},{21,9},
            {10,10},{11,10},{21,10},{22,10},
            {10,12},{11,12},{21,12},{22,12},
            {10,14},{11,14},{21,14},{22,14},
            {10,16},{11,16},{21,16},{22,16},
            {10,18},{11,18},{21,18},{22,18},
            {10,20},{11,20},{21,20},{22,20},
            {11,21},{12,21},{20,21},{21,21},
            {12,22},{13,22},{14,22},{15,22},{16,22},{17,22},{18,22},{19,22},{20,22},
            // P (x=28~40)
            {28,8},{29,8},{30,8},{31,8},{32,8},{33,8},{34,8},{35,8},{36,8},
            {28,9},{29,9},{36,9},{37,9},
            {28,10},{29,10},{37,10},{38,10},
            {28,12},{29,12},{37,12},{38,12},
            {28,14},{29,14},{36,14},{37,14},
            {28,15},{29,15},{30,15},{31,15},{32,15},{33,15},{34,15},{35,15},{36,15},
            {28,18},{29,18},
            {28,20},{29,20},
            {28,22},{29,22},
            // E (x=44~56)
            {44,8},{45,8},{46,8},{47,8},{48,8},{49,8},{50,8},{51,8},{52,8},{53,8},{54,8},
            {44,9},{45,9},
            {44,10},{45,10},
            {44,12},{45,12},
            {44,14},{45,14},{46,14},{47,14},{48,14},{49,14},{50,14},
            {44,16},{45,16},
            {44,18},{45,18},
            {44,20},{45,20},
            {44,22},{45,22},{46,22},{47,22},{48,22},{49,22},{50,22},{51,22},{52,22},{53,22},{54,22},
            // N (x=60~74)
            {60,8},{61,8},{72,8},{73,8},
            {60,10},{61,10},{62,10},{72,10},{73,10},
            {60,12},{61,12},{63,12},{64,12},{72,12},{73,12},
            {60,14},{61,14},{65,14},{66,14},{72,14},{73,14},
            {60,16},{61,16},{67,16},{68,16},{72,16},{73,16},
            {60,18},{61,18},{69,18},{70,18},{72,18},{73,18},
            {60,20},{61,20},{71,20},{72,20},{73,20},
            {60,22},{61,22},{72,22},{73,22},
        };

        for (int i = 0; i < letters.GetLength(0); i++)
        {
            int px = letters[i, 0], py = letters[i, 1];
            if (px >= 0 && px < w && py >= 0 && py < h)
            {
                tex.SetPixel(px, py, glow);
                // 약간 두껍게
                if (px + 1 < w) tex.SetPixel(px + 1, py, glow);
                if (py + 1 < h) tex.SetPixel(px, py + 1, glow);
            }
        }

        tex.Apply();
        return tex;
    }

    static GameObject CreateRoof(GameObject player)
    {
        int bw = 12, bh = 10;
        float roofY = 2.7f;

        var roofParent = new GameObject("Roof");
        Undo.RegisterCreatedObjectUndo(roofParent, "Create Roof");

        // 반투명 가능한 머테리얼
        var roofMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        roofMat.name = "RoofMat";
        Color roofColor = new Color(0.15f, 0.13f, 0.12f, 1f);
        roofMat.SetColor("_BaseColor", roofColor);
        roofMat.SetFloat("_Surface", 1); // Transparent
        roofMat.SetFloat("_Blend", 0); // Alpha
        roofMat.SetOverrideTag("RenderType", "Transparent");
        roofMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        roofMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        roofMat.SetFloat("_ZWrite", 0);
        roofMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        roofMat.renderQueue = 3000;
        roofMat.SetFloat("_Smoothness", 0.2f);

        // 지붕 타일
        for (int x = 0; x <= bw; x++)
        {
            for (int z = 0; z <= bh; z++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Roof_{x}_{z}";
                tile.transform.SetParent(roofParent.transform);
                tile.transform.position = new Vector3(x + 0.5f, roofY, z + 0.5f);
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = new Vector3(TILE_SIZE + 0.02f, TILE_SIZE + 0.02f, 1);
                tile.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
                tile.GetComponent<MeshRenderer>().shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                tile.GetComponent<MeshRenderer>().receiveShadows = false;
                Object.DestroyImmediate(tile.GetComponent<MeshCollider>());
            }
        }

        // RoofController 컴포넌트
        var roofCtrl = roofParent.AddComponent<RoofController>();
        var roofSO = new SerializedObject(roofCtrl);
        roofSO.FindProperty("player").objectReferenceValue = player.transform;
        roofSO.FindProperty("roofObject").objectReferenceValue = roofParent;
        roofSO.FindProperty("buildingMin").vector3Value = new Vector3(0.5f, 0, 0.5f);
        roofSO.FindProperty("buildingMax").vector3Value = new Vector3(bw + 0.5f, 0, bh + 0.5f);
        roofSO.ApplyModifiedProperties();

        return roofParent;
    }

    static void CreateWallOutline(GameObject player, GameObject playerVisual)
    {
        // 아웃라인용 두번째 Quad (벽 뒤에서만 보임)
        var outlineGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        outlineGO.name = "OcclusionOutline";
        outlineGO.transform.SetParent(playerVisual.transform);
        outlineGO.transform.localPosition = Vector3.zero;
        outlineGO.transform.localScale = new Vector3(1.3f, 1.3f, 1);
        Object.DestroyImmediate(outlineGO.GetComponent<MeshCollider>());

        // 커스텀 셰이더로 ZTest Always 확실하게 적용
        var shader = Shader.Find("Custom/OcclusionOutline");
        Material outlineMat;
        if (shader != null)
        {
            outlineMat = new Material(shader);
            outlineMat.name = "OutlineMat";
            outlineMat.SetColor("_Color", new Color(0.3f, 0.8f, 1f, 0.4f));
        }
        else
        {
            // 폴백: URP Unlit
            Debug.LogWarning("[Flashlight] Custom/OcclusionOutline 셰이더를 찾을 수 없음. 셰이더 컴파일 후 다시 실행해주세요.");
            outlineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            outlineMat.name = "OutlineMat";
            outlineMat.SetColor("_BaseColor", new Color(0.3f, 0.8f, 1f, 0.4f));
        }
        outlineGO.GetComponent<MeshRenderer>().sharedMaterial = outlineMat;
        outlineGO.GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        // WallOcclusionOutline 컴포넌트
        var occComp = player.AddComponent<WallOcclusionOutline>();
        var occSO = new SerializedObject(occComp);
        occSO.FindProperty("player").objectReferenceValue = player.transform;
        occSO.FindProperty("outlineRenderer").objectReferenceValue =
            outlineGO.GetComponent<MeshRenderer>();
        occSO.FindProperty("outlineColor").colorValue = new Color(0.3f, 0.8f, 1f, 0.4f);
        occSO.ApplyModifiedProperties();
    }

    static void CreateBuilding()
    {
        var root = new GameObject("Building");
        Undo.RegisterCreatedObjectUndo(root, "Create Building");

        int bw = 12, bh = 10;
        float wallHeight = 2.5f;

        // 머테리얼 생성
        Material floorMat = CreateTextureMaterial(CreateConcreteFloorTex(), "FloorMat");
        Material roadMat = CreateTextureMaterial(CreateAsphaltTex(), "RoadMat");
        Material wallMat = CreateTextureMaterial(CreateBrickWallTex(), "WallMat");

        // ===== 바깥 바닥 (도로) =====
        var outsideParent = new GameObject("OutsideGround");
        outsideParent.transform.SetParent(root.transform);
        for (int x = -4; x <= bw + 4; x++)
        {
            for (int z = -4; z <= bh + 4; z++)
            {
                if (x >= 1 && x < bw && z >= 1 && z < bh) continue;
                CreateFloorTile(outsideParent, $"R_{x}_{z}", x, z, roadMat);
            }
        }

        // ===== 건물 내부 바닥 =====
        var floorParent = new GameObject("Floor");
        floorParent.transform.SetParent(root.transform);
        for (int x = 1; x < bw; x++)
            for (int z = 1; z < bh; z++)
                CreateFloorTile(floorParent, $"F_{x}_{z}", x, z, floorMat);

        // ===== 벽 =====
        var wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(root.transform);

        // 뒷벽 (Z=bh, X방향)
        for (int x = 0; x <= bw; x++)
            CreateWall(wallParent, x, bh, wallHeight, wallMat, WallDir.ZWall);
        // 앞벽 (Z=0, 출입구 5~7 비움)
        for (int x = 0; x <= bw; x++)
        {
            if (x >= 5 && x <= 7) continue;
            CreateWall(wallParent, x, 0, wallHeight, wallMat, WallDir.ZWall);
        }
        // 좌벽 (X=0, Z방향)
        for (int z = 0; z <= bh; z++)
            CreateWall(wallParent, 0, z, wallHeight, wallMat, WallDir.XWall);
        // 우벽 (X=bw, Z방향)
        for (int z = 0; z <= bh; z++)
            CreateWall(wallParent, bw, z, wallHeight, wallMat, WallDir.XWall);
    }

    enum WallDir { XWall, ZWall }

    static void CreateFloorTile(GameObject parent, string name, int x, int z, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent.transform);
        float tileOverlap = TILE_SIZE + 0.02f;
        go.transform.position = new Vector3(x + 0.5f, 0, z + 0.5f);
        go.transform.rotation = Quaternion.Euler(90, 0, 0);
        go.transform.localScale = new Vector3(tileOverlap, tileOverlap, 1);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.GetComponent<MeshRenderer>().receiveShadows = true;
        Object.DestroyImmediate(go.GetComponent<MeshCollider>());
    }

    static void CreateWall(GameObject parent, int x, int z, float height, Material mat, WallDir dir)
    {
        // 벽 비주얼 (Quad, 세워놓음)
        var wallGO = new GameObject($"W_{x}_{z}");
        wallGO.transform.SetParent(parent.transform);

        // Quad 생성
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Visual";
        quad.transform.SetParent(wallGO.transform);
        float wallOverlap = TILE_SIZE + 0.05f;
        quad.transform.localScale = new Vector3(wallOverlap, height, 1);
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
        quad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        quad.GetComponent<MeshRenderer>().receiveShadows = true;
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());

        // Shadow caster (얇은 Box, 빛 차단용)
        var shadowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadowBox.name = "ShadowCaster";
        shadowBox.transform.SetParent(wallGO.transform);
        shadowBox.transform.localScale = new Vector3(
            dir == WallDir.ZWall ? wallOverlap : 0.15f,
            height,
            dir == WallDir.XWall ? wallOverlap : 0.15f
        );
        var shadowRenderer = shadowBox.GetComponent<MeshRenderer>();
        shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        shadowRenderer.receiveShadows = false;

        // 콜라이더 (이동 차단)
        var boxCol = shadowBox.GetComponent<BoxCollider>();
        // BoxCollider는 Cube에 기본 포함됨

        if (dir == WallDir.ZWall)
        {
            wallGO.transform.position = new Vector3(x + 0.5f, height * 0.5f, z + 0.5f);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;
            shadowBox.transform.localPosition = Vector3.zero;
        }
        else
        {
            wallGO.transform.position = new Vector3(x + 0.5f, height * 0.5f, z + 0.5f);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.Euler(0, 90, 0);
            shadowBox.transform.localPosition = Vector3.zero;
        }
    }

    // ================================================================
    //  빌보드 Quad (캐릭터용)
    // ================================================================
    static GameObject CreateBillboardQuad(string name, Texture2D tex, float w, float h)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.localScale = new Vector3(w, h, 1);
        Object.DestroyImmediate(go.GetComponent<MeshCollider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.mainTexture = tex;
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_AlphaClip", 1);
        mat.SetFloat("_Cutoff", 0.5f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = 2450;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return go;
    }

    // ================================================================
    //  텍스처 & 머테리얼 생성
    // ================================================================

    static Material CreateTextureMaterial(Texture2D tex, string name)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;
        mat.mainTexture = tex;
        mat.SetFloat("_Smoothness", 0.1f);
        mat.SetFloat("_Cull", 0); // 양면 렌더링 (Off)
        mat.SetColor("_EmissionColor", Color.black);
        mat.SetFloat("_EnvironmentReflections", 0f);
        mat.SetFloat("_SpecularHighlights", 0f);
        mat.DisableKeyword("_EMISSION");
        mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        return mat;
    }

    static Texture2D CreateAsphaltTex()
    {
        int s = 64;
        var tex = new Texture2D(s, s);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.06f;
                float v = 0.12f + n;
                tex.SetPixel(x, y, new Color(v, v, v + 0.005f));
            }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateConcreteFloorTex()
    {
        int s = 64;
        var tex = new Texture2D(s, s);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.2f + 50, y * 0.2f + 50) * 0.06f;
                float v = 0.22f + n;
                bool grid = (x % 32 == 0 || y % 32 == 0);
                Color c = grid ? new Color(v - 0.04f, v - 0.04f, v - 0.03f)
                               : new Color(v, v - 0.01f, v - 0.02f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateBrickWallTex()
    {
        int w = 64, h = 64;
        var tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color brickBase = new Color(0.32f, 0.22f, 0.18f);
        Color mortarColor = new Color(0.18f, 0.16f, 0.14f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int brickRow = y / 8;
                int offset = (brickRow % 2 == 0) ? 0 : 8;
                int brickCol = (x + offset) % 16;
                bool mortar = (y % 8 == 0) || (brickCol == 0);

                float n = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.06f;
                Color c = mortar ? mortarColor
                    : new Color(brickBase.r + n, brickBase.g + n * 0.7f, brickBase.b + n * 0.4f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateCharacterTexture()
    {
        int w = 32, h = 48;
        var tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        Color body = new Color(0.2f, 0.35f, 0.2f);
        Color head = new Color(0.7f, 0.6f, 0.5f);
        Color helmet = new Color(0.25f, 0.3f, 0.2f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        // 몸통
        for (int y = 0; y < 28; y++)
            for (int x = 8; x < 24; x++)
                tex.SetPixel(x, y, body);
        // 머리
        for (int y = 28; y < 42; y++)
            for (int x = 10; x < 22; x++)
                tex.SetPixel(x, y, head);
        // 헬멧
        for (int y = 40; y < 48; y++)
            for (int x = 8; x < 24; x++)
                tex.SetPixel(x, y, helmet);

        tex.Apply();
        return tex;
    }
}
