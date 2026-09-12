using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;
public static class PlayerIdentityReview {
 const string Root="Assets/ChibiSurvivor/Player/Painted62";
 static string Out=>Path.GetFullPath("../ArtWork/PlayerIdentity62/Unity");
 public static string Run(){
 var original=new Dictionary<Material,Texture>();
 try {
 foreach(var n in new[]{"Player","PlayerGear"}){
 var path=Root+"/Textures/"+n+"_Painted_V2.png";
 File.Copy(Path.GetFullPath("../ArtWork/PlayerIdentity62/Textures/"+n+"_Painted_V2.png"),path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
 var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+n+"_Painted.mat");original.Add(m,m.GetTexture("_BaseMap"));
 }
 Capture("A_Previous");
 foreach(var pair in original){string n=pair.Key.name=="Player_Painted"?"Player":"PlayerGear";pair.Key.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+n+"_Painted_V2.png"));}
 Capture("B_Details");
 }finally{foreach(var pair in original)pair.Key.SetTexture("_BaseMap",pair.Value);}
 return "Six-view A/B saved; live materials restored pending self-review";
 }
 public static string Apply(){
 foreach(var n in new[]{"Player","PlayerGear"}){var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+n+"_Painted.mat");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+n+"_Painted_V2.png");if(texture==null)throw new Exception("Missing V2 texture");m.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(m);}
 AssetDatabase.SaveAssets();
 var p=TopDownPlayer.Instance;
 var mats=p.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m.name=="Player_Painted"||m.name=="PlayerGear_Painted").ToArray();
 var valid=mats.Length==17&&mats.All(m=>AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")).EndsWith("_Painted_V2.png"));
 File.WriteAllText(Out+"/Applied.json",JsonConvert.SerializeObject(new{slots=mats.Length,valid,shaderErrors=mats.Any(m=>ShaderUtil.ShaderHasError(m.shader)),changed="Two BaseMap references",originalTexturesPreserved=true},Formatting.Indented));
 if(!valid)throw new Exception("Runtime material validation failed");
 return "V2 texture references saved and applied to 17 live player slots";
 }
 static string Capture(string prefix){
 var p=TopDownPlayer.Instance;var original=Camera.main;if(original==null)throw new Exception("Main camera missing");
 var go=new GameObject("PlayerPaintReviewCamera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(original);c.enabled=false;
 var extra=go.AddComponent<UniversalAdditionalCameraData>();var old=original.GetComponent<UniversalAdditionalCameraData>();if(old!=null)EditorUtility.CopySerialized(old,extra);
 var rt=new RenderTexture(900,1000,24,RenderTextureFormat.ARGBHalf);var previous=RenderTexture.active;
 try{foreach(var view in new[]{("Front",25f,0f),("Side",25f,90f),("OtherSide",25f,270f),("Back",25f,180f),("Top62",62f,0f),("TopBack62",62f,180f)}){
 var target=p.transform.position+Vector3.up*.92f;var q=Quaternion.Euler(view.Item2,view.Item3,0);c.transform.SetPositionAndRotation(target-q*Vector3.forward*15,q);c.nearClipPlane=13.5f;c.orthographicSize=1.23f;c.aspect=.9f;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
 var t=new Texture2D(900,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,900,1000),0,0);t.Apply();File.WriteAllBytes(Out+"/"+prefix+"_"+view.Item1+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
 }}finally{c.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 return "6 player views captured";
 }
}
