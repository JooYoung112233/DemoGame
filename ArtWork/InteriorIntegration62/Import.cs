using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class InteriorImport
{
 public const string Root="Assets/Art/Environments/TownInteriors62";
 public static JObject Manifest()=>JObject.Parse(File.ReadAllText(Path.GetFullPath("../ArtWork/TownInteriorsPartition62/KitManifest.json")));
 public static bool Include(JToken p)=>!((string)p["asset"]).Contains("PreviewCutaway") && (string)p["kind"]!="previewOnly";
 public static string Run(){
  if(Application.isPlaying)throw new Exception("Stop Play before asset import");
  var manifest=Manifest();Directory.CreateDirectory(Root+"/Models");Directory.CreateDirectory(Root+"/Materials");
  var names=((JObject)manifest["layouts"]).Properties().SelectMany(p=>p.Value).Where(Include).Select(p=>(string)p["asset"]).Distinct().ToArray();
  var known=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".mat")).Select(p=>AssetDatabase.LoadAssetAtPath<Material>(p)).Where(m=>m!=null).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.First());
  var remapped=new List<object>();var mats=new Dictionary<string,Material>();
  foreach(string name in names){
   string path=Root+"/Models/"+name+".fbx";File.Copy(Path.GetFullPath("../ArtWork/TownInteriorsPartition62/Models/"+name+".fbx"),path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
   foreach(var src in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>()){
    string key=src.name;Material mat;
    if(!mats.TryGetValue(key,out mat)){
     if(!key.StartsWith("Interior62_")&&known.TryGetValue(key,out var reusable))mat=reusable;
     else{
      string mp=Root+"/Materials/"+key+".mat";mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
      if(mat==null){mat=new Material(src);mat.name=key;mat.shader=Shader.Find("BRB/GameLit");
       string family=(string)manifest["materials"]?[key]?["family"];
       if(family==null)family=key.Contains("Wood")?"Wood":key.Contains("Linen")||key.Contains("Canvas")||key.Contains("Cloth")?"Cloth":"Metal";
       family=family=="Steel"||family=="PaintedMetal"?"Metal":family=="Tile"||family=="Roof"?"Plaster":family;
       var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/WorldComic62/Textures/"+family+"_Comic.png");if(texture==null)throw new Exception("No comic family "+family);
       mat.SetTexture("_BaseMap",texture);mat.SetFloat("_ComicLighting",1);mat.SetFloat("_BumpScale",0);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",0);mat.DisableKeyword("_NORMALMAP");mat.DisableKeyword("_METALLICSPECGLOSSMAP");mat.DisableKeyword("_EMISSION");
       var authored=manifest["materials"]?[key]?["color"];
       if(authored!=null)mat.SetColor("_BaseColor",new Color((float)authored[0],(float)authored[1],(float)authored[2],1).gamma);
       if(key.Contains("Glass")){
        mat.shader=Shader.Find("Universal Render Pipeline/Lit");mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Smoothness",.05f);mat.SetColor("_BaseColor",new Color(.55f,.67f,.64f,.1f));mat.SetTexture("_BaseMap",null);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.SetShaderPassEnabled("ShadowCaster",false);mat.renderQueue=3000;
       }
       AssetDatabase.CreateAsset(mat,mp);
      }
     }
     mats[key]=mat;
    }
    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),src.name),mat);remapped.Add(new{model=name,slot=key,material=AssetDatabase.GetAssetPath(mat)});
   }
   importer.SaveAndReimport();
  }
  var report=JsonConvert.SerializeObject(new{models=names.Length,materials=mats.Count,remapped},Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/InteriorIntegration62/Imported.json"),report);return "Imported "+names.Length+" models; "+mats.Count+" mapped materials";
 }
}
