using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Newtonsoft.Json;
public static class WorldComicImport {
 public const string Root="Assets/Art/WorldComic62";
 static string Out=>Path.GetFullPath("../ArtWork/WorldComic62/Unity");
 public static string Run(){
 Directory.CreateDirectory(Root+"/Textures");Directory.CreateDirectory(Root+"/Materials");
 foreach(var source in Directory.GetFiles(Path.GetFullPath("../ArtWork/WorldComic62/Textures"),"*.png")){
 string path=Root+"/Textures/"+Path.GetFileName(source);File.Copy(source,path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.mipmapEnabled=true;imp.maxTextureSize=2048;imp.npotScale=TextureImporterNPOTScale.None;imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=4;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.wrapMode=IsAtlas(Path.GetFileNameWithoutExtension(path))?TextureWrapMode.Clamp:TextureWrapMode.Mirror;imp.SaveAndReimport();
 }
 var missingUv=AssetDatabase.FindAssets("t:Model",new[]{"Assets/Art","Assets/ChibiSurvivor"}).Select(AssetDatabase.GUIDToAssetPath).SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().Where(m=>!m.HasVertexAttribute(VertexAttribute.TexCoord0)).Select(m=>new{path=p,m.name,m.vertexCount})).ToArray();
 File.WriteAllText(Out+"/MissingUV.json",JsonConvert.SerializeObject(missingUv,Formatting.Indented));
 return $"Imported {Directory.GetFiles(Root+"/Textures","*.png").Length} comic textures; {missingUv.Length} meshes without UV0";
 }
 public static bool IsAtlas(string s)=>new[]{"Pawnshop","DistrictWarden","VeteranScavenger","WanderingMerchant","SimpleBandit","BanditClub","LegacyBandit"}.Any(s.Contains);
 public static string Family(Material m){
 string path=AssetDatabase.GetAssetPath(m),n=m.name.ToLowerInvariant();
 if(path.Contains("Resources/Materials/PlayerSprite")||n.Contains("decal")||n.Contains("occluder")||!m.HasProperty("_BaseMap"))return "intentional-flat";
 if(path.Contains("Painted62/Materials/"))return "approved-player";
 if(path.Contains("SimpleHeroStudy/Materials/SimpleHero_"))return "player-source";
 if(path.Contains("Bandit01/"))return "legacy-bandit";
 if(n=="districtwarden")return "DistrictWarden";if(n=="pawnshop")return "Pawnshop";if(n=="veteranscavenger")return "VeteranScavenger";if(n=="wanderingmerchant")return "WanderingMerchant";
 if(n=="simplebandit_surface")return "SimpleBandit";if(n=="banditclub_surface")return "BanditClub";
 if(n.Contains("glow")||n.Contains("mapline")||n.EndsWith("_ink")||n.EndsWith("_edge")||n.Contains("dirtseam")||n.Contains("crack")||n.Contains("seam")||n.Contains("grass"))return "intentional-flat";
 if(n.Contains("paper")||n.Contains("label")||n.Contains("cardboard"))return "Paper";
 if(n.Contains("wood")||n.Contains("counter"))return "Wood";
 if(n.Contains("cloth")||n.Contains("canvas")||n.Contains("linen")||n.Contains("blanket")||n.Contains("rug")||n.Contains("wrap")||n.Contains("holster")||n.Contains("bagplastic"))return "Cloth";
 if(n.Contains("brick")||n.Contains("graphite"))return "Brick";
 if(n.Contains("dirt")||n.Contains("soil")||n.Contains("dust")||n.Contains("ground"))return "Earth";
 if(n.Contains("road")||n.Contains("asphalt")||n=="floor")return "Asphalt";
 if(n.Contains("concrete")||n.Contains("plaster")||n.Contains("stone")||n.Contains("slab")||n.Contains("footing")||n.Contains("aggregate")||n.Contains("tile")||n.Contains("roof")||n.Contains("floor")||n=="wall"||n=="ceiling"||n.Contains("cream")||n.Contains("ochre")||n.Contains("sage"))return "Plaster";
 return "Metal";
 }
 public static void Style(Material target,string family){
 var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+family+"_Comic.png");if(t==null)throw new Exception("Texture missing "+family);
 target.SetTexture("_BaseMap",t);
 if(family=="Asphalt"&&target.name.StartsWith("Road_"))target.SetTextureScale("_BaseMap",new Vector2(.25f,.25f));
 if(!IsAtlas(family)&&family!="Brick"&&target.HasProperty("_BumpScale"))target.SetFloat("_BumpScale",0.12f);
 // Keep atlas UV transforms and original material color. Tiling materials retain their established scale.
 }
 public static bool Eligible(string family)=>!new[]{"approved-player","player-source","legacy-bandit","intentional-flat"}.Contains(family);
}
public static class WorldComicApply {
 static string Out=>System.IO.Path.GetFullPath("../ArtWork/WorldComic62/Unity");
 static string MatRoot=>WorldComicImport.Root+"/Materials";
 static readonly System.Collections.Generic.List<object> Report=new();
 public static string Run(){
 if(UnityEngine.Application.isPlaying)throw new System.Exception("Stop play before saving");
 var paths=UnityEditor.AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art","Assets/ChibiSurvivor","Assets/Resources"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
 int changed=0,remaps=0;
 foreach(var path in paths){
 if(path.StartsWith(WorldComicImport.Root+"/"))continue;
 if(!path.EndsWith(".mat"))continue;
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);if(m==null)continue;
 var family=WorldComicImport.Family(m);
 if(!WorldComicImport.Eligible(family)){Report.Add(new{path,name=m.name,status=family});continue;}
 var previous=UnityEditor.AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap"));WorldComicImport.Style(m,family);
 UnityEditor.EditorUtility.SetDirty(m);UnityEditor.AssetDatabase.SaveAssetIfDirty(m);changed++;
 Report.Add(new{path,name=m.name,status="textured",family,previous});
 }
 foreach(var path in paths.Where(p=>p.EndsWith(".fbx",System.StringComparison.OrdinalIgnoreCase))){
 var importer=UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;if(importer==null)continue;
 bool dirty=false;var existing=importer.GetExternalObjectMap();
 foreach(var m in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.Material>()){
 var family=WorldComicImport.Family(m);if(!WorldComicImport.Eligible(family))continue;
 var id=new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),m.name);
 if(existing.TryGetValue(id,out var mapped)&&mapped is UnityEngine.Material mm&&UnityEditor.AssetDatabase.GetAssetPath(mm).EndsWith(".mat"))continue;
 string target=MatRoot+"/"+UnityEditor.AssetDatabase.AssetPathToGUID(path).Substring(0,8)+"_"+m.name+".mat";
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(target);
 if(mat==null){mat=new UnityEngine.Material(m);mat.name=m.name;WorldComicImport.Style(mat,family);UnityEditor.AssetDatabase.CreateAsset(mat,target);}
 importer.AddRemap(id,mat);dirty=true;remaps++;Report.Add(new{path,name=m.name,status="external-remap",family,target});
 }
 if(dirty)importer.SaveAndReimport();
 }
 System.IO.File.WriteAllText(Out+"/MaterialApply.json",Newtonsoft.Json.JsonConvert.SerializeObject(Report,Newtonsoft.Json.Formatting.Indented));
 return $"Saved {changed} materials, remapped {remaps} FBX material slots";
 }
 public static string Scenes(){
 if(UnityEngine.Application.isPlaying)throw new System.Exception("Stop play before saving");
 int changed=0;
 foreach(var path in new[]{"Assets/Scenes/Safehouse.unity","Assets/Scenes/Hideout.unity","Assets/Scenes/Pawnshop.unity"}){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
 if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 if(scene.isDirty)throw new System.Exception("Scene has existing unsaved changes: "+path);
 int edits=0;
 foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>(true))){
 if(r is UnityEngine.SpriteRenderer||r is UnityEngine.ParticleSystemRenderer)continue;
 var mats=r.sharedMaterials;bool dirty=false;
 for(int i=0;i<mats.Length;i++){
 var m=mats[i];if(m==null||!m.HasProperty("_BaseMap"))continue;string mp=UnityEditor.AssetDatabase.GetAssetPath(m);
 if(mp.EndsWith(".mat"))continue;
 if(mp.EndsWith(".fbx",System.StringComparison.OrdinalIgnoreCase)){
 var imp=UnityEditor.AssetImporter.GetAtPath(mp) as UnityEditor.ModelImporter;
 if(imp!=null&&imp.GetExternalObjectMap().TryGetValue(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),m.name),out var replacement)&&replacement is UnityEngine.Material rm){mats[i]=rm;dirty=true;}
 continue;
 }
 if(!string.IsNullOrEmpty(mp))continue;
 var family=WorldComicImport.Family(m);if(!WorldComicImport.Eligible(family))continue;
 // Inline greybox surfaces have no useful material names; use the actual mesh role.
 string rn=r.name.ToLowerInvariant();if(rn.Contains("floor")||rn.Contains("wall"))family="Plaster";else if(rn.Contains("crate")||rn.Contains("shelf"))family="Wood";
 var color=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):UnityEngine.Color.white;
 string signature=UnityEditor.EditorJsonUtility.ToJson(m);string hash=UnityEngine.Hash128.Compute(signature).ToString().Substring(0,12);
 string target=MatRoot+"/Scene_"+family+"_"+hash+".mat";
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(target);if(mat==null){mat=new UnityEngine.Material(m){name="Scene_"+family};WorldComicImport.Style(mat,family);UnityEditor.AssetDatabase.CreateAsset(mat,target);}
 mats[i]=mat;dirty=true;
 }
 if(dirty){r.sharedMaterials=mats;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);UnityEditor.EditorUtility.SetDirty(r);edits++;}
 }
 if(edits>0){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);changed+=edits;}
 if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
 }
 return $"Saved scene material bindings on {changed} renderers";
 }
}
