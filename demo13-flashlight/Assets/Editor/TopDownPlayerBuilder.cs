using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

/// <summary>
/// Resources/PlayerRig.prefab 자동 생성기.
/// 카메라 + 플레이어 + 라이트 + 후처리(Volume)를 한 세트로 묶은 PlayerRig.
/// Bootstrap이 1개만 스폰 → DontDestroyOnLoad로 모든 씬 공유(씬마다 카메라/플레이어 따로 안 둠).
/// 메뉴: Tools > TopDown > Build > Player Rig
/// </summary>
public static class TopDownPlayerBuilder
{
    const string PREFAB_PATH  = "Assets/Resources/PlayerRig.prefab";
    const string PROFILE_PATH = "Assets/Resources/PlayerRigVolume.asset";
    const string OLD_PREFAB    = "Assets/Resources/TopDownPlayer.prefab";
    const string PLAYER_SPRITE_GUID = "f8bd92d6d061f7143986c16a0ea86602";

    [MenuItem("Tools/TopDown/Build/Player Rig")]
    public static void BuildPlayerPrefab()
    {
        EnsureFolder("Assets/Resources");

        // ════ 루트: PlayerRig (DontDestroyOnLoad 컨테이너) ════
        var rig = new GameObject("PlayerRig");

        // ════ Player (TopDownPlayer 싱글톤 + 게임로직) ════
        var root = new GameObject("Player");
        root.transform.SetParent(rig.transform, false);
        root.tag = "Player";
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) root.layer = playerLayer;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var body = root.AddComponent<CircleCollider2D>();
        body.radius = 0.3f;

        // 비주얼 스프라이트
        var spriteGo = new GameObject("PlayerSprite");
        spriteGo.transform.SetParent(root.transform, false);
        if (playerLayer >= 0) spriteGo.layer = playerLayer;
        var sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;
        var spritePath = AssetDatabase.GUIDToAssetPath(PLAYER_SPRITE_GUID);
        if (!string.IsNullOrEmpty(spritePath))
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        // 허트박스 (trigger)
        var hurtGo = new GameObject("Hurtbox");
        hurtGo.transform.SetParent(root.transform, false);
        if (playerLayer >= 0) hurtGo.layer = playerLayer;
        var hbCol = hurtGo.AddComponent<BoxCollider2D>();
        hbCol.isTrigger = true;
        hbCol.size = new Vector2(0.6f, 0.9f);
        hurtGo.AddComponent<Hurtbox>();

        var warm = new Color(1f, 0.95f, 0.82f);

        // 주변 원형광 (중심 고정)
        var ambGo = new GameObject("PlayerAmbientLight");
        ambGo.transform.SetParent(root.transform, false);
        var amb = ambGo.AddComponent<Light2D>();
        amb.lightType = Light2D.LightType.Point;
        amb.intensity = 0.7f;
        amb.color = warm;
        amb.pointLightInnerRadius = 0.2f;
        amb.pointLightOuterRadius = 2.3f;
        amb.pointLightInnerAngle = 360f;
        amb.pointLightOuterAngle = 360f;
        SceneLightingBuilder.ApplyAllSortingLayers(amb);

        // 앞 부채꼴광 (마우스 방향 회전 — lightPivot)
        var lightGo = new GameObject("PlayerConeLight");
        lightGo.transform.SetParent(root.transform, false);
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.intensity = 1.6f;                      // HDR 범위 → bloom 잘 걸림
        light.color = warm;
        light.pointLightInnerRadius = 0.4f;
        light.pointLightOuterRadius = 6.5f;
        light.pointLightInnerAngle = 35f;
        light.pointLightOuterAngle = 80f;
        light.shadowsEnabled = true;        // ShadowCaster2D 그림자 드리움(진짜 캐스트)
        light.shadowIntensity = 0.75f;
        light.shadowSoftness = 0.3f;
        SceneLightingBuilder.ApplyAllSortingLayers(light);

        // 게임 로직 컴포넌트
        var player = root.AddComponent<TopDownPlayer>();
        root.AddComponent<Health>();
        root.AddComponent<PlayerInventory>();
        root.AddComponent<PlayerMedicalSystem>();
        root.AddComponent<InteractionSystem>();
        root.AddComponent<CombatFeedback>();
        root.AddComponent<MedicalHUD>();
        root.AddComponent<PlayerEquipment>();

