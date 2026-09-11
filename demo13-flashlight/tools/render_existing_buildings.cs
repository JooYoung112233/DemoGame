// Read saved scene models into an isolated preview. Never changes the active scene or source assets.
const string output="D:/Demo/ArtWork/BuildingReview";
System.IO.Directory.CreateDirectory(output);
var activeBefore=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var playingBefore=UnityEditor.EditorApplication.isPlaying;
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/Safehouse.unity");
var report=new System.Collections.Generic.List<object>();
var sheet=new Texture2D(1800,1000,TextureFormat.RGB24,false);
try{
 var map=scene.GetRootGameObjects().Single(g=>g.name=="Map").transform;
 var names=new[]{"Pawnshop","Repair","Medical","Furniture","BlackMarket","Container_Home","TownOverview","Safehouse01_Complete"};
 for(int index=0;index<names.Length;index++){
  Transform original;
  if(index==6)original=map;
  else if(index==7){var instance=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/Safehouse01/Safehouse01_Complete.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance,scene);original=instance.transform;}
  else original=map.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name==names[index]);
  if(original==null)throw new System.Exception("Missing building: "+names[index]);
  int width=index>=6?1200:600,height=index>=6?900:500;
  var renderers=original.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.GetComponent<MeshFilter>()!=null&&r.GetComponent<MeshFilter>().sharedMesh!=null).ToArray();
  if(renderers.Length==0)throw new System.Exception("No meshes: "+names[index]);
  var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  var utility=new UnityEditor.PreviewRenderUtility();var stage=new GameObject("BuildingModelPreview");
  Texture2D png=null;
  try{
   foreach(var source in renderers){
    var go=new GameObject(source.name);go.transform.SetParent(stage.transform,false);go.transform.position=source.transform.position-bounds.center;go.transform.rotation=source.transform.rotation;go.transform.localScale=source.transform.lossyScale;
    go.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=source.sharedMaterials;
    var block=new MaterialPropertyBlock();source.GetPropertyBlock(block);mr.SetPropertyBlock(block);
    for(int m=0;m<source.sharedMaterials.Length;m++){source.GetPropertyBlock(block,m);if(!block.isEmpty)mr.SetPropertyBlock(block,m);}
   }
   utility.AddSingleGO(stage);
   var cam=utility.camera;var cameraData=cam.gameObject.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()??cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();cameraData.SetRenderer(0);cameraData.renderPostProcessing=false;
   bool east=index==3||index==4;cam.transform.position=new Vector3(east?1f:.65f,.72f,-1f).normalized*70f;cam.transform.LookAt(Vector3.zero);
   float extentX=0,extentY=0;
   foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})foreach(float z in new[]{-1f,1f}){
    var v=cam.transform.InverseTransformPoint(Vector3.Scale(bounds.extents,new Vector3(x,y,z)));extentX=Mathf.Max(extentX,Mathf.Abs(v.x));extentY=Mathf.Max(extentY,Mathf.Abs(v.y));
   }
   cam.orthographic=true;cam.orthographicSize=Mathf.Max(extentY,extentX/((float)width/height))*1.1f;cam.nearClipPlane=.1f;cam.farClipPlane=200;
   cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.12f,.14f);
   utility.lights[0].intensity=.75f;utility.lights[0].transform.rotation=Quaternion.Euler(45,-35,0);utility.lights[1].intensity=.25f;utility.lights[1].transform.rotation=Quaternion.Euler(25,140,0);utility.ambientColor=new Color(.18f,.18f,.18f);
   utility.BeginStaticPreview(new Rect(0,0,width,height));utility.Render(true);png=utility.EndStaticPreview();
   System.IO.File.WriteAllBytes(output+"/"+names[index]+".png",png.EncodeToPNG());
   if(index<6)sheet.SetPixels((index%3)*600,(1-index/3)*500,600,500,png.GetPixels());
   report.Add(new{name=names[index],source=index==7?"Assets/Art/Environments/Safehouse01/Safehouse01_Complete.prefab":"Assets/Scenes/Safehouse.unity/Map/"+UnityEditor.AnimationUtility.CalculateTransformPath(original,map),meshes=renderers.Length,size=new[]{bounds.size.x,bounds.size.y,bounds.size.z}});
  }finally{if(png!=null)UnityEngine.Object.DestroyImmediate(png);utility.Cleanup();}
 }
 sheet.Apply();System.IO.File.WriteAllBytes(output+"/Buildings.png",sheet.EncodeToPNG());
 var result=new{buildings=report,sourceUnchanged=true,activeScenePreserved=activeBefore==UnityEngine.SceneManagement.SceneManager.GetActiveScene(),playStatePreserved=playingBefore==UnityEditor.EditorApplication.isPlaying,note="Saved scene geometry and materials. Neutral preview lights; each tile framed individually, not a shared scale."};
 System.IO.File.WriteAllText(output+"/Sources.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
}finally{UnityEngine.Object.DestroyImmediate(sheet);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
