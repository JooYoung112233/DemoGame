using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class InteriorAudit
{
 public static string Run(){
  var buildings=UnityEngine.Object.FindObjectsByType<BuildingInterior>();
  var roots=buildings.Select(b=>b.transform.Find("Interior_Art62")).ToArray();
  var meshes=roots.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>(true)).ToArray();
  var missingUv=meshes.Where(m=>m.sharedMesh==null||!m.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)).Select(m=>m.name).ToArray();
  var materials=roots.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
  var badMaterials=materials.Where(m=>m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader").Select(m=>m==null?"null":m.name).ToArray();
  var shaders=materials.Where(m=>m!=null).Select(m=>m.shader).Distinct().SelectMany(s=>ShaderUtil.GetShaderMessages(s)).Select(e=>new{e.message,e.line,severity=e.severity.ToString()}).ToArray();
  var zones=roots.SelectMany(t=>t.Find("LockedRooms").GetComponentsInChildren<BoxCollider>()).ToArray();
  var locks=buildings.Select(b=>b.GetComponent<BuildingUnlock>()).Where(l=>l!=null).Select(l=>new{l.name,l.UnlockFlag,l.IsOpen,shutter=l.transform.Find("Shutter").gameObject.activeSelf}).ToArray();
  var npc=InteractableObject.All.Single(i=>i.name=="전당포 주인");
  var report=new{pass=roots.Length==5&&missingUv.Length==0&&badMaterials.Length==0&&shaders.Length==0&&zones.Length==15&&zones.All(c=>c.enabled&&!c.isTrigger),buildings=5,meshInstances=meshes.Length,materialCount=materials.Length,missingUv,badMaterials,shaderMessages=shaders,lockedZones=zones.Select(z=>new{z.name,z.enabled,z.isTrigger}),shopLocks=locks,npcPosition=npc.transform.position.ToString()};
  var json=JsonConvert.SerializeObject(report,Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/InteriorIntegration62/FinalAudit.json"),json);return json;
 }
}
