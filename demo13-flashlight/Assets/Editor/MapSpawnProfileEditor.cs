using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// MapSpawnProfile 커스텀 인스펙터.
/// 희귀도/카테고리 분포를 시각적으로 표시 + 시뮬레이션 미리보기.
/// </summary>
[CustomEditor(typeof(MapSpawnProfile))]
public class MapSpawnProfileEditor : Editor
{
    bool showSimulation;
    int simBudget = 30;
    bool simNight;
    Dictionary<ItemRarity, int> simRarity;
    Dictionary<ItemCategory, int> simCategory;

    static readonly Color[] rarityColors = {
        Color.white,
        new Color(0.3f, 0.9f, 0.3f),
        new Color(0.3f, 0.5f, 1f),
        new Color(0.7f, 0.3f, 1f),
        new Color(1f, 0.85f, 0.2f),
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var profile = (MapSpawnProfile)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 희귀도 분포 바
        EditorGUILayout.LabelField("희귀도 분포 미리보기", EditorStyles.boldLabel);
        DrawDistributionBar(new float[] {
            profile.rarityCommon, profile.rarityUncommon,
            profile.rarityRare, profile.rarityEpic, profile.rarityLegendary
        }, new string[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" }, rarityColors);

        EditorGUILayout.Space(5);

        // 카테고리 분포 바
        EditorGUILayout.LabelField("카테고리 분포 미리보기", EditorStyles.boldLabel);
        DrawDistributionBar(new float[] {
            profile.catWeapon, profile.catMedical, profile.catConsumable,
            profile.catMaterial, profile.catValuable, profile.catKey, profile.catMisc
        }, new string[] { "무기", "의료", "소비", "재료", "귀중품", "열쇠", "기타" },
        new Color[] {
            new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.3f), new Color(1f, 0.8f, 0.2f),
            new Color(0.6f, 0.4f, 0.2f), new Color(1f, 0.6f, 0f), new Color(0.5f, 0.5f, 1f),
            new Color(0.6f, 0.6f, 0.6f)
        });

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 시뮬레이션
        showSimulation = EditorGUILayout.Foldout(showSimulation, "스폰 시뮬레이션");
        if (showSimulation)
        {
            EditorGUI.indentLevel++;
            simBudget = EditorGUILayout.IntSlider("예산", simBudget, 5, 100);
            simNight = EditorGUILayout.Toggle("밤", simNight);

            if (GUILayout.Button("시뮬레이션 실행"))
                RunSimulation(profile);

            if (simRarity != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("결과:", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("희귀도:");
                foreach (var kv in simRarity)
                {
                    float pct = (float)kv.Value / simBudget * 100f;
                    EditorGUILayout.LabelField($"  {kv.Key}: {kv.Value}개 ({pct:F1}%)");
                }

                EditorGUILayout.LabelField("카테고리:");
                foreach (var kv in simCategory)
                {
                    float pct = (float)kv.Value / simBudget * 100f;
                    EditorGUILayout.LabelField($"  {kv.Key}: {kv.Value}개 ({pct:F1}%)");
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    void DrawDistributionBar(float[] values, string[] labels, Color[] colors)
    {
        float total = 0f;
        for (int i = 0; i < values.Length; i++) total += values[i];
        if (total <= 0) return;

        var rect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        float x = rect.x;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] <= 0) continue;

            float pct = values[i] / total;
            float w = rect.width * pct;

            EditorGUI.DrawRect(new Rect(x, rect.y, w, rect.height), colors[i]);

            if (w > 30)
            {
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.black }
                };
                GUI.Label(new Rect(x, rect.y, w, rect.height),
                    $"{labels[i]}\n{pct * 100:F0}%", style);
            }

            x += w;
        }

        EditorGUILayout.Space(2);
    }

    void RunSimulation(MapSpawnProfile profile)
    {
        simRarity = new Dictionary<ItemRarity, int>();
        simCategory = new Dictionary<ItemCategory, int>();

        float[] rWeights = profile.GetRarityWeights(simNight);
        float[] cWeights = profile.GetCategoryWeights();

        for (int i = 0; i < simBudget; i++)
        {
            var rarity = (ItemRarity)WeightedRandom(rWeights);
            var category = (ItemCategory)WeightedRandom(cWeights);

            if (!simRarity.ContainsKey(rarity)) simRarity[rarity] = 0;
            simRarity[rarity]++;

            if (!simCategory.ContainsKey(category)) simCategory[category] = 0;
            simCategory[category]++;
        }
    }

    static int WeightedRandom(float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];
        if (total <= 0f) return 0;

        float roll = Random.Range(0f, total);
        float cum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cum += weights[i];
            if (roll <= cum) return i;
        }
        return weights.Length - 1;
    }
}

