var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.List<string>();
System.Action<bool,string> check=(ok,label)=>{if(!ok)throw new System.Exception(label);checks.Add(label);};
var keys=new[]{"bandit_melee_1","bandit_ranged","bandit_tank","new_unit"};
foreach(var key in keys){
 var go=UnityEngine.GameObject.Find("BanditProbe_"+key);check(go!=null,"Spawn "+key);
 bool bandit=key.StartsWith("bandit_");var vis=go.GetComponent<BanditEnemyVisual>();
 check((vis!=null)==bandit,"Scoped replacement "+key);
 if(bandit){var meshes=go.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();check(meshes.Length==2&&meshes.Any(r=>r.name=="Bandit_Body"&&r.sharedMesh.subMeshCount==1)&&meshes.Any(r=>r.name=="Bandit_Bat"),"Unified body and bat "+key);check(go.GetComponentInChildren<GreyboxLimbs>()==null,"Legacy body removed "+key);}
 go.GetComponent<EnemyController>().enabled=false;go.GetComponent<UnityEngine.Rigidbody>().linearVelocity=UnityEngine.Vector3.zero;
}
var enemy=UnityEngine.GameObject.Find("BanditProbe_bandit_melee_1").GetComponent<EnemyController>();
var visual=enemy.GetComponent<BanditEnemyVisual>();var renderers=enemy.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
enemy.SetVisionVisible(true);enemy.SetVisionVisible(false);check(renderers.All(r=>!r.enabled),"FOV hides body and bat");
enemy.SetVisionVisible(true);check(renderers.All(r=>r.enabled),"FOV restores body and bat");
var target=new UnityEngine.GameObject("BanditProbe_Target");target.transform.position=enemy.transform.position+new UnityEngine.Vector3(0,0,-.8f);
var hp=target.AddComponent<Health>();hp.SetMaxHp(1000);hp.FullHeal();
System.Action<string,object> set=(n,v)=>typeof(EnemyController).GetField(n,flags).SetValue(enemy,v);
System.Func<string,object> get=n=>typeof(EnemyController).GetField(n,flags).GetValue(enemy);
System.Action<string> call=n=>typeof(EnemyController).GetMethod(n,flags).Invoke(enemy,null);
set("player",target.transform);set("playerHealth",hp);set("attackData",null);set("_nextIsHeavy",false);
try{
 float before=hp.CurrentHp;call("DoAttack");check(enemy.CurrentState==EnemyController.State.Attack,"Attack stays in attack state");
 check(hp.CurrentHp==before,"No damage before swing");
 set("_banditAttackElapsed",(float)get("_banditHitAt"));call("UpdateAttack");
 check(hp.CurrentHp<before,"Damage at swing contact");float after=hp.CurrentHp;call("UpdateAttack");check(hp.CurrentHp==after,"One hit per swing");
 set("_banditAttackElapsed",(float)get("_banditAttackDuration"));call("UpdateAttack");check(enemy.CurrentState==EnemyController.State.Chase,"Recovery returns to chase");
 call("DoAttack");enemy.GetComponent<Health>().TakeDamage(1);check(!(bool)get("_banditPendingHit"),"Hit cancels pending strike");
 check(enemy.CurrentState==EnemyController.State.Hit,"Hit reaction preserved");
 // Use the same X but outside Z range to catch accidental XY distance checks.
 target.transform.position=enemy.transform.position+new UnityEngine.Vector3(0,0,10);call("DoAttack");set("_banditAttackElapsed",(float)get("_banditHitAt"));before=hp.CurrentHp;call("UpdateAttack");check(hp.CurrentHp==before,"Out of range on XZ cannot hit");
 visual.CancelAttack();set("state",EnemyController.State.Patrol);set("player",null);set("playerHealth",null);
 set("unitKey","");set("unitStat",null);call("OnDeath");check(enemy.IsDead&&enemy.GetComponent<LootContainer>()!=null,"Death keeps lootable corpse");
 check(renderers.All(r=>r.enabled),"Corpse visible");
}finally{UnityEngine.Object.DestroyImmediate(target);}
var report=new{passed=true,checks=checks.ToArray()};
System.IO.File.WriteAllText("Library/CodexBlender/BanditRuntimeCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
