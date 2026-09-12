using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;
public static class GroundReview {
 static string Dir=>Path.GetFullPath("../ArtWork/GroundFinish62/Unity");
 public static string Before()=>Capture("Before");
 public static string After()=>Capture("After");
 public static string Runtime()=>Capture("Runtime");
 public static string Night(){
 var sun=UnityEngine.Object.FindObjectsByType<Light>().First(l=>l.type==LightType.Directional);
 var rot=sun.transform.rotation;var color=sun.color;var power=sun.intensity;
 var mode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
 try{var w=Resources.Load<WeatherData>("Data/WeatherData");sun.transform.rotation=Quaternion.Euler(w.nightSunAngle);sun.color=w.nightLightColor;sun.intensity=w.nightIntensity;DayNightCycle.ApplyAmbient(w.nightAmbientColor);return Capture("Night");}
 finally{sun.transform.rotation=rot;sun.color=color;sun.intensity=power;RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;}
 }
 static string Capture(string prefix){
 var original=Camera.main;if(original==null)throw new Exception("Main camera missing");
 var go=new GameObject("GroundReviewCamera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(original);c.enabled=false;
 var extra=go.AddComponent<UniversalAdditionalCameraData>();var oldExtra=original.GetComponent<UniversalAdditionalCameraData>();if(oldExtra!=null)EditorUtility.CopySerialized(oldExtra,extra);
 var rt=new RenderTexture(1500,1000,24,RenderTextureFormat.ARGBHalf);var prev=RenderTexture.active;
 try{
 foreach(var view in new[]{"Entry","Close"}){
 var target=view=="Entry"?new Vector3(55.6f,.25f,12.8f):new Vector3(53.4f,.05f,14.7f);float size=view=="Entry"?6.8f:3.8f;
 var q=Quaternion.Euler(62,0,0);c.transform.SetPositionAndRotation(target-q*Vector3.forward*25,q);c.orthographicSize=size;c.aspect=1.5f;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
 var t=new Texture2D(1500,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1500,1000),0,0);t.Apply();File.WriteAllBytes(Dir+"/"+prefix+"_"+view+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
 }
 }finally{c.targetTexture=null;RenderTexture.active=prev;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 return prefix+" 62-degree renders saved; main camera untouched";
 }
}
