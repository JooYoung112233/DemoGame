using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 통합 스폰 오브젝트 생성 에디터.
/// Enemy Dummy / Interactable / Prop / NPC 4탭 통합.
/// 메뉴: Tools > Dev Tools > Spawn > Spawn Creator
/// </summary>
public class SpawnCreator : EditorWindow
{
    // 탭
    int tab; // 0=Enemy, 1=Interactable, 2=Prop, 3=NPC, 4=Building
    static readonly string[] TAB_NAMES = { "Enemy", "Interactable", "Prop", "NPC", "Building" };
    Vector2 scrollPos;

    // ================================================================
    //  Enemy Dummy 필드
    // ================================================================
    string enemy_name = "EnemyDummy";
    Color enemy_bodyColor = Color.white;
    float enemy_maxHp = 60f;
    float enemy_attackDamage = 15f;
    float enemy_attackRange = 1.5f;
    float enemy_attackSpeed = 0.67f;
    float enemy_moveSpeed = 2.5f;
    float enemy_patrolSpeed = 1.2f;
    float enemy_detectRange = 8f;
    float enemy_loseRange = 12f;
    float enemy_patrolRadius = 5f;
    float enemy_patrolWaitTime = 2f;
    float enemy_attackWindup = 0.8f;
    float enemy_maxGroggy = 100f;
    float enemy_groggyDecay = 8f;
    float enemy_groggyStunDuration = 2f;
    float enemy_flashDuration = 0.12f;
    float enemy_punchScale = 1.25f;
    float enemy_punchDuration = 0.15f;
    float enemy_knockbackDist = 0.15f;
    float enemy_knockbackDuration = 0.1f;
    float enemy_freezeDuration = 0.05f;
    float enemy_lightLungeDist = 0.3f;
    float enemy_lightLungeDuration = 0.08f;
    float enemy_lightReturnDuration = 0.12f;
    float enemy_heavyLungeDist = 0.6f;
    float enemy_heavyLungeDuration = 0.06f;
    float enemy_heavyReturnDuration = 0.18f;
    float enemy_ringRadius = 0.6f;
    float enemy_healthBarY = 0.85f;
    bool enemy_addOcclusionOutline = true;
    Color enemy_occlusionOutlineColor = new Color(1f, 0.3f, 0.2f, 0.7f);
    float enemy_occlusionOutlineWidth = 2f;
    int enemy_presetIndex = 0;
    string[] enemy_presetNames = { "Custom", "Bandit (Weak)", "Bandit (Strong)", "Monster", "Wanderer" };
    string enemy_unitKey = "";
    Sprite enemy_sprite;
    Material enemy_spriteMaterial;
    bool enemy_savePrefab = true;
    bool enemy_showHitFeedback;
    bool enemy_showAttackLunge;

    // ================================================================
    //  Interactable 필드
    // ================================================================
    string inter_name = "Interactable";
    InteractableObject.InteractType inter_type = InteractableObject.InteractType.Generic;
    string inter_promptText = "조사하기";
    float inter_interactRange = 1.5f;
    bool inter_oneShot = false;
    string inter_targetScene = "";
    string inter_spawnPointId = "";
    string inter_noteContent = "";
    string inter_itemId = "";
    int inter_itemCount = 1;
    bool inter_isStorage = false;
    int inter_containerWidth = 4;
    int inter_containerHeight = 5;
    string inter_containerName = "상자";
    FurnitureData inter_furnitureDataRef;
    DoorController.LockType inter_doorLockType = DoorController.LockType.Key;
    string inter_doorKeyId = "";
    bool inter_doorConsumeKey = true;
    string inter_doorQuestId = "";
    Color inter_spriteColor = Color.white;
    Sprite inter_sprite;
    Material inter_spriteMaterial;
    float inter_spriteScale = 1f;
    bool inter_savePrefab = false;
    int inter_presetIndex = 0;
    string[] inter_presetNames = {
        "Custom", "Exit", "Loot Box", "NPC", "Note", "Pickup", "Bed", "Workbench", "Map Board",
        "Storage", "Medical Bench", "Cooking Bench",
        "-- Door --", "Locked (Key)", "Locked (Quest)", "Open Door", "Switch Door",
        "-- Furniture --", "Fridge", "Drawer", "Weapon Rack", "Bookshelf", "Material Bin", "Safe"
    };

    // ================================================================
    //  Prop 필드
    // ================================================================
    string prop_name = "Prop";
    Texture2D prop_texture;
    Material prop_materialOverride;
    Color prop_tint = Color.white;
    float prop_brightness = 1f;
    float prop_lightBoost = 2f;
    float prop_alphaCutoff = 0.5f;
    bool prop_magentaClip = true;
    float prop_magentaThreshold = 0.1f;
    bool prop_useNormalMap = false;
    Texture2D prop_normalMap;
    float prop_normalStrength = 1f;
    float prop_tileScaleX = 1f;
    float prop_tileScaleY = 1f;
    bool prop_billboard = true;
    float prop_scale = 1f;
    float prop_yOffset = 0.5f;
    bool prop_addCollider = false;
    enum PropColliderType { Box, Capsule }
    PropColliderType prop_colliderType = PropColliderType.Box;
    int prop_sortingOrder = 0;
    bool prop_savePrefab = false;
    bool prop_saveMaterial = true;

    // ================================================================
    //  NPC 필드
    // ================================================================
    string npc_id = "";
    string npc_displayName = "";
    string npc_role = "";
    int npc_dialogueCount = 1;
    string[] npc_defaultLines = new string[1] { "" };
    bool npc_hasShop = false;
    bool npc_registerToCatalog = true;
    bool npc_createSceneObject = true;
    Sprite npc_sprite;
    Material npc_spriteMaterial;
    Color npc_spriteColor = Color.white;
    float npc_spriteScale = 1f;
    float npc_interactRange = 2f;
    bool npc_savePrefab = false;

    // ================================================================
    //  Building 필드
    // ================================================================
    string bld_name = "Building";
    Texture2D bld_texture;
    Texture2D bld_glowMask;
    Texture2D bld_heightFadeMask;
    Material bld_materialOverride;
    Color bld_tint = Color.white;
    float bld_brightness = 1f;
    float bld_lightBoost = 2f;
    float bld_alphaCutoff = 0.5f;
    bool bld_magentaClip = true;
    float bld_magentaThreshold = 0.1f;
    bool bld_useNormalMap = false;
    Texture2D bld_normalMap;
    float bld_normalStrength = 1f;
    float bld_tileScaleX = 1f;
    float bld_tileScaleY = 1f;
    // Glow
    bool bld_glowEnabled = true;
    Color bld_glowColor = new Color(1f, 0.85f, 0.5f, 1f);
    float bld_glowIntensity = 1.5f;
    // Height Fade
    bool bld_heightFadeEnabled = true;
    Color bld_heightFadeColor = new Color(0.02f, 0.02f, 0.04f, 1f);
    float bld_heightFadeAmount = 0.6f;
    // Weathering
    bool bld_weatherEnabled = false;
    float bld_dirtAmount = 0.3f;
    float bld_moistureBottom = 0.3f;
    // Geometry
    float bld_scale = 3f;
    float bld_yOffset = 0f;
    bool bld_billboard = false;
    bool bld_addCollider = true;
    Vector2Int bld_footprint = new(2, 2);
    bool bld_isEnterable = false;
    bool bld_savePrefab = true;
    bool bld_saveMaterial = true;
    bool bld_registerToCatalog = true;

    // ================================================================

    [MenuItem("Tools/Dev Tools/Spawn/Spawn Creator")]
    static void Open()
    {
        var window = GetWindow<SpawnCreator>("Spawn Creator");
        window.minSize = new Vector2(420, 650);
    }

