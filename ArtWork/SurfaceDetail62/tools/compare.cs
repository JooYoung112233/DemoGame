if(!UnityEngine.SceneManagement.SceneManager.GetSceneByName("Safehouse").isLoaded)throw new System.Exception("Safehouse no longer loaded; preserve current game.");
const string stage="D:/Demo/ArtWork/SurfaceDetail62";
var changes=new System.Collections.Generic.Dictionary<Material,Material>();
foreach(var name in new[]{"Town_Wood","Town_WoodDark","Town_Steel","Pawnshop_Counter","DistrictWarden"}){
 var path=name=="DistrictWarden"?"Assets/ChibiSurvivor/NPC/StoryNPC62/Materials/DistrictWarden.mat":name=="Pawnshop_Counter"?"Assets/Art/Environments/Town02/Rendering62/Pawnshop_Counter.mat":"Assets/Art/Environments/Town02/Materials/"+name+".mat";
 var original=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);var candidate=new Material(original);
 bool wood=name.Contains("Wood")||name.Contains("Counter");bool character=name=="DistrictWarden";
 var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/SurfaceDetail62/Textures/"+(character?"DistrictWarden_BaseColor_v2":wood?"Wood_Base_v2":"Steel_Base_v2")+".png");
 candidate.SetTexture("_BaseMap",texture);
 if(!character){var c=original.GetColor("_BaseColor");var ratio=wood?new Vector3(1.35569545f,1.39424937f,1.45775068f):new Vector3(1.67287622f,1.69343595f,1.71713072f);candidate.SetColor("_BaseColor",new Color(c.r*ratio.x,c.g*ratio.y,c.b*ratio.z,c.a));}
 changes.Add(original,candidate);
}
var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterials.Any(m=>m!=null&&changes.ContainsKey(m))).ToArray();var originals=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);
var roofs=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.transform.parent!=null&&r.transform.parent.name=="Roof_Art02").ToArray();var roofStates=roofs.Select(r=>r.enabled).ToArray();
var go=new GameObject("SurfaceReviewCamera");go.hideFlags=HideFlags.HideAndDontSave;var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.aspect=1.333333f;cam.transform.rotation=Quaternion.Euler(62,0,0);
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();UnityEditor.EditorUtility.CopySerialized(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(),extra);
var rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
try{
 foreach(var after in new[]{false,true}){
  foreach(var pair in originals)pair.Key.sharedMaterials=after?pair.Value.Select(m=>m!=null&&changes.ContainsKey(m)?changes[m]:m).ToArray():pair.Value;
  foreach(var view in new[]{"Warden","Pawnshop","Town","Metal"}){
   var target=view=="Warden"?new Vector3(50,.6f,16):view=="Pawnshop"?new Vector3(34,.5f,45):view=="Metal"?new Vector3(60.5f,.4f,18):new Vector3(44,0,32);
   cam.orthographicSize=view=="Town"?13:view=="Warden"?2.8f:view=="Metal"?1.8f:3.6f;cam.transform.position=target-cam.transform.forward*25;
   for(int i=0;i<roofs.Length;i++)roofs[i].enabled=view=="Pawnshop"?false:roofStates[i];
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,960,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,960),0,0);tex.Apply();System.IO.File.WriteAllBytes(stage+"/"+(after?"Candidate":"Before")+"_"+view+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
  }
 }
}finally{
 foreach(var pair in originals)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;
 for(int i=0;i<roofs.Length;i++)if(roofs[i]!=null)roofs[i].enabled=roofStates[i];
 cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);foreach(var m in changes.Values)UnityEngine.Object.DestroyImmediate(m);
}
return Newtonsoft.Json.JsonConvert.SerializeObject(new{renderers=renderers.Length,playing=UnityEditor.EditorApplication.isPlaying,result="Compared candidate base maps, restored materials and roofs; main camera/player untouched."});
