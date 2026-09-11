if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop play before placement");
const string folder="Assets/Art/Environments/Village01";
const string scenePath="Assets/Scenes/Safehouse.unity";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
if(!scene.isLoaded)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
if(scene.isDirty)throw new System.Exception("Unsaved scene changes; inspect before placement");
var map=scene.GetRootGameObjects().Single(g=>g.name=="Map");
if(map.transform.Find("Village01_Placed")!=null)throw new System.Exception("Village already placed");
System.IO.Directory.CreateDirectory("Library/Village01");
System.IO.File.Copy(scenePath,"Library/Village01/Safehouse-before-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
// Serialize plain numeric coordinates for Unity Vector3 (avoid recursive normalized properties).
System.Func<UnityEngine.Vector3,float[]> vec=v=>new[]{v.x,v.y,v.z};
System.Func<string> snapshot=()=>Newtonsoft.Json.JsonConvert.SerializeObject(new {
 io=map.GetComponentsInChildren<InteractableObject>(true).Select(i=>new{i.name,type=i.Type.ToString(),pos=vec(i.transform.position),range=i.InteractRange}).ToArray(),
 spawns=map.GetComponentsInChildren<SpawnPoint>(true).Select(s=>new{id=s.PointId,pos=vec(s.transform.position)}).ToArray(),
 doors=map.GetComponentsInChildren<SceneDoor3D>(true).Select(d=>new{d.name,json=UnityEditor.EditorJsonUtility.ToJson(d)}).ToArray()});
var before=snapshot();
var root=new UnityEngine.GameObject("Village01_Placed");root.transform.SetParent(map.transform,false);
var groups=new System.Collections.Generic.Dictionary<string,UnityEngine.Transform>();
foreach(var name in new[]{"Pavement","Plaza","HomeYard","Gate","Facade","GroundWear"}){var g=new UnityEngine.GameObject(name);g.transform.SetParent(root.transform,false);groups.Add(name,g.transform);}
var placements=new System.Collections.Generic.List<object>();
var prefabs=new System.Collections.Generic.Dictionary<string,UnityEngine.GameObject>();
foreach(var path in System.IO.Directory.GetFiles(folder+"/Prefabs","*.prefab"))prefabs.Add(System.IO.Path.GetFileNameWithoutExtension(path),UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path.Replace('\\','/')));
foreach(var name in new[]{"WoodCrate01","Jerrycan01","Generator01","WallLamp01"})prefabs.Add(name,UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Environments/Safehouse01/Prefabs/"+name+".prefab"));
System.Func<string,float,float,float,float,string,UnityEngine.GameObject> put=(asset,x,y,z,yaw,group)=>{
 var obj=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabs[asset],scene);
 obj.transform.SetParent(groups[group],false);obj.transform.position=new UnityEngine.Vector3(x,y,z);obj.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);
 placements.Add(new{asset,position=new[]{x,y,z},yaw,group});return obj;
};
var lit=UnityEngine.Shader.Find("Universal Render Pipeline/Lit");var occ=UnityEngine.Shader.Find("Spike/OccluderFX");
System.Func<string,UnityEngine.Color,bool,UnityEngine.Material> material=(name,color,occluding)=>{
 string path=folder+"/Materials/"+name+".mat";var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
 if(m==null){m=new UnityEngine.Material(occluding?occ:lit);UnityEditor.AssetDatabase.CreateAsset(m,path);}
 m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.10f);UnityEditor.EditorUtility.SetDirty(m);return m;
};
var wallMat=material("Architecture_Plaster",new UnityEngine.Color(.46f,.455f,.408f),true);
var baseMat=material("Architecture_Footing",new UnityEngine.Color(.285f,.30f,.27f),true);
var roofMat=material("Architecture_Roof",new UnityEngine.Color(.26f,.285f,.266f),true);
var woodMat=material("Architecture_Wood",new UnityEngine.Color(.33f,.27f,.19f),true);
var roadMat=material("Ground_Road",new UnityEngine.Color(.26f,.275f,.263f),false);
var dirtMat=material("Ground_Dirt",new UnityEngine.Color(.31f,.305f,.273f),false);
System.Func<string,UnityEngine.Vector3,UnityEngine.Vector3,UnityEngine.Material,UnityEngine.Transform,UnityEngine.GameObject> box=(name,pos,size,mat,parent)=>{
 var o=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);o.name=name;o.transform.position=pos;o.transform.localScale=size;
 UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(o,scene);UnityEngine.Object.DestroyImmediate(o.GetComponent<UnityEngine.Collider>());o.GetComponent<UnityEngine.Renderer>().sharedMaterial=mat;o.transform.SetParent(parent,true);return o;
};
map.transform.Find("Ground").GetComponent<UnityEngine.Renderer>().sharedMaterial=dirtMat;
var old=map.transform.Find("Dressing");
foreach(UnityEngine.Transform t in old){
 if(t.name.StartsWith("Road_")){t.GetComponent<UnityEngine.Renderer>().sharedMaterial=roadMat;continue;}
 if(!t.name.StartsWith("Sign_"))t.gameObject.SetActive(false);
}
// Connected paths: large original roads remain traversable; paving adds readable edges.
for(int x=26;x<=72;x+=2)for(int z=35;z<=37;z+=2)put("PavingSlab01",x,.012f,z,((x+z)%3)*90,"Pavement");
for(int x=38;x<=50;x+=2)for(int z=25;z<=31;z+=2)put("PavingSlab01",x,.018f,z,((x+z)%4)*90,"Pavement");
for(int z=13;z<=27;z+=2)for(int x=54;x<=56;x+=2){if(x==54&&z>=19&&z<=21)continue;put("PavingSlab01",x,.012f,z,0,"Pavement");}
for(int x=26;x<=72;x+=2){if(x>=32&&x<=36)continue;put("Curb01",x,0,33.8f,0,"Pavement");}
for(int x=38;x<=50;x+=2)put("Curb01",x,0,23.8f,0,"Pavement");
foreach(var p in new[]{new UnityEngine.Vector2(27,34.5f),new UnityEngine.Vector2(49,34.5f),new UnityEngine.Vector2(69,34.5f),new UnityEngine.Vector2(53,14)})put("Drain01",p.x,.04f,p.y,90,"Pavement");

