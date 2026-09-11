using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;

public static class AtmosphereValidate
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
        var phases=UnityEngine.Object.FindObjectsByType<PropLight3D>();
        if(phases.Length!=7)throw new Exception("Expected seven phase lights");
        bool prior=cycle.IsNight;var time=RegionTimeManager.Instance;var region=time==null?null:time.GetRegion(time.ActiveRegionId);float elapsed=region==null?0:region.elapsed;
        bool Match(bool night)=>phases.All(p=>Mathf.Approximately(p.GetComponent<Light>().intensity,night?p.nightIntensity:p.dayIntensity));
        bool day=false,night=false,reenabled=false,spawned=false,restored=false;
        GameObject clone=null;
        try{
            cycle.SetNight(false);day=Match(false);
            cycle.SetNight(true);night=Match(true);
            var p=phases[0];p.enabled=false;p.GetComponent<Light>().intensity=0;p.enabled=true;reenabled=Match(true);
            clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/TownFinish62/Prefabs/Pawnshop_Forecourt62.prefab"));
            var added=clone.GetComponentInChildren<PropLight3D>();spawned=added!=null&&Mathf.Approximately(added.GetComponent<Light>().intensity,4.8f);
            cycle.SetNight(false);restored=Match(false);
        }finally{
            if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);
            cycle.SetNight(prior);if(region!=null)region.elapsed=elapsed;
        }
        var cam=Camera.main;var cd=cam.GetUniversalAdditionalCameraData();var urp=UniversalRenderPipeline.asset;
        var vols=UnityEngine.Object.FindObjectsByType<Volume>();
        bool volume=vols.Any(v=>v.enabled&&v.isGlobal&&v.weight==1&&v.profile!=null&&(cd.volumeLayerMask.value&(1<<v.gameObject.layer))!=0&&v.profile.TryGet<ColorAdjustments>(out var c)&&c.active&&c.contrast.overrideState&&Mathf.Approximately(c.contrast.value,14));
        int missing=UnityEngine.Object.FindObjectsByType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var shader=Shader.Find("BRB/GameLit");var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();
        int leaks=UnityEngine.Object.FindObjectsByType<Camera>().Count(c=>c.name=="AtmosphereReviewCamera");
        bool pass=day&&night&&reenabled&&spawned&&restored&&volume&&urp.supportsHDR&&cam.allowHDR&&cd.renderPostProcessing&&missing==0&&errors.Length==0&&leaks==0;
        var result=new {pass,phaseLights=phases.Length,day,night,reenabled,spawnedDuringNight=spawned,returnedToDay=restored,volume,hdr=urp.supportsHDR,cameraHDR=cam.allowHDR,post=cd.renderPostProcessing,cameraAngle=cam.transform.eulerAngles.ToString(),missingScripts=missing,shaderErrors=errors.Length,temporaryCameraLeaks=leaks};
        string json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/Atmosphere62/Validation.json")),json);
        if(!pass)throw new Exception(json);return json;
    }
}
