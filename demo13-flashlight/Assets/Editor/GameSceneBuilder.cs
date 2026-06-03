#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 게임플레이(맵 콘텐츠) 씬 빌더 — 탑다운 2D 맵 골격 생성.
/// 카메라·조명·EventSystem·매니저·플레이어는 모두 Systems 부트 씬이 additive로 공급하므로 넣지 않음.
/// 게임플레이 씬엔 '맵 콘텐츠'만: Grid/Tilemap, SpawnPoint(+ 씬별 마커). (docs/architecture.md)
///
/// Tools > TopDown > Build > InGame Scene (또는 Safehouse Scene)
/// </summary>
public static class GameSceneBuilder
{
    const string SCENE_DIR = "Assets/Scenes";

    // ── 공통: 씬 어느 타입이든 동일하게 들어가는 것 ──────────────────

    [MenuItem("Tools/TopDown/Build/InGame Scene")]
    static void BuildInGame() => BuildScene("InGameScene", SceneType.InGame);

    [MenuItem("Tools/TopDown/Build/Safehouse Scene")]
    static void BuildSafehouse() => BuildScene("Safehouse", SceneType.Safehouse);

    // ─────────────────────────────────────────────────────────────────

    enum SceneType { InGame, Safehouse }

    static void BuildScene(string sceneName, SceneType type)
    {
        Directory.CreateDirectory(SCENE_DIR);
        string path = $"{SCENE_DIR}/{sceneName}.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── 시스템 없음: Systems 부트 씬이 전부 공급 ──────────────────
        //    카메라 / 2D 글로벌 조명 / EventSystem / 매니저 / 플레이어는 모두 Systems 씬에 있고,
        //    게임플레이 씬은 그 위에 additive로 로드된다(docs/architecture.md).
        //    → 여기엔 '맵 콘텐츠'만 만든다: Grid/Tilemap + SpawnPoint(+ 씬별 마커).
        //    (Systems가 빌드세팅에 없을 때만 기존 코드 폴백이 카메라/조명/UI를 스폰)

        // ── Tilemap 루트 ───────────────────────────────────────────────
        var gridGo = new GameObject("Grid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        // 바닥 레이어
        var floorGo = new GameObject("Floor");
        floorGo.transform.SetParent(gridGo.transform);
        var floorTm = floorGo.AddComponent<Tilemap>();
        var floorRend = floorGo.AddComponent<TilemapRenderer>();
        floorRend.sortingLayerName = "Ground";
        floorRend.sortingOrder = 0;
        floorGo.tag = "Floor";

        // 벽 레이어 (충돌 포함)
        var wallGo = new GameObject("Walls");
        wallGo.transform.SetParent(gridGo.transform);
        var wallTm = wallGo.AddComponent<Tilemap>();
        var wallRend = wallGo.AddComponent<TilemapRenderer>();
        wallRend.sortingLayerName = "Default";
        wallRend.sortingOrder = 1;
        wallGo.AddComponent<TilemapCollider2D>();
        wallGo.AddComponent<CompositeCollider2D>();
        var wallRb = wallGo.GetComponent<Rigidbody2D>();
        wallRb.bodyType = RigidbodyType2D.Static;

        // ── 스폰 포인트 ──────────────────────────────────────────────
        if (type == SceneType.Safehouse)
        {
            // 부팅(default) / 귀환(raid_return) / 시간초과(raid_fail) / 사망(raid_death) 복귀 지점
            MakeSpawn("default",     new Vector3(0, 0, 0));
            MakeSpawn("raid_return", new Vector3(1.5f, 0, 0));
            MakeSpawn("raid_fail",   new Vector3(-1.5f, 0, 0));
            MakeSpawn("raid_death",  new Vector3(0, -1.5f, 0));
        }
        else
        {
            // 레이드 진입 스폰 (scrap_market 지역 spawnId = "default")
            MakeSpawn("default", new Vector3(0, -2, 0));
        }

        // ── 씬 타입별 루프 콘텐츠 ────────────────────────────────────
        if (type == SceneType.InGame)
        {
            // 레이드 매니저 (타이머 / 사망 / 시간초과 + 루트 추적)
            new GameObject("RaidManager").AddComponent<RaidManager>();

            // 파밍 — 줍기 아이템 흩뿌림 (ItemDatabase에 존재하는 ID)
            MakePickup("Loot_Knife",      new Vector3(-3, 1, 0),    "knife",       1);
            MakePickup("Loot_CannedFood", new Vector3(3, 1, 0),     "canned_food", 1);
            MakePickup("Loot_ScrapMetal", new Vector3(-2, 3, 0),    "scrap_metal", 2);
            MakePickup("Loot_CoinScrap",  new Vector3(2, 3, 0),     "coin_scrap",  3);
            MakePickup("Loot_RubyShard",  new Vector3(0, 4.5f, 0),  "ruby_shard",  1);

            // 탈출구 — 상호작용 후 5초 대기(거리 이탈 시 취소) → 안전가옥 raid_return
            var exit = MakeInteractable("ExitPoint", new Vector3(0, 6, 0), new Color(0.3f, 1f, 0.45f), 0.7f);
            SetIO(exit, InteractableObject.InteractType.ExitPoint, "탈출하기", 2f, false,
                targetScene: "Safehouse", spawnId: "raid_return", exitWait: 5f);
        }
        else // Safehouse
        {
            // 지도판 — 출전 선택 (MapSelectUI)
            var board = MakeInteractable("MapBoard", new Vector3(3, 0, 0), new Color(0.4f, 0.8f, 1f), 0.8f);
            SetIO(board, InteractableObject.InteractType.MapBoard, "지도판 — 출전 선택", 2f, false);

            // 침대 — 휴식
            var bed = MakeInteractable("Bed", new Vector3(-3, 0, 0), new Color(0.8f, 0.7f, 0.5f), 0.8f);
            SetIO(bed, InteractableObject.InteractType.Bed, "휴식", 2f, false);

            // 작업대 — 제작/수리
            var bench = MakeInteractable("Workbench", new Vector3(-3, 2, 0), new Color(0.7f, 0.6f, 0.4f), 0.8f);
            SetIO(bench, InteractableObject.InteractType.Workbench, "작업대", 2f, false);
        }

        // ── 씬 정보 마커 ──────────────────────────────────────────────
        new GameObject($"__SceneInfo_{sceneName}");

        // ── 저장 ─────────────────────────────────────────────────────
        Selection.activeObject = null;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);

        Debug.Log($"[GameSceneBuilder] 생성 완료(맵 콘텐츠만): {path}");
        if (type == SceneType.InGame)
            Debug.Log("[GameSceneBuilder] InGame: Grid + Spawn(default) + RaidManager + 줍기5(knife/canned_food/scrap_metal/coin_scrap/ruby_shard) + ExitPoint(→Safehouse/raid_return, 5초).");
        else
            Debug.Log("[GameSceneBuilder] Safehouse: Grid + Spawn(default/raid_return/raid_fail/raid_death) + MapBoard + Bed + Workbench.");
        Debug.Log("[GameSceneBuilder] 카메라/조명/EventSystem/매니저/플레이어는 Systems 부트 씬이 additive로 공급(docs/architecture.md). Systems 씬을 열고 Play.");
        Debug.Log("[GameSceneBuilder] 다음 할 일: ① Tilemap에 바닥/벽 타일 배치 ② 오브젝트 위치 조정 ③ 'Tools/TopDown/Build/Systems Scene' 확인");
    }

