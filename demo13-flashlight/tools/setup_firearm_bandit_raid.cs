// 총기 밴딧을 레이드에 넣는다 — docs/bandit-firearms.md §레이드 배치 결정 (2026-09-11). 다시 돌려도 같은 결과.
//  ① Resources/Characters/BanditPistol·BanditRifle — 시안 프리팹의 변형(원본을 고치면 따라온다). 시안 자동 연출은 끈다.
//  ② StatDB에 bandit_pistol·bandit_rifle — bandit_melee_1을 복제(= 같은 드랍 테이블) 후 원거리 수치만 바꾼다.
//  ③ Zone1 씬의 손배치 존 4개 유닛키 교체. 빌더(Zone1GreyboxLayout)도 같은 키라 재빌드해도 유지된다.
// 실행: unity command eval_file --file tools/setup_firearm_bandit_raid.cs  (편집 모드)
var log = new System.Text.StringBuilder();

// ① 프리팹 변형
var ps = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try
{
    foreach (var k in new[] { "Pistol", "Rifle" })
    {
        var src = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChibiSurvivor/Bandit/Firearms/Prefabs/Bandit_" + k + ".prefab");
        if (src == null) throw new System.Exception("원본 프리팹 없음: Bandit_" + k);
        var inst = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(src, ps);
        var pv = inst.GetComponent<BanditFirearmPreview>();
        pv.autoPreview = false; pv.target = null; pv.enabled = false;
        var path = "Assets/Resources/Characters/Bandit" + k + ".prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(inst, path);
        log.AppendLine("프리팹 변형: " + path);
    }
}
finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(ps); }

// ② StatDB 유닛
var db = UnityEditor.AssetDatabase.LoadAssetAtPath<StatDB>("Assets/Resources/Data/StatDB.asset");
var baseUnit = db.units.Find(u => u.id == "bandit_melee_1");
if (baseUnit == null) throw new System.Exception("bandit_melee_1 없음");
UnitStatData Clone() => JsonUtility.FromJson<UnitStatData>(JsonUtility.ToJson(baseUnit));

var pistol = Clone();
pistol.id = "bandit_pistol"; pistol.displayName = "Bandit Pistol";
pistol.maxHp = 34f; pistol.attackDamage = 9f; pistol.attackRange = 9f; pistol.attackSpeed = 0.5f; pistol.attackWindup = 0.7f;
pistol.canBeCancelled = true; pistol.moveSpeed = 2.2f; pistol.detectRange = 10f; pistol.loseRange = 15f;
pistol.rangedWeapon = UnitStatData.RangedWeapon.Pistol; pistol.preferredRange = 6.5f; pistol.projectileSpeed = 16f;
pistol.burstCount = 1; pistol.burstInterval = 0.12f; pistol.spreadDeg = 3f;

var rifle = Clone();
rifle.id = "bandit_rifle"; rifle.displayName = "Bandit Rifle";
rifle.maxHp = 44f; rifle.attackDamage = 6f; rifle.attackRange = 12f; rifle.attackSpeed = 0.38f; rifle.attackWindup = 0.85f;
rifle.canBeCancelled = true; rifle.moveSpeed = 1.9f; rifle.detectRange = 12f; rifle.loseRange = 17f;
rifle.rangedWeapon = UnitStatData.RangedWeapon.Rifle; rifle.preferredRange = 9f; rifle.projectileSpeed = 20f;
// 점사 간격은 플레이어 피격 무적창(Health.HurtIFrame 0.35초)보다 길게 — 0.12초였을 땐 둘째·셋째 탄이 무적창에 전부 먹혀
// 3점사가 늘 1발만 들어갔다(2026-09-11 실측 6회 중 6회). 0.4초면 탄마다 따로 맞고, 첫 발을 보고 빠지면 나머지를 피한다.
rifle.burstCount = 3; rifle.burstInterval = 0.4f; rifle.spreadDeg = 5f;

db.units.RemoveAll(u => u.id == "bandit_pistol" || u.id == "bandit_rifle");
int at = db.units.IndexOf(baseUnit) + 1;
db.units.Insert(at, rifle);
db.units.Insert(at, pistol);
typeof(StatDB).GetField("unitMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(db, null);
UnityEditor.EditorUtility.SetDirty(db);
UnityEditor.AssetDatabase.SaveAssets();
log.AppendLine("StatDB: bandit_pistol, bandit_rifle (bandit_melee_1 복제, 드랍 " + baseUnit.drops.Count + "줄 동일)");

// ③ Zone1 존 교체 — 이름 + 위치로 손배치 존만 고른다(같은 이름의 절차 생성 존이 따로 있다)
string[] names = { "EZ_Apt", "EZ_Lot", "EZ_Mall", "EZ_Tower" };
Vector3[] spots = { new Vector3(152, 0, 80), new Vector3(228, 0, 161), new Vector3(233, 0, 248), new Vector3(238, 0, 68) };
string[] keys = { "bandit_pistol", "bandit_pistol", "bandit_rifle", "bandit_rifle" };
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Zone1.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
int changed = 0;
foreach (var r in sc.GetRootGameObjects())
    foreach (var z in r.GetComponentsInChildren<SpawnZone>(true))
        for (int i = 0; i < names.Length; i++)
        {
            var p = z.transform.position;
            if (z.name != names[i] || new Vector2(p.x - spots[i].x, p.z - spots[i].z).magnitude > 3f) continue;
            if (z.UnitKey != keys[i]) { z.Setup(z.Size, z.EnemyCount, keys[i]); changed++; }
            log.AppendLine("Zone1 " + z.name + " " + p.ToString("F0") + " → " + keys[i] + " x" + z.EnemyCount);
        }
if (changed > 0)
{
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
}
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(sc, true);
log.AppendLine("Zone1 존 변경 " + changed + "개");
return log.ToString();
