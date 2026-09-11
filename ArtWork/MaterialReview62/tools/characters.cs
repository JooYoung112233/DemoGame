if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Preserve Play");
var snapshots=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("D:/Demo/ArtWork/MaterialReview62/BeforeMaterials.json"));
var objects=new System.Collections.Generic.List<GameObject>();var materials=new System.Collections.Generic.List<Material>();
var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.name=="Sun3D");var rot=sun.transform.rotation;var col=sun.color;var power=sun.intensity;
var mode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
var w=Resources.Load<WeatherData>("Data/WeatherData");
var go=new GameObject("CharacterMaterialCamera");objects.Add(go);go.hideFlags=HideFlags.HideAndDontSave;var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.cullingMask=1<<31;cam.orthographicSize=2.2f;cam.aspect=1.6f;cam.transform.rotation=Quaternion.Euler(62,0,0);cam.transform.position=new Vector3(10000,.6f,10000)-cam.transform.forward*25;
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();UnityEditor.EditorUtility.CopySerialized(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(),extra);
var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
var originals=new System.Collections.Generic.Dictionary<Renderer,Material[]>();
var paths=new[]{"Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/SimpleHero.prefab","Assets/ChibiSurvivor/Bandit/SimpleBandit/SimpleBandit.prefab","Assets/ChibiSurvivor/NPC/StoryNPC62/Visuals/DistrictWarden.prefab"};
try{
 for(int i=0;i<paths.Length;i++){
  var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);if(prefab==null)throw new System.Exception(paths[i]);
  var obj=UnityEngine.Object.Instantiate(prefab);objects.Add(obj);obj.hideFlags=HideFlags.HideAndDontSave;obj.transform.position=new Vector3(10000+(i-1)*2.1f,0,10000);obj.transform.rotation=Quaternion.Euler(0,180,0);
  foreach(var t in obj.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
  foreach(var r in obj.GetComponentsInChildren<Renderer>(true))originals.Add(r,r.sharedMaterials);
 }
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(floor);floor.hideFlags=HideFlags.HideAndDontSave;floor.layer=31;UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());floor.transform.position=new Vector3(10000,-.075f,10000);floor.transform.localScale=new Vector3(9,.1f,8);floor.GetComponent<Renderer>().sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Town02/Rendering62/Town_Ground.mat");
 foreach(var night in new[]{false,true}){
  sun.transform.rotation=Quaternion.Euler(night?w.nightSunAngle:w.daySunAngle);sun.color=night?w.nightLightColor:w.dayLightColor;sun.intensity=night?w.nightIntensity:w.dayIntensity;DayNightCycle.ApplyAmbient(night?w.nightAmbientColor:w.dayAmbientColor);
  foreach(var before in new[]{true,false}){
   foreach(var pair in originals){
    pair.Key.sharedMaterials=pair.Value.Select(source=>{
     if(source==null)return source;var m=new Material(source);materials.Add(m);var path=UnityEditor.AssetDatabase.GetAssetPath(source);var data=snapshots.FirstOrDefault(t=>(string)t["path"]==path);
     if(before&&data!=null){foreach(var entry in ((Newtonsoft.Json.Linq.JObject)data["floats"]).Properties())m.SetFloat(entry.Name,(float)entry.Value);m.shaderKeywords=data["keywords"].Select(v=>(string)v).ToArray();}
     return m;
    }).ToArray();
   }
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();System.IO.File.WriteAllBytes("D:/Demo/ArtWork/MaterialReview62/Characters_"+(before?"Before":"After")+(night?"_Night":"_Day")+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
  }
 }
}finally{
 cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);foreach(var o in objects)UnityEngine.Object.DestroyImmediate(o);foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);
 sun.transform.rotation=rot;sun.color=col;sun.intensity=power;RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;
}
return "Character material comparison complete: player, bandit, warden; isolated lighting without player lamps.";
