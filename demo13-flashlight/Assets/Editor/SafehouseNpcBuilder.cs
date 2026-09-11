#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 안전가옥 핵심 NPC 데이터(+전당포 ShopData)를 코드로 생성/갱신. (story-script S-002~S-004 / npc-dialogue §5)
///   • 전당포 주인(강무진, npcId=pawnshop) — ShopData 연결 + 거래 선택지(openShop). 호감15/신뢰10/두려움0(단계0).
///   • 베테랑 회수꾼(veteran_scavenger) — 첫 일거리 안내.
///   • 구역 관리인(district_warden) — 거처/자릿세.
/// 에셋: Assets/Resources/Data/NPC/{npcId}.asset, Assets/Resources/Data/Shops/Shop_pawnshop.asset.
/// 멱등: 있으면 갱신. 이후 SafehouseGreyboxLayout이 storyNpcId로 자동 연결(재실행 시).
/// 대사/관계는 NPC Maker(Tools▸TopDown▸Content▸NPC Maker)로 더 다듬을 수 있음.
///
/// 메뉴: Tools ▸ TopDown ▸ Content ▸ Build Safehouse NPCs
/// </summary>
public static class SafehouseNpcBuilder
{
    const string NpcDir  = "Assets/Resources/Data/NPC";
    const string ShopDir = "Assets/Resources/Data/Shops";

    [MenuItem("Tools/TopDown/콘텐츠/안전가옥 NPC 빌드")]
    public static void Build()
    {
        EnsureFolder(NpcDir);
        EnsureFolder(ShopDir);

        // ── 전당포 ShopData ──
        var shop = CreateOrLoad<ShopData>(ShopDir + "/Shop_pawnshop.asset");
        shop.shopId   = "pawnshop";
        shop.shopName = "전당포";
        shop.buyRate  = 1f;
        shop.sellRate = 0.6f;   // 전당포는 후려침
        shop.allowConsignment = true;   // 위탁은 전당포만
        shop.stock    = LoadItems("Battery", "Canned_Food", "WaterBottle", "AdrenalineShot");
        EditorUtility.SetDirty(shop);

        // ── 전당포 주인(강무진) ──
        var pawn = CreateOrLoad<NPCData>(NpcDir + "/pawnshop.asset");
        pawn.npcId = "pawnshop"; pawn.displayName = "전당포 주인"; pawn.role = "전당포";
        pawn.initialAffinity = 15; pawn.initialTrust = 10; pawn.initialFear = 0;   // story-script S-002
        pawn.shopData = shop;
        pawn.defaultDialogues = new[] { Entry("greet", "또 왔나.", "…쓸 만한 거라도 주워 왔어?") };
        pawn.eventDialogues   = new[] { TradeEvent() };
        EditorUtility.SetDirty(pawn);

        // ── 베테랑 회수꾼 ──
        var vet = CreateOrLoad<NPCData>(NpcDir + "/veteran_scavenger.asset");
        vet.npcId = "veteran_scavenger"; vet.displayName = "회수꾼"; vet.role = "베테랑 회수꾼";
        vet.initialAffinity = 10; vet.initialTrust = 10; vet.initialFear = 0;
        vet.defaultDialogues = new[] { Entry("greet", "옆 게시판에서 의뢰 하나 떼 와.", "갔다 오면 나한테 보고하고.") };
        vet.eventDialogues   = new[] { RepTestEvent() };   // [테스트] 3선택지 + 평판 변동
        EditorUtility.SetDirty(vet);

        // ── 구역 관리인 ──
        var warden = CreateOrLoad<NPCData>(NpcDir + "/district_warden.asset");
        warden.npcId = "district_warden"; warden.displayName = "구역 관리인"; warden.role = "관리인";
        warden.initialAffinity = 10; warden.initialTrust = 5; warden.initialFear = 0;
        warden.defaultDialogues = new[] { Entry("greet", "자릿세는 빚으로 달아놨다.", "갚을 생각은 하고 있고?") };
        EditorUtility.SetDirty(warden);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=cyan>[SafehouseNPC]</color> NPCData 3 + ShopData 1 생성/갱신.\n" +
                  "  • pawnshop(강무진)+ShopData(전당포, 거래선택지) · veteran_scavenger · district_warden.\n" +
                  "  • 'Build Safehouse Greybox Layout'을 (다시) 실행하면 storyNpcId로 자동 연결됩니다.");
        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Safehouse NPCs",
                "NPCData 3 + ShopData 1 생성/갱신 완료.\n\n" +
                "이제 'Tools▸TopDown▸Map▸Build Safehouse Greybox Layout'을 다시 실행하면\n" +
                "전당포/회수꾼/관리인 마커에 NPCData가 자동 연결됩니다.\n\n" +
                "대사·관계는 NPC Maker로 더 다듬을 수 있습니다.", "확인");
    }

    // ── 헬퍼 ──
    static DialogueEntry Entry(string id, params string[] lines)
        => new DialogueEntry { id = id, lines = lines, conditions = new DialogueCondition(), priority = 0 };

    static EventDialogue TradeEvent()
        => new EventDialogue
        {
            id = "trade", oneShot = false,
            triggerCondition = new DialogueCondition(),
            npcLines = new[] { "팔 거라도 있나?" },
            choices = new[]
            {
                new DialogueChoice { text = "거래하기", openShop = true },
                new DialogueChoice { text = "됐어", resultLines = new[] { "흥." } },
            },
        };

    // [테스트] 3선택지 + 평판 변동 — 회수꾼에 부착. W/S+E 선택 & 평판 토스트(↑↓) 검증용.
    static EventDialogue RepTestEvent()
        => new EventDialogue
        {
            id = "rep_test", oneShot = false,
            triggerCondition = new DialogueCondition(),
            npcLines = new[] { "[테스트] 날 어떻게 대할 거야?" },
            choices = new[]
            {
                new DialogueChoice { text = "비위 맞추기 (평판 +10)", reputationChange = 10, resultLines = new[] { "흐흐, 마음에 드는군." } },
                new DialogueChoice { text = "시비 걸기 (평판 -5)",   reputationChange = -5, resultLines = new[] { "...건방진 놈." } },
                new DialogueChoice { text = "그냥 인사 (변화 없음)",  reputationChange = 0,  resultLines = new[] { "그래, 또 보자고." } },
            },
        };

    static List<ItemData> LoadItems(params string[] names)
    {
        string[] dirs = { "Consumable", "Material", "Valuable", "Medical", "Misc", "Weapon" };
        var list = new List<ItemData>();
        foreach (var n in names)
        {
            ItemData it = null;
            foreach (var d in dirs)
            {
                it = AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Resources/Items/{d}/{n}.asset");
                if (it != null) break;
            }
            if (it != null) list.Add(it);
            else Debug.LogWarning($"[SafehouseNPC] 상점 재고 아이템 못 찾음: {n} (스킵)");
        }
        return list;
    }

    static T CreateOrLoad<T>(string path) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null)
        {
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
        }
        return a;
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
