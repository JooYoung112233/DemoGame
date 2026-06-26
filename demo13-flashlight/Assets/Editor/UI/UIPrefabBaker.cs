#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 코드 생성 UI → 프리팹 베이크 (ui-prefab-plan.md '프리팹 우선' 전환).
/// 각 패널의 EditorBake()로 계층을 1회 생성 → SaveAsPrefabAsset로 Resources/UI/에 저장.
/// 런타임 부트스트랩은 이 프리팹을 Instantiate(프리팹 없으면 코드 생성 폴백).
///
/// 메뉴를 패널별로 추가하며 점진 전환. (PoC: ItemDetail)
/// </summary>
public static class UIPrefabBaker
{
    const string OutDir = "Assets/Resources/UI";

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/ItemDetail")]
    public static void BakeItemDetail()
    {
        Bake<ItemDetailUI>("ItemDetailUI", c => c.EditorBake());
    }

    [MenuItem("Tools/TopDown/UI/프리팹 베이크/NoteUI")]
    public static void BakeNoteUI()
    {
        Bake<NoteUI>("NoteUI", c => c.EditorBake());
    }

    // ── 공통 베이크 ────────────────────────────────────────
    static void Bake<T>(string assetName, System.Action<T> build) where T : Component
    {
        EnsureDir();
        var go = new GameObject(assetName);
        try
        {
            var comp = go.AddComponent<T>();   // 에디터 모드 → Awake 미호출, EditorBake가 명시 생성
            build(comp);
            string path = $"{OutDir}/{assetName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log($"[UIPrefabBaker] 베이크 완료 → {path}");
        }
        finally
        {
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
        }
    }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutDir))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
    }
}
#endif
