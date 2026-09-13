using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
public static class PawnshopWelcome
{
 public static string Run(){
  if(Application.isPlaying)throw new Exception("Stop Play before saving NPC placement");
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/Safehouse.unity");if(!scene.isLoaded||scene.isDirty)throw new Exception("Expected clean, loaded Safehouse");
  var b=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BuildingInterior>(true)).Single(n=>n.name=="Pawnshop");
  var npc=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NPCController>(true)).Single(n=>n.name=="전당포 주인");
  var before=npc.transform.position;var visual=npc.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();visual.BakeMesh(mesh,true);
  float minY=mesh.vertices.Min(v=>visual.transform.TransformPoint(v).y);UnityEngine.Object.DestroyImmediate(mesh);
  // NPC root is the centre of its original capsule, unlike the player's feet pivot.
  // Stand at the front-centre of the displays so they do not hide the character.
  npc.transform.position=b.transform.position+new Vector3(0,.9f,-3.9f);
  EditorUtility.SetDirty(npc.transform);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
  var result=JsonConvert.SerializeObject(new{before=before.ToString(),beforeFeetY=minY,after=npc.transform.position.ToString(),reason="restore capsule centre height and face the entry on the central welcome spot"},Formatting.Indented);
  File.WriteAllText(Path.GetFullPath("../ArtWork/PawnshopWelcome62/Applied.json"),result);return result;
 }
}
