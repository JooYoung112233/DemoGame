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
            if ((Plan3D.ToPlan(wi.transform.position) - c).sqrMagnitude <= r2)
                result.Add(wi);
        }
        result.Sort((a, b) =>
            ((Plan3D.ToPlan(a.transform.position) - c).sqrMagnitude)
            .CompareTo((Plan3D.ToPlan(b.transform.position) - c).sqrMagnitude));
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

    /// <summary>코드에서 월드 아이템 드롭.
    /// <paramref name="owner"/>를 주면 그 오브젝트의 **씬**으로 옮긴다 — 맵 씬의 스폰 지점은 반드시 넘길 것.
    /// ⚠️ 안 넘기면 활성 씬에 생기는데, 맵 로드 직후의 Start()는 <c>SetActiveScene(맵)</c>보다 **먼저** 돈다.
    ///    그때 활성 씬은 Systems라 루팅 아이템이 Systems에 쌓이고, 맵을 떠나도 안 지워졌다(Zone1 1회 32개 누수).</summary>
    public static WorldItem Drop(ItemInstance item, Vector3 position, Component owner = null)
    {
        if (item == null || item.data == null) return null;

        GameObject go;
        if (item.data.worldDropPrefab != null)
        {
            go = Instantiate(item.data.worldDropPrefab, position, Quaternion.identity);
        }
        else
        {
            // 임시 그레이박스 상자(희귀도 색). 추후 worldDropPrefab으로 교체.
            // ⚠️ 2D 시절엔 납작한 스프라이트였다. 쿼터뷰에서 바닥에 눕힌 판은 **거의 안 보인다**
            //    — 루팅이 핵심 루프인데 떨어진 물건이 눈에 안 띄면 게임이 성립하지 않는다.
            //    지면에서 살짝 띄운 작은 상자로 세운다.
            go = new GameObject();
            go.transform.position = position;
            const float S = 0.28f;
            GreyboxMesh.Box(go.transform, "Visual", new Vector3(0f, S * 0.5f, 0f),
                            new Vector3(S, S, S), item.data.RarityColor);
        }

        go.name = $"WorldItem_{item.data.itemId}";

        if (owner != null)
        {
            var scene = owner.gameObject.scene;
            // DDOL 씬(플레이어 등)은 MoveGameObjectToScene 대상이 아니다 — 그땐 활성 씬(=현재 맵)에 둔다.
            if (scene.IsValid() && scene.isLoaded && scene != go.scene && scene.name != "DontDestroyOnLoad")
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        }

        // 하이어라키 정리 — 맵 씬 루트에 수십 개가 평평하게 깔리지 않게 [Runtime]/Loot에 묶는다(적은 [Runtime]/Enemies).
        //   Systems·맵툴·DDOL 씬엔 폴더를 만들지 않는다.
        if (SystemsScene.IsGameplayScene(go.scene) && go.scene.name != "DontDestroyOnLoad")
            go.transform.SetParent(HierarchyFolder.RuntimeFolder(go.scene, "Loot"), true);

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

            // 정산 획득 목록 — 여기가 실제 바닥 줍기 경로다(InteractableObject의 TrackLoot는
            // WorldItem이 없을 때만 타는 폴백 분기라, 이게 없으면 정산에 전리품이 하나도 안 잡힌다).
            if (RaidManager.Instance != null)
                RaidManager.Instance.TrackLoot(Item);

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
        // 빌보드 캔버스 — 이름만 "빌보드"였고 실제로는 고정이었다.
        // 2D에선 카메라가 정면이라 티가 안 났지만 쿼터뷰에선 눕혀져 글자가 안 읽힌다.
        var labelGO = new GameObject("WorldLabel");
        labelGO.transform.SetParent(parent, false);
        labelGO.transform.localPosition = new Vector3(0, 0.62f, 0);
        Billboard.Attach(labelGO.transform);

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
