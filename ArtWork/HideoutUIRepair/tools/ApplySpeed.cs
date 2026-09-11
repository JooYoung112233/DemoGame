using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;
public static class ApplyMovementSpeed
{
    static void Configure(TopDownPlayer p)
    {
        var s=new SerializedObject(p);
        s.FindProperty("animCadenceMatchesSpeed").boolValue=true;
        s.FindProperty("walkClipSpeed").floatValue=.45f;
        s.FindProperty("runClipSpeed").floatValue=2.3f;
        s.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string Run()
    {
        var db=AssetDatabase.LoadAssetAtPath<StatDB>("Assets/Resources/Data/StatDB.asset");
        db.playerStat.moveSpeed=.45f;db.playerStat.sprintSpeedMultiplier=5.1f;
        EditorUtility.SetDirty(db);AssetDatabase.SaveAssetIfDirty(db);
        const string path="Assets/Resources/PlayerRig.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{Configure(root.GetComponentInChildren<TopDownPlayer>(true));PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        int players=0;
        foreach(var p in UnityEngine.Object.FindObjectsByType<TopDownPlayer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            Configure(p);players++;
            if(!Application.isPlaying && p.gameObject.scene.IsValid())
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(p);
                EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
                EditorSceneManager.SaveScene(p.gameObject.scene);
            }
        }
        return JsonConvert.SerializeObject(new{playing=Application.isPlaying,players,walk=db.playerStat.moveSpeed,run=db.playerStat.moveSpeed*db.playerStat.sprintSpeedMultiplier,crouch=db.playerStat.moveSpeed*db.playerStat.crouchSpeedMultiplier});
    }
}
