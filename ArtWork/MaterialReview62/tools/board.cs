if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Preserve Play");
var snapshots=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("D:/Demo/ArtWork/MaterialReview62/BeforeMaterials.json"));
var objects=new System.Collections.Generic.List<GameObject>();var materials=new System.Collections.Generic.List<Material>();
var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.name=="Sun3D");var rot=sun.transform.rotation;var col=sun.color;var power=sun.intensity;
var mode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
var w=Resources.Load<WeatherData>("Data/WeatherData");
var go=new GameObject("MaterialBoardCamera");objects.Add(go);go.hideFlags=HideFlags.HideAndDontSave;var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.cullingMask=1<<31;cam.orthographicSize=2.5f;cam.aspect=1.6f;cam.transform.rotation=Quaternion.Euler(62,0,0);cam.transform.position=new Vector3(10000,.2f,10000)-cam.transform.forward*25;
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();UnityEditor.EditorUtility.CopySerialized(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(),extra);
var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
var meshRenderers=new System.Collections.Generic.List<Renderer>();
var names=new[]{"Town_Concrete","Town_Wood","Town_Steel","Town_Canvas"};
try{
 sun.transform.rotation=Quaternion.Euler(w.daySunAngle);sun.color=w.dayLightColor;sun.intensity=w.dayIntensity;DayNightCycle.ApplyAmbient(w.dayAmbientColor);
 for(int i=0;i<4;i++){
  foreach(var sphere in new[]{false,true}){
   var obj=GameObject.CreatePrimitive(sphere?PrimitiveType.Sphere:PrimitiveType.Cube);objects.Add(obj);obj.hideFlags=HideFlags.HideAndDontSave;obj.layer=31;
   UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.transform.position=new Vector3(10000+(i-1.5f)*1.8f,.5f,10000+(sphere?.75f:-.75f));obj.transform.localScale=Vector3.one;
   meshRenderers.Add(obj.GetComponent<Renderer>());
  }
 }
 foreach(var before in new[]{true,false}){
  for(int i=0;i<4;i++){
   string path="Assets/Art/Environments/Town02/Materials/"+names[i]+".mat";var m=new Material(UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path));materials.Add(m);
   if(before){var data=snapshots.Single(t=>(string)t["path"]==path);foreach(var entry in ((Newtonsoft.Json.Linq.JObject)data["floats"]).Properties())m.SetFloat(entry.Name,(float)entry.Value);m.shaderKeywords=data["keywords"].Select(v=>(string)v).ToArray();}
   meshRenderers[i*2].sharedMaterial=m;meshRenderers[i*2+1].sharedMaterial=m;
  }
  cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();System.IO.File.WriteAllBytes("D:/Demo/ArtWork/MaterialReview62/Board_"+(before?"Before":"After")+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
 }
}finally{
 cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);foreach(var o in objects)UnityEngine.Object.DestroyImmediate(o);foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);
 sun.transform.rotation=rot;sun.color=col;sun.intensity=power;RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;
}
return "Controlled material board: left to right concrete, wood, steel, canvas; identical geometry and light.";
