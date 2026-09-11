if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Material comparison requires stopped Editor; preserve Play.");
const string label="After";
var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.type==LightType.Directional&&l.name=="Sun3D");
var weather=Resources.Load<WeatherData>("Data/WeatherData");
var rotation=sun.transform.rotation;var color=sun.color;var intensity=sun.intensity;
var mode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
var fog=RenderSettings.fog;
var go=new GameObject("MaterialReviewCamera");go.hideFlags=HideFlags.HideAndDontSave;
var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.aspect=1.333333f;cam.transform.rotation=Quaternion.Euler(62,0,0);
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();UnityEditor.EditorUtility.CopySerialized(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(),extra);
var rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
var roofs=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.transform.parent!=null&&r.transform.parent.name=="Roof_Art02").ToArray();
var roofStates=roofs.Select(r=>r.enabled).ToArray();
try{
 foreach(var night in new[]{false,true}){
  sun.transform.rotation=Quaternion.Euler(night?weather.nightSunAngle:weather.daySunAngle);sun.color=night?weather.nightLightColor:weather.dayLightColor;sun.intensity=night?weather.nightIntensity:weather.dayIntensity;
  DayNightCycle.ApplyAmbient(night?weather.nightAmbientColor:weather.dayAmbientColor);RenderSettings.fog=false;
  foreach(var view in new[]{"Forecourt"}){
   var target=new Vector3(34,0,37.2f);
   cam.orthographicSize=7.2f;cam.transform.position=target-cam.transform.forward*25;
   for(int i=0;i<roofs.Length;i++)roofs[i].enabled=view=="Pawnshop"?false:roofStates[i];
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var texture=new Texture2D(1280,960,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,960),0,0);texture.Apply();
   System.IO.File.WriteAllBytes("D:/Demo/ArtWork/TownFinish62/"+label+"_"+view+(night?"_Night":"_Day")+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
  }
 }
}finally{
 for(int i=0;i<roofs.Length;i++)roofs[i].enabled=roofStates[i];
 cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
 sun.transform.rotation=rotation;sun.color=color;sun.intensity=intensity;RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;RenderSettings.fog=fog;
}
return "Six same-camera daytime/night-lighting comparison renders; scene state restored.";
