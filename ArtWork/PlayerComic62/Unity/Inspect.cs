using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
public static class ComicInspect {
 public static string Run(){ return JsonConvert.SerializeObject(new {
 playing=EditorApplication.isPlaying,
 scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).name).ToArray(),
 players=Object.FindObjectsByType<TopDownPlayer>(FindObjectsInactive.Include).Select(p=>new {p.name,active=p.gameObject.activeInHierarchy,pos=new[]{p.transform.position.x,p.transform.position.y,p.transform.position.z}}).ToArray(),
 rigs=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include).Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.name.Contains("Player"))).Select(r=>new {r.name,root=r.transform.root.name,active=r.gameObject.activeInHierarchy}).ToArray(),
 cameras=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Select(c=>new {c.name,active=c.gameObject.activeInHierarchy,c.enabled}).ToArray()
 },Formatting.Indented); }
}
