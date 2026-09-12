using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
public static class SeamInspect {
 static string P(Transform t)=>t.parent==null?t.name:P(t.parent)+"/"+t.name;
 static object BoundsIn(Transform root, Transform t){var points=t.GetComponentsInChildren<MeshFilter>(true).SelectMany(m=>m.sharedMesh.vertices.Select(v=>root.InverseTransformPoint(m.transform.TransformPoint(v)))).ToArray();if(points.Length==0)return null;var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);return new{min=b.min.ToString("F4"),max=b.max.ToString("F4")};}
 public static string Run(){var bs=UnityEngine.Object.FindObjectsByType<BuildingInterior>();var result=new{player=TopDownPlayer.Instance.transform.position.ToString(),texts=UnityEngine.Object.FindObjectsByType<TextMesh>().Select(t=>new{path=P(t.transform),t.text,position=t.transform.position.ToString(),size=t.characterSize,components=t.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray(),parentComponents=t.transform.parent.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray()}),buildings=bs.Select(b=>new{b.name,b.PlayerInside,parts=b.transform.Find("Interior_Art62").Cast<Transform>().Where(t=>t.name.Contains("62_")||t.name.Contains("LockedRooms")).Select(t=>new{t.name,position=t.localPosition.ToString(),rotation=t.localEulerAngles.ToString(),bounds=BoundsIn(b.transform.Find("Interior_Art62"),t)})})};var json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/InteriorSeams62/Before.json"),json);return "Saved inspection to Before.json";}
}
