using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.IO;

/// <summary>
/// Resources/TopDownPlayer.prefab 자동 생성기.
/// 탑다운 2D 전환 후 플레이어 프리팹이 사라져 Bootstrap이 스폰 못하는 문제 해결.
/// 코드로 컴포넌트를 조립 → Unity가 GUID/직렬화를 정확히 처리.
/// 메뉴: Tools > TopDown 2D > Build Player Prefab
/// </summary>
public static class TopDownPlayerBuilder
{
    const string PREFAB_PATH = "Assets/Resources/TopDownPlayer.prefab";
    // 기존 Player.prefab의 PlayerSprite가 쓰던 스프라이트 GUID (재사용)
    const string PLAYER_SPRITE_GUID = "f8bd92d6d061f7143986c16a0ea86602";

    [MenuItem("Tools/TopDown 2D/Build Player Prefab")]
    public static void BuildPlayerPrefab()
    {
        EnsureFolder("Assets/Resources");

        // ── 루트 ──────────────────────────────────────────────
        var root = new GameObject("TopDownPlayer");
        root.tag = "Player"; // EnemyController가 FindGameObjectWithTag("Player")로 탐지 (Unity 내장 태그)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) root.layer = playerLayer;

        // 물리
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var body = root.AddComponent<CircleCollider2D>();
        body.radius = 0.3f;
        body.isTrigger = false;

        // ── 자식: 비주얼 스프라이트 ────────────────────────────
        var spriteGo = new GameObject("PlayerSprite");
        spriteGo.transform.SetParent(root.transform, false);
        if (playerLayer >= 0) spriteGo.layer = playerLayer;
        var sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;
        var spritePath = AssetDatabase.GUIDToAssetPath(PLAYER_SPRITE_GUID);
        if (!string.IsNullOrEmpty(spritePath))
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        // ── 자식: 허트박스 (trigger) ───────────────────────────
        var hurtGo = new GameObject("Hurtbox");
        hurtGo.transform.SetParent(root.transform, false);
        if (playerLayer >= 0) hurtGo.layer = playerLayer;
        var hbCol = hurtGo.AddComponent<BoxCollider2D>();
        hbCol.isTrigger = true;
        hbCol.size = new Vector2(0.6f, 0.9f);
        hurtGo.AddComponent<Hurtbox>();

        // ── 자식: 플레이어 라이트 2종 (다크우드식) ───────────────
        // 손전등(FlashlightController) 폐기. 주변 원형(약) + 앞 부채꼴(강) 두 레이어.
        var warm = new Color(1f, 0.95f, 0.82f);

        // (1) 주변 원형광 — 플레이어 바로 주변을 은은하게. 항상 중심 고정(회전 X).
        var ambGo = new GameObject("PlayerAmbientLight");
        ambGo.transform.SetParent(root.transform, false);
        var amb = ambGo.AddComponent<Light2D>();
        amb.lightType = Light2D.LightType.Point;
        amb.intensity = 0.6f;
        amb.color = warm;
        amb.pointLightInnerRadius = 0.2f;
        amb.pointLightOuterRadius = 2.3f;            // 작은 원형
        amb.pointLightInnerAngle = 360f;
        amb.pointLightOuterAngle = 360f;
        SceneLightingBuilder.ApplyAllSortingLayers(amb);

        // (2) 앞 부채꼴광 — 바라보는 방향으로 멀리·밝게. 런타임에 회전+앞 오프셋(lightPivot).
        var lightGo = new GameObject("PlayerConeLight");
        lightGo.transform.SetParent(root.transform, false);
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.intensity = 1.3f;
        light.color = warm;
        light.pointLightInnerRadius = 0.4f;
        light.pointLightOuterRadius = 6.5f;          // 앞으로 길게
        light.pointLightInnerAngle = 35f;            // 부채꼴 — 안쪽(풀밝기) 각
        light.pointLightOuterAngle = 80f;            // 부채꼴 — 바깥(페이드) 각
        SceneLightingBuilder.ApplyAllSortingLayers(light);

        // ── 게임 로직 컴포넌트 ─────────────────────────────────
        var player = root.AddComponent<TopDownPlayer>();
        root.AddComponent<Health>();
        root.AddComponent<PlayerInventory>();
        root.AddComponent<PlayerMedicalSystem>();
        root.AddComponent<InteractionSystem>();
        root.AddComponent<CombatFeedback>();
        root.AddComponent<MedicalHUD>();
        root.AddComponent<PlayerEquipment>();   // 무기 장착

        // ── 직렬화 필드 와이어링 ───────────────────────────────
        var pSo = new SerializedObject(player);
        SetRef(pSo, "spriteRenderer", sr);
        SetRef(pSo, "lightPivot", lightGo.transform);  // 부채꼴 라이트를 마우스 방향으로 회전
        var enemyMaskProp = pSo.FindProperty("enemyMask");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyMaskProp != null && enemyLayer >= 0)
            enemyMaskProp.intValue = 1 << enemyLayer;
        pSo.ApplyModifiedPropertiesWithoutUndo();

        // ── 프리팹 저장 ───────────────────────────────────────
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out bool ok);
        Object.DestroyImmediate(root);

        if (ok)
        {
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"<color=cyan>[TopDownPlayer]</color> 프리팹 생성 완료: {PREFAB_PATH}\n" +
                      "컴포넌트: Rigidbody2D, CircleCollider2D, TopDownPlayer, Health, PlayerInventory, " +
                      "PlayerMedicalSystem, InteractionSystem, CombatFeedback, MedicalHUD, PlayerEquipment\n" +
                      "자식: PlayerSprite, Hurtbox(trigger), PlayerAmbientLight(원형), PlayerConeLight(부채꼴)");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("TopDownPlayer",
                    "Resources/TopDownPlayer.prefab 생성 완료.\n\n" +
                    "• PlayerAmbientLight: 주변 원형 은은한 빛 (중심 고정)\n" +
                    "• PlayerConeLight: 앞 부채꼴 빛 (마우스 방향)\n\n" +
                    "어두운 분위기는 'Tools > TopDown 2D > Setup Scene Lighting'으로 글로벌 어둠을 추가하세요.", "확인");
        }
        else
        {
            Debug.LogError("[TopDownPlayer] 프리팹 저장 실패");
        }
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = value;
        else Debug.LogWarning($"[TopDownPlayer] 직렬화 필드 못찾음: {field}");
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
