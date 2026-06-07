using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// 메인 컨트롤 패널. (Tools ▸ TopDown ▸ Control Panel)
/// - 튜닝: `GameTuning`(Resources/Data) 한 곳에서 수색속도·낮밤시간·레이드시간·드랍배율 등 조절.
///   필드 추가는 GameTuning.cs에 하면 여기 자동 노출(SerializedObject 제너릭 드로우).
/// - 맵 통계: 현재 열린 씬의 오브젝트 개수(인터랙터블 종류별·루트상자·스폰·적·존·라이트·스프라이트…).
/// 플레이 중에도 실시간 튜닝 가능(수색속도는 즉시, 낮밤/레이드는 다음 init부터).
/// </summary>
public class GameControlPanel : EditorWindow
{
    const string TUNING_PATH = "Assets/Resources/Data/GameTuning.asset";

    [MenuItem("Tools/TopDown/컨트롤 패널")]
    static void Open()
    {
        var w = GetWindow<GameControlPanel>("Control Panel");
        w.minSize = new Vector2(360, 480);
    }

    Vector2 scroll;
    bool fTuning = true, fStats = true;
    GameTuning tuning;
    SerializedObject tuningSo;

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // ── 튜닝 ──────────────────────────────────────────────────────
        fTuning = EditorGUILayout.Foldout(fTuning, "⚙ 게임 튜닝 (GameTuning)", true, EditorStyles.foldoutHeader);
        if (fTuning) DrawTuning();

        EditorGUILayout.Space(10);

        // ── 맵 통계 ───────────────────────────────────────────────────
        fStats = EditorGUILayout.Foldout(fStats, "📊 맵 통계 (열린 씬)", true, EditorStyles.foldoutHeader);
        if (fStats) DrawMapStats();

        EditorGUILayout.EndScrollView();
    }

    // ── 튜닝 ──────────────────────────────────────────────────────────
    void DrawTuning()
    {
        if (tuning == null) tuning = GameTuning.Instance; // Resources/Data/GameTuning
        if (tuning == null)
        {
            EditorGUILayout.HelpBox("GameTuning 에셋이 없습니다. 생성하면 모든 시스템이 이 값을 읽습니다(없으면 각자 기본값).", MessageType.Info);
            if (GUILayout.Button("GameTuning 에셋 생성", GUILayout.Height(26)))
                CreateTuning();
            return;
        }

        if (tuningSo == null || tuningSo.targetObject != tuning) tuningSo = new SerializedObject(tuning);
        tuningSo.Update();
        EditorGUI.indentLevel++;
        var p = tuningSo.GetIterator();
        p.NextVisible(true);                 // m_Script 스킵
        while (p.NextVisible(false))
            EditorGUILayout.PropertyField(p, true);
        EditorGUI.indentLevel--;
        if (tuningSo.ApplyModifiedProperties())
            EditorUtility.SetDirty(tuning);

        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("에셋 선택")) EditorGUIUtility.PingObject(tuning);
            if (GUILayout.Button("저장")) AssetDatabase.SaveAssets();
        }
        EditorGUILayout.HelpBox("플레이 중에도 즉시 반영(수색속도). 낮밤/레이드 시간은 다음 진입/레이드부터.\n값 추가: GameTuning.cs에 필드 추가 → 쓰는 시스템에서 읽기 → 여기 자동 노출.", MessageType.None);
    }

    void CreateTuning()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        var asset = ScriptableObject.CreateInstance<GameTuning>();
        AssetDatabase.CreateAsset(asset, TUNING_PATH);
        AssetDatabase.SaveAssets();
        tuning = asset; tuningSo = null;
        EditorGUIUtility.PingObject(asset);
        Debug.Log("[ControlPanel] GameTuning 생성: " + TUNING_PATH);
    }

    // ── 맵 통계 ───────────────────────────────────────────────────────
    void DrawMapStats()
    {
        EditorGUI.indentLevel++;
        if (GUILayout.Button("새로고침 / 집계", GUILayout.Height(22))) Repaint();

        // 인터랙터블: 종류별 집계
        var interactables = FindObjectsByType<InteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var byType = new Dictionary<InteractableObject.InteractType, int>();
        foreach (var io in interactables)
        {
            if (io == null) continue;
            byType.TryGetValue(io.Type, out int c);
            byType[io.Type] = c + 1;
        }

        EditorGUILayout.LabelField($"인터랙터블 총 {interactables.Length}개", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        foreach (var kv in byType)
            EditorGUILayout.LabelField($"· {kv.Key}", kv.Value.ToString());
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4);
        Row("루트 상자(LootContainer)", Count<LootContainer>());
        Row("스폰 포인트(SpawnPoint)", Count<SpawnPoint>());
        Row("적(EnemyController)", Count<EnemyController>());
        Row("아이템 스폰(ItemSpawnPoint)", Count<ItemSpawnPoint>());
        Row("월드 아이템(WorldItem)", Count<WorldItem>());
        Row("파괴 가능(Breakable)", Count<Breakable>());
        Row("Light2D", Count<Light2D>());
        Row("Tilemap", Count<Tilemap>());
        Row("SpriteRenderer(총)", Count<SpriteRenderer>());
        EditorGUI.indentLevel--;

        EditorGUILayout.HelpBox("열린 씬 전체 기준(Systems + 게임플레이 씬이 같이 열려 있으면 합산). 드랍 수량은 GameTuning.lootCountMult로 조절.", MessageType.None);
    }

    static int Count<T>() where T : Object
        => FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

    static void Row(string label, int value)
        => EditorGUILayout.LabelField(label, value.ToString());
}
