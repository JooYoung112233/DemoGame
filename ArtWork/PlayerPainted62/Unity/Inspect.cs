using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class PlayerPaintInspect {
 public static string Run(){var p=TopDownPlayer.Instance??UnityEngine.Object.FindAnyObjectByType<TopDownPlayer>();if(p==null)return JsonConvert.SerializeObject(new{playing=Application.isPlaying,scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>{var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);return new{s.name,s.path,s.isDirty};})});
 var rows=p.GetComponentsInChildren<Renderer>(true).Select(r=>new{name=r.name,active=r.gameObject.activeInHierarchy,type=r.GetType().Name,bounds=r.bounds.ToString(),materials=r.sharedMaterials.Select(m=>m==null?null:new{name=m.name,path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,baseMap=m.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")):null}),mesh=r is SkinnedMeshRenderer s?AssetDatabase.GetAssetPath(s.sharedMesh):null}).ToArray();
 File.WriteAllText(Path.GetFullPath("../ArtWork/PlayerPainted62/Unity/Inspection.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));return JsonConvert.SerializeObject(new{position=p.transform.position.ToString(),rendererCount=rows.Length,paintedSlots=p.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.sharedMaterials).Count(m=>m.name=="Player_Painted"||m.name=="PlayerGear_Painted")});
 }
}
