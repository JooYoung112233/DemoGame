#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 뒤질 수 있는 상자(루팅 컨테이너) 부착 헬퍼 — 지역1 빌더(나무상자·잡동사니·좌판·트렁크)가 쓴다.
///
/// 원래는 건물 '내부' 씬(Int_*) 공용 빌더였다. 2026-09-12 실내 씬 삭제(docs/building-interior.md)와
/// 시스템 정리 5단계(2D 경로 삭제)로 실내 씬 API(Begin·Shell·Exit·Gate·Controller 등)를 걷어내고
/// 외부에서 쓰는 부분만 남겼다.
/// </summary>
public static class InteriorBuild
{
    /// <summary>오브젝트를 뒤질 수 있는 상자로 — LootContainer(종류·이름) + 상호작용 + Container 앵커.</summary>
    public static void MakeSearchable(GameObject go, string kind, string label = null, string prompt = null,
                                      int w = 3, int h = 2, float radius = 1.8f)
    {
        var lc = go.GetComponent<LootContainer>();
        if (lc == null) lc = go.AddComponent<LootContainer>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        lc.Setup(label ?? KindLabel(kind), w, h);
        lc.SetLootKind(kind);

        var io = go.GetComponent<InteractableObject>();
        if (io == null) io = go.AddComponent<InteractableObject>();
        io.Configure(InteractableObject.InteractType.Container, prompt ?? KindPrompt(kind), radius);

        var sp = go.GetComponent<ItemSpawnPoint>();
        if (sp == null) sp = go.AddComponent<ItemSpawnPoint>();
        SetType(sp, 1);   // Container
    }

    public static string KindLabel(string kind) => kind switch
    {
        "register" => "계산대", "safe" => "금고", "crate" => "나무상자", "junk" => "길가 잡동사니",
        "trunk" => "자동차 트렁크", "stall" => "좌판", _ => "상자",
    };

    public static string KindPrompt(string kind) => kind switch
    {
        "register" => "계산대 뒤지기", "safe" => "금고 열기", "trunk" => "트렁크 뒤지기", "stall" => "좌판 뒤지기",
        _ => "뒤지기",
    };

    static void SetType(ItemSpawnPoint sp, int type)
    {
        var so = new SerializedObject(sp);
        var t = so.FindProperty("spawnType");
        if (t != null) { t.enumValueIndex = type; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    public static Transform Find(Transform t, string n)
    {
        if (t.name == n) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = Find(t.GetChild(i), n);
            if (r != null) return r;
        }
        return null;
    }
}
#endif
