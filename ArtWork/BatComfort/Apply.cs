using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
public static class BatComfortApply
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required");
        const string path="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/SimpleHero.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var bat=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hero_Bat");
            // Imported rigid mesh has its shaft at x=-.1, z=-.025, while both authored hand sockets use x=z=0.
            // Convert the mesh-space offset through its local scale (socket scale is 100).
            var center=bat.GetComponent<MeshFilter>().sharedMesh.bounds.center;
            bat.localPosition=Vector3.Scale(new Vector3(-center.x,0,-center.z),bat.localScale);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var w=Resources.Load<WeaponData>("WeaponData/Weapon_Bat");
        var attacks=new[]{w.lightCombo.GetStep(0),w.heavyAttack,w.heavyFullAttack};
        var fps=new[]{60,48,40};
        for(int i=0;i<attacks.Length;i++){attacks[i].fps=fps[i];EditorUtility.SetDirty(attacks[i]);}
        AssetDatabase.SaveAssets();
        var result=JsonConvert.SerializeObject(attacks.Select(a=>new{a.name,a.Duration,a.fps,a.totalFrames,hitStart=a.windows[0].startFrame/(float)a.fps,hitEndExclusive=(a.windows[0].endFrame+1)/(float)a.fps}),Formatting.Indented);
        File.WriteAllText(Path.GetFullPath("../ArtWork/BatComfort/TimingAfter.json"),result);return result;
    }
}
