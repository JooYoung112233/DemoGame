var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(UnityEditor.EditorApplication.isPlaying||scene.path!="Assets/Scenes/MapTool_LookDev.unity")throw new System.Exception("Expected LookDev edit mode");
const string folder="Assets/ChibiSurvivor/Player/DarkSurvivor";
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/DarkSurvivor.prefab").GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).ToDictionary(r=>r.name);int count=0;
foreach(var root in scene.GetRootGameObjects()){
 if(root.name!="Player")continue;
 foreach(var r in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)){
  var key=r.name=="Hero_FaceDetails"?"Hero_Nose":r.name;
  if(!source.TryGetValue(key,out var src))continue;
  if(!r.bones.Select(b=>b.name).SequenceEqual(src.bones.Select(b=>b.name)))throw new System.Exception("Bone order mismatch "+r.name);
  r.sharedMesh=src.sharedMesh;
  if(key=="Hero_Head"||key=="Hero_Nose"){
   var old=r.sharedMaterials.FirstOrDefault(m=>m!=null);
   var original=src.sharedMaterials[0];
   if(old!=null&&old.shader.name=="BRB/Toon"){
    string path=folder+"/SurfaceReview/LookDevMaterials/LookDev_"+original.name+".mat";
    var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if(m==null){m=new UnityEngine.Material(old);UnityEditor.AssetDatabase.CreateAsset(m,path);}else{m.CopyPropertiesFromMaterial(old);}
    m.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));m.SetColor("_BaseColor",UnityEngine.Color.white);m.SetTextureScale("_BaseMap",UnityEngine.Vector2.one);m.SetTextureOffset("_BaseMap",UnityEngine.Vector2.zero);UnityEditor.EditorUtility.SetDirty(m);r.sharedMaterials=new[]{m};
   }else r.sharedMaterials=src.sharedMaterials;
   r.name=key;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
  }
  UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);UnityEditor.EditorUtility.SetDirty(r);count++;
 }
}
if(count!=40)throw new System.Exception("Unexpected hero mesh count "+count);
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");return new{updated=count,faceTextureApplied=true,otherMaterialsPreserved=true};
