if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Review requires stopped Editor; do not interrupt Play.");
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.type==LightType.Directional && l.name=="Sun3D");
var weather=Resources.Load<WeatherData>("Data/WeatherData");
var settings=UnityEditor.EditorJsonUtility.ToJson(pipeline);
var sunRotation=sun.transform.rotation;var sunColor=sun.color;var sunIntensity=sun.intensity;
var ambientMode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
var camObject=new GameObject("ShadowReviewCamera");camObject.hideFlags=HideFlags.HideAndDontSave;
var cam=camObject.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.orthographic=true;cam.orthographicSize=3.6f;cam.aspect=1.333333f;
var extra=camObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var mainExtra=Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(mainExtra!=null)UnityEditor.EditorUtility.CopySerialized(mainExtra,extra);
cam.transform.rotation=Quaternion.Euler(62,0,0);cam.transform.position=new Vector3(50,0.5f,16)-cam.transform.forward*25;
var rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGBHalf);var oldActive=RenderTexture.active;
var output="D:/Demo/ArtWork/ShadowReview62";
try{
 sun.color=weather.dayLightColor;sun.intensity=weather.dayIntensity;
 RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
 RenderSettings.ambientSkyColor=weather.dayAmbientColor*1.25f;RenderSettings.ambientEquatorColor=weather.dayAmbientColor;RenderSettings.ambientGroundColor=weather.dayAmbientColor*.55f;
 foreach(var phase in new[]{"Before","After"}){
  sun.transform.rotation=Quaternion.Euler(phase=="Before"?weather.daySunAngle:new Vector3(58,-30,0));
  if(phase=="After"){
   pipeline.shadowCascadeCount=4;pipeline.cascade4Split=new Vector3(.35f,.6f,.85f);
   pipeline.shadowDepthBias=.5f;pipeline.shadowNormalBias=.25f;
  }
  cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1280,960,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,960),0,0);tex.Apply();
  System.IO.File.WriteAllBytes(output+"/"+phase+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
 }
}finally{
 cam.targetTexture=null;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camObject);
 sun.transform.rotation=sunRotation;sun.color=sunColor;sun.intensity=sunIntensity;
 RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
 UnityEditor.EditorJsonUtility.FromJsonOverwrite(settings,pipeline);
}
return "Before/After rendered with 62 degree camera and restored all preview settings; Play untouched.";
