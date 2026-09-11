using UnityEngine;

public enum CraftingStation
{
    Workbench,   // 작업대: 무기 제작/수리
    MedicalBench,// 의료대: 일회용 치료템
    CookingBench // 조리대: 버프 음식
}

[System.Serializable]
public struct RecipeIngredient
{
    [Tooltip("재료 아이템 ID (ItemDatabase 기준)")]
    public string itemId;

    [Tooltip("필요 수량")]
    [Min(1)]
    public int count;
}

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Dev Tools/Item/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 레시피 ID")]
    public string recipeId;

    [Tooltip("표시 이름")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Header("제작 스테이션")]
    public CraftingStation station;

    [Header("재료")]
    public RecipeIngredient[] ingredients;

    [Header("결과물")]
    [Tooltip("완성 아이템 ID")]
    public string resultItemId;

    [Tooltip("완성 수량")]
    [Min(1)]
    public int resultCount = 1;

    [Header("해금")]
    [Tooltip("true면 처음부터 해금 (기본 레시피)")]
    public bool unlockedByDefault;

    [Tooltip("이 itemId 아이템(레시피 문서) 사용 시 영구 해금. 비우면 수동/기본 해금만")]
    public string unlockRecipeItemId;
}
