using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 제작 시스템 싱글톤.
/// 레시피 해금 관리, 제작 실행, 무기 수리.
/// DontDestroyOnLoad — 세이브/로드 시 해금 목록 유지.
/// </summary>
public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; private set; }

    RecipeData[] allRecipes;
    HashSet<string> unlockedRecipes = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<CraftingSystem>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[CraftingSystem]");
        go.AddComponent<CraftingSystem>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadRecipes();
    }

    void LoadRecipes()
    {
        allRecipes = Resources.LoadAll<RecipeData>("Data/Recipes");
        unlockedRecipes.Clear();

        for (int i = 0; i < allRecipes.Length; i++)
        {
            if (allRecipes[i].unlockedByDefault)
                unlockedRecipes.Add(allRecipes[i].recipeId);
        }

        Debug.Log($"[CraftingSystem] {allRecipes.Length}개 레시피 로드, {unlockedRecipes.Count}개 기본 해금");
    }

    #region 레시피 해금

    public bool IsUnlocked(string recipeId)
    {
        return unlockedRecipes.Contains(recipeId);
    }

    public bool UnlockRecipe(string recipeId)
    {
        if (unlockedRecipes.Contains(recipeId)) return false;
        unlockedRecipes.Add(recipeId);
        Debug.Log($"[CraftingSystem] 레시피 해금: {recipeId}");
        return true;
    }

    /// <summary>레시피 문서(itemId) 사용 시 해금. 이미 해금이면 false(소모 안 함).</summary>
    public bool TryUnlockFromItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || allRecipes == null) return false;

        for (int i = 0; i < allRecipes.Length; i++)
        {
            var recipe = allRecipes[i];
            if (recipe.unlockRecipeItemId != itemId) continue;

            if (unlockedRecipes.Contains(recipe.recipeId))
            {
                Debug.Log($"[CraftingSystem] 이미 해금된 레시피: {recipe.displayName}");
                return false;
            }

            return UnlockRecipe(recipe.recipeId);
        }

        return false;
    }

    #endregion

    #region 레시피 조회

    public List<RecipeData> GetRecipesForStation(CraftingStation station)
    {
        var result = new List<RecipeData>();
        for (int i = 0; i < allRecipes.Length; i++)
        {
            if (allRecipes[i].station == station && unlockedRecipes.Contains(allRecipes[i].recipeId))
                result.Add(allRecipes[i]);
        }
        return result;
    }

    public RecipeData GetRecipe(string recipeId)
    {
        for (int i = 0; i < allRecipes.Length; i++)
        {
            if (allRecipes[i].recipeId == recipeId)
                return allRecipes[i];
        }
        return null;
    }

    #endregion

    #region 제작

    public bool CanCraft(RecipeData recipe, PlayerInventory inventory)
    {
        if (recipe == null || inventory == null) return false;
        if (!unlockedRecipes.Contains(recipe.recipeId)) return false;

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            int have = CountItem(inventory.Grid, recipe.ingredients[i].itemId);
            if (have < recipe.ingredients[i].count) return false;
        }

        var resultData = ItemDatabase.Get(recipe.resultItemId);
        if (resultData == null) return false;

        return true;
    }

    public bool Craft(RecipeData recipe, PlayerInventory inventory)
    {
        if (!CanCraft(recipe, inventory)) return false;

        var resultData = ItemDatabase.Get(recipe.resultItemId);
        if (resultData == null) return false;

        // 결과물이 인벤에 들어갈 공간이 있는지 사전 체크
        var testItem = new ItemInstance(resultData, recipe.resultCount);
        if (!inventory.Grid.CanAutoPlace(testItem)) return false;

        // 재료 소모
        for (int i = 0; i < recipe.ingredients.Length; i++)
            ConsumeItem(inventory.Grid, recipe.ingredients[i].itemId, recipe.ingredients[i].count);

        // 결과물 추가
        var resultItem = new ItemInstance(resultData, recipe.resultCount);
        inventory.Grid.TryAutoPlace(resultItem);

        Debug.Log($"[CraftingSystem] 제작 완료: {resultData.displayName} x{recipe.resultCount}");
        return true;
    }

    #endregion

    #region 수리

    static readonly float REPAIR_AMOUNT = 0.3f; // 1회 수리 = 최대 내구도의 30%

    public struct RepairCost
    {
        public string materialId;
        public int count;
    }

    public RepairCost GetRepairCost(ItemInstance item)
    {
        if (item == null || item.data == null || !item.data.hasDurability)
            return new RepairCost();

        // 무기 카테고리 → 고철, 그 외 → 천
        string matId = item.data.category == ItemCategory.Weapon ? "scrap_metal" : "cloth_rag";
        int cost = Mathf.Max(1, Mathf.CeilToInt(item.data.maxDurability * REPAIR_AMOUNT / 30f));

        return new RepairCost { materialId = matId, count = cost };
    }

    public bool CanRepair(ItemInstance item, PlayerInventory inventory)
    {
        if (item == null || !item.HasDurability) return false;
        if (item.durability >= item.data.maxDurability) return false;

        var cost = GetRepairCost(item);
        if (string.IsNullOrEmpty(cost.materialId)) return false;

        return CountItem(inventory.Grid, cost.materialId) >= cost.count;
    }

    public bool Repair(ItemInstance item, PlayerInventory inventory)
    {
        if (!CanRepair(item, inventory)) return false;

        var cost = GetRepairCost(item);
        ConsumeItem(inventory.Grid, cost.materialId, cost.count);

        float amount = item.data.maxDurability * REPAIR_AMOUNT;
        item.durability = Mathf.Min(item.durability + amount, item.data.maxDurability);

        Debug.Log($"[CraftingSystem] 수리 완료: {item.data.displayName} → {item.durability:F0}/{item.data.maxDurability:F0}");
        return true;
    }

    #endregion

    #region 유틸

    /// <summary>재료 보유량 = 가방(bag) + 메인 창고(MainStash) 합산.
    /// (집에서 제작/수리하므로 창고에 둔 재료도 함께 인정 — HideoutModuleManager와 동일 규칙.)</summary>
    int CountItem(InventoryGrid bag, string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;

        int total = 0;
        if (bag != null) total += bag.CountItem(itemId);

        var stash = (MainStash.Instance != null) ? MainStash.Instance.Grid : null;
        if (stash != null) total += stash.CountItem(itemId);

        return total;
    }

    /// <summary>재료 소모 — 창고 먼저, 부족분은 가방에서. 둘 다 NotifyChanged.</summary>
    void ConsumeItem(InventoryGrid bag, string itemId, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(itemId)) return;

        int remaining = amount;

        var stash = (MainStash.Instance != null) ? MainStash.Instance.Grid : null;
        if (stash != null)
        {
            int take = Mathf.Min(stash.CountItem(itemId), remaining);
            if (take > 0)
            {
                stash.ConsumeItem(itemId, take); // 내부에서 OnChanged 발생
                remaining -= take;
            }
        }

        if (remaining > 0 && bag != null)
            bag.ConsumeItem(itemId, remaining); // 내부에서 OnChanged 발생
    }

    #endregion
}
