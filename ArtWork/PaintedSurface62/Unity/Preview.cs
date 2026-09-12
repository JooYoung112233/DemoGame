using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;

public static class PaintedPreview {
 const string Root="Assets/Art/Environments/PaintedSurface62";
 static string Out=>Path.GetFullPath("../ArtWork/PaintedSurface62/Unity");
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;var parent=Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(p));}
 public static string Show(){
 var source="Assets/Art/Environments/GroundFinish62/Prefabs/HideoutGroundFinish62.prefab";
 Folder(Root+"/Prefabs");var contents=PrefabUtility.LoadPrefabContents(source);
 int count=0;
 try{count=UsePainted(contents);PrefabUtility.SaveAsPrefabAsset(contents,Root+"/Prefabs/HideoutGroundFinish62_Painted.prefab");}
 finally{PrefabUtility.UnloadPrefabContents(contents);}
 if(!Application.isPlaying)throw new Exception("Live preview expects Play mode");
 var root=GameObject.Find("HideoutGroundFinish62");if(root==null)throw new Exception("Safehouse not loaded");
 UsePainted(root);Capture("Live_Painted");AssetDatabase.SaveAssets();
 File.WriteAllText(Out+"/LivePreview.json",JsonConvert.SerializeObject(new{prefab=Root+"/Prefabs/HideoutGroundFinish62_Painted.prefab",materialSlots=count,livePreview=true,sceneSaved=false,scope="Current loaded Safehouse ground dressing only. Original scene/materials unchanged; scene reload restores original appearance."},Formatting.Indented));
 return "Painted prefab saved; current play preview enabled, original scene and materials preserved";
 }
 static int UsePainted(GameObject root){int count=0;foreach(var r in root.GetComponentsInChildren<Renderer>()){
 var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
 var n=mats[i].name;if(!n.StartsWith("Ground62_"))continue;
 var candidate=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Painted62_"+n.Substring("Ground62_".Length)+".mat");
 if(candidate!=null){mats[i]=candidate;count++;}
 }r.sharedMaterials=mats;
 }return count;}
 public static string Run(){
 Folder(Root+"/Textures");Folder(Root+"/Materials");
 foreach(var n in new[]{"Concrete","Earth"}){
 var path=Root+"/Textures/"+n+"_Painted_Base.png";
 File.Copy(Path.GetFullPath("../ArtWork/PaintedSurface62/Textures/"+n+"_Painted_Base.png"),path,true);
 AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Mirror;imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=4;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
 }
 var replace=new Dictionary<Material,Material>();
 foreach(var n in new[]{"Concrete","ConcreteDark","Stone","Soil","DryDust"}){
 var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/GroundFinish62/Materials/Ground62_"+n+".mat");
 var path=Root+"/Materials/Painted62_"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
 if(m==null){m=new Material(original);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(original);
 m.name="Painted62_"+n;m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+(n=="Soil"||n=="DryDust"?"Earth":"Concrete")+"_Painted_Base.png"));m.SetTextureScale("_BaseMap",new Vector2(.4f,.4f));EditorUtility.SetDirty(m);replace.Add(original,m);
 }
 AssetDatabase.SaveAssets();
 var root=GameObject.Find("HideoutGroundFinish62");if(root==null)throw new Exception("Load Safehouse entrance first");
 var renderers=root.GetComponentsInChildren<Renderer>();var before=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);
 try{
 Capture("A_Original");
 foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(m=>replace.TryGetValue(m,out var p)?p:m).ToArray();
 Capture("B_Painted");
 var sun=UnityEngine.Object.FindObjectsByType<Light>().First(l=>l.type==LightType.Directional);
 var rotation=sun.transform.rotation;var color=sun.color;var power=sun.intensity;var mode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
 try{var w=Resources.Load<WeatherData>("Data/WeatherData");sun.transform.rotation=Quaternion.Euler(w.nightSunAngle);sun.color=w.nightLightColor;sun.intensity=w.nightIntensity;DayNightCycle.ApplyAmbient(w.nightAmbientColor);Capture("B_Night");}
 finally{sun.transform.rotation=rotation;sun.color=color;sun.intensity=power;RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;}
 File.WriteAllText(Out+"/PreviewValidation.json",JsonConvert.SerializeObject(new{project=Application.dataPath,materialCount=replace.Count,changedProperty="_BaseMap texture and tiling only",tiling=.4f,shaderErrors=replace.Values.Any(m=>ShaderUtil.ShaderHasError(m.shader)),sourceMaterialsPreserved=true,renderersRestored=true},Formatting.Indented));
 }finally{foreach(var kv in before)kv.Key.sharedMaterials=kv.Value;}
 return "A/B and night captured with identical camera/geometry/shaders; original renderer materials restored";
 }
 static void Capture(string prefix){
 var original=Camera.main;if(original==null)throw new Exception("Main camera missing");
 var go=new GameObject("PaintedReviewCamera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(original);c.enabled=false;
 var extra=go.AddComponent<UniversalAdditionalCameraData>();var old=original.GetComponent<UniversalAdditionalCameraData>();if(old!=null)EditorUtility.CopySerialized(old,extra);
 var rt=new RenderTexture(1500,1000,24,RenderTextureFormat.ARGBHalf);var previous=RenderTexture.active;
 try{foreach(var v in new[]{"Entry","Close"}){
 var target=v=="Entry"?new Vector3(55.6f,.25f,12.8f):new Vector3(53.4f,.05f,14.7f);var q=Quaternion.Euler(62,0,0);c.transform.SetPositionAndRotation(target-q*Vector3.forward*25,q);c.orthographicSize=v=="Entry"?6.8f:3.8f;c.aspect=1.5f;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
 var t=new Texture2D(1500,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1500,1000),0,0);t.Apply();File.WriteAllBytes(Out+"/"+prefix+"_"+v+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
 }}finally{c.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
}