        var pSo = new SerializedObject(player);
        SetRef(pSo, "spriteRenderer", sr);
        SetRef(pSo, "lightPivot", lightGo.transform);
        var enemyMaskProp = pSo.FindProperty("enemyMask");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyMaskProp != null && enemyLayer >= 0) enemyMaskProp.intValue = 1 << enemyLayer;
        pSo.ApplyModifiedPropertiesWithoutUndo();

        // ════ Main Camera (한 세트) ════
        var camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(rig.transform, false);
        camGo.tag = "MainCamera";
        camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.allowHDR = true;                         // bloom 위해
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraSortSetup>();       // 2D Y정렬축(CustomAxis) — PlayerRig 카메라가 유일 카메라이므로 필수

        var camData = camGo.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        var follow = camGo.AddComponent<CameraFollow>();
        var fSo = new SerializedObject(follow);
        SetRef(fSo, "target", root.transform);
        fSo.ApplyModifiedPropertiesWithoutUndo();

        // ════ 후처리 Volume (다크우드 룩 — bloom/vignette/color) ════
        var profile = CreateDarkwoodProfile();
        var volGo = new GameObject("PlayerRig Volume");
        volGo.transform.SetParent(rig.transform, false);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.profile = profile;

        // ════ 프리팹 저장 ════
        var prefab = PrefabUtility.SaveAsPrefabAsset(rig, PREFAB_PATH, out bool ok);
        Object.DestroyImmediate(rig);

        // 구 단일 프리팹 정리
        if (AssetDatabase.LoadAssetAtPath<GameObject>(OLD_PREFAB) != null)
            AssetDatabase.DeleteAsset(OLD_PREFAB);

        if (ok)
        {
            AssetDatabase.SaveAssets();

            // ── 현재 씬에 인스턴스 배치 (씬뷰에서 바로 보이게) ──
            // 이미 PlayerRig/플레이어가 있으면 스킵(중복 방지). Play 시 Bootstrap도 중복 스킵.
            bool placed = false;
            if (!Application.isBatchMode &&
                Object.FindFirstObjectByType<TopDownPlayer>(FindObjectsInactive.Include) == null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = "PlayerRig";
                Undo.RegisterCreatedObjectUndo(inst, "Place PlayerRig");
                Selection.activeObject = inst;
                EditorGUIUtility.PingObject(inst);
                EditorSceneManager.MarkSceneDirty(inst.scene);
                placed = true;
            }
            else
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log($"<color=cyan>[PlayerRig]</color> 생성 완료: {PREFAB_PATH}" +
                      (placed ? " + 현재 씬에 배치됨" : "") + "\n" +
                      "PlayerRig = Player(+Sprite/Hurtbox/Lights) + Main Camera(+CameraFollow/Volume).\n" +
                      "Bootstrap이 1개 스폰 → DontDestroyOnLoad로 모든 씬 공유. 씬의 다른 카메라는 자동 비활성.");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("PlayerRig",
                    "Resources/PlayerRig.prefab 생성 완료" + (placed ? " + 현재 씬에 배치" : "") + ".\n\n" +
                    "카메라+플레이어+라이트+후처리(Volume)가 한 세트입니다.\n" +
                    (placed ? "씬뷰에서 바로 보입니다(라이트). Bloom 등 후처리는 게임뷰에서 보여요.\n\n"
                            : "씬에 이미 플레이어가 있어 배치는 스킵했습니다.\n\n") +
                    "빛 느낌은 PlayerRig Volume(Bloom/Vignette/Color) + Light 값으로 조정하세요.", "확인");
        }
        else Debug.LogError("[PlayerRig] 프리팹 저장 실패");
    }

    /// <summary>다크우드풍 후처리 프로파일 생성 (bloom 번짐 + 비네트 + 따뜻한 컬러).</summary>
    static VolumeProfile CreateDarkwoodProfile()
    {
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH) != null)
            AssetDatabase.DeleteAsset(PROFILE_PATH);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, PROFILE_PATH);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.5f);              // 낮게 → Light2D 빛이 번짐
        bloom.intensity.Override(1.3f);
        bloom.scatter.Override(0.75f);
        bloom.tint.Override(new Color(1f, 0.9f, 0.78f));
        AssetDatabase.AddObjectToAsset(bloom, profile);

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.42f);
        vig.smoothness.Override(0.5f);
        vig.color.Override(new Color(0.01f, 0.01f, 0.03f));
        AssetDatabase.AddObjectToAsset(vig, profile);

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.1f);
        color.contrast.Override(10f);
        color.colorFilter.Override(new Color(1f, 0.92f, 0.8f));  // 따뜻
        color.saturation.Override(-6f);
        AssetDatabase.AddObjectToAsset(color, profile);

        var ca = profile.Add<ChromaticAberration>(true);
        ca.intensity.Override(0f);                   // 피격 시 PlayerHitReaction이 펄스
        AssetDatabase.AddObjectToAsset(ca, profile);

        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.22f);
        AssetDatabase.AddObjectToAsset(grain, profile);

        AssetDatabase.SaveAssets();
        return profile;
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = value;
        else Debug.LogWarning($"[PlayerRig] 직렬화 필드 못찾음: {field}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace("\\", "/");
        var folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
