#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 탑다운 2D 검증용 테스트 씬을 한 번에 생성.
/// 2D 카메라 + 바닥 + 벽(Collider2D) + 플레이어(Rigidbody2D+TopDownPlayer2D) + 2D 손전등.
/// Tools > TopDown > Build Test Scene
/// </summary>
public static class TopDownTestSceneBuilder
{
    const string SpriteDir = "Assets/Scripts/TopDown/Sprites";
    const string ScenePath = "Assets/Scripts/TopDown/TopDownTest.unity";

    [MenuItem("Tools/TopDown/Build Test Scene")]
    static void Build()
    {
        Sprite square = GetSprite("td_square", DrawSquare);
        Sprite circle = GetSprite("td_circle", DrawCircle);
        Material lit = LitMat();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2D 카메라 (정탑다운)
        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.1f, 0.11f, 0.14f);
        // 80° 고각도 탑다운: 아래쪽(작은 Y) 스프라이트가 앞으로 → Y축 정렬
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = new Vector3(0, 1, 0);
        camGo.AddComponent<AudioListener>();

        // 바닥 (큰 사각형, 어두운 회색)
        MakeSprite("Floor", square, lit, new Color(0.4f, 0.4f, 0.45f), Vector2.zero, new Vector2(20, 20), -10, false);

        // 벽 (ㄷ자, Collider2D)
        Vector2[][] walls = {
            new[]{ new Vector2(-4, 3), new Vector2(8, 0.5f) },   // 위 가로
            new[]{ new Vector2(-4, -3), new Vector2(8, 0.5f) },  // 아래 가로
            new[]{ new Vector2(-4, 0), new Vector2(0.5f, 6.5f) },// 좌 세로
        };
        foreach (var w in walls)
            MakeSprite($"Wall_{w[0].x}_{w[0].y}", square, lit, new Color(0.6f, 0.5f, 0.45f), w[0], w[1], 0, true);

        // 플레이어
        var p = MakeSprite("Player", circle, lit, new Color(0.3f, 0.8f, 0.9f), new Vector2(0, 0), Vector2.one, 10, false);
        var rb = p.AddComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.freezeRotation = true;
        var col = p.AddComponent<CapsuleCollider2D>(); col.size = new Vector2(0.8f, 0.8f);
        var mover = p.AddComponent<TopDownPlayer2D>();
        var so = new SerializedObject(mover);
        so.FindProperty("sprite").objectReferenceValue = p.GetComponent<SpriteRenderer>();
        so.FindProperty("moveSpeed").floatValue = 5f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 라이팅 — 글로벌(밤) + 손전등
        var glGo = new GameObject("GlobalLight2D");
        var gl = glGo.AddComponent<Light2D>(); gl.lightType = Light2D.LightType.Global;
        gl.intensity = 0.18f; gl.color = new Color(0.5f, 0.55f, 0.7f);

        var flGo = new GameObject("Flashlight2D"); flGo.transform.SetParent(p.transform, false);
        var fl = flGo.AddComponent<Light2D>(); fl.lightType = Light2D.LightType.Point;
        fl.pointLightInnerRadius = 0.5f; fl.pointLightOuterRadius = 7f;
        fl.pointLightInnerAngle = 30f; fl.pointLightOuterAngle = 55f;
        fl.intensity = 1.4f; fl.color = new Color(1f, 0.95f, 0.8f);
        fl.shadowsEnabled = true; fl.shadowIntensity = 1f;
        var tf = flGo.AddComponent<TopDownFlashlight2D>();
        var tfso = new SerializedObject(tf);
        tfso.FindProperty("angleOffset").floatValue = -90f;
        tfso.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeObject = null;
        EditorSceneManager.MarkSceneDirty(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[TopDown] 생성 완료: {ScenePath} — Play 후 WASD 이동, 마우스로 손전등. (벽 충돌, 2D 손전등)");
        Debug.Log("[TopDown] ⚠️ URP를 URP-2D로 전환해야 2D 라이트가 보입니다 (Project Settings > Graphics/Quality).");
    }

    static GameObject MakeSprite(string name, Sprite sprite, Material mat, Color color, Vector2 pos, Vector2 scale, int order, bool collider)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sharedMaterial = mat; sr.sortingOrder = order;
        if (collider)
        {
            var bc = go.AddComponent<BoxCollider2D>();
        }
        return go;
    }

    static Material _lit;
    static Material LitMat()
    {
        if (_lit != null) return _lit;
        string path = $"{SpriteDir}/SpriteLit2D.mat";
        _lit = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (_lit == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default") ?? Shader.Find("Sprites/Default");
            Directory.CreateDirectory(SpriteDir);
            _lit = new Material(sh);
            AssetDatabase.CreateAsset(_lit, path);
        }
        return _lit;
    }

    static Sprite GetSprite(string name, System.Action<Texture2D> draw)
    {
        Directory.CreateDirectory(SpriteDir);
        string path = $"{SpriteDir}/{name}.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            draw(tex); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 64;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void DrawSquare(Texture2D t)
    {
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) t.SetPixel(x, y, Color.white);
    }

    static void DrawCircle(Texture2D t)
    {
        Vector2 c = new Vector2(32, 32);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                if (Vector2.Distance(new Vector2(x, y), c) <= 30) t.SetPixel(x, y, Color.white);
    }
}
#endif
