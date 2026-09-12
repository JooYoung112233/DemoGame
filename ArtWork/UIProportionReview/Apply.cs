using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using Newtonsoft.Json;
public static class ApplyUIProportions {
 static void FixMap(GameObject root){foreach(var c in root.GetComponentsInChildren<MapSelectUI>(true)){var title=c.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Title"&&t.transform.parent.name=="Header");if(title!=null)title.rectTransform.sizeDelta=new Vector2(900,0);}}
 public static string Run(){if(Application.isPlaying)throw new Exception("Stop play before saving UI assets");var paths=new List<string>();int count=0;
 foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/UI"}).Select(AssetDatabase.GUIDToAssetPath)){
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset.GetComponentsInChildren<CanvasScaler>(true).Length==0)continue;
 var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var s in root.GetComponentsInChildren<CanvasScaler>(true)){if(s.GetComponent<Canvas>().renderMode==RenderMode.WorldSpace)continue;UITheme.ConfigureCanvasScale(s);count++;}root.GetComponentInChildren<CharacterPanelUI>(true)?.ApplyReadableTypography();FixMap(root);PrefabUtility.SaveAsPrefabAsset(root,path);paths.Add(path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
 var scene=SceneManager.GetSceneByName("Systems");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Systems.unity",OpenSceneMode.Additive);
 foreach(var root in scene.GetRootGameObjects()){foreach(var s in root.GetComponentsInChildren<CanvasScaler>(true)){if(s.GetComponent<Canvas>().renderMode==RenderMode.WorldSpace)continue;UITheme.ConfigureCanvasScale(s);EditorUtility.SetDirty(s);count++;}root.GetComponentInChildren<CharacterPanelUI>(true)?.ApplyReadableTypography();FixMap(root);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
 AssetDatabase.SaveAssets();File.WriteAllText(Path.GetFullPath("../ArtWork/UIProportionReview/AppliedAssets.json"),JsonConvert.SerializeObject(new{count,paths},Formatting.Indented));return "Configured "+count+" scalers across UI prefabs and Systems";
 }
}
