if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Preserve Play; defer saved material changes.");
const string stage="D:/Demo/ArtWork/MaterialReview62";
void Backup(string path){var dest=stage+"/BeforeFiles/"+path;System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));if(!System.IO.File.Exists(dest))System.IO.File.Copy(path,dest);}
var rows=new System.Collections.Generic.List<object>();
var paths=UnityEditor.AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Environments/Town02"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".mat")).ToArray();
foreach(var path in paths){
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);if(m.shader.name!="BRB/GameLit")continue;
 var mask=m.GetTexture("_MetallicGlossMap") as Texture2D;if(mask==null)continue;
 var maskPath=UnityEditor.AssetDatabase.GetAssetPath(mask);var importer=UnityEditor.AssetImporter.GetAtPath(maskPath) as UnityEditor.TextureImporter;
 if(importer==null||importer.sRGBTexture)throw new System.Exception("Mask must be linear: "+path);
 Backup(path);UnityEditor.Undo.RecordObject(m,"Differentiate town surface materials");
 m.SetFloat("_UsePackedMask",1);m.EnableKeyword("_GAMELIT_PACKED_MASK");m.DisableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);
 // Source masks carry material identity. Keep dry ground free of specular sheen.
 bool dry=m.name.Contains("Dirt")||m.name.Contains("Ground")||m.name.Contains("Asphalt")||m.name.StartsWith("Road_")||m.name.Contains("Concrete")||m.name.Contains("Floor")||m.name.Contains("Plaster")||m.name.Contains("Brick")||m.name.Contains("Roof")||m.name.Contains("Canvas")||m.name.Contains("Paper")||m.name.Contains("Cream")||m.name.Contains("Ochre");
 if(dry)m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");else m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
 m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
 m.SetFloat("_GrimeStrength",dry?.12f:.09f);m.SetFloat("_ShadowDesaturation",.18f);
 m.SetFloat("_BumpScale",m.name.Contains("Brick")?.65f:m.name.Contains("Wood")||m.name.Contains("Counter")?.5f:m.name.Contains("Canvas")?.5f:.35f);
 UnityEditor.EditorUtility.SetDirty(m);UnityEditor.AssetDatabase.SaveAssetIfDirty(m);
 rows.Add(new{path,specular=!dry,mask=maskPath});
}
foreach(var name in new[]{"Pawnshop","DistrictWarden","VeteranScavenger","WanderingMerchant"}){
 var path="Assets/ChibiSurvivor/NPC/StoryNPC62/Materials/"+name+".mat";Backup(path);var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
 UnityEditor.Undo.RecordObject(m,"Reduce waxy rim on story NPCs");m.SetFloat("_RimStrength",.012f);m.SetFloat("_BumpScale",.6f);m.SetFloat("_GrimeStrength",.07f);m.SetFloat("_ShadowDesaturation",.14f);
 UnityEditor.EditorUtility.SetDirty(m);UnityEditor.AssetDatabase.SaveAssetIfDirty(m);rows.Add(new{path});
}
var report=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(stage+"/AppliedMaterials.json",report);return rows.Count;
