using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;using Newtonsoft.Json;
public static class WorldComicAudit{
 public static string Models(){return JsonConvert.SerializeObject(new[]{"Bicycle02","Clothesline02","SpoolTable02","MerchantCart02"}.Select(n=>{string p="Assets/Art/Environments/"+(n=="Bicycle02"||n=="Clothesline02"?"TownProps02":"Town02")+"/Models/"+n+".fbx";var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);return new{n,rotation=g.transform.localEulerAngles.ToString(),scale=g.transform.localScale.ToString(),materials=g.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>new{m.name,path=AssetDatabase.GetAssetPath(m),color=m.color.ToString(),basecolor=m.GetColor("_BaseColor").ToString()})};}));}
 public static string Reload(){
 AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
 var s=SceneManager.GetSceneByName("Safehouse");if(s.IsValid())EditorSceneManager.CloseScene(s,true);
 EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
 foreach(var id in AssetDatabase.FindAssets("t:Material Road_",new[]{"Assets/Art/Environments/Town02/Rendering62"})){
 var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));m.SetTextureScale("_BaseMap",new Vector2(.25f,.25f));EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);
 }
 return "Reloaded original clean scene for per-material binding preservation; road UV calibrated";
 }
 public static string Run(){
 var scene=SceneManager.GetSceneByName("Safehouse");if(!scene.IsValid())scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
 var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r is MeshRenderer).ToArray();
 var rows=rs.Where(r=>r.name.Contains("Road")||r.name.Contains("Ground")||r.name.Contains("Floor")).Select(r=>{var m=r.GetComponent<MeshFilter>()?.sharedMesh;var uv=m==null?new Vector2[0]:m.uv;return new{name=r.name,active=r.gameObject.activeInHierarchy,enabled=r.enabled,materials=r.sharedMaterials.Select(x=>x==null?"null":AssetDatabase.GetAssetPath(x)+":"+x.name),uvMin=uv.Length==0?null:new[]{uv.Min(x=>x.x),uv.Min(x=>x.y)},uvMax=uv.Length==0?null:new[]{uv.Max(x=>x.x),uv.Max(x=>x.y)}};}).ToArray();
 File.WriteAllText(Path.GetFullPath("../ArtWork/WorldComic62/Unity/RoadAudit.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
 return JsonConvert.SerializeObject(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).name,SceneManager.GetSceneAt(i).isDirty}));
 }
}
