public static class WorldComicReview {
 static string Out=>System.IO.Path.GetFullPath("../ArtWork/WorldComic62/Unity");
 public static string Run(){
 if(!UnityEngine.Application.isPlaying)throw new System.Exception("Play runtime required");
 var rs=UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsInactive.Include).Where(r=>!(r is UnityEngine.SpriteRenderer)&&!(r is UnityEngine.ParticleSystemRenderer)).ToArray();
 var originals=rs.ToDictionary(r=>r,r=>r.sharedMaterials);var copies=new System.Collections.Generic.Dictionary<UnityEngine.Material,UnityEngine.Material>();
 foreach(var m in originals.Values.SelectMany(x=>x).Where(m=>m!=null&&m.HasProperty("_BaseMap")).Distinct()){
 var family=WorldComicImport.Family(m);if(!WorldComicImport.Eligible(family))continue;
 var clone=new UnityEngine.Material(m);WorldComicImport.Style(clone,family);copies.Add(m,clone);
 }
 try{Capture("Before");foreach(var r in rs)r.sharedMaterials=originals[r].Select(m=>m!=null&&copies.ContainsKey(m)?copies[m]:m).ToArray();Capture("Candidate");}
 finally{foreach(var pair in originals)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;foreach(var m in copies.Values)UnityEngine.Object.DestroyImmediate(m);}
 return $"Compared {copies.Count} material variants; live assignments restored";
 }
 public static void Capture(string prefix){
 var main=UnityEngine.Camera.main;if(main==null)throw new System.Exception("Main camera absent");
 var go=new UnityEngine.GameObject("WorldComicReviewCamera"){hideFlags=UnityEngine.HideFlags.HideAndDontSave};var c=go.AddComponent<UnityEngine.Camera>();c.CopyFrom(main);c.enabled=false;
 var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();var old=main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(old!=null)UnityEditor.EditorUtility.CopySerialized(old,extra);
 var rt=new UnityEngine.RenderTexture(1440,1080,24,UnityEngine.RenderTextureFormat.ARGBHalf);var active=UnityEngine.RenderTexture.active;
 var all=UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsInactive.Include);
 var roofs=all.Where(r=>r.name.EndsWith("_Roof")||r.transform.parent!=null&&r.transform.parent.name=="Roof_Art02").ToArray();var states=roofs.Select(r=>r.enabled).ToArray();
 try{
 var views=new[]{("Town",new UnityEngine.Vector3(40,0,27),26f),("Pawnshop",new UnityEngine.Vector3(34,1,40),9f),("Home",new UnityEngine.Vector3(75,1,10),5.5f),("Shops",new UnityEngine.Vector3(17,1,36),6f),("Props",new UnityEngine.Vector3(44,1,23),5f),("Medical",new UnityEngine.Vector3(69,1,36),5f),("Furniture",new UnityEngine.Vector3(22,1,23),5f),("Market",new UnityEngine.Vector3(24,1,7),5f)};
 foreach(var v in views)Save(v.Item1,v.Item2,v.Item3,62,0,false);
 for(int i=0;i<roofs.Length;i++)roofs[i].enabled=false;
 foreach(var n in new[]{"Pawnshop","DistrictWarden","VeteranScavenger","WanderingMerchant"}){
 var r=all.FirstOrDefault(r=>r is UnityEngine.SkinnedMeshRenderer&&r.gameObject.activeInHierarchy&&r.sharedMaterials.Any(m=>m!=null&&(m.name==n||m.name==n+" (Instance)")));
 if(r==null)continue;
 foreach(var yaw in new[]{0,90,180,270})Save(n+"_"+yaw,r.bounds.center,1.8f,30,yaw,true);
 Save(n+"_Top62",r.bounds.center,2.1f,62,0,true);
 }
 void Save(string name,UnityEngine.Vector3 target,float size,float pitch,float yaw,bool close){
 bool restoreFog=UnityEngine.RenderSettings.fog;if(close)UnityEngine.RenderSettings.fog=false;
 c.transform.rotation=UnityEngine.Quaternion.Euler(pitch,yaw,0);c.transform.position=target-c.transform.forward*(close?8:30);c.orthographicSize=size;c.aspect=4f/3;c.nearClipPlane=close?5:0.3f;c.targetTexture=rt;c.Render();UnityEngine.RenderTexture.active=rt;
 var t=new UnityEngine.Texture2D(1440,1080,UnityEngine.TextureFormat.RGB24,false);t.ReadPixels(new UnityEngine.Rect(0,0,1440,1080),0,0);t.Apply();System.IO.File.WriteAllBytes(Out+"/"+prefix+"_"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
 UnityEngine.RenderSettings.fog=restoreFog;
 }
 }finally{for(int i=0;i<roofs.Length;i++)if(roofs[i]!=null)roofs[i].enabled=states[i];c.targetTexture=null;UnityEngine.RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
 public static string Final(){Capture("Applied");return "Captured saved scene and material result";}
 public static string Night(){
 var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();if(cycle==null)throw new System.Exception("DayNightCycle absent");
 var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var field=typeof(DayNightCycle).GetField("isNight",flags);var method=typeof(DayNightCycle).GetMethod("ApplyLighting",flags);var previous=field.GetValue(cycle);
 var lights=UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsInactive.Include).Select(l=>new{l,l.intensity,l.color,rotation=l.transform.rotation}).ToArray();
 var sky=UnityEngine.RenderSettings.ambientSkyColor;var eq=UnityEngine.RenderSettings.ambientEquatorColor;var ground=UnityEngine.RenderSettings.ambientGroundColor;var mode=UnityEngine.RenderSettings.ambientMode;var fog=UnityEngine.RenderSettings.fog;var fc=UnityEngine.RenderSettings.fogColor;var fd=UnityEngine.RenderSettings.fogDensity;
 try{field.SetValue(cycle,true);method.Invoke(cycle,null);Capture("Night");}
 finally{field.SetValue(cycle,previous);foreach(var x in lights){x.l.intensity=x.intensity;x.l.color=x.color;x.l.transform.rotation=x.rotation;}UnityEngine.RenderSettings.ambientMode=mode;UnityEngine.RenderSettings.ambientSkyColor=sky;UnityEngine.RenderSettings.ambientEquatorColor=eq;UnityEngine.RenderSettings.ambientGroundColor=ground;UnityEngine.RenderSettings.fog=fog;UnityEngine.RenderSettings.fogColor=fc;UnityEngine.RenderSettings.fogDensity=fd;}
 return "Captured configured night lighting; gameplay phase/time untouched, render settings restored";
 }
}
