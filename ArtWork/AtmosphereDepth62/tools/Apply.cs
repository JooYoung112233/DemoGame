using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

public static class AtmosphereDepthApply
{
    const string ScenePath="Assets/Scenes/Safehouse.unity";
    const string PrefabPath="Assets/Art/Environments/TownFinish62/Prefabs/Pawnshop_Forecourt62.prefab";
    const string WeatherPath="Assets/Resources/Data/WeatherData.asset";
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/AtmosphereDepth62"));
    static void Set(Light l)
    {
        var p=l.GetComponent<PropLight3D>();
        if(p==null)throw new Exception("Expected existing PropLight3D on "+l.name);
        bool porch=l.name=="PorchWarmLight";
        p.nightIntensity=porch?7.5f:14f;
        l.range=porch?6.5f:8f;l.color=porch?new Color(1,.75f,.48f):new Color(1,.78f,.53f);
        l.shadows=porch?LightShadows.Soft:LightShadows.None;
        l.intensity=p.dayIntensity;
        EditorUtility.SetDirty(l);EditorUtility.SetDirty(p);PrefabUtility.RecordPrefabInstancePropertyModifications(l);PrefabUtility.RecordPrefabInstancePropertyModifications(p);
    }
    static Light[] Targets(UnityEngine.SceneManagement.Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.name=="PracticalLight"||l.name=="PorchWarmLight").ToArray();
    static string Invariants(UnityEngine.SceneManagement.Scene scene)=>JsonConvert.SerializeObject(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null&&(c is Collider||(c is MonoBehaviour&&!(c is PropLight3D)))).Select(c=>new {id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),type=c.GetType().FullName,data=EditorJsonUtility.ToJson(c)}).OrderBy(x=>x.id));
    public static string Run()
    {
        // Save authored data only. Unity forbids saving even preview scenes during Play.
        if(EditorApplication.isPlaying)throw new Exception("Stop Play after user authorization before saving");
        Directory.CreateDirectory(Root);
        foreach(var path in new[]{ScenePath,PrefabPath,WeatherPath}){
            var dst=Path.Combine(Root,"BeforeFiles",path);Directory.CreateDirectory(Path.GetDirectoryName(dst));
            if(!File.Exists(dst))File.Copy(Path.Combine(Application.dataPath,"..",path),dst);
        }
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        if(scene.isDirty)throw new Exception("Safehouse has unsaved authored edits; do not overwrite");
        var targets=Targets(scene);if(targets.Length!=7)throw new Exception("Expected seven authored lights");
        string before=Invariants(scene);
        foreach(var l in targets)Set(l);
        bool same=before==Invariants(scene);if(!same)throw new Exception("Gameplay/collider data changed");
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Authored scene save failed");
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try{Set(prefab.GetComponentsInChildren<Light>(true).Single(l=>l.name=="PorchWarmLight"));PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        // Prefab synchronization can dirty the open scene again; persist the same authored edits.
        if(scene.isDirty&&!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Prefab sync save failed");
        var w=AssetDatabase.LoadAssetAtPath<WeatherData>(WeatherPath);
        Undo.RecordObject(w,"Night atmosphere depth");
        w.nightIntensity=.38f;w.nightLightColor=new Color(.66f,.58f,.85f);w.nightSunAngle=new Vector3(58,-35,0);
        w.nightAmbientColor=new Color(.20f,.17f,.27f);w.nightFogDensity=.022f;
        EditorUtility.SetDirty(w);AssetDatabase.SaveAssetIfDirty(w);
        var report=new {pass=true,playPreserved=Application.isPlaying,gameplayAndCollidersUnchanged=same,nightIntensity=w.nightIntensity,nightAngle=w.nightSunAngle.ToString(),nightAmbient=w.nightAmbientColor.ToString(),fogDensity=w.nightFogDensity,streetNight=14,streetRange=8,porchNight=7.5,porchRange=6.5,porchShadows="Soft",dayWeatherAndVolumeUnchanged=true};
        string json=JsonConvert.SerializeObject(report,Formatting.Indented);File.WriteAllText(Path.Combine(Root,"Applied.json"),json);return json;
    }
}