// Existing building footprints, door gaps, colliders and roof visibility remain authoritative.
foreach(var entry in new[]{new{name="Pawnshop",x=34f,z=44f,w=16f,d=12f,east=false},new{name="Repair",x=12f,z=44f,w=14f,d=10f,east=false},new{name="Medical",x=64f,z=44f,w=18f,d=10f,east=false},new{name="Furniture",x=12f,z=27f,w=14f,d=10f,east=true},new{name="BlackMarket",x=12f,z=10f,w=14f,d=12f,east=true}}){
 var building=map.transform.Find(entry.name);var roof=building.Find("Roof");
 foreach(var r in building.GetComponentsInChildren<UnityEngine.MeshRenderer>()){
  if(r.name=="Roof")r.sharedMaterial=roofMat;
  else if(r.name.StartsWith(entry.name+"_"))r.sharedMaterial=wallMat;
 }
 var detail=new UnityEngine.GameObject("Village01_Detail");detail.transform.SetParent(building,false);
 float front=entry.z-entry.d/2-.19f;float side=entry.x+entry.w/2+.19f;
 if(!entry.east){
  foreach(float dx in new[]{-entry.w*.32f,entry.w*.32f}){var w=put("Window01",entry.x+dx,2.1f,front,180,"Facade");w.transform.SetParent(detail.transform,true);}
  var a=put("Awning01",entry.x,2.85f,front,180,"Facade");a.transform.SetParent(detail.transform,true);
  put("ShopSign01",entry.x,3.52f,front,180,"Facade").transform.SetParent(detail.transform,true);
  for(int sideSign=-1;sideSign<=1;sideSign+=2){float width=entry.w/2-1f;box("Plinth",new UnityEngine.Vector3(entry.x+sideSign*(width/2+1),.22f,front+.05f),new UnityEngine.Vector3(width,.44f,.20f),baseMat,detail.transform);}
 }else{
  foreach(float dz in new[]{-entry.d*.30f,entry.d*.30f})put("Window01",side,2.1f,entry.z+dz,90,"Facade").transform.SetParent(detail.transform,true);
  put("Awning01",side,2.85f,entry.z,90,"Facade").transform.SetParent(detail.transform,true);
  put("ShopSign01",side,3.52f,entry.z,90,"Facade").transform.SetParent(detail.transform,true);
 }
 // Low roof seams and perimeter caps disappear with the original Roof renderer group.
 foreach(float dz in new[]{-entry.d/2,entry.d/2})box("Roof_Coping",new UnityEngine.Vector3(entry.x,4.79f,entry.z+dz),new UnityEngine.Vector3(entry.w+.5f,.14f,.18f),baseMat,roof);
 for(float x=entry.x-entry.w/2+.4f;x<entry.x+entry.w/2;x+=1.2f)box("Roof_Seam",new UnityEngine.Vector3(x,4.81f,entry.z),new UnityEngine.Vector3(.055f,.035f,entry.d),roofMat,roof);
 foreach(float dx in new[]{-entry.w/2,entry.w/2})foreach(float dz in new[]{-entry.d/2,entry.d/2})box("Corner_Repair",new UnityEngine.Vector3(entry.x+dx,2.2f,entry.z+dz),new UnityEngine.Vector3(.28f,4.4f,.28f),baseMat,detail.transform);
 box("Roof_VentBase",new UnityEngine.Vector3(entry.x+entry.w*.28f,4.92f,entry.z+1),new UnityEngine.Vector3(1,.20f,.8f),baseMat,roof);
 box("Roof_Vent",new UnityEngine.Vector3(entry.x+entry.w*.28f,5.12f,entry.z+1),new UnityEngine.Vector3(.7f,.22f,.55f),roofMat,roof);
 // Back and side plaster patches give large wall surfaces some scale.
 box("Wall_RepairPanel",new UnityEngine.Vector3(entry.x-entry.w/2-.17f,1.3f,entry.z),new UnityEngine.Vector3(.08f,1.9f,2.2f),baseMat,detail.transform);
}
// Keep labels readable from the established quarter-view camera.
foreach(UnityEngine.Transform t in old)if(t.name.StartsWith("Sign_")){t.position+=UnityEngine.Vector3.up*.48f;var text=t.GetComponent<UnityEngine.TextMesh>();if(text!=null)text.color=new UnityEngine.Color(.86f,.81f,.65f);}

