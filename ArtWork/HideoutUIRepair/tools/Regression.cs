using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
public static class RepairRegression
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/HideoutUIRepair"));
    public static string Keys(){if(GameInput.Virtual)throw new Exception("Virtual input already in use");TopDownPlayer.Instance.StartCoroutine(KeyChecks());return "Keyboard regression started";}
    static IEnumerator Tap(KeyCode key){GameInput.VTapKey(key);yield return null;GameInput.VEndFrame();yield return null;}
    static IEnumerator KeyChecks(){
        var rows=new List<object>();GameInput.Virtual=true;
        try{
            UIManager.Instance.CloseAll();yield return null;
            yield return Tap(KeyCode.Escape);rows.Add(new{check="ESC opens confirmation",pass=HideoutController.ExitConfirmationOpen});
            yield return Tap(KeyCode.Escape);rows.Add(new{check="ESC cancels confirmation",pass=!HideoutController.ExitConfirmationOpen});
            yield return Tap(KeyCode.Tab);rows.Add(new{check="Tab opens inventory",pass=UIManager.Instance.IsInventoryOpen});
            yield return Tap(KeyCode.Escape);rows.Add(new{check="ESC closes inventory only",pass=!UIManager.Instance.IsInventoryOpen&&!HideoutController.ExitConfirmationOpen});
        }finally{GameInput.Virtual=false;File.WriteAllText(Path.Combine(Root,"KeyboardRegression.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));}
    }
    public static string Speed(){if(GameInput.Virtual)throw new Exception("Virtual input already in use");TopDownPlayer.Instance.StartCoroutine(SpeedChecks());return "Speed regression started";}
    public static string Enter(){if(GameInput.Virtual)throw new Exception("Virtual input already in use");TopDownPlayer.Instance.StartCoroutine(EnterCheck());return "Walking to hideout entrance";}
    static IEnumerator EnterCheck(){
        GameInput.Virtual=true;UIManager.Instance.CloseAll();float start=Time.unscaledTime;
        try{
            GameInput.VPressKey(KeyCode.LeftShift);GameInput.VSetMove(TopDownPlayer.WorldToInput(Vector2.right));
            while(!HideoutController.IsActive && Time.unscaledTime-start<8f)yield return null;
        }finally{GameInput.Virtual=false;File.WriteAllText(Path.Combine(Root,"EntranceRegression.json"),JsonConvert.SerializeObject(new{pass=HideoutController.IsActive,method="GameInput walking through existing SceneDoor3D trigger",elapsed=Time.unscaledTime-start}));}
    }
    static IEnumerator SpeedChecks(){
        var p=TopDownPlayer.Instance;var origin=p.transform.position;var rb=p.GetComponent<Rigidbody>();var rows=new List<object>();bool crouched=p.IsCrouching;
        UIManager.Instance.CloseAll();Time.timeScale=1;GameInput.Virtual=true;
        try{
            if(crouched)yield return Tap(KeyCode.C);
            foreach(string mode in new[]{"walk","diagonal","sprint","crouch"}){
                GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.15f);
                if(rb!=null){rb.position=origin;if(!rb.isKinematic)rb.linearVelocity=Vector3.zero;}p.transform.position=origin;Physics.SyncTransforms();
                if(mode=="sprint")GameInput.VPressKey(KeyCode.LeftShift);
                if(mode=="crouch")yield return Tap(KeyCode.C);
                GameInput.VSetMove(mode=="diagonal"?new Vector2(-1,1):Vector2.up);
                yield return new WaitForSeconds(.3f);var start=p.transform.position;float time=Time.time;
                yield return new WaitForSeconds(1f);float elapsed=Time.time-time;float distance=Plan3D.PlanDistance(start,p.transform.position);
                float expected=mode=="sprint"?2.295f:mode=="crouch"?.225f:.45f;
                rows.Add(new{mode,elapsed,distance,measured=distance/elapsed,expected,pass=Mathf.Abs(distance/elapsed-expected)<.035f,p.IsSprinting,p.IsCrouching,p.enabled,p.CanMove,virtualInput=GameInput.Virtual,axis=GameInput.GetAxisRaw("Vertical"),uiOpen=UIManager.Instance.IsAnyUIOpen(),title=TitleScreen.IsShowing,hideout=HideoutController.IsActive});
                GameInput.VSetMove(Vector2.zero);GameInput.VReleaseKey(KeyCode.LeftShift);if(mode=="crouch")yield return Tap(KeyCode.C);
            }
            if(crouched)yield return Tap(KeyCode.C);
        }finally{GameInput.Virtual=false;p.transform.position=origin;if(rb!=null){rb.position=origin;if(!rb.isKinematic)rb.linearVelocity=Vector3.zero;}Physics.SyncTransforms();File.WriteAllText(Path.Combine(Root,"SpeedRegression.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));}
    }
}
