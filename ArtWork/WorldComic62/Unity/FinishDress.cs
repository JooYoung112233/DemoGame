using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
public static class TownDressFinish {
 public static string Run(){
 if(Application.isPlaying)throw new Exception("Stop play");var scene=SceneManager.GetSceneByName("Safehouse");if(scene.isDirty)throw new Exception("Unsaved scene");var root=scene.GetRootGameObjects().Single(g=>g.name=="Map").transform.Find("TownLife_Comic62");
 var home=root.Find("Home_LaundryKitchen");home.position+=new Vector3(5.5f,0,0);
 Move("Repair_Workyard/Bicycle02",new Vector3(2.3f,0,2.3f));Move("Repair_Workyard/ScrapHeap02",new Vector3(0,0,1.4f));Move("Repair_Workyard/Ladder02",new Vector3(-.4f,0,1.4f));
 void Move(string p,Vector3 v){var t=root.Find(p);t.position+=v;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
 // Fix imported FBX color defaults and reuse the established town response.
 int remapped=0;
 foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/WorldComic62/Materials"})){
 var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));var n=m.name;int i=n.IndexOf("TownProps_");if(i>=0)n=n.Substring(i+10);else{i=n.IndexOf("Town_");if(i<0)continue;n=n.Substring(i+5);}
 var target=n.StartsWith("Wood")?(n=="WoodDark"?"WoodDark":"Wood"):n;
 if(new[]{"Linen","BlueCloth"}.Contains(n))target="Canvas";if(n=="Cardboard")target="Paper";if(n=="Rust")target="Red";if(new[]{"Rubber","BagPlastic","DirtSeam"}.Contains(n))target="Edge";if(n=="Wear")target="Concrete";if(n=="Blue")target="Olive";
 var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Town02/Materials/Town_"+target+".mat");if(source==null)continue;
 var oldname=m.name;EditorUtility.CopySerialized(source,m);m.name=oldname;EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);remapped++;
 }
 PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Art/WorldComic62/Prefabs/TownLife_Comic62.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 var data=JObject.Parse(File.ReadAllText(Path.GetFullPath("../ArtWork/WorldComic62/Unity/TownPlacement.json")));var used=new System.Collections.Generic.HashSet<Transform>();
 foreach(var row in data["props"]){var p=root.Find((string)row["group"]).Cast<Transform>().First(t=>t.name==(string)row["name"]&&!used.Contains(t));used.Add(p);var rs=p.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);row["position"]=JArray.FromObject(new[]{p.position.x,p.position.y,p.position.z});row["center"]=JArray.FromObject(new[]{b.center.x,b.center.y,b.center.z});row["size"]=JArray.FromObject(new[]{b.size.x,b.size.y,b.size.z});}
 File.WriteAllText(Path.GetFullPath("../ArtWork/WorldComic62/Unity/TownPlacement.json"),data.ToString());return $"Moved home cluster clear of house, repaired visibility; harmonized {remapped} imported materials";
 }
}
