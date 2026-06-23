using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// 밸런스·컨트롤 통합 패널. (Tools ▸ TopDown ▸ 밸런스·컨트롤)
/// 탭 2개 — 둘 다 "밸런스 컨트롤"이라 한 창으로 합침:
///   ① 컨트롤: GameTuning(수색속도·낮밤·레이드·드랍배율) + StatDB(플레이어/적 스탯) + 맵 통계.
///   ② CSV 밸런스: tools/ 의 items·quests·region_loot + tools/balance/*.csv 격자 편집(엑셀 양방향).
///      ⚠️ 셀에 콤마(,) 금지(구분자). 복수값 = ; / | / ·. 저장 후 생성기로 SO 반영.
/// </summary>
public class GameControlPanel : EditorWindow
{
    const string TUNING_PATH = "Assets/Resources/Data/GameTuning.asset";
    static string ToolsDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../tools"));

    static readonly string[] TabNames = { "컨트롤 (튜닝·스탯·통계)", "NPC·상점", "몬스터", "지역 루트", "CSV 밸런스", "전투", "도구·검증" };
    int tab;
    readonly AttackDataEditorWindow attackTab = new AttackDataEditorWindow();   // 전투 탭(공격 에디터 임베드)

    [MenuItem("Tools/TopDown/밸런스·컨트롤")]
    static void Open()
    {
        var w = GetWindow<GameControlPanel>("밸런스·컨트롤");
        w.minSize = new Vector2(1000, 560);
    }

    void OnEnable() => RefreshFiles();

    void OnGUI()
    {
        EditorGUILayout.Space(3);
        tab = GUILayout.Toolbar(tab, TabNames, GUILayout.Height(26));
        DrawSeparator(3);

        if (tab == 0) DrawControlTab();
        else if (tab == 1) DrawNpcTab();
        else if (tab == 2) DrawMonsterTab();
        else if (tab == 3) DrawRegionLootTab();
        else if (tab == 4) DrawCsvTab();
        else if (tab == 5) DrawCombatTab();
        else          DrawToolsTab();
    }

    // ══════════════════════════════════════════════════════════════════
    //  탭 — 전투 (공격/콤보 AttackData 에디터 임베드)
    // ══════════════════════════════════════════════════════════════════
    void DrawCombatTab() => attackTab.Draw(Repaint);

