using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.Rendering.Universal;
public static class WorldAssetReview {
 public static string Run(){
 string output=Path.GetFullPath("../ArtWork/WorldComic62/Unity");
 var paths=new[]{"Assets/Art/Environments/Hideout02/Hideout02_Interior.prefab","Assets/ChibiSurvivor/Bandit/SimpleBandit/SimpleBandit.prefab","Assets/ChibiSurvivor/Bandit/Firearms/Prefabs/Bandit_Pistol.prefab","Assets/ChibiSurvivor/Bandit/Firearms/Prefabs/Bandit_Rifle.prefab","Assets/ChibiSurvivor/Bandit/SimpleBandit/Weapons/BanditClub.prefab"};
 foreach(string path in paths){
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new Exception(path);
 foreach(int pitch in new[]{30,62}){
 var utility=new PreviewRenderUtility();Texture2D png=null;
 try{
 var instance=UnityEngine.Object.Instantiate(asset);utility.AddSingleGO(instance);
 var rs=instance.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 var c=utility.camera;var d=c.gameObject.GetComponent<UniversalAdditionalCameraData>()??c.gameObject.AddComponent<UniversalAdditionalCameraData>();d.SetRenderer(0);d.renderPostProcessing=false;d.renderShadows=true;
 c.transform.rotation=Quaternion.Euler(pitch,path.Contains("Hideout")?145:180,0);c.transform.position=b.center-c.transform.forward*30;c.orthographic=true;c.orthographicSize=b.extents.magnitude*1.12f;c.nearClipPlane=.1f;c.farClipPlane=100;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.11f,.12f,.13f);
 utility.lights[0].intensity=1.3f;utility.lights[0].transform.rotation=Quaternion.Euler(50,-30,0);utility.lights[1].intensity=.35f;utility.lights[1].transform.rotation=Quaternion.Euler(25,140,0);utility.ambientColor=new Color(.28f,.28f,.28f);
 utility.BeginStaticPreview(new Rect(0,0,1200,1200));utility.Render(true);png=utility.EndStaticPreview();File.WriteAllBytes(output+"/Asset_"+asset.name+"_"+pitch+".png",png.EncodeToPNG());
 }finally{if(png!=null)UnityEngine.Object.DestroyImmediate(png);utility.Cleanup();}
 }
 }
 return "Captured 10 asset views: actual imported models/materials, isolated Unity review lighting";
 }
}
