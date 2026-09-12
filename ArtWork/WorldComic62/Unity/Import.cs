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
