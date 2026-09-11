var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var checks=new System.Collections.Generic.List<string>();
void Check(bool ok,string message){if(!ok)throw new System.Exception(message);checks.Add(message);}
var baked=new Mesh();
try{
 var go=new GameObject("CarryProbe");go.AddComponent<CapsuleCollider>();var rb=go.AddComponent<Rigidbody>();rb.useGravity=false;var enemy=go.AddComponent<EnemyController>();enemy.enabled=false;
 var visual=go.AddComponent<BanditEnemyVisual>();Check(visual.Initialize(enemy,1),"Runtime resource initializes");var animator=go.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
 var wrist=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="Hand.R");var neutralWrist=wrist.localRotation;float maxWristDeviation=0;
 void State(EnemyController.State s){typeof(EnemyController).GetField("state",flags).SetValue(enemy,s);}
 void Tick(){typeof(BanditEnemyVisual).GetMethod("LateUpdate",flags).Invoke(visual,null);animator.Update(.35f);}
 State(EnemyController.State.Patrol);Tick();Check(animator.GetCurrentAnimatorStateInfo(0).IsName("PatrolLocomotion"),"Patrol uses one-hand carry");
 State(EnemyController.State.Investigate);Tick();Check(animator.GetCurrentAnimatorStateInfo(0).IsName("PatrolLocomotion"),"Noise investigation keeps casual carry until enemy detected");
 var target=new GameObject("DetectedTarget");typeof(EnemyController).GetField("player",flags).SetValue(enemy,target.transform);
 State(EnemyController.State.Chase);Tick();Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Detected target switches to two-hand ready");
 State(EnemyController.State.Hit);Tick();Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Hit reaction retains combat stance");
 State(EnemyController.State.Patrol);Tick();Check(animator.GetCurrentAnimatorStateInfo(0).IsName("PatrolLocomotion"),"Lost target returns to one-hand carry");
 var hand=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="HandSocket.R");var left=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="HandSocket.L");
 var skin=go.GetComponentsInChildren<SkinnedMeshRenderer>().Single();var club=go.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="BanditClub");var weights=skin.sharedMesh.boneWeights;int idx=System.Array.FindIndex(skin.bones,b=>b.name=="Hand.R");var ids=Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==idx&&weights[i].weight0>.99f).ToArray();
 float maxPalm=0,minTip=100,minFreeHand=100;var bounds=club.GetComponent<MeshFilter>().sharedMesh.bounds;
 foreach(float speed in new[]{0f,.5f,1f,1.5f,2f}){
  animator.Play("PatrolLocomotion",0,0);animator.SetFloat("Speed",speed);animator.Update(0);
  for(int i=0;i<180;i++){
   animator.Update(1f/120);maxWristDeviation=Mathf.Max(maxWristDeviation,Quaternion.Angle(neutralWrist,wrist.localRotation));skin.BakeMesh(baked);var vertices=baked.vertices;var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
   var center=ids.Aggregate(Vector3.zero,(sum,n)=>sum+club.transform.InverseTransformPoint(matrix.MultiplyPoint3x4(vertices[n])))/ids.Length;
   maxPalm=Mathf.Max(maxPalm,Vector2.Distance(new Vector2(center.x,center.z),new Vector2(bounds.center.x,bounds.center.z)));
   minTip=Mathf.Min(minTip,club.transform.TransformPoint(new Vector3(bounds.center.x,bounds.max.y,bounds.center.z)).y);minFreeHand=Mathf.Min(minFreeHand,Vector3.Distance(hand.position,left.position));
  }
 }
 Check(maxWristDeviation<5,"Carry wrist stays neutral across idle, walk, run and blended speeds");
 Check(maxPalm<.015f,"Right palm encloses actual bat handle in all casual gaits");Check(minTip>.04f,"Carried bat clears ground");Check(minFreeHand>.20f,"Left hand is free during patrol");
 State(EnemyController.State.Patrol);Tick();var before=hand.position;visual.Windup(.8f);visual.SetWindupRemaining(.8f);typeof(BanditEnemyVisual).GetMethod("LateUpdate",flags).Invoke(visual,null);Check(Vector3.Distance(before,hand.position)<.001f,"Close detection starts windup from current carry without snap");
 visual.SetWindupRemaining(.55f);typeof(BanditEnemyVisual).GetMethod("LateUpdate",flags).Invoke(visual,null);Check(Vector3.Distance(left.position,hand.position-hand.up*.15f)<.01f,"Close detection completes two-hand pickup during windup");
 var report=new{passed=true,maxWristDeviation,maxPalmError=maxPalm,minTipHeight=minTip,minFreeHandDistance=minFreeHand,checks};var dir="D:/Demo/ArtWork/SimpleBandit/PatrolCarry";System.IO.Directory.CreateDirectory(dir);System.IO.File.WriteAllText(dir+"/Checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEngine.Object.DestroyImmediate(baked);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);}
