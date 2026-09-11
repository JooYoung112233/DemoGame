// Isolated Editor scene: invoke real AI attack methods without starting a raid or touching saves.
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
var random=UnityEngine.Random.state;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.List<string>();
System.Action<bool,string> check=(ok,label)=>{if(!ok)throw new System.Exception(label);checks.Add(label);};
try{
 var go=new GameObject("BanditRuntimeProbe");go.AddComponent<CapsuleCollider>();var rb=go.AddComponent<Rigidbody>();rb.useGravity=false;
 var enemy=go.AddComponent<EnemyController>();enemy.enabled=false;
 System.Action<string,object> set=(n,v)=>typeof(EnemyController).GetField(n,flags).SetValue(enemy,v);
 System.Func<string,object> get=n=>typeof(EnemyController).GetField(n,flags).GetValue(enemy);
 System.Action<string> call=n=>typeof(EnemyController).GetMethod(n,flags).Invoke(enemy,null);
 set("_rb",rb);set("renderers",new Renderer[0]);
 var visual=go.AddComponent<BanditEnemyVisual>();check(visual.Initialize(enemy,1),"Loads prepared bandit through runtime Resources path");set("_banditVisual",visual);
 var animator=go.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
 var body=go.GetComponentsInChildren<SkinnedMeshRenderer>();var renderers=go.GetComponentsInChildren<Renderer>();
 check(body.Length==1&&body[0].name=="Bandit_Body","Single skinned character body");
 var club=renderers.Single(r=>r.name=="BanditClub");check(club is MeshRenderer&&club.transform.parent.name=="HandSocket.R","Rigid club attached to right socket");
 check(animator.runtimeAnimatorController.name=="SimpleBandit","New dedicated motion controller");
 enemy.SetVisionVisible(false);check(renderers.All(r=>!r.enabled),"FOV hides body and weapon");enemy.SetVisionVisible(true);check(renderers.All(r=>r.enabled),"FOV restores body and weapon");
 var target=new GameObject("Target");target.transform.position=new Vector3(0,0,-.8f);var hp=target.AddComponent<Health>();hp.SetMaxHp(1000);hp.FullHeal();
 set("player",target.transform);set("playerHealth",hp);set("attackData",null);set("_nextIsHeavy",false);
 System.Action sample=()=>typeof(BanditEnemyVisual).GetMethod("LateUpdate",flags).Invoke(visual,null);
 float before=hp.CurrentHp;call("DoAttack");check(hp.CurrentHp==before,"Attack start does not deal instant damage");
 visual.SetAttackElapsed((float)get("_banditHitAt"));sample();
 var info=animator.GetCurrentAnimatorStateInfo(0);check(info.IsName("SwordSlash")&&Mathf.Abs(info.normalizedTime-.385f)<.002f,"Contact samples new bat clip at 38.5 percent");
 sample();check(Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-.385f)<.002f,"Visual update does not advance damage clock");
 set("_banditAttackElapsed",(float)get("_banditHitAt"));call("UpdateAttack");check(hp.CurrentHp<before,"Damage on contact");float after=hp.CurrentHp;call("UpdateAttack");check(hp.CurrentHp==after,"Only one damage event per swing");
 set("_banditAttackElapsed",(float)get("_banditAttackDuration"));call("UpdateAttack");check(enemy.CurrentState==EnemyController.State.Chase,"Recovery returns to chase");sample();animator.Update(.15f);check(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Animation returns to locomotion");
 call("DoAttack");typeof(EnemyController).GetMethod("OnDamaged",flags).Invoke(enemy,new object[]{1f});check(enemy.CurrentState==EnemyController.State.Hit&&!(bool)get("_banditPendingHit"),"Hit reaction cancels pending attack");
 target.transform.position=new Vector3(0,0,10);call("DoAttack");before=hp.CurrentHp;set("_banditAttackElapsed",(float)get("_banditHitAt"));call("UpdateAttack");check(hp.CurrentHp==before,"Out of range target takes no damage");
 visual.CancelAttack();set("state",EnemyController.State.Patrol);
 var left=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="HandSocket.L");var right=club.transform.parent;float grip=0,palmError=0;
 var weights=body[0].sharedMesh.boneWeights;var palmIndices=new[]{"Hand.L","Hand.R"}.Select(name=>{int index=System.Array.FindIndex(body[0].bones,b=>b.name==name);return Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==index&&weights[i].weight0>.99f).ToArray();}).ToArray();
 var batBounds=club.GetComponent<MeshFilter>().sharedMesh.bounds;var baked=new Mesh();
 try{
 foreach(var state in new[]{"SwordWalk","Locomotion","SwordSlash"}){
  animator.speed=1;animator.SetFloat("Speed",state=="Locomotion"?2:0);animator.Play(state,0,0);animator.Update(0);
  for(int i=0;i<120;i++){
   animator.Update(.01f);grip=Mathf.Max(grip,Vector3.Distance(left.position,right.position-right.up*.15f));
   body[0].BakeMesh(baked);var vertices=baked.vertices;var matrix=Matrix4x4.TRS(body[0].transform.position,body[0].transform.rotation,Vector3.one);
   foreach(var indices in palmIndices){var center=indices.Aggregate(Vector3.zero,(sum,index)=>sum+club.transform.InverseTransformPoint(matrix.MultiplyPoint3x4(vertices[index])))/indices.Length;
    palmError=Mathf.Max(palmError,Vector2.Distance(new Vector2(center.x,center.z),new Vector2(batBounds.center.x,batBounds.center.z)));
    check(center.y>-.19f&&center.y<.065f,"Palm lies along wrapped grip");
   }
  }
 }
 check(grip<.01f,"Two-hand grip holds through walk, run and attack");
 check(palmError<.015f,"Actual palm meshes surround club handle, not just attachment sockets");
 }finally{UnityEngine.Object.DestroyImmediate(baked);}
 visual.Die();check(renderers.All(r=>r.enabled),"Death presentation keeps corpse visible");
 var report=new{passed=true,mode="Isolated Editor scene / real EnemyController attack methods",maxGripError=grip,maxPalmToHandleError=palmError,checks=checks.Distinct().ToArray(),fullRaidPlaytest=false};
 System.IO.File.WriteAllText("D:/Demo/ArtWork/SimpleBandit/CombatReview/RuntimeCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEngine.Random.state=random;UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);}
