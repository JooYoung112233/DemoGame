var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(UnityEditor.EditorApplication.isPlaying||scene.path!="Assets/Scenes/MapTool_LookDev.unity")throw new System.Exception("Expected LookDev in edit mode");
const string folder="Assets/ChibiSurvivor/Bandit/Bandit01";
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/Bandit01_Bat.prefab").GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).ToDictionary(r=>r.name);
var bodies=UnityEngine.Object.FindObjectsByType<UnityEngine.SkinnedMeshRenderer>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene&&r.name=="Bandit_Body").ToArray();
foreach(var body in bodies){
 var owner=UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(body.gameObject);var bat=owner.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="Bandit_Bat");
 var style=bat.sharedMaterials.FirstOrDefault(m=>m!=null&&m.shader.name=="BRB/Toon");string key=body.transform.root.name.StartsWith("Bandit_")?body.transform.root.name:"LampHolder";
 string path=folder+"/Materials/LookDev_"+key+".mat";var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
 if(mat==null){mat=style!=null?new UnityEngine.Material(style):ToonMaterial.From(source["Bandit_Body"].sharedMaterial,"LookDev_"+key);UnityEditor.AssetDatabase.CreateAsset(mat,path);}
 mat.SetColor("_BaseColor",UnityEngine.Color.white);mat.SetTexture("_BaseMap",source["Bandit_Body"].sharedMaterial.GetTexture("_BaseMap"));mat.SetTextureScale("_BaseMap",UnityEngine.Vector2.one);mat.SetTextureOffset("_BaseMap",UnityEngine.Vector2.zero);UnityEditor.EditorUtility.SetDirty(mat);
 body.sharedMesh=source["Bandit_Body"].sharedMesh;body.sharedMaterials=new[]{mat};UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(body);UnityEditor.EditorUtility.SetDirty(body);
 var so=new UnityEditor.SerializedObject(bat);UnityEditor.PrefabUtility.RevertPropertyOverride(so.FindProperty("m_Mesh"),UnityEditor.InteractionMode.AutomatedAction);
 if(bat.sharedMesh!=source["Bandit_Bat"].sharedMesh)throw new System.Exception("Old bat mesh override");
}
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");return new{bandits=bodies.Length,renderersPerBandit=2,scene=scene.path};
