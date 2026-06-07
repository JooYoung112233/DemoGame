using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// NPC 저작 윈도우. (Tools ▸ TopDown ▸ Content ▸ NPC Maker)
///
/// NPCData(대사·이벤트 분기·선택지·조건·플래그·퀘스트·관계 초기값) + 연결된 ShopData를 한 창에서 편집하고,
/// 버튼 하나로 현재 씬에 NPC(InteractableObject+NPCController+스프라이트)를 배치한다.
///
/// 복잡한 배열(기본대화/이벤트대화/선택지/조건)은 SerializedProperty 기본 드로어로 펼쳐 편집 →
/// 필드 경로 오류 없이 전체 중첩 편집 가능. 위에 자산 생성/상점 연결/전당포 프리셋/씬 배치 편의 버튼을 얹음.
/// </summary>
public class NPCMakerWindow : EditorWindow
{
    [MenuItem("Tools/TopDown/콘텐츠/NPC 메이커")]
    static void Open()
    {
        var w = GetWindow<NPCMakerWindow>("NPC Maker");
        w.minSize = new Vector2(440, 560);
    }

    NPCData target;
    SerializedObject so;
    SerializedObject shopSo;
    Vector2 scroll;

    bool fBasic = true, fRel = false, fDefault = true, fEvent = true, fShop = true, fQuest = false;

    void OnGUI()
    {
        DrawAssetBar();
        if (target == null)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("편집할 NPCData를 선택하거나 '새 NPCData'로 만드세요.\n" +
                "NPC = NPCData(대사/상점/퀘스트/관계) + 씬의 InteractableObject(NPC)+NPCController.", MessageType.Info);
            return;
        }

        EnsureSO();
        so.Update();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSection(ref fBasic, "기본 정보", DrawBasic);
        DrawSection(ref fRel, "관계 초기값", DrawRelationship);
        DrawSection(ref fDefault, "기본 대화 (조건부 인사)", DrawDefaultDialogues);
        DrawSection(ref fEvent, "이벤트 대화 (분기/선택지)", DrawEventDialogues);
        DrawSection(ref fShop, "상점 (거래)", DrawShop);
        DrawSection(ref fQuest, "제공 퀘스트", DrawQuests);

        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties();

