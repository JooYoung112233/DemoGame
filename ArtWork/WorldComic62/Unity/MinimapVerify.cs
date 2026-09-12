using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
public static class MinimapVerify {
 public static string Refresh(){AssetDatabase.Refresh();return "Refresh requested";}
 public static string ArchiveCapture(){var src=System.IO.Path.GetFullPath("Assets/Screenshots/TownMinimap.png");System.IO.File.Copy(src,System.IO.Path.GetFullPath("../ArtWork/WorldComic62/Unity/TownMinimap.png"),true);AssetDatabase.DeleteAsset("Assets/Screenshots/TownMinimap.png");if(System.IO.Directory.GetFileSystemEntries("Assets/Screenshots").Length==0)AssetDatabase.DeleteAsset("Assets/Screenshots");return "Capture archived outside runtime assets";}
 public static string State(){return JsonConvert.SerializeObject(new{scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{name=SceneManager.GetSceneAt(i).name,active=SceneManager.GetSceneAt(i)==SceneManager.GetActiveScene()}),playing=Application.isPlaying,hideout=HideoutController.IsActive,title=TitleScreen.IsShowing,modal=UIManager.Instance?.IsAnyUIOpen(),player=TopDownPlayer.Instance==null?null:new[]{TopDownPlayer.Instance.transform.position.x,TopDownPlayer.Instance.transform.position.z},hud=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Where(c=>c.gameObject.activeInHierarchy).Select(c=>c.name)});}
 public static string Town(){UIManager.Instance.CloseAll(); if(TitleScreen.Instance!=null)typeof(TitleScreen).GetMethod("Hide",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(TitleScreen.Instance,null);SceneTransitionManager.Instance.TransitionTo("Safehouse","default");return "Town transition";}
 public static string Check(){SceneTransitionManager.Instance.StartCoroutine(Checks());return "Minimap checks started";}
 static IEnumerator Checks(){
 var results=new System.Collections.Generic.List<object>();
 var hud=Object.FindAnyObjectByType<TownMinimapHUD>();
 var panel=NavigationHUD.Instance.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="TownMinimap");
 var dot=panel.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="PlayerPosition");
 var player=TopDownPlayer.Instance;var rb=player.GetComponent<Rigidbody>();var original=player.transform.position;
 bool visible=panel.gameObject.activeInHierarchy;
 var a=dot.anchoredPosition;
 rb.position=original+new Vector3(-3,0,3);player.transform.position=rb.position;rb.linearVelocity=Vector3.zero;
 yield return new WaitForSecondsRealtime(.15f);
 var delta=dot.anchoredPosition-a;
 results.Add(new{check="town visible and movement XZ",passed=visible&&delta.x<-10&&delta.y>10,delta=new[]{delta.x,delta.y}});
 rb.position=original;player.transform.position=original;rb.linearVelocity=Vector3.zero;
 PauseMenu.Show();yield return null;yield return null;
 results.Add(new{check="modal hides map",passed=!panel.gameObject.activeInHierarchy});
 PauseMenu.Instance.Hide();yield return null;yield return null;
 results.Add(new{check="modal closes restores map",passed=panel.gameObject.activeInHierarchy});
 var graphics=panel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
 results.Add(new{check="no input interception",passed=graphics.All(g=>!g.raycastTarget)});
 results.Add(new{check="six building footprints",passed=panel.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Building_"))==6});
 var quest=Object.FindAnyObjectByType<QuestHUD>();
 var qp=(GameObject)typeof(QuestHUD).GetField("panelRoot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(quest);
 var qr=qp.GetComponent<RectTransform>();var before=qr.sizeDelta;var coords=new Vector3[4];qr.GetWorldCorners(coords);var mc=new Vector3[4];panel.GetWorldCorners(mc);
 results.Add(new{check="quest below map",passed=coords[1].y<mc[0].y,questSize=new[]{qr.rect.width,qr.rect.height}});
 quest.SetTownMapVisible(false);var restored=qr.sizeDelta;quest.SetTownMapVisible(true);
 results.Add(new{check="quest size preserved",passed=Vector2.Distance(before,restored)<.01f});
 SceneTransitionManager.Instance.TransitionTo("Hideout","default");yield return new WaitForSecondsRealtime(3f);
 results.Add(new{check="hideout hides map",passed=HideoutController.IsActive&&!panel.gameObject.activeInHierarchy});
 SceneTransitionManager.Instance.TransitionTo("Safehouse","from_hideout");yield return new WaitForSecondsRealtime(3f);
 results.Add(new{check="return town restores one map",passed=!HideoutController.IsActive&&panel.gameObject.activeInHierarchy&&NavigationHUD.Instance.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="TownMinimap")==1});
 var day=Object.FindAnyObjectByType<DayNightCycle>();var sun=(Light)typeof(DayNightCycle).GetField("directionalLight",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(day);
 results.Add(new{check="day/night sun bound to destination town",passed=sun!=null&&sun.gameObject.scene.name=="Safehouse",scene=sun?.gameObject.scene.name});
 System.IO.File.WriteAllText(System.IO.Path.GetFullPath("../ArtWork/WorldComic62/Unity/MinimapValidation.json"),JsonConvert.SerializeObject(results,Formatting.Indented));
 }
}
