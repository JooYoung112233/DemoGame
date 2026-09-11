const string folder="Assets/ChibiSurvivor/Bandit/SimpleBandit",hero="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game",output="D:/Demo/ArtWork/SimpleBandit/MotionRefinement";
System.IO.Directory.CreateDirectory(output);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleBandit.prefab"),preview);
var animator=instance.GetComponentInChildren<Animator>();animator.enabled=false;
var transforms=instance.GetComponentsInChildren<Transform>();Transform Bone(string n)=>transforms.Single(t=>t.name==n);
var chest=Bone("Chest");var hips=Bone("Hips");
var modified=new[]{"Chest","Hips","Thigh.L","Shin.L","Foot.L","Thigh.R","Shin.R","Foot.R"}.Select(Bone).ToArray();
var protectedPaths=new[]{hero+"/BatWalk.anim",hero+"/BatSwing.anim",folder+"/Animations/Bandit_BatIdle.anim",folder+"/Animations/Bandit_BatRun.anim"};
var protectedBytes=protectedPaths.ToDictionary(p=>p,System.IO.File.ReadAllBytes);
var records=new System.Collections.Generic.List<object>();
void SolveLeg(string side,Vector3 target,Quaternion rotation){
 var a=Bone("Thigh."+side);var b=Bone("Shin."+side);var c=Bone("Foot."+side);
 float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
 var delta=target-a.position;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.00001f,l1+l2-.00001f);var axis=delta.normalized;
 var bend=Vector3.ProjectOnPlane(b.position-a.position,axis).normalized;if(bend.sqrMagnitude<.1f)bend=Vector3.ProjectOnPlane(Vector3.forward,axis).normalized;
 float along=(l1*l1-l2*l2+d*d)/(2*d);var knee=a.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
 a.rotation=Quaternion.FromToRotation(b.position-a.position,knee-a.position)*a.rotation;
 b.rotation=Quaternion.FromToRotation(c.position-b.position,target-b.position)*b.rotation;c.rotation=rotation;
}
try{
 foreach(var item in new[]{("BatWalk","Bandit_BatWalk",1f),("BatSwing","Bandit_BatAttack",.9f)}){
  bool walk=item.Item1=="BatWalk";var source=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(hero+"/"+item.Item1+".anim");
  var target=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/Animations/"+item.Item2+".anim");
  var backup=output+"/Before_"+item.Item2+".anim";if(!System.IO.File.Exists(backup))System.IO.File.Copy(UnityEditor.AssetDatabase.GetAssetPath(target),backup);
  var result=UnityEngine.Object.Instantiate(source);result.name=item.Item2;
  if(item.Item3!=1)foreach(var binding in UnityEditor.AnimationUtility.GetCurveBindings(result)){
   var curve=UnityEditor.AnimationUtility.GetEditorCurve(result,binding);var keys=curve.keys;
   for(int i=0;i<keys.Length;i++){keys[i].time*=item.Item3;keys[i].inTangent/=item.Item3;keys[i].outTangent/=item.Item3;}curve.keys=keys;UnityEditor.AnimationUtility.SetEditorCurve(result,binding,curve);
  }
  float length=source.length*item.Item3;int count=Mathf.RoundToInt(length*60);float maxFoot=0,maxGrip=0,maxRaise=0,maxChest=0;
  var curves=modified.ToDictionary(t=>t,t=>Enumerable.Range(0,7).Select(i=>new System.Collections.Generic.List<Keyframe>()).ToArray());
  for(int i=0;i<=count;i++){
   float u=(float)i/count,time=u*length;source.SampleAnimation(instance,time/item.Item3);
   var feet=new[]{Bone("Foot.L"),Bone("Foot.R")};var positions=feet.Select(t=>t.position).ToArray();var rotations=feet.Select(t=>t.rotation).ToArray();var oldChest=chest.rotation;
   if(walk){
    float phase=2*Mathf.PI*u;
    // Rotate the common torso parent: both hands and the weapon move together.
    chest.rotation=Quaternion.AngleAxis(2.2f*Mathf.Sin(phase),Vector3.up)*Quaternion.AngleAxis(1.5f*Mathf.Sin(phase+.35f),Vector3.forward)*chest.rotation;
   }else{
    // Preserve anticipation and contact. Ease out excess crouch only in follow-through.
    float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.385f,.52f,u))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,.94f,u)));
    float raise=.028f*envelope;hips.position+=Vector3.up*raise;maxRaise=Mathf.Max(maxRaise,raise);
    SolveLeg("L",positions[0],rotations[0]);SolveLeg("R",positions[1],rotations[1]);
    var torso=(Bone("Head").position-chest.position).normalized;
    chest.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(torso,Vector3.up),.20f*envelope)*chest.rotation;
   }
   maxChest=Mathf.Max(maxChest,Quaternion.Angle(oldChest,chest.rotation));
   for(int k=0;k<2;k++)maxFoot=Mathf.Max(maxFoot,Vector3.Distance(feet[k].position,positions[k]));
   var right=Bone("HandSocket.R");maxGrip=Mathf.Max(maxGrip,Vector3.Distance(Bone("HandSocket.L").position,right.position-right.up*.15f));
   foreach(var t in modified){var p=t.localPosition;var q=t.localRotation;var values=new[]{p.x,p.y,p.z,q.x,q.y,q.z,q.w};for(int c=0;c<7;c++)curves[t][c].Add(new Keyframe(time,values[c]));}
  }
  if(maxFoot>.003f||maxGrip>.005f)throw new System.Exception("Foot/grip drift "+item.Item2+": "+maxFoot+" / "+maxGrip);
  string[] properties={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
  foreach(var t in modified)for(int c=0;c<7;c++){
   var curve=new AnimationCurve(curves[t][c].ToArray());
   for(int k=0;k<curve.length;k++){UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);}
   UnityEditor.AnimationUtility.SetEditorCurve(result,UnityEditor.EditorCurveBinding.FloatCurve(UnityEditor.AnimationUtility.CalculateTransformPath(t,instance.transform),typeof(Transform),properties[c]),curve);
  }
  result.EnsureQuaternionContinuity();var settings=UnityEditor.AnimationUtility.GetAnimationClipSettings(target);settings.stopTime=length;UnityEditor.AnimationUtility.SetAnimationClipSettings(result,settings);
  UnityEditor.EditorUtility.CopySerialized(result,target);UnityEditor.EditorUtility.SetDirty(target);UnityEngine.Object.DestroyImmediate(result);
  records.Add(new{clip=item.Item2,length,maxFootDrift=maxFoot,maxGripError=maxGrip,maxHipRaise=maxRaise,maxChestRotation=maxChest});
 }
 UnityEditor.AssetDatabase.SaveAssets();foreach(var pair in protectedBytes)if(!pair.Value.SequenceEqual(System.IO.File.ReadAllBytes(pair.Key)))throw new System.Exception("Unrelated clip changed");
 var report=new{passed=true,records,playerIdleRunUnchanged=true};System.IO.File.WriteAllText(output+"/RefinementCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