// Corrugation and corner frames on existing container shells, with the west entrance unobstructed.
foreach(var name in new[]{"Container_Home","Container_D1","Container_D2"}){
 var shell=map.transform.Find(name);var b=shell.GetComponent<UnityEngine.Renderer>().bounds;shell.GetComponent<UnityEngine.Renderer>().sharedMaterial=roofMat;
 var detail=new UnityEngine.GameObject(name+"_Detail");detail.transform.SetParent(groups["HomeYard"],false);
 for(float x=b.min.x+.2f;x<b.max.x;x+=.42f)foreach(float z in new[]{b.min.z-.035f,b.max.z+.035f})box("Corrugation",new UnityEngine.Vector3(x,b.center.y,z),new UnityEngine.Vector3(.085f,b.size.y-.12f,.09f),baseMat,detail.transform);
 foreach(float z in new[]{b.min.z,b.max.z}){box("Top_Rail",new UnityEngine.Vector3(b.center.x,b.max.y,z),new UnityEngine.Vector3(b.size.x,.12f,.16f),baseMat,detail.transform);box("Bottom_Rail",new UnityEngine.Vector3(b.center.x,.09f,z),new UnityEngine.Vector3(b.size.x,.18f,.16f),baseMat,detail.transform);}
 foreach(float x in new[]{b.min.x,b.max.x})foreach(float z in new[]{b.min.z,b.max.z})box("Container_Corner",new UnityEngine.Vector3(x,b.center.y,z),new UnityEngine.Vector3(.18f,b.size.y,.18f),baseMat,detail.transform);
}
put("Awning01",56.9f,2.55f,12,270,"HomeYard");
// A visible west door aligned with the existing SceneDoor3D trigger.
box("Home_Door",new UnityEngine.Vector3(56.94f,1.05f,12),new UnityEngine.Vector3(.09f,2.1f,1.20f),woodMat,groups["HomeYard"]);
box("Home_Door_Handle",new UnityEngine.Vector3(56.87f,1,11.59f),new UnityEngine.Vector3(.12f,.055f,.17f),baseMat,groups["HomeYard"]);
put("Window01",61,1.75f,7.43f,180,"HomeYard");put("Window01",67,1.75f,7.43f,180,"HomeYard");

