using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
public static class ArmoryProbe
{
    static string Root=>Path.GetFullPath("../ArtWork/ArmoryInteraction62");
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Place(Vector3 p){var player=TopDownPlayer.Instance;var rb=player.GetComponent<Rigidbody>();player.transform.position=p;rb.position=p;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();CameraFollow.Instance.SnapToTarget();}
    static void Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Hidden).Invoke(o,args);
    public static string Run(){TopDownPlayer.Instance.StartCoroutine(Test());return "Testing wall occlusion, 1.8m range, weapon ownership, equipment, storage and save round-trip";}
    static IEnumerator Test()
    {
        var p=TopDownPlayer.Instance;var eq=p.GetComponent<PlayerEquipment>();var inv=p.GetComponent<PlayerInventory>();var interaction=p.GetComponent<InteractionSystem>();
        var origin=p.transform.position;var equips=eq.GetSaveData();var weaponStates=eq.GetWeaponStates();
        var grids=new[]{inv.Grid,inv.PocketsGrid,inv.SecureGrid}.Where(g=>g!=null).ToArray();var backups=grids.Select(g=>g.GetSaveData()).ToArray();
        var stash=MainStash.Ensure();var savedStash=stash.GetSaveData();var quick=QuickSlotBar.Instance.GetSlotIds();
        bool suppress=SaveManager.SuppressWrites,virtualBefore=GameInput.Virtual;SaveManager.SuppressWrites=true;GameInput.Virtual=true;
        var checks=new Dictionary<string,bool>();var detail=new List<object>(); GameObject wall=null;
        try
        {
            UIManager.Instance.CloseAll();GameInput.VSetMove(Vector2.zero);
            var npc=InteractableObject.All.Single(n=>n.name=="전당포 주인");
            Place(new Vector3(34,0,38.55f));yield return new WaitForSeconds(.4f);
            InteractableObject Target()=> (InteractableObject)typeof(InteractionSystem).GetField("currentTarget",Hidden).GetValue(interaction);
            checks["near_npc_target"]=Target()==npc;
            checks["no_colour_highlight"]=!(bool)typeof(InteractableObject).GetField("isHighlighted",Hidden).GetValue(npc);
            ScreenCapture.CaptureScreenshot(Path.Combine(Root,"NearNPC.png"));yield return new WaitForSeconds(.15f);
            Place(new Vector3(34,0,38.05f));yield return new WaitForSeconds(.25f);checks["beyond_1_8m_hidden"]=Target()!=npc;
            Place(new Vector3(34,0,38.55f));yield return new WaitForSeconds(.2f);
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA_InteractionWall";wall.transform.position=new Vector3(34,1,39.35f);wall.transform.localScale=new Vector3(1.6f,2,.15f);Physics.SyncTransforms();
            yield return new WaitForSeconds(.2f);checks["solid_wall_blocks_target"]=Target()!=npc&&!interaction.HasAccess(npc.transform);
            GameInput.VPressKey(KeyCode.E);yield return null;GameInput.VEndFrame();GameInput.VReleaseKey(KeyCode.E);yield return new WaitForSeconds(.2f);
            checks["wall_blocks_E"]=!UIManager.Instance.IsAnyUIOpen();UnityEngine.Object.Destroy(wall);wall=null;
            Place(new Vector3(34,0,36.5f));yield return new WaitForSeconds(.3f);checks["roofed_interior_hidden"]=!interaction.HasAccess(npc.transform);
            Place(new Vector3(34,0,38.55f));yield return new WaitForSeconds(.3f);
            GameInput.VPressKey(KeyCode.E);yield return null;GameInput.VEndFrame();GameInput.VReleaseKey(KeyCode.E);yield return new WaitForSeconds(.3f);
            checks["near_E_opens_dialogue"]=UIManager.Instance.IsAnyUIOpen();UIManager.Instance.CloseAll();
            Place(new Vector3(34,0,34));yield return new WaitForSeconds(.25f);
            eq.ResetForNewGame();foreach(var g in grids)g.Clear();stash.GetGrid().Clear();
            var pistol=new ItemInstance(ItemDatabase.Get("pistol9"));pistol.SetAttachment(WeaponPartType.Magazine,"mag_9x19");pistol.ammoCount=11;pistol.ammoItemId="ammo_9x19";
            var bat=new ItemInstance(ItemDatabase.Get("bat"));var sg=stash.GetGrid();sg.TryAutoPlace(pistol);sg.TryAutoPlace(bat);
            UIManager.Instance.ShowCharacterPanelWithStash();yield return new WaitForSeconds(.2f);
            var ui=UnityEngine.Object.FindAnyObjectByType<CharacterPanelUI>();
            Invoke(ui,"EquipFromGrid",pistol,sg,EquipSlot.PrimaryWeapon);
            checks["UI_pistol_equip"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==pistol&&sg.FindItem("pistol9")==null;
            Invoke(ui,"EquipFromGrid",bat,sg,EquipSlot.SecondaryWeapon);
            checks["UI_secondary_slot_honoured"]=eq.GetSlotInstance(EquipSlot.SecondaryWeapon)==bat&&eq.EquippedWeapon==pistol.data;
            QuickSlotBar.Instance.AssignToSlot(0,"pistol9");QuickSlotBar.Instance.AssignToSlot(1,"bat");
            Invoke(QuickSlotBar.Instance,"UseSlot",1);yield return new WaitForSeconds(.25f);
            checks["quick_draw_equipped_bat"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==bat&&eq.GetSlotInstance(EquipSlot.SecondaryWeapon)==pistol;
            UIManager.Instance.CloseAll();yield return new WaitForSeconds(.35f);
            checks["bat_model_visible"]=p.GetComponentsInChildren<Renderer>().Any(r=>r.name=="Hero_Bat"&&r.enabled);
            ScreenCapture.CaptureScreenshot(Path.Combine(Root,"BatEquipped.png"));yield return new WaitForSeconds(.15f);
            Invoke(QuickSlotBar.Instance,"UseSlot",0);yield return new WaitForSeconds(.4f);
            checks["quick_draw_equipped_pistol"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==pistol&&pistol.ammoCount==11;
            checks["pistol_model_visible"]=p.GetComponentsInChildren<PlayerFirearmVisual>().Any(v=>v.Equipped);
            ScreenCapture.CaptureScreenshot(Path.Combine(Root,"PistolEquipped.png"));yield return new WaitForSeconds(.15f);
            checks["fire_consumes_one_round"]=p.GetComponent<PlayerGun>().TryFire()&&pistol.ammoCount==10;
            checks["store_equipped_pistol"]=eq.TryStoreWeapon(EquipSlot.PrimaryWeapon,sg)&&eq.EquippedWeapon==null&&sg.FindItem("pistol9").item==pistol;
            sg.AutoSort();checks["sort_preserves_ammo"]=sg.FindItem("pistol9").item.ammoCount==10;
            checks["retrieve_then_quick_draw"]=eq.TryEquipWeaponFromGrid(pistol,sg)&&eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==pistol;
            var snapshot=JsonConvert.DeserializeObject<GameSaveData>(SaveManager.Instance.ToJson(SaveManager.Instance.BuildSaveData()));
            eq.ResetForNewGame();eq.LoadSaveData(snapshot.equippedWeapon);eq.RestoreWeaponStates(snapshot.equippedWeaponStates);
            var restored=eq.GetSlotInstance(EquipSlot.PrimaryWeapon);
            checks["save_roundtrip_all_weapon_states"]=restored!=null&&restored.ammoCount==10&&restored.ammoItemId=="ammo_9x19"&&restored.LoadedMagazineData.itemId=="mag_9x19"&&eq.GetSlot(EquipSlot.SecondaryWeapon)==bat.data;
            var gun=p.GetComponent<PlayerGun>();var mag=new ItemInstance(ItemDatabase.Get("mag_9x19"));mag.ammoCount=15;mag.ammoItemId="ammo_9x19";inv.PocketsGrid.TryAutoPlace(mag);
            bool began=gun.TryReload();eq.TryDrawWeapon(EquipSlot.SecondaryWeapon);yield return new WaitForSeconds(2.5f);
            checks["switch_during_reload_cancels_without_loss"]=began&&eq.EquippedWeapon==bat.data&&inv.PocketsGrid.FindItem("mag_9x19").item==mag&&restored.ammoCount==10&&!eq.GetSlotInstance(EquipSlot.PrimaryWeapon).HasAnyAttachment;
            eq.TryDrawWeapon(EquipSlot.SecondaryWeapon);for(int i=0;i<3;i++)inv.PocketsGrid.TryAutoPlace(new ItemInstance(bat.data));
            began=gun.TryReload();yield return new WaitForSeconds(2.5f);
            checks["full_pockets_reload_returns_old_magazine"]=began&&restored.ammoCount==15&&inv.PocketsGrid.FindItem("mag_9x19").item.ammoCount==10&&inv.PocketsGrid.ItemCount==4;
            inv.PocketsGrid.Clear();eq.TryStoreWeapon(EquipSlot.PrimaryWeapon,sg);
            UIManager.Instance.ShowCharacterPanelWithStash();yield return new WaitForSeconds(.25f);
            var gridRT=(RectTransform)typeof(CharacterPanelUI).GetField("containerGridRoot",Hidden).GetValue(ui);
            GameInput.VSetMousePos(RectTransformUtility.WorldToScreenPoint(null,gridRT.TransformPoint(new Vector3(30,-30,0))));
            Invoke(ui,"TryQuickTransfer");yield return new WaitForSeconds(.15f);
            checks["Ctrl_retrieves_weapon_without_equipping"]=inv.PocketsGrid.FindItem("pistol9")!=null&&eq.EquippedWeapon==null&&sg.FindItem("pistol9")==null;
            var pocketRT=(RectTransform)typeof(CharacterPanelUI).GetField("pocketsGridRoot",Hidden).GetValue(ui);
            GameInput.VSetMousePos(RectTransformUtility.WorldToScreenPoint(null,pocketRT.TransformPoint(new Vector3(30,-30,0))));
            Invoke(ui,"TryQuickTransfer");yield return new WaitForSeconds(.15f);
            checks["Ctrl_stores_weapon_without_equipping"]=sg.FindItem("pistol9")!=null&&eq.EquippedWeapon==null&&inv.PocketsGrid.FindItem("pistol9")==null;
            eq.TryEquipWeaponFromGrid(restored,sg);
            var full=new InventoryGrid(1,1);full.TryAutoPlace(new ItemInstance(bat.data));checks["full_stash_no_loss"]=!eq.TryStoreWeapon(EquipSlot.PrimaryWeapon,full)&&eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==restored;
            checks["no_weapon_alias_in_inventory"]=!grids.Any(g=>g.GetAll().Any(i=>i.item==restored));
            int count=sg.ItemCount;checks["starter_granted_only_once"]=!stash.GrantStarterArmory()&&sg.ItemCount==count;
            UIManager.Instance.ShowCharacterPanelWithStash();yield return new WaitForSeconds(.3f);
            checks["control_hint_present"]=ui.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.name=="ControlHint"&&!string.IsNullOrEmpty(t.text));
            ScreenCapture.CaptureScreenshot(Path.Combine(Root,"InventoryStash.png"));yield return new WaitForSeconds(.2f);
            File.WriteAllText(Path.Combine(Root,"Validation.json"),JsonConvert.SerializeObject(new{pass=checks.Values.All(v=>v),checks},Formatting.Indented));
        }
        finally
        {
            if(wall!=null)UnityEngine.Object.Destroy(wall);UIManager.Instance.CloseAll();
            eq.ResetForNewGame();eq.LoadSaveData(equips);eq.RestoreWeaponStates(weaponStates);
            for(int i=0;i<grids.Length;i++)grids[i].LoadSaveData(backups[i]);stash.LoadSaveData(savedStash);QuickSlotBar.Instance.LoadSlotIds(quick);
            GameInput.VSetMove(Vector2.zero);GameInput.Virtual=virtualBefore;Place(origin);SaveManager.SuppressWrites=suppress;
        }
    }
}
