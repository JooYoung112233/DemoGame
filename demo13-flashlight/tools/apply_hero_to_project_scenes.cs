const string path="Assets/ChibiSurvivor/Player/DarkSurvivor/DarkSurvivor.prefab";
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
var sources=prefab.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).ToDictionary(r=>r.name);
var changed=new System.Collections.Generic.List<string>();
var scenes=new System.Collections.Generic.HashSet<UnityEngine.SceneManagement.Scene>();
foreach(var r in UnityEngine.Object.FindObjectsByType<UnityEngine.SkinnedMeshRenderer>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None)){
 if(!r.gameObject.scene.IsValid()||UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(r.gameObject.scene))continue;
 if(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject)!=path||!sources.TryGetValue(r.name,out var source))continue;
 if(!r.bones.Select(b=>b.name).SequenceEqual(source.bones.Select(b=>b.name)))throw new System.Exception("Skeleton mismatch "+r.name);
 var so=new UnityEditor.SerializedObject(r);
 foreach(var field in new[]{"m_Mesh","m_Materials"}){
  var property=so.FindProperty(field);if(property!=null)UnityEditor.PrefabUtility.RevertPropertyOverride(property,UnityEditor.InteractionMode.AutomatedAction);
  so.Update();
 }
 if(r.sharedMesh!=source.sharedMesh||!r.sharedMaterials.SequenceEqual(source.sharedMaterials))throw new System.Exception("Prefab override remains "+r.name);
 if(r.sharedMesh.uv.Length==0||r.sharedMaterials.Any(m=>m.GetTexture("_BaseMap")==null))throw new System.Exception("Missing textured surface "+r.name);
 scenes.Add(r.gameObject.scene);changed.Add(r.name);
}
foreach(var scene in scenes){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Could not save "+scene.path);}
var report=new{updatedRenderers=changed.Count,scenes=scenes.Select(s=>s.path).ToArray(),mainPrefab=path,originalTransformAndLightingPreserved=true};
System.IO.File.WriteAllText("Assets/ChibiSurvivor/Player/DarkSurvivor/SurfaceReview/ProjectSceneApplied.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