    // ══════════════════════════════════════════════════════════════════
    //  탭 — 도구·검증 (데이터 생성기 + 자가검증)
    // ══════════════════════════════════════════════════════════════════
    void DrawToolsTab()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("데이터 생성", EditorStyles.boldLabel);
        if (GUILayout.Button("특성 SO 재생성 (CSV→AssetDatabase)", GUILayout.Height(26)))
            TraitDataImporter.Generate();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("자가검증 (결과는 Console)", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("특성 시스템", GUILayout.Height(26))) TraitManagerSelfTest.Run();
            if (GUILayout.Button("평판 시스템", GUILayout.Height(26))) ReputationSelfTest.Run();
        }
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("모든 컨트롤·밸런스·데이터 도구는 이 패널 한 곳에서. (별도 메뉴 폐지)", MessageType.None);
    }

    // ══════════════════════════════════════════════════════════════════
    //  탭 1 — 컨트롤 (GameTuning + StatDB + 맵 통계)
    // ══════════════════════════════════════════════════════════════════
    Vector2 ctrlScroll;
    bool fTuning = true, fStatDB = true, fStats = true;
    GameTuning tuning;
    SerializedObject tuningSo;
    StatDB statDB;
    SerializedObject statDBSo;

    void DrawControlTab()
    {
        ctrlScroll = EditorGUILayout.BeginScrollView(ctrlScroll);

        fTuning = EditorGUILayout.Foldout(fTuning, "⚙ 게임 튜닝 (GameTuning)", true, EditorStyles.foldoutHeader);
        if (fTuning) DrawTuning();

        EditorGUILayout.Space(10);
        fStatDB = EditorGUILayout.Foldout(fStatDB, "🎮 플레이어 스탯 (StatDB)", true, EditorStyles.foldoutHeader);
        if (fStatDB) DrawStatDB();

        EditorGUILayout.Space(10);
        fStats = EditorGUILayout.Foldout(fStats, "📊 맵 통계 (열린 씬)", true, EditorStyles.foldoutHeader);
        if (fStats) DrawMapStats();

        EditorGUILayout.EndScrollView();
    }

    // ── 튜닝: [Header] 그룹 미러(GameTuning.cs 필드 순서와 1:1 일치) ──
    //  GameTuning에 필드 추가 시 여기도 맞춰 추가할 것(누락 필드는 우측 컬럼 하단 '기타'에 자동 노출).
    struct TuneGroup { public string title; public Color accent; public string[] fields; }

    static readonly Color TG_Search   = new Color(0.22f, 0.40f, 0.52f); // 청록 — 수색
    static readonly Color TG_Time     = new Color(0.42f, 0.36f, 0.55f); // 보라 — 시간/현상
    static readonly Color TG_Raid     = new Color(0.52f, 0.32f, 0.30f); // 적갈 — 레이드
    static readonly Color TG_Loot     = new Color(0.30f, 0.46f, 0.30f); // 초록 — 드랍
    static readonly Color TG_Survival = new Color(0.52f, 0.44f, 0.24f); // 황토 — 생존
    static readonly Color TG_Sleep4   = new Color(0.28f, 0.42f, 0.50f); // 청 — 수면4h
    static readonly Color TG_Sleep8   = new Color(0.24f, 0.36f, 0.50f); // 짙은 청 — 수면8h
    static readonly Color TG_Anomaly  = new Color(0.50f, 0.30f, 0.46f); // 자홍 — 짙은현상
    static readonly Color TG_Shop     = new Color(0.42f, 0.40f, 0.24f); // 금 — 상점

    // 좌측 컬럼 그룹
    static readonly TuneGroup[] TuneColLeft =
    {
        new TuneGroup { title = "수색 (루팅 상자)", accent = TG_Search, fields = new[]
            { "searchSpeedMult", "searchSecCommon", "searchSecUncommon", "searchSecRare", "searchSecEpic", "searchSecLegendary" } },
        new TuneGroup { title = "시간 / 현상 (지역 낮·밤, 초)", accent = TG_Time, fields = new[]
            { "dayDuration", "nightDuration" } },
        new TuneGroup { title = "레이드", accent = TG_Raid, fields = new[]
            { "raidDuration" } },
        new TuneGroup { title = "드랍 (맵 전체)", accent = TG_Loot, fields = new[]
            { "lootCountMult", "lootChanceMult", "valuableWeightMult", "itemSpawnCountMult" } },
        new TuneGroup { title = "생존 (레이드 중 차감)", accent = TG_Survival, fields = new[]
            { "survivalWaterMinutesToEmpty", "survivalSatietyMinutesToEmpty", "survivalStarveHpPerSec" } },
    };

    // 우측 컬럼 그룹
    static readonly TuneGroup[] TuneColRight =
    {
        new TuneGroup { title = "수면 4시간", accent = TG_Sleep4, fields = new[]
            { "sleep4hHpPct", "sleep4hWater", "sleep4hSatiety" } },
        new TuneGroup { title = "수면 8시간", accent = TG_Sleep8, fields = new[]
            { "sleep8hHpPct", "sleep8hWater", "sleep8hSatiety" } },
        new TuneGroup { title = "짙은현상 (구간)", accent = TG_Anomaly, fields = new[]
            { "anomalyActiveDuration", "anomalyTelegraph", "anomalyWarning", "anomalyCollapse",
              "anomalyIntervalMin", "anomalyIntervalMax", "anomalyMaxConcurrent", "anomalySpotCountWeights", "anomalyMonsterMinDist" } },
        new TuneGroup { title = "상점 해금 평판 (희귀도별)", accent = TG_Shop, fields = new[]
            { "shopTierRare", "shopTierEpic", "shopTierLegendary" } },
    };

    GUIStyle _tuneSection;
    GUIStyle TuneSectionText => _tuneSection ??= new GUIStyle(EditorStyles.label)
    { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };

    /// <summary>맵툴식 색상 섹션 헤더 바(좌측 강조 라인 + 굵은 흰 글씨).</summary>
    void TuneSectionHeader(string title, Color accent)
    {
        EditorGUILayout.Space(6);
        var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, accent);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), accent * 1.6f);
        GUI.Label(new Rect(rect.x + 9, rect.y, rect.width - 12, rect.height), title, TuneSectionText);
    }

    void DrawTuning()
    {
        if (tuning == null) tuning = GameTuning.Instance;
        if (tuning == null)
        {
            EditorGUILayout.HelpBox("GameTuning 에셋이 없습니다. 생성하면 모든 시스템이 이 값을 읽습니다(없으면 각자 기본값).", MessageType.Info);
            if (GUILayout.Button("GameTuning 에셋 생성", GUILayout.Height(26))) CreateTuning();
            return;
        }

        if (tuningSo == null || tuningSo.targetObject != tuning) tuningSo = new SerializedObject(tuning);
        tuningSo.Update();

        // 이름이 안 잘리게 라벨 폭을 넉넉히(컬럼 폭의 ~58%).
        float prevLabelW = EditorGUIUtility.labelWidth;
        var drawn = new HashSet<string>();

        EditorGUILayout.BeginHorizontal();

        // 좌측 컬럼
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.labelWidth = 215f;
        foreach (var g in TuneColLeft) DrawTuneGroup(g, drawn);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // 우측 컬럼
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.labelWidth = 215f;
        foreach (var g in TuneColRight) DrawTuneGroup(g, drawn);

        // 그룹 정의에 누락된 신규 필드가 있으면 자동으로 우측 하단 '기타'에 노출(빠뜨림 방지).
        DrawTuneLeftovers(drawn);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = prevLabelW;
        if (tuningSo.ApplyModifiedProperties()) EditorUtility.SetDirty(tuning);

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("에셋 선택")) EditorGUIUtility.PingObject(tuning);
            if (GUILayout.Button("저장")) AssetDatabase.SaveAssets();
        }
        EditorGUILayout.HelpBox("플레이 중에도 즉시 반영(수색속도). 낮밤/레이드 시간은 다음 진입/레이드부터.", MessageType.None);
    }

    void DrawTuneGroup(TuneGroup g, HashSet<string> drawn)
    {
        TuneSectionHeader(g.title, g.accent);
        EditorGUI.indentLevel++;
        foreach (var fieldName in g.fields)
        {
            var prop = tuningSo.FindProperty(fieldName);
            if (prop == null)
            {
                EditorGUILayout.LabelField(fieldName, "(필드 없음)");
                continue;
            }
            EditorGUILayout.PropertyField(prop, true);
            drawn.Add(fieldName);
        }
        EditorGUI.indentLevel--;
    }

    void DrawTuneLeftovers(HashSet<string> drawn)
    {
        bool headerDrawn = false;
        var p = tuningSo.GetIterator();
        p.NextVisible(true); // m_Script (skip)
        while (p.NextVisible(false))
        {
            if (p.name == "m_Script" || drawn.Contains(p.name)) continue;
            if (!headerDrawn)
            {
                TuneSectionHeader("기타 (미분류 — 그룹 정의에 추가 필요)", new Color(0.45f, 0.30f, 0.30f));
                EditorGUI.indentLevel++;
                headerDrawn = true;
            }
            EditorGUILayout.PropertyField(p, true);
        }
        if (headerDrawn) EditorGUI.indentLevel--;
    }

    void DrawStatDB()
    {
        if (statDB == null) statDB = StatDB.Instance;
        if (statDB == null)
        {
            EditorGUILayout.HelpBox("StatDB 에셋이 없습니다 (Resources/Data/StatDB.asset).", MessageType.Info);
            return;
        }

        if (statDBSo == null || statDBSo.targetObject != statDB) statDBSo = new SerializedObject(statDB);
        statDBSo.Update();
        EditorGUI.indentLevel++;
        var playerProp = statDBSo.FindProperty("playerStat");   // 적/몬스터(units)는 '몬스터' 탭으로 분리
        if (playerProp != null) DrawKoClass(playerProp);
        EditorGUI.indentLevel--;
        if (statDBSo.ApplyModifiedProperties()) EditorUtility.SetDirty(statDB);

        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("에셋 선택")) EditorGUIUtility.PingObject(statDB);
            if (GUILayout.Button("저장")) AssetDatabase.SaveAssets();
        }
        EditorGUILayout.HelpBox("플레이어 이동속도 = 이동 ▸ 이동 속도. 필드 라벨은 한글(영문 변수명은 라벨 위 툴팁).\n※ 적/몬스터 스탯은 상단 '몬스터' 탭에서 편집.", MessageType.None);
    }

    // ══════════════════════════════════════════════════════════════════
    //  탭 — 몬스터 (StatDB.units 마스터-디테일) — 가짓수 많아 NPC처럼 분리
    // ══════════════════════════════════════════════════════════════════
    int monsterIdx;
    Vector2 monsterListScroll, monsterDetailScroll;
    string monsterFilter = "";
    bool monsterAddReq;        // 레이아웃 종료 후 처리(구조 변경을 스코프 안에서 하지 않음)
    int monsterDelReq = -1;
    UnitStatData monsterPresetReq;   // 프리셋 추가 요청(다음 프레임 SO 재생성 전에 처리)

    void DrawMonsterTab()
    {
        if (statDB == null) statDB = StatDB.Instance;
        if (statDB == null)
        {
            EditorGUILayout.HelpBox("StatDB 에셋이 없습니다 (Resources/Data/StatDB.asset).", MessageType.Info);
            return;
        }

        // 프리셋 추가 요청(이전 프레임 버튼) — SerializedObject 재생성 전에 직접 리스트에 추가.
        if (monsterPresetReq != null)
        {
            var preset = monsterPresetReq; monsterPresetReq = null;
            int existingIdx = statDB.units.FindIndex(u => u != null && u.id == preset.id);
            if (existingIdx >= 0)
            {
                // 같은 id 유닛이 이미 있으면 중복 생성하지 않고 선택만 — 프리셋 반복 클릭 시 _1/_2 누적 방지
                monsterIdx = existingIdx;
                ShowNotification(new GUIContent($"'{preset.id}' 이미 있음 — 중복 생성 안 함(기존 선택)"));
            }
            else
            {
                Undo.RecordObject(statDB, "Add Unit Preset");
                statDB.units.Add(preset);
                EditorUtility.SetDirty(statDB);
                AssetDatabase.SaveAssets();
                statDBSo = null;                 // 새 유닛 반영 위해 재생성
                monsterIdx = statDB.units.Count - 1;
            }
        }

        if (statDBSo == null || statDBSo.targetObject != statDB) statDBSo = new SerializedObject(statDB);
        statDBSo.Update();

        var units = statDBSo.FindProperty("units");
        if (units == null) { EditorGUILayout.HelpBox("StatDB.units 를 찾을 수 없습니다.", MessageType.Warning); return; }

        monsterAddReq = false;
        monsterDelReq = -1;

        // ── 상단 바: 개수 + 새 몬스터 + 에셋/저장 + 검색 ──
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"몬스터/적 유닛  {units.arraySize}종 (StatDB ▸ Units)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.40f, 0.85f, 0.50f);
            if (GUILayout.Button("＋ 새 몬스터", GUILayout.Width(96))) monsterAddReq = true;
            GUI.backgroundColor = prev;
            // 프리셋(스탯 자동 채움) — bandit_melee/ranged/tank/boss_01
            if (GUILayout.Button("근접", GUILayout.Width(44)))   monsterPresetReq = UnitStatData.PresetMelee();
            if (GUILayout.Button("원거리", GUILayout.Width(52)))  monsterPresetReq = UnitStatData.PresetRanged();
            if (GUILayout.Button("탱크", GUILayout.Width(44)))   monsterPresetReq = UnitStatData.PresetTank();
            if (GUILayout.Button("보스", GUILayout.Width(44)))   monsterPresetReq = UnitStatData.PresetBoss();
            if (GUILayout.Button("에셋 선택", GUILayout.Width(76))) EditorGUIUtility.PingObject(statDB);
            if (GUILayout.Button("저장", GUILayout.Width(56))) { statDBSo.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("🔍", GUILayout.Width(18));
            monsterFilter = EditorGUILayout.TextField(monsterFilter, GUILayout.Width(220));
        }
        DrawSeparator(2);

        if (units.arraySize > 0)
        {
            monsterIdx = Mathf.Clamp(monsterIdx, 0, units.arraySize - 1);

            using (new EditorGUILayout.HorizontalScope())
            {
                // ── 좌: 몬스터 목록(세로 스크롤) ──
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(230)))
                {
                    monsterListScroll = EditorGUILayout.BeginScrollView(monsterListScroll, GUILayout.ExpandHeight(true));
                    DrawMonsterList(units);
                    EditorGUILayout.EndScrollView();
                }

                var vsep = GUILayoutUtility.GetRect(2, 2, GUILayout.Width(2), GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(vsep, CSep);

                // ── 우: 선택 몬스터 상세(2열 섹션) ──
                using (new EditorGUILayout.VerticalScope())
                {
                    monsterDetailScroll = EditorGUILayout.BeginScrollView(monsterDetailScroll);
                    DrawMonsterDetail(units);
                    EditorGUILayout.EndScrollView();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("등록된 몬스터가 없습니다. ‘＋ 새 몬스터’로 추가하세요.", MessageType.Info);
        }

        // ── 구조 변경은 모든 레이아웃 스코프 종료 후 적용(스크롤뷰 불일치 방지) ──
        if (monsterAddReq)
        {
            units.arraySize++;
            int at = units.arraySize - 1;
            var ne = units.GetArrayElementAtIndex(at);
            var idP = ne.FindPropertyRelative("id");          if (idP != null) idP.stringValue = "new_unit";
            var nmP = ne.FindPropertyRelative("displayName"); if (nmP != null) nmP.stringValue = "New Unit";
            monsterIdx = at;
        }
        else if (monsterDelReq >= 0 && monsterDelReq < units.arraySize)
        {
            units.DeleteArrayElementAtIndex(monsterDelReq);
            if (monsterIdx >= units.arraySize) monsterIdx = Mathf.Max(0, units.arraySize - 1);
        }

        if (statDBSo.ApplyModifiedProperties()) EditorUtility.SetDirty(statDB);
    }

    void DrawMonsterList(SerializedProperty units)
    {
        string f = string.IsNullOrEmpty(monsterFilter) ? null : monsterFilter.ToLowerInvariant();
        for (int i = 0; i < units.arraySize; i++)
        {
            var u = units.GetArrayElementAtIndex(i);
            string id = u.FindPropertyRelative("id")?.stringValue ?? "";
            string nm = u.FindPropertyRelative("displayName")?.stringValue ?? "";
            if (f != null && !id.ToLowerInvariant().Contains(f) && !nm.ToLowerInvariant().Contains(f)) continue;

            bool sel = i == monsterIdx;
            var rect = GUILayoutUtility.GetRect(0, 34, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint && sel) EditorGUI.DrawRect(rect, CSelBg);

            var titleStyle = new GUIStyle(EditorStyles.label)
            { fontStyle = FontStyle.Bold, padding = new RectOffset(8, 2, 0, 0),
              normal = { textColor = sel ? Color.white : EditorStyles.label.normal.textColor } };
            GUI.Label(new Rect(rect.x, rect.y + 2, rect.width - 26, 16), string.IsNullOrEmpty(nm) ? id : nm, titleStyle);
            GUI.Label(new Rect(rect.x + 4, rect.y + 18, rect.width - 30, 13), id, HdrSub);

            // 삭제 요청(실제 삭제는 레이아웃 종료 후)
            if (GUI.Button(new Rect(rect.xMax - 24, rect.y + 8, 20, 18), new GUIContent("✕", "이 몬스터 삭제")))
            {
                if (EditorUtility.DisplayDialog("삭제", $"‘{(string.IsNullOrEmpty(nm) ? id : nm)}’ 몬스터를 삭제할까요?", "삭제", "취소"))
                    monsterDelReq = i;
            }
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width - 26, rect.height), GUIContent.none, GUIStyle.none))
                monsterIdx = i;
        }
    }

    void DrawMonsterDetail(SerializedProperty units)
    {
        var u = units.GetArrayElementAtIndex(monsterIdx);
        string id = u.FindPropertyRelative("id")?.stringValue ?? "";
        string nm = u.FindPropertyRelative("displayName")?.stringValue ?? "";

        var hdr = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(hdr, new Color(0.50f, 0.28f, 0.30f));
        EditorGUI.DrawRect(new Rect(hdr.x, hdr.y, 4f, hdr.height), new Color(0.50f, 0.28f, 0.30f) * 1.6f);
        GUI.Label(new Rect(hdr.x + 9, hdr.y, hdr.width - 12, hdr.height),
            $"{(string.IsNullOrEmpty(nm) ? id : nm)}   ·   {id}", KoSecStyle);

        EditorGUILayout.Space(2);
        EditorGUI.indentLevel++;
        DrawKoClass(u);
        EditorGUI.indentLevel--;
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

    void DrawMapStats()
    {
        EditorGUI.indentLevel++;
        if (GUILayout.Button("새로고침 / 집계", GUILayout.Height(22))) Repaint();

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
        foreach (var kv in byType) EditorGUILayout.LabelField($"· {kv.Key}", kv.Value.ToString());
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

        EditorGUILayout.HelpBox("열린 씬 전체 기준(Systems + 게임플레이 씬 합산). 드랍 수량은 GameTuning.lootCountMult로 조절.", MessageType.None);
    }

    static int Count<T>() where T : Object
        => FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

    static void Row(string label, int value)
        => EditorGUILayout.LabelField(label, value.ToString());

    // ══════════════════════════════════════════════════════════════════
    //  탭 2 — NPC·상점 수치 (NPCData + ShopData)
    //  "모든 밸런스 수치는 에디터에서" 규칙 — NPC 관계·대화·상점 환율/재고를 한 곳에서.
    // ══════════════════════════════════════════════════════════════════
    Vector2 npcScroll;
    NPCData[] npcAssets;
    ShopData[] shopAssets;
    SerializedObject[] npcSo, shopSo;
    readonly Dictionary<string, bool> npcFold = new Dictionary<string, bool>();

    void ReloadNpcAssets()
    {
        npcAssets  = Resources.LoadAll<NPCData>("Data/NPC");
        shopAssets = Resources.LoadAll<ShopData>("Data/Shops");
        npcSo  = System.Array.ConvertAll(npcAssets,  a => new SerializedObject(a));
        shopSo = System.Array.ConvertAll(shopAssets, a => new SerializedObject(a));
    }

    void DrawNpcTab()
    {
        if (npcAssets == null) ReloadNpcAssets();

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("NPC·상점 수치 (Resources/Data/NPC · Data/Shops)", EditorStyles.boldLabel);
            if (GUILayout.Button("새로고침", GUILayout.Width(80))) ReloadNpcAssets();
        }
        EditorGUILayout.HelpBox("NPC 관계(호감/신뢰/두려움)·대화·상점 환율(구매가/판매가 배율)·재고를 여기서 편집. 항목 펼쳐 수정 후 '저장'.\n필드 라벨은 한글로 표시(영문 변수명은 라벨 위에 마우스 올리면 툴팁). 대화/선택지 같은 묶음은 펼쳐서 값 수정.\n※ 대화·수배 항목의 추가/삭제는 인스펙터·NPC 메이커에서. / 적·몬스터 전투 스탯은 상단 '몬스터' 탭.", MessageType.None);

        npcScroll = EditorGUILayout.BeginScrollView(npcScroll);
        DrawAssetSection("── NPC (NPCData) ──", npcAssets, npcSo);
        EditorGUILayout.Space(8);
        DrawAssetSection("── 상점 (ShopData) ──", shopAssets, shopSo);
        EditorGUILayout.EndScrollView();
    }

    void DrawAssetSection(string title, Object[] assets, SerializedObject[] sos)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        if (assets == null || assets.Length == 0) { EditorGUILayout.HelpBox("에셋 없음.", MessageType.Info); return; }

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] == null || sos[i] == null) continue;
            var so = sos[i];
            so.Update();

            string key = title + "/" + assets[i].name;
            npcFold.TryGetValue(key, out bool open);
            // 폴드아웃 제목: 한글 표시이름(displayName/shopName) 우선 + 에셋 파일명(회색)
            string disp = so.FindProperty("displayName")?.stringValue;
            if (string.IsNullOrEmpty(disp)) disp = so.FindProperty("shopName")?.stringValue;
            string foldTitle = string.IsNullOrEmpty(disp) ? assets[i].name : $"{disp}   ({assets[i].name})";
            open = EditorGUILayout.Foldout(open, foldTitle, true);
            npcFold[key] = open;
            if (!open) continue;

            EditorGUI.indentLevel++;
            DrawKoRoot(so);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("에셋 선택", GUILayout.Width(90))) EditorGUIUtility.PingObject(assets[i]);
                if (GUILayout.Button("저장", GUILayout.Width(70)))
                { so.ApplyModifiedProperties(); EditorUtility.SetDirty(assets[i]); AssetDatabase.SaveAssets(); }
            }
            EditorGUI.indentLevel--;
            if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(assets[i]);
            DrawSeparator(1);
        }
    }

    // ── SO 필드를 한글 라벨로 재귀 렌더(NPC/Shop 등). 영문 변수명은 데이터에 그대로, 표시만 한글. ──
    //  · 리프/원시·오브젝트 배열/문자열 배열 → Unity 기본(리오더 리스트·추가삭제 유지, 라벨만 한글)
    //  · 클래스 배열(요소가 직렬화 클래스) → 수동 재귀(요소 내부 필드까지 한글). 값 편집 전용(구조 추가/삭제는 인스펙터/메이커에서).
    static void DrawKoProperty(SerializedProperty prop)
    {
        string ko = KoLabels.Field(prop.name);
        string tip = string.IsNullOrEmpty(prop.tooltip) ? prop.name : $"{prop.name}\n{prop.tooltip}";
        var label = new GUIContent(ko ?? prop.displayName, tip);

        bool isClassArray = prop.isArray
            && prop.propertyType == SerializedPropertyType.Generic
            && prop.arraySize > 0
            && prop.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.Generic
            && prop.GetArrayElementAtIndex(0).hasVisibleChildren;

        if (isClassArray)
        {
            prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, $"{label.text}   ({prop.arraySize})", true);
            if (prop.isExpanded)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < prop.arraySize; i++)
                {
                    var el = prop.GetArrayElementAtIndex(i);
                    // 요소 제목: 요소 안 id/displayName/text가 있으면 미리보기로
                    string idv = el.FindPropertyRelative("id")?.stringValue;
                    string nm  = el.FindPropertyRelative("displayName")?.stringValue;
                    string hint =
                        !string.IsNullOrEmpty(idv) && !string.IsNullOrEmpty(nm) ? $"{idv} · {nm}" :
                        !string.IsNullOrEmpty(idv) ? idv :
                        !string.IsNullOrEmpty(nm)  ? nm  :
                        el.FindPropertyRelative("text")?.stringValue;
                    string elTitle = string.IsNullOrEmpty(hint) ? $"{i}" : $"{i}  ·  {hint}";
                    el.isExpanded = EditorGUILayout.Foldout(el.isExpanded, elTitle, true);
                    if (el.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawKoClass(el);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
            return;
        }

        // 일반 직렬화 클래스(자식 보유, 배열 아님) → 수동 foldout + 섹션 2열 그리드
        if (prop.propertyType == SerializedPropertyType.Generic && !prop.isArray && prop.hasVisibleChildren)
        {
            prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, label, true);
            if (prop.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawKoClass(prop);
                EditorGUI.indentLevel--;
            }
            return;
        }

        // 그 외 → Unity 기본(라벨만 한글 교체)
        EditorGUILayout.PropertyField(prop, label, true);
    }

    // 단순 자식 순회(폴백 — 타입 리플렉션 실패 시)
    static void DrawKoChildren(SerializedProperty parent)
    {
        var it = parent.Copy();
        var end = parent.GetEndProperty();
        bool enter = true;
        while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
        {
            enter = false;
            DrawKoProperty(it);
        }
    }

    // ── 클래스(또는 배열 요소)를 [Header] 섹션별로 묶고, 스칼라 필드는 2열로 가로 배치 ──
    static void DrawKoClass(SerializedProperty classProp)
    {
        var t = ResolveManagedType(classProp);
        if (t == null) { DrawKoChildren(classProp); return; }   // 타입 못 풀면 기존 세로 순회
        DrawKoContainer(t, classProp.FindPropertyRelative);
    }

    // SerializedObject 루트(NPCData/ShopData/StatDB 등)도 동일 레이아웃으로
    static void DrawKoRoot(SerializedObject so)
    {
        var t = so.targetObject != null ? so.targetObject.GetType() : null;
        if (t == null) { var p = so.GetIterator(); p.NextVisible(true); while (p.NextVisible(false)) DrawKoProperty(p); return; }
        DrawKoContainer(t, so.FindProperty);
    }

    // 타입의 [Header] 그룹 순서대로: (제목 섹션 바) + (그룹 필드 2열 그리드 / 복잡필드 전체폭)
    static void DrawKoContainer(System.Type t, System.Func<string, SerializedProperty> find)
    {
        foreach (var g in GroupsOf(t))
        {
            var props = new List<SerializedProperty>();
            foreach (var fn in g.fields)
            {
                var cp = find(fn);
                if (cp != null) props.Add(cp);
            }
            if (props.Count == 0) continue;

            if (!string.IsNullOrEmpty(g.title)) KoSection(KoLabels.Header(g.title));
            DrawKoFieldGrid(props);
        }
    }

    static readonly Color KoSecColor = new Color(0.27f, 0.33f, 0.41f);
    static GUIStyle _koSecStyle;
    static GUIStyle KoSecStyle => _koSecStyle ??= new GUIStyle(EditorStyles.label)
    { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };

    static void KoSection(string title)
    {
        EditorGUILayout.Space(4);
        var r = GUILayoutUtility.GetRect(0, 19, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, KoSecColor);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 3f, r.height), KoSecColor * 1.6f);
        GUI.Label(new Rect(r.x + 8, r.y, r.width - 12, r.height), title, KoSecStyle);
    }

    // 스칼라는 2열, 배열/중첩클래스 등 복잡 필드는 전체폭으로
    static void DrawKoFieldGrid(List<SerializedProperty> props)
    {
        int savedIndent = EditorGUI.indentLevel;
        float indentPx = savedIndent * 15f;
        float avail = EditorGUIUtility.currentViewWidth - indentPx - 30f;
        int colCount = avail >= 540f ? 2 : 1;
        float cellW = colCount == 2 ? avail / 2f - 6f : Mathf.Max(avail, 220f);
        float labelW = Mathf.Clamp(cellW * 0.48f, 92f, 190f);

        EditorGUI.indentLevel = 0;     // 셀은 수동 들여쓰기(가로 정렬 안정화)
        int col = 0;
        bool rowOpen = false;

        void EndRow() { if (rowOpen) { EditorGUILayout.EndHorizontal(); rowOpen = false; col = 0; } }

        foreach (var p in props)
        {
            if (!IsCellable(p))
            {
                EndRow();
                EditorGUI.indentLevel = savedIndent;   // 복잡필드(폴드아웃)는 정상 들여쓰기로
                DrawKoProperty(p);
                EditorGUI.indentLevel = 0;
                continue;
            }
            if (col == 0) { EditorGUILayout.BeginHorizontal(); GUILayout.Space(indentPx + 4); rowOpen = true; }
            DrawKoCell(p, cellW, labelW);
            if (++col >= colCount) EndRow();
        }
        EndRow();
        EditorGUI.indentLevel = savedIndent;
    }

    static bool IsCellable(SerializedProperty p)
    {
        if (p.isArray && p.propertyType != SerializedPropertyType.String) return false;       // 배열 = 전체폭
        if (p.propertyType == SerializedPropertyType.Generic && p.hasVisibleChildren) return false; // 중첩 클래스 = 전체폭
        return true;
    }

    static void DrawKoCell(SerializedProperty p, float cellW, float labelW)
    {
        string ko = KoLabels.Field(p.name);
        string tip = string.IsNullOrEmpty(p.tooltip) ? p.name : $"{p.name}\n{p.tooltip}";
        var label = new GUIContent(ko ?? p.displayName, tip);
        float prevLW = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = labelW;
        EditorGUILayout.PropertyField(p, label, false, GUILayout.Width(cellW));
        EditorGUIUtility.labelWidth = prevLW;
    }

    // ── 리플렉션: 타입의 직렬화 필드를 [Header] 기준 그룹으로(캐시) ──
    //  ※ 단일 클래스 계층 전용. base에 직렬화 public 필드가 있는 타입에 쓰면 MetadataToken 정렬이
    //    계층을 가로질러 선언순서를 보장 못 해 헤더 매핑이 어긋날 수 있음(현재 대상 4종은 무관).
    class KoGroup { public string title; public readonly List<string> fields = new List<string>(); }
    static readonly Dictionary<System.Type, List<KoGroup>> _koGroupCache = new Dictionary<System.Type, List<KoGroup>>();

    static List<KoGroup> GroupsOf(System.Type t)
    {
        if (_koGroupCache.TryGetValue(t, out var cached)) return cached;
        var groups = new List<KoGroup>();
        var cur = new KoGroup { title = null };
        groups.Add(cur);
        // GetFields()는 순서 미보장 → MetadataToken(선언 순서 대응)으로 정렬해 [Header] 매핑 안정화
        var fields = t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                      .OrderBy(f => f.MetadataToken);
        foreach (var fi in fields)
        {
            if (fi.IsNotSerialized) continue;
            var hdr = (HeaderAttribute)System.Attribute.GetCustomAttribute(fi, typeof(HeaderAttribute));
            if (hdr != null) { cur = new KoGroup { title = hdr.header }; groups.Add(cur); }
            cur.fields.Add(fi.Name);
        }
        _koGroupCache[t] = groups;
        return groups;
    }

    // SerializedProperty(클래스/배열 요소)의 관리형 타입 해석. 실패 시 null.
    static System.Type ResolveManagedType(SerializedProperty prop)
    {
        var obj = prop.serializedObject?.targetObject;
        if (obj == null) return null;
        System.Type t = obj.GetType();
        string path = prop.propertyPath.Replace(".Array.data[", "[");
        foreach (var seg in path.Split('.'))
        {
            if (seg.Length == 0) continue;
            string fname = seg;
            bool indexed = false;
            int b = seg.IndexOf('[');
            if (b >= 0) { fname = seg.Substring(0, b); indexed = true; }
            var fi = GetFieldRec(t, fname);
            if (fi == null) return null;
            t = fi.FieldType;
            if (indexed) t = ElementTypeOf(t);
            if (t == null) return null;
        }
        return t;
    }

    static System.Reflection.FieldInfo GetFieldRec(System.Type t, string name)
    {
        const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        while (t != null) { var fi = t.GetField(name, F); if (fi != null) return fi; t = t.BaseType; }
        return null;
    }

    static System.Type ElementTypeOf(System.Type t)
    {
        if (t.IsArray) return t.GetElementType();
        if (t.IsGenericType) { var a = t.GetGenericArguments(); if (a.Length > 0) return a[0]; }
        return t;
    }

    // ══════════════════════════════════════════════════════════════════
    //  탭 3 — 지역 루트 (비주얼) — region_loot.txt를 구조화해 보기·편집
    //  CSV 탭은 raw 폴백으로 유지. 여기는 "맵 루트 확률을 한눈에" 보는 GUI.
    // ══════════════════════════════════════════════════════════════════
    static string RegionLootPath => Path.Combine(Application.dataPath, "Resources", "region_loot.txt");

    // 파싱된 한 아이템 행
    class RLEntry
    {
        public string itemId;
        public float weight;
        public int min;
        public int max;
    }

    // 한 티어 풀 (지역 × 티어)
    class RLPool
    {
        public int rollCount;
        public readonly List<RLEntry> entries = new List<RLEntry>();
    }

    // 한 지역 (티어 → 풀)
    class RLRegion
    {
        public string regionId;
        public readonly Dictionary<RegionLootTier, RLPool> tiers = new Dictionary<RegionLootTier, RLPool>();
    }

    // 지역 순서 보존(파일 등장 순). regionId → 모델
    List<RLRegion> rlRegions;
    string[] rlRegionTabNames;       // 지역 선택 툴바 라벨
    int rlRegionIdx;
    Vector2 rlScroll;
    bool rlLoaded;
    bool rlDirty;

    // 에디터 전용 아이템 인덱스 (런타임 ItemDatabase.Instance는 에디트모드에 null이라 직접 로드)
    Dictionary<string, ItemData> rlItemDb;

    // 티어 표시 순서 + 한글 라벨 + 섹션색
    static readonly RegionLootTier[] RLTierOrder =
    {
        RegionLootTier.ContainerDay, RegionLootTier.ContainerNight,
        RegionLootTier.GroundDay,    RegionLootTier.GroundNight,
        RegionLootTier.ContainerAnomaly, RegionLootTier.GroundAnomaly,
    };

    static string RLTierLabel(RegionLootTier t) => t switch
    {
        RegionLootTier.ContainerDay      => "컨테이너 (낮)",
        RegionLootTier.ContainerNight    => "컨테이너 (밤)",
        RegionLootTier.GroundDay         => "바닥 (낮)",
        RegionLootTier.GroundNight       => "바닥 (밤)",
        RegionLootTier.ContainerAnomaly  => "컨테이너 (현상)",
        RegionLootTier.GroundAnomaly     => "바닥 (현상)",
        _ => t.ToString(),
    };

    static Color RLTierColor(RegionLootTier t) => t switch
    {
        RegionLootTier.ContainerDay      => new Color(0.30f, 0.46f, 0.30f), // 초록 — 컨테이너 낮
        RegionLootTier.ContainerNight    => new Color(0.24f, 0.36f, 0.50f), // 청 — 컨테이너 밤
        RegionLootTier.GroundDay         => new Color(0.46f, 0.42f, 0.26f), // 황토 — 바닥 낮
        RegionLootTier.GroundNight       => new Color(0.32f, 0.34f, 0.46f), // 회청 — 바닥 밤
        RegionLootTier.ContainerAnomaly  => new Color(0.50f, 0.28f, 0.42f), // 자홍 — 현상
        RegionLootTier.GroundAnomaly     => new Color(0.44f, 0.26f, 0.40f),
        _ => new Color(0.35f, 0.35f, 0.38f),
    };

    // 희귀도 순서(낮은→높은) + 색 (ItemData.RarityColor와 동일 값)
    static readonly ItemRarity[] RLRarityOrder =
    { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary };

    static Color RLRarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common    => Color.white,
        ItemRarity.Uncommon  => new Color(0.3f, 0.9f, 0.3f),
        ItemRarity.Rare      => new Color(0.3f, 0.5f, 1f),
        ItemRarity.Epic      => new Color(0.7f, 0.3f, 1f),
        ItemRarity.Legendary => new Color(1f, 0.85f, 0.2f),
        _ => Color.white,
    };

    static string RLRarityLabel(ItemRarity r) => r switch
    {
        ItemRarity.Common    => "일반",
        ItemRarity.Uncommon  => "고급",
        ItemRarity.Rare      => "희귀",
        ItemRarity.Epic      => "영웅",
        ItemRarity.Legendary => "전설",
        _ => r.ToString(),
    };

    GUIStyle _rlSection;
    GUIStyle RLSectionText => _rlSection ??= new GUIStyle(EditorStyles.label)
    { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };

    void EnsureRlItemDb()
    {
        if (rlItemDb != null) return;
        rlItemDb = new Dictionary<string, ItemData>();
        foreach (var it in Resources.LoadAll<ItemData>("Items"))
        {
            if (it == null || string.IsNullOrEmpty(it.itemId)) continue;
            if (!rlItemDb.ContainsKey(it.itemId)) rlItemDb[it.itemId] = it;
        }
    }

    /// <summary>DB 조회(에디트모드 안전). 없으면 null.</summary>
    ItemData RlGetItem(string itemId)
    {
        EnsureRlItemDb();
        rlItemDb.TryGetValue(itemId, out var d);
        return d;
    }

    void RlLoad()
    {
        rlLoaded = true;
        rlDirty = false;
        rlRegions = new List<RLRegion>();
        var byId = new Dictionary<string, RLRegion>();

        var path = RegionLootPath;
        if (!File.Exists(path))
        {
            rlRegionTabNames = new string[0];
            rlRegionIdx = 0;
            return;
        }

        var lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            // 1행(헤더) 스킵, 빈 줄·주석 가드
            if (i == 0) continue;
            var raw = lines[i];
            if (raw == null) continue;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            var p = line.Split(',');
            if (p.Length < 7) continue;

            string region = p[0].Trim();
            string tierStr = p[1].Trim();
            if (region.Length == 0 || tierStr.Length == 0) continue;

            if (!System.Enum.TryParse<RegionLootTier>(RlTierEnumName(tierStr), true, out var tier))
                continue;

            int rollCount = RlParseInt(p[2], 1);
            string itemId = p[3].Trim();
            float weight = RlParseFloat(p[4], 0f);
            int minC = RlParseInt(p[5], 1);
            int maxC = RlParseInt(p[6], 1);

            if (!byId.TryGetValue(region, out var reg))
            {
                reg = new RLRegion { regionId = region };
                byId[region] = reg;
                rlRegions.Add(reg);
            }
            if (!reg.tiers.TryGetValue(tier, out var pool))
            {
                pool = new RLPool();
                reg.tiers[tier] = pool;
            }
            pool.rollCount = rollCount; // 같은 티어 = 동일 roll_count(파일 규약). 마지막 값 채택.
            pool.entries.Add(new RLEntry { itemId = itemId, weight = weight, min = minC, max = maxC });
        }

        rlRegionTabNames = new string[rlRegions.Count];
        for (int i = 0; i < rlRegions.Count; i++)
        {
            var def = WorldRegionCatalog.GetById(rlRegions[i].regionId);
            rlRegionTabNames[i] = def.HasValue ? def.Value.displayName : rlRegions[i].regionId;
        }
        if (rlRegionIdx >= rlRegions.Count) rlRegionIdx = 0;
    }

    // container_day → ContainerDay (RegionLootCatalog.ToTierEnumName과 동일 규칙)
    static string RlTierEnumName(string csvTier)
    {
        var parts = csvTier.Split('_');
        var sb = new StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            sb.Append(char.ToUpper(parts[i][0]));
            if (parts[i].Length > 1) sb.Append(parts[i].Substring(1));
        }
        return sb.ToString();
    }

    // ContainerDay → container_day (저장 시 파일 컬럼 복원)
    static string RlTierCsvName(RegionLootTier tier)
    {
        var s = tier.ToString();
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }

    static int RlParseInt(string s, int fallback)
        => int.TryParse(s.Trim(), out var v) ? v : fallback;

    static float RlParseFloat(string s, float fallback)
        => float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;

    void RlSave()
    {
        var sb = new StringBuilder();
        sb.Append("region,tier,roll_count,item_id,weight,min,max\n");
        foreach (var reg in rlRegions)
        {
            // 파일 가독성 위해 티어 표시 순서대로 기록(런타임 파서는 순서 무관)
            foreach (var tier in RLTierOrder)
            {
                if (!reg.tiers.TryGetValue(tier, out var pool)) continue;
                string tierCsv = RlTierCsvName(tier);
                foreach (var e in pool.entries)
                {
                    float w = e.weight;
                    string wStr = (w == Mathf.Floor(w))
                        ? ((int)w).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : w.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    sb.Append(reg.regionId).Append(',')
                      .Append(tierCsv).Append(',')
                      .Append(pool.rollCount).Append(',')
                      .Append(e.itemId).Append(',')
                      .Append(wStr).Append(',')
                      .Append(e.min).Append(',')
                      .Append(e.max).Append('\n');
                }
            }
        }
        File.WriteAllText(RegionLootPath, sb.ToString());
        rlDirty = false;
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("region_loot.txt 저장됨"));
        Debug.Log("[ControlPanel] region_loot.txt 저장: " + RegionLootPath);
    }

    void DrawRegionLootTab()
    {
        EnsureRlItemDb();
        if (!rlLoaded) RlLoad();

        if (rlRegions == null || rlRegions.Count == 0)
        {
            EditorGUILayout.HelpBox("Resources/region_loot.txt 를 읽지 못했습니다(없거나 비어 있음).", MessageType.Warning);
            if (GUILayout.Button("다시 로드", GUILayout.Height(24))) RlLoad();
            return;
        }

        DrawRegionLootSummaryBar();
        DrawSeparator(3);

        // 지역 선택 툴바
        EditorGUILayout.Space(2);
        int newIdx = GUILayout.Toolbar(rlRegionIdx, rlRegionTabNames, GUILayout.Height(24));
        if (newIdx != rlRegionIdx) rlRegionIdx = newIdx;
        DrawSeparator(2);

        rlScroll = EditorGUILayout.BeginScrollView(rlScroll);
        var reg = rlRegions[rlRegionIdx];
        foreach (var tier in RLTierOrder)
        {
            if (!reg.tiers.TryGetValue(tier, out var pool)) continue;
            DrawRegionLootTierSection(tier, pool);
        }
        EditorGUILayout.EndScrollView();

        DrawSeparator(3);
        DrawRegionLootFooter();
    }

    // 1) 상단 전체 요약 바
    void DrawRegionLootSummaryBar()
    {
        var gt = GameTuning.Instance;
        float chanceMult = gt != null ? gt.lootChanceMult : 1f;
        float countMult  = gt != null ? gt.lootCountMult : 1f;
        float valMult    = gt != null ? gt.valuableWeightMult : 1f;

        // 전체 맵 고급(Rare+) 비중: 모든 지역·티어 weight 합산 기준
        float totalW = 0f, rareW = 0f;
        foreach (var reg in rlRegions)
            foreach (var kv in reg.tiers)
                foreach (var e in kv.Value.entries)
                {
                    totalW += e.weight;
                    var d = RlGetItem(e.itemId);
                    if (d != null && d.rarity >= ItemRarity.Rare) rareW += e.weight;
                }
        float rarePct = totalW > 0f ? rareW / totalW * 100f : 0f;

        var box = GUILayoutUtility.GetRect(0, 46, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(box, new Color(0.16f, 0.18f, 0.22f));
        var inner = new Rect(box.x + 10, box.y + 5, box.width - 20, box.height - 10);

        var l1 = new Rect(inner.x, inner.y, inner.width, 18);
        GUI.Label(l1,
            $"전역 배율 (컨트롤 탭에서 조절) —  드랍확률 ×{chanceMult:0.##}    수량 ×{countMult:0.##}    귀중품가중 ×{valMult:0.##}",
            EditorStyles.boldLabel);

        var l2 = new Rect(inner.x, inner.y + 20, inner.width, 18);
        GUI.Label(l2,
            $"전체 맵 고급(희귀 이상) 비중: {rarePct:0.0}%   (모든 지역·티어 weight 합산 기준 · 전역 확률 미반영)",
            EditorStyles.miniLabel);
    }

    // 3) 티어별 섹션 (헤더 + 희귀도 분포 막대 + 아이템 테이블)
    void DrawRegionLootTierSection(RegionLootTier tier, RLPool pool)
    {
        EditorGUILayout.Space(8);

        // ── 색 섹션 헤더: 티어명 + roll_count(편집) + 아이템 수 ──
        var hdr = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(hdr, RLTierColor(tier));
        EditorGUI.DrawRect(new Rect(hdr.x, hdr.y, 4f, hdr.height), RLTierColor(tier) * 1.6f);
        GUI.Label(new Rect(hdr.x + 9, hdr.y, hdr.width - 230, hdr.height),
            $"{RLTierLabel(tier)}   ·   아이템 {pool.entries.Count}개", RLSectionText);

        // roll_count 편집 (헤더 우측)
        var rcLabelRect = new Rect(hdr.xMax - 200, hdr.y + 3, 56, 18);
        GUI.Label(rcLabelRect, "굴림 수", RLSectionText);
        var rcFieldRect = new Rect(hdr.xMax - 140, hdr.y + 3, 50, 18);
        int newRoll = EditorGUI.IntField(rcFieldRect, pool.rollCount);
        if (newRoll != pool.rollCount) { pool.rollCount = Mathf.Max(0, newRoll); rlDirty = true; }

        // Σweight 계산(드랍확률·분포 공통)
        float sumW = 0f;
        for (int i = 0; i < pool.entries.Count; i++) sumW += Mathf.Max(0f, pool.entries[i].weight);

        // ── 희귀도 분포 누적 막대 ──
        DrawRarityDistributionBar(pool, sumW);

        EditorGUILayout.Space(3);

        // ── 아이템 행 테이블 (희귀도 내림차순 = 고급 위로) ──
        // 정렬용 인덱스(원본 entries는 유지: 저장 안정성). 표시만 정렬.
        var ordered = pool.entries
            .OrderByDescending(e =>
            {
                var d = RlGetItem(e.itemId);
                return d != null ? (int)d.rarity : -1; // 미등록은 맨 아래
            })
            .ThenByDescending(e => e.weight)
            .ToList();

        // 테이블 헤더
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(14);
            GUILayout.Label("아이템", EditorStyles.miniBoldLabel, GUILayout.Width(220));
            GUILayout.Label("weight", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            GUILayout.Label("드랍 %", EditorStyles.miniBoldLabel, GUILayout.Width(64));
            GUILayout.Label("min", EditorStyles.miniBoldLabel, GUILayout.Width(44));
            GUILayout.Label("max", EditorStyles.miniBoldLabel, GUILayout.Width(44));
        }

        foreach (var e in ordered)
            DrawRegionLootEntryRow(e, sumW);
    }

    void DrawRarityDistributionBar(RLPool pool, float sumW)
    {
        EditorGUILayout.Space(2);
        var bar = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(bar, new Color(0f, 0f, 0f, 0.30f));

        if (sumW <= 0f)
        {
            GUI.Label(new Rect(bar.x + 6, bar.y, bar.width - 12, bar.height), "(weight 합 0)", EditorStyles.miniLabel);
            return;
        }

        // 희귀도별 weight 합
        var wByR = new float[RLRarityOrder.Length];
        float unknownW = 0f;
        foreach (var e in pool.entries)
        {
            float w = Mathf.Max(0f, e.weight);
            var d = RlGetItem(e.itemId);
            if (d == null) { unknownW += w; continue; }
            int ri = System.Array.IndexOf(RLRarityOrder, d.rarity);
            if (ri >= 0) wByR[ri] += w;
        }

        // 누적 막대(고급이 오른쪽으로 가도록 일반→전설 순으로 채움)
        float x = bar.x;
        for (int i = 0; i < RLRarityOrder.Length; i++)
        {
            if (wByR[i] <= 0f) continue;
            float frac = wByR[i] / sumW;
            float w = bar.width * frac;
            EditorGUI.DrawRect(new Rect(x, bar.y, w, bar.height), RLRarityColor(RLRarityOrder[i]));
            x += w;
        }
        if (unknownW > 0f)
        {
            float w = bar.width * (unknownW / sumW);
            EditorGUI.DrawRect(new Rect(x, bar.y, w, bar.height), new Color(0.4f, 0.4f, 0.4f));
        }

        // 막대 아래 각 % 라벨
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(2);
            for (int i = 0; i < RLRarityOrder.Length; i++)
            {
                float pct = wByR[i] / sumW * 100f;
                if (pct <= 0f) continue;
                var prev = GUI.color;
                GUI.color = RLRarityColor(RLRarityOrder[i]);
                GUILayout.Label($"● {RLRarityLabel(RLRarityOrder[i])} {pct:0.0}%", EditorStyles.miniLabel, GUILayout.Width(96));
                GUI.color = prev;
            }
            if (unknownW > 0f)
                GUILayout.Label($"● 미등록 {unknownW / sumW * 100f:0.0}%", EditorStyles.miniLabel, GUILayout.Width(96));
        }
    }

    void DrawRegionLootEntryRow(RLEntry e, float sumW)
    {
        var d = RlGetItem(e.itemId);
        bool known = d != null;
        ItemRarity rarity = known ? d.rarity : ItemRarity.Common;
        string name = known ? (string.IsNullOrEmpty(d.displayName) ? e.itemId : d.displayName) : e.itemId + "  (미등록)";

        using (new EditorGUILayout.HorizontalScope())
        {
            // 희귀도 색 점
            var dot = GUILayoutUtility.GetRect(12, 16, GUILayout.Width(12));
            var dotColor = known ? RLRarityColor(rarity) : new Color(0.45f, 0.45f, 0.45f);
            EditorGUI.DrawRect(new Rect(dot.x + 2, dot.y + 4, 8, 8), dotColor);

            // 표시명 (미등록은 회색)
            var prevColor = GUI.color;
            GUI.color = known ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label(new GUIContent(name, e.itemId), GUILayout.Width(220));
            GUI.color = prevColor;

            // weight 편집
            float nw = EditorGUILayout.FloatField(e.weight, GUILayout.Width(70));
            if (!Mathf.Approximately(nw, e.weight)) { e.weight = Mathf.Max(0f, nw); rlDirty = true; }

            // 드랍 % (weight / Σweight)
            float pct = sumW > 0f ? Mathf.Max(0f, e.weight) / sumW * 100f : 0f;
            GUILayout.Label($"{pct:0.0}%", GUILayout.Width(64));

            // min / max 편집
            int nmin = EditorGUILayout.IntField(e.min, GUILayout.Width(44));
            if (nmin != e.min) { e.min = Mathf.Max(0, nmin); rlDirty = true; }
            int nmax = EditorGUILayout.IntField(e.max, GUILayout.Width(44));
            if (nmax != e.max) { e.max = Mathf.Max(e.min, nmax); rlDirty = true; }
        }
    }

    // 4) 하단 버튼 바
    void DrawRegionLootFooter()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(rlDirty ? "변경됨 ●" : "변경 없음",
                rlDirty ? EditorStyles.boldLabel : EditorStyles.miniLabel, GUILayout.Width(90));

            var prev = GUI.backgroundColor;
            using (new EditorGUI.DisabledScope(!rlDirty))
            {
                GUI.backgroundColor = new Color(0.45f, 0.70f, 1f);
                if (GUILayout.Button("저장", GUILayout.Height(26), GUILayout.Width(90))) RlSave();
            }
            GUI.backgroundColor = prev;

            using (new EditorGUI.DisabledScope(!rlDirty))
            {
                if (GUILayout.Button("되돌리기 (재로드)", GUILayout.Height(26), GUILayout.Width(140))) RlLoad();
            }

            if (GUILayout.Button("새로고침", GUILayout.Height(26), GUILayout.Width(90)))
            { rlItemDb = null; RlLoad(); }

            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.HelpBox(
            "weight = 같은 티어 풀 내 상대 확률(p = weight/Σweight). 굴림 수 = 풀당 롤 횟수. min~max = 1회 획득 수량 범위.\n" +
            "전역 드랍확률·수량 배율은 '컨트롤' 탭 GameTuning에서 조절(여기 막대·%엔 미반영).", MessageType.None);
    }

    // ══════════════════════════════════════════════════════════════════
    //  탭 4 — CSV 밸런스 (격자 편집, 맵툴 스타일)
    // ══════════════════════════════════════════════════════════════════
    List<string> files;
    string current;
    List<string[]> rows;   // rows[0] = 헤더
    Vector2 csvScroll;   // 우측 그리드(가로+세로)
    Vector2 fileScroll;  // 좌측 파일 목록(세로)
    bool dirty;
    float colW = 130f;
    string filter = "";

    // ── 타입드 그리드: 컬럼 타입 추론 + 정렬 상태 (파일 로드 시 1회 계산) ──
    enum ColType { Text, Int, Float, Bool, EnumCategory, EnumRarity, EnumTier, EnumRegion }
    ColType[] colTypes;          // 컬럼별 타입 (헤더 기준 인덱스, 길이 = rows[0].Length)
    int sortCol = -1;            // 정렬 기준 컬럼(-1 = 없음)
    bool sortAsc = true;

    // enum 드롭다운 옵션 캐시(파일 로드 시 1회). region은 자유입력 폴백 위해 마지막에 "(직접입력)" 추가.
    static string[] _categoryNames, _rarityNames, _tierCsvNames, _regionIds;
    static string[] CategoryNames => _categoryNames ??= System.Enum.GetNames(typeof(ItemCategory));
    static string[] RarityNames   => _rarityNames   ??= System.Enum.GetNames(typeof(ItemRarity));
    static string[] TierCsvNames  => _tierCsvNames  ??= BuildTierCsvNames();
    static string[] RegionIds      => _regionIds      ??= BuildRegionIds();

    static string[] BuildTierCsvNames()
    {
        var vals = (RegionLootTier[])System.Enum.GetValues(typeof(RegionLootTier));
        var arr = new string[vals.Length];
        for (int i = 0; i < vals.Length; i++) arr[i] = RlTierCsvName(vals[i]);
        return arr;
    }

    static string[] BuildRegionIds()
    {
        var all = WorldRegionCatalog.All;
        var arr = new string[all.Length];
        for (int i = 0; i < all.Length; i++) arr[i] = all[i].regionId;
        return arr;
    }

    GUIStyle _bigBtn, _barLabel, _hdrCell, _rowNum;
    static readonly Color CSep      = new Color(0f, 0f, 0f, 0.35f);
    static readonly Color CHeaderBg = new Color(0.20f, 0.31f, 0.46f);
    static readonly Color CSelBg    = new Color(0.22f, 0.42f, 0.85f);
    static readonly Color CZebra    = new Color(1f, 1f, 1f, 0.035f);
    static readonly Color CNumBg    = new Color(0f, 0f, 0f, 0.18f);
    static readonly Color CSortCol  = new Color(0.30f, 0.45f, 0.62f);

    GUIStyle BigBtn   => _bigBtn   ??= new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fixedHeight = 26 };
    GUIStyle BarLabel => _barLabel ??= new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
    GUIStyle HdrCell  => _hdrCell  ??= new GUIStyle(EditorStyles.label)
    { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white }, padding = new RectOffset(4, 2, 0, 0) };
    GUIStyle RowNum   => _rowNum   ??= new GUIStyle(EditorStyles.miniLabel)
    { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.6f, 0.65f, 0.75f) } };
    GUIStyle _hdrSub;
    GUIStyle HdrSub   => _hdrSub   ??= new GUIStyle(EditorStyles.miniLabel)
    { alignment = TextAnchor.MiddleLeft, fontSize = 9, padding = new RectOffset(4, 2, 0, 0),
      normal = { textColor = new Color(0.74f, 0.80f, 0.90f, 0.85f) } };

    void RefreshFiles()
    {
        files = new List<string>();
        foreach (var f in new[] { "items.csv", "quests.csv", "region_loot.csv", "region_items.csv" })
        {
            var p = Path.Combine(ToolsDir, f);
            if (File.Exists(p)) files.Add(p);
        }
        var bdir = Path.Combine(ToolsDir, "balance");
        if (Directory.Exists(bdir))
            files.AddRange(Directory.GetFiles(bdir, "*.csv").OrderBy(x => x));

        // 런타임이 실제 로드하는 파일(Resources/*.txt) — 여기서 편집하면 게임에 바로 반영.
        //  맵 드랍 튜닝: region_loot.txt (지역별 개별 드랍 = weight=개별확률, roll_count/min/max=수량).
        foreach (var rf in new[] { "region_loot.txt", "region_items.txt" })
        {
            var p = Path.Combine(Application.dataPath, "Resources", rf);
            if (File.Exists(p)) files.Add(p);
        }
    }

    void Load(string path)
    {
        current = path;
        rows = new List<string[]>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 && rows.Count > 0) continue;
            rows.Add(line.Split(','));
        }
        int cols = rows.Count > 0 ? rows[0].Length : 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Length == cols) continue;
            var a = new string[cols];
            for (int c = 0; c < cols; c++) a[c] = c < rows[i].Length ? rows[i][c] : "";
            rows[i] = a;
        }
        sortCol = -1;
        sortAsc = true;
        InferColTypes();
        dirty = false;
    }

    // ── 컬럼 타입 추론 (헤더명 + 데이터행 값 기준, 로드 시 1회) ──
    void InferColTypes()
    {
        if (rows == null || rows.Count == 0) { colTypes = new ColType[0]; return; }
        int cols = rows[0].Length;
        colTypes = new ColType[cols];
        for (int c = 0; c < cols; c++)
            colTypes[c] = InferOneCol(c);
    }

    ColType InferOneCol(int c)
    {
        string header = (rows[0][c] ?? "").Trim().ToLowerInvariant();

        // 데이터행(헤더 제외) 값 수집
        var vals = new List<string>();
        for (int r = 1; r < rows.Count; r++)
        {
            if (c >= rows[r].Length) continue;
            vals.Add(rows[r][c] ?? "");
        }

        bool AllInt() { foreach (var v in vals) { var t = v.Trim(); if (t.Length == 0) continue; if (!int.TryParse(t, out _)) return false; } return true; }
        bool AllNum() { foreach (var v in vals) { var t = v.Trim(); if (t.Length == 0) continue; if (!float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) return false; } return true; }
        bool AnyNonEmpty() { foreach (var v in vals) if (v.Trim().Length > 0) return true; return false; }

        // enum: 헤더명 규칙 + 값 정합
        if ((header == "cat" || header == "category") && AnyNonEmpty() && AllInt()) return ColType.EnumCategory;
        if ((header == "rar" || header == "rarity")   && AnyNonEmpty() && AllInt()) return ColType.EnumRarity;
        if (header == "tier" && AnyNonEmpty())
        {
            // 모든 비어있지 않은 값이 RegionLootTier csv명일 때만 (reputation_tiers의 F/E/D 등은 제외)
            bool allTier = true;
            foreach (var v in vals)
            {
                var t = v.Trim();
                if (t.Length == 0) continue;
                if (System.Array.IndexOf(TierCsvNames, t) < 0) { allTier = false; break; }
            }
            if (allTier) return ColType.EnumTier;
        }
        if (header == "region" && AnyNonEmpty()) return ColType.EnumRegion;

        // bool: 값이 전부 {"","0","1"}
        bool allBool = true;
        foreach (var v in vals)
        {
            var t = v.Trim();
            if (t.Length == 0 || t == "0" || t == "1") continue;
            allBool = false; break;
        }
        if (allBool && AnyNonEmpty()) return ColType.Bool;

        if (AnyNonEmpty() && AllInt()) return ColType.Int;
        if (AnyNonEmpty() && AllNum()) return ColType.Float;
        return ColType.Text;
    }

    static string TypeBadge(ColType t) => t switch
    {
        ColType.Int          => "#",
        ColType.Float        => "#.#",
        ColType.Bool         => "✓",
        ColType.EnumCategory => "▼cat",
        ColType.EnumRarity   => "▼rar",
        ColType.EnumTier     => "▼tier",
        ColType.EnumRegion   => "▼reg",
        _                    => "T",
    };

    void Save()
    {
        var sb = new StringBuilder();
        foreach (var r in rows) sb.AppendLine(string.Join(",", r));
        File.WriteAllText(current, sb.ToString());
        dirty = false;
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("저장됨"));
    }

    void DrawCsvTab()
    {
        if (files == null) RefreshFiles();

        DrawCsvToolbar();
        DrawSeparator(3);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(210));
        fileScroll = EditorGUILayout.BeginScrollView(fileScroll, false, false, GUILayout.ExpandHeight(true));
        DrawFileList();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        var vsep = GUILayoutUtility.GetRect(2, 2, GUILayout.Width(2), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(vsep, CSep);

        EditorGUILayout.BeginVertical();
        DrawGrid();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    // 상단 바: 한글 제목 우선 "퀘스트 (quests.csv)" — 매핑 없으면 파일명만
    string CsvToolbarTitle()
    {
        var name = Path.GetFileName(current);
        var ko = KoLabels.FileTitle(name);
        return ko != null ? $"{ko}  ({name})" : name;
    }

    void DrawCsvToolbar()
    {
        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label(rows == null ? "CSV 미선택" : CsvToolbarTitle() + (dirty ? "  ●" : ""),
            BarLabel, GUILayout.Width(260), GUILayout.Height(26));

        var prev = GUI.backgroundColor;
        using (new EditorGUI.DisabledScope(rows == null || !dirty))
        {
            GUI.backgroundColor = new Color(0.45f, 0.70f, 1f);
            if (GUILayout.Button("저장", BigBtn, GUILayout.Width(70))) Save();
        }
        GUI.backgroundColor = prev;
        using (new EditorGUI.DisabledScope(rows == null))
        {
            if (GUILayout.Button("되돌리기", BigBtn, GUILayout.Width(80))) Load(current);
            GUI.backgroundColor = new Color(0.40f, 0.85f, 0.50f);
            if (GUILayout.Button("＋ 행 추가", BigBtn, GUILayout.Width(90))) AddRow();
            GUI.backgroundColor = prev;
        }

        GUILayout.Space(12);
        GUILayout.Label("열 너비", BarLabel, GUILayout.Width(48), GUILayout.Height(26));
        colW = EditorGUILayout.Slider(colW, 60f, 260f, GUILayout.Width(180));

        GUILayout.FlexibleSpace();
        GUILayout.Label("🔍", GUILayout.Width(18), GUILayout.Height(26));
        filter = EditorGUILayout.TextField(filter, GUILayout.Width(180), GUILayout.Height(20));
        if (GUILayout.Button("새로고침", BigBtn, GUILayout.Width(80))) RefreshFiles();

        EditorGUILayout.EndHorizontal();
    }

    void DrawFileList()
    {
        GUILayout.Label("CSV 파일", EditorStyles.boldLabel);
        foreach (var f in files)
        {
            var name  = Path.GetFileName(f);
            string ko = KoLabels.FileTitle(name);   // 파일 한글 제목(없으면 null → 파일명만)
            bool sel  = current == f;

            float rowH = ko != null ? 34f : 22f;
            var rect = GUILayoutUtility.GetRect(0, rowH, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint && sel)
                EditorGUI.DrawRect(rect, CSelBg);

            if (ko != null)
            {
                // 1행: 한글 제목(굵게) / 2행: 파일명(회색 작게)
                var titleStyle = new GUIStyle(EditorStyles.label)
                { fontStyle = FontStyle.Bold, padding = new RectOffset(8, 2, 0, 0),
                  normal = { textColor = sel ? Color.white : EditorStyles.label.normal.textColor } };
                GUI.Label(new Rect(rect.x, rect.y + 2, rect.width, 16), ko, titleStyle);
                GUI.Label(new Rect(rect.x + 4, rect.y + 18, rect.width - 6, 13), name, HdrSub);
            }
            else
            {
                var lblStyle = new GUIStyle(EditorStyles.label)
                { padding = new RectOffset(8, 2, 0, 0), normal = { textColor = sel ? Color.white : EditorStyles.label.normal.textColor } };
                if (sel) lblStyle.fontStyle = FontStyle.Bold;
                GUI.Label(rect, name, lblStyle);
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && !sel)
            {
                if (dirty && !EditorUtility.DisplayDialog("저장 안 함", "변경을 버리고 이동할까요?", "버리기", "취소")) { }
                else Load(f);
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("타입드 그리드: 컬럼 타입 자동 추론(#=숫자 ✓=불리언 ▼=드롭다운 T=텍스트).\n헤더 클릭 = 정렬(오름/내림). 행마다 [⧉복제][▲][▼][✕].\n텍스트 셀 콤마(,) 입력 → 자동 ';' 치환(구분자 보호).\n.csv = tools 원본(생성기로 SO 반영) · .txt = Resources 런타임(저장 즉시 반영).", MessageType.None);
        EditorGUILayout.HelpBox("맵 드랍 튜닝:\n• 전체 확률·수량 배율 = '컨트롤' 탭 ▸ GameTuning(lootChanceMult/lootCountMult/valuableWeightMult)\n• 지역별 개별 드랍 = region_loot.txt (weight=개별확률, roll_count/min/max=수량)", MessageType.Info);
    }

    void DrawGrid()
    {
        if (rows == null || rows.Count == 0)
        {
            EditorGUILayout.HelpBox("좌측에서 CSV 파일을 선택하세요.", MessageType.Info);
            return;
        }

        int cols = rows[0].Length;
        EnsureColTypes(cols);

        bool hasFilter = !string.IsNullOrEmpty(filter);
        string f = hasFilter ? filter.ToLowerInvariant() : null;

        csvScroll = EditorGUILayout.BeginScrollView(csvScroll);

        // 콘텐츠 폭을 확정해야 가로 스크롤바가 잡힌다(#열 34 + 데이터열 cols×colW + 행조작버튼 94).
        float totalW = 34f + cols * colW + 94f + 6f;
        EditorGUILayout.BeginVertical(GUILayout.Width(totalW));

        DrawGridHeader(cols);

        int rowOp = 0, opAt = -1; // rowOp: 1=복제 2=위 3=아래 4=삭제
        for (int r = 1; r < rows.Count; r++)
        {
            if (hasFilter && !RowMatches(rows[r], f)) continue;

            var rr = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint && (r & 1) == 0)
                EditorGUI.DrawRect(rr, CZebra);

            var numRect = GUILayoutUtility.GetRect(34, 18, GUILayout.Width(34));
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(numRect, CNumBg);
            GUI.Label(numRect, r.ToString(), RowNum);

            for (int c = 0; c < cols; c++)
                DrawCell(r, c);

            // 행 조작 버튼
            if (GUILayout.Button(new GUIContent("⧉", "이 행 복제"), GUILayout.Width(24))) { rowOp = 1; opAt = r; }
            using (new EditorGUI.DisabledScope(r <= 1))
                if (GUILayout.Button(new GUIContent("▲", "위로"), GUILayout.Width(22))) { rowOp = 2; opAt = r; }
            using (new EditorGUI.DisabledScope(r >= rows.Count - 1))
                if (GUILayout.Button(new GUIContent("▼", "아래로"), GUILayout.Width(22))) { rowOp = 3; opAt = r; }
            if (GUILayout.Button(new GUIContent("✕", "삭제"), GUILayout.Width(22))) { rowOp = 4; opAt = r; }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // 행 조작 적용 (열거 종료 후)
        if (opAt >= 1 && opAt < rows.Count)
        {
            switch (rowOp)
            {
                case 1: rows.Insert(opAt + 1, (string[])rows[opAt].Clone()); dirty = true; break;
                case 2: if (opAt > 1) { (rows[opAt - 1], rows[opAt]) = (rows[opAt], rows[opAt - 1]); dirty = true; } break;
                case 3: if (opAt < rows.Count - 1) { (rows[opAt + 1], rows[opAt]) = (rows[opAt], rows[opAt + 1]); dirty = true; } break;
                case 4: rows.RemoveAt(opAt); dirty = true; break;
            }
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.40f, 0.85f, 0.50f);
            if (GUILayout.Button("＋ 행 추가", GUILayout.Width(110), GUILayout.Height(22))) AddRow();
            GUI.backgroundColor = prev;
            if (sortCol >= 0 && GUILayout.Button("정렬 해제", GUILayout.Width(90), GUILayout.Height(22))) sortCol = -1;
        }

        DrawSeparator(2);
        int dataCount = rows.Count - 1;
        string info = $"{dataCount} 행 · {cols} 열";
        if (sortCol >= 0 && sortCol < cols) info += $"  (정렬: \"{rows[0][sortCol]}\" {(sortAsc ? "▲" : "▼")})";
        if (hasFilter) info += $"  (검색: \"{filter}\")";
        GUILayout.Label(info, EditorStyles.miniLabel);
    }

    // 컬럼 수 가변/파일전환/null 가드 — colTypes 길이가 안 맞으면 재추론
    void EnsureColTypes(int cols)
    {
        if (colTypes == null || colTypes.Length != cols) InferColTypes();
    }

    const float CsvHdrH = 34f; // 2행 헤더(한글 + 영문 원문)

    void DrawGridHeader(int cols)
    {
        var hRect = EditorGUILayout.BeginHorizontal();
        if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(hRect, CHeaderBg);
        GUILayout.Label("#", RowNum, GUILayout.Width(34), GUILayout.Height(CsvHdrH));

        string file = current != null ? Path.GetFileName(current) : null;

        for (int c = 0; c < cols; c++)
        {
            var cellRect = GUILayoutUtility.GetRect(colW, CsvHdrH, GUILayout.Width(colW), GUILayout.Height(CsvHdrH));
            if (Event.current.type == EventType.Repaint && c == sortCol)
                EditorGUI.DrawRect(cellRect, CSortCol);

            string raw   = rows[0][c] ?? "";
            string ko    = KoLabels.Get(file, raw);          // 한글 라벨(없으면 null → 영문 원문 fallback)
            string arrow = c == sortCol ? (sortAsc ? " ▲" : " ▼") : "";
            string badge = (colTypes != null && c < colTypes.Length) ? TypeBadge(colTypes[c]) : "T";

            // 1행: 한글 우선(없으면 영문 헤더) — 굵게 흰색 / 우측 타입 뱃지
            var primary = new GUIContent($"{ko ?? raw}{arrow}", $"{raw}  ·  타입: {badge}  (헤더 클릭=정렬)");
            GUI.Label(new Rect(cellRect.x + 4, cellRect.y + 1, cellRect.width - 28, 16), primary, HdrCell);
            GUI.Label(new Rect(cellRect.xMax - 26, cellRect.y + 1, 24, 14), badge, RowNum);
            // 2행: 영문 원문(회색 작게) — 한글 라벨이 있을 때만(영문 fallback이면 1행에 이미 표기)
            if (ko != null)
                GUI.Label(new Rect(cellRect.x + 4, cellRect.y + 17, cellRect.width - 6, 13), raw, HdrSub);

            if (GUI.Button(cellRect, GUIContent.none, GUIStyle.none))
                SortByColumn(c);
        }
        GUILayout.Space(94); // 행 조작 버튼 너비 자리
        EditorGUILayout.EndHorizontal();
    }

    void SortByColumn(int c)
    {
        if (rows == null || rows.Count <= 2) return;
        if (sortCol == c) sortAsc = !sortAsc;
        else { sortCol = c; sortAsc = true; }

        var data = rows.GetRange(1, rows.Count - 1);
        var ct = (colTypes != null && c < colTypes.Length) ? colTypes[c] : ColType.Text;
        bool numeric = ct == ColType.Int || ct == ColType.Float || ct == ColType.Bool ||
                       ct == ColType.EnumCategory || ct == ColType.EnumRarity;

        data.Sort((a, b) =>
        {
            string sa = c < a.Length ? (a[c] ?? "") : "";
            string sb = c < b.Length ? (b[c] ?? "") : "";
            int cmp;
            if (numeric)
            {
                float fa = float.TryParse(sa.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var va) ? va : float.MinValue;
                float fb = float.TryParse(sb.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vb) ? vb : float.MinValue;
                cmp = fa.CompareTo(fb);
            }
            else cmp = string.Compare(sa, sb, System.StringComparison.OrdinalIgnoreCase);
            return sortAsc ? cmp : -cmp;
        });

        for (int i = 0; i < data.Count; i++) rows[i + 1] = data[i]; // 헤더(rows[0]) 고정
        dirty = true; // 재배열 = 저장 순서 변경
    }

    void AddRow()
    {
        if (rows == null || rows.Count == 0) return;
        rows.Add(new string[rows[0].Length]);
        dirty = true;
    }

    // ── 타입드 셀 위젯 (편집 시 string으로 포맷해 rows[r][c]에 반영) ──
    void DrawCell(int r, int c)
    {
        // 컬럼 수 가변 행 가드: 짧으면 패딩
        if (c >= rows[r].Length)
        {
            var padded = new string[rows[0].Length];
            for (int k = 0; k < padded.Length; k++) padded[k] = k < rows[r].Length ? rows[r][k] : "";
            rows[r] = padded;
        }

        string cur = rows[r][c] ?? "";
        ColType ct = (colTypes != null && c < colTypes.Length) ? colTypes[c] : ColType.Text;

        switch (ct)
        {
            case ColType.EnumCategory: DrawEnumIntCell(r, c, cur, CategoryNames); break;
            case ColType.EnumRarity:   DrawEnumIntCell(r, c, cur, RarityNames);   break;
            case ColType.EnumTier:     DrawEnumStrCell(r, c, cur, TierCsvNames);  break;
            case ColType.EnumRegion:   DrawRegionCell(r, c, cur);                 break;
            case ColType.Bool:
            {
                bool on = cur.Trim() == "1";
                bool nv = EditorGUILayout.Toggle(on, GUILayout.Width(colW));
                if (nv != on) SetCell(r, c, nv ? "1" : "0");
                break;
            }
            case ColType.Int:
            {
                int iv = int.TryParse(cur.Trim(), out var p) ? p : 0;
                int nv = EditorGUILayout.DelayedIntField(iv, GUILayout.Width(colW));
                if (nv != iv) SetCell(r, c, nv.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            }
            case ColType.Float:
            {
                float fv = float.TryParse(cur.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0f;
                float nv = EditorGUILayout.DelayedFloatField(fv, GUILayout.Width(colW));
                if (!Mathf.Approximately(nv, fv)) SetCell(r, c, FormatFloat(nv));
                break;
            }
            default:
            {
                string nv = EditorGUILayout.DelayedTextField(cur, GUILayout.Width(colW));
                if (nv != cur) SetCell(r, c, SanitizeText(nv));
                break;
            }
        }
    }

    // enum(int 저장): 드롭다운 표시, rows엔 정수 인덱스 문자열로 저장
    void DrawEnumIntCell(int r, int c, string cur, string[] names)
    {
        int idx = int.TryParse(cur.Trim(), out var p) ? p : -1;
        bool valid = idx >= 0 && idx < names.Length;
        // 미지원 인덱스는 "?N" 표기로 보존(덮어쓰지 않음)
        var display = new string[names.Length + (valid ? 0 : 1)];
        for (int i = 0; i < names.Length; i++) display[i] = $"{i} {names[i]}";
        int popupIdx = idx;
        if (!valid)
        {
            display[names.Length] = $"? {cur}";
            popupIdx = names.Length;
        }
        int sel = EditorGUILayout.Popup(popupIdx, display, GUILayout.Width(colW));
        if (sel != popupIdx && sel < names.Length)
            SetCell(r, c, sel.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // enum(문자열 저장, 예: tier=container_day): 드롭다운 표시, rows엔 csv명 그대로
    void DrawEnumStrCell(int r, int c, string cur, string[] names)
    {
        int idx = System.Array.IndexOf(names, cur.Trim());
        var display = new string[names.Length + (idx < 0 ? 1 : 0)];
        System.Array.Copy(names, display, names.Length);
        int popupIdx = idx;
        if (idx < 0) { display[names.Length] = $"? {cur}"; popupIdx = names.Length; }
        int sel = EditorGUILayout.Popup(popupIdx, display, GUILayout.Width(colW));
        if (sel != popupIdx && sel < names.Length)
            SetCell(r, c, names[sel]);
    }

    // region: 드롭다운(알려진 id 선택용) + 자유입력 텍스트필드(폴백). 둘 다 항상 노출.
    void DrawRegionCell(int r, int c, string cur)
    {
        var ids = RegionIds;
        int idx = System.Array.IndexOf(ids, cur.Trim());
        bool known = idx >= 0;

        // 드롭다운: 알려진 지역 id들 + (미등록이면 현재값) 항목
        var display = new string[ids.Length + (known ? 0 : 1)];
        System.Array.Copy(ids, display, ids.Length);
        int popupIdx = idx;
        if (!known) { display[ids.Length] = $"? {cur}"; popupIdx = ids.Length; }

        // 좁은 폭이면 드롭다운만, 충분하면 드롭다운+자유입력 텍스트
        float popW = colW >= 120f ? colW * 0.55f : colW;
        int sel = EditorGUILayout.Popup(popupIdx, display, GUILayout.Width(popW));
        if (sel != popupIdx && sel < ids.Length) SetCell(r, c, ids[sel]);

        if (colW >= 120f)
        {
            string nv = EditorGUILayout.DelayedTextField(cur, GUILayout.Width(Mathf.Max(50f, colW - popW - 4f)));
            if (nv != cur) SetCell(r, c, SanitizeText(nv));
        }
    }

    void SetCell(int r, int c, string value)
    {
        if (c >= rows[r].Length)
        {
            var padded = new string[rows[0].Length];
            for (int k = 0; k < padded.Length; k++) padded[k] = k < rows[r].Length ? rows[r][k] : "";
            rows[r] = padded;
        }
        if (rows[r][c] != value) { rows[r][c] = value; dirty = true; }
    }

    // 정수면 정수로, 아니면 invariant 실수 표기 (불필요한 소수점 방지)
    static string FormatFloat(float v)
        => (v == Mathf.Floor(v) && !float.IsInfinity(v))
            ? ((int)v).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // 텍스트 셀 콤마 보호: ',' → ';' 치환(구분자 깨짐 방지)
    string SanitizeText(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.IndexOf(',') >= 0)
        {
            ShowNotification(new GUIContent("콤마(,)는 ';' 로 치환됨"));
            return s.Replace(',', ';');
        }
        return s;
    }

    static bool RowMatches(string[] row, string lowerFilter)
    {
        for (int i = 0; i < row.Length; i++)
            if (!string.IsNullOrEmpty(row[i]) && row[i].ToLowerInvariant().Contains(lowerFilter)) return true;
        return false;
    }

    void DrawSeparator(float h)
    {
        var rect = GUILayoutUtility.GetRect(1, h, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, CSep);
    }
}