// Replace only visual placeholders; interactables and existing collider references stay in place.
foreach(var name in new[]{"Board_Dispatch","Board_Quest"}){
 var original=map.transform.Find(name);original.GetComponent<UnityEngine.MeshRenderer>().enabled=false;
 var visual=put("NoticeBoard01",original.position.x,0,original.position.z,180,"Gate");
 foreach(var c in visual.GetComponentsInChildren<UnityEngine.Collider>())UnityEngine.Object.DestroyImmediate(c);
 visual.transform.SetParent(original,true);
}
foreach(var p in new[]{new UnityEngine.Vector2(30,31.5f),new UnityEngine.Vector2(58,31.5f),new UnityEngine.Vector2(22,38.5f),new UnityEngine.Vector2(52,38.5f),new UnityEngine.Vector2(52.4f,15),new UnityEngine.Vector2(70,26.5f)}){
 var lamp=put("StreetLamp01",p.x,0,p.y,180,"Plaza");var g=new UnityEngine.GameObject("PracticalLight");g.transform.SetParent(lamp.transform,false);g.transform.localPosition=new UnityEngine.Vector3(0,3.2f,.55f);
 var l=g.AddComponent<UnityEngine.Light>();l.type=UnityEngine.LightType.Point;l.color=new UnityEngine.Color(1,.76f,.46f);l.intensity=.9f;l.range=4;l.shadows=UnityEngine.LightShadows.None;
}
put("Brazier01",44,0,29,0,"Plaza");
put("Bench01",40.5f,0,31.6f,180,"Plaza");put("Bench01",47.5f,0,31.6f,180,"Plaza");
put("Bench01",46.8f,0,26,0,"Plaza");put("Bench01",69.7f,0,31.8f,180,"Gate");
foreach(float x in new[]{39f,49f})put("Bollard01",x,0,32.8f,0,"Plaza");
// Merchant's small supply corner, offset from the NPC interaction position (36,14).
put("Pallet01",33.8f,0,14.6f,-8,"HomeYard");put("WoodCrate01",33.8f,.23f,14.6f,-8,"HomeYard");put("WoodCrate01",33.85f,.68f,14.6f,3,"HomeYard");put("OilDrum01",33.5f,0,16.1f,8,"HomeYard");
foreach(var p in new[]{new UnityEngine.Vector2(28,38.7f),new UnityEngine.Vector2(40.2f,38.7f),new UnityEngine.Vector2(7,38.4f),new UnityEngine.Vector2(72.3f,38.6f),new UnityEngine.Vector2(62.5f,18),new UnityEngine.Vector2(74.8f,20)}){
 put("Pallet01",p.x,0,p.y,0,"HomeYard");put("WoodCrate01",p.x-.22f,.23f,p.y,0,"HomeYard");put("WoodCrate01",p.x+.34f,.23f,p.y+.16f,90,"HomeYard");put("WoodCrate01",p.x-.22f,.68f,p.y,5,"HomeYard");put("OilDrum01",p.x+1.1f,0,p.y+.1f,20,"HomeYard");
}
put("Generator01",60.5f,0,18,180,"HomeYard");put("Jerrycan01",59.7f,0,18.2f,12,"HomeYard");put("Tires01",71.7f,0,18,0,"HomeYard");
foreach(float x in new[]{60f,62f,64f,66f,68f})put("LowFence01",x,0,5.8f,0,"HomeYard");
foreach(float x in new[]{75.5f,77.5f})foreach(float z in new[]{25.6f,32.5f})put("Sandbags01",x,0,z,0,"Gate");
put("Tires01",74.7f,0,33.3f,0,"Gate");
foreach(var p in new[]{new UnityEngine.Vector2(25,39),new UnityEngine.Vector2(42.8f,48),new UnityEngine.Vector2(5.5f,20),new UnityEngine.Vector2(21,8),new UnityEngine.Vector2(73.5f,42),new UnityEngine.Vector2(59,6.5f),new UnityEngine.Vector2(73,16)})put("Rubble01",p.x,.01f,p.y,30,"GroundWear");

// Preserve the scene's functional objects exactly, then record editable placement inventory.
if(before!=snapshot())throw new System.Exception("Interaction / spawn / door configuration changed");
UnityEngine.Physics.SyncTransforms();
int missing=map.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
if(missing>0)throw new System.Exception("Missing scripts");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllText(folder+"/ScenePlacement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scenePath,footprint=new[]{80,56},placements,interactionWiringPreserved=true,missingScripts=missing},Newtonsoft.Json.Formatting.Indented));
UnityEditor.Selection.activeGameObject=root;
return new{scene=scenePath,prefabPlacements=placements.Count,interactionWiringPreserved=true,missingScripts=missing};