        DrawFooter();
    }

    // ── 자산 바 ──────────────────────────────────────────────────────
    void DrawAssetBar()
    {
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            var picked = (NPCData)EditorGUILayout.ObjectField("NPCData", target, typeof(NPCData), false);
            if (picked != target) { target = picked; so = null; shopSo = null; }
            if (GUILayout.Button("새 NPCData", GUILayout.Width(96))) CreateNewNPCData();
        }
    }

    void EnsureSO()
    {
        if (so == null || so.targetObject != target)
            so = new SerializedObject(target);
    }

    // ── 섹션 헬퍼 ────────────────────────────────────────────────────
    void DrawSection(ref bool open, string title, System.Action body)
    {
        EditorGUILayout.Space(2);
        open = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
        if (!open) return;
        EditorGUI.indentLevel++;
        body();
        EditorGUI.indentLevel--;
    }

    void Prop(string path, string label = null)
    {
        var p = so.FindProperty(path);
        if (p == null) { EditorGUILayout.LabelField($"(필드 없음: {path})"); return; }
        if (label != null) EditorGUILayout.PropertyField(p, new GUIContent(label), true);
        else EditorGUILayout.PropertyField(p, true);
    }

    // ── 섹션 본문 ────────────────────────────────────────────────────
    void DrawBasic()
    {
        Prop("npcId", "ID (예: pawnshop_owner)");
        Prop("displayName", "이름");
        Prop("role", "역할 (예: 전당포)");
    }

    void DrawRelationship()
    {
        EditorGUILayout.HelpBox("첫 대면 시 1회 시드. 이후엔 대화 선택지의 affinity/trust/fearChange로 변동.", MessageType.None);
        Prop("initialAffinity", "호감(Affinity)");
        Prop("initialTrust", "신뢰(Trust)");
        Prop("initialFear", "공포(Fear)");
    }

    void DrawDefaultDialogues()
    {
        EditorGUILayout.HelpBox("조건(친밀/신뢰/플래그/퀘스트)을 만족하는 항목 중 priority가 높은 인사를 출력.", MessageType.None);
        Prop("defaultDialogues");
    }

    void DrawEventDialogues()
    {
        EditorGUILayout.HelpBox("선택지(choices)마다 결과대사·관계변동·setFlag·triggerQuest·openShop을 설정. oneShot=1회성.", MessageType.None);
        Prop("eventDialogues");
    }

    void DrawShop()
    {
        var shopProp = so.FindProperty("shopData");
        EditorGUILayout.PropertyField(shopProp, new GUIContent("ShopData"));

        if (shopProp.objectReferenceValue == null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("새 ShopData 생성·연결")) CreateNewShopData();
                if (GUILayout.Button("전당포 프리셋 만들기")) MakePawnshopPreset();
            }
            EditorGUILayout.HelpBox("거래를 열려면 ShopData를 연결하고, 이벤트 대화 선택지에서 openShop을 켜세요.", MessageType.Info);
            return;
        }

        var shop = shopProp.objectReferenceValue as ShopData;
        if (shopSo == null || shopSo.targetObject != shop) shopSo = new SerializedObject(shop);
        shopSo.Update();
        EditorGUILayout.LabelField("─ 연결된 ShopData ─", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(shopSo.FindProperty("shopId"));
        EditorGUILayout.PropertyField(shopSo.FindProperty("shopName"));
        EditorGUILayout.PropertyField(shopSo.FindProperty("buyRate"), new GUIContent("매입 배율(buyRate)"));
        EditorGUILayout.PropertyField(shopSo.FindProperty("sellRate"), new GUIContent("매도 배율(sellRate)"));
        EditorGUILayout.PropertyField(shopSo.FindProperty("stock"), new GUIContent("판매 목록(stock)"), true);
        shopSo.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("stock에는 Resources/Items의 ItemData를 드래그. 매도 배율은 전당포일수록 낮게(0.6 등).", MessageType.None);

        if (GUILayout.Button("openShop 선택지 이벤트대화 추가('거래하기')")) AddOpenShopEventDialogue();
    }

    void DrawQuests()
    {
        Prop("availableQuests");
    }

    // ── 푸터 (씬 배치) ───────────────────────────────────────────────
    void DrawFooter()
    {
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            if (GUILayout.Button("현재 씬에 NPC 배치", GUILayout.Height(28))) PlaceInScene();
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("에셋 선택", GUILayout.Width(80), GUILayout.Height(28)))
                EditorGUIUtility.PingObject(target);
        }
    }

    // ── 자산 생성 ────────────────────────────────────────────────────
    void CreateNewNPCData()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 NPCData", "NewNPC", "asset",
            "NPCData 에셋을 저장할 위치를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;
        var asset = ScriptableObject.CreateInstance<NPCData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        target = asset; so = null; shopSo = null;
        EditorGUIUtility.PingObject(asset);
    }

    ShopData CreateNewShopData()
    {
        string defName = "Shop_" + (string.IsNullOrEmpty(target.npcId) ? "npc" : target.npcId);
        string path = EditorUtility.SaveFilePanelInProject("새 ShopData", defName, "asset",
            "ShopData 에셋을 저장할 위치를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return null;
        var shop = ScriptableObject.CreateInstance<ShopData>();
        AssetDatabase.CreateAsset(shop, path);
        AssetDatabase.SaveAssets();
        so.FindProperty("shopData").objectReferenceValue = shop;
        so.ApplyModifiedProperties();
        shopSo = null;
        return shop;
    }

    /// <summary>전당포 한 방 세팅: ShopData 생성·연결 + 역할/배율 + openShop 선택지 추가.</summary>
    void MakePawnshopPreset()
    {
        var shop = CreateNewShopData();
        if (shop == null) return;
        var sso = new SerializedObject(shop);
        sso.FindProperty("shopId").stringValue = string.IsNullOrEmpty(target.npcId) ? "pawnshop" : target.npcId;
        sso.FindProperty("shopName").stringValue = "전당포";
        sso.FindProperty("buyRate").floatValue = 1f;
        sso.FindProperty("sellRate").floatValue = 0.6f;
        sso.ApplyModifiedProperties();

        if (string.IsNullOrEmpty(target.role)) so.FindProperty("role").stringValue = "전당포";
        so.ApplyModifiedProperties();
        AddOpenShopEventDialogue();
    }

    /// <summary>eventDialogues에 openShop=true 선택지를 가진 '거래하기' 항목을 추가.</summary>
    void AddOpenShopEventDialogue()
    {
        var ed = so.FindProperty("eventDialogues");
        int i = ed.arraySize;
        ed.arraySize = i + 1;
        var entry = ed.GetArrayElementAtIndex(i);
        entry.FindPropertyRelative("id").stringValue = "trade";
        entry.FindPropertyRelative("oneShot").boolValue = false;

        var npcLines = entry.FindPropertyRelative("npcLines");
        npcLines.arraySize = 1;
        npcLines.GetArrayElementAtIndex(0).stringValue = "팔 거라도 있나?";

        var choices = entry.FindPropertyRelative("choices");
        choices.arraySize = 1;
        var c = choices.GetArrayElementAtIndex(0);
        c.FindPropertyRelative("text").stringValue = "거래하기";
        c.FindPropertyRelative("openShop").boolValue = true;
        c.FindPropertyRelative("affinityChange").intValue = 0;
        c.FindPropertyRelative("trustChange").intValue = 0;
        c.FindPropertyRelative("fearChange").intValue = 0;

        so.ApplyModifiedProperties();
        fEvent = true;
    }

    // ── 씬 배치 ──────────────────────────────────────────────────────
    void PlaceInScene()
    {
        string goName = !string.IsNullOrEmpty(target.displayName) ? target.displayName
            : (!string.IsNullOrEmpty(target.npcId) ? target.npcId : "NPC");
        var go = new GameObject(goName);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"); // 플레이스홀더 비주얼
        sr.color = new Color(0.55f, 0.8f, 1f);
        sr.sortingOrder = 5;

        // NPCController는 [RequireComponent(InteractableObject)] → 함께 추가됨
        var npc = go.AddComponent<NPCController>();
        var io = go.GetComponent<InteractableObject>();

        var ioSo = new SerializedObject(io);
        var t = ioSo.FindProperty("type");           if (t != null) t.enumValueIndex = (int)InteractableObject.InteractType.NPC;
        var pr = ioSo.FindProperty("promptText");     if (pr != null) pr.stringValue = "대화: " + goName;
        var rg = ioSo.FindProperty("interactRange");  if (rg != null) rg.floatValue = 2f;
        ioSo.ApplyModifiedProperties();

        var npcSo = new SerializedObject(npc);
        var dataProp = npcSo.FindProperty("npcData");
        if (dataProp != null) dataProp.objectReferenceValue = target;
        npcSo.ApplyModifiedProperties();

        go.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(go, "Place NPC");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorGUIUtility.PingObject(go);
        Debug.Log($"[NPC Maker] '{goName}' 배치 완료 (InteractableObject=NPC + NPCController, npcData 연결). 위치/스프라이트는 조정하세요.");
    }
}
