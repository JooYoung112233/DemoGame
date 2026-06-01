using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 루팅 상자 / 창고 컴포넌트.
/// 자체 InventoryGrid를 소유.
/// InteractableObject.Container와 연동.
///
/// 현재 동작: 열면 내부 아이템을 전부 플레이어 인벤토리로 자동 이동.
/// 향후: UI 패널에서 드래그앤드롭으로 선택적 루팅.
/// </summary>
public class LootContainer : MonoBehaviour
{
    [Header("격자 설정")]
    [Tooltip("격자 가로 칸 수")]
    [SerializeField] int gridWidth = 4;
    [Tooltip("격자 세로 칸 수")]
    [SerializeField] int gridHeight = 5;

    [Header("표시")]
    [Tooltip("상자 이름 (UI에 표시)")]
    [SerializeField] string containerName = "상자";

    [Header("초기 아이템 (스폰)")]
    [Tooltip("상자 생성 시 자동으로 들어갈 아이템 목록")]
    [SerializeField] LootEntry[] initialLoot;

    public InventoryGrid Grid { get; private set; }
    public string ContainerName => containerName;
    public bool IsOpen { get; private set; }
    public bool IsLooted { get; private set; }

    /// <summary>수색 연출을 이미 완료했는지 (true면 재오픈 시 즉시 전체 공개)</summary>
    public bool HasBeenSearched { get; set; }

    [System.Serializable]
    public struct LootEntry
    {
        [Tooltip("ItemDatabase에 등록된 아이템 ID")]
        public string itemId;
        [Tooltip("수량")]
        public int count;
        [Tooltip("스폰 확률 (0~1, 1이면 항상)")]
        [Range(0f, 1f)]
        public float chance;
    }

    void Awake()
    {
        Grid = new InventoryGrid(gridWidth, gridHeight);
    }

    void Start()
    {
        // 초기 아이템 생성
        if (initialLoot != null)
        {
            for (int i = 0; i < initialLoot.Length; i++)
            {
                if (Random.value > initialLoot[i].chance) continue;

                var data = ItemDatabase.Get(initialLoot[i].itemId);
                if (data == null)
                {
                    Debug.LogWarning($"[LootContainer] ItemDatabase에 '{initialLoot[i].itemId}' 없음");
                    continue;
                }

                int count = Mathf.Max(1, initialLoot[i].count);
                var item = new ItemInstance(data, count);
                Grid.TryAutoPlace(item);
            }
        }
    }

    /// <summary>상자 열기 → CharacterPanelUI에서 드래그앤드롭으로 루팅</summary>
    // TODO(TopDownPlayer): public void Open(PlayerController player)
    public void Open(GameObject playerGO)
    {
        IsOpen = true;

        if (IsLooted)
        {
            Debug.Log($"[LootContainer] {containerName}: 이미 루팅됨 (비어있음)");
            return;
        }

        Debug.Log($"[LootContainer] {containerName} 열기 ({Grid.ItemCount}개 아이템)");
    }

    /// <summary>상자 닫기</summary>
    public void Close()
    {
        IsOpen = false;

        // 비어있으면 루팅 완료 처리
        if (Grid.ItemCount == 0)
            IsLooted = true;
    }

    /// <summary>코드에서 아이템 추가</summary>
    public bool AddItem(ItemInstance item)
    {
        return Grid.TryAutoPlace(item);
    }
}
