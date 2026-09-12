using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;

public static class ComicLightingValidate
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/ComicLighting62"));
    public static string Capture(string label)
    {
        var main=Camera.main;var go=new GameObject("ComicValidationCamera"){hideFlags=HideFlags.HideAndDontSave};
        var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.transform.SetPositionAndRotation(main.transform.position,main.transform.rotation);
        var cd=go.AddComponent<UniversalAdditionalCameraData>();EditorUtility.CopySerialized(main.GetUniversalAdditionalCameraData(),cd);cd.renderType=CameraRenderType.Base;
        var rt=new RenderTexture(1440,960,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
        var tex=new Texture2D(1440,960,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1440,960),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Root,label+".png"),tex.EncodeToPNG());}
        finally{cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        return label;
    }
    public static string Town()
    {
        var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();bool oldNight=cycle.IsNight;
        var region=RegionTimeManager.Instance.GetRegion(RegionTimeManager.Instance.ActiveRegionId);float elapsed=region==null?0:region.elapsed;
        var flashlight=TopDownPlayer.Instance.GetComponentsInChildren<Light>(true);
        var oldOn=flashlight.Select(l=>l.enabled).ToArray();
        var lights=UnityEngine.Object.FindObjectsByType<PropLight3D>();var phases=new List<object>();bool phasePass=true;
        try{
            foreach(bool night in new[]{false,true}){
                cycle.SetNight(night);
                foreach(var p in lights){p.enabled=false;p.enabled=true;float expected=night?p.nightIntensity:p.dayIntensity;float actual=p.GetComponent<Light>().intensity;bool pass=Mathf.Approximately(expected,actual);phasePass&=pass;phases.Add(new{night,p.name,expected,actual,pass});}
            }
            foreach(var l in flashlight)l.enabled=false;Capture("Night_FlashlightOff");
            for(int i=0;i<flashlight.Length;i++)flashlight[i].enabled=oldOn[i];Capture("Night_FlashlightOn");
        }finally{cycle.SetNight(oldNight);if(region!=null)region.elapsed=elapsed;for(int i=0;i<flashlight.Length;i++)flashlight[i].enabled=oldOn[i];}
        var errors=new[]{"BRB/GameLit","BRB/GroundFadeLit"}.SelectMany(name=>ShaderUtil.GetShaderMessages(Shader.Find(name)).Select(e=>new{shader=name,e.message,e.line,severity=e.severity.ToString()})).ToArray();
        var materials=UnityEngine.Object.FindObjectsByType<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
        var missing=materials.Where(m=>m.shader==null||m.shader.name=="Hidden/InternalErrorShader").Select(m=>m.name).ToArray();
        var legacy=materials.Where(m=>m.shader.name=="Universal Render Pipeline/Lit" && AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")).Contains("WorldComic62")).Select(m=>m.name).ToArray();
        var volumes=UnityEngine.Object.FindObjectsByType<Volume>().Select(v=>new{v.name,profile=v.profile.name,components=v.profile.components.Select(c=>new{type=c.GetType().Name,data=EditorJsonUtility.ToJson(c)})}).ToArray();
        var report=new{pass=phasePass && errors.Length==0 && missing.Length==0 && legacy.Length==0,phasePass,phases,shaderMessages=errors,missing,legacy,postControllers=UnityEngine.Object.FindObjectsByType<PostProcessController>().Select(p=>new{p.name,p.enabled}),volumes};
        string json=JsonConvert.SerializeObject(report,Formatting.Indented);File.WriteAllText(Path.Combine(Root,"Validation.json"),json);return json;
    }
}
