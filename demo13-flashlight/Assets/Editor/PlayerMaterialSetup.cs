using UnityEditor;
using UnityEngine;

/// <summary>
/// 플레이어 머티리얼(BRB/PlayerSprite) 생성 + PlayerRig 프리팹의 PlayerSprite에 적용.
/// 메뉴: Tools ▸ TopDown ▸ Setup ▸ Create & Assign Player Material
/// 셰이더에 _FlashAmount가 있어 HitFlash가 이 머티리얼을 그대로 써서 적중 시 흰색 깜빡(SpriteFlash 교체 안 함).
/// </summary>
public static class PlayerMaterialSetup
{
    const string MatPath = "Assets/Resources/Materials/PlayerSprite.mat";
    const string RigPath = "Assets/Resources/PlayerRig.prefab";

    [MenuItem("Tools/TopDown/초기설정/플레이어 머티리얼 생성")]
    static void Run()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("플레이어 머티리얼",
                "BRB/PlayerSprite 셰이더를 못 찾았습니다. 셰이더 컴파일 에러가 없는지 확인하세요.", "확인");
            return;
        }

        // 1) 머티리얼 생성/갱신
        EnsureFolder("Assets/Resources/Materials");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "PlayerSprite" };
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        else mat.shader = shader;
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // 2) PlayerRig 프리팹의 PlayerSprite에 적용
        int applied = 0;
        var root = PrefabUtility.LoadPrefabContents(RigPath);
        if (root != null)
        {
            // 우선 이름에 "PlayerSprite" 포함하는 렌더러
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                if (sr.gameObject.name.Contains("PlayerSprite")) { sr.sharedMaterial = mat; applied++; }
            }
            // 폴백: 못 찾으면 'Bar'/그림자/오버레이 아닌 첫 바디 스프라이트
            if (applied == 0)
            {
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr == null) continue;
                    string nm = sr.gameObject.name;
                    if (nm.Contains("Bar") || nm.Contains("Shadow") || nm.Contains("Overlay")) continue;
                    sr.sharedMaterial = mat; applied++; break;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, RigPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
        else
        {
            Debug.LogWarning($"[PlayerMaterial] PlayerRig 프리팹을 못 열었습니다: {RigPath} — 머티리얼만 생성됨, 수동 할당하세요.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = mat;
        EditorUtility.FocusProjectWindow();
        Debug.Log($"[PlayerMaterial] '{MatPath}' 준비 완료. PlayerRig SpriteRenderer {applied}개에 적용.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
