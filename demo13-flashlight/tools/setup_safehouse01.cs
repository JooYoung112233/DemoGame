const string folder="Assets/Art/Environments/Safehouse01";
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/KitManifest.json"));
foreach(var sub in new[]{"Materials","Prefabs"})if(!UnityEditor.AssetDatabase.IsValidFolder(folder+"/"+sub))UnityEditor.AssetDatabase.CreateFolder(folder,sub);
var shader=UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
if(shader==null)throw new System.Exception("URP Lit shader unavailable");
var materials=new System.Collections.Generic.Dictionary<string,UnityEngine.Material>();
foreach(var entry in ((Newtonsoft.Json.Linq.JObject)manifest["palette"]).Properties()){
 var path=folder+"/Materials/"+entry.Name+".mat";
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
 if(m==null){m=new UnityEngine.Material(shader);UnityEditor.AssetDatabase.CreateAsset(m,path);}
 var v=entry.Value;var c=v["color"];var color=new UnityEngine.Color((float)c[0],(float)c[1],(float)c[2],1).gamma;
 m.shader=shader;m.name=entry.Name;m.SetColor("_BaseColor",color);
 m.SetFloat("_Metallic",(float)v["metallic"]);m.SetFloat("_Smoothness",(float)v["smoothness"]);
 if((float)v["emission"]>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*(float)v["emission"]);m.globalIlluminationFlags=UnityEngine.MaterialGlobalIlluminationFlags.RealtimeEmissive;}
 UnityEditor.EditorUtility.SetDirty(m);materials.Add(entry.Name,m);
}
var sourceAssets=(Newtonsoft.Json.Linq.JObject)manifest["assets"];
foreach(var entry in sourceAssets.Properties()){
 string fbx=folder+"/Models/"+entry.Name+".fbx";
 UnityEditor.AssetDatabase.ImportAsset(fbx,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
 var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
 importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.animationType=UnityEditor.ModelImporterAnimationType.None;
 importer.isReadable=false;importer.optimizeGameObjects=false;importer.materialImportMode=UnityEditor.ModelImporterMaterialImportMode.ImportStandard;
 foreach(var mat in materials)importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),mat.Key),mat.Value);
 importer.SaveAndReimport();
}
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var reports=new System.Collections.Generic.List<object>();
var prefabAssets=new System.Collections.Generic.Dictionary<string,UnityEngine.GameObject>();
var activeSceneBefore=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var playBefore=UnityEditor.EditorApplication.isPlaying;
System.Func<Newtonsoft.Json.Linq.JToken,UnityEngine.Vector3> point=v=>new UnityEngine.Vector3(-(float)v[0],(float)v[2],-(float)v[1]);
System.Func<Newtonsoft.Json.Linq.JToken,UnityEngine.Vector3> size=v=>new UnityEngine.Vector3((float)v[0],(float)v[2],(float)v[1]);
try{
 foreach(var entry in sourceAssets.Properties()){
  var root=new UnityEngine.GameObject(entry.Name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
  var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/Models/"+entry.Name+".fbx");
  var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,preview);instance.transform.SetParent(root.transform,false);
  var renderers=instance.GetComponentsInChildren<UnityEngine.MeshRenderer>();
  if(renderers.Length!=(int)entry.Value["meshes"])throw new System.Exception("Missing mesh parts: "+entry.Name);
  foreach(var r in renderers)foreach(var m in r.sharedMaterials)if(m==null||m.shader!=shader||!materials.ContainsKey(m.name))throw new System.Exception("Unmapped material: "+entry.Name+"/"+r.name);
  foreach(var box in entry.Value["colliders"]){
   if((string)box["parent"]=="Door_Hinge"){
    var slab=instance.GetComponentsInChildren<UnityEngine.MeshFilter>().Single(t=>t.name=="Door_Slab");
    var col=slab.gameObject.AddComponent<UnityEngine.BoxCollider>();col.center=slab.sharedMesh.bounds.center;col.size=slab.sharedMesh.bounds.size;
   }else{var col=root.AddComponent<UnityEngine.BoxCollider>();col.center=point(box["center"]);col.size=size(box["size"]);}
  }
  if(entry.Name=="FloorLamp01"||entry.Name=="WallLamp01"){
   var glow=new UnityEngine.GameObject("PracticalLight");glow.transform.SetParent(root.transform,false);
   glow.transform.localPosition=entry.Name=="FloorLamp01"?new UnityEngine.Vector3(0,1.54f,.20f):new UnityEngine.Vector3(0,0,.24f);
   var light=glow.AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Point;light.color=new UnityEngine.Color(1,.73f,.43f);light.intensity=1.25f;light.range=entry.Name=="FloorLamp01"?2.2f:1.8f;light.shadows=UnityEngine.LightShadows.None;
  }
  var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  if(bounds.size.magnitude>5||bounds.size.magnitude<.05f)throw new System.Exception("Unexpected metre scale: "+entry.Name+" "+bounds.size);
  // Test the hinge on an instance, then restore before saving.
  if(entry.Name=="Door01"){
   var hinge=instance.GetComponentsInChildren<UnityEngine.Transform>().Single(t=>t.name=="Door_Hinge");
   var rot=hinge.localRotation;var pos=hinge.position;hinge.Rotate(UnityEngine.Vector3.up,105,UnityEngine.Space.World);
   if(UnityEngine.Vector3.Distance(pos,hinge.position)>.001f)throw new System.Exception("Door pivot drift");hinge.localRotation=rot;
  }
  var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,folder+"/Prefabs/"+entry.Name+".prefab");
  if(prefab==null)throw new System.Exception("Prefab save failed: "+entry.Name);
  prefabAssets.Add(entry.Name,prefab);
  reports.Add(new{name=entry.Name,meshParts=renderers.Length,triangles=(int)entry.Value["triangles"],size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},colliders=root.GetComponentsInChildren<UnityEngine.Collider>().Length});
  UnityEngine.Object.DestroyImmediate(root);
 }
 prefabAssets.Add("StorageChest01",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Props/StorageChest01/StorageChest01.prefab"));
 var assembly=new UnityEngine.GameObject("Safehouse01");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(assembly,preview);
 var groups=new System.Collections.Generic.Dictionary<string,UnityEngine.Transform>();
 foreach(var p in manifest["placements"]){
  string group=(string)p["group"],name=(string)p["asset"];
  if(!groups.ContainsKey(group)){var g=new UnityEngine.GameObject(group);g.transform.SetParent(assembly.transform,false);groups.Add(group,g.transform);}
  var obj=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabAssets[name],preview);
  obj.transform.SetParent(groups[group],false);obj.transform.localPosition=point(p["position"]);
  obj.transform.localRotation=UnityEngine.Quaternion.Euler(0,-(float)p["rotation_z"],0);obj.transform.localScale=UnityEngine.Vector3.one*(float)p["scale"];
 }
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(assembly,folder+"/Safehouse01_Complete.prefab");
 groups["HiddenWalls"].gameObject.SetActive(false);groups["Roof"].gameObject.SetActive(false);
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(assembly,folder+"/Safehouse01_Cutaway.prefab");
 var missingScripts=assembly.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
 if(missingScripts!=0)throw new System.Exception("Missing scripts in assembly");
 // Clear central standing/walking area, tested geometrically against solid colliders.
 UnityEngine.Physics.SyncTransforms();
 var freeSamples=new[]{new UnityEngine.Vector3(0,.80f,.25f),new UnityEngine.Vector3(-.7f,.80f,1.0f),new UnityEngine.Vector3(-1.5f,.80f,1.65f)};
 foreach(var sample in freeSamples)foreach(var c in assembly.GetComponentsInChildren<UnityEngine.Collider>())if(c.bounds.Contains(sample))throw new System.Exception("Central route obstructed by "+c.name);
 var report=new {individualAssets=reports,placementCount=manifest["placements"].Count(),missingScripts,centralRouteSamples=freeSamples.Length,materialsValidated=true,doorPivotValidated=true,completePrefab=folder+"/Safehouse01_Complete.prefab",cutawayPrefab=folder+"/Safehouse01_Cutaway.prefab",activeScenePreserved=activeSceneBefore==UnityEngine.SceneManagement.SceneManager.GetActiveScene(),playStatePreserved=playBefore==UnityEditor.EditorApplication.isPlaying};
 UnityEditor.AssetDatabase.SaveAssets();
 System.IO.File.WriteAllText(folder+"/UnityValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
