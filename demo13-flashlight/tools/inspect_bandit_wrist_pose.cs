var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChibiSurvivor/Bandit/SimpleBandit/SimpleBandit.prefab"),scene);
try {
 var animator=go.GetComponentInChildren<Animator>();animator.enabled=false;
 var bones=go.GetComponentsInChildren<Transform>();
 Transform Bone(string n)=>bones.Single(t=>t.name==n);
 var hand=Bone("Hand.R");var forearm=Bone("Forearm.R");
 var rest=hand.localRotation;
 var rows=new System.Collections.Generic.List<object>();
 foreach(var name in new[]{"Bandit_CarryIdle","Bandit_CarryWalk","Bandit_CarryRun","Bandit_BatIdle","Bandit_BatWalk"}) {
  var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ChibiSurvivor/Bandit/SimpleBandit/Animations/"+name+".anim");
  if(clip==null)continue;
  for(int i=0;i<4;i++) {
   clip.SampleAnimation(go,clip.length*i/4f);
   rows.Add(new{clip=name,phase=i/4f,wristFromBindDegrees=Quaternion.Angle(rest,hand.localRotation),wristLocalEuler=hand.localEulerAngles.ToString("F1"),forearmToPalmDegrees=Vector3.Angle(hand.position-forearm.position,Bone("HandSocket.R").position-hand.position)});
  }
 }
 var report=new{rows};
 System.IO.Directory.CreateDirectory("D:/Demo/ArtWork/SimpleBandit/WristDiagnosis");
 System.IO.File.WriteAllText("D:/Demo/ArtWork/SimpleBandit/WristDiagnosis/Checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
