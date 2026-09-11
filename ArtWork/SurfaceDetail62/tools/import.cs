const string root="Assets/Art/SurfaceDetail62";
if(!UnityEditor.AssetDatabase.IsValidFolder(root))UnityEditor.AssetDatabase.CreateFolder("Assets/Art","SurfaceDetail62");
if(!UnityEditor.AssetDatabase.IsValidFolder(root+"/Textures"))UnityEditor.AssetDatabase.CreateFolder(root,"Textures");
foreach(var name in new[]{"Wood_Base_v2","Steel_Base_v2","DistrictWarden_BaseColor_v2"}){
 var path=root+"/Textures/"+name+".png";
 if(!System.IO.File.Exists(path))System.IO.File.Copy("D:/Demo/ArtWork/SurfaceDetail62/Generated/"+name+".png",path);
 UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceUpdate);
 var importer=UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
 importer.textureType=UnityEditor.TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.npotScale=UnityEditor.TextureImporterNPOTScale.None;
 importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.wrapMode=name.StartsWith("District")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.SaveAndReimport();
}
return "Three candidate textures imported. Existing materials and Play state untouched.";
