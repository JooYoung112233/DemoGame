const string root="Assets/ChibiSurvivor/Player/DarkSurvivor";
const string folder=root+"/SurfaceReview";
const string fbx=folder+"/DarkSurvivor_Textured.fbx";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
if(!UnityEditor.AssetDatabase.IsValidFolder(folder+"/Materials"))UnityEditor.AssetDatabase.CreateFolder(folder,"Materials");
foreach(var label in new[]{"BaseColor","Normal","Mask"}){
 var t=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(folder+"/Textures/Hero_"+label+".png");
 t.textureType=label=="Normal"?UnityEditor.TextureImporterType.NormalMap:UnityEditor.TextureImporterType.Default;
 t.sRGBTexture=label=="BaseColor";t.convertToNormalmap=false;t.mipmapEnabled=true;t.maxTextureSize=2048;t.alphaSource=UnityEditor.TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=false;
 t.wrapMode=UnityEngine.TextureWrapMode.Clamp;t.filterMode=UnityEngine.FilterMode.Trilinear;t.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
}
var baseMap=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Textures/Hero_BaseColor.png");
var normal=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Textures/Hero_Normal.png");
var mask=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Textures/Hero_Mask.png");
var source=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(root+"/DarkSurvivor.fbx");
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
importer.animationType=source.animationType;importer.avatarSetup=source.avatarSetup;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
importer.importNormals=UnityEditor.ModelImporterNormals.Import;importer.importTangents=UnityEditor.ModelImporterTangents.CalculateMikk;importer.animationCompression=source.animationCompression;importer.resampleCurves=source.resampleCurves;importer.SaveAndReimport();
importer.clipAnimations=source.clipAnimations;
var profiles=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/SurfaceCheck.json"))["profiles"];
foreach(var entry in profiles){
 string name=(string)entry["material"],path=folder+"/Materials/"+name+".mat";
 var original=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"/Materials/"+name+".mat");if(original==null)throw new System.Exception("Missing original material "+name);
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
 if(m==null){m=new UnityEngine.Material(original);m.name=name;UnityEditor.AssetDatabase.CreateAsset(m,path);}
 m.SetTexture("_BaseMap",baseMap);m.SetColor("_BaseColor",UnityEngine.Color.white);
 if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",baseMap);if(m.HasProperty("_Color"))m.SetColor("_Color",UnityEngine.Color.white);
 m.SetTexture("_MetallicGlossMap",mask);m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.SetFloat("_SmoothnessTextureChannel",0);m.EnableKeyword("_METALLICSPECGLOSSMAP");
 m.SetTexture("_OcclusionMap",mask);m.SetFloat("_OcclusionStrength",.35f);m.EnableKeyword("_OCCLUSIONMAP");
 if((string)entry["surface"]!="plain"){m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.7f);m.EnableKeyword("_NORMALMAP");}
 else{m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");}
 UnityEditor.EditorUtility.SetDirty(m);importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),name),m);
}
importer.SaveAndReimport();
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{
 var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx),preview);instance.name="DarkSurvivor_Textured";
 var animator=instance.GetComponentInChildren<UnityEngine.Animator>();animator.runtimeAnimatorController=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>(root+"/DarkSurvivor.controller");animator.applyRootMotion=false;
 foreach(var r in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>()){r.updateWhenOffscreen=true;if(r.name=="Hero_SwordProxy")r.enabled=false;if(r.sharedMesh.uv.Length==0)throw new System.Exception("Missing UV: "+r.name);if(r.sharedMaterials.Any(m=>m==null||m.GetTexture("_BaseMap")==null))throw new System.Exception("Missing texture: "+r.name);}
 var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
 foreach(var n in new[]{"Idle","Walk","Run","SwordWalk","SwordSlash"})if(!clips.Any(c=>c.name==n))throw new System.Exception("Missing clip "+n);
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,folder+"/DarkSurvivor_Textured.prefab");
 UnityEditor.AssetDatabase.SaveAssets();
 var report=new{materials=profiles.Count(),meshes=instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Length,clips=clips.Select(c=>c.name).ToArray(),normalMapImported=true,maskLinear=true,gameplayChanged=false};
 System.IO.File.WriteAllText(folder+"/UnitySurfaceCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
