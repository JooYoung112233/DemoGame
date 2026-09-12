using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class InteriorInspect
{
 public static string Run(){
  var buildings=UnityEngine.Object.FindObjectsByType<BuildingInterior>(FindObjectsInactive.Include);
  var data=buildings.Select(b=>new {b.name,position=b.transform.position.ToString(),rotation=b.transform.eulerAngles.ToString(),scale=b.transform.lossyScale.ToString(),inside=b.PlayerInside,config=EditorJsonUtility.ToJson(b),children=b.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,b.transform),active=t.gameObject.activeSelf,pos=t.localPosition.ToString(),scale=t.localScale.ToString(),components=t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name)}),colliders=b.GetComponentsInChildren<Collider>(true).Select(c=>new{path=AnimationUtility.CalculateTransformPath(c.transform,b.transform),c.enabled,c.isTrigger,bounds=c.bounds.ToString(),json=EditorJsonUtility.ToJson(c)})});
  var result=JsonConvert.SerializeObject(new{buildings=data,locks=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).Where(c=>c!=null&&c.GetType().Name.Contains("Building")).Select(c=>new{c.name,type=c.GetType().Name,data=EditorJsonUtility.ToJson(c)})},Formatting.Indented);
  File.WriteAllText(Path.GetFullPath("../ArtWork/InteriorIntegration62/Before.json"),result);return "Inspected "+buildings.Length+" buildings";
 }
}
