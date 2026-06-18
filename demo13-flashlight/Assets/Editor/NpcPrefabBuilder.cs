#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NPC를 "맵에 박기 전" 프리팹으로 굽는다. (Tools ▸ TopDown ▸ 콘텐츠 ▸ NPC 프리팹 빌드)
/// Resources/Data/NPC 의 모든 NPCData마다 gb_npc 비주얼 + NPCController(데이터 연결) + InteractableObject(NPC)
/// 를 묶어 Resources/NPC/{npcId}.prefab 으로 저장 → 씬에 드래그만 하면 되는 독립 프리팹.
/// 멱등: 같은 경로로 다시 구우면 덮어씀. NPCData는 SafehouseNpcBuilder/NPC Maker로 먼저 만든 것 사용.
/// </summary>
public static class NpcPrefabBuilder
{
    const string NpcDataDir = "Assets/Resources/Data/NPC";
    const string OutDir     = "Assets/Resources/NPC";
    const string BasePrefab = "Assets/Resources/Props2D/Prefabs/gb_npc.prefab";

    [MenuItem("Tools/TopDown/콘텐츠/NPC 프리팹 빌드")]
    public static void Build()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog("NPC 프리팹 빌드", $"기본 프리팹 없음:\n{BasePrefab}\n\n그레이박스 팔레트(Tools▸TopDown▸맵▸그레이박스 팔레트 생성)를 먼저 생성하세요.", "확인");
            return;
        }
        EnsureFolder(OutDir);

        var guids = AssetDatabase.FindAssets("t:NPCData", new[] { NpcDataDir });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("NPC 프리팹 빌드", $"{NpcDataDir} 에 NPCData가 없습니다.\n'안전가옥 NPC 빌드'/NPC Maker로 먼저 생성하세요.", "확인");
            return;
        }

        int n = 0;
        var made = new System.Text.StringBuilder();
        foreach (var g in guids)
        {
            var dataPath = AssetDatabase.GUIDToAssetPath(g);
            var data = AssetDatabase.LoadAssetAtPath<NPCData>(dataPath);
            if (data == null || string.IsNullOrEmpty(data.npcId)) continue;

            // gb_npc 복제(프리팹 링크 없는 독립 인스턴스) → 데이터 주입 → 프리팹으로 저장.
            var inst = (GameObject)Object.Instantiate(basePrefab);
            inst.name = data.npcId;

            // NPCController: npcData + storyNpcId
            var npc = inst.GetComponentInChildren<NPCController>(true);
            if (npc != null)
            {
                var so = new SerializedObject(npc);
                var dp  = so.FindProperty("npcData");    if (dp  != null) dp.objectReferenceValue = data;
                var sid = so.FindProperty("storyNpcId"); if (sid != null) sid.stringValue = data.npcId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // InteractableObject: NPC 타입 + 프롬프트
            var io = inst.GetComponentInChildren<InteractableObject>(true);
            if (io != null)
            {
                var so = new SerializedObject(io);
                var t  = so.FindProperty("type");       if (t  != null) t.enumValueIndex = (int)InteractableObject.InteractType.NPC;
                var pt = so.FindProperty("promptText"); if (pt != null) pt.stringValue = "대화";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 라벨(자식 TextMesh "Label") = 표시 이름
            var label = inst.transform.Find("Label");
            if (label != null)
            {
                var tm = label.GetComponent<TextMesh>();
                if (tm != null) tm.text = data.displayName;
            }

            string outPath = $"{OutDir}/{data.npcId}.prefab";
            PrefabUtility.SaveAsPrefabAsset(inst, outPath);
            Object.DestroyImmediate(inst);
            n++;
            made.AppendLine($"  • {data.npcId}  ({data.displayName})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[NpcPrefab]</color> NPC 프리팹 {n}개 → {OutDir}\n{made}");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("NPC 프리팹 빌드",
                $"{OutDir} 에 NPC 프리팹 {n}개 생성/갱신:\n\n{made}\n각 프리팹은 NPCData·NPC 상호작용·이름표가 연결돼 있어 씬에 드래그만 하면 됩니다.", "확인");
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
