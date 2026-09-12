using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
public static class ComicGameCapture {
 public static string Run(){
 var main=Camera.main;if(main==null)throw new Exception("Main camera required");
 var go=new GameObject("ComicReviewGameCamera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(main);c.enabled=false;c.transform.SetPositionAndRotation(main.transform.position,main.transform.rotation);
 var extra=go.AddComponent<UniversalAdditionalCameraData>();var old=main.GetComponent<UniversalAdditionalCameraData>();if(old!=null)EditorUtility.CopySerialized(old,extra);
 int w=Mathf.Max(1,main.pixelWidth),h=Mathf.Max(1,main.pixelHeight);var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;Texture2D t=null;
 try{c.aspect=(float)w/h;c.targetTexture=rt;c.Render();RenderTexture.active=rt;t=new Texture2D(w,h,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,w,h),0,0);t.Apply();File.WriteAllBytes(Path.GetFullPath("../ArtWork/PlayerComic62/Unity/GameCamera.png"),t.EncodeToPNG());}
 finally{RenderTexture.active=active;c.targetTexture=null;if(t!=null)UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 return "Game camera scale and native resolution captured (overlay UI excluded)";
 }
}
