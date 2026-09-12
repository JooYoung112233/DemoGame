using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
public static class MinimapInspect {
 public static string Run() {
 var scene=SceneManager.GetSceneByName("Safehouse");
 if(!scene.isLoaded)return "Safehouse not loaded";
 var ts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true));
 var result=JsonConvert.SerializeObject(ts.Where(t=>t.GetComponent<InteractableObject>()!=null||t.name.Contains("Home")||t.name.Contains("Ground")||t.name.Contains("Door")).Select(t=>new {t.name,parent=t.parent?.name,p=new[]{t.position.x,t.position.y,t.position.z},components=t.GetComponents<Component>().Select(c=>c?.GetType().Name),data=t.GetComponent<InteractableObject>()==null?null:JsonUtility.ToJson(t.GetComponent<InteractableObject>())}),Formatting.Indented);
 System.IO.File.WriteAllText(System.IO.Path.GetFullPath("../ArtWork/WorldComic62/Unity/MinimapScene.json"),result);return result;
 }
}
