using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class InteriorFinish
{
 static void Adjust(Transform art,bool market){
  foreach(Transform t in art){
   var p=t.localPosition;
   if(t.name.Contains("Floor62_"))p.y=.025f;
   if(t.name.StartsWith("Reuse_Rug01"))p.y=.06f;
   if(market&&(t.name.StartsWith("Reuse_WoodCrate01")||t.name.StartsWith("Reuse_FoldedTarp02")))p.z=-2.8f;
   t.localPosition=p;EditorUtility.SetDirty(t);
  }
 }
 public static string Run(){
  if(Application.isPlaying)throw new Exception("Stop Play before save");
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/Safehouse.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
  if(scene.isDirty)throw new Exception("Unsaved town edits");
  var buildings=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BuildingInterior>(true)).ToArray();
  foreach(var b in buildings)Adjust(b.transform.Find("Interior_Art62"),b.name=="BlackMarket");
  var npc=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<InteractableObject>(true)).Single(n=>n.name=="전당포 주인");npc.transform.position=buildings.Single(b=>b.name=="Pawnshop").transform.position+new Vector3(-1.6f,0,-2.2f);EditorUtility.SetDirty(npc.transform);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  foreach(var b in buildings){string path="Assets/Art/Environments/TownInteriors62/Prefabs/"+b.name+"_Interior62.prefab";var g=PrefabUtility.LoadPrefabContents(path);try{Adjust(g.transform,b.name=="BlackMarket");PrefabUtility.SaveAsPrefabAsset(g,path);}finally{PrefabUtility.UnloadPrefabContents(g);}}
  return "Saved floor separation, rugs and clear door approaches";
 }
}
