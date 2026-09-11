var folder="Assets/ChibiSurvivor/Player/DarkSurvivor";
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/DarkSurvivor.prefab");
var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(folder+"/DarkSurvivor.controller");
var clips=controller.animationClips.Select(c=>c.name).Distinct().OrderBy(n=>n).ToArray();
if(clips.Length!=5)throw new System.Exception("Expected five source clips");
var root=new GameObject("__SwordValidation");root.hideFlags=HideFlags.HideAndDontSave;root.transform.position=new Vector3(0,-1000,0);
var visual=root.AddComponent<ChibiPlayerVisual>();
var records=new System.Collections.Generic.List<object>();
try
{
 if(!visual.Initialize(model,controller,null,1))throw new System.Exception("Initialize failed");
 var animator=root.GetComponentInChildren<Animator>();animator.Update(0);
 var sword=root.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Hero_SwordProxy");
 var bones=root.GetComponentsInChildren<Transform>();var right=bones.Single(t=>t.name=="HandSocket.R");var left=bones.Single(t=>t.name=="HandSocket.L");
 if(sword.enabled)throw new System.Exception("Unarmed sword visible");
 foreach(var speed in new[]{0f,1f,2f})
 {
  visual.SetSwordEquipped(true);
  for(int i=0;i<12;i++){visual.UpdateMotion(Vector2.up,speed,speed==2,true,1,.025f);animator.Update(.025f);}
  var gap=Vector3.Distance(left.position,right.position);
  var names=animator.GetCurrentAnimatorClipInfo(0).Where(c=>c.weight>.5f).Select(c=>c.clip.name).ToArray();
  if(!sword.enabled || Mathf.Abs(gap-.16f)>.008f)throw new System.Exception("Grip failed speed="+speed+" gap="+gap);
  if(speed==1&&!names.Contains("SwordWalk"))throw new System.Exception("Sword walk not active");
  if(speed==2&&!names.Contains("Run"))throw new System.Exception("Run fallback not active");
  records.Add(new{speed,gap,clips=names});
 }
 visual.PlaySwordSlash(1.2f,Vector2.up);animator.Update(.12f);
 var facing=root.transform.Find("Character3D").rotation;
 for(int i=0;i<12;i++){visual.UpdateMotion(Vector2.right,0,false,false,1,.025f);animator.Update(.025f);}
 if(!animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="SwordSlash"&&c.weight>.5f))throw new System.Exception("Slash did not start");
 if(Quaternion.Angle(facing,root.transform.Find("Character3D").rotation)>.01f)throw new System.Exception("Attack facing not locked");
 var attackGap=Vector3.Distance(left.position,right.position);
 if(Mathf.Abs(attackGap-.16f)>.008f)throw new System.Exception("Slash grip detached "+attackGap);
 for(int i=0;i<60;i++){visual.UpdateMotion(Vector2.up,1,false,true,1,.025f);animator.Update(.025f);}
 if(!animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="SwordWalk"&&c.weight>.5f))throw new System.Exception("Slash did not return to walking");
 visual.PlaySwordSlash(.5f,Vector2.up);visual.CancelAttack();
 for(int i=0;i<10;i++){visual.UpdateMotion(Vector2.up,0,false,true,1,.025f);animator.Update(.025f);}
 if(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="SwordSlash"&&c.weight>.5f))throw new System.Exception("Cancelled slash still playing");
 visual.SetSwordEquipped(false);visual.UpdateMotion(Vector2.up,1,false,true,1,.2f);animator.Update(.2f);
 if(sword.enabled)throw new System.Exception("Unequip did not hide sword");
 if(animator.GetLayerWeight(animator.GetLayerIndex("SwordHold"))>0)throw new System.Exception("Unequip kept sword pose");
 var weapon=UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Resources/Items/Weapon/LongSword.asset");
 if(weapon.weaponData==null||!weapon.weaponData.useTwoHandSwordAnimations)throw new System.Exception("Item not configured");
 var result=new{clips,poses=records,attackGap,slashStarted=true,attackFacingLocked=true,returnedToWalk=true,cancelled=true,unequipped=true};
 System.IO.File.WriteAllText("Library/CodexBlender/DarkSurvivorGame/SwordUnityCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 return result;
}
finally{UnityEngine.Object.DestroyImmediate(root);}
