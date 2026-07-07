using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 월드에 떨어진 아이템.
/// 바닥에 드롭되거나 스폰된 아이템. E키로 줍기.
/// InteractableObject와 연동.
/// 아이템 위에 이름 라벨 표시.
/// </summary>
public class WorldItem : MonoBehaviour
{
    /// <summary>활성 WorldItem 정적 레지스트리 — 바닥 중첩 클러스터 판정용.</summary>
    public static readonly List<WorldItem> All = new List<WorldItem>();

    [SerializeField] ItemData itemData;
    [SerializeField] int stackCount = 1;

    ItemInstance itemInstance;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>center 반경 radius 안의 WorldItem들을 가까운 순으로 모은다(중첩 줍기 목록용).</summary>
    public static List<WorldItem> GatherNear(Vector3 center, float radius)
    {
        var result = new List<WorldItem>();
        float r2 = radius * radius;
        Vector2 c = center;
        for (int i = 0; i < All.Count; i++)
        {
            var wi = All[i];
            if (wi == null) continue;
            if (((Vector2)wi.transform.position - c).sqrMagnitude <= r2)
                result.Add(wi);
        }
        result.Sort((a, b) =>
            (((Vector2)a.transform.position - c).sqrMagnitude)
            .CompareTo(((Vector2)b.transform.position - c).sqrMagnitude));
        return result;
    }

    /// <summary>아이템 인스턴스 (런타임 생성 또는 외부 할당)</summary>
    public ItemInstance Item
    {
        get
        {
            if (itemInstance == null && itemData != null)
                itemInstance = new ItemInstance(itemData, stackCount);
            return itemInstance;
        }
    }

    /// <summary>코드에서 월드 아이템 드롭</summary>
    public static WorldItem Drop(ItemInstance item, Vector3 position)
    {
        if (item == null || item.data == null) return null;

        GameObject go;
        if (item.data.worldDropPrefab != null)
        {
            go = Instantiate(item.data.worldDropPrefab, position, Quaternion.identity);
        }
        else
        {
            // 임시 2D 스프라이트 (사각, 희귀도 색). 추후 worldDropPrefab으로 교체
            go = new GameObject();
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprite.Square;
            sr.color = item.data.RarityColor;
            sr.sortingOrder = 3;
        }

        go.name = $"WorldItem_{item.data.itemId}";

        // WorldItem 컴포넌트
        var worldItem = go.AddComponent<WorldItem>();
        worldItem.itemInstance = item;
        worldItem.itemData = item.data;
        worldItem.stackCount = item.stackCount;

        // InteractableObject 추가 (E키 줍기 연동)
        var interactable = go.AddComponent<InteractableObject>();
        string displayName = item.stackCount > 1
            ? $"{item.data.displayName} x{item.stackCount}"
            : item.data.displayName;
        interactable.SetupAsPickup(item.data.itemId, item.stackCount, $"줍기: {displayName}");

        // 월드 이름 라벨 생성
        CreateWorldLabel(go.transform, displayName, item.data.RarityColor);

        return worldItem;
    }

    /// <summary>플레이어가 줍기 시도</summary>
    public bool TryPickup(GameObject playerGO)
    {
        if (Item == null) return false;

        var inventory = playerGO.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[WorldItem] PlayerInventory를 찾을 수 없음");
            return false;
        }

        if (inventory.TryPickup(Item))
        {
            Debug.Log($"[WorldItem] {Item.DisplayName} 획득");

            // 수집형 퀘스트 목표 카운트 — E키 픽업(InteractableObject:340)과 동일 훅.
            // 이게 없으면 바닥 줍기/클러스터(GroundPickupUI) 픽업이 BQ/DQ 수집 목표에 안 잡힌다.
            if (QuestManager.Instance != null && Item.data != null)
                QuestManager.Instance.UpdateObjective(ObjectiveType.CollectItem, Item.data.itemId, Mathf.Max(1, Item.stackCount));

            Destroy(gameObject);
            return true;
        }
        else
        {
            string reason = inventory.HasBackpack ? "인벤토리 공간 부족" : "가방을 장착하세요";
            Debug.Log($"[WorldItem] {reason}");
            ToastManager.Show(reason, ToastManager.ToastType.Warning);
            return false;
        }
    }

    /// <summary>아이템 위에 월드 스페이스 이름 라벨 생성</summary>
    static void CreateWorldLabel(Transform parent, string itemName, Color rarityColor)
    {
        // 빌보드 캔버스
        var labelGO = new GameObject("WorldLabel");
        labelGO.transform.SetParent(parent, false);
        labelGO.transform.localPosition = new Vector3(0, 0.5f, 0);

        var canvas = labelGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        var canvasRT = labelGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(2f, 0.4f);
        canvasRT.localScale = new Vector3(0.01f, 0.01f, 0.01f); // 월드 스케일

        // 배경
        var bgGO = new GameObject("Bg");
        bgGO.transform.SetParent(canvasRT, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.75f);

        // 이름 텍스트
        var textGO = new GameObject("Name");
        textGO.transform.SetParent(canvasRT, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(4, 0);
        textRT.offsetMax = new Vector2(-4, 0);

        var txt = textGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.fontStyle = FontStyle.Bold;
        txt.color = rarityColor;
        txt.text = itemName;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        // 그림자
        var shadow = textGO.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);

        // 탑다운 2D: 카메라가 고정 정면이라 빌보드 불필요 (라벨이 화면에 평평하게 보임)
    }
}
