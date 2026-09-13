using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
public static class BatComfortInspect
{
    public static string Run()
    {
        var p=TopDownPlayer.Instance;
        var a=p.GetComponentInChildren<Animator>();
        var bat=a.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hero_Bat");
        object Bone(Transform t)=>new{name=t.name,parent=t.parent.name,local=t.localPosition.ToString("F5"),scale=t.lossyScale.ToString("F5"),position=a.transform.InverseTransformPoint(t.position).ToString("F5"),rotation=t.localEulerAngles.ToString("F3")};
        var w=Resources.Load<WeaponData>("WeaponData/Weapon_Bat");
        var clips=a.runtimeAnimatorController.animationClips.Distinct().Where(c=>c.name.Contains("Bat")).Select(c=>new{c.name,c.length,path=AssetDatabase.GetAssetPath(c)}).ToArray();
        var report=new{model=AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(a.gameObject)),clips,bat=Bone(bat),mesh=bat.GetComponent<MeshFilter>().sharedMesh.bounds.ToString(),bones=a.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("Hand")||t.name.Contains("Arm")).Select(Bone).ToArray(),attacks=new[]{w.lightCombo.GetStep(0),w.heavyAttack,w.heavyFullAttack}.Select(at=>new{at.name,at.Duration,at.fps,at.totalFrames,windows=at.windows.Select(v=>new{v.startFrame,v.endFrame})}),state=a.GetCurrentAnimatorStateInfo(0).fullPathHash,layerWeights=Enumerable.Range(0,a.layerCount).Select(i=>new{name=a.GetLayerName(i),weight=a.GetLayerWeight(i)})};
        var json=JsonConvert.SerializeObject(report,Formatting.Indented);
        File.WriteAllText(Path.GetFullPath("../ArtWork/BatComfort/Before.json"),json);
        return json;
    }
}
