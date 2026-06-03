using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 레이어를 코드로 보장(없으면 빈 유저 슬롯에 생성).
/// 파괴 가능 오브젝트의 Hurtbox는 "Destructible" 레이어에 두고,
/// 플레이어 공격 마스크(enemyMask)에 이 레이어를 추가해 직접 때려 부순다.
/// </summary>
public static class GameLayers
{
    public const string Destructible = "Destructible";

    [MenuItem("Tools/TopDown/Setup/Ensure 'Destructible' Layer")]
    public static int EnsureDestructible() => EnsureLayer(Destructible);

    /// <summary>레이어 이름을 보장하고 인덱스를 반환. 실패 시 -1.</summary>
    public static int EnsureLayer(string layerName)
    {
        if (string.IsNullOrEmpty(layerName)) return -1;

        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0) { Debug.LogWarning("[GameLayers] TagManager.asset 못 찾음."); return -1; }

        var tagManager = new SerializedObject(asset[0]);
        var layers = tagManager.FindProperty("layers");
        if (layers == null) { Debug.LogWarning("[GameLayers] layers 프로퍼티 없음."); return -1; }

        // 유저 레이어 슬롯(8~31)에서 빈 칸 찾기
        for (int i = 8; i < layers.arraySize; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (sp != null && string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log($"[GameLayers] '{layerName}' 레이어 생성(슬롯 {i}).");
                return i;
            }
        }

        Debug.LogWarning($"[GameLayers] 빈 유저 레이어 슬롯이 없어 '{layerName}' 생성 실패.");
        return -1;
    }

    [MenuItem("Tools/TopDown/Setup/Ensure 'Ceiling' Sorting Layer")]
    public static void EnsureCeilingSortingLayer() => EnsureSortingLayer("Ceiling");

    /// <summary>Sorting Layer 이름 보장(없으면 맨 끝에 추가 = 최상단 렌더). 천장 등 최상위 정렬용.
    /// uniqueID는 비-0 필수(0은 빌트인 Default와 충돌해 드롭다운에 안 뜸). 깨진(0) 항목은 복구.</summary>
    public static void EnsureSortingLayer(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("m_SortingLayers");
        if (layers == null) return;

        // 이름 기반 비-0 uniqueID(FNV-1a). 0 금지(Default와 충돌).
        uint uid = 2166136261u;
        foreach (char c in name) uid = (uid ^ c) * 16777619u;
        if (uid == 0u) uid = 1u;

        // 이미 있으면: uniqueID가 0(깨진 항목)일 때만 복구하고 종료.
        for (int i = 0; i < layers.arraySize; i++)
        {
            var e = layers.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("name").stringValue == name)
            {
                var idp = e.FindPropertyRelative("uniqueID");
                if (idp != null && idp.longValue == 0)
                {
                    idp.longValue = uid;
                    so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[GameLayers] Sorting Layer '{name}' uniqueID 복구({uid}).");
                }
                return;
            }
        }

        int idx = layers.arraySize;
        layers.InsertArrayElementAtIndex(idx);
        var el = layers.GetArrayElementAtIndex(idx);
        el.FindPropertyRelative("name").stringValue = name;
        el.FindPropertyRelative("uniqueID").longValue = uid;   // uint이므로 longValue로 써야 안전(intValue는 0 됨)
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"[GameLayers] Sorting Layer '{name}' 생성(uid {uid}, 맨 끝=최상단).");
    }
}