/// <summary>
/// 맵 스폰 프로파일 프리셋 생성기.
/// 메뉴: Tools > Dev Tools > Data > Generate Map Spawn Profiles
/// </summary>
public class MapSpawnProfileGenerator
{
    [MenuItem("Tools/Dev Tools/Data/Generate Map Spawn Profiles")]
    static void Generate()
    {
        string folder = "Assets/Resources/Data/MapSpawn";
        EnsureFolder(folder);

        // 7개 지역별 프로파일 생성
        CreateProfile(folder, "scrap_market", "고철 시장", 1f, 1f,
            groundMin: 12, groundLen: 8, containerMin: 18, containerLen: 12,
            miscWeight: 50, medWeight: 12, matWeight: 15);

        CreateProfile(folder, "silence_living", "침묵 거주지", 1f, 0.8f,
            groundMin: 10, groundLen: 8, containerMin: 15, containerLen: 10,
            miscWeight: 45, medWeight: 18, consWeight: 18);

        CreateProfile(folder, "industrial", "산업 구역", 1.1f, 1.2f,
            groundMin: 15, groundLen: 10, containerMin: 22, containerLen: 13,
            matWeight: 25, weapWeight: 10, miscWeight: 35);

        CreateProfile(folder, "entertainment", "환락 구역", 0.9f, 1.4f,
            groundMin: 10, groundLen: 6, containerMin: 16, containerLen: 10,
            valWeight: 18, miscWeight: 35, consWeight: 18);

        CreateProfile(folder, "railway_scrap", "폐선 고철장", 1.2f, 1.1f,
            groundMin: 18, groundLen: 12, containerMin: 25, containerLen: 15,
            matWeight: 22, miscWeight: 45, weapWeight: 8);

        CreateProfile(folder, "sanctuary_memorial", "성역 기념관", 0.8f, 1.6f,
            groundMin: 8, groundLen: 6, containerMin: 12, containerLen: 8,
            valWeight: 22, medWeight: 15, miscWeight: 30);

        CreateProfile(folder, "eternal_night_core", "영원한 밤 중심부", 0.7f, 2.5f,
            groundMin: 6, groundLen: 4, containerMin: 10, containerLen: 6,
            valWeight: 25, weapWeight: 12, miscWeight: 20, matWeight: 18);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>[MapSpawnProfile]</color> 7개 지역 프로파일 생성/갱신 완료: {folder}");
    }

    static void CreateProfile(string folder, string id, string displayName,
        float spawnMult, float qualityMult,
        int groundMin = 12, int groundLen = 8,
        int containerMin = 18, int containerLen = 12,
        float weapWeight = 5f, float medWeight = 15f, float consWeight = 15f,
        float matWeight = 12f, float valWeight = 8f, float miscWeight = 45f)
    {
        string path = $"{folder}/{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<MapSpawnProfile>(path);

        MapSpawnProfile p;
        if (existing != null)
        {
            p = existing;
        }
        else
        {
            p = ScriptableObject.CreateInstance<MapSpawnProfile>();
            AssetDatabase.CreateAsset(p, path);
        }

        p.profileId = id;
        p.displayName = displayName;
        p.spawnMultiplier = spawnMult;
        p.qualityMultiplier = qualityMult;
        p.groundItemBudget = new RangeInt(groundMin, groundLen);
        p.containerItemBudget = new RangeInt(containerMin, containerLen);
        p.catWeapon = weapWeight;
        p.catMedical = medWeight;
        p.catConsumable = consWeight;
        p.catMaterial = matWeight;
        p.catValuable = valWeight;
        p.catMisc = miscWeight;

        EditorUtility.SetDirty(p);
    }

    static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
