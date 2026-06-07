using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// 탑다운 2D 맵 편집용 씬을 생성한다.
/// 구성: 2D Orthographic 카메라 + URP Global Light 2D + Grid(Ground/Walls Tilemap).
/// Walls Tilemap엔 TilemapCollider2D(+정적 Rigidbody2D)가 붙어 "못 가는 곳"이 된다.
/// 프롭은 Prop Catalog(Tools ▸ TopDown ▸ Map ▸ Prop Catalog)의 '씬에 배치'로 찍는다.
/// 메뉴: Tools ▸ TopDown ▸ Build ▸ Map Tool Scene
/// </summary>
public static class MapTool2DSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MapTool2D.unity";

    [MenuItem("Tools/TopDown/빌드/맵 편집 씬")]
    public static void CreateMapToolScene()
    {
        var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);  // 현재 씬 유지(폴더에만 생성)

        // ── 카메라 없음 — PlayerRig(자동 스폰)가 한 세트로 들고 옴 ──
        //    편집은 씬뷰에서 하므로 게임 카메라 불필요. Play 시 PlayerRig 카메라가 따라옴.

        // ── URP Global Light 2D (라이트는 씬 담당! 없으면 스프라이트가 까맣게 나옴) ──
        //    PlayerRig는 플레이어 주변 점광만 들고 옴 → 글로벌 앰비언트는 씬이 둬야 함.
        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // ── Grid + Tilemap ──
        var gridGo = new GameObject("Grid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        CreateTilemap("Ground", gridGo.transform, sortingOrder: 0, withCollider: false);
        CreateTilemap("Walls", gridGo.transform, sortingOrder: 10, withCollider: true);

        // 맵 부모 오브젝트(배치된 프롭이 이 하위로) — 카탈로그 '맵 저장'이 이걸 프리팹으로 저장.
        new GameObject("Map");

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneBuildUtil.SaveAndClose(scene, ScenePath, prevActive);  // 저장 후 닫기(현재 씬 유지)
        Debug.Log($"[MapTool2D] 맵툴 씬 생성: {ScenePath}");
    }

    static void CreateTilemap(string name, Transform parent, int sortingOrder, bool withCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = sortingOrder;

        if (withCollider)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>(); // 칠한 타일이 곧 막힘
        }
    }
}
