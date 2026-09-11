// A planted two-hand gait: smooth pelvis, low foot clearance, no added torso wobble.
const string folder="Assets/ChibiSurvivor/Bandit/SimpleBandit",output="D:/Demo/ArtWork/SimpleBandit/GripAndWalkFix";
System.IO.Directory.CreateDirectory(output);
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleBandit.prefab"),scene);
var mesh=new Mesh();
try{
 go.GetComponentInChildren<Animator>().enabled=false;var all=go.GetComponentsInChildren<Transform>();Transform Bone(string name)=>all.Single(t=>t.name==name);
 var source=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/BatWalk.anim");
 var target=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/Animations/Bandit_BatWalk.anim");
 var backup=output+"/Before_Bandit_BatWalk.anim";if(!System.IO.File.Exists(backup))System.IO.File.Copy(UnityEditor.AssetDatabase.GetAssetPath(target),backup);
 source.SampleAnimation(go,0);var skin=go.GetComponentsInChildren<SkinnedMeshRenderer>().Single();skin.BakeMesh(mesh);var weights=skin.sharedMesh.boneWeights;var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);var vertices=mesh.vertices;
 var feet=new[]{Bone("Foot.L"),Bone("Foot.R")};
 var soles=feet.Select(foot=>{int idx=System.Array.FindIndex(skin.bones,b=>b==foot);return Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==idx&&weights[i].weight0>.99f).Select(i=>foot.InverseTransformPoint(matrix.MultiplyPoint3x4(vertices[i]))).ToArray();}).ToArray();
 var modified=new[]{"Chest","Hips","Thigh.L","Shin.L","Foot.L","Thigh.R","Shin.R","Foot.R"}.Select(Bone).ToArray();
 var curves=modified.ToDictionary(t=>t,t=>Enumerable.Range(0,7).Select(i=>new System.Collections.Generic.List<Keyframe>()).ToArray());
 float maxTargetError=0,maxGroundError=0,maxGrip=0;var first=new System.Collections.Generic.Dictionary<Transform,(Vector3,Quaternion)>();float loopPosition=0,loopRotation=0;
 void Solve(string side,Vector3 targetPosition,Quaternion targetRotation){
  var a=Bone("Thigh."+side);var b=Bone("Shin."+side);var c=Bone("Foot."+side);float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
  var delta=targetPosition-a.position;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.00001f,l1+l2-.00001f);var axis=delta.normalized;var bend=Vector3.ProjectOnPlane(Vector3.forward,axis).normalized;
  float along=(l1*l1-l2*l2+d*d)/(2*d);var knee=a.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
  a.rotation=Quaternion.FromToRotation(b.position-a.position,knee-a.position)*a.rotation;b.rotation=Quaternion.FromToRotation(c.position-b.position,targetPosition-b.position)*b.rotation;c.rotation=targetRotation;
  maxTargetError=Mathf.Max(maxTargetError,Vector3.Distance(c.position,targetPosition));
 }
 const int count=96;float length=source.length;
 for(int i=0;i<=count;i++){
  float u=(float)i/count,p=2*Mathf.PI*u;source.SampleAnimation(go,u*length);
  var hips=Bone("Hips");hips.position=new Vector3(-.009f*Mathf.Sin(p),.553f-.006f*Mathf.Cos(2*p),-.004f);hips.rotation=Quaternion.Euler(4.8f,4f*Mathf.Cos(p),1f*Mathf.Sin(p));
  for(int side=0;side<2;side++){
   float cycle=Mathf.Repeat(u+(side==0?0:.5f),1),z,lift,pitch;
   if(cycle<.60f){float t=cycle/.60f;z=Mathf.Lerp(.128f,-.105f,t);lift=0;pitch=Mathf.Lerp(10,0,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.18f)))-7*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.83f,1,t));}
   else{float t=(cycle-.60f)/.40f,t2=t*t,t3=t2*t,tangent=-(.128f+.105f)/.60f*.40f;
    // Match the planted foot velocity at toe-off and landing, including the loop seam.
    z=(2*t3-3*t2+1)*(-.105f)+(t3-2*t2+t)*tangent+(-2*t3+3*t2)*.128f+(t3-t2)*tangent;
    lift=.038f*Mathf.Pow(Mathf.Sin(Mathf.PI*t),2);pitch=Mathf.Lerp(-7,10,Mathf.SmoothStep(0,1,t));}
   var rotation=Quaternion.Euler(57.5f+pitch,180,180);feet[side].rotation=rotation;float bottom=soles[side].Min(v=>feet[side].TransformVector(v).y);
   var desired=new Vector3(side==0?-.128f:.128f,-bottom+lift+.001f,z);Solve(side==0?"L":"R",desired,rotation);
   maxGroundError=Mathf.Max(maxGroundError,Mathf.Abs(soles[side].Min(v=>feet[side].TransformPoint(v).y)-(lift+.001f)));
  }
  Bone("Chest").rotation=Quaternion.Euler(10f+.5f*Mathf.Cos(2*p),-3f+1.5f*Mathf.Cos(p),.6f*Mathf.Sin(p));
  var right=Bone("HandSocket.R");maxGrip=Mathf.Max(maxGrip,Vector3.Distance(Bone("HandSocket.L").position,right.position-right.up*.15f));
  foreach(var t in modified){var pos=t.localPosition;var q=t.localRotation;var values=new[]{pos.x,pos.y,pos.z,q.x,q.y,q.z,q.w};for(int c=0;c<7;c++)curves[t][c].Add(new Keyframe(u*length,values[c]));if(i==0)first[t]=(t.position,t.rotation);if(i==count){loopPosition=Mathf.Max(loopPosition,Vector3.Distance(first[t].Item1,t.position));loopRotation=Mathf.Max(loopRotation,Quaternion.Angle(first[t].Item2,t.rotation));}}
 }
 if(maxTargetError>.002f||maxGroundError>.002f||maxGrip>.005f||loopPosition>.001f||loopRotation>.05f)throw new System.Exception("Gait constraints "+new{maxTargetError,maxGroundError,maxGrip,loopPosition,loopRotation});
 var clip=UnityEngine.Object.Instantiate(source);clip.name=target.name;string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
 foreach(var t in modified)for(int c=0;c<7;c++){var curve=new AnimationCurve(curves[t][c].ToArray());for(int k=0;k<curve.length;k++){UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);}UnityEditor.AnimationUtility.SetEditorCurve(clip,UnityEditor.EditorCurveBinding.FloatCurve(UnityEditor.AnimationUtility.CalculateTransformPath(t,go.transform),typeof(Transform),props[c]),curve);}
 clip.EnsureQuaternionContinuity();UnityEditor.EditorUtility.CopySerialized(clip,target);UnityEditor.EditorUtility.SetDirty(target);UnityEngine.Object.DestroyImmediate(clip);UnityEditor.AssetDatabase.SaveAssets();
 var report=new{passed=true,maxTargetError,maxGroundError,maxGrip,loopPosition,loopRotation,footClearance=.038f,cycleSeconds=length,stanceFraction=.60f};System.IO.File.WriteAllText(output+"/WalkCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