    // ── 빌드 헬퍼 ────────────────────────────────────────────────────

    /// <summary>이름붙은 SpawnPoint 생성 (pointId 직렬화 설정).</summary>
    static SpawnPoint MakeSpawn(string id, Vector3 pos)
    {
        var go = new GameObject($"Spawn_{id}");
        go.transform.position = pos;
        var sp = go.AddComponent<SpawnPoint>();
        var so = new SerializedObject(sp);
        var p = so.FindProperty("pointId");
        if (p != null) { p.stringValue = id; so.ApplyModifiedPropertiesWithoutUndo(); }
        return sp;
    }

    /// <summary>가시용 스프라이트(빌트인) + InteractableObject를 가진 마커 생성.</summary>
    static InteractableObject MakeInteractable(string name, Vector3 pos, Color color, float scale)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"); // 임시 플레이스홀더 비주얼
        sr.color = color;
        sr.sortingOrder = 5;
        return go.AddComponent<InteractableObject>();
    }

    /// <summary>줍기(Pickup) 마커 생성 (itemId/수량 설정).</summary>
    static void MakePickup(string name, Vector3 pos, string itemId, int count)
    {
        var io = MakeInteractable(name, pos, new Color(1f, 0.9f, 0.4f), 0.5f);
        SetIO(io, InteractableObject.InteractType.Pickup, $"줍기: {itemId}", 1.5f, true,
            itemId: itemId, itemCount: count);
    }

    /// <summary>InteractableObject의 private 직렬화 필드를 한 번에 설정.</summary>
    static void SetIO(InteractableObject io, InteractableObject.InteractType type, string prompt,
        float range, bool oneShot, string targetScene = null, string spawnId = null,
        float exitWait = 0f, string itemId = null, int itemCount = 1)
    {
        var so = new SerializedObject(io);
        var t = so.FindProperty("type");           if (t != null) t.enumValueIndex = (int)type;
        var pr = so.FindProperty("promptText");     if (pr != null) pr.stringValue = prompt;
        var rg = so.FindProperty("interactRange");  if (rg != null) rg.floatValue = range;
        var os = so.FindProperty("oneShot");        if (os != null) os.boolValue = oneShot;
        var ew = so.FindProperty("exitWaitTime");   if (ew != null) ew.floatValue = exitWait;
        var ic = so.FindProperty("itemCount");      if (ic != null) ic.intValue = itemCount;
        if (targetScene != null) { var p = so.FindProperty("targetScene");  if (p != null) p.stringValue = targetScene; }
        if (spawnId != null)     { var p = so.FindProperty("spawnPointId"); if (p != null) p.stringValue = spawnId; }
        if (itemId != null)      { var p = so.FindProperty("itemId");       if (p != null) p.stringValue = itemId; }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
