using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;

public static class AtmosphereReview
{
    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../ArtWork/Atmosphere62"));
    static string Json(object value) => JsonConvert.SerializeObject(value, Formatting.Indented);
    public static string Inspect()
    {
        Directory.CreateDirectory(Root);
        var cam = Camera.main;
        var urp = UniversalRenderPipeline.asset;
        var report = new {
            playing=Application.isPlaying, scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            pipeline=urp == null ? null : urp.name, hdr=urp != null && urp.supportsHDR,
            camera=cam == null ? null : new {cam.name,position=cam.transform.position.ToString(),angle=cam.transform.eulerAngles.ToString(),cam.orthographicSize,cam.allowHDR,post=cam.GetUniversalAdditionalCameraData().renderPostProcessing,mask=cam.GetUniversalAdditionalCameraData().volumeLayerMask.value},
            volumes=UnityEngine.Object.FindObjectsByType<Volume>().Select(v=>new {v.name,v.enabled,v.isGlobal,v.weight,v.priority,layer=v.gameObject.layer,profile=v.sharedProfile == null ? null : v.sharedProfile.name,instanced=v.HasInstantiatedProfile(),components=v.profile.components.Select(c=>new {type=c.GetType().Name,c.active,data=EditorJsonUtility.ToJson(c)})}),
            lights=UnityEngine.Object.FindObjectsByType<Light>().Select(l=>new {l.name,type=l.type.ToString(),l.enabled,l.intensity,l.range,color=l.color.ToString(),pos=l.transform.position.ToString(),shadows=l.shadows.ToString()}),
            weather=EditorJsonUtility.ToJson(Resources.Load<WeatherData>("Data/WeatherData")),
            shaderErrors=ShaderUtil.GetShaderMessages(Shader.Find("BRB/GameLit")).Select(e=>new {e.message,e.file,e.line,severity=e.severity.ToString()})
        };
        string output=Json(report); File.WriteAllText(Path.Combine(Root,"Inspect.json"),output);return output;
    }
    static void Grade(VolumeProfile p)
    {
        if(p.TryGet<ColorAdjustments>(out var c)){c.contrast.Override(14);c.saturation.Override(-6);c.postExposure.Override(.10f);}
        if(p.TryGet<Vignette>(out var v))v.intensity.Override(.16f);
        if(p.TryGet<Bloom>(out var b)){b.intensity.Override(.15f);b.threshold.Override(1.1f);}
    }
    // Synchronous same-frame lighting tests. Does not fire game phase events or alter region clocks.
    public static string Capture(int variant, string label)
    {
        Directory.CreateDirectory(Root);
        var main=Camera.main;if(main==null)throw new Exception("Main camera missing");
        var sun=UnityEngine.Object.FindObjectsByType<Light>().First(l=>l.type==LightType.Directional);
        var w=Resources.Load<WeatherData>("Data/WeatherData");
        var rot=sun.transform.rotation;var col=sun.color;var power=sun.intensity;
        var ambientMode=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var gr=RenderSettings.ambientGroundColor;
        var fog=RenderSettings.fog;var fogColor=RenderSettings.fogColor;var density=RenderSettings.fogDensity;var fogMode=RenderSettings.fogMode;
        var volumes=UnityEngine.Object.FindObjectsByType<Volume>();
        var profiles=volumes.Select(v=>v.profile).ToArray();
        var copies=profiles.Select(p=>UnityEngine.Object.Instantiate(p)).ToArray();
        // Deep-copy components: cloned profile's component list otherwise shares ScriptableObjects.
        for(int i=0;i<copies.Length;i++)copies[i].components=profiles[i].components.Select(c=>UnityEngine.Object.Instantiate(c)).ToList();
        var roofs=UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.transform.parent!=null && r.transform.parent.name=="Roof_Art02").ToArray();
        var roofStates=roofs.Select(r=>r.enabled).ToArray();
        var practicals=UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.name=="PracticalLight" || l.name=="PorchWarmLight" || l.name=="Bulb").ToArray();
        var practicalStates=practicals.Select(l=>new {l.enabled,l.intensity,l.range,l.color}).ToArray();
        var go=new GameObject("AtmosphereReviewCamera"){hideFlags=HideFlags.HideAndDontSave};
        var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.aspect=4f/3;cam.transform.rotation=Quaternion.Euler(62,0,0);
        var cd=go.AddComponent<UniversalAdditionalCameraData>();EditorUtility.CopySerialized(main.GetUniversalAdditionalCameraData(),cd);cd.renderType=CameraRenderType.Base;
        var rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
        try{
            for(int i=0;i<volumes.Length;i++){volumes[i].profile=copies[i];if(variant>=2)Grade(copies[i]);}
            foreach(bool night in new[]{false,true}){
                foreach(var l in practicals)if(l.TryGetComponent<PropLight3D>(out var phase))l.intensity=night?phase.nightIntensity:phase.dayIntensity;
                sun.transform.rotation=Quaternion.Euler(night?w.nightSunAngle:w.daySunAngle);
                sun.color=variant==0?(night?w.nightLightColor:w.dayLightColor):(night?new Color(.60f,.56f,.74f):new Color(1,.93f,.84f));
                sun.intensity=variant==0?(night?w.nightIntensity:w.dayIntensity):(night?.22f:.95f);
                DayNightCycle.ApplyAmbient(variant==0?(night?w.nightAmbientColor:w.dayAmbientColor):(night?new Color(.12f,.115f,.17f):new Color(.29f,.32f,.35f)));
                RenderSettings.fog=night?w.nightFog:w.dayFog;RenderSettings.fogColor=night?w.nightFogColor:w.dayFogColor;
                RenderSettings.fogDensity=variant==0?(night?w.nightFogDensity:w.dayFogDensity):(night?.012f:w.dayFogDensity);
                if(variant>=3){
                    sun.color=night?w.nightLightColor:new Color(1,.93f,.84f);
                    DayNightCycle.ApplyAmbient(night?w.nightAmbientColor:new Color(.29f,.32f,.35f));
                    RenderSettings.fogDensity=night?w.nightFogDensity:w.dayFogDensity;
                    foreach(var l in practicals){
                        if(l.name=="PracticalLight"){l.intensity=18;l.range=7.5f;l.color=new Color(1,.80f,.57f);}
                        if(l.name=="PorchWarmLight"){l.intensity=4.8f;l.range=5;}
                    }
                }
                foreach(string view in new[]{"Forecourt","Warden","Interior","Town"}){
                    var target=view=="Forecourt"?new Vector3(34,0,39):view=="Warden"?new Vector3(50,.5f,16):view=="Interior"?new Vector3(34,.5f,45):new Vector3(44,0,32);
                    cam.orthographicSize=view=="Town"?13:view=="Forecourt"?7:3.6f;cam.transform.position=target-cam.transform.forward*25;
                    for(int i=0;i<roofs.Length;i++)roofs[i].enabled=view=="Interior"?false:roofStates[i];
                    for(int i=0;i<practicals.Length;i++)if(practicals[i].name=="Bulb")practicals[i].enabled=view=="Interior" || practicalStates[i].enabled;
                    cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;
                    var tex=new Texture2D(1280,960,TextureFormat.RGB24,false);
                    try{tex.ReadPixels(new Rect(0,0,1280,960),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Root,label+"_"+view+(night?"_Night":"_Day")+".png"),tex.EncodeToPNG());}
                    finally{UnityEngine.Object.DestroyImmediate(tex);}
                }
            }
        }finally{
            for(int i=0;i<roofs.Length;i++)roofs[i].enabled=roofStates[i];
            for(int i=0;i<practicals.Length;i++){var l=practicals[i];var s=practicalStates[i];l.enabled=s.enabled;l.intensity=s.intensity;l.range=s.range;l.color=s.color;}
            for(int i=0;i<volumes.Length;i++){volumes[i].profile=profiles[i];foreach(var c in copies[i].components)UnityEngine.Object.DestroyImmediate(c);UnityEngine.Object.DestroyImmediate(copies[i]);}
            cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
            sun.transform.rotation=rot;sun.color=col;sun.intensity=power;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=gr;
            RenderSettings.fog=fog;RenderSettings.fogColor=fogColor;RenderSettings.fogDensity=density;RenderSettings.fogMode=fogMode;
        }
        return "Captured "+label+"; temporary lighting, roof, camera and volume states restored.";
    }
}

