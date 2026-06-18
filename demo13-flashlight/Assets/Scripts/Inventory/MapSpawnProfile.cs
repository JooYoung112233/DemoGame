using UnityEngine;

/// <summary>
/// 맵 스폰 프로파일 ScriptableObject.
/// 맵 단위 아이템 스폰의 총량, 희귀도 분포, 카테고리 쿼터, 난이도를 정의.
/// MapSpawnController에서 참조.
/// </summary>
[CreateAssetMenu(fileName = "NewMapSpawnProfile", menuName = "Dev Tools/Item/Map Spawn Profile")]
public class MapSpawnProfile : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("프로파일 ID (맵 이름과 매칭)")]
    public string profileId;

    [Tooltip("표시 이름")]
    public string displayName;

    [Header("총량 예산")]
    [Tooltip("바닥 스폰 포인트 총 아이템 수 (min~max)")]
    public RangeInt groundItemBudget = new RangeInt(15, 10); // 15~25

    [Tooltip("상자 스폰 포인트 총 아이템 수 (min~max)")]
    public RangeInt containerItemBudget = new RangeInt(20, 15); // 20~35

    [Tooltip("고정 아이템 수 (열쇠, 스토리 등)")]
    public int fixedItemCount = 2;

    [Header("난이도 배율")]
    [Tooltip("전체 스폰량 배율 (1=기본, 0.5=절반, 2=두배)")]
    [Range(0.1f, 3f)]
    public float spawnMultiplier = 1f;

    [Tooltip("고급 아이템 확률 배율 (1=기본, 높을수록 좋은 아이템 증가)")]
    [Range(0.1f, 3f)]
    public float qualityMultiplier = 1f;

    [Header("희귀도 분포 (가중치, 합계 100일 필요 없음)")]
    [Tooltip("Common 가중치")]
    public float rarityCommon = 50f;
    [Tooltip("Uncommon 가중치")]
    public float rarityUncommon = 28f;
    [Tooltip("Rare 가중치")]
    public float rarityRare = 14f;
    [Tooltip("Epic 가중치")]
    public float rarityEpic = 6f;
    [Tooltip("Legendary 가중치")]
    public float rarityLegendary = 2f;

    [Header("카테고리 쿼터 (가중치)")]
    [Tooltip("무기")]
    public float catWeapon = 5f;
    [Tooltip("의료")]
    public float catMedical = 15f;
    [Tooltip("소비")]
    public float catConsumable = 15f;
    [Tooltip("재료")]
    public float catMaterial = 12f;
    [Tooltip("귀중품")]
    public float catValuable = 8f;
    [Tooltip("열쇠 (고정 스폰 별도이므로 낮게)")]
    public float catKey = 0f;
    [Tooltip("기타 (잡템 등)")]
    public float catMisc = 45f;

    [Header("낮/밤 보정")]
    [Tooltip("밤에 스폰량 배율")]
    [Range(0.5f, 2f)]
    public float nightSpawnMultiplier = 1.2f;

    [Tooltip("밤에 고급 아이템 확률 배율")]
    [Range(0.5f, 3f)]
    public float nightQualityMultiplier = 1.5f;

    /// <summary>전역 아이템 스폰 총량 배율 (GameTuning, 에셋 없으면 1f = 동일)</summary>
    static float GlobalSpawnMult()
    {
        return GameTuning.Instance != null ? GameTuning.Instance.itemSpawnCountMult : 1f;
    }

    /// <summary>실제 바닥 아이템 예산 (배율 적용)</summary>
    public int GetGroundBudget(bool isNight)
    {
        float mult = spawnMultiplier * (isNight ? nightSpawnMultiplier : 1f) * GlobalSpawnMult();
        int min = Mathf.RoundToInt(groundItemBudget.start * mult);
        int max = Mathf.RoundToInt((groundItemBudget.start + groundItemBudget.length) * mult);
        return Random.Range(min, max + 1);
    }

    /// <summary>실제 상자 아이템 예산 (배율 적용)</summary>
    public int GetContainerBudget(bool isNight)
    {
        float mult = spawnMultiplier * (isNight ? nightSpawnMultiplier : 1f) * GlobalSpawnMult();
        int min = Mathf.RoundToInt(containerItemBudget.start * mult);
        int max = Mathf.RoundToInt((containerItemBudget.start + containerItemBudget.length) * mult);
        return Random.Range(min, max + 1);
    }

    /// <summary>희귀도별 가중치 배열 반환 (인덱스 = ItemRarity)</summary>
    public float[] GetRarityWeights(bool isNight)
    {
        float q = qualityMultiplier * (isNight ? nightQualityMultiplier : 1f);
        return new float[]
        {
            rarityCommon / q,       // Common은 품질 배율에 반비례 (낮아짐)
            rarityUncommon,
            rarityRare * q,
            rarityEpic * q,
            rarityLegendary * q,
        };
    }

    /// <summary>카테고리별 가중치 배열 반환 (인덱스 = ItemCategory)</summary>
    public float[] GetCategoryWeights()
    {
        return new float[]
        {
            catWeapon,
            catMedical,
            catConsumable,
            catMaterial,
            catValuable,
            catKey,
            catMisc,
        };
    }
}