    void OnGUI()
    {
        // 탭 바
        tab = GUILayout.Toolbar(tab, TAB_NAMES, GUILayout.Height(30));
        EditorGUILayout.Space(4);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (tab)
        {
            case 0: DrawEnemyTab(); break;
            case 1: DrawInteractableTab(); break;
            case 2: DrawPropTab(); break;
            case 3: DrawNPCTab(); break;
            case 4: DrawBuildingTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    //  TAB 0: Enemy Dummy
    // ================================================================

    void DrawEnemyTab()
    {
        EditorGUILayout.LabelField("Enemy Dummy Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bandit_Weak 1.prefab 구조 기준으로 적 더미 생성", MessageType.None);
        EditorGUILayout.Space(5);

        // 프리셋
        EditorGUI.BeginChangeCheck();
        enemy_presetIndex = EditorGUILayout.Popup("프리셋", enemy_presetIndex, enemy_presetNames);
        if (EditorGUI.EndChangeCheck() && enemy_presetIndex > 0)
            ApplyEnemyPreset(enemy_presetIndex);

        EditorGUILayout.Space(10);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        enemy_name = EditorGUILayout.TextField("이름", enemy_name);
        enemy_bodyColor = EditorGUILayout.ColorField("스프라이트 색상", enemy_bodyColor);
        enemy_sprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", enemy_sprite, typeof(Sprite), false);
        enemy_spriteMaterial = (Material)EditorGUILayout.ObjectField("스프라이트 머티리얼", enemy_spriteMaterial, typeof(Material), false);

        EditorGUILayout.Space(10);

        // 전투 스탯
        EditorGUILayout.LabelField("전투 스탯", EditorStyles.boldLabel);
        enemy_maxHp = EditorGUILayout.FloatField("최대 HP", enemy_maxHp);
        enemy_attackDamage = EditorGUILayout.FloatField("공격력", enemy_attackDamage);
        enemy_attackRange = EditorGUILayout.FloatField("공격 사거리", enemy_attackRange);
        enemy_attackSpeed = EditorGUILayout.FloatField("공격 속도 (초당)", enemy_attackSpeed);
        enemy_attackWindup = EditorGUILayout.FloatField("예비동작 시간", enemy_attackWindup);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("이동 / 감지", EditorStyles.boldLabel);
        enemy_moveSpeed = EditorGUILayout.FloatField("이동 속도", enemy_moveSpeed);
        enemy_patrolSpeed = EditorGUILayout.FloatField("순찰 속도", enemy_patrolSpeed);
        enemy_detectRange = EditorGUILayout.FloatField("감지 범위", enemy_detectRange);
        enemy_loseRange = EditorGUILayout.FloatField("추격 포기 범위", enemy_loseRange);
        enemy_patrolRadius = EditorGUILayout.FloatField("순찰 반경", enemy_patrolRadius);
        enemy_patrolWaitTime = EditorGUILayout.FloatField("순찰 대기 시간", enemy_patrolWaitTime);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("그로기", EditorStyles.boldLabel);
        enemy_maxGroggy = EditorGUILayout.FloatField("최대 그로기", enemy_maxGroggy);
        enemy_groggyDecay = EditorGUILayout.FloatField("그로기 감소/초", enemy_groggyDecay);
        enemy_groggyStunDuration = EditorGUILayout.FloatField("스턴 지속 시간", enemy_groggyStunDuration);

        EditorGUILayout.Space(5);
        enemy_showHitFeedback = EditorGUILayout.Foldout(enemy_showHitFeedback, "피격 피드백 (HitFeedback)");
        if (enemy_showHitFeedback)
        {
            EditorGUI.indentLevel++;
            enemy_flashDuration = EditorGUILayout.FloatField("플래시 시간", enemy_flashDuration);
            enemy_punchScale = EditorGUILayout.FloatField("스케일 펀치 배율", enemy_punchScale);
            enemy_punchDuration = EditorGUILayout.FloatField("펀치 시간", enemy_punchDuration);
            enemy_knockbackDist = EditorGUILayout.FloatField("넉백 거리", enemy_knockbackDist);
            enemy_knockbackDuration = EditorGUILayout.FloatField("넉백 시간", enemy_knockbackDuration);
            enemy_freezeDuration = EditorGUILayout.FloatField("히트스탑 시간", enemy_freezeDuration);
            EditorGUI.indentLevel--;
        }

        enemy_showAttackLunge = EditorGUILayout.Foldout(enemy_showAttackLunge, "공격 돌진 (AttackLunge)");
        if (enemy_showAttackLunge)
        {
            EditorGUI.indentLevel++;
            enemy_lightLungeDist = EditorGUILayout.FloatField("약공 돌진 거리", enemy_lightLungeDist);
            enemy_lightLungeDuration = EditorGUILayout.FloatField("약공 돌진 시간", enemy_lightLungeDuration);
            enemy_lightReturnDuration = EditorGUILayout.FloatField("약공 복귀 시간", enemy_lightReturnDuration);
            enemy_heavyLungeDist = EditorGUILayout.FloatField("강공 돌진 거리", enemy_heavyLungeDist);
            enemy_heavyLungeDuration = EditorGUILayout.FloatField("강공 돌진 시간", enemy_heavyLungeDuration);
            enemy_heavyReturnDuration = EditorGUILayout.FloatField("강공 복귀 시간", enemy_heavyReturnDuration);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("기타", EditorStyles.boldLabel);
        enemy_ringRadius = EditorGUILayout.FloatField("타겟 링 반경", enemy_ringRadius);
        enemy_healthBarY = EditorGUILayout.FloatField("HP바 Y 위치", enemy_healthBarY);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("벽 뒤 아웃라인", EditorStyles.boldLabel);
        enemy_addOcclusionOutline = EditorGUILayout.Toggle("아웃라인 추가", enemy_addOcclusionOutline);
        if (enemy_addOcclusionOutline)
        {
            enemy_occlusionOutlineColor = EditorGUILayout.ColorField("아웃라인 색", enemy_occlusionOutlineColor);
            enemy_occlusionOutlineWidth = EditorGUILayout.Slider("아웃라인 두께", enemy_occlusionOutlineWidth, 0.5f, 6f);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("데이터 참조 (선택)", EditorStyles.boldLabel);
        enemy_unitKey = EditorGUILayout.TextField("StatDB Unit Key", enemy_unitKey);

        if (!string.IsNullOrEmpty(enemy_unitKey) && GUILayout.Button("Load from StatDB"))
            LoadEnemyFromStatDB();

        enemy_savePrefab = EditorGUILayout.Toggle("프리팹으로 저장", enemy_savePrefab);

        EditorGUILayout.Space(15);
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("적 더미 생성", GUILayout.Height(40)))
            CreateEnemyDummy();
        GUI.backgroundColor = Color.white;
    }

    // ================================================================
    //  TAB 1: Interactable
    // ================================================================

    void DrawInteractableTab()
    {
        EditorGUILayout.LabelField("Interactable Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 프리셋
        EditorGUI.BeginChangeCheck();
        inter_presetIndex = EditorGUILayout.Popup("프리셋", inter_presetIndex, inter_presetNames);
        if (EditorGUI.EndChangeCheck() && inter_presetIndex > 0)
            ApplyInteractablePreset(inter_presetIndex);

        EditorGUILayout.Space(10);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        inter_name = EditorGUILayout.TextField("이름", inter_name);
        inter_type = (InteractableObject.InteractType)EditorGUILayout.EnumPopup("타입", inter_type);
        inter_promptText = EditorGUILayout.TextField("프롬프트 텍스트", inter_promptText);
        inter_interactRange = EditorGUILayout.FloatField("상호작용 범위", inter_interactRange);
        inter_oneShot = EditorGUILayout.Toggle("일회용", inter_oneShot);

        EditorGUILayout.Space(5);

        // 타입별 설정
        switch (inter_type)
        {
            case InteractableObject.InteractType.ExitPoint:
                EditorGUILayout.LabelField("탈출구/진입구 설정", EditorStyles.boldLabel);
                inter_targetScene = EditorGUILayout.TextField("목표 씬", inter_targetScene);
                inter_spawnPointId = EditorGUILayout.TextField("스폰 포인트 ID", inter_spawnPointId);
                break;

            case InteractableObject.InteractType.Note:
                EditorGUILayout.LabelField("쪽지 설정", EditorStyles.boldLabel);
                inter_noteContent = EditorGUILayout.TextArea(inter_noteContent, GUILayout.Height(60));
                break;

            case InteractableObject.InteractType.Pickup:
                EditorGUILayout.LabelField("줍기 설정", EditorStyles.boldLabel);
                inter_itemId = EditorGUILayout.TextField("아이템 ID", inter_itemId);
                inter_itemCount = EditorGUILayout.IntField("수량", inter_itemCount);
                break;

            case InteractableObject.InteractType.Door:
                EditorGUILayout.LabelField("문 설정", EditorStyles.boldLabel);
                inter_doorLockType = (DoorController.LockType)EditorGUILayout.EnumPopup("잠금 타입", inter_doorLockType);
                switch (inter_doorLockType)
                {
                    case DoorController.LockType.Key:
                        inter_doorKeyId = EditorGUILayout.TextField("필요 열쇠 ID", inter_doorKeyId);
                        inter_doorConsumeKey = EditorGUILayout.Toggle("열쇠 소모", inter_doorConsumeKey);
                        break;
                    case DoorController.LockType.Quest:
                        inter_doorQuestId = EditorGUILayout.TextField("필요 퀘스트 ID", inter_doorQuestId);
                        break;
                    case DoorController.LockType.Switch:
                        EditorGUILayout.HelpBox("외부 스위치/레버에서 Unlock() 호출로 열립니다.", MessageType.Info);
                        break;
                    case DoorController.LockType.None:
                        EditorGUILayout.HelpBox("잠금 없음 - 바로 열립니다.", MessageType.Info);
                        break;
                }
                break;

            case InteractableObject.InteractType.Container:
                EditorGUILayout.LabelField("상자/창고 설정", EditorStyles.boldLabel);
                inter_isStorage = EditorGUILayout.Toggle("안전가옥 가구 창고", inter_isStorage);
                if (inter_isStorage)
                {
                    inter_furnitureDataRef = (FurnitureData)EditorGUILayout.ObjectField(
                        "가구 데이터", inter_furnitureDataRef, typeof(FurnitureData), false);
                    if (inter_furnitureDataRef != null)
                    {
                        EditorGUILayout.HelpBox(
                            $"[{inter_furnitureDataRef.displayName}] {inter_furnitureDataRef.gridWidth}x{inter_furnitureDataRef.gridHeight}\n" +
                            $"허용: {inter_furnitureDataRef.AllowedCategorySummary}\n" +
                            $"가격: {inter_furnitureDataRef.buyPriceRudy} 루디",
                            MessageType.Info);
                    }
                }
                else
                {
                    inter_containerName = EditorGUILayout.TextField("상자 이름", inter_containerName);
                    inter_containerWidth = EditorGUILayout.IntField("격자 가로", inter_containerWidth);
                    inter_containerHeight = EditorGUILayout.IntField("격자 세로", inter_containerHeight);
                }
                break;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("비주얼", EditorStyles.boldLabel);
        inter_sprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", inter_sprite, typeof(Sprite), false);
        inter_spriteMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", inter_spriteMaterial, typeof(Material), false);
        inter_spriteColor = EditorGUILayout.ColorField("스프라이트 색상", inter_spriteColor);
        inter_spriteScale = EditorGUILayout.FloatField("스프라이트 크기", inter_spriteScale);

        EditorGUILayout.Space(5);
        inter_savePrefab = EditorGUILayout.Toggle("프리팹으로 저장", inter_savePrefab);

        EditorGUILayout.Space(15);
        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("생성", GUILayout.Height(35)))
            CreateInteractable();
        GUI.backgroundColor = Color.white;
    }

    // ================================================================
    //  TAB 2: Prop
    // ================================================================

    void DrawPropTab()
    {
        EditorGUILayout.LabelField("Prop Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Quad + InkCity/Prop 셰이더로 빠르게 프랍 생성", MessageType.None);
        EditorGUILayout.Space(5);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        prop_name = EditorGUILayout.TextField("이름", prop_name);

        EditorGUI.BeginChangeCheck();
        prop_texture = (Texture2D)EditorGUILayout.ObjectField("텍스쳐", prop_texture, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (prop_texture != null && prop_name == "Prop")
                prop_name = prop_texture.name;
        }

        prop_materialOverride = (Material)EditorGUILayout.ObjectField("머티리얼 직접 지정 (선택)", prop_materialOverride, typeof(Material), false);

        // 텍스쳐 미리보기
        if (prop_texture != null)
        {
            EditorGUILayout.Space(5);
            var rect = GUILayoutUtility.GetRect(120, 120, GUILayout.ExpandWidth(false));
            rect.x = (EditorGUIUtility.currentViewWidth - 120) * 0.5f;
            EditorGUI.DrawPreviewTexture(rect, prop_texture, null, ScaleMode.ScaleToFit);
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.Space(10);

        // 셰이더 설정
        if (prop_materialOverride == null)
        {
            EditorGUILayout.LabelField("셰이더 설정", EditorStyles.boldLabel);
            prop_tint = EditorGUILayout.ColorField("Tint", prop_tint);
            prop_brightness = EditorGUILayout.Slider("Brightness", prop_brightness, 0f, 2f);
            prop_lightBoost = EditorGUILayout.Slider("Light Boost", prop_lightBoost, 1f, 5f);
            prop_alphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", prop_alphaCutoff, 0f, 1f);

            EditorGUILayout.Space(5);
            prop_magentaClip = EditorGUILayout.Toggle("마젠타 -> 투명", prop_magentaClip);
            if (prop_magentaClip)
            {
                EditorGUI.indentLevel++;
                prop_magentaThreshold = EditorGUILayout.Slider("Threshold", prop_magentaThreshold, 0.01f, 0.5f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            prop_useNormalMap = EditorGUILayout.Toggle("노멀맵 사용", prop_useNormalMap);
            if (prop_useNormalMap)
            {
                EditorGUI.indentLevel++;
                prop_normalMap = (Texture2D)EditorGUILayout.ObjectField("Normal Map", prop_normalMap, typeof(Texture2D), false);
                prop_normalStrength = EditorGUILayout.Slider("Normal Strength", prop_normalStrength, 0f, 2f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            prop_tileScaleX = EditorGUILayout.Slider("Tile Scale X", prop_tileScaleX, 0.1f, 10f);
            prop_tileScaleY = EditorGUILayout.Slider("Tile Scale Y", prop_tileScaleY, 0.1f, 10f);
        }

        EditorGUILayout.Space(10);

        // 트랜스폼
        EditorGUILayout.LabelField("트랜스폼", EditorStyles.boldLabel);
        prop_billboard = EditorGUILayout.Toggle("빌보드 (90,0,0)", prop_billboard);
        prop_scale = EditorGUILayout.FloatField("스케일", prop_scale);
        prop_yOffset = EditorGUILayout.FloatField("Y 오프셋 (바닥 높이)", prop_yOffset);

        EditorGUILayout.Space(5);
        prop_addCollider = EditorGUILayout.Toggle("콜라이더 추가", prop_addCollider);
        if (prop_addCollider)
        {
            EditorGUI.indentLevel++;
            prop_colliderType = (PropColliderType)EditorGUILayout.EnumPopup("타입", prop_colliderType);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);
        prop_sortingOrder = EditorGUILayout.IntField("Sorting Order", prop_sortingOrder);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("저장", EditorStyles.boldLabel);
        prop_saveMaterial = EditorGUILayout.Toggle("머티리얼 에셋 저장", prop_saveMaterial);
        prop_savePrefab = EditorGUILayout.Toggle("프리팹으로 저장", prop_savePrefab);

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.9f);
        if (GUILayout.Button("프랍 생성", GUILayout.Height(40)))
            CreateProp();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);
        GUI.backgroundColor = new Color(0.6f, 0.8f, 0.5f);
        if (GUILayout.Button("선택된 텍스쳐들 -> 일괄 생성", GUILayout.Height(30)))
            BatchCreateProps();
        GUI.backgroundColor = Color.white;
    }

    // ================================================================
    //  TAB 3: NPC
    // ================================================================

    void DrawNPCTab()
    {
        EditorGUILayout.LabelField("NPC Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("NPCData SO 생성 + 씬 오브젝트 배치 + 카탈로그 등록", MessageType.None);
        EditorGUILayout.Space(5);

        // 기본 정보
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        npc_id = EditorGUILayout.TextField("NPC ID", npc_id);
        npc_displayName = EditorGUILayout.TextField("표시 이름", npc_displayName);
        npc_role = EditorGUILayout.TextField("역할 (상인, 정보원 등)", npc_role);

        EditorGUILayout.Space(10);

        // 대화
        EditorGUILayout.LabelField("기본 대화 (인사말)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"대사: {npc_dialogueCount}줄", GUILayout.Width(100));
        if (GUILayout.Button("+", GUILayout.Width(25))) npc_dialogueCount++;
        if (GUILayout.Button("-", GUILayout.Width(25)) && npc_dialogueCount > 0) npc_dialogueCount--;
        EditorGUILayout.EndHorizontal();

        if (npc_defaultLines.Length != npc_dialogueCount)
            System.Array.Resize(ref npc_defaultLines, npc_dialogueCount);

        for (int i = 0; i < npc_dialogueCount; i++)
        {
            if (npc_defaultLines[i] == null) npc_defaultLines[i] = "";
            npc_defaultLines[i] = EditorGUILayout.TextField($"  대사 {i + 1}", npc_defaultLines[i]);
        }

        EditorGUILayout.Space(5);
        npc_hasShop = EditorGUILayout.Toggle("상점 NPC", npc_hasShop);

        EditorGUILayout.Space(10);

        // 비주얼
        EditorGUILayout.LabelField("비주얼", EditorStyles.boldLabel);
        npc_sprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", npc_sprite, typeof(Sprite), false);
        npc_spriteMaterial = (Material)EditorGUILayout.ObjectField("스프라이트 머티리얼", npc_spriteMaterial, typeof(Material), false);
        npc_spriteColor = EditorGUILayout.ColorField("색상", npc_spriteColor);
        npc_spriteScale = EditorGUILayout.FloatField("스프라이트 크기", npc_spriteScale);
        npc_interactRange = EditorGUILayout.FloatField("상호작용 범위", npc_interactRange);

        EditorGUILayout.Space(10);

        // 옵션
        EditorGUILayout.LabelField("생성 옵션", EditorStyles.boldLabel);
        npc_createSceneObject = EditorGUILayout.Toggle("씬에 오브젝트 배치", npc_createSceneObject);
        npc_registerToCatalog = EditorGUILayout.Toggle("맵 빌더 카탈로그 등록", npc_registerToCatalog);
        npc_savePrefab = EditorGUILayout.Toggle("프리팹으로 저장", npc_savePrefab);

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "NPCData SO가 Resources/Data/NPC/ 에 생성됩니다.\n" +
            "이벤트 대화, 퀘스트, 상점 아이템은 Inspector에서 상세 편집하세요.",
            MessageType.Info);

        EditorGUILayout.Space(15);

        bool valid = !string.IsNullOrEmpty(npc_id);
        GUI.enabled = valid;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.9f);
        if (GUILayout.Button("NPC 생성", GUILayout.Height(40)))
            CreateNPC();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (!valid)
            EditorGUILayout.HelpBox("NPC ID를 입력하세요.", MessageType.Warning);
    }

    // ================================================================
    //  생성 로직: Enemy Dummy
    // ================================================================

    void ApplyEnemyPreset(int index)
    {
        switch (index)
        {
            case 1: enemy_name = "Bandit_Weak"; enemy_bodyColor = Color.white;
                enemy_maxHp = 40f; enemy_attackDamage = 10f; enemy_attackRange = 1.5f; enemy_attackSpeed = 0.8f;
                enemy_moveSpeed = 2.5f; enemy_patrolSpeed = 1f; enemy_detectRange = 7f; enemy_loseRange = 11f;
                enemy_patrolRadius = 4f; enemy_patrolWaitTime = 2f; enemy_attackWindup = 0.7f;
                enemy_maxGroggy = 80f; enemy_groggyDecay = 10f; enemy_groggyStunDuration = 2.5f; break;
            case 2: enemy_name = "Bandit_Strong"; enemy_bodyColor = Color.white;
                enemy_maxHp = 100f; enemy_attackDamage = 22f; enemy_attackRange = 1.8f; enemy_attackSpeed = 0.5f;
                enemy_moveSpeed = 2f; enemy_patrolSpeed = 0.8f; enemy_detectRange = 9f; enemy_loseRange = 13f;
                enemy_patrolRadius = 5f; enemy_patrolWaitTime = 2f; enemy_attackWindup = 1.0f;
                enemy_maxGroggy = 150f; enemy_groggyDecay = 6f; enemy_groggyStunDuration = 1.5f; break;
            case 3: enemy_name = "Monster"; enemy_bodyColor = Color.white;
                enemy_maxHp = 80f; enemy_attackDamage = 18f; enemy_attackRange = 2f; enemy_attackSpeed = 0.6f;
                enemy_moveSpeed = 3f; enemy_patrolSpeed = 1.5f; enemy_detectRange = 10f; enemy_loseRange = 14f;
                enemy_patrolRadius = 6f; enemy_patrolWaitTime = 2f; enemy_attackWindup = 0.6f;
                enemy_maxGroggy = 120f; enemy_groggyDecay = 5f; enemy_groggyStunDuration = 1.8f; break;
            case 4: enemy_name = "Wanderer_Day"; enemy_bodyColor = Color.white;
                enemy_maxHp = 25f; enemy_attackDamage = 5f; enemy_attackRange = 1.2f; enemy_attackSpeed = 0.5f;
                enemy_moveSpeed = 1.5f; enemy_patrolSpeed = 0.8f; enemy_detectRange = 5f; enemy_loseRange = 8f;
                enemy_patrolRadius = 3f; enemy_patrolWaitTime = 2f; enemy_attackWindup = 1.0f;
                enemy_maxGroggy = 50f; enemy_groggyDecay = 12f; enemy_groggyStunDuration = 3f; break;
        }
    }

    void LoadEnemyFromStatDB()
    {
        var db = AssetDatabase.LoadAssetAtPath<StatDB>("Assets/Resources/Data/StatDB.asset");
        if (db == null) return;
        var unit = db.GetUnit(enemy_unitKey);
        if (unit == null) { Debug.LogWarning($"[SpawnCreator] StatDB '{enemy_unitKey}' not found"); return; }
        enemy_name = unit.displayName;
        enemy_maxHp = unit.maxHp; enemy_attackDamage = unit.attackDamage;
        enemy_attackRange = unit.attackRange; enemy_attackSpeed = unit.attackSpeed;
        enemy_attackWindup = unit.attackWindup; enemy_moveSpeed = unit.moveSpeed;
        enemy_patrolSpeed = unit.patrolSpeed; enemy_detectRange = unit.detectRange;
        enemy_loseRange = unit.loseRange; enemy_patrolRadius = unit.patrolRadius;
        enemy_patrolWaitTime = unit.patrolWaitTime; enemy_maxGroggy = unit.maxGroggy;
        enemy_groggyDecay = unit.groggyDecay; enemy_groggyStunDuration = unit.groggyStunDuration;
        enemy_bodyColor = unit.tintColor;
        Debug.Log($"[SpawnCreator] StatDB '{enemy_unitKey}' loaded");
    }

    void CreateEnemyDummy()
    {
        var root = new GameObject(enemy_name);
        Undo.RegisterCreatedObjectUndo(root, "Create Enemy Dummy");

        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.radius = 0.25f; capsule.height = 0.8f; capsule.center = new Vector3(0, 0.4f, 0);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.speed = enemy_moveSpeed; agent.acceleration = 50f; agent.angularSpeed = 0f;
        agent.radius = 0.3f; agent.height = 0.8f; agent.stoppingDistance = 0.3f;

        var health = root.AddComponent<Health>();
        SetField(health, "maxHp", enemy_maxHp);

        var enemy = root.AddComponent<EnemyController>();
        SetField(enemy, "detectRange", enemy_detectRange); SetField(enemy, "attackRange", enemy_attackRange);
        SetField(enemy, "loseRange", enemy_loseRange); SetField(enemy, "moveSpeed", enemy_moveSpeed);
        SetField(enemy, "patrolSpeed", enemy_patrolSpeed); SetField(enemy, "patrolRadius", enemy_patrolRadius);
        SetField(enemy, "patrolWaitTime", enemy_patrolWaitTime); SetField(enemy, "attackDamage", enemy_attackDamage);
        SetField(enemy, "attackSpeed", enemy_attackSpeed); SetField(enemy, "attackWindup", enemy_attackWindup);
        SetField(enemy, "maxGroggy", enemy_maxGroggy); SetField(enemy, "groggyDecay", enemy_groggyDecay);
        SetField(enemy, "groggyStunDuration", enemy_groggyStunDuration);
        if (!string.IsNullOrEmpty(enemy_unitKey)) SetField(enemy, "unitKey", enemy_unitKey);

        root.AddComponent<EnemyOutline>();
        SetField(root.GetComponent<EnemyOutline>(), "ringRadius", enemy_ringRadius);

        if (enemy_addOcclusionOutline)
        {
            var wo = root.AddComponent<WallOcclusionOutline>();
            SetField(wo, "outlineColor", enemy_occlusionOutlineColor);
            SetField(wo, "outlineWidth", enemy_occlusionOutlineWidth);
            SetField(wo, "autoExcludeOwnLayer", true);
        }

        var cfb = root.AddComponent<CombatFeedback>();
        SetField(cfb, "flashDuration", enemy_flashDuration); SetField(cfb, "punchScale", enemy_punchScale);
        SetField(cfb, "punchDuration", enemy_punchDuration); SetField(cfb, "knockbackDist", enemy_knockbackDist);
        SetField(cfb, "knockbackDuration", enemy_knockbackDuration); SetField(cfb, "freezeDuration", enemy_freezeDuration);
        SetField(cfb, "lightLungeDist", enemy_lightLungeDist); SetField(cfb, "lightLungeDuration", enemy_lightLungeDuration);
        SetField(cfb, "lightReturnDuration", enemy_lightReturnDuration); SetField(cfb, "heavyLungeDist", enemy_heavyLungeDist);
        SetField(cfb, "heavyLungeDuration", enemy_heavyLungeDuration); SetField(cfb, "heavyReturnDuration", enemy_heavyReturnDuration);

        root.AddComponent<IsometricDepthSorter>();

        // Visual root
        var visualRoot = new GameObject("Root");
        visualRoot.transform.SetParent(root.transform);
        visualRoot.transform.localPosition = new Vector3(0, 0.4f, 0);
        visualRoot.transform.localRotation = Quaternion.Euler(35.264f, 45f, 0f);

        var spriteObj = new GameObject("EnemySprite");
        spriteObj.transform.SetParent(visualRoot.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        var sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.color = enemy_bodyColor;
        if (enemy_sprite != null) sr.sprite = enemy_sprite;
        else { var ps = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Player/Player.png"); if (ps) sr.sprite = ps; }
        if (enemy_spriteMaterial != null) sr.sharedMaterial = enemy_spriteMaterial;

        // Shadow
        var shadow = new GameObject("shadow");
        shadow.transform.SetParent(root.transform);
        shadow.transform.localPosition = new Vector3(0, 0.02f, 0);
        shadow.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        shadow.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        var ssr = shadow.AddComponent<SpriteRenderer>();
        ssr.color = new Color(0, 0, 0, 0.35f); ssr.sortingOrder = -10;
        if (enemy_spriteMaterial != null) ssr.sharedMaterial = enemy_spriteMaterial;

        if (enemy_savePrefab) SavePrefab(root, "Assets/Prefab", enemy_name);

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"<color=cyan>[SpawnCreator]</color> Enemy '{enemy_name}' created | HP:{enemy_maxHp} ATK:{enemy_attackDamage}");
    }

    // ================================================================
    //  생성 로직: Interactable
    // ================================================================

    void ApplyInteractablePreset(int index)
    {
        switch (index)
        {
            case 1: inter_name = "ExitPoint"; inter_type = InteractableObject.InteractType.ExitPoint; inter_promptText = "탈출하기"; inter_interactRange = 2f; inter_oneShot = false; break;
            case 2: inter_name = "LootBox"; inter_type = InteractableObject.InteractType.Container; inter_promptText = "뒤지기"; inter_interactRange = 1.5f; inter_isStorage = false; inter_containerName = "상자"; inter_containerWidth = 4; inter_containerHeight = 5; break;
            case 3: inter_name = "NPC"; inter_type = InteractableObject.InteractType.NPC; inter_promptText = "대화하기"; inter_interactRange = 2f; break;
            case 4: inter_name = "Note"; inter_type = InteractableObject.InteractType.Note; inter_promptText = "읽기"; inter_interactRange = 1.2f; inter_oneShot = true; inter_noteContent = "여기에 쪽지 내용 입력"; break;
            case 5: inter_name = "Item_Pickup"; inter_type = InteractableObject.InteractType.Pickup; inter_promptText = "줍기"; inter_interactRange = 1.5f; inter_oneShot = true; break;
            case 6: inter_name = "Bed"; inter_type = InteractableObject.InteractType.Bed; inter_promptText = "쉬기"; inter_interactRange = 1.5f; break;
            case 7: inter_name = "Workbench"; inter_type = InteractableObject.InteractType.Workbench; inter_promptText = "제작하기"; inter_interactRange = 1.5f; break;
            case 8: inter_name = "MapBoard"; inter_type = InteractableObject.InteractType.MapBoard; inter_promptText = "출전 준비"; inter_interactRange = 2f; break;
            case 9: inter_name = "Storage"; inter_type = InteractableObject.InteractType.Container; inter_promptText = "창고 열기"; inter_interactRange = 2f; inter_isStorage = true; break;
            case 10: inter_name = "MedicalBench"; inter_type = InteractableObject.InteractType.MedicalBench; inter_promptText = "치료품 제작"; inter_interactRange = 1.6f; break;
            case 11: inter_name = "CookingBench"; inter_type = InteractableObject.InteractType.CookingBench; inter_promptText = "요리하기"; inter_interactRange = 1.6f; break;
            case 12: inter_presetIndex = 0; break; // separator
            case 13: inter_name = "LockedDoor_Key"; inter_type = InteractableObject.InteractType.Door; inter_promptText = "문 열기"; inter_interactRange = 2f; inter_doorLockType = DoorController.LockType.Key; inter_doorConsumeKey = true; break;
            case 14: inter_name = "LockedDoor_Quest"; inter_type = InteractableObject.InteractType.Door; inter_promptText = "문 열기"; inter_interactRange = 2f; inter_doorLockType = DoorController.LockType.Quest; break;
            case 15: inter_name = "Door_Open"; inter_type = InteractableObject.InteractType.Door; inter_promptText = "문 열기"; inter_interactRange = 2f; inter_doorLockType = DoorController.LockType.None; break;
            case 16: inter_name = "LockedDoor_Switch"; inter_type = InteractableObject.InteractType.Door; inter_promptText = "문 열기"; inter_interactRange = 2f; inter_doorLockType = DoorController.LockType.Switch; break;
            case 17: inter_presetIndex = 0; break; // separator
            case 18: ApplyFurniturePreset("Fridge", "냉장고 열기", "fridge"); break;
            case 19: ApplyFurniturePreset("Drawer", "서랍 열기", "drawer"); break;
            case 20: ApplyFurniturePreset("WeaponRack", "무기 꺼내기", "weapon_rack"); break;
            case 21: ApplyFurniturePreset("Bookshelf", "책장 열기", "bookshelf"); break;
            case 22: ApplyFurniturePreset("MaterialBin", "재료함 열기", "material_bin"); break;
            case 23: ApplyFurniturePreset("Safe", "금고 열기", "safe"); break;
        }
    }

    void ApplyFurniturePreset(string name, string prompt, string furnitureAssetName)
    {
        inter_name = name; inter_type = InteractableObject.InteractType.Container;
        inter_promptText = prompt; inter_interactRange = 1.5f; inter_isStorage = true;
        var loaded = AssetDatabase.LoadAssetAtPath<FurnitureData>($"Assets/Resources/Data/Furniture/{furnitureAssetName}.asset");
        if (loaded != null) inter_furnitureDataRef = loaded;
    }

    void CreateInteractable()
    {
        var root = new GameObject(inter_name);
        Undo.RegisterCreatedObjectUndo(root, "Create Interactable");

        var interactable = root.AddComponent<InteractableObject>();
        SetField(interactable, "type", inter_type);
        SetField(interactable, "promptText", inter_promptText);
        SetField(interactable, "interactRange", inter_interactRange);
        SetField(interactable, "oneShot", inter_oneShot);

        if (inter_type == InteractableObject.InteractType.ExitPoint)
        { SetField(interactable, "targetScene", inter_targetScene); SetField(interactable, "spawnPointId", inter_spawnPointId); }
        else if (inter_type == InteractableObject.InteractType.Note)
        { SetField(interactable, "noteContent", inter_noteContent); }
        else if (inter_type == InteractableObject.InteractType.Pickup)
        { SetField(interactable, "itemId", inter_itemId); SetField(interactable, "itemCount", inter_itemCount); }
        else if (inter_type == InteractableObject.InteractType.Door)
        {
            var door = root.AddComponent<DoorController>();
            SetField(door, "lockType", inter_doorLockType);
            if (inter_doorLockType == DoorController.LockType.Key)
            { SetField(door, "requiredKeyId", inter_doorKeyId); SetField(door, "consumeKey", inter_doorConsumeKey); }
            else if (inter_doorLockType == DoorController.LockType.Quest)
            { SetField(door, "requiredQuestId", inter_doorQuestId); }
            var blocker = root.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0, 0.5f, 0); blocker.size = new Vector3(1f, 1f, 0.2f); blocker.isTrigger = false;
            SetField(door, "doorCollider", blocker);
        }
        else if (inter_type == InteractableObject.InteractType.Container)
        {
            if (inter_isStorage) { var s = root.AddComponent<SafehouseStorage>(); if (inter_furnitureDataRef) SetField(s, "furnitureData", inter_furnitureDataRef); }
            else { var lc = root.AddComponent<LootContainer>(); SetField(lc, "gridWidth", inter_containerWidth); SetField(lc, "gridHeight", inter_containerHeight); SetField(lc, "containerName", inter_containerName); }
        }

        root.AddComponent<IsometricDepthSorter>();

        // Visual
        var visualRoot = new GameObject("Root");
        visualRoot.transform.SetParent(root.transform);
        visualRoot.transform.localPosition = new Vector3(0, 0.3f, 0);
        visualRoot.transform.localRotation = Quaternion.Euler(35.264f, 45f, 0f);

        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(visualRoot.transform);
        spriteObj.transform.localScale = Vector3.one * inter_spriteScale;
        var sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.color = inter_spriteColor;
        if (inter_sprite) sr.sprite = inter_sprite;
        if (inter_spriteMaterial) sr.sharedMaterial = inter_spriteMaterial;

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.3f, 0); col.size = new Vector3(0.5f, 0.6f, 0.5f); col.isTrigger = true;

        if (inter_savePrefab) SavePrefab(root, "Assets/Prefab/Interactable", inter_name);

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"<color=cyan>[SpawnCreator]</color> Interactable '{inter_name}' ({inter_type}) created");
    }

    // ================================================================
    //  생성 로직: Prop
    // ================================================================

    void CreateProp()
    {
        if (prop_texture == null && prop_materialOverride == null)
        { EditorUtility.DisplayDialog("Error", "텍스쳐 또는 머티리얼을 지정하세요.", "OK"); return; }
        var root = CreatePropObject(prop_name, prop_texture);
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"<color=cyan>[SpawnCreator]</color> Prop '{prop_name}' created");
    }

    void BatchCreateProps()
    {
        int count = 0;
        foreach (var obj in Selection.objects)
        { if (obj is Texture2D tex) { CreatePropObject(tex.name, tex); count++; } }
        if (count == 0) EditorUtility.DisplayDialog("Info", "Project에서 텍스쳐를 선택 후 사용하세요.", "OK");
        else Debug.Log($"<color=cyan>[SpawnCreator]</color> {count} props batch created");
    }

    GameObject CreatePropObject(string name, Texture2D tex)
    {
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create Prop");

        if (SceneView.lastActiveSceneView != null)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            var pos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
            root.transform.position = new Vector3(pos.x, prop_yOffset, pos.z);
        }
        else root.transform.position = new Vector3(0, prop_yOffset, 0);

        var quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.name = "Visual";
        quadObj.transform.SetParent(root.transform);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = prop_billboard ? Quaternion.Euler(90f, 0, 0) : Quaternion.identity;

        float sx = prop_scale, sy = prop_scale;
        if (tex != null && tex.width > 0 && tex.height > 0)
        { float a = (float)tex.width / tex.height; if (a > 1f) sy = prop_scale / a; else sx = prop_scale * a; }
        quadObj.transform.localScale = new Vector3(sx, sy, 1f);

        var meshCol = quadObj.GetComponent<MeshCollider>();
        if (meshCol) Object.DestroyImmediate(meshCol);

        var renderer = quadObj.GetComponent<MeshRenderer>();
        Material mat;
        if (prop_materialOverride != null)
        { mat = new Material(prop_materialOverride); if (tex) mat.SetTexture("_MainTex", tex); }
        else
        {
            var shader = Shader.Find("InkCity/Prop") ?? Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            if (tex) mat.SetTexture("_MainTex", tex);
            mat.SetColor("_Color", prop_tint); mat.SetFloat("_Brightness", prop_brightness);
            mat.SetFloat("_LightBoost", prop_lightBoost); mat.SetFloat("_Cutoff", prop_alphaCutoff);
            mat.SetFloat("_TileScaleX", prop_tileScaleX); mat.SetFloat("_TileScaleY", prop_tileScaleY);
            if (prop_magentaClip) { mat.EnableKeyword("_MAGENTA_CLIP"); mat.SetFloat("_MagentaClip", 1f); }
            else { mat.DisableKeyword("_MAGENTA_CLIP"); mat.SetFloat("_MagentaClip", 0f); }
            mat.SetFloat("_MagentaThreshold", prop_magentaThreshold);
            if (prop_useNormalMap && prop_normalMap)
            { mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_NormalMapToggle", 1f); mat.SetTexture("_BumpMap", prop_normalMap); mat.SetFloat("_BumpScale", prop_normalStrength); }
        }
        mat.name = $"Mat_{name}";
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = prop_sortingOrder;

        if (prop_addCollider)
        {
            if (prop_colliderType == PropColliderType.Box)
            { var box = root.AddComponent<BoxCollider>(); box.size = new Vector3(sx * 0.8f, 0.5f, sy * 0.8f); box.center = new Vector3(0, 0.25f, 0); }
            else
            { var cap = root.AddComponent<CapsuleCollider>(); cap.radius = Mathf.Min(sx, sy) * 0.4f; cap.height = 0.5f; cap.center = new Vector3(0, 0.25f, 0); }
        }

        if (prop_saveMaterial && prop_materialOverride == null)
        {
            EnsureFolder("Assets/Materials/Props");
            string matPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Materials/Props/{mat.name}.mat");
            AssetDatabase.CreateAsset(mat, matPath);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        if (prop_savePrefab) SavePrefab(root, "Assets/Prefab/Props", name);

        return root;
    }

    // ================================================================
    //  생성 로직: NPC
    // ================================================================

    void CreateNPC()
    {
        // 1) NPCData SO 생성
        string npcFolder = "Assets/Resources/Data/NPC";
        EnsureFolder(npcFolder);

        var npcData = ScriptableObject.CreateInstance<NPCData>();
        npcData.npcId = npc_id;
        npcData.displayName = string.IsNullOrEmpty(npc_displayName) ? npc_id : npc_displayName;
        npcData.role = npc_role;

        // 기본 대화
        var dialogues = new List<DialogueEntry>();
        var validLines = new List<string>();
        foreach (var line in npc_defaultLines)
        {
            if (!string.IsNullOrEmpty(line)) validLines.Add(line);
        }
        if (validLines.Count > 0)
        {
            dialogues.Add(new DialogueEntry
            {
                id = "greeting",
                lines = validLines.ToArray(),
                priority = 0
            });
        }
        npcData.defaultDialogues = dialogues.ToArray();
        npcData.eventDialogues = new EventDialogue[0];
        npcData.availableQuests = new QuestData[0];
        npcData.shopInventory = npc_hasShop ? new ItemData[0] : null;

        string assetPath = $"{npcFolder}/{npc_id}.asset";
        if (AssetDatabase.LoadAssetAtPath<NPCData>(assetPath) != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기",
                $"'{npc_id}.asset' 이 이미 존재합니다.\n덮어쓰시겠습니까?", "덮어쓰기", "취소"))
                return;
        }
        AssetDatabase.CreateAsset(npcData, assetPath);
        Debug.Log($"<color=cyan>[SpawnCreator]</color> NPCData SO created: {assetPath}");

        // 2) 맵 빌더 카탈로그 등록
        if (npc_registerToCatalog)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<IsometricMapEditor.MapBuilderCatalog>(
                "Assets/Resources/MapBuilder/MapBuilderCatalog.asset");
            if (catalog != null)
            {
                var list = new List<NPCData>(catalog.npcs) { npcData };
                catalog.npcs = list.ToArray();
                EditorUtility.SetDirty(catalog);
                Debug.Log($"<color=green>[SpawnCreator]</color> NPC '{npc_id}' registered to MapBuilderCatalog");
            }
        }

        // 3) 씬 오브젝트 생성
        if (npc_createSceneObject)
        {
            var root = new GameObject($"NPC_{npc_id}");
            Undo.RegisterCreatedObjectUndo(root, "Create NPC");

            // InteractableObject
            var interactable = root.AddComponent<InteractableObject>();
            SetField(interactable, "type", InteractableObject.InteractType.NPC);
            SetField(interactable, "promptText", "대화하기");
            SetField(interactable, "interactRange", npc_interactRange);

            // NPCController
            var npcCtrl = root.AddComponent<NPCController>();
            SetField(npcCtrl, "npcData", npcData);

            // IsometricDepthSorter
            root.AddComponent<IsometricDepthSorter>();

            // Visual
            var visualRoot = new GameObject("Root");
            visualRoot.transform.SetParent(root.transform);
            visualRoot.transform.localPosition = new Vector3(0, 0.4f, 0);
            visualRoot.transform.localRotation = Quaternion.Euler(35.264f, 45f, 0f);

            var spriteObj = new GameObject("NPCSprite");
            spriteObj.transform.SetParent(visualRoot.transform);
            spriteObj.transform.localScale = Vector3.one * npc_spriteScale;

            var sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.color = npc_spriteColor;
            if (npc_sprite != null) sr.sprite = npc_sprite;
            else
            {
                var fallback = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Player/Player.png");
                if (fallback) sr.sprite = fallback;
            }
            if (npc_spriteMaterial) sr.sharedMaterial = npc_spriteMaterial;

            // Collider
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.25f; col.height = 0.8f; col.center = new Vector3(0, 0.4f, 0);

            // Shadow
            var shadow = new GameObject("shadow");
            shadow.transform.SetParent(root.transform);
            shadow.transform.localPosition = new Vector3(0, 0.02f, 0);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            shadow.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
            var ssr = shadow.AddComponent<SpriteRenderer>();
            ssr.color = new Color(0, 0, 0, 0.3f); ssr.sortingOrder = -10;

            // SceneView
            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                var pos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                root.transform.position = new Vector3(pos.x, 0, pos.z);
            }

            if (npc_savePrefab) SavePrefab(root, "Assets/Prefab/NPC", $"NPC_{npc_id}");

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log($"<color=cyan>[SpawnCreator]</color> NPC scene object created: NPC_{npc_id}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ================================================================
    //  TAB 4: Building
    // ================================================================

    void DrawBuildingTab()
    {
        EditorGUILayout.LabelField("Building Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Quad + InkCity/CityBuilding 셰이더로 건물 생성.\n프리팹 저장 후 MapBuilderCatalog에 등록.", MessageType.None);
        EditorGUILayout.Space(5);

        bld_name = EditorGUILayout.TextField("이름", bld_name);

        EditorGUI.BeginChangeCheck();
        bld_texture = (Texture2D)EditorGUILayout.ObjectField("텍스쳐", bld_texture, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (bld_texture != null && bld_name == "Building")
                bld_name = bld_texture.name;
        }

        bld_materialOverride = (Material)EditorGUILayout.ObjectField("머티리얼 직접 지정 (선택)", bld_materialOverride, typeof(Material), false);

        // 텍스쳐 미리보기
        if (bld_texture != null)
        {
            EditorGUILayout.Space(2);
            var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, bld_texture, null, ScaleMode.ScaleToFit);
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("셰이더 설정", EditorStyles.boldLabel);

        if (bld_materialOverride == null)
        {
            bld_tint = EditorGUILayout.ColorField("Tint", bld_tint);
            bld_brightness = EditorGUILayout.Slider("Brightness", bld_brightness, 0f, 2f);
            bld_lightBoost = EditorGUILayout.Slider("Light Boost", bld_lightBoost, 1f, 5f);
            bld_alphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", bld_alphaCutoff, 0f, 1f);

            EditorGUILayout.Space(2);
            bld_magentaClip = EditorGUILayout.Toggle("마젠타 -> 투명", bld_magentaClip);
            if (bld_magentaClip)
            {
                EditorGUI.indentLevel++;
                bld_magentaThreshold = EditorGUILayout.Slider("Threshold", bld_magentaThreshold, 0.01f, 0.5f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            bld_useNormalMap = EditorGUILayout.Toggle("노멀맵 사용", bld_useNormalMap);
            if (bld_useNormalMap)
            {
                EditorGUI.indentLevel++;
                bld_normalMap = (Texture2D)EditorGUILayout.ObjectField("Normal Map", bld_normalMap, typeof(Texture2D), false);
                bld_normalStrength = EditorGUILayout.Slider("Normal Strength", bld_normalStrength, 0f, 2f);
                EditorGUI.indentLevel--;
            }

            bld_tileScaleX = EditorGUILayout.Slider("Tile Scale X", bld_tileScaleX, 0.1f, 10f);
            bld_tileScaleY = EditorGUILayout.Slider("Tile Scale Y", bld_tileScaleY, 0.1f, 10f);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Window Glow", EditorStyles.boldLabel);
            bld_glowEnabled = EditorGUILayout.Toggle("Glow 사용", bld_glowEnabled);
            if (bld_glowEnabled)
            {
                EditorGUI.indentLevel++;
                bld_glowMask = (Texture2D)EditorGUILayout.ObjectField("Glow Mask", bld_glowMask, typeof(Texture2D), false);
                bld_glowColor = EditorGUILayout.ColorField("Glow Color", bld_glowColor);
                bld_glowIntensity = EditorGUILayout.Slider("Glow Intensity", bld_glowIntensity, 0f, 5f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Height Fade", EditorStyles.boldLabel);
            bld_heightFadeEnabled = EditorGUILayout.Toggle("Height Fade 사용", bld_heightFadeEnabled);
            if (bld_heightFadeEnabled)
            {
                EditorGUI.indentLevel++;
                bld_heightFadeMask = (Texture2D)EditorGUILayout.ObjectField("Fade Mask", bld_heightFadeMask, typeof(Texture2D), false);
                bld_heightFadeColor = EditorGUILayout.ColorField("Fade Color", bld_heightFadeColor);
                bld_heightFadeAmount = EditorGUILayout.Slider("Fade Amount", bld_heightFadeAmount, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Weathering", EditorStyles.boldLabel);
            bld_weatherEnabled = EditorGUILayout.Toggle("풍화 효과", bld_weatherEnabled);
            if (bld_weatherEnabled)
            {
                EditorGUI.indentLevel++;
                bld_dirtAmount = EditorGUILayout.Slider("Dirt Amount", bld_dirtAmount, 0f, 1f);
                bld_moistureBottom = EditorGUILayout.Slider("Moisture Bottom", bld_moistureBottom, 0f, 1f);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("배치 설정", EditorStyles.boldLabel);
        bld_scale = EditorGUILayout.FloatField("스케일", bld_scale);
        bld_yOffset = EditorGUILayout.FloatField("Y 오프셋", bld_yOffset);
        bld_billboard = EditorGUILayout.Toggle("빌보드 (Y축 눕히기)", bld_billboard);
        bld_addCollider = EditorGUILayout.Toggle("콜라이더 추가", bld_addCollider);
        bld_footprint = EditorGUILayout.Vector2IntField("풋프린트 (셀)", bld_footprint);
        bld_isEnterable = EditorGUILayout.Toggle("진입 가능", bld_isEnterable);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("저장 옵션", EditorStyles.boldLabel);
        bld_savePrefab = EditorGUILayout.Toggle("프리팹 저장", bld_savePrefab);
        bld_saveMaterial = EditorGUILayout.Toggle("머티리얼 저장", bld_saveMaterial);
        bld_registerToCatalog = EditorGUILayout.Toggle("카탈로그 등록", bld_registerToCatalog);

        EditorGUILayout.Space(10);
        bool valid = bld_texture != null || bld_materialOverride != null;
        GUI.enabled = valid;
        if (GUILayout.Button("건물 생성", GUILayout.Height(35)))
            CreateBuilding();
        GUI.enabled = true;
    }

    void CreateBuilding()
    {
        if (bld_texture == null && bld_materialOverride == null)
        { EditorUtility.DisplayDialog("Error", "텍스쳐 또는 머티리얼을 지정하세요.", "OK"); return; }

        var root = new GameObject(bld_name);
        Undo.RegisterCreatedObjectUndo(root, "Create Building");

        if (SceneView.lastActiveSceneView != null)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            var pos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
            root.transform.position = new Vector3(pos.x, bld_yOffset, pos.z);
        }
        else root.transform.position = new Vector3(0, bld_yOffset, 0);

        // Quad
        var quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.name = "Visual";
        quadObj.transform.SetParent(root.transform);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = bld_billboard ? Quaternion.Euler(90f, 0, 0) : Quaternion.identity;

        float sx = bld_scale, sy = bld_scale;
        if (bld_texture != null && bld_texture.width > 0 && bld_texture.height > 0)
        { float a = (float)bld_texture.width / bld_texture.height; if (a > 1f) sy = bld_scale / a; else sx = bld_scale * a; }
        quadObj.transform.localScale = new Vector3(sx, sy, 1f);

        var meshCol = quadObj.GetComponent<MeshCollider>();
        if (meshCol) Object.DestroyImmediate(meshCol);

        // Material (InkCity/CityBuilding)
        var renderer = quadObj.GetComponent<MeshRenderer>();
        Material mat;
        if (bld_materialOverride != null)
        {
            mat = new Material(bld_materialOverride);
            if (bld_texture) mat.SetTexture("_MainTex", bld_texture);
        }
        else
        {
            var shader = Shader.Find("InkCity/CityBuilding") ?? Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            if (bld_texture) mat.SetTexture("_MainTex", bld_texture);
            mat.SetColor("_Color", bld_tint);
            mat.SetFloat("_Brightness", bld_brightness);
            mat.SetFloat("_LightBoost", bld_lightBoost);
            mat.SetFloat("_Cutoff", bld_alphaCutoff);
            mat.SetFloat("_TileScaleX", bld_tileScaleX);
            mat.SetFloat("_TileScaleY", bld_tileScaleY);

            if (bld_magentaClip) { mat.EnableKeyword("_MAGENTA_CLIP"); mat.SetFloat("_MagentaClip", 1f); }
            else { mat.DisableKeyword("_MAGENTA_CLIP"); mat.SetFloat("_MagentaClip", 0f); }
            mat.SetFloat("_MagentaThreshold", bld_magentaThreshold);

            if (bld_useNormalMap && bld_normalMap)
            { mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_NormalMapToggle", 1f); mat.SetTexture("_BumpMap", bld_normalMap); mat.SetFloat("_BumpScale", bld_normalStrength); }

            // Glow
            if (bld_glowEnabled)
            {
                mat.EnableKeyword("_GLOW_ON"); mat.SetFloat("_GlowToggle", 1f);
                if (bld_glowMask) mat.SetTexture("_GlowMask", bld_glowMask);
                mat.SetColor("_GlowColor", bld_glowColor);
                mat.SetFloat("_GlowIntensity", bld_glowIntensity);
            }
            else { mat.DisableKeyword("_GLOW_ON"); mat.SetFloat("_GlowToggle", 0f); }

            // Height Fade
            if (bld_heightFadeEnabled)
            {
                mat.EnableKeyword("_HEIGHTFADE_ON"); mat.SetFloat("_HeightFadeToggle", 1f);
                if (bld_heightFadeMask) mat.SetTexture("_HeightFadeMask", bld_heightFadeMask);
                mat.SetColor("_HeightFadeColor", bld_heightFadeColor);
                mat.SetFloat("_HeightFadeAmount", bld_heightFadeAmount);
            }
            else { mat.DisableKeyword("_HEIGHTFADE_ON"); mat.SetFloat("_HeightFadeToggle", 0f); }

            // Weathering
            if (bld_weatherEnabled)
            {
                mat.EnableKeyword("_WEATHER_ON"); mat.SetFloat("_WeatherToggle", 1f);
                mat.SetFloat("_DirtAmount", bld_dirtAmount);
                mat.SetFloat("_MoistureBottom", bld_moistureBottom);
            }
            else { mat.DisableKeyword("_WEATHER_ON"); mat.SetFloat("_WeatherToggle", 0f); }
        }
        mat.name = $"Mat_{bld_name}";
        renderer.sharedMaterial = mat;

        // Collider
        if (bld_addCollider)
        {
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(sx * 0.9f, sy * 0.9f, 0.3f);
            box.center = new Vector3(0, sy * 0.5f, 0);
        }

        // Save material
        if (bld_saveMaterial && bld_materialOverride == null)
        {
            EnsureFolder("Assets/Materials/Buildings");
            string matPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Materials/Buildings/{mat.name}.mat");
            AssetDatabase.CreateAsset(mat, matPath);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        // Save prefab
        if (bld_savePrefab)
            SavePrefab(root, "Assets/Prefab/Buildings", bld_name);

        // Register to MapBuilderCatalog
        if (bld_registerToCatalog)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<IsometricMapEditor.MapBuilderCatalog>(
                "Assets/Resources/MapBuilder/MapBuilderCatalog.asset");
            if (catalog != null)
            {
                // BuildingDefinition SO 생성
                EnsureFolder("Assets/Resources/MapBuilder/Buildings");
                var bDef = ScriptableObject.CreateInstance<IsometricMapEditor.BuildingDefinition>();
                bDef.buildingId = bld_name;
                bDef.displayName = bld_name;
                bDef.footprint = bld_footprint;
                bDef.isEnterable = bld_isEnterable;

                // 프리팹이 저장됐으면 연결
                if (bld_savePrefab)
                {
                    string prefabPath = $"Assets/Prefab/Buildings/{bld_name}.prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null) bDef.prefab = prefab;
                }

                string defPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Resources/MapBuilder/Buildings/{bld_name}.asset");
                AssetDatabase.CreateAsset(bDef, defPath);

                var list = new List<IsometricMapEditor.BuildingDefinition>(catalog.buildings) { bDef };
                catalog.buildings = list.ToArray();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=cyan>[SpawnCreator]</color> Building registered to catalog: {bld_name}");
            }
            else
            {
                Debug.LogWarning("[SpawnCreator] MapBuilderCatalog not found at Resources/MapBuilder/MapBuilderCatalog.asset");
            }
        }

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"<color=cyan>[SpawnCreator]</color> Building '{bld_name}' created (Quad + CityBuilding shader)");
    }

    // ================================================================
    //  유틸리티
    // ================================================================

    static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
            if (target is Object unityObj) EditorUtility.SetDirty(unityObj);
        }
        else Debug.LogWarning($"<color=yellow>Field '{fieldName}' not found on {target.GetType().Name}</color>");
    }

    static void SavePrefab(GameObject go, string folder, string name)
    {
        EnsureFolder(folder);
        string path = $"{folder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            if (!EditorUtility.DisplayDialog("프리팹 덮어쓰기",
                $"'{name}.prefab' 이미 존재합니다.\n덮어쓰시겠습니까?", "덮어쓰기", "취소"))
                return;
        }
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log($"<color=green>[SpawnCreator]</color> Prefab saved: {path}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
