using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class InteriorApply
{
 const string Root="Assets/Art/Environments/TownInteriors62";
 static Vector3 V(JToken t)=>new Vector3((float)t[0],(float)t[1],(float)t[2]);
 static bool Include(JToken p)=>!((string)p["asset"]).Contains("PreviewCutaway") && (string)p["kind"]!="previewOnly";
 public static string Run(){
  if(Application.isPlaying)throw new Exception("Stop Play before authored scene save");
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/Safehouse.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
  if(scene.isDirty)throw new Exception("Unsaved town edits");
  var m=JObject.Parse(File.ReadAllText(Path.GetFullPath("../ArtWork/TownInteriorsPartition62/KitManifest.json")));
  Directory.CreateDirectory(Root+"/Prefabs");
  var map=scene.GetRootGameObjects().Single(g=>g.name=="Map");var buildings=map.GetComponentsInChildren<BuildingInterior>(true);
  var report=new List<object>();
  foreach(var b in buildings){
   string name=b.name;var spec=m["buildings"][name];var layout=m["layouts"][name];
   if(b.transform.Find("Interior_Art62")!=null)throw new Exception("Already integrated: "+name);
   var art=new GameObject("Interior_Art62");art.transform.SetParent(b.transform,false);art.transform.localRotation=Quaternion.Euler(0,name=="Furniture"||name=="BlackMarket"?-90:0,0);
   var hide=new List<GameObject>();
   foreach(string n in new[]{"Roof_Art02","Shell_Art02"}){var t=b.transform.Find(n);if(t==null)throw new Exception(name+" missing "+n);hide.Add(t.gameObject);}
   int instances=0,solids=0;
   foreach(var p in layout.Where(Include)){
    string asset=(string)p["asset"],kind=(string)p["kind"];
    var holder=new GameObject(asset+"_"+instances);holder.transform.SetParent(art.transform,false);holder.transform.localPosition=V(p["position"]);holder.transform.localRotation=Quaternion.Euler(0,(float)p["yaw"],0);holder.transform.localScale=Vector3.one*(float)p["scale"];
    var src=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+asset+".fbx");if(src==null)throw new Exception("Missing model "+asset);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(src,holder.transform);
    // Preserve FBX import rotation and centimetre conversion on the model itself.
    // Authored floor tops sit about 2 cm above the town ground; visual only, no raised collider.
    if(kind=="obstacle"||kind=="lockedDoor"||kind=="expandablePartition"||kind=="fixedRoomDivider"||kind=="permanentClosure"||asset.Contains("RearLining")){
     foreach(var mf in model.GetComponentsInChildren<MeshFilter>()){var col=mf.gameObject.AddComponent<MeshCollider>();col.sharedMesh=mf.sharedMesh;solids++;}
    }
    // Omit high camera-facing lining from the inside view, preserving its colliders.
    if(asset.Contains("TallLining") || ((float)spec["worldPlacementXYZYaw"][3]==90 && asset.Contains("LeftPartition")))hide.Add(holder);
    instances++;
   }
   var zones=new GameObject("LockedRooms");zones.transform.SetParent(art.transform,false);
   foreach(var zone in m["expansionZones"][name]){var go=new GameObject((string)zone["id"]);go.transform.SetParent(zones.transform,false);var box=go.AddComponent<BoxCollider>();box.center=V(zone["boundsCenter"]);box.size=V(zone["boundsSize"]);}
   var lightGo=new GameObject("ServiceLight");lightGo.transform.SetParent(art.transform,false);lightGo.transform.localPosition=new Vector3(0,2.7f,(float)spec["serviceCenterZ"]);
   var light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.92f,.80f);light.intensity=1.8f;light.range=7;light.shadows=LightShadows.Soft;
   var serial=new SerializedObject(b);var roofs=serial.FindProperty("roof");roofs.arraySize=hide.Count;for(int i=0;i<hide.Count;i++)roofs.GetArrayElementAtIndex(i).objectReferenceValue=hide[i];var lights=serial.FindProperty("interiorLights");lights.arraySize=1;lights.GetArrayElementAtIndex(0).objectReferenceValue=light;serial.ApplyModifiedPropertiesWithoutUndo();
   var floor=b.transform.Find("Floor_In");if(floor!=null)foreach(var r in floor.GetComponentsInChildren<Renderer>())r.enabled=false;
   // Save an independently reusable assembly without changing its scene rotation.
   var rotation=art.transform.localRotation;art.transform.localRotation=Quaternion.identity;PrefabUtility.SaveAsPrefabAsset(art,Root+"/Prefabs/"+name+"_Interior62.prefab");art.transform.localRotation=rotation;
   report.Add(new{name,instances,meshColliders=solids,lockedRooms=3,world=art.transform.position.ToString(),yaw=art.transform.eulerAngles.y});
  }
  var old=map.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Pawnshop_Interior");
  var npc=old.GetComponentsInChildren<InteractableObject>(true).Single();
  // NPC pivot is its capsule centre (.9m), not the player's ground pivot.
  var pawn=buildings.Single(b=>b.name=="Pawnshop");npc.transform.position=pawn.transform.position+new Vector3(0,.9f,-3.9f);
  foreach(Transform t in old)if(t!=npc.transform)t.gameObject.SetActive(false);
  EditorUtility.SetDirty(npc.transform);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
  var result=JsonConvert.SerializeObject(new{buildings=report,npcPosition=npc.transform.position.ToString()},Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/InteriorIntegration62/Applied.json"),result);return result;
 }
}
