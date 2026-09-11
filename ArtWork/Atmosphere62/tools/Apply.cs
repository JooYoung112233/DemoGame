using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;

public static class AtmosphereApply
{
    const string WeatherPath="Assets/Resources/Data/WeatherData.asset";
    const string ProfilePath="Assets/Resources/PlayerRigVolume3D.asset";
    const string ScenePath="Assets/Scenes/Safehouse.unity";
    const string PrefabPath="Assets/Art/Environments/TownFinish62/Prefabs/Pawnshop_Forecourt62.prefab";
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/Atmosphere62"));
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop this review's Play before saving scene values.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        if(scene.isDirty)throw new Exception("Safehouse has unsaved edits; do not overwrite.");
        var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
        var streets=lights.Where(l=>l.name=="PracticalLight").ToArray();
        var porch=lights.Single(l=>l.name=="PorchWarmLight");
        if(streets.Length!=6)throw new Exception("Expected six existing street lamps.");
        string Invariants()=>JsonConvert.SerializeObject(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null && (c is Collider || (c is MonoBehaviour && !(c is Volume) && !(c is PropLight3D)))).Select(c=>new {id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),type=c.GetType().FullName,data=EditorJsonUtility.ToJson(c)}).OrderBy(c=>c.id));
        string before=Invariants();
        foreach(var path in new[]{WeatherPath,ProfilePath,ScenePath,PrefabPath}){
            string dst=Path.Combine(Root,"BeforeFiles",path);Directory.CreateDirectory(Path.GetDirectoryName(dst));
            if(!File.Exists(dst))File.Copy(Path.Combine(Application.dataPath,"..",path),dst);
        }
        var w=AssetDatabase.LoadAssetAtPath<WeatherData>(WeatherPath);Undo.RecordObject(w,"Town atmosphere daylight");
        w.dayIntensity=.95f;w.dayLightColor=new Color(1,.93f,.84f);w.dayAmbientColor=new Color(.29f,.32f,.35f);
        EditorUtility.SetDirty(w);AssetDatabase.SaveAssetIfDirty(w);
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if(!profile.TryGet<ColorAdjustments>(out var c)||!profile.TryGet<Bloom>(out var b)||!profile.TryGet<Vignette>(out var v))throw new Exception("Required grading components missing");
        Undo.RecordObjects(new UnityEngine.Object[]{c,b,v},"Town atmosphere grading");
        c.contrast.Override(14);c.saturation.Override(-6);c.postExposure.Override(.10f);
        b.intensity.Override(.15f);b.threshold.Override(1.1f);v.intensity.Override(.16f);
        foreach(var component in new VolumeComponent[]{c,b,v}){EditorUtility.SetDirty(component);AssetDatabase.SaveAssetIfDirty(component);}
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        foreach(var l in streets){Undo.RecordObject(l,"Town street light atmosphere");l.intensity=6;l.range=7.5f;l.color=new Color(1,.8f,.57f);Bind(l,6,18);EditorUtility.SetDirty(l);PrefabUtility.RecordPrefabInstancePropertyModifications(l);}
        Undo.RecordObject(porch,"Town porch atmosphere");porch.intensity=2.4f;porch.range=5;Bind(porch,2.4f,4.8f);EditorUtility.SetDirty(porch);PrefabUtility.RecordPrefabInstancePropertyModifications(porch);
        string after=Invariants();if(before!=after)throw new Exception("Gameplay/collider serialization changed unexpectedly.");
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try{var l=prefab.GetComponentsInChildren<Light>(true).Single(x=>x.name=="PorchWarmLight");l.intensity=2.4f;l.range=5;Bind(l,2.4f,4.8f);PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Safehouse save failed");
        var report=new {weather=WeatherPath,profile=ProfilePath,scene=ScenePath,prefab=PrefabPath,streetCount=streets.Length,gameplayAndCollidersUnchanged=before==after,dayIntensity=w.dayIntensity,dayColor=w.dayLightColor.ToString(),dayAmbient=w.dayAmbientColor.ToString(),nightSettingsUnchanged=true,streetDay=6,streetNight=18,streetRange=7.5,porchDay=2.4,porchNight=4.8,porchRange=5,contrast=c.contrast.value,saturation=c.saturation.value,exposure=c.postExposure.value,bloom=b.intensity.value,vignette=v.intensity.value};
        string json=JsonConvert.SerializeObject(report,Formatting.Indented);File.WriteAllText(Path.Combine(Root,"Applied.json"),json);return json;
    }
    static void Bind(Light l,float day,float night)
    {
        var phase=l.GetComponent<PropLight3D>();if(phase==null)phase=Undo.AddComponent<PropLight3D>(l.gameObject);
        Undo.RecordObject(phase,"Practical light day/night");phase.dayIntensity=day;phase.nightIntensity=night;EditorUtility.SetDirty(phase);
    }
}
