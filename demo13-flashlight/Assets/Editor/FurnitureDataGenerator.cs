using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 기본 FurnitureData SO 에셋 일괄 생성.
/// 메뉴: Tools > Dev Tools > Data > Generate Default Furniture
/// </summary>
public class FurnitureDataGenerator
{
    static readonly string FOLDER = "Assets/Resources/Data/Furniture";

    struct FurnitureDef
    {
        public string id, name, desc;
        public int w, h, price;
        public ItemCategory[] cats;

        public FurnitureDef(string id, string name, string desc, int w, int h, int price, ItemCategory[] cats)
        {
            this.id = id; this.name = name; this.desc = desc;
            this.w = w; this.h = h; this.price = price; this.cats = cats;
        }
    }

    [MenuItem("Tools/Dev Tools/Data/Generate Default Furniture")]
    static void Generate()
    {
        // 폴더 확보
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        if (!AssetDatabase.IsValidFolder(FOLDER))
            AssetDatabase.CreateFolder("Assets/Resources/Data", "Furniture");

        var defs = new FurnitureDef[]
        {
            new FurnitureDef("box_basic", "일반 상자", "아무 아이템이나 보관 가능한 범용 상자.",
                4, 4, 50, new ItemCategory[0]),
            new FurnitureDef("fridge", "냉장고", "음식과 음료를 신선하게 보관.",
                3, 5, 200, new[] { ItemCategory.Consumable }),
            new FurnitureDef("drawer", "서랍", "의료품, 소비품, 기타 잡화를 정리.",
                5, 3, 120, new[] { ItemCategory.Medical, ItemCategory.Consumable, ItemCategory.Misc }),
            new FurnitureDef("weapon_rack", "무기 거치대", "무기를 안전하게 거치.",
                6, 2, 300, new[] { ItemCategory.Weapon }),
            new FurnitureDef("bookshelf", "책꽂이", "열쇠, 쪽지, 문서류 보관.",
                3, 4, 100, new[] { ItemCategory.Key, ItemCategory.Misc }),
            new FurnitureDef("material_bin", "재료함", "제작 재료를 분류 보관.",
                4, 5, 150, new[] { ItemCategory.Material }),
            new FurnitureDef("safe", "금고", "귀중품 전용. 견고한 잠금장치.",
                3, 3, 500, new[] { ItemCategory.Valuable }),
        };

        int created = 0, skipped = 0;

        foreach (var d in defs)
        {
            string path = $"{FOLDER}/{d.id}.asset";
            if (AssetDatabase.LoadAssetAtPath<FurnitureData>(path) != null)
            {
                skipped++;
                continue;
            }

            var so = ScriptableObject.CreateInstance<FurnitureData>();
            so.furnitureId = d.id;
            so.displayName = d.name;
            so.description = d.desc;
            so.gridWidth = d.w;
            so.gridHeight = d.h;
            so.allowedCategories = d.cats;
            so.buyPriceRudy = d.price;
            so.materialCost = new FurnitureData.MaterialCost[0];

            AssetDatabase.CreateAsset(so, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[FurnitureDataGenerator]</color> 생성: {created}개, 스킵(이미 존재): {skipped}개 → {FOLDER}");
        EditorUtility.DisplayDialog("가구 데이터 생성",
            $"생성: {created}개\n스킵: {skipped}개\n위치: {FOLDER}", "확인");
    }
}
