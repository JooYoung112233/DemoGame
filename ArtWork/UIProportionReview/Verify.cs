using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEditor;using Newtonsoft.Json;
public static class VerifyUIProportions {
 public static string Run(){SceneTransitionManager.Instance.StartCoroutine(Checks());return "UI interaction regression started";}
 static Button FieldButton(object target,string name)=>(Button)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
 static bool Click(Button b){var corners=new Vector3[4];b.GetComponent<RectTransform>().GetWorldCorners(corners);var e=new PointerEventData(EventSystem.current){position=(corners[0]+corners[2])*.5f};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);bool ok=hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b;if(ok)b.onClick.Invoke();return ok;}
 static IEnumerator Checks(){var rows=new List<object>();UIManager.Instance.CloseAll();yield return null;
 rows.Add(new{check="one active EventSystem",pass=UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1});
 CodexUI.Show();yield return null;UIManager.Instance.CloseAll();yield return null;rows.Add(new{check="CloseAll clears codex",pass=!CodexUI.IsShowing});
 TraitPanelUI.Show();yield return null;UIManager.Instance.CloseAll();yield return null;rows.Add(new{check="CloseAll clears traits",pass=!TraitPanelUI.IsShowing});
 UIManager.Instance.ShowCharacterPanel();yield return null;yield return null;
 var inv=UnityEngine.Object.FindAnyObjectByType<CharacterPanelUI>();var map=NavigationHUD.Instance.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="TownMinimap");
 rows.Add(new{check="inventory hides town map",pass=!map.gameObject.activeInHierarchy});
 bool click=Click(FieldButton(inv,"closeBtn"));yield return null;yield return null;
 rows.Add(new{check="inventory close button raycast and action",pass=click&&!inv.IsShowing&&map.gameObject.activeInHierarchy});
 PauseMenu.Show();yield return null;SettingsUI.Show();yield return null;
 click=Click(FieldButton(SettingsUI.Instance,"closeBtn"));yield return null;
 rows.Add(new{check="settings close restores pause",pass=click&&!SettingsUI.IsShowing&&PauseMenu.Instance.IsShowing});
 click=Click(FieldButton(PauseMenu.Instance,"resumeBtn"));yield return null;
 rows.Add(new{check="pause resume button and time",pass=click&&!PauseMenu.Instance.IsShowing&&Time.timeScale>0});
 QuestLogUI.Show();yield return null;yield return null;
 var scroll=UnityEngine.Object.FindObjectsByType<ScrollRect>().Single(s=>s.name=="ThreadScroll");
 Canvas.ForceUpdateCanvases();var before=scroll.content.anchoredPosition;scroll.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-3)});yield return null;yield return null;
 rows.Add(new{check="quest content visible and scrolls",pass=scroll.viewport.GetComponent<RectMask2D>()!=null&&scroll.content.childCount>0&&Vector2.Distance(before,scroll.content.anchoredPosition)>1});
 var close=scroll.transform.root.GetComponentsInChildren<Button>().Single(b=>b.name=="Close");click=Click(close);yield return null;
 rows.Add(new{check="quest close button raycast and action",pass=click&&!QuestLogUI.IsShowing});
 rows.Add(new{check="root canvas scaling consistent",pass=UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include).Where(s=>s.GetComponent<Canvas>().isRootCanvas&&s.GetComponent<Canvas>().renderMode!=RenderMode.WorldSpace).All(s=>s.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand)});
 UIManager.Instance.CloseAll();
 File.WriteAllText(Path.GetFullPath("../ArtWork/UIProportionReview/InteractionValidation.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
 var original=JsonConvert.DeserializeObject<Dictionary<string,int>>(File.ReadAllText(Path.GetFullPath("../ArtWork/UIProportionReview/OriginalView.json")));var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");type.GetProperty("selectedSizeIndex",BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).SetValue(EditorWindow.GetWindow(type),original["index"]);
 }
}
