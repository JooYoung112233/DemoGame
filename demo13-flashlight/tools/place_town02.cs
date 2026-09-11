// Visual-only exterior pass: preserve all original colliders, NPCs, doors and interaction data.
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop play mode before placing town art.");
const string path="Assets/Scenes/Safehouse.unity",folder="Assets/Art/Environments/Town02";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
var activeBefore=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(!opened&&scene.isDirty)throw new System.Exception("Safehouse has unsaved changes; preserve them before placement.");
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
 var map=scene.GetRootGameObjects().Single(g=>g.name=="Map");
 System.Func<string,Transform> find=n=>map.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n);
 System.Func<Vector3,float[]> vec=v=>new[]{v.x,v.y,v.z};
 System.Func<string> snapshot=()=>Newtonsoft.Json.JsonConvert.SerializeObject(new{
  io=map.GetComponentsInChildren<InteractableObject>(true).Select(i=>new{i.name,type=i.Type.ToString(),pos=vec(i.transform.position),range=i.InteractRange}).ToArray(),
  spawns=map.GetComponentsInChildren<SpawnPoint>(true).Select(s=>new{id=s.PointId,pos=vec(s.transform.position)}).ToArray(),
  doors=map.GetComponentsInChildren<SceneDoor3D>(true).Select(d=>new{d.name,json=UnityEditor.EditorJsonUtility.ToJson(d)}).ToArray(),
  colliders=map.GetComponentsInChildren<Collider>(true).Select(c=>new{path=UnityEditor.AnimationUtility.CalculateTransformPath(c.transform,map.transform),json=UnityEditor.EditorJsonUtility.ToJson(c),pos=vec(c.transform.position),scale=vec(c.transform.lossyScale)}).ToArray()});
 var before=snapshot();
 System.IO.Directory.CreateDirectory("Library/Town02");System.IO.File.Copy(path,"Library/Town02/Safehouse-before-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
 var mats=System.IO.Directory.GetFiles(folder+"/Materials","*.mat").ToDictionary(p=>System.IO.Path.GetFileNameWithoutExtension(p),p=>UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(p.Replace('\\','/')));
 System.Action<MeshRenderer,Material> set=(r,m)=>{r.sharedMaterials=Enumerable.Repeat(m,r.sharedMaterials.Length).ToArray();r.SetPropertyBlock(null);for(int i=0;i<r.sharedMaterials.Length;i++)r.SetPropertyBlock(null,i);};
 var names=new[]{"Pawnshop","Repair","Medical","Furniture","BlackMarket","Container_Home"};
 foreach(var name in names){
  var building=find(name);bool home=name=="Container_Home",east=name=="Furniture"||name=="BlackMarket";
  foreach(var r in building.GetComponentsInChildren<MeshRenderer>(true))if(r.name!="Floor_In"&&r.name!="Shutter")r.enabled=false;
  foreach(var key in new[]{"Shell","Roof"}){
   string child=key=="Roof"?"Roof_Art02":"Shell_Art02";var old=building.Find(child);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var group=new GameObject(child);group.transform.SetParent(building,false);group.transform.position=home?new Vector3(building.position.x,0,building.position.z):building.position;
   group.transform.rotation=Quaternion.Euler(0,east?-90:0,0);var s=building.lossyScale;group.transform.localScale=new Vector3(1/s.x,1/s.y,1/s.z);
   var model=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Models/"+name+"_"+key+".fbx"),scene);model.transform.SetParent(group.transform,false);
  }
  var interior=building.GetComponent<BuildingInterior>();
  if(interior!=null){var so=new UnityEditor.SerializedObject(interior);var roofs=so.FindProperty("roof");roofs.arraySize=1;roofs.GetArrayElementAtIndex(0).objectReferenceValue=building.Find("Roof_Art02").gameObject;so.ApplyModifiedPropertiesWithoutUndo();}
  foreach(var r in building.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Shutter"))set(r,mats["Town_Olive"]);
 }
 // Legacy container dressing is a sibling under Village01, not under the scaled shell.
 var legacyHomeDetail=map.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name=="Container_Home_Detail");
 if(legacyHomeDetail!=null)foreach(var r in legacyHomeDetail.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
 foreach(var r in map.GetComponentsInChildren<MeshRenderer>(true)){
  string rp=UnityEditor.AnimationUtility.CalculateTransformPath(r.transform,map.transform);
  if(rp.Contains("Village01_Placed/HomeYard/Window01")||rp.Contains("Village01_Placed/HomeYard/Home_Door"))r.enabled=false;
 }
 // Town-only shared material replacements; no existing material asset is overwritten.
 foreach(var r in map.GetComponentsInChildren<MeshRenderer>(true)){
  if(!r.enabled||UnityEditor.AnimationUtility.CalculateTransformPath(r.transform,map.transform).Contains("_Art02/"))continue;
  var mm=r.sharedMaterials;bool changed=false;
  for(int i=0;i<mm.Length;i++){
   if(mm[i]==null)continue;string n=mm[i].name;string target=null;
   if(n.StartsWith("Village_")||n.StartsWith("Safehouse_")){
    string tail=n.Substring(n.IndexOf('_')+1);
    target=tail.Contains("Wood")?(tail.Contains("Dark")?"WoodDark":"Wood"):tail.Contains("Concrete")?"Concrete":tail.Contains("Olive")?"Olive":tail.Contains("Canvas")||tail.Contains("Linen")?"Canvas":tail.Contains("Steel")||tail.Contains("Metal")?"Steel":null;
   }
   if(n.StartsWith("Architecture_"))target=n.Contains("Wood")?"Wood":n.Contains("Roof")?"Roof":"Concrete";
   if(target!=null){mm[i]=mats["Town_"+target];changed=true;}
  }
  if(changed){r.sharedMaterials=mm;r.SetPropertyBlock(null);for(int i=0;i<mm.Length;i++)r.SetPropertyBlock(null,i);}
 }
 // Metric UVs on large legacy boxes prevent stretching a one-metre texture over the whole town.
 foreach(var r in map.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&(r.name=="Ground"||r.name.StartsWith("Road_")||r.name.StartsWith("Wall_")||r.name.StartsWith("Container_D")))){
  var mf=r.GetComponent<MeshFilter>();if(mf==null||mf.sharedMesh==null)continue;
  var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=r.name+"_MetreUV";var vs=mesh.vertices;var ns=mesh.normals;var uv=new Vector2[vs.Length];
  for(int i=0;i<vs.Length;i++){var p=r.transform.TransformPoint(vs[i]);var n=r.transform.TransformDirection(ns[i]);uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y);}
  mesh.uv=uv;mesh.RecalculateTangents();string asset=folder+"/Meshes/"+r.name+".asset";var prior=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(asset);
  if(prior==null){UnityEditor.AssetDatabase.CreateAsset(mesh,asset);mf.sharedMesh=mesh;}else{UnityEditor.EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);mf.sharedMesh=prior;}
  set(r,mats[r.name=="Ground"?"Town_Dirt":r.name.StartsWith("Road_")?"Town_Asphalt":r.name.StartsWith("Container_D")?"Town_Olive":"Town_Concrete"]);
 }
 // Small clusters sit against wall ends, outside existing door and NPC approach lanes.
 var priorDressing=map.transform.Find("Town02_Dressing");if(priorDressing!=null)UnityEngine.Object.DestroyImmediate(priorDressing.gameObject);
 var dress=new GameObject("Town02_Dressing");dress.transform.SetParent(map.transform,false);
 System.Action<string,Vector3,float> prop=(asset,pos,yaw)=>{
  string kit=asset=="WoodCrate01"?"Safehouse01":"Village01";
  var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/"+kit+"/Prefabs/"+asset+".prefab"),scene);
  go.transform.SetParent(dress.transform,false);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);
  foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
  foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){
   var mm=r.sharedMaterials;
   for(int i=0;i<mm.Length;i++){string n=mm[i].name;string key=n.Contains("Wood")?"Wood":n.Contains("Canvas")||n.Contains("Linen")?"Canvas":n.Contains("Concrete")?"Concrete":n.Contains("Steel")||n.Contains("Metal")?"Steel":"Olive";mm[i]=mats["Town_"+key];}
   r.sharedMaterials=mm;
  }
 };
 foreach(var p in new[]{new Vector3(27,0,36.8f),new Vector3(41,0,36.9f),new Vector3(6,0,37.7f),new Vector3(71.7f,0,37.7f),new Vector3(20.1f,0,30.5f),new Vector3(20.1f,0,6),new Vector3(70.8f,0,6.6f)}){
  prop("Pallet01",p,0);prop("WoodCrate01",p+new Vector3(-.3f,.23f,0),-5);prop("WoodCrate01",p+new Vector3(.4f,.23f,.1f),8);prop("OilDrum01",p+new Vector3(1.1f,0,.1f),15);
 }
 // Original functional objects and every original collider are byte-for-byte unchanged.
 if(before!=snapshot())throw new System.Exception("Town pass changed a collider or gameplay connection.");
 int missing=map.GetComponentsInChildren<Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
 if(missing!=0)throw new System.Exception("Missing scripts after town placement.");
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
 var report=new{scene=path,buildingArt=6,newModels=12,newPropPlacements=dress.transform.childCount,gameplayAndCollidersPreserved=true,missingScripts=missing,roofGroups=5,activeScenePreserved=activeBefore==UnityEngine.SceneManagement.SceneManager.GetActiveScene()};
 System.IO.File.WriteAllText(folder+"/SceneValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
