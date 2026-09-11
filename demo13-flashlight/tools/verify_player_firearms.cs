// Edit-mode integration: real inventory equipment API and actual player model, isolated from saves/scenes.
const string game="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game";
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
void Set(object obj,string field,object value){obj.GetType().GetField(field,flags).SetValue(obj,value);}
void Call(object obj,string method){obj.GetType().GetMethod(method,flags).Invoke(obj,null);}
void Check(bool pass,string message){if(!pass)throw new System.Exception(message);}
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new GameObject("PlayerFirearmVerification");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
var garbage=new System.Collections.Generic.List<UnityEngine.Object>();var rows=new System.Collections.Generic.List<object>();
ChibiPlayerVisual visual=null;
var oldDB=ItemDatabase.Instance;
try{
 if(oldDB==null){var db=root.AddComponent<ItemDatabase>();typeof(ItemDatabase).GetProperty("Instance").SetValue(null,db);Call(db,"LoadAll");}
 var player=root.AddComponent<TopDownPlayer>();player.enabled=false;
 var equip=root.AddComponent<PlayerEquipment>();Call(equip,"Awake");
 var gun=root.AddComponent<PlayerGun>();gun.enabled=false;
 visual=root.AddComponent<ChibiPlayerVisual>();
 Check(visual.Initialize(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(game+"/SimpleHero.prefab"),UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(game+"/SimpleHero.controller"),null,1),"Model initialization");
 Set(player,"_character3D",visual);Set(player,"_gun",gun);
 var animator=root.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
 var pistol=Resources.Load<ItemData>("Items/Weapon/Pistol9");
 var rifle=UnityEngine.Object.Instantiate(pistol);garbage.Add(rifle);
 rifle.weaponData=UnityEngine.Object.Instantiate(pistol.weaponData);garbage.Add(rifle.weaponData);rifle.weaponData.firearmStance=WeaponData.FirearmStance.AssaultRifle;
 var bag=new InventoryGrid(12,12);var carried=new ItemInstance(pistol);Check(bag.TryAutoPlace(carried),"Inventory possession");
 Call(player,"UpdateWeaponVisual");Check(!visual.HasFirearmVisual,"Possession must not display gun");
 foreach(var item in new[]{pistol,rifle}){
  equip.EquipWeapon(item);var inst=new ItemInstance(item){ammoCount=7};inst.SetAttachment(WeaponPartType.Magazine,Resources.Load<ItemData>("Items/Misc/WeaponPart/Mag9x19").itemId);equip.SetSlotInstance(EquipSlot.PrimaryWeapon,inst);
  Call(player,"UpdateWeaponVisual");Check(visual.HasFirearmVisual,"Equipped gun missing");
  var fv=root.GetComponentInChildren<PlayerFirearmVisual>();
  var bones=animator.GetComponentsInChildren<Transform>();var thigh=bones.Single(x=>x.name=="Thigh.L");var hand=bones.Single(x=>x.name=="HandSocket.R");var support=bones.Single(x=>x.name=="HandSocket.L");
  var weapon=bones.Single(x=>x.name=="PlayerFirearm");
  float lowerError=0,gripError=0,supportError=0;
  foreach(var sprint in new[]{false,true,false}){
   for(int frame=0;frame<100;frame++){
    visual.SetSprintWeaponStowed(sprint);visual.UpdateMotion(new Vector2(.6f,.8f),sprint?5:2,sprint,true,1,.02f);animator.Update(.02f);
    var p=animator.transform.position;var q=animator.transform.rotation;var leg=thigh.localRotation;
    fv.Tick(.02f);
    lowerError=Mathf.Max(lowerError,Quaternion.Angle(leg,thigh.localRotation));
    Check(Vector3.Distance(p,animator.transform.position)<.00001f&&Quaternion.Angle(q,animator.transform.rotation)<.001f,"Root/facing changed");
    Check(weapon.lossyScale.x>.99f&&weapon.lossyScale.x<1.01f,"Weapon scale");
    if(!sprint&&frame>65){gripError=Mathf.Max(gripError,Vector3.Distance(weapon.TransformPoint(new Vector3(0,-.035f,0)),hand.position));if(item==rifle)supportError=Mathf.Max(supportError,Vector3.Distance(weapon.TransformPoint(new Vector3(0,-.04f,.155f)),support.position));}
   }
   Check(sprint?fv.DrawAmount==0:fv.DrawAmount==1,"Sprint state");
   Check(weapon.parent.name==(sprint?"PlayerFirearmStow":"PlayerFirearmGrip"),"Weapon attachment");
   Check(equip.GetSlotInstance(EquipSlot.PrimaryWeapon)==inst&&inst.ammoCount==7,"Sprint mutated inventory/ammo");
  }
  Check(lowerError<.1f&&gripError<.001f&&supportError<.003f,"Grip or locomotion corruption");
  visual.SetSprintWeaponStowed(false);visual.UpdateMotion(Vector2.up,0,false,true,1,.02f);fv.Tick(30);
  Check(fv.DrawAmount==1&&weapon.parent.name=="PlayerFirearmGrip","Idle must remain armed without aim input");
  visual.UpdateMotion(Vector2.up,5,true,true,1,.02f);fv.Tick(2);
  Check(fv.DrawAmount==1,"Dodge/run animation alone must not stow");
  visual.SetSprintWeaponStowed(true);visual.UpdateMotion(Vector2.up,5,true,true,1,.02f);fv.Tick(.4f);
  visual.SetSprintWeaponStowed(false);visual.UpdateMotion(Vector2.up,2,false,true,1,.02f);fv.Tick(1);
  Check(fv.DrawAmount==1,"Interrupted stow must reverse");
  visual.SetSprintWeaponStowed(true);visual.UpdateMotion(Vector2.up,5,true,true,1,.02f);fv.Tick(2);
  Set(gun,"_nextShotAt",0f);Check(gun.HasMagazine,"Magazine ID fixture");
  Check(gun.TryFire()&&inst.ammoCount==6&&fv.DrawAmount==1,"Actual shot from sprint");
  var projectile=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Single(x=>Vector3.Distance(x.transform.position,fv.Muzzle.position)<.001f);garbage.Add(projectile.gameObject);
  Check(!player.IsSprinting,"Fire must stop sprint");
  visual.UpdateMotion(Vector2.up,0,false,true,1,.02f);fv.Tick(.1f);
  rows.Add(new{stance=item.weaponData.firearmStance.ToString(),lowerError,gripError,supportError,ammo=inst.ammoCount});
  equip.Unequip(EquipSlot.PrimaryWeapon);Call(player,"UpdateWeaponVisual");Check(!visual.HasFirearmVisual&&!root.GetComponentsInChildren<Transform>().Any(x=>x.name=="PlayerFirearm"),"Unequip must remove all gun props");
 }
 equip.EquipWeapon(pistol);Call(player,"UpdateWeaponVisual");equip.EquipWeapon(rifle);Call(player,"UpdateWeaponVisual");
 Check(root.GetComponentsInChildren<Transform>().Count(x=>x.name=="PlayerFirearm")==1,"Gun swap duplicate");
 equip.EquipWeapon(Resources.Load<ItemData>("Items/Weapon/Bat"));Call(player,"UpdateWeaponVisual");Check(!visual.HasFirearmVisual,"Gun-to-melee swap retained gun");
 foreach(bool bat in new[]{true,false}){
  visual.SetMeleeEquipped(true,bat);visual.SetSprintWeaponStowed(true);visual.UpdateMotion(Vector2.up,5,true,true,1,.02f);animator.Update(.2f);
  var name=bat?"Hero_Bat":"Hero_SwordProxy";var rs=root.GetComponentsInChildren<Renderer>();
  Check(!rs.Single(x=>x.name==name).enabled&&rs.Single(x=>x.name==name+"_Stowed").enabled,"Melee sprint stow");
  Check(animator.GetLayerWeight(animator.GetLayerIndex(bat?"BatHold":"SwordHold"))==0,"Sprint hold layer");
  visual.SetSprintWeaponStowed(false);visual.UpdateMotion(Vector2.up,0,false,true,1,.02f);animator.Update(.2f);
  Check(rs.Single(x=>x.name==name).enabled&&!rs.Single(x=>x.name==name+"_Stowed").enabled,"Melee idle draw");
  visual.SetMeleeEquipped(false,bat);Check(!rs.Single(x=>x.name==name).enabled&&!rs.Single(x=>x.name==name+"_Stowed").enabled,"Melee unequip");
 }
 var result=new{passed=true,inventoryPossessionHidden=true,equipmentAndAmmoPreserved=true,sprintOnlyStow=true,actualProjectileFromMuzzle=true,meleeStow=true,rows,liveInputPlaytest=false};
 System.IO.File.WriteAllText("Library/player-firearms-verify.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
}finally{typeof(ItemDatabase).GetProperty("Instance").SetValue(null,oldDB);if(visual!=null)Set(visual,"view",null);UnityEngine.Object.DestroyImmediate(root);foreach(var o in garbage)if(o!=null)UnityEngine.Object.DestroyImmediate(o);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
