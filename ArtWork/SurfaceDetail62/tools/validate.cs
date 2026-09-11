var entries=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("D:/Demo/ArtWork/SurfaceDetail62/AppliedMaterials.json"));
var reports=new System.Collections.Generic.List<object>();
foreach(var e in entries){
 var path=(string)e["path"];var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);var tex=mat.GetTexture("_BaseMap") as Texture2D;
 if(UnityEditor.AssetDatabase.GetAssetPath(tex)!=(string)e["texturePath"])throw new System.Exception("Base map mismatch: "+path);
 if(UnityEditor.AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap"))!=(string)e["normal"]||UnityEditor.AssetDatabase.GetAssetPath(mat.GetTexture("_MetallicGlossMap"))!=(string)e["mask"])throw new System.Exception("Surface map changed: "+path);
 var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath((string)e["texturePath"]);
 var errs=UnityEditor.ShaderUtil.GetShaderMessages(mat.shader).Where(x=>x.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(x=>x.message).ToArray();
 if(errs.Length>0||!importer.sRGBTexture||!importer.mipmapEnabled)throw new System.Exception("Shader/import validation failed: "+path);
 reports.Add(new{path,shader=mat.shader.name,texture=tex.name,width=tex.width,height=tex.height,importer.sRGBTexture,importer.mipmapEnabled,normalMaskPreserved=true,shaderErrors=errs.Length});
}
var camera=Camera.main;var leftovers=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.name=="SurfaceReviewCamera").Count();
if(leftovers!=0)throw new System.Exception("Review camera leaked");
var result=new{materials=reports,playing=UnityEditor.EditorApplication.isPlaying,scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name).ToArray(),cameraRotation=camera.transform.eulerAngles.ToString(),cameraPosition=camera.transform.position.ToString(),temporaryCameras=leftovers};
var json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("D:/Demo/ArtWork/SurfaceDetail62/Validation.json",json);return json;
