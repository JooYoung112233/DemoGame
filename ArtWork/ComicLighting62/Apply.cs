using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;

public static class ComicLightingApply
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/ComicLighting62"));
    static bool Target(Light l)=>l.name=="PracticalLight"||l.name=="PorchWarmLight"||l.name=="Bulb";
    static void Set(Light l)
    {
        bool porch=l.name=="PorchWarmLight", bulb=l.name=="Bulb";
        l.color=bulb?new Color(1,.92f,.80f):new Color(1,.87f,.69f);
        if(porch)l.range=4.5f;
        if(l.TryGetComponent<PropLight3D>(out var p)){
            p.dayIntensity=porch?.65f:2.5f;p.nightIntensity=porch?2.8f:9f;l.intensity=p.dayIntensity;
            EditorUtility.SetDirty(p);PrefabUtility.RecordPrefabInstancePropertyModifications(p);
        }else if(bulb)l.intensity=1.8f;
        EditorUtility.SetDirty(l);PrefabUtility.RecordPrefabInstancePropertyModifications(l);
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Apply authored assets outside Play mode");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Safehouse.unity");
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
        if(scene.isDirty)throw new Exception("Safehouse has unsaved edits");
        var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(Target).ToArray();
        if(lights.Count(l=>l.GetComponent<PropLight3D>()!=null)!=7)throw new Exception("Unexpected town phase light count");
        var prefabPaths=lights.Select(l=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(l)).Where(p=>!string.IsNullOrEmpty(p)).Concat(new[]{"Assets/Art/Environments/TownFinish62/Prefabs/Pawnshop_Forecourt62.prefab"}).Distinct().ToArray();
        var changed=new List<string>();
        foreach(var path in prefabPaths){
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var l in root.GetComponentsInChildren<Light>(true).Where(Target))Set(l);PrefabUtility.SaveAsPrefabAsset(root,path);changed.Add(path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var l in lights)Set(l);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
        changed.Add(scene.path);
        var w=Resources.Load<WeatherData>("Data/WeatherData");
        w.dayIntensity=.82f;w.dayLightColor=new Color(1,.98f,.94f);w.dayAmbientColor=new Color(.25f,.28f,.32f);
        w.nightIntensity=.30f;w.nightLightColor=new Color(.63f,.70f,.88f);w.nightAmbientColor=new Color(.13f,.16f,.23f);
        w.nightFogColor=new Color(.06f,.08f,.12f);w.nightFogDensity=.016f;
        EditorUtility.SetDirty(w);AssetDatabase.SaveAssetIfDirty(w);changed.Add(AssetDatabase.GetAssetPath(w));
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Resources/PlayerRigVolume3D.asset");
        if(!profile.TryGet<ColorAdjustments>(out var c)){c=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(c,profile);}
        c.contrast.Override(6);c.saturation.Override(5);c.postExposure.Override(.05f);
        if(profile.TryGet<FilmGrain>(out var grain))grain.intensity.Override(0);
        if(profile.TryGet<Vignette>(out var vignette))vignette.intensity.Override(.10f);
        if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.04f);bloom.threshold.Override(1.4f);bloom.scatter.Override(.35f);}
        foreach(var component in profile.components)EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);changed.Add(AssetDatabase.GetAssetPath(profile));
        var converted=new List<string>();
        var shader=Shader.Find("BRB/GameLit");if(shader==null)throw new Exception("GameLit missing");
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art"})){
            var path=AssetDatabase.GUIDToAssetPath(guid);var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m.shader.name!="Universal Render Pipeline/Lit"||!m.HasProperty("_BaseMap")||m.GetFloat("_Surface")!=0)continue;
            var texPath=AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap"));
            if(!texPath.Contains("WorldComic62")&&!texPath.Contains("Painted62"))continue;
            m.shader=shader;m.SetFloat("_ComicLighting",1);EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);converted.Add(path);
        }
        changed.AddRange(converted);
        string json=JsonConvert.SerializeObject(new{pass=true,phaseLights=7,convertedCount=converted.Count,converted,changed},Formatting.Indented);
        File.WriteAllText(Path.Combine(Root,"Applied.json"),json);return json;
    }
}
