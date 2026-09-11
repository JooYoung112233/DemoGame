var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ChibiSurvivor/Player/DarkSurvivor/DarkSurvivor.prefab");
var sources=model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).ToDictionary(r=>r.name);
var updated=new System.Collections.Generic.List<string>();
foreach(var r in UnityEngine.Object.FindObjectsByType<UnityEngine.SkinnedMeshRenderer>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None)){
 if(r.gameObject.scene.path!="Assets/Scenes/MapTool_LookDev.unity"||r.transform.root.name!="Player"||!sources.TryGetValue(r.name,out var source))continue;
 if(!r.bones.Select(b=>b.name).SequenceEqual(source.bones.Select(b=>b.name)))throw new System.Exception("Bone mismatch "+r.name);
 r.sharedMesh=source.sharedMesh;
 // Keep the look-development shader and its settings; refresh only surface input maps.
 var mats=r.sharedMaterials;
 for(int i=0;i<mats.Length&&i<source.sharedMaterials.Length;i++){
  var src=source.sharedMaterials[i];var dst=mats[i];if(dst==null||src==null)continue;
  if(dst.HasProperty("_BaseMap"))dst.SetTexture("_BaseMap",src.GetTexture("_BaseMap"));
  if(dst.HasProperty("_BaseColor"))dst.SetColor("_BaseColor",src.GetColor("_BaseColor"));
 }
 OutlineNormals.Apply(r.gameObject);updated.Add(r.name);
}
UnityEditor.SceneView.RepaintAll();return new{updated=updated.Count,parts=updated};
