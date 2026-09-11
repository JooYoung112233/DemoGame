const string source="Assets/ChibiSurvivor/Bandit/SimpleBandit/SimpleBandit.prefab";
const string destination="Assets/Resources/Characters/Bandit01.prefab";
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(source);
if(model==null)throw new System.Exception("Prepared bandit missing");
var guid=UnityEditor.AssetDatabase.AssetPathToGUID(destination);
var backup="D:/Demo/ArtWork/SimpleBandit/CombatReview/BeforeRuntimeApply.prefab";
if(!System.IO.File.Exists(backup))System.IO.File.Copy(destination,backup);
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{
 var root=new GameObject("Bandit01");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
 var nested=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,scene);nested.transform.SetParent(root.transform,false);
 var animator=nested.GetComponentInChildren<Animator>();
 if(animator==null||animator.runtimeAnimatorController==null||animator.avatar==null||!animator.avatar.isValid)throw new System.Exception("Invalid animator");
 if(nested.GetComponentsInChildren<SkinnedMeshRenderer>().Length!=1||!nested.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name=="BanditClub"))throw new System.Exception("Body/club layout");
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,destination);
 if(UnityEditor.AssetDatabase.AssetPathToGUID(destination)!=guid)throw new System.Exception("Resource GUID changed");
 return new {applied=destination,source,guid,controller=animator.runtimeAnimatorController.name};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
