using System;using System.IO;using System.Linq;using System.Collections;using UnityEngine;using UnityEditor;using Newtonsoft.Json;
public static class WorldComicFinalize {
 public static string Run(){
 var forecourt=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/TownFinish62/Materials/Forecourt_Asphalt.mat");forecourt.SetColor("_BaseColor",new Color(.43f,.44f,.40f,1));EditorUtility.SetDirty(forecourt);AssetDatabase.SaveAssetIfDirty(forecourt);
 var paths=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art","Assets/ChibiSurvivor","Assets/Resources"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().Where(p=>p.EndsWith(".mat")).ToArray();
 var rows=paths.Select(p=>{var m=AssetDatabase.LoadAssetAtPath<Material>(p);var t=m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):null;return new{path=p,m.name,shader=m.shader.name,baseMap=t==null?null:AssetDatabase.GetAssetPath(t)};}).ToArray();
 File.WriteAllText(Path.GetFullPath("../ArtWork/WorldComic62/Unity/Coverage.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
 return $"Material coverage recorded: {rows.Count(r=>r.baseMap!=null&&r.baseMap.Contains("WorldComic62"))} comic material assets; forecourt tone harmonized";
 }
 public static string GamePreview(){
 if(!Application.isPlaying)throw new Exception("Play required");TopDownPlayer.Instance.StartCoroutine(Capture());return "Preparing actual gameplay camera near community props";
 }
 static IEnumerator Capture(){
 var p=TopDownPlayer.Instance;var rb=p.GetComponent<Rigidbody>();rb.position=new Vector3(43,.05f,20.5f);p.transform.position=rb.position;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForSecondsRealtime(2);
 var c=Camera.main;var old=c.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf);
 try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1920,1080,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1920,1080),0,0);t.Apply();File.WriteAllBytes(Path.GetFullPath("../ArtWork/WorldComic62/Unity/GameCamera.png"),t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
 finally{c.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);}
 }
}
