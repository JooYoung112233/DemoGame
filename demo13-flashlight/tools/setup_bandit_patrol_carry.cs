const string folder="Assets/ChibiSurvivor/Bandit/SimpleBandit",hero="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game";
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleBandit.prefab"),scene);
try{
 go.GetComponentInChildren<Animator>().enabled=false;var bones=go.GetComponentsInChildren<Transform>();Transform Bone(string n)=>bones.Single(t=>t.name==n);
 var chest=Bone("Chest");var upper=chest.GetComponentsInChildren<Transform>().Where(t=>!t.GetComponent<Renderer>()&&!t.GetComponent<MeshFilter>()).ToArray();
 var neutralWrist=Bone("Hand.R").localRotation;
 var clips=new System.Collections.Generic.List<AnimationClip>();var rows=new System.Collections.Generic.List<object>();
 foreach(var mode in new[]{"Idle","Walk","Run"}){
  var source=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(hero+"/"+mode+".anim");
  var lower=mode=="Walk"?UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/Animations/Bandit_BatWalk.anim"):source;
  float length=lower.length;var clip=UnityEngine.Object.Instantiate(lower);clip.name="Bandit_Carry"+mode;
  var curves=upper.ToDictionary(t=>t,t=>Enumerable.Range(0,7).Select(i=>new System.Collections.Generic.List<Keyframe>()).ToArray());
  var idle=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(hero+"/Idle.anim");idle.SampleAnimation(go,0);
  var restArm=Bone("UpperArm.R").localRotation;var restForearm=Bone("Forearm.R").localRotation;
  int count=Mathf.RoundToInt(length*60);float lowestTip=100;Quaternion previousHand=Quaternion.identity;float maxHandStep=0;
  for(int i=0;i<=count;i++){
   float u=(float)i/count,time=u*length;source.SampleAnimation(go,u*source.length);
   var poses=upper.ToDictionary(t=>t,t=>(t.localPosition,t.localRotation));lower.SampleAnimation(go,time);
   foreach(var pair in poses){pair.Key.localPosition=pair.Value.Item1;pair.Key.localRotation=pair.Value.Item2;}
   // Carry the weight in the right arm. The empty left arm keeps the original gait.
   Bone("UpperArm.R").localRotation=Quaternion.Slerp(restArm,Bone("UpperArm.R").localRotation,mode=="Idle"?1:.30f);
   Bone("Forearm.R").localRotation=Quaternion.Slerp(restForearm,Bone("Forearm.R").localRotation,.35f);
   var hand=Bone("Hand.R");var socket=Bone("HandSocket.R");
   // Tilt the carried club with the arm chain, preserving a straight wrist.
   // Forcing a backward world-space socket direction folded the wrist over 100 degrees.
   var tiltAxis=go.transform.right;
   Bone("UpperArm.R").rotation=Quaternion.AngleAxis(18,tiltAxis)*Bone("UpperArm.R").rotation;
   Bone("Forearm.R").rotation=Quaternion.AngleAxis(12,tiltAxis)*Bone("Forearm.R").rotation;
   hand.localRotation=neutralWrist;
   if(i>0)maxHandStep=Mathf.Max(maxHandStep,Quaternion.Angle(previousHand,hand.rotation));previousHand=hand.rotation;
   lowestTip=Mathf.Min(lowestTip,(socket.position+socket.up*.636f).y);
   foreach(var t in upper){var p=t.localPosition;var q=t.localRotation;var v=new[]{p.x,p.y,p.z,q.x,q.y,q.z,q.w};for(int c=0;c<7;c++)curves[t][c].Add(new Keyframe(time,v[c]));}
  }
  if(lowestTip<.04f||maxHandStep>15)throw new System.Exception("Carry clearance/continuity "+mode+" "+lowestTip+" "+maxHandStep);
  string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
  foreach(var t in upper)for(int c=0;c<7;c++){var curve=new AnimationCurve(curves[t][c].ToArray());for(int k=0;k<curve.length;k++){UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);}UnityEditor.AnimationUtility.SetEditorCurve(clip,UnityEditor.EditorCurveBinding.FloatCurve(UnityEditor.AnimationUtility.CalculateTransformPath(t,go.transform),typeof(Transform),props[c]),curve);}
  clip.EnsureQuaternionContinuity();var settings=UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;UnityEditor.AnimationUtility.SetAnimationClipSettings(clip,settings);
  string path=folder+"/Animations/"+clip.name+".anim";var saved=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(saved==null){UnityEditor.AssetDatabase.CreateAsset(clip,path);saved=clip;}else{UnityEditor.EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);}clips.Add(saved);rows.Add(new{mode,lowestTip,maxHandStep});
 }
 var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(folder+"/SimpleBandit.controller");var sm=controller.layers[0].stateMachine;
 var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="PatrolLocomotion")??sm.AddState("PatrolLocomotion");
 var tree=state.motion as UnityEditor.Animations.BlendTree;if(tree==null){tree=new UnityEditor.Animations.BlendTree{name="PatrolCarry",blendType=UnityEditor.Animations.BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};UnityEditor.AssetDatabase.AddObjectToAsset(tree,controller);state.motion=tree;}
 tree.children=clips.Select((c,i)=>new UnityEditor.Animations.ChildMotion{motion=c,threshold=i,timeScale=1}).ToArray();sm.defaultState=state;
 UnityEditor.EditorUtility.SetDirty(tree);UnityEditor.EditorUtility.SetDirty(state);UnityEditor.EditorUtility.SetDirty(controller);UnityEditor.AssetDatabase.SaveAssets();return new{ready=true,rows};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
