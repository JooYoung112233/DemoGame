using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
public static class LampClearanceValidate
{
    public static string Run(){SceneTransitionManager.Instance.StartCoroutine(Test());return "Testing animated lamp mount against baked head/cap bounds";}
    static IEnumerator Test()
    {
        var p=TopDownPlayer.Instance;var eq=p.GetComponent<PlayerEquipment>();var equips=eq.GetSaveData();var states=eq.GetWeaponStates();
        bool virt=GameInput.Virtual,suppress=SaveManager.SuppressWrites;GameInput.Virtual=true;GameInput.VSetMove(Vector2.zero);SaveManager.SuppressWrites=true;
        var lamp=p.GetComponentInChildren<WornLamp>();var light=lamp.GetComponent<Light>();var cycle=Object.FindAnyObjectByType<DayNightCycle>();bool night=cycle!=null&&cycle.IsNight;
        var heads=p.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name=="Study_Head"||r.name=="Study_Cap"||r.name=="Study_CapBrim").ToArray();
        var chest=p.GetComponentsInChildren<Transform>().Single(t=>t.name=="Chest");var mesh=new Mesh();
        var checks=new Dictionary<string,bool>();var measurements=new List<object>();var origin=p.transform.position;
        try
        {
            var g=new InventoryGrid(4,1);var bat=new ItemInstance(ItemDatabase.Get("bat"));g.TryAutoPlace(bat);eq.TryEquipWeaponFromGrid(bat,g);
            lamp.ForcePhase(true);yield return new WaitForSeconds(.3f);
            bool Clear(){foreach(var r in heads){r.BakeMesh(mesh,true);if(mesh.bounds.Contains(r.transform.InverseTransformPoint(lamp.transform.position)))return false;}return true;}
            float Error()=>Vector3.Distance(lamp.transform.position,chest.position+chest.rotation*new Vector3(.12f,-.04f,.22f));
            checks["night_lamp_remains_on"]=light.enabled&&light.intensity>0;
            checks["idle_clear_of_head"]=Clear();checks["idle_mount_follows_chest"]=Error()<.005f;
            foreach(int variant in new[]{0,1,2})
            {
                if(variant==0)typeof(TopDownPlayer).GetMethod("StartLightCombo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);
                else{typeof(TopDownPlayer).GetField("_chargeTimer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,variant==1?0f:100f);typeof(TopDownPlayer).GetMethod("DoHeavyAttack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);}
                int frames=0,overlaps=0;float maxError=0;float deadline=Time.realtimeSinceStartup+3;bool captured=false;
                while(p.IsAttacking&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();frames++;if(!Clear())overlaps++;maxError=Mathf.Max(maxError,Error());
                    if(!captured&&p.GetComponent<AttackPerformer>().NormalizedTime>.4f){ScreenCapture.CaptureScreenshot(Path.GetFullPath($"../ArtWork/LampClearance/After-{variant}.png"));captured=true;}
                    yield return null;
                }
                checks[$"attack_{variant}_head_clear"]=frames>0&&overlaps==0;
                checks[$"attack_{variant}_mount_tracks"]=maxError<.005f;
                measurements.Add(new{variant,frames,overlaps,maxError});yield return new WaitForSeconds(.2f);
            }
            GameInput.VSetMove(new Vector2(.4f,0));yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();
            checks["walking_head_clear"]=Clear();checks["walking_mount_tracks"]=Error()<.005f;GameInput.VSetMove(Vector2.zero);
            File.WriteAllText(Path.GetFullPath("../ArtWork/LampClearance/Validation.json"),JsonConvert.SerializeObject(new{pass=checks.Values.All(x=>x),checks,measurements},Formatting.Indented));
        }
        finally{GameInput.VSetMove(Vector2.zero);eq.ResetForNewGame();eq.LoadSaveData(equips);eq.RestoreWeaponStates(states);SpawnPoint.PlacePlayer(p.gameObject,origin);lamp.ForcePhase(night);Object.Destroy(mesh);GameInput.Virtual=virt;SaveManager.SuppressWrites=suppress;}
    }
}
