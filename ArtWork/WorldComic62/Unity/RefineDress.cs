using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
public static class TownDressRefine{
 const string Folder="Assets/Art/WorldComic62";
 public static string Run(){
 if(Application.isPlaying)throw new Exception("Stop play");var scene=SceneManager.GetSceneByName("Safehouse");if(scene.isDirty)throw new Exception("Unsaved scene");
 var root=scene.GetRootGameObjects().Single(g=>g.name=="Map").transform.Find("TownLife_Comic62");
 var data=JObject.Parse(File.ReadAllText(Path.GetFullPath("../ArtWork/WorldComic62/Unity/TownPlacement.json")));var used=new HashSet<Transform>();
 foreach(var row in data["props"]){string n=(string)row["name"],group=(string)row["group"],path=(string)row["path"];var parent=root.Find(group);var g=parent.Cast<Transform>().First(t=>t.name==n&&!used.Contains(t));used.Add(g);
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);g.rotation=Quaternion.Euler(0,(float)row["yaw"],0)*asset.transform.localRotation;
 foreach(var hit in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(hit.gameObject.name=="Collision"?hit.gameObject:(UnityEngine.Object)hit);
 var rs=g.GetComponentsInChildren<Renderer>();foreach(var r in rs){r.sharedMaterials=r.sharedMaterials.Select(Map).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 var center=row["center"].Select(t=>(float)t).ToArray();g.position+=new Vector3(center[0]-b.center.x,.055f-b.min.y,center[2]-b.center.z);
 b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 if(b.size.y>.4f){var h=new GameObject("Collision");h.transform.SetParent(g,false);h.transform.rotation=Quaternion.identity;h.transform.position=b.center;var c=h.AddComponent<BoxCollider>();var ls=h.transform.lossyScale;c.size=new Vector3(b.size.x*.88f/ls.x,b.size.y/ls.y,b.size.z*.88f/ls.z);}
 PrefabUtility.RecordPrefabInstancePropertyModifications(g);
 row["position"]=JArray.FromObject(new[]{g.position.x,g.position.y,g.position.z});row["center"]=JArray.FromObject(new[]{b.center.x,b.center.y,b.center.z});row["size"]=JArray.FromObject(new[]{b.size.x,b.size.y,b.size.z});
 }
 // Keep the planter clear of the existing plaza bench.
 var planter=root.Find("Community_RestCorner/Planter02");planter.position+=new Vector3(1.5f,0,-.8f);PrefabUtility.RecordPrefabInstancePropertyModifications(planter);
 PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"/Prefabs/TownLife_Comic62.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText(Path.GetFullPath("../ArtWork/WorldComic62/Unity/TownPlacement.json"),data.ToString());return "Corrected FBX axis, physical scale and town material palette for 38 props";
 }
 static Material Map(Material m){
 string n=m.name;int start=n.IndexOf("TownProps_");if(start>=0)n=n.Substring(start+10);else{start=n.IndexOf("Town_");if(start<0)return m;n=n.Substring(start+5);}
 string target=n;
 if(n.StartsWith("Wood"))target=n=="WoodDark"?"WoodDark":"Wood";
 if(n=="Linen"||n=="BlueCloth")target="Canvas";
 if(n=="Cardboard")target="Paper";
 if(n=="Rust")target="Red";
 if(n=="Rubber"||n=="BagPlastic"||n=="DirtSeam")target="Edge";
 if(n=="Wear")target="Concrete";
 if(n=="Blue")target="Olive";
 var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Town02/Materials/Town_"+target+".mat");return mat??m;
 }
}
