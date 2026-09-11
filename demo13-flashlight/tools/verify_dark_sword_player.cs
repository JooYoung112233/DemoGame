var player=TopDownPlayer.Instance;
if(player==null||player.CurrentState!=TopDownPlayer.CombatState.Idle)throw new System.Exception("Player must be idle for equipment hookup verification");
var equipment=player.GetComponent<PlayerEquipment>();
var priorItem=equipment.EquippedWeapon;var priorInstance=equipment.GetSlotInstance(EquipSlot.PrimaryWeapon);
var longsword=UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Resources/Items/Weapon/LongSword.asset");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var fields=new[]{"_state","_attackStateTimer","_comboStep","_comboBuffered","_lightCooldownTimer","_stamina"};
var saved=fields.ToDictionary(n=>n,n=>typeof(TopDownPlayer).GetField(n,flags).GetValue(player));
var visual=player.GetComponent<ChibiPlayerVisual>();var animator=player.GetComponentInChildren<Animator>();
try
{
 if(priorItem!=longsword)equipment.EquipWeapon(longsword);
 typeof(TopDownPlayer).GetMethod("UpdateWeaponVisual",flags).Invoke(player,null);
 if(!player.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Hero_SwordProxy").enabled)throw new System.Exception("Equipping long sword did not show proxy");
 typeof(TopDownPlayer).GetMethod("StartLightCombo",flags).Invoke(player,null);
 animator.Update(.08f);
 if(player.CurrentState!=TopDownPlayer.CombatState.LightAttack)throw new System.Exception("Attack command failed");
 if(!animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="SwordSlash"&&c.weight>.5f))throw new System.Exception("Attack command did not play slash");
 var result=new{equipmentToPose=true,playerAttackToSlash=true,duration=player.GetComponent<AttackPerformer>().Current.Duration,clipRate=animator.GetFloat("SlashSpeed")};
 System.IO.File.WriteAllText("Library/CodexBlender/DarkSurvivorGame/PlayerSwordCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 return result;
}
finally
{
 player.GetComponent<AttackPerformer>().Cancel();visual.CancelAttack();
 if(priorItem!=longsword)
 {
  equipment.Unequip(EquipSlot.PrimaryWeapon);
  if(priorItem!=null){equipment.EquipWeapon(priorItem);if(priorInstance!=null)equipment.SetSlotInstance(EquipSlot.PrimaryWeapon,priorInstance);}
 }
 foreach(var pair in saved)typeof(TopDownPlayer).GetField(pair.Key,flags).SetValue(player,pair.Value);
 typeof(TopDownPlayer).GetMethod("UpdateWeaponVisual",flags).Invoke(player,null);
}
