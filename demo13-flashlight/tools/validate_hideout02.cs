// Saved-scene validation with a 0.28m standing footprint, including low furniture.
const string folder="Assets/Art/Environments/Hideout02";
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool playing=UnityEditor.EditorApplication.isPlaying;
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/Hideout.unity");
try{
 var kit=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Hideout02_Placed");
 var anchors=kit.GetComponentsInChildren<HideoutFacilityAnchor>();var failures=new System.Collections.Generic.List<string>();
 foreach(var a in anchors)foreach(var c in kit.GetComponentsInChildren<BoxCollider>()){
  float distance=float.MaxValue;
  foreach(float height in new[]{.28f,.58f,.88f,1.18f,1.47f}){
   var sample=a.StandPosition+Vector3.up*height;var q=c.transform.InverseTransformPoint(sample)-c.center;var e=c.size*.5f;
   var closest=c.transform.TransformPoint(c.center+new Vector3(Mathf.Clamp(q.x,-e.x,e.x),Mathf.Clamp(q.y,-e.y,e.y),Mathf.Clamp(q.z,-e.z,e.z)));
   distance=Mathf.Min(distance,Vector3.Distance(sample,closest));
  }
  if(distance<.28f)failures.Add(a.moduleKey+" overlaps "+c.name+" at "+distance.ToString("F3")+"m");
 }
 var bad=kit.GetComponentsInChildren<Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
 var dio=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HideoutDiorama>()).Single();
 bool focus=new UnityEditor.SerializedObject(dio).FindProperty("roomCenter").objectReferenceValue!=null;
 var meshes=kit.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).Distinct().ToArray();
 var report=new{facilityCount=anchors.Length,uniqueKeys=anchors.Select(a=>a.moduleKey).Distinct().Count(),missingScripts=bad,roomFocusAssigned=focus,standingRadius=.28f,standingConflicts=failures,surfaceChannels=meshes.All(m=>m.uv.Length==m.vertexCount&&m.normals.Length==m.vertexCount&&m.tangents.Length==m.vertexCount),activeScenePreserved=active==UnityEngine.SceneManagement.SceneManager.GetActiveScene(),playStatePreserved=playing==UnityEditor.EditorApplication.isPlaying};
 System.IO.File.WriteAllText(folder+"/SavedSceneValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
