var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/Safehouse.unity");
try{
 var roots=scene.GetRootGameObjects();var map=roots.Single(g=>g.name=="Map");
 System.Func<UnityEngine.Vector3,float[]> vec=v=>new[]{v.x,v.y,v.z};
 var children=new System.Collections.Generic.List<object>();
 foreach(UnityEngine.Transform t in map.transform){
  var rs=t.GetComponentsInChildren<UnityEngine.MeshRenderer>(true);var bounds=rs.Length>0?rs[0].bounds:new UnityEngine.Bounds(t.position,UnityEngine.Vector3.zero);
  foreach(var r in rs)bounds.Encapsulate(r.bounds);
  children.Add(new{name=t.name,active=t.gameObject.activeSelf,position=vec(t.position),scale=vec(t.localScale),center=vec(bounds.center),size=vec(bounds.size),components=t.GetComponents<UnityEngine.Component>().Select(c=>c==null?"Missing":c.GetType().Name).ToArray()});
 }
 var io=roots.SelectMany(r=>r.GetComponentsInChildren<InteractableObject>(true)).Select(a=>new{name=a.name,type=a.Type.ToString(),position=vec(a.transform.position),range=a.InteractRange}).ToArray();
 var doors=roots.SelectMany(r=>r.GetComponentsInChildren<SceneDoor3D>(true)).Select(d=>new{d.name,position=vec(d.transform.position),config=UnityEditor.EditorJsonUtility.ToJson(d)}).ToArray();
 var spawns=roots.SelectMany(r=>r.GetComponentsInChildren<SpawnPoint>(true)).Select(p=>new{id=p.PointId,position=vec(p.transform.position)}).ToArray();
 var colliders=roots.SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Collider>(true)).Where(c=>!c.isTrigger&&c.enabled&&c.gameObject.activeInHierarchy).Select(c=>new{name=c.name,center=vec(c.bounds.center),size=vec(c.bounds.size)}).ToArray();
 var dressing=map.transform.Find("Dressing");
 var decor=dressing==null?new object[0]:dressing.Cast<UnityEngine.Transform>().Select(t=>(object)new{name=t.name,position=vec(t.position),scale=vec(t.localScale),rotation=vec(t.eulerAngles)}).ToArray();
 var report=new {scene=scene.path,children,interactions=io,doors,spawns,colliders,dressing=decor};
 System.IO.Directory.CreateDirectory("Library/Village01");System.IO.File.WriteAllText("Library/Village01/ExistingLayout.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 return new{children,interactions=io,doors,spawns,dressingCount=decor.Length};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
