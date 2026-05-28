using UnityEngine;
using UnityEditor;

/// <summary>
/// 상호작용 오브젝트 생성 에디터.
/// 메뉴: Tools > Dev Tools > Spawn > Interactable Creator
/// </summary>
public class InteractableCreator : EditorWindow
{
    string objName = "Interactable";
    InteractableObject.InteractType type = InteractableObject.InteractType.Generic;
    string promptText = "조사하기";
    float interactRange = 1.5f;
    bool oneShot = false;

    // Door
    string targetScene = "";
    string spawnPointId = "";

    // Note
    string noteContent = "";

    // Pickup
    string itemId = "";
    int itemCount = 1;

    // Container
    bool isStorage = false; // true=안전가옥 창고, false=루팅 상자
    int containerWidth = 4;
    int containerHeight = 5;
    string containerName = "상자";
    FurnitureData furnitureDataRef; // 창고용 FurnitureData SO

    // Visual
    Color spriteColor = Color.white;
    Sprite objSprite;
    Material spriteMaterial;
    float spriteScale = 1f;

    bool savePrefab = false;
    Vector2 scrollPos;

    // 프리셋
    int presetIndex = 0;
    string[] presetNames = {
        "커스텀", "탈출구", "루팅 상자", "NPC", "쪽지", "바닥 아이템", "침대", "작업대", "지도판",
        "창고 (범용)", "의료대", "조리대",
        "── 가구 ──", "냉장고", "서랍장", "무기거치대", "책장", "재료함", "금고"
    };

