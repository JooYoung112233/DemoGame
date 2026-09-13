using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
public static class BatComfortProbe
{
    const BindingFlags H=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,H).GetValue(o);
    static void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,H).Invoke(o,args);
    static string Root=>Path.GetFullPath("../ArtWork/BatComfort");
    public static string Run(){TopDownPlayer.Instance.StartCoroutine(Test());return "Validating double-click and live bat attacks; save writes suppressed and runtime state restored";}
    static IEnumerator Test()
    {
        var p=TopDownPlayer.Instance;var eq=p.GetComponent<PlayerEquipment>();var inv=p.GetComponent<PlayerInventory>();
        var grids=new[]{inv.Grid,inv.PocketsGrid,inv.SecureGrid}.Where(g=>g!=null).ToArray();
        var savedGrids=grids.Select(g=>g.GetSaveData()).ToArray();var savedEq=eq.GetSaveData();var savedWeapons=eq.GetWeaponStates();
        var bar=QuickSlotBar.Instance;var savedBar=bar.GetSlotIds();var origin=p.transform.position;
        bool suppress=SaveManager.SuppressWrites,virt=GameInput.Virtual;float timeScale=Time.timeScale;
        var checks=new Dictionary<string,bool>();var measured=new List<object>();CharacterPanelUI ui=null;
        SaveManager.SuppressWrites=true;GameInput.Virtual=true;
        try
        {
            UIManager.Instance.ShowCharacterPanelWithStash();yield return null;ui=UnityEngine.Object.FindAnyObjectByType<CharacterPanelUI>();
            var fixture=new InventoryGrid(8,1);var bat=new ItemInstance(ItemDatabase.Get("bat"));var pistol=new ItemInstance(ItemDatabase.Get("pistol9"));
            fixture.TryAutoPlace(bat);fixture.TryAutoPlace(pistol);eq.TryEquipWeaponFromGrid(bat,fixture);eq.TryEquipWeaponFromGrid(pistol,fixture,EquipSlot.SecondaryWeapon);
            foreach(var g in grids)g.Clear();
            var buttons=(Dictionary<EquipSlot,Image>)Get(ui,"equipSlotBgs");
            void Click(EquipSlot slot)=>buttons[slot].GetComponent<Button>().onClick.Invoke();
            void Reset()=>Call(ui,"ResetEquipClick");
            void Reequip(){var source=grids.FirstOrDefault(g=>g.GetAll().Any(x=>x.item==bat));if(source!=null)eq.TryEquipWeaponFromGrid(bat,source);Reset();}
            Time.timeScale=0;Reset();Click(EquipSlot.PrimaryWeapon);
            checks["single_click_keeps_equipped"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==bat&&Get(ui,"selectedItem")==bat;
            yield return new WaitForSecondsRealtime(.12f);Click(EquipSlot.PrimaryWeapon);
            checks["double_click_unequips_while_paused"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==null&&grids.Any(g=>g.GetAll().Any(x=>x.item==bat));
            Reequip();Click(EquipSlot.PrimaryWeapon);yield return new WaitForSecondsRealtime(.36f);Click(EquipSlot.PrimaryWeapon);
            checks["slow_clicks_do_not_unequip"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==bat;
            Reset();Click(EquipSlot.PrimaryWeapon);Click(EquipSlot.SecondaryWeapon);
            checks["different_slots_not_double_click"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==bat&&eq.GetSlotInstance(EquipSlot.SecondaryWeapon)==pistol;
            Reset();Click(EquipSlot.PrimaryWeapon);ui.Hide();ui.Show();Click(EquipSlot.PrimaryWeapon);
            checks["reopen_resets_click_pair"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==bat;
            Reset();GameInput.VPressKey(KeyCode.LeftControl);Click(EquipSlot.PrimaryWeapon);GameInput.VReleaseKey(KeyCode.LeftControl);
            checks["ctrl_click_still_unequips"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==null;Reequip();
            foreach(var g in grids)for(int i=0;i<200;i++)if(!g.TryAutoPlace(new ItemInstance(bat.data)))break;
            Reset();Click(EquipSlot.PrimaryWeapon);Click(EquipSlot.PrimaryWeapon);
            checks["full_inventory_double_click_preserves_weapon"]=eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==bat&&!grids.Any(g=>g.GetAll().Any(x=>x.item==bat));
            foreach(var g in grids)g.Clear();Reset();Click(EquipSlot.PrimaryWeapon);
            GameInput.VEndFrame();GameInput.VPressKey(KeyCode.Alpha6);Call(ui,"HandleDragAndDrop");yield return null;GameInput.VReleaseKey(KeyCode.Alpha6);GameInput.VEndFrame();
            checks["single_click_number_registration_kept"]=bar.GetSlotIds()[5]=="bat"&&eq.GetSlotInstance(EquipSlot.PrimaryWeapon)==bat;
            checks["hint_explains_double_click"]=ui.GetComponentsInChildren<Text>().Any(t=>t.name=="ControlHint"&&t.text.Contains("더블클릭"));
            UIManager.Instance.CloseAll();Time.timeScale=1;
            var rb=p.GetComponent<Rigidbody>();p.transform.position=new Vector3(34,0,38.55f);rb.position=p.transform.position;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();CameraFollow.Instance.SnapToTarget();
            GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.3f);
            var transforms=p.GetComponentsInChildren<Transform>(true);var batT=transforms.Single(t=>t.name=="Hero_Bat");var center=batT.GetComponent<MeshFilter>().sharedMesh.bounds.center;
            float Grip(string side){var hand=transforms.Single(t=>t.name=="HandSocket."+side);var q=batT.InverseTransformPoint(hand.position);return new Vector2(q.x-center.x,q.z-center.z).magnitude;}
            checks["idle_both_grips_aligned"]=Grip("R")<.005f&&Grip("L")<.005f;
            ScreenCapture.CaptureScreenshot(Path.Combine(Root,"InGameGrip62.png"));yield return new WaitForSeconds(.12f);
            var perf=p.GetComponent<AttackPerformer>();var animator=p.GetComponentInChildren<Animator>();
            foreach(int variant in new[]{0,1,2})
            {
                if(variant==0)Call(p,"StartLightCombo");else{p.GetType().GetField("_chargeTimer",H).SetValue(p,variant==1?0f:100f);Call(p,"DoHeavyAttack");}
                var attack=perf.Current;float start=Time.time,firstHit=-1,lastHit=-1,maxGrip=0,maxPhase=0;
                while(p.IsAttacking&&Time.time-start<2f)
                {
                    yield return null;float elapsed=Time.time-start;
                    if(perf.Current!=null&&attack.windows[0].IsActiveAtFrame(perf.CurrentFrame)){if(firstHit<0)firstHit=elapsed;lastHit=elapsed;}
                    if(elapsed>.15f&&elapsed<attack.Duration-.08f){maxGrip=Mathf.Max(maxGrip,Grip("R"),Grip("L"));maxPhase=Mathf.Max(maxPhase,Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-perf.NormalizedTime));}
                }
                float duration=Time.time-start;
                checks[$"attack_{variant}_duration"]=Mathf.Abs(duration-attack.Duration)<.08f;
                checks[$"attack_{variant}_hit_window"]=firstHit>=0&&Mathf.Abs(firstHit-attack.windows[0].startFrame/(float)attack.fps)<.08f;
                checks[$"attack_{variant}_grips"]=maxGrip<.015f;
                checks[$"attack_{variant}_visual_sync"]=maxPhase<.10f;
                measured.Add(new{variant,duration,firstHit,lastHit,maxGrip,maxPhase});
                yield return new WaitForSeconds(.2f);
            }
            GameInput.VSetMove(new Vector2(.5f,0));yield return new WaitForSeconds(.35f);
            checks["walk_both_grips_aligned"]=Grip("R")<.005f&&Grip("L")<.005f;GameInput.VSetMove(Vector2.zero);
            File.WriteAllText(Path.Combine(Root,"Validation.json"),JsonConvert.SerializeObject(new{pass=checks.Values.All(v=>v),checks,measured},Formatting.Indented));
        }
        finally
        {
            Time.timeScale=timeScale;GameInput.VSetMove(Vector2.zero);p.GetComponent<AttackPerformer>().Cancel();p.GetComponent<ChibiPlayerVisual>().CancelAttack();
            p.GetType().GetField("_state",H).SetValue(p,TopDownPlayer.CombatState.Idle);
            UIManager.Instance.CloseAll();eq.ResetForNewGame();eq.LoadSaveData(savedEq);eq.RestoreWeaponStates(savedWeapons);
            for(int i=0;i<grids.Length;i++)grids[i].LoadSaveData(savedGrids[i]);bar.LoadSlotIds(savedBar);
            p.transform.position=origin;var rb=p.GetComponent<Rigidbody>();rb.position=origin;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();CameraFollow.Instance.SnapToTarget();
            GameInput.Virtual=virt;SaveManager.SuppressWrites=suppress;
        }
    }
}
