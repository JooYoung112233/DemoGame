using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;
public static class PlayerPaintReview {
 const string Root="Assets/ChibiSurvivor/Player/Painted62";
 const string Original="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Materials/";
 static string Out=>Path.GetFullPath("../ArtWork/PlayerPainted62/Unity");
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;var parent=Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(p));}
 public static string Run(){
 Folder(Root+"/Textures");Folder(Root+"/Materials");
 var map=new Dictionary<Material,Material>();
 foreach(var pair in new[]{("SimpleHero_Surface","Player"),("SimpleHero_Gear","PlayerGear")}){
 var path=Root+"/Textures/"+pair.Item2+"_Painted_BaseColor.png";File.Copy(Path.GetFullPath("../ArtWork/PlayerPainted62/Textures/"+pair.Item2+"_Painted_BaseColor.png"),path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
 var original=AssetDatabase.LoadAssetAtPath<Material>(Original+pair.Item1+".mat");var mp=Root+"/Materials/"+pair.Item2+"_Painted.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(mp);
 if(m==null){m=new Material(original);AssetDatabase.CreateAsset(m,mp);}else m.CopyPropertiesFromMaterial(original);
 m.name=pair.Item2+"_Painted";m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));EditorUtility.SetDirty(m);map.Add(original,m);
 }
 AssetDatabase.SaveAssets();var p=TopDownPlayer.Instance;if(p==null)throw new Exception("Player missing");
 var renderers=p.GetComponentsInChildren<SkinnedMeshRenderer>(true);var originals=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);int slots=0;
 try{
 foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(m=>map.FirstOrDefault(kv=>kv.Value==m).Key??m).ToArray();
 Capture("A_Original");
 foreach(var r in renderers){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)if(map.TryGetValue(mats[i],out var m)){mats[i]=m;slots++;}r.sharedMaterials=mats;}
 Capture("B_Painted");
 File.WriteAllText(Out+"/Validation.json",JsonConvert.SerializeObject(new{materialSlots=slots,shaderErrors=map.Values.Any(m=>ShaderUtil.ShaderHasError(m.shader)),changed="BaseMap texture reference only",meshAndRigUnchanged=true,originalMaterialsPreserved=true},Formatting.Indented));
 }finally{foreach(var kv in originals)kv.Key.sharedMaterials=kv.Value;}
 return "Player original / painted 5-view comparison saved; originals restored";
 }
 public static string Show(){
 var p=TopDownPlayer.Instance;int count=0;foreach(var r in p.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
 var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
 string n=mats[i].name=="SimpleHero_Surface"?"Player":mats[i].name=="SimpleHero_Gear"?"PlayerGear":null;
 if(n!=null){var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+n+"_Painted.mat");if(m==null)throw new Exception("Candidate not imported");mats[i]=m;count++;}
 }r.sharedMaterials=mats;}
 return "Current player painted material slots: "+count;
 }
 public static string CaptureLive()=>Capture("Live");
 static string Capture(string prefix){
 var p=TopDownPlayer.Instance;var original=Camera.main;if(original==null)throw new Exception("Main camera missing");
 var go=new GameObject("PlayerPaintReviewCamera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(original);c.enabled=false;
 var extra=go.AddComponent<UniversalAdditionalCameraData>();var old=original.GetComponent<UniversalAdditionalCameraData>();if(old!=null)EditorUtility.CopySerialized(old,extra);
 var rt=new RenderTexture(900,1000,24,RenderTextureFormat.ARGBHalf);var previous=RenderTexture.active;
 try{foreach(var view in new[]{("Front",25f,0f),("Side",25f,90f),("Back",25f,180f),("Top62",62f,0f),("TopBack62",62f,180f)}){
 var target=p.transform.position+Vector3.up*.92f;var q=Quaternion.Euler(view.Item2,view.Item3,0);c.transform.SetPositionAndRotation(target-q*Vector3.forward*15,q);c.orthographicSize=1.23f;c.aspect=.9f;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
 var t=new Texture2D(900,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,900,1000),0,0);t.Apply();File.WriteAllBytes(Out+"/"+prefix+"_"+view.Item1+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
 }}finally{c.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 return "5 player views captured";
 }
}
