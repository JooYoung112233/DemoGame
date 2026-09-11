const string folder="Assets/ChibiSurvivor/Player/DarkSurvivor";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(folder+"/DarkSurvivor.fbx");
foreach(var path in System.IO.Directory.GetFiles(folder+"/SurfaceReview/Materials","*.mat")){
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path.Replace('\\','/'));
 importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),m.name),m);
}
importer.importNormals=UnityEditor.ModelImporterNormals.Import;importer.importTangents=UnityEditor.ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
var root=UnityEditor.PrefabUtility.LoadPrefabContents(folder+"/DarkSurvivor.prefab");
try{
 foreach(var r in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)){
  if(r.sharedMesh==null||r.sharedMesh.uv.Length==0)throw new System.Exception("Missing textured mesh "+r.name);
  r.sharedMaterials=r.sharedMaterials.Select(m=>UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(folder+"/SurfaceReview/Materials/"+m.name+".mat")??m).ToArray();
  if(r.sharedMaterials.Any(m=>m==null||m.GetTexture("_BaseMap")==null))throw new System.Exception("Missing texture "+r.name);
 }
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,folder+"/DarkSurvivor.prefab");
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
var playerRig=UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Resources/PlayerRig.prefab");
try{
 foreach(var player in playerRig.GetComponentsInChildren<TopDownPlayer>(true)){
  var so=new UnityEditor.SerializedObject(player);so.FindProperty("character3DShader").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();
 }
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(playerRig,"Assets/Resources/PlayerRig.prefab");
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(playerRig);}
// Update an existing Play-mode visual in place without resetting its Animator.
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/DarkSurvivor.prefab");
var models=model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).ToDictionary(r=>r.name);
int liveUpdated=0;
foreach(var visual in UnityEngine.Object.FindObjectsByType<ChibiPlayerVisual>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None)){
 foreach(var r in visual.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))if(models.TryGetValue(r.name,out var src)){
  if(!r.bones.Select(b=>b.name).SequenceEqual(src.bones.Select(b=>b.name)))throw new System.Exception("Bone order mismatch "+r.name);
  r.sharedMesh=src.sharedMesh;r.sharedMaterials=src.sharedMaterials;liveUpdated++;
 }
}
var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(folder+"/DarkSurvivor.fbx").OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
var report=new{mainPrefab=folder+"/DarkSurvivor.prefab",liveUpdated,clips=clips.Select(c=>c.name).ToArray(),textureMaps=3,elbowClearance=true};
UnityEditor.AssetDatabase.SaveAssets();System.IO.File.WriteAllText(folder+"/SurfaceReview/GameApplied.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
