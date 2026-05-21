using UnityEngine;
using UnityEditor;

/// <summary>
/// EnemyData 기반 적 프리팹 자동 생성.
/// 해골 프리팹을 베이스로 색상/글로우/파티클/크기를 적용하여 저장.
/// </summary>
public static class EnemyPrefabFactory
{
    const string BASE_PREFAB = "Assets/PixelArtStudio/SkeletonsPack/CommonSoldier/Prefabs/CommonSoldier.prefab";
    const string OUTPUT_FOLDER = "Assets/Prefabs/Enemies";

    /// <summary>EnemyData 설정으로 프리팹 생성 및 저장</summary>
    public static GameObject GeneratePrefab(EnemyData enemyData)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_PREFAB);
        if (basePrefab == null)
        {
            Debug.LogError($"[PrefabFactory] 베이스 프리팹 없음: {BASE_PREFAB}");
            return null;
        }

        EnsureFolder();

        // 임시 인스턴스 생성
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = GetSafeName(enemyData.displayName);

        // ===== 비주얼 적용 =====
        ApplyTint(instance, enemyData);
        ApplyShadow(instance, enemyData);

        if (enemyData.useGlow)
            AddGlow(instance, enemyData);

        if (enemyData.useTrailParticle)
            AddTrailParticle(instance, enemyData);

        // 스케일
        instance.transform.localScale = Vector3.one * enemyData.scale;

        // SkeletonAnimController 확인
        if (instance.GetComponent<SkeletonAnimController>() == null)
            instance.AddComponent<SkeletonAnimController>();

        // ===== 프리팹 저장 =====
        string path = $"{OUTPUT_FOLDER}/{instance.name}.prefab";

        // 기존 프리팹이 있으면 덮어쓰기
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);

        // EnemyData에 참조 연결
        enemyData.generatedPrefab = savedPrefab;
        EditorUtility.SetDirty(enemyData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[PrefabFactory] '{enemyData.displayName}' 프리팹 생성: {path}");
        return savedPrefab;
    }

    /// <summary>씬에 프리팹을 미리보기로 생성 (임시)</summary>
    public static GameObject PreviewInScene(EnemyData enemyData)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_PREFAB);
        if (basePrefab == null) return null;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = $"[Preview] {enemyData.displayName}";
        instance.transform.position = new Vector3(6, 0.1f, 4); // 건물 중앙

        ApplyTint(instance, enemyData);
        ApplyShadow(instance, enemyData);

        if (enemyData.useGlow)
            AddGlow(instance, enemyData);
        if (enemyData.useTrailParticle)
            AddTrailParticle(instance, enemyData);

        instance.transform.localScale = Vector3.one * enemyData.scale;

        Undo.RegisterCreatedObjectUndo(instance, "Preview Enemy");

        // SceneView 포커스
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(instance.transform.position);

        return instance;
    }

    /// <summary>기존 프리팹을 EnemyData로 업데이트</summary>
    public static void UpdateExistingPrefab(EnemyData enemyData)
    {
        if (enemyData.generatedPrefab == null)
        {
            GeneratePrefab(enemyData);
            return;
        }

        string path = AssetDatabase.GetAssetPath(enemyData.generatedPrefab);
        if (string.IsNullOrEmpty(path))
        {
            GeneratePrefab(enemyData);
            return;
        }

        // 프리팹 인스턴스로 열어서 수정
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(enemyData.generatedPrefab);

        // 기존 추가 오브젝트 제거 (Glow, Trail 등)
        CleanupAddons(instance);

        // 다시 적용
        ApplyTint(instance, enemyData);
        ApplyShadow(instance, enemyData);

        if (enemyData.useGlow)
            AddGlow(instance, enemyData);
        if (enemyData.useTrailParticle)
            AddTrailParticle(instance, enemyData);

        instance.transform.localScale = Vector3.one * enemyData.scale;

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);

        Debug.Log($"[PrefabFactory] '{enemyData.displayName}' 프리팹 업데이트!");
    }

    // ================================================================
    //  비주얼 적용
    // ================================================================

    static void ApplyTint(GameObject root, EnemyData data)
    {
        var renderers = root.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "shadow") continue;
            r.color = data.tintColor;
        }
    }

    static void ApplyShadow(GameObject root, EnemyData data)
    {
        var renderers = root.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "shadow")
                r.color = data.shadowColor;
        }
    }

    static void AddGlow(GameObject root, EnemyData data)
    {
        // 기존 글로우 제거
        var existing = root.transform.Find("EnemyGlow");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var glowGO = new GameObject("EnemyGlow");
        glowGO.transform.SetParent(root.transform);
        glowGO.transform.localPosition = new Vector3(0, 0.3f, 0);

        var light = glowGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = data.glowColor;
        light.intensity = data.glowIntensity;
        light.range = data.glowRange;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
    }

    static void AddTrailParticle(GameObject root, EnemyData data)
    {
        var existing = root.transform.Find("EnemyTrail");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var trailGO = new GameObject("EnemyTrail");
        trailGO.transform.SetParent(root.transform);
        trailGO.transform.localPosition = new Vector3(0, 0.2f, 0);

        var ps = trailGO.AddComponent<ParticleSystem>();

        // Main
        var main = ps.main;
        main.maxParticles = 30;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = data.trailColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 8f;

        // Shape
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Color over lifetime
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(new Color(data.trailColor.r, data.trailColor.g, data.trailColor.b), 0f),
                new GradientColorKey(new Color(data.trailColor.r, data.trailColor.g, data.trailColor.b), 1f)
            },
            new[] {
                new GradientAlphaKey(data.trailColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = gradient;

        // Size over lifetime
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

        // Material
        var renderer = trailGO.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetColor("_BaseColor", data.trailColor);
        // Additive blending
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 1);   // Additive
        renderer.sharedMaterial = mat;
    }

    static void CleanupAddons(GameObject root)
    {
        // 추가된 Glow, Trail 제거
        var glow = root.transform.Find("EnemyGlow");
        if (glow != null) Object.DestroyImmediate(glow.gameObject);

        var trail = root.transform.Find("EnemyTrail");
        if (trail != null) Object.DestroyImmediate(trail.gameObject);
    }

    // ================================================================
    //  유틸
    // ================================================================

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
    }

    static string GetSafeName(string name)
    {
        // 파일명으로 안전한 문자열 변환
        string safe = name.Replace(" ", "_");
        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            safe = safe.Replace(c, '_');
        return safe;
    }
}
