var result=new System.Collections.Generic.List<object>();
foreach(var npc in UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None)){
 foreach(var r in npc.GetComponentsInChildren<SkinnedMeshRenderer>()){
  var mesh=new Mesh();r.BakeMesh(mesh,false);var b=new Bounds(r.transform.TransformPoint(mesh.vertices[0]),Vector3.zero);foreach(var p in mesh.vertices)b.Encapsulate(r.transform.TransformPoint(p));
  result.Add(new{id=npc.Data.npcId,rendererBounds=r.bounds.ToString("F3"),bakedFalse=b.ToString("F3"),transform=r.transform.localToWorldMatrix.ToString("F3"),chain=r.GetComponentsInParent<Transform>().Select(t=>t.name+" pos="+t.localPosition.ToString("F3")+" scale="+t.localScale.ToString("F3")).ToArray()});UnityEngine.Object.DestroyImmediate(mesh);
 }
}
return Newtonsoft.Json.JsonConvert.SerializeObject(result);
