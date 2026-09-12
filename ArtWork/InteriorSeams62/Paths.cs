using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class SeamPaths
{
 public static string Run(){
  var m=JObject.Parse(File.ReadAllText(Path.GetFullPath("../ArtWork/TownInteriorsPartition62/KitManifest.json")));var rows=new List<object>();
  foreach(var b in UnityEngine.Object.FindObjectsByType<BuildingInterior>()){
   var art=b.transform.Find("Interior_Art62");var spec=m["buildings"][b.name];float width=(float)spec["serviceWidth"],depth=(float)spec["serviceDepth"],center=(float)spec["serviceCenterZ"];
   const float step=.15f;int nx=Mathf.CeilToInt(width/step),nz=Mathf.CeilToInt(depth/step);var free=new bool[nx,nz];var seen=new bool[nx,nz];
   Vector3 Point(int x,int z)=>art.TransformPoint(new Vector3(-width*.5f+(x+.5f)*step,0,center-depth*.5f+(z+.5f)*step));
   bool Clear(Vector3 p)=>!Physics.OverlapCapsule(p+Vector3.up*.33f,p+Vector3.up*1.49f,.30f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.GetComponentInParent<TopDownPlayer>()==null);
   for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)free[x,z]=Clear(Point(x,z));
   int sx=nx/2,sz=4;var queue=new Queue<Vector2Int>();if(free[sx,sz]){seen[sx,sz]=true;queue.Enqueue(new Vector2Int(sx,sz));}
   var dirs=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
   while(queue.Count>0){var p=queue.Dequeue();foreach(var d in dirs){int x=p.x+d.x,z=p.y+d.y;if(x<0||z<0||x>=nx||z>=nz||seen[x,z]||!free[x,z])continue;seen[x,z]=true;queue.Enqueue(new Vector2Int(x,z));}}
   var targets=new List<object>();int i=0;foreach(var t in spec["approachTargets"]){if(i++==1)continue;int x=Mathf.Clamp(Mathf.RoundToInt(((float)t[0]+width*.5f)/step-.5f),0,nx-1);int z=Mathf.Clamp(Mathf.RoundToInt(((float)t[1]-center+depth*.5f)/step-.5f),0,nz-1);targets.Add(new{x,z,reachable=seen[x,z]});}
   var doors=new List<object>();foreach(var zone in m["expansionZones"][b.name]){var d=zone["door"];var c=d["openingCenter"];var mid=art.TransformPoint(new Vector3((float)c[0],1,(float)c[2]));var forward=art.rotation*Quaternion.Euler(0,(float)d["yaw"],0)*Vector3.forward;var hits=Physics.RaycastAll(mid-forward*.8f,forward,1.6f,~0,QueryTriggerInteraction.Ignore);doors.Add(new{id=(string)zone["id"],blocked=hits.Any(h=>h.collider.name.Contains("StorageDoorLeaf")||h.collider.transform.parent.name=="LockedRooms")});}
   rows.Add(new{name=b.name,targets,doors});
  }
  var json=JsonConvert.SerializeObject(rows,Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/InteriorSeams62/PathValidation.json"),json);return json;
 }
}
