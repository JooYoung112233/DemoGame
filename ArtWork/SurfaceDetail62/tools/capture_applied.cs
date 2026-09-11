if(!UnityEngine.SceneManagement.SceneManager.GetSceneByName("Safehouse").isLoaded)throw new System.Exception("Safehouse no longer loaded; preserve current game.");
const string stage="D:/Demo/ArtWork/SurfaceDetail62";
var roofs=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.transform.parent!=null&&r.transform.parent.name=="Roof_Art02").ToArray();var roofStates=roofs.Select(r=>r.enabled).ToArray();
var go=new GameObject("SurfaceReviewCamera");go.hideFlags=HideFlags.HideAndDontSave;var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.aspect=1.333333f;cam.transform.rotation=Quaternion.Euler(62,0,0);
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();UnityEditor.EditorUtility.CopySerialized(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(),extra);
var rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
try{
 foreach(var after in new[]{false}){

  foreach(var view in new[]{"Warden","Pawnshop","Town","Metal"}){
   var target=view=="Warden"?new Vector3(50,.6f,16):view=="Pawnshop"?new Vector3(34,.5f,45):view=="Metal"?new Vector3(60.5f,.4f,18):new Vector3(44,0,32);
   cam.orthographicSize=view=="Town"?13:view=="Warden"?2.8f:view=="Metal"?1.8f:3.6f;cam.transform.position=target-cam.transform.forward*25;
   for(int i=0;i<roofs.Length;i++)roofs[i].enabled=view=="Pawnshop"?false:roofStates[i];
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,960,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,960),0,0);tex.Apply();System.IO.File.WriteAllBytes(stage+"/"+"Applied"+"_"+view+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
  }
 }
}finally{

 for(int i=0;i<roofs.Length;i++)if(roofs[i]!=null)roofs[i].enabled=roofStates[i];
 cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
}
return "Applied material capture complete; original camera and roofs preserved.";
