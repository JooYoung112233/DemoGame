using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 제작/수리 UI.
/// 작업대/의료대/조리대 상호작용 시 표시.
/// UIManager 자식으로 자동 생성.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    public bool IsShowing => isShowing;

    bool isShowing;
    CraftingStation currentStation;

    // 레퍼런스
    PlayerInventory playerInventory;

    // uGUI
    Canvas canvas;
    GameObject panelRoot;
    RectTransform panelRT;
    Text titleText;
    Text descText;

    // 탭 (제작 / 수리)
    GameObject craftTab;
    GameObject repairTab;
    Button craftTabBtn, repairTabBtn;
    Image craftTabBg, repairTabBg;
    bool showingRepairTab;

    // 제작 목록
    RectTransform recipeListRoot;
    List<RecipeData> currentRecipes = new List<RecipeData>();
    int selectedRecipeIndex = -1;

    // 선택된 레시피 상세
    Text selectedNameText;
    Text selectedIngredientsText;
    Text selectedResultText;
    Button craftButton;
    Text craftButtonText;
    string craftActionLabel = "제작";

    // 수리 목록
    RectTransform repairListRoot;
    List<InventoryGrid.PlacedItem> repairableItems = new List<InventoryGrid.PlacedItem>();
    int selectedRepairIndex = -1;

    // 수리 상세
    Text repairNameText;
    Text repairCostText;
    Text repairDurText;
    Button repairButton;
    Text repairButtonText;

    static readonly float PANEL_W = 520f;
    static readonly float PANEL_H = 480f;
    static readonly Color BG_COLOR = new Color(0.06f, 0.06f, 0.1f, 0.96f);
    static readonly Color ACCENT = new Color(1f, 0.75f, 0.2f);

    void Awake()
    {
        BuildUI();
    }

    void Update()
    {
        if (!isShowing) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }

        if (showingRepairTab)
            RefreshRepairDetail();
        else
            RefreshCraftDetail();
    }

    #region Show / Hide

    public void Show(CraftingStation station)
    {
        FindRefs();
        currentStation = station;
        isShowing = true;
        panelRoot.SetActive(true);

        string stationName;
        string hint;
        switch (station)
        {
            case CraftingStation.Workbench:
                stationName = "작업대";
                craftActionLabel = "제작";
                hint = "해금된 레시피 제작 · 수리 탭에서 내구도 복구";
                break;
            case CraftingStation.MedicalBench:
                stationName = "의료대";
                craftActionLabel = "제작";
                hint = "재료 소모 후 일회용 치료템 제작";
                break;
            case CraftingStation.CookingBench:
                stationName = "조리대";
                craftActionLabel = "조리";
                hint = "레시피 문서 해금 후 재료로 조리";
                break;
            default:
                stationName = "제작대";
                craftActionLabel = "제작";
                hint = "";
                break;
        }
        titleText.text = stationName;
        if (descText != null) descText.text = hint;

        bool hasRepair = station == CraftingStation.Workbench;
        repairTabBtn.gameObject.SetActive(hasRepair);

        showingRepairTab = false;
        SelectTab(false);
        if (craftButtonText != null)
            craftButtonText.text = craftActionLabel;
    }

    public void Hide()
    {
        isShowing = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    #endregion

    #region 레퍼런스

    void FindRefs()
    {
        if (playerInventory != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        playerInventory = go.GetComponent<PlayerInventory>();
    }

    public void ResetRefs()
    {
        playerInventory = null;
    }

    #endregion

    #region UI Build

    void BuildUI()
    {
        var canvasGO = new GameObject("CraftingUI_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // 전체 루트 (딤 배경)
        panelRoot = new GameObject("CraftRoot");
        panelRoot.transform.SetParent(canvasRT, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;
        panelRoot.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        // 중앙 패널
        var centerGO = new GameObject("CenterPanel");
        centerGO.transform.SetParent(rootRT, false);
        panelRT = centerGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(PANEL_W, PANEL_H);
        centerGO.AddComponent<Image>().color = BG_COLOR;

        // 제목
        titleText = MakeText(panelRT, "Title", "작업대",
            new Vector2(15, -8), new Vector2(200, 30), 20, ACCENT, TextAnchor.MiddleLeft);
        titleText.fontStyle = FontStyle.Bold;

        descText = MakeText(panelRT, "Desc", "",
            new Vector2(15, -34), new Vector2(380, 22), 12, new Color(0.65f, 0.7f, 0.8f), TextAnchor.MiddleLeft);

        // 닫기 버튼
        var closeBtnGO = new GameObject("CloseBtn");
        closeBtnGO.transform.SetParent(panelRT, false);
        var closeBtnRT = closeBtnGO.AddComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 1);
        closeBtnRT.anchorMax = new Vector2(1, 1);
        closeBtnRT.pivot = new Vector2(1, 1);
        closeBtnRT.anchoredPosition = new Vector2(-8, -8);
        closeBtnRT.sizeDelta = new Vector2(28, 28);
        var closeBg = closeBtnGO.AddComponent<Image>();
        closeBg.color = new Color(0.3f, 0.15f, 0.15f);
        var closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.AddListener(Hide);
        MakeChildText(closeBtnGO.transform, "X", 16, Color.white);

        // 탭 버튼
        float tabY = -58f;
        craftTabBtn = MakeTabButton(panelRT, "제작", new Vector2(15, tabY), out craftTabBg);
        craftTabBtn.onClick.AddListener(() => SelectTab(false));
        repairTabBtn = MakeTabButton(panelRT, "수리", new Vector2(80, tabY), out repairTabBg);
        repairTabBtn.onClick.AddListener(() => SelectTab(true));

        // 제작 탭 콘텐츠
        craftTab = new GameObject("CraftContent");
        craftTab.transform.SetParent(panelRT, false);
        var craftRT = craftTab.AddComponent<RectTransform>();
        craftRT.anchorMin = Vector2.zero;
        craftRT.anchorMax = Vector2.one;
        craftRT.offsetMin = new Vector2(10, 10);
        craftRT.offsetMax = new Vector2(-10, -82);
        BuildCraftContent(craftRT);

        // 수리 탭 콘텐츠
        repairTab = new GameObject("RepairContent");
        repairTab.transform.SetParent(panelRT, false);
        var repairRT = repairTab.AddComponent<RectTransform>();
        repairRT.anchorMin = Vector2.zero;
        repairRT.anchorMax = Vector2.one;
        repairRT.offsetMin = new Vector2(10, 10);
        repairRT.offsetMax = new Vector2(-10, -82);
        BuildRepairContent(repairRT);

        panelRoot.SetActive(false);
    }

    void BuildCraftContent(RectTransform parent)
    {
        // 좌측: 레시피 목록
        var listGO = new GameObject("RecipeList");
        listGO.transform.SetParent(parent, false);
        recipeListRoot = listGO.AddComponent<RectTransform>();
        recipeListRoot.anchorMin = new Vector2(0, 0);
        recipeListRoot.anchorMax = new Vector2(0.45f, 1);
        recipeListRoot.offsetMin = Vector2.zero;
        recipeListRoot.offsetMax = Vector2.zero;

        // 우측: 상세
        var detailGO = new GameObject("CraftDetail");
        detailGO.transform.SetParent(parent, false);
        var detailRT = detailGO.AddComponent<RectTransform>();
        detailRT.anchorMin = new Vector2(0.48f, 0);
        detailRT.anchorMax = Vector2.one;
        detailRT.offsetMin = Vector2.zero;
        detailRT.offsetMax = Vector2.zero;
        detailGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.8f);

        selectedNameText = MakeText(detailRT, "SelName", "",
            new Vector2(10, -8), new Vector2(240, 24), 16, Color.white, TextAnchor.MiddleLeft);
        selectedNameText.fontStyle = FontStyle.Bold;

        selectedIngredientsText = MakeText(detailRT, "SelIngr", "레시피를 선택하세요",
            new Vector2(10, -40), new Vector2(240, 140), 13, new Color(0.7f, 0.8f, 0.9f), TextAnchor.UpperLeft);

        selectedResultText = MakeText(detailRT, "SelResult", "",
            new Vector2(10, -185), new Vector2(240, 50), 13, new Color(0.4f, 1f, 0.5f), TextAnchor.UpperLeft);

        // 제작 버튼
        var btnGO = new GameObject("CraftBtn");
        btnGO.transform.SetParent(detailRT, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.1f, 0);
        btnRT.anchorMax = new Vector2(0.9f, 0);
        btnRT.pivot = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(0, 10);
        btnRT.sizeDelta = new Vector2(0, 36);
        var btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(0.15f, 0.35f, 0.2f);
        craftButton = btnGO.AddComponent<Button>();
        craftButton.targetGraphic = btnBg;
        craftButton.onClick.AddListener(OnCraftClicked);
        craftButtonText = MakeChildText(btnGO.transform, craftActionLabel, 15, Color.white);
    }

    void BuildRepairContent(RectTransform parent)
    {
        // 좌측: 수리 가능 아이템 목록
        var listGO = new GameObject("RepairList");
        listGO.transform.SetParent(parent, false);
        repairListRoot = listGO.AddComponent<RectTransform>();
        repairListRoot.anchorMin = new Vector2(0, 0);
        repairListRoot.anchorMax = new Vector2(0.45f, 1);
        repairListRoot.offsetMin = Vector2.zero;
        repairListRoot.offsetMax = Vector2.zero;

        // 우측: 상세
        var detailGO = new GameObject("RepairDetail");
        detailGO.transform.SetParent(parent, false);
        var detailRT = detailGO.AddComponent<RectTransform>();
        detailRT.anchorMin = new Vector2(0.48f, 0);
        detailRT.anchorMax = Vector2.one;
        detailRT.offsetMin = Vector2.zero;
        detailRT.offsetMax = Vector2.zero;
        detailGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.8f);

        repairNameText = MakeText(detailRT, "RepName", "",
            new Vector2(10, -8), new Vector2(240, 24), 16, Color.white, TextAnchor.MiddleLeft);
        repairNameText.fontStyle = FontStyle.Bold;

        repairDurText = MakeText(detailRT, "RepDur", "수리할 아이템을 선택하세요",
            new Vector2(10, -40), new Vector2(240, 60), 13, new Color(0.7f, 0.8f, 0.9f), TextAnchor.UpperLeft);

        repairCostText = MakeText(detailRT, "RepCost", "",
            new Vector2(10, -110), new Vector2(240, 60), 13, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperLeft);

        // 수리 버튼
        var btnGO = new GameObject("RepairBtn");
        btnGO.transform.SetParent(detailRT, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.1f, 0);
        btnRT.anchorMax = new Vector2(0.9f, 0);
        btnRT.pivot = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(0, 10);
        btnRT.sizeDelta = new Vector2(0, 36);
        var btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(0.15f, 0.25f, 0.4f);
        repairButton = btnGO.AddComponent<Button>();
        repairButton.targetGraphic = btnBg;
        repairButton.onClick.AddListener(OnRepairClicked);
        repairButtonText = MakeChildText(btnGO.transform, "수리", 15, Color.white);
    }

    #endregion

    #region 탭 전환

    void SelectTab(bool repair)
    {
        showingRepairTab = repair;
        craftTab.SetActive(!repair);
        repairTab.SetActive(repair);
        craftTabBg.color = !repair ? new Color(0.2f, 0.35f, 0.55f) : new Color(0.12f, 0.12f, 0.18f);
        repairTabBg.color = repair ? new Color(0.2f, 0.35f, 0.55f) : new Color(0.12f, 0.12f, 0.18f);

        if (repair)
            RefreshRepairList();
        else
            RefreshRecipeList();
    }

    #endregion

    #region 제작 목록

    void RefreshRecipeList()
    {
        ClearChildren(recipeListRoot);
        selectedRecipeIndex = -1;

        if (CraftingSystem.Instance == null) return;
        currentRecipes = CraftingSystem.Instance.GetRecipesForStation(currentStation);

        if (currentRecipes.Count == 0)
        {
            MakeText(recipeListRoot, "Empty", "해금된 레시피 없음",
                new Vector2(5, -5), new Vector2(220, 24), 13, new Color(0.5f, 0.5f, 0.6f), TextAnchor.MiddleLeft);
            return;
        }

        for (int i = 0; i < currentRecipes.Count; i++)
        {
            int idx = i;
            var recipe = currentRecipes[i];
            bool canCraft = playerInventory != null &&
                CraftingSystem.Instance.CanCraft(recipe, playerInventory);

            var btnGO = new GameObject($"Recipe_{i}");
            btnGO.transform.SetParent(recipeListRoot, false);
            var rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -i * 30);
            rt.sizeDelta = new Vector2(0, 28);

            var bg = btnGO.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.3f, 0.45f);
            btn.colors = colors;
            btn.onClick.AddListener(() => SelectRecipe(idx));

            Color nameColor = canCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            var txt = MakeChildText(btnGO.transform, recipe.displayName, 13, nameColor);
            txt.alignment = TextAnchor.MiddleLeft;

            // 좌측 패딩
            var txtRT = txt.GetComponent<RectTransform>();
            txtRT.offsetMin = new Vector2(8, 0);
        }
    }

    void SelectRecipe(int idx)
    {
        selectedRecipeIndex = idx;
        RefreshCraftDetail();
    }

    void RefreshCraftDetail()
    {
        if (selectedRecipeIndex < 0 || selectedRecipeIndex >= currentRecipes.Count)
        {
            selectedNameText.text = "";
            selectedIngredientsText.text = "레시피를 선택하세요";
            selectedResultText.text = "";
            craftButton.interactable = false;
            craftButtonText.color = new Color(0.4f, 0.4f, 0.4f);
            return;
        }

        var recipe = currentRecipes[selectedRecipeIndex];
        selectedNameText.text = recipe.displayName;
        selectedNameText.color = ACCENT;

        // 재료 표시
        string ingr = "<b>재료:</b>\n";
        bool allHave = true;
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            var ing = recipe.ingredients[i];
            var matData = ItemDatabase.Get(ing.itemId);
            string matName = matData != null ? matData.displayName : ing.itemId;
            int have = playerInventory != null ? CountInInventory(ing.itemId) : 0;
            bool enough = have >= ing.count;
            if (!enough) allHave = false;

            string colorHex = enough ? "88CC88" : "CC4444";
            ingr += $"  <color=#{colorHex}>{matName} {have}/{ing.count}</color>\n";
        }
        selectedIngredientsText.text = ingr;

        // 결과물
        var resultData = ItemDatabase.Get(recipe.resultItemId);
        if (resultData != null)
        {
            string resultName = recipe.resultCount > 1
                ? $"{resultData.displayName} x{recipe.resultCount}"
                : resultData.displayName;
            selectedResultText.text = $"<b>결과:</b> {resultName}";
        }
        else
        {
            selectedResultText.text = $"<b>결과:</b> {recipe.resultItemId} (미등록)";
        }

        bool canCraft = allHave && CraftingSystem.Instance != null
            && CraftingSystem.Instance.CanCraft(recipe, playerInventory);
        craftButton.interactable = canCraft;
        craftButtonText.color = canCraft ? Color.white : new Color(0.4f, 0.4f, 0.4f);
    }

    void OnCraftClicked()
    {
        if (selectedRecipeIndex < 0 || selectedRecipeIndex >= currentRecipes.Count) return;
        if (CraftingSystem.Instance == null || playerInventory == null) return;

        var recipe = currentRecipes[selectedRecipeIndex];
        if (CraftingSystem.Instance.Craft(recipe, playerInventory))
        {
            RefreshRecipeList();
            RefreshCraftDetail();
        }
    }

    #endregion

    #region 수리 목록

    void RefreshRepairList()
    {
        ClearChildren(repairListRoot);
        repairableItems.Clear();
        selectedRepairIndex = -1;

        if (playerInventory == null || playerInventory.Grid == null) return;

        var all = playerInventory.Grid.GetAll();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].item.HasDurability && all[i].item.durability < all[i].item.data.maxDurability)
                repairableItems.Add(all[i]);
        }

        if (repairableItems.Count == 0)
        {
            MakeText(repairListRoot, "Empty", "수리할 아이템 없음",
                new Vector2(5, -5), new Vector2(220, 24), 13, new Color(0.5f, 0.5f, 0.6f), TextAnchor.MiddleLeft);
            return;
        }

        for (int i = 0; i < repairableItems.Count; i++)
        {
            int idx = i;
            var p = repairableItems[i];
            float ratio = p.item.DurabilityRatio;

            var btnGO = new GameObject($"Repair_{i}");
            btnGO.transform.SetParent(repairListRoot, false);
            var rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -i * 30);
            rt.sizeDelta = new Vector2(0, 28);

            var bg = btnGO.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.3f, 0.45f);
            btn.colors = colors;
            btn.onClick.AddListener(() => SelectRepairItem(idx));

            Color durColor = ratio > 0.5f ? new Color(0.4f, 1f, 0.5f)
                : ratio > 0.2f ? new Color(1f, 0.9f, 0.3f)
                : new Color(1f, 0.3f, 0.3f);

            string label = $"{p.item.data.displayName}  <color=#{ColorUtility.ToHtmlStringRGB(durColor)}>{p.item.durability:F0}/{p.item.data.maxDurability:F0}</color>";
            var txt = MakeChildText(btnGO.transform, label, 12, Color.white);
            txt.supportRichText = true;
            txt.alignment = TextAnchor.MiddleLeft;
            var txtRT = txt.GetComponent<RectTransform>();
            txtRT.offsetMin = new Vector2(8, 0);
        }
    }

    void SelectRepairItem(int idx)
    {
        selectedRepairIndex = idx;
    }

    void RefreshRepairDetail()
    {
        if (selectedRepairIndex < 0 || selectedRepairIndex >= repairableItems.Count)
        {
            repairNameText.text = "";
            repairDurText.text = "수리할 아이템을 선택하세요";
            repairCostText.text = "";
            repairButton.interactable = false;
            repairButtonText.color = new Color(0.4f, 0.4f, 0.4f);
            return;
        }

        var p = repairableItems[selectedRepairIndex];
        var item = p.item;
        repairNameText.text = item.data.displayName;
        repairNameText.color = item.data.RarityColor;

        float ratio = item.DurabilityRatio;
        Color durColor = ratio > 0.5f ? new Color(0.4f, 1f, 0.5f)
            : ratio > 0.2f ? new Color(1f, 0.9f, 0.3f)
            : new Color(1f, 0.3f, 0.3f);

        float previewDur = Mathf.Min(item.durability + item.data.maxDurability * 0.3f, item.data.maxDurability);
        repairDurText.text = $"내구도: <color=#{ColorUtility.ToHtmlStringRGB(durColor)}>{item.durability:F0}</color> / {item.data.maxDurability:F0}\n" +
            $"수리 후: <color=#88CC88>{previewDur:F0}</color> / {item.data.maxDurability:F0}";

        if (CraftingSystem.Instance != null)
        {
            var cost = CraftingSystem.Instance.GetRepairCost(item);
            var matData = ItemDatabase.Get(cost.materialId);
            string matName = matData != null ? matData.displayName : cost.materialId;
            int have = playerInventory != null ? CountInInventory(cost.materialId) : 0;
            bool enough = have >= cost.count;
            string colorHex = enough ? "88CC88" : "CC4444";
            repairCostText.text = $"비용: <color=#{colorHex}>{matName} {have}/{cost.count}</color>";

            bool canRepair = enough && CraftingSystem.Instance.CanRepair(item, playerInventory);
            repairButton.interactable = canRepair;
            repairButtonText.color = canRepair ? Color.white : new Color(0.4f, 0.4f, 0.4f);
        }
    }

    void OnRepairClicked()
    {
        if (selectedRepairIndex < 0 || selectedRepairIndex >= repairableItems.Count) return;
        if (CraftingSystem.Instance == null || playerInventory == null) return;

        var item = repairableItems[selectedRepairIndex].item;
        if (CraftingSystem.Instance.Repair(item, playerInventory))
            RefreshRepairList();
    }

    #endregion

    #region 유틸

    int CountInInventory(string itemId)
    {
        if (playerInventory == null || playerInventory.Grid == null) return 0;
        int total = 0;
        var all = playerInventory.Grid.GetAll();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].item.data != null && all[i].item.data.itemId == itemId)
                total += all[i].item.stackCount;
        }
        return total;
    }

    void ClearChildren(RectTransform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    Button MakeTabButton(RectTransform parent, string label, Vector2 pos, out Image bg)
    {
        var go = new GameObject($"Tab_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(60, 24);

        bg = go.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        MakeChildText(go.transform, label, 13, Color.white);
        return btn;
    }

    Text MakeText(RectTransform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.text = content;
        txt.alignment = align;
        txt.supportRichText = true;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    Text MakeChildText(Transform parent, string content, int fontSize, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.text = content;
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
    }

    #endregion
}
