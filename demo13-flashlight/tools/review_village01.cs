const string folder="Assets/Art/Environments/Village01";
var preview=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/Safehouse.unity");
var previousRT=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture rt=null;UnityEngine.Texture2D texture=null;
try{
 var map=preview.GetRootGameObjects().Single(g=>g.name=="Map");var kit=map.transform.Find("Village01_Placed");
 var renderers=map.GetComponentsInChildren<UnityEngine.MeshRenderer>();
 var bad=renderers.Where(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||!m.shader.isSupported)).Select(r=>r.name).ToArray();
 var solid=map.GetComponentsInChildren<UnityEngine.Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
 UnityEngine.Physics.SyncTransforms();
 var routes=new[]{
  new[]{new UnityEngine.Vector2(54.2f,12),new UnityEngine.Vector2(52,12),new UnityEngine.Vector2(48,16),new UnityEngine.Vector2(47.5f,23),new UnityEngine.Vector2(40,29),new UnityEngine.Vector2(40,34),new UnityEngine.Vector2(34,36.5f),new UnityEngine.Vector2(34,39)},
  new[]{new UnityEngine.Vector2(40,29),new UnityEngine.Vector2(60,29),new UnityEngine.Vector2(66,27),new UnityEngine.Vector2(69,26),new UnityEngine.Vector2(70,22.5f),new UnityEngine.Vector2(72,22.5f)},
  new[]{new UnityEngine.Vector2(60,29),new UnityEngine.Vector2(65,30.2f),new UnityEngine.Vector2(68.5f,29),new UnityEngine.Vector2(74,29),new UnityEngine.Vector2(78,29)}
 };
 var blocked=new System.Collections.Generic.List<object>();int samples=0;
 foreach(var route in routes)for(int s=1;s<route.Length;s++){
  int steps=UnityEngine.Mathf.CeilToInt(UnityEngine.Vector2.Distance(route[s-1],route[s])/.25f);
  for(int j=0;j<=steps;j++){
   var p=UnityEngine.Vector2.Lerp(route[s-1],route[s],(float)j/steps);var body=new UnityEngine.Bounds(new UnityEngine.Vector3(p.x,.98f,p.y),new UnityEngine.Vector3(.64f,1.65f,.64f));samples++;
   foreach(var c in solid)if(c.bounds.Intersects(body))blocked.Add(new{position=new[]{p.x,p.y},collider=c.name});
  }
 }
 var report=new{materialsInvalid=bad,routeSamples=samples,blocked,meshRenderers=renderers.Length,decorPrefabs=kit.GetComponentsInChildren<UnityEngine.Transform>().Count(t=>UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)),missingMeshes=map.GetComponentsInChildren<UnityEngine.MeshFilter>().Count(m=>m.sharedMesh==null)};
 System.IO.File.WriteAllText(folder+"/SceneValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 var cg=new UnityEngine.GameObject("VillageReviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,preview);var cam=cg.AddComponent<UnityEngine.Camera>();cam.scene=preview;cam.orthographic=true;cam.nearClipPlane=.1f;cam.farClipPlane=250;cam.clearFlags=UnityEngine.CameraClearFlags.SolidColor;cam.backgroundColor=new UnityEngine.Color(.14f,.16f,.16f);
 var data=cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.SetRenderer(0);data.renderPostProcessing=false;
 // Hide only the foreground boundary in the review copy so the whole layout is visible.
 map.transform.Find("Wall_S").gameObject.SetActive(false);
 rt=new UnityEngine.RenderTexture(1800,1200,24);rt.Create();cam.targetTexture=rt;texture=new UnityEngine.Texture2D(1800,1200,UnityEngine.TextureFormat.RGB24,false);
 foreach(var view in new[]{new{name="TownOverview",target=new UnityEngine.Vector3(40,0,28),size=32f,yaw=0f},new{name="PawnshopPlaza",target=new UnityEngine.Vector3(37,1,35),size=10.5f,yaw=0f},new{name="HomeYard",target=new UnityEngine.Vector3(58,1,15),size=10f,yaw=25f}}){
  cam.orthographicSize=view.size;cam.transform.rotation=UnityEngine.Quaternion.Euler(55,view.yaw,0);cam.transform.position=view.target-cam.transform.forward*100;
  cam.Render();UnityEngine.RenderTexture.active=rt;texture.ReadPixels(new UnityEngine.Rect(0,0,1800,1200),0,0);texture.Apply();System.IO.File.WriteAllBytes(folder+"/Previews/"+view.name+".png",UnityEngine.ImageConversion.EncodeToPNG(texture));
 }
 return report;
}finally{UnityEngine.RenderTexture.active=previousRT;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
