using System;
using UnityEngine;
using UnityEditor;

public static class BackpackApply
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop review Play before applying");
        const string path="Assets/Resources/PlayerRig.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var lamp=root.GetComponentInChildren<WornLamp>(true);
            var so=new SerializedObject(lamp);
            so.FindProperty("spillFloor").floatValue=.08f;
            so.FindProperty("spillRatio").floatValue=.04f;
            so.FindProperty("spillHeightOffset").floatValue=-.65f;
            so.ApplyModifiedPropertiesWithoutUndo();
            if(PrefabUtility.SaveAsPrefabAsset(root,path)==null)throw new Exception("Prefab save failed");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        return "Saved PlayerRig spill floor=.08, ratio=.04, world height offset=-.65";
    }
}
