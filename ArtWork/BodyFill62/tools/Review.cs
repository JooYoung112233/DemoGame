using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;

public static class BodyFillReview
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/BodyFill62"));
    public static string Capture(string label,bool diagnostics)
    {
        Directory.CreateDirectory(Root);
        var p=UnityEngine.Object.FindAnyObjectByType<TopDownPlayer>();
        var lamp=UnityEngine.Object.FindAnyObjectByType<WornLamp>();
        var main=Camera.main;var w=Resources.Load<WeatherData>("Data/WeatherData");
        var lights=UnityEngine.Object.FindObjectsByType<Light>();
        var states=lights.Select(l=>new {l.enabled,l.intensity,l.color,rotation=l.transform.rotation}).ToArray();
        var sun=lights.Single(l=>l.type==LightType.Directional);
        var glow=lights.Single(l=>l.name=="BodyGlow");var spill=lights.Single(l=>l.name=="LampSpill");
        var spillLocal=spill.transform.localPosition;var glowLocal=glow.transform.localPosition;var view=p.GetComponentsInChildren<Transform>().Single(t=>t.name=="Character3D");var viewRot=view.localRotation;
        var am=RenderSettings.ambientMode;var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
        var fog=RenderSettings.fog;var fc=RenderSettings.fogColor;var fd=RenderSettings.fogDensity;
        var go=new GameObject("BodyFillReviewCamera"){hideFlags=HideFlags.HideAndDontSave};
        var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.aspect=4f/3;cam.orthographicSize=1.7f;cam.transform.rotation=Quaternion.Euler(62,0,0);cam.transform.position=p.transform.position+Vector3.up*.8f-cam.transform.forward*25;
        var cd=go.AddComponent<UniversalAdditionalCameraData>();EditorUtility.CopySerialized(main.GetUniversalAdditionalCameraData(),cd);cd.renderType=CameraRenderType.Base;
        var rt=new RenderTexture(960,720,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
        File.WriteAllText(Path.Combine(Root,label+"_Inspect.json"),JsonConvert.SerializeObject(new {lamp=EditorJsonUtility.ToJson(lamp),lights=lights.Where(l=>l==glow||l==spill||l.gameObject==lamp.gameObject).Select(l=>new {l.name,l.intensity,l.range,pos=l.transform.position.ToString()}),renderers=p.GetComponentsInChildren<Renderer>().Select(r=>new {r.name,materials=r.sharedMaterials.Where(m=>m!=null).Select(m=>new {m.name,path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,emission=m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor").ToString():null,smoothness=m.HasProperty("_Smoothness")?m.GetFloat("_Smoothness"):0})})},Formatting.Indented));
        try{
            foreach(bool night in new[]{false,true}){
                lamp.ForcePhase(night);
                sun.color=night?w.nightLightColor:w.dayLightColor;sun.intensity=night?w.nightIntensity:w.dayIntensity;sun.transform.rotation=Quaternion.Euler(night?w.nightSunAngle:w.daySunAngle);
                DayNightCycle.ApplyAmbient(night?w.nightAmbientColor:w.dayAmbientColor);RenderSettings.fog=night?w.nightFog:w.dayFog;RenderSettings.fogColor=night?w.nightFogColor:w.dayFogColor;RenderSettings.fogDensity=night?w.nightFogDensity:w.dayFogDensity;
                foreach(var l in lights)if(l.TryGetComponent<PropLight3D>(out var phase))l.intensity=night?phase.nightIntensity:phase.dayIntensity;
                float gi=glow.intensity,si=spill.intensity;
                foreach(int yaw in diagnostics?new[]{90,270}:new[]{0,45,90,135,180,225,270,315}){
                    view.localRotation=Quaternion.Euler(0,yaw,0);lamp.transform.rotation=Quaternion.Euler(16,yaw,0);
                    foreach(string test in diagnostics?new[]{"Before","NoGlow","NoSpill","NoBeam","SoftFill"}:new[]{label}){
                        spill.transform.position=lamp.transform.position+Vector3.up*new SerializedObject(lamp).FindProperty("spillHeightOffset").floatValue;
                        glow.transform.position=lamp.transform.position+Vector3.up*new SerializedObject(lamp).FindProperty("bodyGlowHeight").floatValue+Vector3.back*(test=="SoftFill"?1.1f:new SerializedObject(lamp).FindProperty("bodyGlowTowardCamera").floatValue);
                        glow.intensity=test=="NoGlow"?0:test=="SoftFill"?.06f:gi;
                        spill.intensity=test=="NoSpill"?0:si;
                        lamp.GetComponent<Light>().intensity=test=="NoBeam"?0:(night?3.4f:.2f);
                        cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;
                        var tex=new Texture2D(960,720,TextureFormat.RGB24,false);
                        try{tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Root,test+"_"+(night?"Night":"Day")+("_Yaw"+yaw)+".png"),tex.EncodeToPNG());}
                        finally{UnityEngine.Object.DestroyImmediate(tex);}
                    }
                }
            }
        }finally{
            view.localRotation=viewRot;spill.transform.localPosition=spillLocal;glow.transform.localPosition=glowLocal;
            for(int i=0;i<lights.Length;i++){var l=lights[i];var s=states[i];l.enabled=s.enabled;l.intensity=s.intensity;l.color=s.color;l.transform.rotation=s.rotation;}
            RenderSettings.ambientMode=am;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=ground;RenderSettings.fog=fog;RenderSettings.fogColor=fc;RenderSettings.fogDensity=fd;
            cam.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
        }
        return "Captured player front/back day/night; lights, visual facing and environment restored.";
    }
}
