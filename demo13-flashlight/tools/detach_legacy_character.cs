if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop Play mode before archiving.");
const string current="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/SimpleHero.prefab";
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(current);if(model==null)throw new System.Exception("Current player missing");
var replacements=new System.Collections.Generic.List<string>();
foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
{
 if(go==null||!go.scene.IsValid()||UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(go)!=go)continue;
 if(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)!="Assets/ChibiSurvivor/Player/DarkSurvivor/DarkSurvivor.prefab")continue;
 var scene=go.scene;bool wasDirty=scene.isDirty;var old=go.transform;
 var replacement=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,scene);replacement.name="SimpleHero";
 replacement.transform.SetParent(old.parent,false);replacement.transform.localPosition=old.localPosition;replacement.transform.localRotation=old.localRotation;replacement.transform.localScale=old.localScale;replacement.transform.SetSiblingIndex(old.GetSiblingIndex());replacement.SetActive(go.activeSelf);
 UnityEditor.Undo.RegisterCreatedObjectUndo(replacement,"Replace legacy character");UnityEditor.Undo.DestroyObjectImmediate(go);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!wasDirty&&!string.IsNullOrEmpty(scene.path))UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 replacements.Add(scene.path);
}
const string oldWeapon="Assets/ChibiSurvivor/Player/DarkSurvivor/Weapon_LongSword.asset";
const string weapons="Assets/Resources/WeaponData";
if(!UnityEditor.AssetDatabase.IsValidFolder(weapons))UnityEditor.AssetDatabase.CreateFolder("Assets/Resources","WeaponData");
var destination=UnityEditor.AssetDatabase.GenerateUniqueAssetPath(weapons+"/Weapon_LongSword.asset");
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(oldWeapon)!=null){var error=UnityEditor.AssetDatabase.MoveAsset(oldWeapon,destination);if(!string.IsNullOrEmpty(error))throw new System.Exception(error);}
UnityEditor.AssetDatabase.SaveAssets();return new{replacedScenes=replacements,weaponData=destination};
