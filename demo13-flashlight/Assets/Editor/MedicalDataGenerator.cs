using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// MedicalItemData SO 일괄 생성 + ItemData.medicalData 자동 연결.
/// 메뉴: Tools > Dev Tools > Data > Generate Medical Data
/// </summary>
public class MedicalDataGenerator
{
    static readonly string FOLDER = "Assets/Resources/Data/Medical";

    struct MedDef
    {
        public string itemId, name, desc;
        public InjuryType[] targets;
        public bool universal;
        public float useTime, heal, duration;

        public MedDef(string id, string name, string desc,
            InjuryType[] targets, bool universal,
            float useTime, float heal, float duration)
        {
            this.itemId = id; this.name = name; this.desc = desc;
            this.targets = targets; this.universal = universal;
            this.useTime = useTime; this.heal = heal; this.duration = duration;
        }
    }

    [MenuItem("Tools/Dev Tools/Data/Generate Medical Data")]
    static void Generate()
    {
        // 폴더 확보
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        if (!AssetDatabase.IsValidFolder(FOLDER))
            AssetDatabase.CreateFolder("Assets/Resources/Data", "Medical");

        var defs = new MedDef[]
        {
            // 출혈 치료
            new MedDef("bandage", "붕대", "가벼운 출혈을 지혈한다.",
                new[] { InjuryType.Bleeding }, false, 3f, 0.6f, 0f),
            new MedDef("bandage_compress", "압박 붕대", "출혈을 빠르게 지혈하는 압박 붕대.",
                new[] { InjuryType.Bleeding }, false, 2f, 1f, 0f),
            new MedDef("gauze_roll", "거즈 롤", "넓은 부위의 출혈에 사용.",
                new[] { InjuryType.Bleeding }, false, 3f, 0.8f, 0f),
            new MedDef("hemostatic_powder", "지혈 분말", "심한 출혈에 효과적인 지혈제.",
                new[] { InjuryType.Bleeding }, false, 1.5f, 1f, 0f),
            new MedDef("tourniquet", "지혈대", "사지 출혈을 즉시 차단. 장기 사용 시 부작용.",
                new[] { InjuryType.Bleeding }, false, 1f, 1f, 0f),

            // 골절 치료
            new MedDef("splint", "부목", "골절 부위를 고정한다.",
                new[] { InjuryType.Fracture }, false, 4f, 1f, 0f),
            new MedDef("splint_makeshift", "응급 부목", "임시 고정. 완전하진 않지만 응급용.",
                new[] { InjuryType.Fracture }, false, 3f, 0.5f, 0f),

            // 통증 완화
            new MedDef("painkiller", "진통제", "일시적으로 통증을 억제한다.",
                new[] { InjuryType.Pain }, false, 1f, 1f, 180f),
            new MedDef("morphine_ampule", "모르핀 앰플", "강력한 진통 효과. 모든 통증 즉시 완화.",
                new[] { InjuryType.Pain }, false, 0.5f, 1f, 300f),

            // 범용
            new MedDef("first_aid_kit", "구급상자", "모든 부상을 치료할 수 있는 종합 키트.",
                new[] { InjuryType.Bleeding, InjuryType.Fracture, InjuryType.Pain },
                true, 5f, 1f, 0f),
            new MedDef("ifak_injury", "개인응급키트(IFAK)", "전투용 다목적 의료 키트.",
                new[] { InjuryType.Bleeding, InjuryType.Fracture, InjuryType.Pain },
                true, 4f, 1f, 0f),

            // 기타 의료
            new MedDef("antidote", "해독제", "독소/감염 치료. 출혈도 부분적으로 완화.",
                new[] { InjuryType.Bleeding, InjuryType.Pain }, false, 2f, 0.5f, 0f),
            new MedDef("disinfectant", "소독약", "상처 감염 방지. 출혈 회복 보조.",
                new[] { InjuryType.Bleeding }, false, 2f, 0.3f, 0f),
        };

        int created = 0, linked = 0;
        var medSOMap = new Dictionary<string, MedicalItemData>();

        // 1단계: MedicalItemData SO 생성
        foreach (var d in defs)
        {
            string path = $"{FOLDER}/{d.itemId}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MedicalItemData>(path);
            if (existing != null)
            {
                medSOMap[d.itemId] = existing;
                continue;
            }

            var so = ScriptableObject.CreateInstance<MedicalItemData>();
            so.itemId = d.itemId;
            so.displayName = d.name;
            so.description = d.desc;
            so.targetInjuries = d.targets;
            so.isUniversal = d.universal;
            so.useTime = d.useTime;
            so.healAmount = d.heal;
            so.effectDuration = d.duration;

            AssetDatabase.CreateAsset(so, path);
            medSOMap[d.itemId] = so;
            created++;
        }

        AssetDatabase.SaveAssets();

        // 2단계: ItemData SO에 medicalData 연결
        var allItems = Resources.LoadAll<ItemData>("Items");
        foreach (var item in allItems)
        {
            if (item.medicalData != null) continue; // 이미 연결됨
            if (!medSOMap.ContainsKey(item.itemId)) continue;

            item.medicalData = medSOMap[item.itemId];
            EditorUtility.SetDirty(item);
            linked++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[MedicalDataGenerator]</color> SO 생성: {created}개, ItemData 연결: {linked}개");
        EditorUtility.DisplayDialog("의료 데이터 생성",
            $"MedicalItemData SO 생성: {created}개\nItemData 연결: {linked}개\n위치: {FOLDER}", "확인");
    }
}
