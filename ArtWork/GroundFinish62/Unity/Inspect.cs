using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;
public static class GroundInspect {
 public static string Run(){
 var scene=SceneManager.GetSceneByName("Safehouse");
 if(!scene.isLoaded){if(Application.isPlaying)throw new Exception("Town not loaded during Play");scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);}
 var roots=scene.GetRootGameObjects();
 var rs=roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true));
 var result=new{playing=Application.isPlaying,scene.path,scene.isDirty,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).name,SceneManager.GetSceneAt(i).isDirty}),
 doors=roots.SelectMany(r=>r.GetComponentsInChildren<SceneDoor3D>(true)).Select(d=>new{d.name,d.TargetScene,pos=d.transform.position.ToString()}),
 near=rs.Where(r=>r.enabled && r.bounds.center.x>47 && r.bounds.center.x<59 && r.bounds.center.z>5 && r.bounds.center.z<19).Select(r=>new{path=AnimationUtility.CalculateTransformPath(r.transform,null),center=r.bounds.center.ToString(),size=r.bounds.size.ToString(),r.enabled,mats=r.sharedMaterials.Select(m=>m==null?"NULL":AssetDatabase.GetAssetPath(m))})};
 var json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/GroundFinish62/Unity/HomeInspection.json"),json);return "Home inspection written";
 }
}
