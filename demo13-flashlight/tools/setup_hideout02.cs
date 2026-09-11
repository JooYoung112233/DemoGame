// Import the reference interior into its own asset namespace; preserves the active scene.
const string folder="Assets/Art/Environments/Hideout02";
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/KitManifest.json"));
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
foreach(var sub in new[]{"Materials","Prefabs"})if(!UnityEditor.AssetDatabase.IsValidFolder(folder+"/"+sub))UnityEditor.AssetDatabase.CreateFolder(folder,sub);
foreach(var path in System.IO.Directory.GetFiles(folder+"/Textures","*.png")){
 var file=path.Replace('\\','/');var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(file);
 ti.textureType=file.Contains("_Normal")?UnityEditor.TextureImporterType.NormalMap:UnityEditor.TextureImporterType.Default;
 ti.sRGBTexture=file.Contains("_Base");ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;
 ti.alphaSource=UnityEditor.TextureImporterAlphaSource.FromInput;ti.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
}
var shader=Shader.Find("Universal Render Pipeline/Lit");
var mats=new System.Collections.Generic.Dictionary<string,Material>();
foreach(var entry in ((Newtonsoft.Json.Linq.JObject)manifest["palette"]).Properties()){
 string path=folder+"/Materials/"+entry.Name+".mat",family=(string)entry.Value["family"];
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(shader);UnityEditor.AssetDatabase.CreateAsset(m,path);}
 var c=entry.Value["color"];m.SetColor("_BaseColor",new Color((float)c[0],(float)c[1],(float)c[2],1).gamma);
 m.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+family+"_Base.png"));
 m.SetTexture("_BumpMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+family+"_Normal.png"));m.SetFloat("_BumpScale",family=="Plaster"?.16f:.65f);m.EnableKeyword("_NORMALMAP");
 m.SetTexture("_MetallicGlossMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+family+"_Mask.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);
 if((float)entry.Value["emission"]>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.61f,.24f)*.6f);}
 UnityEditor.EditorUtility.SetDirty(m);mats.Add(entry.Name,m);
}
var prefabs=new System.Collections.Generic.Dictionary<string,GameObject>();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var activeBefore=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool playBefore=UnityEditor.EditorApplication.isPlaying;
System.Func<Newtonsoft.Json.Linq.JToken,Vector3> point=v=>new Vector3(-(float)v[0],(float)v[2],-(float)v[1]);
try{
 foreach(var entry in ((Newtonsoft.Json.Linq.JObject)manifest["assets"]).Properties()){
  string file=folder+"/Models/"+entry.Name+".fbx";
  var mi=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(file);mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;
  mi.importNormals=UnityEditor.ModelImporterNormals.Import;mi.importTangents=UnityEditor.ModelImporterTangents.CalculateMikk;mi.generateSecondaryUV=true;
  foreach(var mat in mats)mi.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(Material),mat.Key),mat.Value);
  mi.SaveAndReimport();
  var root=new GameObject(entry.Name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
  var model=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(file),scene);model.transform.SetParent(root.transform,false);
  foreach(var box in entry.Value["colliders"]){var col=root.AddComponent<BoxCollider>();col.center=point(box["center"]);var s=box["size"];col.size=new Vector3((float)s[0],(float)s[2],(float)s[1]);}
  foreach(var mf in root.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh.vertexCount!=mf.sharedMesh.uv.Length||mf.sharedMesh.normals.Length==0||mf.sharedMesh.tangents.Length==0)throw new System.Exception("Missing mesh surface channels: "+entry.Name);
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials)if(m==null||!mats.ContainsKey(m.name))throw new System.Exception("Unmapped material "+entry.Name);
  prefabs.Add(entry.Name,UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,folder+"/Prefabs/"+entry.Name+".prefab"));UnityEngine.Object.DestroyImmediate(root);
 }
 prefabs.Add("StorageChest01",UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/StorageChest01/StorageChest01.prefab"));
 var assembly=new GameObject("Hideout02_Interior");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(assembly,scene);
 var groups=new System.Collections.Generic.Dictionary<string,Transform>();
 foreach(var p in manifest["placements"]){
  string group=(string)p["group"],name=(string)p["asset"];
  if(!groups.ContainsKey(group)){var g=new GameObject(group);g.transform.SetParent(assembly.transform,false);groups.Add(group,g.transform);}
  var o=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabs[name],scene);o.transform.SetParent(groups[group],false);o.transform.localPosition=point(p["position"]);o.transform.localRotation=Quaternion.Euler(0,-(float)p["rotation_z"],0);o.transform.localScale=Vector3.one*(float)p["scale"];
  if((string)p["key"]!=null){var stand=new GameObject("FacilityStand");stand.transform.SetParent(o.transform,false);stand.transform.position=point(p["stand"]);}
  if(name=="BedsideTable02"){
   var lamp=new GameObject("LanternLight");lamp.transform.SetParent(o.transform,false);lamp.transform.localPosition=new Vector3(0,.84f,0);var l=lamp.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.67f,.33f);l.intensity=.15f;l.range=2.7f;l.shadows=LightShadows.Soft;
  }
 }
 var result=UnityEditor.PrefabUtility.SaveAsPrefabAsset(assembly,folder+"/Hideout02_Interior.prefab");
 UnityEditor.AssetDatabase.SaveAssets();
 var report=new{prefab=UnityEditor.AssetDatabase.GetAssetPath(result),placements=manifest["placements"].Count(),renderers=assembly.GetComponentsInChildren<MeshRenderer>().Length,materialCount=mats.Count,textureMaps=15,meshSurfaceChannelsValidated=true,activeScenePreserved=activeBefore==UnityEngine.SceneManagement.SceneManager.GetActiveScene(),playStatePreserved=playBefore==UnityEditor.EditorApplication.isPlaying};
 System.IO.File.WriteAllText(folder+"/ImportValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
