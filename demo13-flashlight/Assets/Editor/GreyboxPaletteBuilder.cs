#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 레벨 디자인(그레이박스) 테스트용 팔레트 생성기.
///
/// 단색 박스 스프라이트 + 한글 라벨(자식 TextMesh)을 가진 프롭들을 만들고,
/// 기존 Prop2D 카탈로그(Resources/Props2D/*.asset)에 등록한다.
/// → 기존 "2D Prop Catalog" 창(Tools ▸ TopDown ▸ Map ▸ Prop Catalog)의
///   각 탭(바닥/벽/오브젝트/천장)에 바로 떠서 클릭 배치로 레이아웃을 시험할 수 있다.
///
/// 아트는 중요하지 않다 — 색/라벨/정렬/콜라이더·기능만 맞춘다.
///   • 바닥/지붕  = 통과(콜라이더 None)
///   • 벽/막힘    = 막힘(Box 콜라이더, 비-트리거)
///   • 문         = Door 기능(DoorController)
///   • 상자/선반  = LootContainer(수색)
///   • 스폰       = SpawnPoint
///   • 탈출       = Interactable(ExitPoint)
///   • 적/ NPC    = 마커(적=기능없음, NPC=NPC 기능)
///
/// 조립·등록은 일반 프롭과 동일 경로를 쓴다:
///   1) 단색 PNG 스프라이트 생성 (Resources/Greybox/{propId}.png, PPU=프로젝트 기본 100 → 1x1m)
///   2) Prop2DDefinition 작성(카테고리/콜라이더/기능/정렬 등)
///   3) Prop2DBuilder.Build(def)로 프리팹 조립(콜라이더+기능 컴포넌트가 정확히 부착됨)
///      → Resources/Props2D/Prefabs/{propId}.prefab 로 저장
///   4) 그 프리팹을 열어 자식 TextMesh "Label"(한글 표시명) 추가 후 재저장
///   5) def.prefab 연결 + Resources/Props2D/{propId}.asset 로 등록(카탈로그가 t:Prop2DDefinition 으로 자동 발견)
///
/// 재실행 시(멱등): 같은 propId의 정의가 이미 있으면 새로 만들지 않고 그 자리에서 갱신한다.
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Generate Greybox Palette
/// </summary>
public static class GreyboxPaletteBuilder
{
    // ── 경로(기존 Prop2DCatalogEditor 규약과 동일) ──
    const string PropFolder    = "Assets/Resources/Props2D";          // 카탈로그가 t:Prop2DDefinition 으로 스캔하는 폴더
    const string PrefabFolder  = PropFolder + "/Prefabs";             // 프리팹 규약: {propId}.prefab
    const string SpriteFolder  = "Assets/Resources/Greybox";          // 그레이박스 PNG 출력 폴더

    // 프로젝트 기본 PPU(기존 floor 스프라이트 meta: spritePixelsToUnits: 100).
    // 텍스처를 PPU와 같은 픽셀 크기로 만들면 정확히 1x1 월드유닛 = 1m 타일.
    const int   PixelsPerUnit  = 100;
    const int   TexSize        = PixelsPerUnit;                       // 100x100 → 1x1m
    const int   BorderPx       = 3;                                   // 박스 모서리 가독용 어두운 테두리

    // 라벨(한글) — Windows 기본 한글 폰트. 빌트인(LegacyRuntime)은 한글 미렌더라 동적 폰트 사용.
    const string KoreanOsFont  = "Malgun Gothic";
    const int    LabelFontSize = 48;                                  // 동적 폰트 베이스 크기(선명도용, 크게)
    const float  LabelCharSize = 0.05f;                               // 월드 문자 크기(0.05 → 라벨 한 글자 ≈ 0.12m)

    /// <summary>그레이박스 타입 1개 사양.</summary>
    struct Spec
    {
        public string id;            // propId (gb_*)
        public string label;         // displayName(한글) = 라벨 텍스트
        public Color  color;         // 박스 색(알파 포함 — 마커는 ~0.6 반투명)
        public Prop2DDefinition.Category   category;
        public Prop2DDefinition.ColliderMode colliderMode;
        public bool   isTrigger;
        public Prop2DDefinition.Function   function;
        public string sortingLayer;
        public int    sortingOffset;
        public bool   labelDark;     // true=검은 라벨(밝은 박스), false=흰 라벨(어두운 박스)

