using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Newtonsoft.Json;
public static class PawnshopReview {
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static string P(Transform t)=>t.parent==null?t.name:P(t.parent)+"/"+t.name;
 public static string Inspect(){var p=TopDownPlayer.Instance;var rb=p.GetComponent<Rigidbody>();var pawn=UnityEngine.Object.FindObjectsByType<BuildingInterior>().First(b=>b.name=="Pawnshop");
 var data=new{project=Application.dataPath,player=new{pos=V(p.transform.position),p.CanMove,p.IsSprinting,p.IsCrouching,gravity=rb.useGravity,rb.constraints,velocity=V(rb.linearVelocity),stat=JsonUtility.ToJson(StatDB.Instance.playerStat),colliders=p.GetComponentsInChildren<Collider>(true).Select(c=>new{path=P(c.transform),c.enabled,c.isTrigger,min=V(c.bounds.min),max=V(c.bounds.max),shape=EditorJsonUtility.ToJson(c)})},pawn=new{pos=V(pawn.transform.position),scale=V(pawn.transform.lossyScale),pawn.PlayerInside,colliders=pawn.GetComponentsInChildren<Collider>(true).Select(c=>new{path=P(c.transform),c.enabled,c.isTrigger,min=V(c.bounds.min),max=V(c.bounds.max),material=c.sharedMaterial?c.sharedMaterial.name:"default",shape=EditorJsonUtility.ToJson(c)}),meshes=pawn.GetComponentsInChildren<MeshFilter>(true).Select(m=>new{path=P(m.transform),mesh=AssetDatabase.GetAssetPath(m.sharedMesh),enabled=m.GetComponent<Renderer>().enabled,min=V(m.GetComponent<Renderer>().bounds.min),max=V(m.GetComponent<Renderer>().bounds.max)})}};
 string json=JsonConvert.SerializeObject(data,Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/PawnshopEntryReview/Inspection.json"),json);return json;
 }
}
