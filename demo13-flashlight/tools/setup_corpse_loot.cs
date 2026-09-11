// 배그식 사망 루팅 준비 — docs/combat.md §배그식 사망 루팅, docs/economy.md §적 현금 드랍 (2026-09-11). 다시 돌려도 같은 결과.
//  ① 현금 아이템 Resources/Items/Valuable/Cash.asset (itemId cash, 무게 0, 1개 = ◈1)
//  ② StatDB 유닛별 cashMin/cashMax (일반 50~150 · 총기 80~200 · 탱크 150~400)
//  ③ 물리 레이어 12에 "Corpse" 이름 (래그돌 팔다리 — 플레이어·적과의 충돌 제외는 BanditRagdoll이 런타임에)
// 실행: unity command eval_file --file tools/setup_corpse_loot.cs  (편집 모드)
var log = new System.Text.StringBuilder();

// ① 현금 아이템
const string cashPath = "Assets/Resources/Items/Valuable/Cash.asset";
var cash = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(cashPath);
if (cash == null)
{
    cash = ScriptableObject.CreateInstance<ItemData>();
    UnityEditor.AssetDatabase.CreateAsset(cash, cashPath);
    log.AppendLine("현금 아이템 생성: " + cashPath);
}
cash.itemId = CashWallet.ItemId;
cash.displayName = "현금";
cash.description = "레이드에서 주운 돈. 들고 탈출해 안전가옥에 돌아오면 ◈스크랩으로 정산된다. 죽으면 잃는다.";
cash.category = ItemCategory.Valuable;
cash.rarity = ItemRarity.Common;
cash.maxStack = 99999;
cash.weight = 0f;
cash.sellPrice = 1;
cash.buyPrice = 0;
cash.gridWidth = 1;
cash.gridHeight = 1;
UnityEditor.EditorUtility.SetDirty(cash);

// ② StatDB 현금
var db = UnityEditor.AssetDatabase.LoadAssetAtPath<StatDB>("Assets/Resources/Data/StatDB.asset");
void SetCash(string id, int min, int max)
{
    var u = db.units.Find(x => x.id == id);
    if (u == null) { log.AppendLine("StatDB에 없음: " + id); return; }
    u.cashMin = min; u.cashMax = max;
    log.AppendLine("현금 " + id + " = " + min + "~" + max);
}
SetCash("bandit_melee_1", 50, 150);
SetCash("bandit_ranged", 50, 150);
SetCash("bandit_pistol", 80, 200);
SetCash("bandit_rifle", 80, 200);
SetCash("bandit_tank", 150, 400);
typeof(StatDB).GetField("unitMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(db, null);
UnityEditor.EditorUtility.SetDirty(db);

// ③ 레이어 이름
var tm = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
var layer = tm.FindProperty("layers").GetArrayElementAtIndex(BanditRagdoll.CorpseLayer);
if (string.IsNullOrEmpty(layer.stringValue)) { layer.stringValue = "Corpse"; tm.ApplyModifiedProperties(); log.AppendLine("레이어 12 = Corpse"); }
else log.AppendLine("레이어 12 이미: " + layer.stringValue);

UnityEditor.AssetDatabase.SaveAssets();
return log.ToString();
