const string folder="Assets/ChibiSurvivor/Player/DarkSurvivor";
const string surface=folder+"/SurfaceReview";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
string texturePath=surface+"/Textures/Hero_Face_Neutral.png";
var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
ti.textureType=UnityEditor.TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaSource=UnityEditor.TextureImporterAlphaSource.None;ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.filterMode=UnityEngine.FilterMode.Trilinear;ti.wrapMode=UnityEngine.TextureWrapMode.Clamp;ti.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
var tex=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(texturePath);
string matPath=surface+"/Materials/Hero_Face.mat";
var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(matPath);
if(mat==null){mat=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));UnityEditor.AssetDatabase.CreateAsset(mat,matPath);}
mat.SetTexture("_BaseMap",tex);mat.SetColor("_BaseColor",UnityEngine.Color.white);mat.SetFloat("_Smoothness",.1f);mat.SetFloat("_Metallic",0);mat.SetTexture("_BumpMap",null);mat.DisableKeyword("_NORMALMAP");UnityEditor.EditorUtility.SetDirty(mat);
foreach(var path in new[]{folder+"/DarkSurvivor.fbx",surface+"/DarkSurvivor_Textured.fbx"}){
 var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(path);
 importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),"Hero_Face"),mat);importer.SaveAndReimport();
}
UnityEditor.AssetDatabase.SaveAssets();return new{texture=texturePath,material=matPath};
