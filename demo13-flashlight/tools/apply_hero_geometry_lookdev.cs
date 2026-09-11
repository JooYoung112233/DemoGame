var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(UnityEditor.EditorApplication.isPlaying||scene.path!="Assets/Scenes/MapTool_LookDev.unity")throw new System.Exception("Expected LookDev edit mode");
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ChibiSurvivor/Player/DarkSurvivor/DarkSurvivor.prefab").GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).ToDictionary(r=>r.name);int count=0;
foreach(var root in scene.GetRootGameObjects()){
 if(root.name!="Player")continue;
 foreach(var r in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)){
  if(!source.TryGetValue(r.name,out var src))continue;
  if(!r.bones.Select(b=>b.name).SequenceEqual(src.bones.Select(b=>b.name)))throw new System.Exception("Bone order mismatch "+r.name);
  r.sharedMesh=src.sharedMesh;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);UnityEditor.EditorUtility.SetDirty(r);count++;
 }
}
if(count!=40)throw new System.Exception("Unexpected hero mesh count "+count);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");return new{updated=count,materialsAndSceneTransformsPreserved=true};