        // 기능별 추가 파라미터(필요한 것만 채움)
        public InteractableObject.InteractType interactType;
        public DoorController.LockType         doorLockType;
        public string spawnPointId;
        public int    lootW, lootH;
        public bool   lootUseRegion;

        // 천장 컷어웨이(지붕)
        public string ceilingGroupId;
        public float  ceilingHiddenAlpha;
        public float  ceilingFadeSpeed;
    }

    [MenuItem("Tools/TopDown/Map/Generate Greybox Palette")]
    public static void Generate()
    {
        // 지붕은 최상단 'Ceiling' 정렬 레이어가 있어야 위로 그려짐(없으면 보장/복구).
        GameLayers.EnsureSortingLayer("Ceiling");

        var specs = BuildSpecs();

        EnsureFolder(SpriteFolder);
        EnsureFolder(PropFolder);
        EnsureFolder(PrefabFolder);

        int made = 0, updated = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < specs.Count; i++)
            {
                if (!Application.isBatchMode)
                    EditorUtility.DisplayProgressBar("그레이박스 팔레트 생성",
                        $"{specs[i].label} ({specs[i].id})", (float)i / specs.Count);
                bool isNew = BuildOne(specs[i]);
                if (isNew) made++; else updated++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            if (!Application.isBatchMode) EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"그레이박스 팔레트 {specs.Count}종 준비 완료 — 신규 {made} · 갱신 {updated}.\n" +
                     $"스프라이트: {SpriteFolder}/gb_*.png\n" +
                     $"프리팹: {PrefabFolder}/gb_*.prefab\n" +
                     $"정의: {PropFolder}/gb_*.asset\n\n" +
                     "Tools ▸ TopDown ▸ Map ▸ Prop Catalog 의 바닥/벽/오브젝트/천장 탭에서 클릭 배치하세요.";
        Debug.Log("<color=cyan>[Greybox]</color> " + msg.Replace("\n", " "));
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("그레이박스 팔레트", msg, "확인");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 11종 사양
    // ─────────────────────────────────────────────────────────────────────
    static List<Spec> BuildSpecs()
    {
        // 정렬: 바닥 최하단(Ground -10) → 벽/오브젝트(Ground 0~2) → 마커 약간 위(Ground 5) → 지붕(Ceiling).
        var list = new List<Spec>
        {
            // 바닥 — 통과, 최하단.
            new Spec {
                id = "gb_floor", label = "바닥", color = Opaque(0.55f, 0.55f, 0.55f),
                category = Prop2DDefinition.Category.Floor,
                colliderMode = Prop2DDefinition.ColliderMode.None, isTrigger = false,
                function = Prop2DDefinition.Function.None,
                sortingLayer = "Ground", sortingOffset = -10, labelDark = true,
            },
            // 지붕 — 천장 컷어웨이(진입 시 페이드), 통과, 최상단 'Ceiling' 레이어.
            new Spec {
                id = "gb_roof", label = "지붕", color = Opaque(0.30f, 0.30f, 0.34f),
                category = Prop2DDefinition.Category.Ceiling,
                colliderMode = Prop2DDefinition.ColliderMode.None, isTrigger = false,
                function = Prop2DDefinition.Function.None,
                sortingLayer = "Ceiling", sortingOffset = 0, labelDark = false,
                ceilingGroupId = "gb", ceilingHiddenAlpha = 0.25f, ceilingFadeSpeed = 6f,
            },
            // 벽 — 막힘(Box, 비-트리거).
            new Spec {
                id = "gb_wall", label = "벽", color = Opaque(0.40f, 0.40f, 0.42f),
                category = Prop2DDefinition.Category.Wall,
                colliderMode = Prop2DDefinition.ColliderMode.Box, isTrigger = false,
                function = Prop2DDefinition.Function.None,
                sortingLayer = "Ground", sortingOffset = 0, labelDark = false,
            },
            // 막힘(바리케이드) — 지금은 막힘, 통합 시 제거 예정.
            new Spec {
                id = "gb_barricade", label = "막힘", color = Opaque(0.85f, 0.45f, 0.15f),
                category = Prop2DDefinition.Category.Wall,
                colliderMode = Prop2DDefinition.ColliderMode.Box, isTrigger = false,
                function = Prop2DDefinition.Function.None,
                sortingLayer = "Ground", sortingOffset = 1, labelDark = true,
            },
            // 문 — Door(DoorController, 잠금 없음). Box 막힘이 문 차단 콜라이더로 연결됨.
            new Spec {
                id = "gb_door", label = "문", color = Opaque(0.55f, 0.38f, 0.22f),
                category = Prop2DDefinition.Category.Object,
                colliderMode = Prop2DDefinition.ColliderMode.Box, isTrigger = false,
                function = Prop2DDefinition.Function.Door,
                doorLockType = DoorController.LockType.None,
                sortingLayer = "Ground", sortingOffset = 2, labelDark = false,
            },
            // 상자 — LootContainer(수색), 작은 격자 4x3.
            new Spec {
                id = "gb_crate", label = "상자", color = Opaque(0.85f, 0.72f, 0.20f),
                category = Prop2DDefinition.Category.Object,
                colliderMode = Prop2DDefinition.ColliderMode.Box, isTrigger = false,
                function = Prop2DDefinition.Function.LootContainer,
                lootW = 4, lootH = 3, lootUseRegion = true,
                sortingLayer = "Ground", sortingOffset = 2, labelDark = true,
            },
            // 선반 — LootContainer(수색), 격자 6x2.
            new Spec {
                id = "gb_shelf", label = "선반", color = Opaque(0.70f, 0.55f, 0.30f),
                category = Prop2DDefinition.Category.Object,
                colliderMode = Prop2DDefinition.ColliderMode.Box, isTrigger = false,
                function = Prop2DDefinition.Function.LootContainer,
                lootW = 6, lootH = 2, lootUseRegion = true,
                sortingLayer = "Ground", sortingOffset = 2, labelDark = true,
            },
            // 스폰 — SpawnPoint(통과 트리거 마커, 반투명).
            new Spec {
                id = "gb_spawn", label = "스폰", color = Marker(0.25f, 0.70f, 0.30f),
                category = Prop2DDefinition.Category.Object,
                colliderMode = Prop2DDefinition.ColliderMode.None, isTrigger = true,
                function = Prop2DDefinition.Function.SpawnPoint,
                spawnPointId = "default",
                sortingLayer = "Ground", sortingOffset = 5, labelDark = false,
            },
            // 탈출 — Interactable(ExitPoint), 통과 트리거 마커.
            new Spec {
                id = "gb_exit", label = "탈출", color = Marker(0.20f, 0.50f, 0.85f),
                category = Prop2DDefinition.Category.Object,
                colliderMode = Prop2DDefinition.ColliderMode.None, isTrigger = true,
                function = Prop2DDefinition.Function.Interactable,
                interactType = InteractableObject.InteractType.ExitPoint,
                sortingLayer = "Ground", sortingOffset = 5, labelDark = false,
            },
            // 적 — 마커만(실제 적은 추후 배치). 기능 없음, 반투명.
            new Spec {
                id = "gb_enemy", label = "적", color = Marker(0.80f, 0.25f, 0.25f),
                category = Prop2DDefinition.Category.Object,
                colliderMode = Prop2DDefinition.ColliderMode.None, isTrigger = true,
                function = Prop2DDefinition.Function.None,
                sortingLayer = "Ground", sortingOffset = 5, labelDark = false,
            },
            // NPC — NPC 기능(npcId 비움 = 빈 NPC), 통과 트리거 마커.
            new Spec {
                id = "gb_npc", label = "NPC", color = Marker(0.20f, 0.70f, 0.70f),
                category = Prop2DDefinition.Category.Object,
                colliderMode = Prop2DDefinition.ColliderMode.None, isTrigger = true,
                function = Prop2DDefinition.Function.NPC,
                sortingLayer = "Ground", sortingOffset = 5, labelDark = false,
            },
        };
        return list;
    }

    static Color Opaque(float r, float g, float b) => new Color(r, g, b, 1f);
    static Color Marker(float r, float g, float b) => new Color(r, g, b, 0.6f); // 마커는 바닥이 비치게 반투명

    // ─────────────────────────────────────────────────────────────────────
    // 한 종 생성: 스프라이트 → 정의 → 프리팹(빌더) → 라벨 추가 → 등록
    // 반환: 신규면 true, 기존 갱신이면 false.
    // ─────────────────────────────────────────────────────────────────────
    static bool BuildOne(Spec s)
    {
        // 1) 단색 박스 스프라이트(어두운 테두리) 생성/갱신.
        Sprite sprite = CreateOrUpdateSprite(s);

        // 2) 정의 — 멱등: 기존 gb_* 정의 있으면 그걸 갱신, 없으면 새로.
        bool isNew;
        Prop2DDefinition def = FindDefinitionById(s.id);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<Prop2DDefinition>();
            isNew = true;
        }
        else
        {
            isNew = false;
        }

        ApplySpecToDefinition(def, s, sprite);

        if (isNew)
        {
            string assetPath = $"{PropFolder}/{s.id}.asset";
            AssetDatabase.CreateAsset(def, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(def);
        }

        // 3) 빌더로 프리팹 조립(콜라이더 + 기능 컴포넌트가 일반 프롭과 동일하게 부착됨).
        var temp = Prop2DBuilder.Build(def);
        string prefabPath = $"{PrefabFolder}/{s.id}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp); // 씬에서 임시 GO 정리

        // 4) 저장된 프리팹을 열어 자식 TextMesh "Label"(한글) 추가 후 재저장.
        AddLabelToPrefab(prefabPath, s);

        // 5) def.prefab 연결 + 더티.
        def.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorUtility.SetDirty(def);

        return isNew;
    }

    /// <summary>Spec → Prop2DDefinition 필드 채우기(정확한 필드명 사용).</summary>
    static void ApplySpecToDefinition(Prop2DDefinition def, Spec s, Sprite sprite)
    {
        def.propId       = s.id;
        def.displayName  = s.label;
        def.category     = s.category;
        def.group        = "그레이박스";        // 카탈로그 분류 태그로 묶기(런타임 영향 없음)
        def.sprite       = sprite;
        def.material     = null;
        def.sortingLayer = s.sortingLayer;
        def.sortingOffset= s.sortingOffset;
        def.drawMode     = SpriteDrawMode.Simple;

        def.colliderMode = s.colliderMode;
        def.isTrigger    = s.isTrigger;
        def.boxSizeScale = Vector2.one;        // 그림 크기(=1x1m) 그대로
        def.boxOffset    = Vector2.zero;

        def.castShadow   = false;              // 그레이박스는 그림자 불필요
        def.breakable    = false;
        def.weathered    = false;
        def.emitsLight   = false;
        def.noVisual     = false;              // 색 박스를 보이게(마커도 박스로 표시)

        // 기능 + 파라미터.
        def.function     = s.function;
        switch (s.function)
        {
            case Prop2DDefinition.Function.SpawnPoint:
                def.spawnPointId = string.IsNullOrEmpty(s.spawnPointId) ? "default" : s.spawnPointId;
                break;
            case Prop2DDefinition.Function.Interactable:
                def.interactType  = s.interactType;
                def.interactRange = 1.5f;
                break;
            case Prop2DDefinition.Function.LootContainer:
                def.lootGridWidth   = Mathf.Max(1, s.lootW);
                def.lootGridHeight  = Mathf.Max(1, s.lootH);
                def.lootUseRegionLoot = s.lootUseRegion;
                def.interactRange   = 1.5f;
                break;
            case Prop2DDefinition.Function.Door:
                def.doorLockType  = s.doorLockType;
                def.interactRange = 1.5f;
                break;
            case Prop2DDefinition.Function.NPC:
                def.npcId         = "";        // 빈 NPC(추후 지정)
                def.interactRange = 1.5f;
                break;
        }

        // 천장(지붕) 컷어웨이 파라미터.
        if (s.category == Prop2DDefinition.Category.Ceiling)
        {
            def.ceilingGroupId     = s.ceilingGroupId ?? "";
            def.ceilingHiddenAlpha = Mathf.Clamp01(s.ceilingHiddenAlpha);
            def.ceilingFadeSpeed   = Mathf.Max(0.1f, s.ceilingFadeSpeed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 스프라이트(단색 + 어두운 테두리) 생성·임포트
    // ─────────────────────────────────────────────────────────────────────
    static Sprite CreateOrUpdateSprite(Spec s)
    {
        // PNG 임포터는 배치모드에서 "방금 쓴 새 파일"을 즉시 Sprite로 못 읽는 타이밍 문제가 있다.
        // → Texture2D + Sprite를 .asset으로 직접 생성(임포터 우회)해 안정적으로 처리.
        string path = $"{SpriteFolder}/{s.id}.asset";

        var fill   = s.color;
        // 테두리 = 채움색을 어둡게(알파는 채움색 따라감 — 마커 반투명 유지).
        var border = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, fill.a);

        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            name = s.id + "_tex",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var px = new Color[TexSize * TexSize];
        for (int y = 0; y < TexSize; y++)
        for (int x = 0; x < TexSize; x++)
        {
            bool edge = x < BorderPx || x >= TexSize - BorderPx ||
                        y < BorderPx || y >= TexSize - BorderPx;
            px[y * TexSize + x] = edge ? border : fill;
        }
        tex.SetPixels(px);
        tex.Apply();

        var sprite = Sprite.Create(
            tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f),
            PixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = s.id;

        // 기존 것 있으면 교체
        if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(sprite, path);     // 메인 = Sprite
        AssetDatabase.AddObjectToAsset(tex, sprite); // 서브 = Texture2D
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path);

        // 배치모드에선 CreateAsset 직후 LoadAssetAtPath가 null일 수 있으므로,
        // asset에 바인딩된 sprite 객체를 직접 반환(유효 참조 → def.sprite 직렬화됨).
        return sprite;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 프리팹에 한글 라벨(자식 TextMesh) 추가
    // ─────────────────────────────────────────────────────────────────────
    static void AddLabelToPrefab(string prefabPath, Spec s)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) return;
        try
        {
            // 기존 라벨 제거(재실행 멱등).
            var old = root.transform.Find("Label");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(root.transform, false);
            labelGO.transform.localPosition = Vector3.zero;

            var tm = labelGO.AddComponent<TextMesh>();
            tm.text          = s.label;
            tm.anchor        = TextAnchor.MiddleCenter;
            tm.alignment     = TextAlignment.Center;
            tm.fontSize      = LabelFontSize;
            tm.characterSize = LabelCharSize;
            tm.color         = s.labelDark ? new Color(0.05f, 0.05f, 0.05f, 1f)
                                           : new Color(0.97f, 0.97f, 0.97f, 1f);

            var font = LoadKoreanFont();
            if (font != null)
            {
                tm.font = font;
                // TextMesh는 폰트의 머티리얼을 MeshRenderer에 직접 물려야 글자가 보인다.
                var mr = labelGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = font.material;
            }

            // 라벨은 박스 위에 그려지도록 같은 정렬 레이어 + 오프셋 +1.
            var mrend = labelGO.GetComponent<MeshRenderer>();
            if (mrend != null)
            {
                mrend.sortingLayerName = s.sortingLayer;
                mrend.sortingOrder     = s.sortingOffset + 1;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>한글 렌더 가능한 폰트. OS '맑은 고딕' 동적 폰트 우선, 실패 시 빌트인 폴백.</summary>
    static Font LoadKoreanFont()
    {
        // 동적 OS 폰트(맑은 고딕) — 한글 글리프 지원. 에디터에서 베이크 시 정상 동작.
        var f = Font.CreateDynamicFontFromOSFont(KoreanOsFont, LabelFontSize);
        if (f != null) return f;
        // 일부 환경 폰트명 차이 대비.
        f = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Arial" }, LabelFontSize);
        if (f != null) return f;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // ASCII만 — 최후 폴백
    }

    // ─────────────────────────────────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>propId로 기존 Prop2DDefinition 찾기(카탈로그와 동일하게 t:Prop2DDefinition 스캔).</summary>
    static Prop2DDefinition FindDefinitionById(string propId)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prop2DDefinition"))
        {
            var d = AssetDatabase.LoadAssetAtPath<Prop2DDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (d != null && d.propId == propId) return d;
        }
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf   = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
