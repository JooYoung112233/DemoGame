var reviewRenderer=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/ForwardRenderer.asset");var reviewAO=reviewRenderer.rendererFeatures.Single(f=>f.GetType().Name=="ScreenSpaceAmbientOcclusion");reviewAO.SetActive(true);reviewRenderer.SetDirty();
if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play required");
if(!UnityEngine.SceneManagement.SceneManager.GetSceneByName("Safehouse").isLoaded)throw new System.Exception("Safehouse no longer loaded; preserve current task.");
var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.type==LightType.Directional && l.name=="Sun3D");
var camObject=new GameObject("ShadowReviewCamera");camObject.hideFlags=HideFlags.HideAndDontSave;
var cam=camObject.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.aspect=1.333333f;
var extra=camObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();UnityEditor.EditorUtility.CopySerialized(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(),extra);
cam.transform.rotation=Camera.main.transform.rotation;
var rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGBHalf);var oldActive=RenderTexture.active;
try{
 foreach(var id in new[]{"district_warden","veteran_scavenger"}){
  var npc=UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Single(n=>n.Data.npcId==id);
  cam.transform.position=new Vector3(npc.transform.position.x,.5f,npc.transform.position.z)-cam.transform.forward*25;
  cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1280,960,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,960),0,0);tex.Apply();
  System.IO.File.WriteAllBytes("D:/Demo/ArtWork/MaterialReview62/Runtime_"+id+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
 }
}finally{cam.targetTexture=null;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camObject);}
var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{playing=UnityEditor.EditorApplication.isPlaying,sunAngle=sun.transform.eulerAngles.ToString(),sun.intensity,cameraAngle=Camera.main.transform.eulerAngles.ToString(),pipeline.shadowCascadeCount,pipeline.shadowNormalBias,pipeline.shadowDepthBias,shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(Shader.Find("BRB/GameLit")).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray()});
System.IO.File.WriteAllText("D:/Demo/ArtWork/MaterialReview62/RuntimeValidation.json",report);return report;
