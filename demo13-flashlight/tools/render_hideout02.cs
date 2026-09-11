// Actual Unity meshes/materials, isolated review lighting; no image generation or paint-over.
const string output="D:/Demo/ArtWork/HideoutReview";System.IO.Directory.CreateDirectory(output);
var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/Hideout02/Hideout02_Interior.prefab");
if(asset==null)throw new System.Exception("Import the Hideout02 kit first.");
var paths=new System.Collections.Generic.List<string>();
for(int view=0;view<3;view++){
 var utility=new UnityEditor.PreviewRenderUtility();Texture2D png=null;
 try{
  var instance=UnityEngine.Object.Instantiate(asset);utility.AddSingleGO(instance);
  var cam=utility.camera;var d=cam.gameObject.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()??cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();d.SetRenderer(0);d.renderPostProcessing=false;d.renderShadows=true;
  var target=view==2?new Vector3(-1.0f,.50f,-1.4f):new Vector3(0,.45f,0);
  cam.transform.position=view==1?new Vector3(-7,11,10):view==2?target+new Vector3(0,8,7):new Vector3(0,12,9);
  cam.transform.LookAt(target);cam.orthographic=true;cam.orthographicSize=view==2?2.20f:4.05f;cam.nearClipPlane=.1f;cam.farClipPlane=100;
  cam.backgroundColor=new Color(.06f,.07f,.08f);cam.clearFlags=CameraClearFlags.SolidColor;
  utility.lights[0].intensity=1.2f;utility.lights[0].color=new Color(1,.89f,.75f);utility.lights[0].shadows=LightShadows.Soft;utility.lights[0].shadowBias=.015f;utility.lights[0].shadowNormalBias=.015f;utility.lights[0].transform.rotation=Quaternion.Euler(58,160,0);
  utility.lights[1].intensity=.35f;utility.lights[1].color=new Color(.73f,.82f,1);utility.lights[1].transform.rotation=Quaternion.Euler(32,-45,0);utility.ambientColor=new Color(.24f,.24f,.24f);
  utility.BeginStaticPreview(new Rect(0,0,1500,1500));utility.Render(true);png=utility.EndStaticPreview();
  string path=output+"/"+new[]{"After","QuarterView","Detail"}[view]+".png";System.IO.File.WriteAllBytes(path,png.EncodeToPNG());paths.Add(path);
 }finally{if(png!=null)UnityEngine.Object.DestroyImmediate(png);utility.Cleanup();}
}
return new{images=paths,note="Unity asset preview with review lighting; not the runtime game camera."};
