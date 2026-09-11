using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class CalmerWalkProbe
{
    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/CalmerWalk"));
    public static string State() { var p=TopDownPlayer.Instance; return JsonConvert.SerializeObject(new{playing=Application.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,player=p!=null,enabled=p!=null&&p.enabled,kinematic=p!=null&&p.GetComponent<Rigidbody>().isKinematic,interaction=p!=null&&p.GetComponent<InteractionSystem>().enabled,walk=StatDB.Instance.playerStat.moveSpeed}); }
    public static string Town() { UIManager.Instance.CloseAll(); if(TitleScreen.Instance!=null)typeof(TitleScreen).GetMethod("Hide",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(TitleScreen.Instance,null); Time.timeScale=1;SceneTransitionManager.Instance.TransitionTo("Safehouse","default");return "Town requested"; }
    public static string Hideout() { UIManager.Instance.CloseAll();SceneTransitionManager.Instance.TransitionTo("Hideout","default");return "Hideout requested"; }
    public static string Before() { TopDownPlayer.Instance.StartCoroutine(Measure("before", false)); return "8-direction baseline started"; }
    public static string After() { TopDownPlayer.Instance.StartCoroutine(Measure("after", true)); return "All movement modes started"; }
    public static string Crouch() { TopDownPlayer.Instance.StartCoroutine(Measure("crouch", true)); return "Crouch regression started"; }
    static IEnumerator TapC() { GameInput.VPressKey(KeyCode.C); yield return null; GameInput.VEndFrame(); GameInput.VReleaseKey(KeyCode.C); yield return null; GameInput.VEndFrame(); }
    static IEnumerator Measure(string file, bool allModes)
    {
        var p=TopDownPlayer.Instance; var rb=p.GetComponent<Rigidbody>();
        if(!p.enabled || rb.isKinematic) throw new Exception("Enter walkable town first");
        var origin=p.transform.position; bool oldCrouch=p.IsCrouching; float oldScale=Time.timeScale;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name="MovementProbeGround";
        ground.transform.position=new Vector3(1000,-.5f,1000); ground.transform.localScale=new Vector3(100,1,100);
        ground.GetComponent<Renderer>().enabled=false;
        var center=new Vector3(1000,.03f,1000); var rows=new List<object>();
        UIManager.Instance.CloseAll(); Time.timeScale=1; GameInput.Virtual=true;
        try
        {
            if(oldCrouch) yield return TapC();
            foreach(string mode in file=="crouch"?new[]{"crouch"}:allModes?new[]{"walk","run"}:new[]{"walk"})
            {
                if(mode=="crouch") yield return TapC();
                var inputs=new[]{Vector2.up};
                foreach(var input in inputs)
                {
                    GameInput.VSetMove(Vector2.zero); GameInput.VReleaseKey(KeyCode.LeftShift);
                    yield return new WaitForSeconds(.1f);
                    rb.position=center; p.transform.position=center; rb.linearVelocity=Vector3.zero; Physics.SyncTransforms();
                    if(mode=="run") GameInput.VPressKey(KeyCode.LeftShift);
                    GameInput.VSetMove(input); yield return new WaitForSeconds(.3f);
                    var from=rb.position; double time=Time.fixedTimeAsDouble;
                    yield return new WaitForSeconds(.5f);
                    float elapsed=(float)(Time.fixedTimeAsDouble-time), measured=Plan3D.PlanDistance(from,rb.position)/elapsed;
                    var stat=StatDB.Instance.playerStat; float expected=stat.moveSpeed*(mode=="run"?stat.sprintSpeedMultiplier:mode=="crouch"?stat.crouchSpeedMultiplier:1)*Mathf.Min(1,input.magnitude);
                    var animator=p.GetComponentInChildren<Animator>();
                    rows.Add(new{mode,input=input.ToString(),elapsed,measured,expected,pass=Mathf.Abs(measured-expected)<.025f,animatorSpeed=animator!=null?animator.speed:0,moveMagnitude=p.MoveDirection.magnitude,p.IsSprinting,p.IsCrouching});
                }
                GameInput.VSetMove(Vector2.zero);GameInput.VReleaseKey(KeyCode.LeftShift);
                if(mode=="crouch") yield return TapC();
            }
            if(oldCrouch) yield return TapC();
        }
        finally
        {
            GameInput.Virtual=false;Time.timeScale=oldScale;rb.position=origin;p.transform.position=origin;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();
            UnityEngine.Object.Destroy(ground);
            File.WriteAllText(Path.Combine(Root,file+".json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
        }
    }
}
