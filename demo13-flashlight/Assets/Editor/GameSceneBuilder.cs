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

        // ── 5. SpawnPoint (플레이어 스폰 위치) ───────────────────────
        var spawnGo = new GameObject("SpawnPoint_Default");
        spawnGo.AddComponent<SpawnPoint>();
        spawnGo.transform.position = type == SceneType.Safehouse
            ? new Vector3(0, 0, 0)
            : new Vector3(0, -2, 0); // InGame은 입구 쪽

        // ── 6. 씬 타입별 추가 ────────────────────────────────────────
        if (type == SceneType.InGame)
        {
            // 탈출 트리거 플레이스홀더
            var exitGo = new GameObject("ExitTrigger_Placeholder");
            exitGo.transform.position = new Vector3(0, 6, 0);
            var exitCol = exitGo.AddComponent<BoxCollider2D>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector2(2, 0.5f);
            // EscapePoint 컴포넌트는 별도 추가 필요 (현재 미구현)

            // 레이드 타이머용 빈 오브젝트
            new GameObject("RaidManager_Placeholder");
        }

        if (type == SceneType.Safehouse)
        {
            // 안전가옥은 timeScale=0 (코드에서 처리) — 메모 오브젝트
            var infoGo = new GameObject("__SafehouseInfo");
            // 별도 맵보드(MapBoard) 트리거 추가 위치 — 실제 배치는 Tilemap에서
        }

        // ── 7. 씬 정보 마커 ──────────────────────────────────────────
        var infoObj = new GameObject($"__SceneInfo_{sceneName}");
        // 씬 이름을 찾기 쉽게 최상단에 배치

        // ── 저장 ─────────────────────────────────────────────────────
        Selection.activeObject = null;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);

        Debug.Log($"[GameSceneBuilder] 생성 완료(맵 콘텐츠만): {path}");
        Debug.Log($"[GameSceneBuilder] 포함: Grid(바닥+벽 Tilemap), SpawnPoint" + (type == SceneType.InGame ? ", ExitTrigger/RaidManager 플레이스홀더" : "") + ".");
        Debug.Log($"[GameSceneBuilder] 카메라/조명/EventSystem/매니저/플레이어는 Systems 부트 씬이 additive로 공급(docs/architecture.md). Systems 씬을 열고 Play하면 이 맵을 로드.");
        Debug.Log($"[GameSceneBuilder] 다음 할 일: ① Tilemap에 타일 배치 ② SpawnPoint 위치 조정 ③ 'Tools/TopDown/Build/Systems Scene' 확인");
    }
}
#endif
