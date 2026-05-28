using UnityEngine;

/// <summary>
/// 아이템 스폰 테이블 ScriptableObject.
/// 루팅 상자, 바닥 스폰 등에서 사용.
/// </summary>
[CreateAssetMenu(fileName = "NewSpawnTable", menuName = "Dev Tools/Item/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    [Tooltip("테이블 ID")]
    public string tableId;

    [Tooltip("스폰 항목들")]
    public SpawnEntry[] entries;

    [Tooltip("한 번에 뽑는 횟수")]
    [Min(1)]
    public int rollCount = 3;

    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("스폰할 아이템")]
        public ItemData itemData;

        [Tooltip("확률 가중치 (높을수록 잘 나옴)")]
        [Min(0.01f)]
        public float weight = 1f;

        [Tooltip("최소 수량")]
        [Min(1)]
        public int minCount = 1;

        [Tooltip("최대 수량")]
        [Min(1)]
        public int maxCount = 1;
    }

    /// <summary>테이블에서 아이템 뽑기</summary>
    public ItemInstance[] Roll()
    {
        if (entries == null || entries.Length == 0) return new ItemInstance[0];

        // 가중치 합계
        float totalWeight = 0f;
        for (int i = 0; i < entries.Length; i++)
            totalWeight += entries[i].weight;

        if (totalWeight <= 0f) return new ItemInstance[0];

        var results = new ItemInstance[rollCount];
        int resultCount = 0;

        for (int r = 0; r < rollCount; r++)
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                cumulative += entries[i].weight;
                if (roll <= cumulative)
                {
                    var entry = entries[i];
                    if (entry.itemData != null)
                    {
                        int count = Random.Range(entry.minCount, entry.maxCount + 1);
                        results[resultCount++] = new ItemInstance(entry.itemData, count);
                    }
                    break;
                }
            }
        }

        // 실제 생성된 수만큼 배열 축소
        var trimmed = new ItemInstance[resultCount];
        System.Array.Copy(results, trimmed, resultCount);
        return trimmed;
    }
}
