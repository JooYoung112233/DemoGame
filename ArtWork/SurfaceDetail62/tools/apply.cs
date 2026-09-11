const string stage="D:/Demo/ArtWork/SurfaceDetail62";
var results=new System.Collections.Generic.List<object>();
foreach(var name in new[]{"Town_Wood","Town_WoodDark","Town_Steel","Pawnshop_Counter","DistrictWarden"}){
 var path=name=="DistrictWarden"?"Assets/ChibiSurvivor/NPC/StoryNPC62/Materials/DistrictWarden.mat":name=="Pawnshop_Counter"?"Assets/Art/Environments/Town02/Rendering62/Pawnshop_Counter.mat":"Assets/Art/Environments/Town02/Materials/"+name+".mat";
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
 bool wood=name.Contains("Wood")||name.Contains("Counter"),character=name=="DistrictWarden";
 var texturePath="Assets/Art/SurfaceDetail62/Textures/"+(character?"DistrictWarden_BaseColor_v2":wood?"Wood_Base_v2":"Steel_Base_v2")+".png";
 var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
 if(mat==null||texture==null)throw new System.Exception("Missing asset: "+path);
 if(mat.GetTexture("_BaseMap")==texture)throw new System.Exception("Already applied; do not multiply tint twice: "+path);
}
System.IO.Directory.CreateDirectory(stage+"/BeforeFiles");
foreach(var name in new[]{"Town_Wood","Town_WoodDark","Town_Steel","Pawnshop_Counter","DistrictWarden"}){
 var path=name=="DistrictWarden"?"Assets/ChibiSurvivor/NPC/StoryNPC62/Materials/DistrictWarden.mat":name=="Pawnshop_Counter"?"Assets/Art/Environments/Town02/Rendering62/Pawnshop_Counter.mat":"Assets/Art/Environments/Town02/Materials/"+name+".mat";
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
 System.IO.File.Copy(path,stage+"/BeforeFiles/"+name+".mat",false);
 bool wood=name.Contains("Wood")||name.Contains("Counter"),character=name=="DistrictWarden";
 var texturePath="Assets/Art/SurfaceDetail62/Textures/"+(character?"DistrictWarden_BaseColor_v2":wood?"Wood_Base_v2":"Steel_Base_v2")+".png";
 var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
 var oldTexture=UnityEditor.AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap"));var oldColor=mat.GetColor("_BaseColor");
 mat.SetTexture("_BaseMap",texture);
 if(!character){var ratio=wood?new Vector3(1.35569545f,1.39424937f,1.45775068f):new Vector3(1.67287622f,1.69343595f,1.71713072f);mat.SetColor("_BaseColor",new Color(oldColor.r*ratio.x,oldColor.g*ratio.y,oldColor.b*ratio.z,oldColor.a));}
 UnityEditor.EditorUtility.SetDirty(mat);UnityEditor.AssetDatabase.SaveAssetIfDirty(mat);
 results.Add(new{path,oldTexture,texturePath,oldColor=oldColor.ToString(),newColor=mat.GetColor("_BaseColor").ToString(),normal=UnityEditor.AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")),mask=UnityEditor.AssetDatabase.GetAssetPath(mat.GetTexture("_MetallicGlossMap"))});
}
var json=Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(stage+"/AppliedMaterials.json",json);return json;
