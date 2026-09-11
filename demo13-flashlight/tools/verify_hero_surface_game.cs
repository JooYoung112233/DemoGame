const string folder="Assets/ChibiSurvivor/Player/DarkSurvivor";
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/DarkSurvivor.prefab");
var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(folder+"/DarkSurvivor.fbx").OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{
 var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,preview);instance.GetComponentInChildren<UnityEngine.Animator>().enabled=false;
 var renderers=instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
 var problems=new System.Collections.Generic.List<string>();
 foreach(var r in renderers){if(r.sharedMesh==null||r.sharedMesh.uv.Length==0)problems.Add(r.name+" UV missing");if(r.sharedMaterials.Any(m=>m==null||m.GetTexture("_BaseMap")==null))problems.Add(r.name+" texture missing");}
 foreach(var clip in clips)foreach(var fraction in new[]{0f,.25f,.5f,.75f,1f}){
  clip.SampleAnimation(instance,clip.length*fraction);
  foreach(var r in renderers){var mesh=new UnityEngine.Mesh();r.BakeMesh(mesh,false);if(mesh.vertices.Any(v=>float.IsNaN(v.x)||float.IsInfinity(v.x)||v.magnitude>5))problems.Add(r.name+" invalid pose "+clip.name);UnityEngine.Object.DestroyImmediate(mesh);}
 }
 var rig=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/PlayerRig.prefab");
 var players=rig.GetComponentsInChildren<TopDownPlayer>(true).Concat(UnityEngine.Object.FindObjectsByType<TopDownPlayer>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None)).Select(p=>{var so=new UnityEditor.SerializedObject(p);return new{name=p.name,model=UnityEditor.AssetDatabase.GetAssetPath(so.FindProperty("character3DPrefab").objectReferenceValue),shader=so.FindProperty("character3DShader").objectReferenceValue?.name};}).ToArray();
 var report=new{problems,meshes=renderers.Length,clips=clips.Select(c=>c.name).ToArray(),players};System.IO.File.WriteAllText(folder+"/SurfaceReview/GameVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
