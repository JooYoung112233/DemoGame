#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

/// <summary>
/// 게임 시스템 씬 빌더 — 탑다운 2D 기반 플레이 씬 골격 생성.
/// 싱글톤·플레이어는 GameBootstrap/PlayerController.Bootstrap이 자동 생성하므로 씬에 넣지 않음.
/// 씬에 직접 필요한 것만: 카메라, 2D 라이팅, EventSystem, Tilemap, SpawnPoint.
///
/// Tools > BRB > Build Scene > InGame Scene (또는 Safehouse Scene)
/// </summary>
public static class GameSceneBuilder
{
    const string SCENE_DIR = "Assets/Scenes";

    // ── 공통: 씬 어느 타입이든 동일하게 들어가는 것 ──────────────────

    [MenuItem("Tools/BRB/Build Scene/InGame Scene")]
    static void BuildInGame() => BuildScene("InGameScene", SceneType.InGame);

    [MenuItem("Tools/BRB/Build Scene/Safehouse Scene")]
    static void BuildSafehouse() => BuildScene("SafehouseScene", SceneType.Safehouse);

    // ─────────────────────────────────────────────────────────────────

    enum SceneType { InGame, Safehouse }

    static void BuildScene(string sceneName, SceneType type)
    {
        Directory.CreateDirectory(SCENE_DIR);
        string path = $"{SCENE_DIR}/{sceneName}.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── 1. 카메라 ────────────────────────────────────────────────
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
        // Y축 정렬 — 화면 아래쪽(Y 작은) 스프라이트가 앞으로
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = new Vector3(0, 1, 0);
        camGo.AddComponent<AudioListener>();

        // 플레이어 추적 (플레이어 스폰 후 자동 재탐색)
        camGo.AddComponent<CameraFollow>();

        // ── 2. EventSystem ────────────────────────────────────────────
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ── 3. 글로벌 라이팅 (밤 앰비언트) ───────────────────────────
        var lightRoot = new GameObject("Lighting");
        var gl = lightRoot.AddComponent<Light2D>();
        gl.lightType = Light2D.LightType.Global;
        gl.intensity = 0.04f;
        gl.color = new Color(0.3f, 0.35f, 0.4f);

        // ── 4. Tilemap 루트 ───────────────────────────────────────────
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

        Debug.Log($"[GameSceneBuilder] 생성 완료: {path}");
        Debug.Log($"[GameSceneBuilder] 포함: 카메라(CameraFollow2D), EventSystem, GlobalLight2D(어둠), Grid(바닥+벽 Tilemap), SpawnPoint");
        Debug.Log($"[GameSceneBuilder] 자동 생성(코드): Player(Resources/Player), GameBootstrap 싱글톤 14개");
        Debug.Log($"[GameSceneBuilder] 다음 할 일: ① Tilemap에 타일 배치 ② SpawnPoint 위치 조정 ③ Player 프리팹 확인");
    }
}
#endif
