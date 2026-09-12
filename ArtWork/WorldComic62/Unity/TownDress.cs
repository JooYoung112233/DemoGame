using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;using Newtonsoft.Json;
public static class TownComicDress {
 const string Folder="Assets/Art/WorldComic62";
 static Scene scene;static Transform root;static List<object> rows=new();
 public static string Run(){
 if(Application.isPlaying)throw new Exception("Stop play");scene=SceneManager.GetSceneByName("Safehouse");if(!scene.isLoaded||scene.isDirty)throw new Exception("Clean loaded Safehouse required");
 var map=scene.GetRootGameObjects().Single(g=>g.name=="Map");if(map.transform.Find("TownLife_Comic62")!=null)throw new Exception("Already placed");
 var originalColliders=map.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
 var originalBehaviours=map.GetComponentsInChildren<MonoBehaviour>(true).Where(x=>x!=null).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
 var main=new GameObject("TownLife_Comic62");SceneManager.MoveGameObjectToScene(main,scene);main.transform.SetParent(map.transform,false);root=main.transform;
 try{
 Group("Repair_Workyard");
 Put("Workbench01",17,36,180,1,true,"Hideout");Put("Bicycle02",18.2f,34.1f,-20);Put("Toolbox02",15.1f,35.9f,15);Put("ScrapHeap02",16,33.9f,-8);Put("FireExtinguisher02",18.8f,37.1f,0);Put("Ladder02",18.9f,36,90);
 Group("Medical_SupplyWash");
 Put("MedicalBox01",68,37,0,1,true,"Hideout");Put("SupplyCase02",69.1f,37,4,1,true,"Hideout");Put("UtilitySink02",70.4f,35.9f,180);Put("WaterBarrelStand02",72.2f,35.9f,0);Put("Bucket02",70.7f,34.5f,12);Put("CardboardStack02",67.8f,35.5f,-5);
 Group("Home_LaundryKitchen");
 Put("Clothesline02",70,12,0);Put("LaundryBasket02",68,10.8f,-12);Put("UtilitySink02",69,8.1f,180);Put("OutdoorStove02",71,8.1f,180);Put("WaterBarrelStand02",72.5f,9,0);Put("Bucket02",68.2f,8.6f,8);Put("RopeCoil02",71.7f,10.4f,20);Put("Planter02",70,5.4f,0);
 Group("Community_RestCorner");
 Put("SpoolTable02",44,23,0);Put("YardChair02",42.7f,23,-70);Put("YardChair02",45.2f,22.2f,145);Put("YardChair02",44.5f,24.3f,10);Put("Planter02",46.3f,24.6f,90);
 Group("Furniture_Display");
 Put("SpoolTable02",23,22.4f,5);Put("YardChair02",21.5f,23.6f,65);Put("YardChair02",24.3f,22.4f,-70);Put("FoldedTarp02",22.8f,24.3f,-9);Put("Planter02",21.3f,20.5f,0);
 Group("BlackMarket_Unloading");
 Put("MerchantCart02",25,7.5f,-12);Put("FoldedTarp02",23,6,8);Put("CardboardStack02",24.4f,5.3f,-10);Put("Toolbox02",26.5f,6.1f,0);Put("RopeCoil02",27,8.2f,15);
 Group("West_ServiceAlley");
 Put("Dumpster02",3,35,0);Put("GarbageBags02",4.8f,33.8f,25);Put("CardboardStack02",3.1f,32.9f,-12);
 foreach(var pair in originalColliders)if(pair.Key==null||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Existing collider changed");
 foreach(var pair in originalBehaviours)if(pair.Key==null||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Existing behaviour changed");
 int missing=main.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));if(missing!=0)throw new Exception("Missing scripts");
 Directory.CreateDirectory(Folder+"/Prefabs");AssetDatabase.Refresh();
 PrefabUtility.SaveAsPrefabAsset(main,Folder+"/Prefabs/TownLife_Comic62.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText(Path.GetFullPath("../ArtWork/WorldComic62/Unity/TownPlacement.json"),JsonConvert.SerializeObject(new{props=rows,originalColliderCount=originalColliders.Count,originalBehaviourCount=originalBehaviours.Count,existingGameplayPreserved=true,missingScripts=missing},Formatting.Indented));
 return $"Saved {rows.Count} prop placements in 7 clusters; existing gameplay and colliders unchanged";
 }catch{UnityEngine.Object.DestroyImmediate(main);throw;}
 }
 static void Group(string name){var g=new GameObject(name);g.transform.SetParent(root.parent==null?root:root.name=="TownLife_Comic62"?root:root.parent,false);root=g.transform;}
 static void Put(string name,float x,float z,float yaw,float scale=1,bool solid=true,string kit="Town"){
 string path=kit=="Hideout"?$"Assets/Art/Environments/Hideout02/Prefabs/{name}.prefab":$"Assets/Art/Environments/TownProps02/Models/{name}.fbx";
 if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)path=$"Assets/Art/Environments/Town02/Models/{name}.fbx";
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new Exception("Missing prop "+path);
 var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);g.name=name;g.transform.SetParent(root,false);g.transform.rotation=Quaternion.Euler(0,yaw,0)*asset.transform.localRotation;g.transform.localScale*=scale;
 foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
 var rs=g.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 g.transform.position+=new Vector3(x-b.center.x,.055f-b.min.y,z-b.center.z);
 b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 if(solid&&b.size.y>.4f){var hit=new GameObject("Collision");hit.transform.SetParent(g.transform,false);hit.transform.rotation=Quaternion.identity;hit.transform.position=b.center;var c=hit.AddComponent<BoxCollider>();var ls=hit.transform.lossyScale;c.size=new Vector3(b.size.x*.88f/ls.x,b.size.y/ls.y,b.size.z*.88f/ls.z);}
 rows.Add(new{group=root.name,name,path,position=new[]{g.transform.position.x,g.transform.position.y,g.transform.position.z},center=new[]{b.center.x,b.center.y,b.center.z},size=new[]{b.size.x,b.size.y,b.size.z},yaw});
 }
}
