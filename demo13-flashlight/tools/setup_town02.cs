// Import only the Town02 namespace through the connected Unity MCP editor.
const string folder="Assets/Art/Environments/Town02";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
foreach(var sub in new[]{"Materials","Meshes"})if(!UnityEditor.AssetDatabase.IsValidFolder(folder+"/"+sub))UnityEditor.AssetDatabase.CreateFolder(folder,sub);
foreach(var path in System.IO.Directory.GetFiles(folder+"/Textures","*.png")){
 var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path.Replace('\\','/'));
 ti.textureType=path.Contains("_Normal")?UnityEditor.TextureImporterType.NormalMap:UnityEditor.TextureImporterType.Default;
 ti.sRGBTexture=path.Contains("_Base");ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;
 ti.alphaSource=UnityEditor.TextureImporterAlphaSource.FromInput;ti.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
}
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/KitManifest.json"));
var mats=new System.Collections.Generic.Dictionary<string,Material>();
foreach(var e in ((Newtonsoft.Json.Linq.JObject)manifest["palette"]).Properties()){
 string path=folder+"/Materials/"+e.Name+".mat",family=(string)e.Value["family"];
 string textures=(family=="Brick"||family=="Plaster"||family=="Asphalt"||family=="Tile"||family=="Roof"?folder:"Assets/Art/Environments/Hideout02")+"/Textures/";
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
 if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));UnityEditor.AssetDatabase.CreateAsset(m,path);}
 var c=e.Value["color"];m.SetColor("_BaseColor",new Color((float)c[0],(float)c[1],(float)c[2]).gamma);
 m.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(textures+family+"_Base.png"));
 m.SetTexture("_BumpMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(textures+family+"_Normal.png"));m.SetFloat("_BumpScale",family=="Brick"?.65f:.28f);m.EnableKeyword("_NORMALMAP");
 m.SetTexture("_MetallicGlossMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(textures+family+"_Mask.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);
 UnityEditor.EditorUtility.SetDirty(m);mats.Add(e.Name,m);
}
int vertices=0,triangles=0;
foreach(var e in ((Newtonsoft.Json.Linq.JObject)manifest["assets"]).Properties()){
 var mi=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(folder+"/Models/"+e.Name+".fbx");
 mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;mi.importNormals=UnityEditor.ModelImporterNormals.Import;mi.importTangents=UnityEditor.ModelImporterTangents.CalculateMikk;mi.generateSecondaryUV=true;
 foreach(var mat in mats)mi.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(Material),mat.Key),mat.Value);
 mi.SaveAndReimport();
 var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Models/"+e.Name+".fbx");
 foreach(var mf in model.GetComponentsInChildren<MeshFilter>()){
  var mesh=mf.sharedMesh;if(mesh.uv.Length!=mesh.vertexCount||mesh.normals.Length!=mesh.vertexCount||mesh.tangents.Length!=mesh.vertexCount)throw new System.Exception("Missing mesh surface channels: "+e.Name);
  vertices+=mesh.vertexCount;triangles+=mesh.triangles.Length/3;
 }
 foreach(var r in model.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials)if(m==null||!mats.ContainsKey(m.name))throw new System.Exception("Unmapped material "+e.Name);
}
UnityEditor.AssetDatabase.SaveAssets();
return new{models=((Newtonsoft.Json.Linq.JObject)manifest["assets"]).Count,materials=mats.Count,vertices,triangles,uvNormalsTangents=true};
