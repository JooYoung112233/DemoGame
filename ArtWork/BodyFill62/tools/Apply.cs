using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

public static class BodyFillApply
{
    static void Set(WornLamp lamp)
    {
        var so=new SerializedObject(lamp);
        so.FindProperty("bodyGlowIntensity").floatValue=.06f;
        so.FindProperty("bodyGlowTowardCamera").floatValue=1.1f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string Run()
    {
        const string path="Assets/Resources/PlayerRig.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{Set(root.GetComponentInChildren<WornLamp>(true));if(PrefabUtility.SaveAsPrefabAsset(root,path)==null)throw new Exception("Prefab save failed");}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var live=UnityEngine.Object.FindAnyObjectByType<WornLamp>();
        if(Application.isPlaying&&live!=null){Set(live);var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();live.ForcePhase(cycle!=null&&cycle.IsNight);}
        return "Saved PlayerRig and updated live WornLamp; Play and region clocks preserved.";
    }
    public static string Verify()
    {
        var live=UnityEngine.Object.FindAnyObjectByType<WornLamp>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PlayerRig.prefab").GetComponentInChildren<WornLamp>(true);
        var glow=live.GetComponentsInChildren<Light>().Single(l=>l.name=="BodyGlow");
        bool Valid(WornLamp lamp){var so=new SerializedObject(lamp);return Mathf.Approximately(so.FindProperty("bodyGlowIntensity").floatValue,.06f)&&Mathf.Approximately(so.FindProperty("bodyGlowTowardCamera").floatValue,1.1f);}
        var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();bool prior=cycle!=null&&cycle.IsNight;
        bool day,night;
        try{live.ForcePhase(false);day=Mathf.Approximately(glow.intensity,.06f);live.ForcePhase(true);night=Mathf.Approximately(glow.intensity,.06f);}
        finally{live.ForcePhase(prior);}
        bool position=Vector3.Distance(glow.transform.position,live.transform.position+Vector3.up*.1f+Vector3.back*1.1f)<.001f;
        int errors=ShaderUtil.GetShaderMessages(Shader.Find("BRB/GameLit")).Count(m=>m.severity.ToString()=="Error");
        var result=new {pass=Valid(live)&&Valid(prefab)&&day&&night&&position&&errors==0,liveValues=Valid(live),prefabValues=Valid(prefab),day,night,position,shaderErrors=errors,playing=Application.isPlaying};
        var json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/BodyFill62/Validation.json")),json);
        if(!result.pass)throw new Exception(json);return json;
    }
}
