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

        // ── 자식: 손전등 피벗 (Light2D) ────────────────────────
        var pivotGo = new GameObject("FlashlightPivot");
        pivotGo.transform.SetParent(root.transform, false);
        var light = pivotGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.intensity = 1f;
        light.color = new Color(1f, 0.96f, 0.85f);
        light.pointLightInnerRadius = 1f;
        light.pointLightOuterRadius = 6f;

        // ── 게임 로직 컴포넌트 ─────────────────────────────────
        var player = root.AddComponent<TopDownPlayer>();
        root.AddComponent<Health>();
        root.AddComponent<PlayerInventory>();
        root.AddComponent<PlayerMedicalSystem>();
        root.AddComponent<InteractionSystem>();
        root.AddComponent<CombatFeedback>();
        root.AddComponent<MedicalHUD>();
        var flashlight = root.AddComponent<FlashlightController>();

        // ── 직렬화 필드 와이어링 ───────────────────────────────
        var pSo = new SerializedObject(player);
        SetRef(pSo, "spriteRenderer", sr);
        SetRef(pSo, "flashlightPivot", pivotGo.transform);
        var enemyMaskProp = pSo.FindProperty("enemyMask");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyMaskProp != null && enemyLayer >= 0)
            enemyMaskProp.intValue = 1 << enemyLayer;
        pSo.ApplyModifiedPropertiesWithoutUndo();

        var fSo = new SerializedObject(flashlight);
        SetRef(fSo, "spotLight", light);
        fSo.ApplyModifiedPropertiesWithoutUndo();

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
                      "PlayerMedicalSystem, InteractionSystem, CombatFeedback, MedicalHUD, FlashlightController\n" +
                      "자식: PlayerSprite, Hurtbox(trigger), FlashlightPivot(Light2D)");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("TopDownPlayer",
                    "Resources/TopDownPlayer.prefab 생성 완료.\n\n" +
                    "이제 Play 하면 자동 스폰됩니다.\n" +
                    "스프라이트/콜라이더 크기는 인스펙터에서 조정하세요.", "확인");
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
