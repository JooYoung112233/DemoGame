using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Newtonsoft.Json;

public static class MovementAudit
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/MovementAudit62"));
    static object State(AnimatorState s)=>new {s.name,s.speed,s.speedParameter,s.speedParameterActive,motion=s.motion==null?null:s.motion.name,tree=s.motion is BlendTree b?b.children.Select(c=>new {clip=c.motion==null?null:c.motion.name,c.threshold,c.timeScale}).ToArray():null};
    public static string Inspect()
    {
        Directory.CreateDirectory(Root);
        var p=UnityEngine.Object.FindAnyObjectByType<TopDownPlayer>();
        if(p==null)return "No live player: start a gameplay scene before inspection. Previous snapshot preserved.";
        var so=new SerializedObject(p);
        var anim=p.GetComponentInChildren<Animator>();
        if(!Application.isPlaying || anim==null || anim.runtimeAnimatorController==null)
            return "Player visual is not running/initialized. Previous snapshot preserved.";
        var controller=anim.runtimeAnimatorController as AnimatorController;
        object Fields(TopDownPlayer player){var s=new SerializedObject(player);return new {scale=s.FindProperty("character3DScale").floatValue,matchSpeed=s.FindProperty("animCadenceMatchesSpeed").boolValue,walkClipSpeed=s.FindProperty("walkClipSpeed").floatValue,runClipSpeed=s.FindProperty("runClipSpeed").floatValue,maxCadence=s.FindProperty("animCadenceMax").floatValue};}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PlayerRig.prefab").GetComponentInChildren<TopDownPlayer>(true);
        var w=p.CurrentWeapon;
        var report=new {capturedUtc=DateTime.UtcNow.ToString("O"),dodgeEnabled=GameTuning.Instance!=null && GameTuning.Instance.dodgeEnabled,playing=Application.isPlaying,timeScale=Time.timeScale,fixedDeltaTime=Time.fixedDeltaTime,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,virtualInput=GameInput.Virtual,
            playerFields=Fields(p),prefabFields=Fields(prefab),stat=StatDB.Instance.playerStat,player=new {pos=p.transform.position.ToString(),p.IsCrouching,p.IsSprinting,p.IsExhausted,p.CanMove,velocity=p.GetComponent<Rigidbody>().linearVelocity.ToString()},
            weapon=w==null?null:new {w.name,w.moveSpeedMult,w.adsMoveMult,w.isRanged},animator=new {anim.name,anim.speed,anim.applyRootMotion,controller=AssetDatabase.GetAssetPath(anim.runtimeAnimatorController),state=anim.GetCurrentAnimatorStateInfo(0).fullPathHash,clips=anim.GetCurrentAnimatorClipInfo(0).Select(c=>new {c.clip.name,c.weight})},
            layers=controller.layers.Select(l=>new {l.name,l.defaultWeight,states=l.stateMachine.states.Select(s=>State(s.state))}),
            clips=anim.runtimeAnimatorController.animationClips.Distinct().Select(c=>new {c.name,c.length,c.frameRate,c.isLooping,rootMotion=c.hasRootCurves,path=AssetDatabase.GetAssetPath(c)}),
            feet=anim.GetComponentsInChildren<Transform>().Where(t=>t.name.ToLower().Contains("foot")||t.name.ToLower().Contains("ankle")).Select(t=>new {t.name,pos=t.position.ToString()}),
            weapons=AssetDatabase.FindAssets("t:WeaponData").Select(AssetDatabase.GUIDToAssetPath).Select(path=>AssetDatabase.LoadAssetAtPath<WeaponData>(path)).Where(d=>d!=null).Select(d=>new {d.name,d.moveSpeedMult,d.adsMoveMult,d.isRanged}),
            firearmSets=AssetDatabase.FindAssets("t:PlayerFirearmSet").Select(AssetDatabase.GUIDToAssetPath).Select(path=>AssetDatabase.LoadAssetAtPath<PlayerFirearmSet>(path)).Select(s=>new{s.name,walk=s.walk.name,walkLength=s.walk.length,aim=s.aim.name,drawLength=s.draw.length})};
        string json=JsonConvert.SerializeObject(report,Formatting.Indented);File.WriteAllText(Path.Combine(Root,"Inspect.json"),json);return json;
    }
}