    [MenuItem("Tools/Dev Tools/Spawn/Interactable Creator")]
    static void Open()
    {
        var window = GetWindow<InteractableCreator>("Interactable Creator");
        window.minSize = new Vector2(380, 500);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("상호작용 오브젝트 생성기", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 프리셋
        EditorGUI.BeginChangeCheck();
        presetIndex = EditorGUILayout.Popup("프리셋", presetIndex, presetNames);
        if (EditorGUI.EndChangeCheck() && presetIndex > 0)
            ApplyPreset(presetIndex);

        EditorGUILayout.Space(10);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        objName = EditorGUILayout.TextField("이름", objName);
        type = (InteractableObject.InteractType)EditorGUILayout.EnumPopup("타입", type);
        promptText = EditorGUILayout.TextField("프롬프트 텍스트", promptText);
        interactRange = EditorGUILayout.FloatField("상호작용 범위", interactRange);
        oneShot = EditorGUILayout.Toggle("일회용", oneShot);

        EditorGUILayout.Space(5);

        // 타입별 설정
        switch (type)
        {
            case InteractableObject.InteractType.ExitPoint:
                EditorGUILayout.LabelField("탈출구/진입구 설정", EditorStyles.boldLabel);
                targetScene = EditorGUILayout.TextField("목표 씬", targetScene);
                spawnPointId = EditorGUILayout.TextField("스폰 포인트 ID", spawnPointId);
                break;

            case InteractableObject.InteractType.Note:
                EditorGUILayout.LabelField("쪽지 설정", EditorStyles.boldLabel);
                noteContent = EditorGUILayout.TextArea(noteContent, GUILayout.Height(60));
                break;

            case InteractableObject.InteractType.Pickup:
                EditorGUILayout.LabelField("줍기 설정", EditorStyles.boldLabel);
                itemId = EditorGUILayout.TextField("아이템 ID", itemId);
                itemCount = EditorGUILayout.IntField("수량", itemCount);
                break;

            case InteractableObject.InteractType.Container:
                EditorGUILayout.LabelField("상자/창고 설정", EditorStyles.boldLabel);
                isStorage = EditorGUILayout.Toggle("안전가옥 가구 창고", isStorage);
                if (isStorage)
                {
                    furnitureDataRef = (FurnitureData)EditorGUILayout.ObjectField(
                        "가구 데이터", furnitureDataRef, typeof(FurnitureData), false);
                    if (furnitureDataRef != null)
                    {
                        EditorGUILayout.HelpBox(
                            $"[{furnitureDataRef.displayName}] {furnitureDataRef.gridWidth}x{furnitureDataRef.gridHeight}\n" +
                            $"허용: {furnitureDataRef.AllowedCategorySummary}\n" +
                            $"가격: {furnitureDataRef.buyPriceRudy} 루디",
                            MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("FurnitureData SO를 할당하세요.\n비어있으면 기본 범용 상자(4x4)로 생성됩니다.", MessageType.Warning);
                    }
                }
                else
                {
                    containerName = EditorGUILayout.TextField("상자 이름", containerName);
                    containerWidth = EditorGUILayout.IntField("격자 가로", containerWidth);
                    containerHeight = EditorGUILayout.IntField("격자 세로", containerHeight);
                }
                break;
        }

        EditorGUILayout.Space(5);

        // 비주얼
        EditorGUILayout.LabelField("비주얼", EditorStyles.boldLabel);
        objSprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", objSprite, typeof(Sprite), false);
        spriteMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", spriteMaterial, typeof(Material), false);
        spriteColor = EditorGUILayout.ColorField("스프라이트 색상", spriteColor);
        spriteScale = EditorGUILayout.FloatField("스프라이트 크기", spriteScale);

        EditorGUILayout.Space(5);
        savePrefab = EditorGUILayout.Toggle("프리팹으로 저장", savePrefab);

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("생성", GUILayout.Height(35)))
            CreateInteractable();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    void ApplyPreset(int index)
    {
        switch (index)
        {
            case 1: // 탈출구
                objName = "ExitPoint";
                type = InteractableObject.InteractType.ExitPoint;
                promptText = "탈출하기";
                interactRange = 2f;
                oneShot = false;
                break;
            case 2: // 루팅 상자
                objName = "LootBox";
                type = InteractableObject.InteractType.Container;
                promptText = "뒤지기";
                interactRange = 1.5f;
                oneShot = false;
                isStorage = false;
                containerName = "상자";
                containerWidth = 4;
                containerHeight = 5;
                break;
            case 3: // NPC
                objName = "NPC";
                type = InteractableObject.InteractType.NPC;
                promptText = "대화하기";
                interactRange = 2f;
                oneShot = false;
                break;
            case 4: // 쪽지
                objName = "Note";
                type = InteractableObject.InteractType.Note;
                promptText = "읽기";
                interactRange = 1.2f;
                oneShot = true;
                noteContent = "여기에 쪽지 내용 입력";
                break;
            case 5: // 바닥 아이템
                objName = "Item_Pickup";
                type = InteractableObject.InteractType.Pickup;
                promptText = "줍기";
                interactRange = 1.5f;
                oneShot = true;
                break;
            case 6: // 침대
                objName = "Bed";
                type = InteractableObject.InteractType.Bed;
                promptText = "쉬기";
                interactRange = 1.5f;
                oneShot = false;
                break;
            case 7: // 작업대
                objName = "Workbench";
                type = InteractableObject.InteractType.Workbench;
                promptText = "제작하기";
                interactRange = 1.5f;
                oneShot = false;
                break;
            case 8: // 지도판
                objName = "MapBoard";
                type = InteractableObject.InteractType.MapBoard;
                promptText = "출전 준비";
                interactRange = 2f;
                oneShot = false;
                break;
            case 9: // 창고
                objName = "Storage";
                type = InteractableObject.InteractType.Container;
                promptText = "창고 열기";
                interactRange = 2f;
                oneShot = false;
                isStorage = true;
                break;
            case 10: // 의료대
                objName = "MedicalBench";
                type = InteractableObject.InteractType.MedicalBench;
                promptText = "치료품 제작";
                interactRange = 1.6f;
                oneShot = false;
                break;
            case 11: // 조리대
                objName = "CookingBench";
                type = InteractableObject.InteractType.CookingBench;
                promptText = "요리하기";
                interactRange = 1.6f;
                oneShot = false;
                break;
            case 12: // ── 가구 ── (구분선, 무시)
                presetIndex = 0;
                break;
            case 13: // 냉장고
                ApplyFurniturePreset("Fridge", "냉장고 열기", "fridge");
                break;
            case 14: // 서랍장
                ApplyFurniturePreset("Drawer", "서랍 열기", "drawer");
                break;
            case 15: // 무기거치대
                ApplyFurniturePreset("WeaponRack", "무기 꺼내기", "weapon_rack");
                break;
            case 16: // 책장
                ApplyFurniturePreset("Bookshelf", "책장 열기", "bookshelf");
                break;
            case 17: // 재료함
                ApplyFurniturePreset("MaterialBin", "재료함 열기", "material_bin");
                break;
            case 18: // 금고
                ApplyFurniturePreset("Safe", "금고 열기", "safe");
                break;
        }
    }

    void ApplyFurniturePreset(string name, string prompt, string furnitureAssetName)
    {
        objName = name;
        type = InteractableObject.InteractType.Container;
        promptText = prompt;
        interactRange = 1.5f;
        oneShot = false;
        isStorage = true;

        // Resources/Data/Furniture/ 에서 SO 자동 로드
        string path = $"Assets/Resources/Data/Furniture/{furnitureAssetName}.asset";
        var loaded = AssetDatabase.LoadAssetAtPath<FurnitureData>(path);
        if (loaded != null)
            furnitureDataRef = loaded;
        else
            Debug.LogWarning($"FurnitureData 에셋 없음: {path}");
    }

    void CreateInteractable()
    {
        // 루트 오브젝트
        var root = new GameObject(objName);
        Undo.RegisterCreatedObjectUndo(root, "Create Interactable");

        // InteractableObject 컴포넌트
        var interactable = root.AddComponent<InteractableObject>();
        SetField(interactable, "type", type);
        SetField(interactable, "promptText", promptText);
        SetField(interactable, "interactRange", interactRange);
        SetField(interactable, "oneShot", oneShot);

        // 타입별 필드
        if (type == InteractableObject.InteractType.ExitPoint)
        {
            SetField(interactable, "targetScene", targetScene);
            SetField(interactable, "spawnPointId", spawnPointId);
        }
        else if (type == InteractableObject.InteractType.Note)
        {
            SetField(interactable, "noteContent", noteContent);
        }
        else if (type == InteractableObject.InteractType.Pickup)
        {
            SetField(interactable, "itemId", itemId);
            SetField(interactable, "itemCount", itemCount);
        }
        else if (type == InteractableObject.InteractType.Container)
        {
            if (isStorage)
            {
                var storage = root.AddComponent<SafehouseStorage>();
                if (furnitureDataRef != null)
                    SetField(storage, "furnitureData", furnitureDataRef);
            }
            else
            {
                var loot = root.AddComponent<LootContainer>();
                SetField(loot, "gridWidth", containerWidth);
                SetField(loot, "gridHeight", containerHeight);
                SetField(loot, "containerName", containerName);
            }
        }

        // IsometricDepthSorter
        root.AddComponent<IsometricDepthSorter>();

        // 비주얼 (Root → Sprite 구조)
        var visualRoot = new GameObject("Root");
        visualRoot.transform.SetParent(root.transform);
        visualRoot.transform.localPosition = new Vector3(0, 0.3f, 0);
        visualRoot.transform.localRotation = Quaternion.Euler(26f, 42f, 0f);

        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(visualRoot.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = Vector3.one * spriteScale;

        var sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.color = spriteColor;
        if (objSprite != null) sr.sprite = objSprite;
        if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;

        // 콜라이더 (상호작용 감지용은 아니지만 씬 선택 편의)
        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.3f, 0);
        col.size = new Vector3(0.5f, 0.6f, 0.5f);
        col.isTrigger = true;

        // 프리팹 저장
        if (savePrefab)
        {
            string folderPath = "Assets/Prefab/Interactable";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefab"))
                    AssetDatabase.CreateFolder("Assets", "Prefab");
                AssetDatabase.CreateFolder("Assets/Prefab", "Interactable");
            }

            string path = $"{folderPath}/{objName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                if (!EditorUtility.DisplayDialog("프리팹 덮어쓰기",
                    $"'{objName}.prefab'이 이미 존재합니다.\n덮어쓰시겠습니까?",
                    "덮어쓰기", "취소"))
                {
                    Selection.activeGameObject = root;
                    return;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"<color=green>프리팹 저장:</color> {path}");
        }

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"<color=cyan>상호작용 오브젝트 생성:</color> {objName} ({type}) | Range:{interactRange} | Prompt:\"{promptText}\"");
    }

    static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        if (field != null)
        {
            field.SetValue(target, value);
            if (target is Object unityObj)
                EditorUtility.SetDirty(unityObj);
        }
    }
}
