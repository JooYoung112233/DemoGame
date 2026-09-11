const string folder="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game";
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleHero.prefab");var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(folder+"/SimpleHero.controller");
var rigPrefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PlayerRig.prefab");
foreach(var p in rigPrefab.GetComponentsInChildren<TopDownPlayer>(true)){var s=new UnityEditor.SerializedObject(p);if(s.FindProperty("character3DPrefab").objectReferenceValue!=model||s.FindProperty("character3DController").objectReferenceValue!=controller)throw new System.Exception("PlayerRig reference mismatch");}
var root=new GameObject("SimpleHero_Verify");root.hideFlags=HideFlags.HideAndDontSave;root.transform.position=new Vector3(0,-1000,0);var visual=root.AddComponent<ChibiPlayerVisual>();var records=new System.Collections.Generic.List<object>();
try{
 if(!visual.Initialize(model,controller,null,1))throw new System.Exception("Initialize failed");
 var animator=root.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
 var bones=root.GetComponentsInChildren<Transform>();var left=bones.Single(t=>t.name=="HandSocket.L");var right=bones.Single(t=>t.name=="HandSocket.R");var head=bones.Single(t=>t.name=="Head");
 var sword=root.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Hero_SwordProxy");
 if(sword.enabled||!visual.HasSwordAnimations)throw new System.Exception("Weapon initial state");
 foreach(var armed in new[]{false,true})foreach(var speed in new[]{0f,1f,2f}){
  visual.SetSwordEquipped(armed);float maxGrip=0;var positions=new System.Collections.Generic.List<Vector3>();
  for(int k=0;k<50;k++){visual.UpdateMotion(Vector2.up,speed,speed==2,true,1,.02f);animator.Update(.02f);if(k>15){positions.Add(head.position);maxGrip=Mathf.Max(maxGrip,Vector3.Distance(left.position,right.position-right.up*.15f));}}
  var active=animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(x=>x.weight).First().clip.name;
  string expected=armed&&speed==1?"SwordWalk":speed==0?"Idle":speed==1?"Walk":"Run";
  if(active!=expected)throw new System.Exception("Unexpected clip "+active+" expected "+expected);
  if(armed&&maxGrip>.012f)throw new System.Exception("Armed grip "+speed+" error "+maxGrip);
  if(sword.enabled!=armed)throw new System.Exception("Weapon visibility");
  if(positions.Max(p=>Vector3.Distance(p,positions[0]))<.001f)throw new System.Exception("Static character "+active);
  records.Add(new{armed,speed,clip=active,maxGrip});
 }
 visual.PlaySwordSlash(1.2f,Vector2.up);float attackGrip=0;
 for(int k=0;k<50;k++){visual.UpdateMotion(Vector2.right,0,false,false,1,.02f);animator.Update(.02f);if(k>5)attackGrip=Mathf.Max(attackGrip,Vector3.Distance(left.position,right.position-right.up*.15f));}
 if(attackGrip>.012f)throw new System.Exception("Attack grip "+attackGrip);
 if(!animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="SwordSlash"&&c.weight>.5f))throw new System.Exception("Attack state");
 visual.CancelAttack();visual.SetSwordEquipped(false);
 for(int k=0;k<20;k++){visual.UpdateMotion(Vector2.up,1,false,true,1,.02f);animator.Update(.02f);}
 if(sword.enabled)throw new System.Exception("Unequip visibility");
 var mats=root.GetComponentsInChildren<Renderer>().Where(r=>r.name!="Hero_SwordProxy").SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
 if(mats.Length!=2||mats.Any(m=>m==null||m.GetTexture("_BaseMap")==null))throw new System.Exception("Character atlas missing");
 var report=new{playerRigUpdated=true,characterMeshes=root.GetComponentsInChildren<SkinnedMeshRenderer>().Length,materialCount=mats.Length,poses=records,attackGrip,attackCancel=true,unequip=true,scenePlayTest=false};
 System.IO.File.WriteAllText("Library/simple-hero-unity-check.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{typeof(ChibiPlayerVisual).GetField("view",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(visual,null);UnityEngine.Object.DestroyImmediate(root);}
