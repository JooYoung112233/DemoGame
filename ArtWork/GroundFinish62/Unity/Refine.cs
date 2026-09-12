using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;
public static class GroundRefine {
 public static string Run(){
 if(Application.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetSceneByName("Safehouse");
 var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="HideoutGroundFinish62");
 foreach(var t in root.Cast<Transform>().Where(t=>t.name.StartsWith("BrokenPaving"))){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/GroundFinish62/Prefabs/"+t.name+".prefab");t.rotation=Quaternion.Euler(0,270,0)*prefab.transform.rotation;var p=t.position;p.y=-.028f;t.position=p;}
 var mf=root.Find("InnerPavementJoin").GetComponent<MeshFilter>();var v=mf.sharedMesh.vertices;for(int i=0;i<v.Length;i++)v[i].y=.044f;mf.sharedMesh.vertices=v;mf.sharedMesh.RecalculateBounds();EditorUtility.SetDirty(mf.sharedMesh);
 var old=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).First(r=>r.name=="Weathered_Slab"&&Mathf.Abs(r.bounds.center.x-56)<.1f&&Mathf.Abs(r.bounds.center.z-13)<.1f);
 var block=new MaterialPropertyBlock();old.GetPropertyBlock(block);Color oldColor=block.HasColor("_BaseColor")?block.GetColor("_BaseColor"):old.sharedMaterial.GetColor("_BaseColor");
 foreach(var key in new[]{"Concrete","ConcreteDark","Stone"}){
 var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/GroundFinish62/Materials/Ground62_"+key+".mat");
 m.SetColor("_BaseColor",oldColor*(key=="Concrete"?.97f:key=="ConcreteDark"?.93f:.83f));m.SetFloat("_GrimeStrength",.06f);EditorUtility.SetDirty(m);
 }
 PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Art/Environments/GroundFinish62/Prefabs/HideoutGroundFinish62.prefab");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 var json=JsonConvert.SerializeObject(new{pavingRootY=-.028f,pavingYaw=270,joinTop=.044f,matchedConcreteBaseColor=oldColor.ToString(),reason="Align with existing 4.5cm paving and expose the drain; chipped side faces plaza"},Formatting.Indented);
 File.WriteAllText(Path.GetFullPath("../ArtWork/GroundFinish62/Unity/Refinement.json"),json);return json;
 }
}
