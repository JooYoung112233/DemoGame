using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 폐상가 교역 지구 1차 그레이박스 맵 생성.
/// Zone 1 프리팹과 동일: XZ 평면 Quad 바닥 + Cube 벽, 프로젝트 머티리얼 사용.
/// </summary>
public static class ScrapMarketGreyboxBuilder
{
    const string PrefabPath = "Assets/Prefabs/Maps/ScrapMarket_Greybox.prefab";
    const float TileSize = 1f;
    const int MapLayer = 8;
    const float WallHeight = 2.4f;
    const float WallThickness = 0.2f;

    static readonly string MatAsphalt = "Assets/Resources/MapBuilder/Mat/Asphalt_Floor.mat";
    static readonly string MatBuilding = "Assets/Resources/MapBuilder/Mat/CityBuilding.mat";
    static readonly string MatProps = "Assets/Resources/MapBuilder/Mat/props.mat";

    [MenuItem("Tools/Dev Tools/Map/Build Scrap Market Greybox Prefab")]
    public static void BuildPrefab()
    {
        var root = BuildMapHierarchy();
        try
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Maps");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[ScrapMarket]</color> 프리팹 저장: {PrefabPath} " +
                      $"(자식 {root.transform.childCount}그룹). InGameScene에 배치 후 NavMesh Rebake 권장.");
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Tools/Dev Tools/Map/Place Scrap Market Greybox In Scene")]
    public static void PlaceInScene()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            instance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Place Scrap Market Greybox");
            Selection.activeGameObject = instance;
            Debug.Log("[ScrapMarket] 기존 프리팹을 씬에 배치했습니다.");
            return;
        }

        var root = BuildMapHierarchy();
        Undo.RegisterCreatedObjectUndo(root, "Place Scrap Market Greybox");
        Selection.activeGameObject = root;
        Debug.Log("[ScrapMarket] 프리팹이 없어 임시 오브젝트로 배치했습니다. 메뉴에서 Prefab도 생성하세요.");
    }

    static GameObject BuildMapHierarchy()
    {
        var matFloor = LoadMat(MatAsphalt);
        var matWall = LoadMat(MatBuilding);
        var matProp = LoadMat(MatProps);

        var root = new GameObject("ScrapMarket_Greybox");
        root.transform.position = Vector3.zero;

        var meta = root.AddComponent<ScrapMarketMapMetadata>();

        // ── 바닥 ──
        var floors = new GameObject("Floors");
        floors.transform.SetParent(root.transform, false);

        // 골목·주변 (0~25 x 0~17)
        FillFloorRect(floors.transform, 0, 0, 26, 18, matFloor);
        // 편의점 실내 — 동일 아스팔트(나중에 인테리어 타일로 교체)
        // (이미 포함됨)

        // ── 벽 ──
        var walls = new GameObject("Walls");
        walls.transform.SetParent(root.transform, false);

        // 편의점 외벽 x=4~11, z=2~8
        AddWallBox(walls.transform, new Vector3(3.5f, WallHeight * 0.5f, 5f), new Vector3(WallThickness, WallHeight, 7f), matWall); // 서
        AddWallBox(walls.transform, new Vector3(11.5f, WallHeight * 0.5f, 5f), new Vector3(WallThickness, WallHeight, 7f), matWall); // 동
        AddWallBox(walls.transform, new Vector3(7.5f, WallHeight * 0.5f, 8.5f), new Vector3(8f, WallHeight, WallThickness), matWall); // 북
        // 남벽 — 출입문( x=5~7 )
        AddWallBox(walls.transform, new Vector3(4.5f, WallHeight * 0.5f, 1.5f), new Vector3(1f, WallHeight, WallThickness), matWall);
        AddWallBox(walls.transform, new Vector3(9.5f, WallHeight * 0.5f, 1.5f), new Vector3(5f, WallHeight, WallThickness), matWall);

        // 골목 측면 가이드 벽 (부분)
        AddWallBox(walls.transform, new Vector3(0.5f, WallHeight * 0.5f, 9f), new Vector3(WallThickness, WallHeight, 10f), matWall);
        AddWallBox(walls.transform, new Vector3(25.5f, WallHeight * 0.5f, 6f), new Vector3(WallThickness, WallHeight, 14f), matWall);

        // ── 그레이박스 소품 (3D 큐브) ──
        var props = new GameObject("Props_Greybox");
        props.transform.SetParent(root.transform, false);
        AddPropCube(props.transform, new Vector3(5.5f, 0.75f, 3.5f), new Vector3(1.8f, 1.5f, 0.5f), matProp, "Shelf_A");
        AddPropCube(props.transform, new Vector3(8.5f, 0.75f, 3.5f), new Vector3(1.8f, 1.5f, 0.5f), matProp, "Shelf_B");
        AddPropCube(props.transform, new Vector3(9.5f, 0.5f, 6f), new Vector3(2f, 1f, 0.8f), matProp, "Counter");
        AddPropCube(props.transform, new Vector3(12f, 0.4f, 1.2f), new Vector3(1.2f, 0.8f, 0.6f), matProp, "Checkpoint_Barrier");
        AddPropCube(props.transform, new Vector3(20f, 0.35f, 8f), new Vector3(2.5f, 0.7f, 1.2f), matProp, "Debris");

        // ── 구역 라벨 (빈 오브젝트 + 메타) ──
        var zones = new GameObject("Zones");
        zones.transform.SetParent(root.transform, false);
        CreateZoneMarker(zones.transform, "Alley", new Vector3(14f, 0f, 3f));
        CreateZoneMarker(zones.transform, "ConvenienceStore", new Vector3(7.5f, 0f, 5f));
        CreateZoneMarker(zones.transform, "Checkpoint", new Vector3(12f, 0f, 2f));

        // ── 게임플레이 ──
        var gameplay = new GameObject("Gameplay");
        gameplay.transform.SetParent(root.transform, false);

        CreateSpawn(gameplay.transform, "default", new Vector3(5.5f, 0f, 0.5f), Quaternion.Euler(0, 0, 0));
        CreateSpawn(gameplay.transform, "scrap_market", new Vector3(5.5f, 0f, 0.5f), Quaternion.Euler(0, 0, 0));

        CreateExit(gameplay.transform, new Vector3(22.5f, 0f, 4.5f), "Safehouse", "house", 8f);

        CreateNote(gameplay.transform, new Vector3(7.5f, 0f, 7f),
            "쪽지 (그레이박스)\n\n밤이 되면 지하 창고 문이 열린다고 한다.\n후문 탈출로는 동쪽 골목 끝.");

        var lootA = CreateLootContainer(gameplay.transform, new Vector3(5.5f, 0f, 4f), "진열대 A", 3, 4);
        var lootB = CreateLootContainer(gameplay.transform, new Vector3(8.5f, 0f, 4f), "진열대 B", 3, 4);
        CreateItemSpawn(gameplay.transform, new Vector3(5.5f, 0f, 4f), ItemSpawnPoint.SpawnType.Container, lootA);
        CreateItemSpawn(gameplay.transform, new Vector3(8.5f, 0f, 4f), ItemSpawnPoint.SpawnType.Container, lootB);
        CreateItemSpawn(gameplay.transform, new Vector3(14f, 0f, 2f), ItemSpawnPoint.SpawnType.Ground, null);
        CreateItemSpawn(gameplay.transform, new Vector3(18f, 0f, 6f), ItemSpawnPoint.SpawnType.Ground, null);
        CreateItemSpawn(gameplay.transform, new Vector3(3f, 0f, 5f), ItemSpawnPoint.SpawnType.Ground, null);

        meta.regionId = "scrap_market";
        meta.displayName = "폐상가 교역 지구";
        meta.subzone = "무너진 상가 / 검문소";
        meta.floorCellCount = 26 * 18;
        meta.wallCount = walls.transform.childCount;

        SetLayerRecursive(root.transform, MapLayer);
        return root;
    }

    static void FillFloorRect(Transform parent, int x0, int z0, int width, int height, Material mat)
    {
        var group = parent.Find($"Floor_{x0}_{z0}_{width}x{height}");
        if (group == null)
        {
            var go = new GameObject($"Floor_{x0}_{z0}_{width}x{height}");
            go.transform.SetParent(parent, false);
            group = go.transform;
        }

        for (int x = x0; x < x0 + width; x++)
        {
            for (int z = z0; z < z0 + height; z++)
            {
                var pos = new Vector3(x * TileSize + TileSize * 0.5f, 0.001f, z * TileSize + TileSize * 0.5f);
                var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Tile_{x}_{z}";
                tile.transform.SetParent(group, false);
                tile.transform.position = pos;
                tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                tile.transform.localScale = new Vector3(TileSize, TileSize, 1f);
                tile.GetComponent<MeshRenderer>().sharedMaterial = mat;
                tile.GetComponent<MeshRenderer>().sortingLayerName = "Ground";
            }
        }
    }

    static void AddWallBox(Transform parent, Vector3 center, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Wall_{center.x:0.#}_{center.z:0.#}";
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = Vector3.one;
    }

    static void AddPropCube(Transform parent, Vector3 center, Vector3 scale, Material mat, string label)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = label;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    static void CreateZoneMarker(Transform parent, string zoneName, Vector3 pos)
    {
        var go = new GameObject($"Zone_{zoneName}");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
    }

    static void CreateSpawn(Transform parent, string pointId, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject($"Spawn_{pointId}");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, rot);
        var sp = go.AddComponent<SpawnPoint>();
        SetField(sp, "pointId", pointId);
    }

    static void CreateExit(Transform parent, Vector3 pos, string scene, string spawnId, float wait)
    {
        var go = CreateInteractableRoot(parent, "Exit_ToSafehouse", pos, new Color(0.2f, 0.9f, 0.35f));
        var io = go.GetComponent<InteractableObject>();
        SetField(io, "type", InteractableObject.InteractType.ExitPoint);
        SetField(io, "promptText", "탈출하기");
        SetField(io, "interactRange", 2.5f);
        SetField(io, "targetScene", scene);
        SetField(io, "spawnPointId", spawnId);
        SetField(io, "exitWaitTime", wait);
    }

    static void CreateNote(Transform parent, Vector3 pos, string content)
    {
        var go = CreateInteractableRoot(parent, "Note_WarehouseHint", pos, new Color(0.95f, 0.9f, 0.5f));
        var io = go.GetComponent<InteractableObject>();
        SetField(io, "type", InteractableObject.InteractType.Note);
        SetField(io, "promptText", "읽기");
        SetField(io, "oneShot", true);
        SetField(io, "noteContent", content);
    }

    static LootContainer CreateLootContainer(Transform parent, Vector3 pos, string containerName, int w, int h)
    {
        var go = CreateInteractableRoot(parent, $"Container_{containerName}", pos, new Color(0.85f, 0.55f, 0.2f));
        var io = go.GetComponent<InteractableObject>();
        SetField(io, "type", InteractableObject.InteractType.Container);
        SetField(io, "promptText", "수색하기");
        SetField(io, "interactRange", 1.8f);

        var lc = go.AddComponent<LootContainer>();
        SetField(lc, "gridWidth", w);
        SetField(lc, "gridHeight", h);
        SetField(lc, "containerName", containerName);
        return lc;
    }

    static void CreateItemSpawn(Transform parent, Vector3 pos, ItemSpawnPoint.SpawnType type, LootContainer linked)
    {
        var go = new GameObject($"ItemSpawn_{type}_{pos.x:0.#}_{pos.z:0.#}");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sp = go.AddComponent<ItemSpawnPoint>();
        SetField(sp, "spawnType", type);
        SetField(sp, "useRegionLoot", true);
        SetField(sp, "regionIdOverride", "scrap_market");
        if (linked != null)
            SetField(sp, "linkedContainer", linked);
    }

    static GameObject CreateInteractableRoot(Transform parent, string objName, Vector3 pos, Color markerColor)
    {
        var root = new GameObject(objName);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.AddComponent<InteractableObject>();
        root.AddComponent<TopDownDepthSorter>();

        var col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0, 0.35f, 0);
        col.size = new Vector3(0.8f, 0.7f, 0.8f);

        // 3D 마커 (프로젝트 머티리얼)
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Marker";
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0, 0.35f, 0);
        marker.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        var mat = LoadMat(MatProps);
        if (mat != null)
        {
            var inst = new Material(mat);
            if (inst.HasProperty("_BaseColor"))
                inst.SetColor("_BaseColor", markerColor);
            else if (inst.HasProperty("_Color"))
                inst.SetColor("_Color", markerColor);
            marker.GetComponent<MeshRenderer>().sharedMaterial = inst;
        }
        Object.DestroyImmediate(marker.GetComponent<Collider>());

        return root;
    }

    static Material LoadMat(string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
            Debug.LogWarning($"[ScrapMarket] Material not found: {path}");
        return m;
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

    static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i), layer);
    }

    static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);
        if (field != null)
            field.SetValue(target, value);
    }
}
