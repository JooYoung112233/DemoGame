using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class InteriorProbe
{
 static string Root=>Path.GetFullPath("../ArtWork/InteriorIntegration62");
 public static string Run(){TopDownPlayer.Instance.StartCoroutine(Test());return "Walking all five interiors";}
 static void Place(Vector3 pos){var p=TopDownPlayer.Instance;var rb=p.GetComponent<Rigidbody>();p.transform.position=pos;rb.position=pos;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();}
 public static void Capture(string label,Vector3 target,float size){
  var main=Camera.main;var go=new GameObject("InteriorReviewCamera"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.orthographicSize=size;cam.aspect=4f/3;cam.transform.rotation=Quaternion.Euler(62,0,0);cam.transform.position=target-cam.transform.forward*25;
  var cd=go.AddComponent<UniversalAdditionalCameraData>();EditorUtility.CopySerialized(main.GetUniversalAdditionalCameraData(),cd);cd.renderType=CameraRenderType.Base;
  var rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;var tex=new Texture2D(1280,960,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,960),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Root,label+".png"),tex.EncodeToPNG());}
  finally{cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
 static IEnumerator Test(){
  var p=TopDownPlayer.Instance;var origin=p.transform.position;bool virtualBefore=GameInput.Virtual;
  var m=JObject.Parse(File.ReadAllText(Path.GetFullPath("../ArtWork/TownInteriorsPartition62/KitManifest.json")));
  var buildings=UnityEngine.Object.FindObjectsByType<BuildingInterior>();var rows=new List<object>();
  var locks=buildings.Select(b=>b.GetComponent<BuildingUnlock>()).Where(b=>b!=null).ToArray();var enabled=locks.Select(l=>l.enabled).ToArray();var shutters=locks.Select(l=>l.transform.Find("Shutter").gameObject).ToArray();var shutterStates=shutters.Select(g=>g.activeSelf).ToArray();
  UIManager.Instance.CloseAll();GameInput.Virtual=true;
  try{
   for(int i=0;i<locks.Length;i++){locks[i].enabled=false;shutters[i].SetActive(false);}
   foreach(var b in buildings.OrderBy(b=>b.name)){
    var art=b.transform.Find("Interior_Art62");var spec=m["buildings"][b.name];float depth=(float)spec["depth"];
    var start=art.TransformPoint(new Vector3(0,0,-depth*.5f-.8f));var forward=art.TransformDirection(Vector3.forward);
    GameInput.VSetMove(Vector2.zero);Place(start);yield return new WaitForSeconds(.2f);
    GameInput.VSetMove(new Vector2(forward.x,forward.z));yield return new WaitForSeconds(2f);GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.3f);
    float distance=Vector3.Dot(p.transform.position-start,forward);bool entered=b.PlayerInside;float y=p.transform.position.y;
    var roof=b.transform.Find("Roof_Art02").GetComponentInChildren<Renderer>();bool hidden=!roof.enabled;
    var service=art.TransformPoint(new Vector3(0,.4f,(float)spec["serviceCenterZ"]));Capture(b.name+"_Inside",service,4.8f);
    string target=null;bool dialogue=false;
    if(b.name=="Pawnshop"){
     Place(art.TransformPoint(new Vector3(-1.2f,0,-4.0f)));yield return new WaitForSeconds(.3f);
     var obj=(InteractableObject)typeof(InteractionSystem).GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p.GetComponent<InteractionSystem>());target=obj==null?null:obj.name;
     GameInput.VPressKey(KeyCode.E);yield return null;GameInput.VEndFrame();GameInput.VReleaseKey(KeyCode.E);yield return new WaitForSeconds(.3f);dialogue=UIManager.Instance.IsAnyUIOpen();UIManager.Instance.CloseAll();var right=art.TransformDirection(Vector3.right);GameInput.VSetMove(new Vector2(right.x,right.z));yield return new WaitForSeconds(.95f);GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.2f);
    }
    GameInput.VSetMove(new Vector2(-forward.x,-forward.z));yield return new WaitForSeconds(2.5f);GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.3f);
    bool exited=!b.PlayerInside;bool restored=roof.enabled;
    rows.Add(new{name=b.name,distance,y,entered,roofHidden=hidden,exited,roofRestored=restored,npcTarget=target,dialogueOpened=dialogue,pass=distance>1.7f&&Mathf.Abs(y)<.02f&&entered&&hidden&&exited&&restored&&(b.name!="Pawnshop"||dialogue)});
   }
  }finally{
   GameInput.VSetMove(Vector2.zero);GameInput.Virtual=virtualBefore;Place(origin);
   for(int i=0;i<locks.Length;i++){shutters[i].SetActive(shutterStates[i]);locks[i].enabled=enabled[i];}
   File.WriteAllText(Path.Combine(Root,"WalkValidation.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
  }
 }
}
