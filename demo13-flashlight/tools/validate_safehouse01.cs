const string folder="Assets/Art/Environments/Safehouse01";
var sceneBefore=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool playing=UnityEditor.EditorApplication.isPlaying;
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.RenderTexture rt=null;UnityEngine.Texture2D texture=null;var previousRT=UnityEngine.RenderTexture.active;
try{
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/Safehouse01_Cutaway.prefab");
 var root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,preview);
 var renderers=root.GetComponentsInChildren<UnityEngine.MeshRenderer>(true);
 var badMaterials=renderers.Where(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name!="Universal Render Pipeline/Lit")).Select(r=>r.name).ToArray();
 var missing=root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
 var missingMeshes=root.GetComponentsInChildren<UnityEngine.MeshFilter>(true).Count(m=>m.sharedMesh==null);
 var prefabs=System.IO.Directory.GetFiles(folder+"/Prefabs","*.prefab");
 UnityEngine.Physics.SyncTransforms();
 var samples=new[]{new UnityEngine.Vector3(0,.8f,.25f),new UnityEngine.Vector3(-.7f,.8f,1f),new UnityEngine.Vector3(-1.5f,.8f,1.65f)};
 var blocked=new System.Collections.Generic.List<string>();
 foreach(var p in samples)foreach(var c in root.GetComponentsInChildren<UnityEngine.Collider>())if(c.bounds.Contains(p))blocked.Add(c.name+":"+p);
 var hinge=root.GetComponentsInChildren<UnityEngine.Transform>(true).Single(t=>t.name=="Door_Hinge");
 var originalPosition=hinge.position;var rotation=hinge.localRotation;hinge.Rotate(UnityEngine.Vector3.up,105,UnityEngine.Space.World);
 float drift=UnityEngine.Vector3.Distance(originalPosition,hinge.position);hinge.localRotation=rotation;
 int placementCount=0;foreach(UnityEngine.Transform group in root.transform)placementCount+=group.childCount;
 var report=new {individualPrefabs=prefabs.Length,placements=placementCount,meshRenderers=renderers.Length,missingScripts=missing,missingMeshes,badMaterials,centralRouteBlocked=blocked,doorPivotDrift=drift,cutawayRoofHidden=!root.transform.Find("Roof").gameObject.activeSelf,cutawayFrontWallsHidden=!root.transform.Find("HiddenWalls").gameObject.activeSelf,playStatePreserved=playing==UnityEditor.EditorApplication.isPlaying,activeScenePreserved=sceneBefore==UnityEngine.SceneManagement.SceneManager.GetActiveScene()};
 System.IO.File.WriteAllText(folder+"/UnityValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 if(missing>0||missingMeshes>0||badMaterials.Length>0||drift>.001f||blocked.Count>0) return report;
 var cg=new UnityEngine.GameObject("SafehouseReviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,preview);
 var cam=cg.AddComponent<UnityEngine.Camera>();cam.scene=preview;cam.orthographic=true;cam.orthographicSize=3.65f;
 cam.transform.position=new UnityEngine.Vector3(-8,9,11);cam.transform.LookAt(new UnityEngine.Vector3(0,1,0));cam.clearFlags=UnityEngine.CameraClearFlags.SolidColor;cam.backgroundColor=new UnityEngine.Color(.15f,.18f,.18f);cam.nearClipPlane=.1f;cam.farClipPlane=80;
 var data=cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.SetRenderer(0);data.renderPostProcessing=false;
 foreach(var practical in root.GetComponentsInChildren<UnityEngine.Light>())practical.intensity=.35f;
 foreach(var row in new[]{new[]{-3f,7f,4f,.65f},new[]{4f,5f,1f,.25f},new[]{0f,5f,-4f,.2f}}){
  var go=new UnityEngine.GameObject("ReviewLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,preview);go.transform.position=new UnityEngine.Vector3(row[0],row[1],row[2]);go.transform.LookAt(UnityEngine.Vector3.zero);
  var light=go.AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Directional;light.intensity=row[3];light.color=new UnityEngine.Color(1,.94f,.84f);light.shadows=UnityEngine.LightShadows.Soft;
 }
 rt=new UnityEngine.RenderTexture(1400,1100,24);rt.Create();cam.targetTexture=rt;cam.Render();UnityEngine.RenderTexture.active=rt;
 texture=new UnityEngine.Texture2D(1400,1100,UnityEngine.TextureFormat.RGB24,false);texture.ReadPixels(new UnityEngine.Rect(0,0,1400,1100),0,0);texture.Apply();
 System.IO.File.WriteAllBytes(folder+"/Previews/UnityPreview.png",UnityEngine.ImageConversion.EncodeToPNG(texture));
 return report;
}finally{
 UnityEngine.RenderTexture.active=previousRT;
 if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
