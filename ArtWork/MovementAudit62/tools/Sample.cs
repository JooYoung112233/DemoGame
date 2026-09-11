using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

public static class MovementSamples
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/MovementAudit62"));
    public static string Run()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<TopDownPlayer>();
        if(p==null || !Application.isPlaying)return "No playing player: start a gameplay scene before sampling. Previous snapshot preserved.";
        var so=new SerializedObject(p);
        var source=so.FindProperty("character3DPrefab").objectReferenceValue as GameObject;
        var controller=so.FindProperty("character3DController").objectReferenceValue as RuntimeAnimatorController;
        var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
        var rows=new List<object>();
        try{
            root=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);root.transform.localScale=Vector3.one*so.FindProperty("character3DScale").floatValue;
            foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
            var feet=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Foot.L"||t.name=="Foot.R").ToArray();
            if(feet.Length!=2)throw new Exception("Expected two foot bones");
            var clips=controller.animationClips.Distinct().Where(c=>c.name=="Walk"||c.name=="Run"||c.name=="SwordWalk"||c.name=="BatWalk").ToArray();
            foreach(var clip in clips){
                const int count=240;var tracks=feet.Select(f=>new Vector3[count+1]).ToArray();float dt=clip.length/count;
                for(int i=0;i<=count;i++){clip.SampleAnimation(root,i*dt);for(int f=0;f<feet.Length;f++)tracks[f][i]=feet[f].position-root.transform.position;}
                var footRows=new List<object>();
                for(int f=0;f<feet.Length;f++){
                    var track=tracks[f];float minY=track.Min(v=>v.y),maxY=track.Max(v=>v.y);float threshold=minY+Mathf.Max(.01f,(maxY-minY)*.15f);
                    var contact=new List<float>();
                    for(int i=1;i<=count;i++){
                        float backward=-(track[i].z-track[i-1].z)/dt;
                        if(track[i].y<=threshold&&track[i-1].y<=threshold&&backward>.01f)contact.Add(backward);
                    }
                    contact.Sort();
                    footRows.Add(new {foot=feet[f].name,minY,maxY,contactThreshold=threshold,zExcursion=track.Max(v=>v.z)-track.Min(v=>v.z),contactSamples=contact.Count,contactFraction=(float)contact.Count/count,meanSpeed=contact.Count>0?contact.Average():0,medianSpeed=contact.Count>0?contact[contact.Count/2]:0,minSpeed=contact.Count>0?contact[0]:0,maxSpeed=contact.Count>0?contact[contact.Count-1]:0});
                }
                rows.Add(new{clip=clip.name,clip.length,clip.frameRate,samples=count,feet=footRows});
            }
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
        var json=JsonConvert.SerializeObject(new{method="240 samples/cycle, backward foot-bone velocity during lowest 15% of lift (minimum 1cm tolerance); in-place clips, model gameplay scale",rows},Formatting.Indented);
        Directory.CreateDirectory(Root);File.WriteAllText(Path.Combine(Root,"FootSamples.json"),json);return json;
    }
}
