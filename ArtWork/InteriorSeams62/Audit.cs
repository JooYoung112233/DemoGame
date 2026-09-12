using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class SeamAudit
{
 static Transform Part(Transform art,string suffix)=>art.Cast<Transform>().Single(t=>t.name.Contains("_"+suffix+"_"));
 static Vector3[] Points(Transform art,Transform holder)=>holder.GetComponentsInChildren<MeshFilter>(true).SelectMany(m=>m.sharedMesh.vertices.Select(v=>art.InverseTransformPoint(m.transform.TransformPoint(v)))).ToArray();
 static Bounds BoundsOf(IEnumerable<Vector3> points){var ps=points.ToArray();var b=new Bounds(ps[0],Vector3.zero);foreach(var p in ps)b.Encapsulate(p);return b;}
 static Bounds B(Transform art,string part)=>BoundsOf(Points(art,Part(art,part)));
 static float[] Joins(Transform art){
  var rear=B(art,"RearClosureRoof62");var left=B(art,"LeftClosureRoof62");var right=B(art,"RightClosureRoof62");
  var lw=BoundsOf(Points(art,Part(art,"LeftPartition62")).Where(p=>p.y>2.8f));var rw=BoundsOf(Points(art,Part(art,"RightPartition62")).Where(p=>p.y>2.8f));var back=BoundsOf(Points(art,Part(art,"RearLining62")).Where(p=>p.y>2.8f));
  var front=Points(art,Part(art,"ClosedFrontReturn62"));float li=front.Where(p=>p.x<0).Max(p=>p.x),ri=front.Where(p=>p.x>0).Min(p=>p.x);
  // Positive means a physical gap; negative is a shallow overlap into the wall thickness.
  return new[]{lw.min.x-left.max.x,right.min.x-rw.max.x,rear.min.z-back.max.z,lw.min.x-li,ri-rw.max.x,rear.min.z-left.max.z,rear.min.z-right.max.z};
 }
 public static string Run(){
  var rows=new List<object>();bool pass=true;
  foreach(var b in UnityEngine.Object.FindObjectsByType<BuildingInterior>().OrderBy(b=>b.name)){
   var art=b.transform.Find("Interior_Art62");var joins=Joins(art);bool geometry=joins.All(v=>v<=.001f);
   var so=new SerializedObject(b);var roofs=so.FindProperty("roof");int signs=0;for(int i=0;i<roofs.arraySize;i++){var go=(GameObject)roofs.GetArrayElementAtIndex(i).objectReferenceValue;if(go!=null&&go.TryGetComponent<TextMesh>(out _))signs++;}
   var prefab=PrefabUtility.LoadPrefabContents("Assets/Art/Environments/TownInteriors62/Prefabs/"+b.name+"_Interior62.prefab");bool prefabMatches;
   try{prefabMatches=Joins(prefab.transform).Zip(joins,(a,c)=>Mathf.Abs(a-c)<.001f).All(v=>v);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
   var blockers=art.Find("LockedRooms").GetComponentsInChildren<BoxCollider>();bool aligned=true;
   foreach(var c in blockers){string zone=c.name.Split('.').Last();zone=char.ToUpperInvariant(zone[0])+zone.Substring(1);var roof=B(art,zone+"ClosureRoof62");var cb=new Bounds(c.center,c.size);aligned&=Mathf.Abs(cb.min.x-roof.min.x)<.001f&&Mathf.Abs(cb.max.x-roof.max.x)<.001f&&Mathf.Abs(cb.min.z-roof.min.z)<.001f&&Mathf.Abs(cb.max.z-roof.max.z)<.001f&&c.enabled&&!c.isTrigger;}
   var unlock=b.GetComponent<BuildingUnlock>();bool locked=unlock==null||(!unlock.IsOpen&&b.transform.Find("Shutter").gameObject.activeSelf);
   bool ok=geometry&&prefabMatches&&aligned&&signs==1&&locked;pass&=ok;
   rows.Add(new{b.name,pass=ok,joinsMetres=joins,prefabMatches,blockersAligned=aligned,signBindings=signs,originalShopLockPreserved=locked});
  }
  var json=JsonConvert.SerializeObject(new{pass,joinOrder=new[]{"left roof / wall","right roof / wall","rear roof / wall","left front return / wall","right front return / wall","rear / left lid","rear / right lid"},buildings=rows},Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/InteriorSeams62/FinalAudit.json"),json);return json;
 }
}
