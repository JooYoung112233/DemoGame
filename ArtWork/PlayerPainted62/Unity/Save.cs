using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class PlayerPaintSave {
 const string Prefab="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/SimpleHero.prefab";
 static string Snapshot(GameObject root)=>JsonConvert.SerializeObject(new{
 transforms=root.GetComponentsInChildren<Transform>(true).Select(t=>new{t.name,p=t.localPosition.ToString("F6"),r=t.localRotation.ToString("F6"),s=t.localScale.ToString("F6")}),
 skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s=>new{s.name,mesh=AssetDatabase.GetAssetPath(s.sharedMesh),bones=s.bones.Select(b=>b.name),rootBone=s.rootBone?.name}),
 animators=root.GetComponentsInChildren<Animator>(true).Select(a=>new{controller=AssetDatabase.GetAssetPath(a.runtimeAnimatorController),avatar=AssetDatabase.GetAssetPath(a.avatar),a.applyRootMotion})});
 public static string Run(){var go=PrefabUtility.LoadPrefabContents(Prefab);int changed=0;string before=Snapshot(go);
 try{
 foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
 var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
 var n=mats[i].name=="SimpleHero_Surface"?"Player":mats[i].name=="SimpleHero_Gear"?"PlayerGear":null;
 if(n==null)continue;var candidate=AssetDatabase.LoadAssetAtPath<Material>("Assets/ChibiSurvivor/Player/Painted62/Materials/"+n+"_Painted.mat");if(candidate==null)throw new Exception("Candidate missing");mats[i]=candidate;changed++;
 }r.sharedMaterials=mats;
 }
 if(before!=Snapshot(go))throw new Exception("Unexpected model/rig/animation mutation");
 if(changed==0)throw new Exception("No original material slots found");
 PrefabUtility.SaveAsPrefabAsset(go,Prefab);
 }finally{PrefabUtility.UnloadPrefabContents(go);}
 var verify=PrefabUtility.LoadPrefabContents(Prefab);
 try{var oldSlots=verify.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.sharedMaterials).Count(m=>m.name=="SimpleHero_Surface"||m.name=="SimpleHero_Gear");if(oldSlots!=0||before!=Snapshot(verify))throw new Exception("Saved prefab verification failed");}
 finally{PrefabUtility.UnloadPrefabContents(verify);}
 File.WriteAllText(Path.GetFullPath("../ArtWork/PlayerPainted62/Unity/Saved.json"),JsonConvert.SerializeObject(new{prefab=Prefab,changedMaterialSlots=changed,reloadedPrefabVerified=true,meshRigAnimationPreserved=true,originalTextureAndMaterialFilesPreserved=true},Formatting.Indented));
 return "Saved and reloaded player prefab: "+changed+" painted material slots; model/rig/animation unchanged";
 }
}
