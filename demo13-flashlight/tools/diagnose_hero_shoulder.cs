var renderers=UnityEngine.Object.FindObjectsByType<UnityEngine.SkinnedMeshRenderer>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Hero_")&&r.transform.root.name=="Player").ToArray();
var cam=UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None).First(c=>c.enabled&&c.gameObject.activeInHierarchy);
var lights=UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None);var shadows=lights.Select(l=>l.shadows).ToArray();
var materials=renderers.Select(r=>r.sharedMaterials).ToArray();var temps=new System.Collections.Generic.List<UnityEngine.Material>();
var oldTarget=cam.targetTexture;var oldActive=UnityEngine.RenderTexture.active;var rt=new UnityEngine.RenderTexture(1400,1000,24);rt.Create();var tex=new UnityEngine.Texture2D(1400,1000,UnityEngine.TextureFormat.RGB24,false);
string dir="Library/CodexBlender/ShoulderDiagnosis";System.IO.Directory.CreateDirectory(dir);
try{
 cam.targetTexture=rt;
 foreach(var mode in new[]{"Current","NoShadows","NoOutline","Unlit"}){
  if(mode=="NoShadows")foreach(var l in lights)l.shadows=UnityEngine.LightShadows.None;
  if(mode=="NoOutline"){
   for(int li=0;li<lights.Length;li++)lights[li].shadows=shadows[li];
   for(int i=0;i<renderers.Length;i++){
    var mm=materials[i].Select(src=>{var m=new UnityEngine.Material(src);m.SetShaderPassEnabled("SRPDefaultUnlit",false);m.SetShaderPassEnabled("Outline",false);if(m.HasProperty("_OutlineWidth"))m.SetFloat("_OutlineWidth",0);if(m.HasProperty("_OutlinePush"))m.SetFloat("_OutlinePush",0);temps.Add(m);return m;}).ToArray();renderers[i].sharedMaterials=mm;
   }
  }
  if(mode=="Unlit")for(int i=0;i<renderers.Length;i++){
   var mm=new UnityEngine.Material[materials[i].Length];
   for(int j=0;j<mm.Length;j++){var src=materials[i][j];var m=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",src.HasProperty("_BaseColor")?src.GetColor("_BaseColor"):UnityEngine.Color.white);m.SetTexture("_BaseMap",src.GetTexture("_BaseMap"));temps.Add(m);mm[j]=m;}
   renderers[i].sharedMaterials=mm;
  }
  cam.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,1400,1000),0,0);tex.Apply();System.IO.File.WriteAllBytes(dir+"/"+mode+".png",tex.EncodeToPNG());
 }
 return new{camera=cam.name,parts=renderers.Length,shoulders=renderers.Select((r,i)=>new{r,i}).Where(a=>a.r.name.Contains("Sleeve")&&!a.r.name.Contains("Rolled")).Select(a=>new{name=a.r.name,vertices=a.r.sharedMesh.vertexCount,uv=a.r.sharedMesh.uv.Length,colors=a.r.sharedMesh.colors.Length,materials=materials[a.i].Select(m=>new{m.name,shader=m.shader.name,baseMap=UnityEditor.AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")),color=m.GetColor("_BaseColor").ToString(),ao=m.HasProperty("_VertexAO")?m.GetFloat("_VertexAO"):-1,wrap=m.HasProperty("_Wrap")?m.GetFloat("_Wrap"):-1}).ToArray()}).ToArray()};
}finally{
 for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=materials[i];for(int i=0;i<lights.Length;i++)lights[i].shadows=shadows[i];
 cam.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;foreach(var m in temps)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
}

