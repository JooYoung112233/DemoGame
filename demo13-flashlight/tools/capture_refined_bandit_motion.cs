const string folder="Assets/ChibiSurvivor/Bandit/SimpleBandit";const string output="D:/Demo/ArtWork/SimpleBandit/MotionRefinement";
System.IO.Directory.CreateDirectory(output+"/Frames");
var utility=new UnityEditor.PreviewRenderUtility();
var animated=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleBandit.prefab"));animated.hideFlags=HideFlags.HideAndDontSave;
var animator=animated.GetComponentInChildren<Animator>();animator.Rebind();animator.SetFloat("Speed",0);animator.Update(.001f);animator.enabled=false;
var skin=animated.GetComponentsInChildren<SkinnedMeshRenderer>().Single();var club=animated.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="BanditClub");
foreach(var renderer in animated.GetComponentsInChildren<Renderer>())renderer.enabled=false;
var baked=new Mesh();baked.MarkDynamic();var displayRoot=new GameObject("Bandit review render");var bodies=new System.Collections.Generic.List<Transform>();var weapons=new System.Collections.Generic.List<Transform>();
for(int i=0;i<2;i++){
 var pivot=new GameObject("View"+i).transform;pivot.SetParent(displayRoot.transform,false);pivot.localPosition=new Vector3(i==0?-.9f:.9f,0,0);pivot.localRotation=Quaternion.Euler(0,i==0?-20:70,0);
 var body=new GameObject("Body");body.transform.SetParent(pivot,false);body.AddComponent<MeshFilter>().sharedMesh=baked;body.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;bodies.Add(body.transform);
 var bat=new GameObject("Club");bat.transform.SetParent(pivot,false);bat.AddComponent<MeshFilter>().sharedMesh=club.GetComponent<MeshFilter>().sharedMesh;bat.AddComponent<MeshRenderer>().sharedMaterial=club.sharedMaterial;weapons.Add(bat.transform);
}
var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());floor.transform.SetParent(displayRoot.transform,false);floor.transform.localPosition=new Vector3(0,.005f,0);floor.transform.localScale=new Vector3(8,.035f,6);
var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.SetColor("_BaseColor",new Color(.11f,.12f,.13f));groundMaterial.SetFloat("_Smoothness",0);floor.GetComponent<Renderer>().sharedMaterial=groundMaterial;
utility.AddSingleGO(displayRoot);utility.camera.transform.position=new Vector3(0,1.65f,6);utility.camera.transform.LookAt(new Vector3(0,.9f,0));utility.camera.orthographic=true;utility.camera.orthographicSize=1.4f;utility.camera.nearClipPlane=.01f;utility.camera.farClipPlane=20;utility.camera.clearFlags=CameraClearFlags.SolidColor;utility.camera.backgroundColor=new Color(.07f,.075f,.08f);
utility.lights[0].intensity=1.8f;utility.lights[0].transform.rotation=Quaternion.Euler(38,-25,0);utility.lights[1].intensity=1.1f;utility.lights[1].transform.rotation=Quaternion.Euler(20,150,0);utility.ambientColor=new Color(.45f,.45f,.45f);
int frame=0;const int count=300;UnityEditor.EditorApplication.CallbackFunction tick=null;var samples=new System.Collections.Generic.List<object>();
void Status(string state,string error=null){System.IO.File.WriteAllText(output+"/CaptureStatus.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{state,frame,total=count,error},Newtonsoft.Json.Formatting.Indented));}
void Cleanup(){UnityEditor.EditorApplication.update-=tick;utility.Cleanup();UnityEngine.Object.DestroyImmediate(animated);UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(groundMaterial);}
tick=()=>{
 try{
  float t=frame/30f;
  float speed=t<1.5f?0:t<1.8f?Mathf.SmoothStep(0,1,(t-1.5f)/.3f):t<4.3f?1:t<4.6f?Mathf.SmoothStep(1,0,(t-4.3f)/.3f):0;
  animator.SetFloat("Speed",speed);if(frame==157||frame==225)animator.SetTrigger("Attack");animator.Update(1f/30);
  skin.BakeMesh(baked);
  for(int i=0;i<bodies.Count;i++){bodies[i].localPosition=skin.transform.position;bodies[i].localRotation=skin.transform.rotation;bodies[i].localScale=Vector3.one;weapons[i].localPosition=club.transform.position;weapons[i].localRotation=club.transform.rotation;weapons[i].localScale=club.transform.lossyScale;}
  utility.BeginStaticPreview(new Rect(0,0,1200,720));utility.Render(true);var image=utility.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/Frames/"+frame.ToString("D4")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
  if(frame%15==0){samples.Add(new{frame,state=animator.GetCurrentAnimatorStateInfo(0).shortNameHash,time=animator.GetCurrentAnimatorStateInfo(0).normalizedTime,speed,hand=animated.GetComponentsInChildren<Transform>().Single(x=>x.name=="HandSocket.R").position.ToString("F4")});Status("running");}
  frame++;if(frame>=count){System.IO.File.WriteAllText(output+"/CaptureSamples.json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));Status("complete");Cleanup();}
 }catch(System.Exception e){Status("failed",e.ToString());Cleanup();}
};Status("running");UnityEditor.EditorApplication.update+=tick;return new{captureStarted=true,totalFrames=count,status=output+"/CaptureStatus.json"};



