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

// Run through the project-pinned Unity MCP in edit mode. FBX sources are preserved.
public static class SeamApply
{
 const string AssetsRoot="Assets/Art/Environments/TownInteriors62";
 static readonly Dictionary<string,string> Labels=new Dictionary<string,string>{{"Pawnshop","전당포"},{"Repair","수리점"},{"Medical","의료소"},{"Furniture","가구점"},{"BlackMarket","암시장"}};
 static Transform Part(Transform art,string suffix)=>art.Cast<Transform>().Single(t=>t.name.Contains("_"+suffix+"_"));
 static Bounds LocalBounds(Transform art,Transform holder){
  var points=holder.GetComponentsInChildren<MeshFilter>(true).SelectMany(m=>m.sharedMesh.vertices.Select(v=>art.InverseTransformPoint(m.transform.TransformPoint(v)))).ToArray();
  var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);return b;
 }
 static void Fit(Transform art,Transform holder,float left,float right,float front,float back){
  var b=LocalBounds(art,holder);var sx=(right-left)/b.size.x;var sz=(back-front)/b.size.z;
  var pos=holder.localPosition;pos.x=left+(pos.x-b.min.x)*sx;pos.z=front+(pos.z-b.min.z)*sz;
  holder.localPosition=pos;holder.localScale=Vector3.Scale(holder.localScale,new Vector3(sx,1,sz));EditorUtility.SetDirty(holder);
 }
 static Mesh FrontReturn(Transform art,Transform holder,string name,float inner){
  string path=AssetsRoot+"/Meshes/"+name+"_JoinedFrontReturn.asset";
  var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
  var mf=holder.GetComponentsInChildren<MeshFilter>(true).Single();var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=name+"_JoinedFrontReturn";
  var vs=mesh.vertices;var pts=vs.Select(v=>art.InverseTransformPoint(mf.transform.TransformPoint(v))).ToArray();
  float oldInner=pts.Min(p=>Mathf.Abs(p.x)),outer=pts.Max(p=>Mathf.Abs(p.x));
  for(int i=0;i<vs.Length;i++){var p=pts[i];p.x=Mathf.Sign(p.x)*Mathf.LerpUnclamped(inner,outer,(Mathf.Abs(p.x)-oldInner)/(outer-oldInner));vs[i]=mf.transform.InverseTransformPoint(art.TransformPoint(p));}
  mesh.vertices=vs;mesh.RecalculateBounds();mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 static void Adjust(Transform art,string name,JToken spec){
  float W=(float)spec["width"],D=(float)spec["depth"],w=(float)spec["serviceWidth"],d=(float)spec["serviceDepth"],cz=(float)spec["serviceCenterZ"];
  // Partition centrelines, not the nominal service rectangle, are the common join datum.
  float side=w*.5f-.175f,rear=cz+d*.5f-.175f,outer=W*.5f-.06f,front=-D*.5f+.075f,back=D*.5f-.075f;
  foreach(string zone in new[]{"Rear","Left","Right"}){
   float l=zone=="Right"?side:-outer,r=zone=="Left"?-side:outer,f=zone=="Rear"?rear:front,b=zone=="Rear"?back:rear;
   foreach(string suffix in new[]{"ClosureRoof62","ClosedFloor62"})Fit(art,Part(art,zone+suffix),l,r,f,b);
   var box=art.Find("LockedRooms/"+name.ToLowerInvariant()+"."+zone.ToLowerInvariant()).GetComponent<BoxCollider>();
   box.center=new Vector3((l+r)*.5f,1.5f,(f+b)*.5f);box.size=new Vector3(r-l,3,b-f);EditorUtility.SetDirty(box);
  }
  var holder=Part(art,"ClosedFrontReturn62");var joined=FrontReturn(art,holder,name,side);
  var mf=holder.GetComponentsInChildren<MeshFilter>(true).Single();mf.sharedMesh=joined;EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
  foreach(var col in holder.GetComponentsInChildren<MeshCollider>(true)){col.sharedMesh=joined;EditorUtility.SetDirty(col);PrefabUtility.RecordPrefabInstancePropertyModifications(col);}
 }
 public static string Run(){
  if(Application.isPlaying)throw new Exception("Stop Play before scene save");
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/Safehouse.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
  if(scene.isDirty)throw new Exception("Unsaved town edits");
  if(!AssetDatabase.IsValidFolder(AssetsRoot+"/Meshes"))AssetDatabase.CreateFolder(AssetsRoot,"Meshes");
  var manifest=JObject.Parse(File.ReadAllText(Path.GetFullPath("../ArtWork/TownInteriorsPartition62/KitManifest.json")));
  var buildings=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BuildingInterior>(true)).ToArray();
  var texts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)).ToArray();
  foreach(var b in buildings){
   var label=texts.Single(t=>t.name=="Sign_"+Labels[b.name]);var so=new SerializedObject(b);var roofs=so.FindProperty("roof");
   bool linked=false;for(int i=0;i<roofs.arraySize;i++)linked|=roofs.GetArrayElementAtIndex(i).objectReferenceValue==label.gameObject;
   if(!linked){int i=roofs.arraySize;roofs.InsertArrayElementAtIndex(i);roofs.GetArrayElementAtIndex(i).objectReferenceValue=label.gameObject;so.ApplyModifiedPropertiesWithoutUndo();}
   Billboard.Attach(label.transform);label.transform.rotation=Quaternion.Euler(62,0,0);EditorUtility.SetDirty(label.transform);
   Adjust(b.transform.Find("Interior_Art62"),b.name,manifest["buildings"][b.name]);
   string prefab=AssetsRoot+"/Prefabs/"+b.name+"_Interior62.prefab";var go=PrefabUtility.LoadPrefabContents(prefab);
   try{Adjust(go.transform,b.name,manifest["buildings"][b.name]);PrefabUtility.SaveAsPrefabAsset(go,prefab);}finally{PrefabUtility.UnloadPrefabContents(go);}
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
  return "Saved five sign/roof bindings, 15 aligned lids/floors/blockers, and five joined front returns to scene and prefabs";
 }
}
