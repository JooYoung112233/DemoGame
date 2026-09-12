using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Newtonsoft.Json;
public static class GroundPlace {
 const string Dir="Assets/Art/Environments/GroundFinish62";
 static string Out=>Path.GetFullPath("../ArtWork/GroundFinish62/Unity");
 static string Gameplay(GameObject map)=>JsonConvert.SerializeObject(new{
 colliders=map.GetComponentsInChildren<Collider>(true).Select(c=>new{id=c.GetEntityId().ToString(),data=EditorJsonUtility.ToJson(c),pos=c.transform.position.ToString(),scale=c.transform.lossyScale.ToString()}),
 scripts=map.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).Select(c=>new{id=c.GetEntityId().ToString(),data=EditorJsonUtility.ToJson(c)})});
 public static string Run(){
 if(Application.isPlaying)throw new Exception("Placement requires stopped editor");
 var s=SceneManager.GetSceneByName("Safehouse");if(!s.isLoaded||s.isDirty)throw new Exception("Safehouse must be loaded with saved state");
 var map=s.GetRootGameObjects().Single(g=>g.name=="Map");
 if(map.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="HideoutGroundFinish62"))throw new Exception("Already placed");
 string baseline=Gameplay(map);File.WriteAllText(Out+"/GameplayBefore.json",baseline);
 var root=new GameObject("HideoutGroundFinish62");SceneManager.MoveGameObjectToScene(root,s);root.transform.SetParent(map.transform.Find("Environment/Ground")??map.transform,false);
 var placements=new List<object>();
 Action<string,Vector3,float,float> put=(name,pos,yaw,length)=>{
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/Prefabs/"+name+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,s);
 go.transform.SetParent(root.transform,false);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0)*go.transform.rotation;
 if(length!=1)go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(length,1,1));
 var b=go.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in go.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
 if(b.size.magnitude<.3f||b.size.magnitude>10)throw new Exception("Unexpected FBX world size "+name+": "+b.size);
 placements.Add(new{name,position=pos.ToString(),yaw,size=b.size.ToString()});
 };
 // Replace the exposed west paving column; the inner column and its collision stay intact.
 var slabs=map.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Weathered_Slab"&&Mathf.Abs(r.bounds.center.x-54)<.1f&&r.bounds.center.z>12&&r.bounds.center.z<18).ToArray();
 if(slabs.Length!=3)throw new Exception("Expected three west paving slabs, got "+slabs.Length);
 var hidden=new List<string>();foreach(var slab in slabs)foreach(var r in slab.transform.parent.GetComponentsInChildren<Renderer>()){if(r.enabled){hidden.Add(AnimationUtility.CalculateTransformPath(r.transform,map.transform));r.enabled=false;}}
 var mesh=new Mesh{name="InnerPavementJoin"};mesh.vertices=new[]{new Vector3(54,.069f,12),new Vector3(54,.069f,18),new Vector3(55,.069f,18),new Vector3(55,.069f,12)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.uv=new[]{new Vector2(0,0),new Vector2(0,6),new Vector2(1,6),new Vector2(1,0)};mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Dir+"/InnerPavementJoin.asset");
 var join=new GameObject("InnerPavementJoin",typeof(MeshFilter),typeof(MeshRenderer));join.transform.SetParent(root.transform,false);join.GetComponent<MeshFilter>().sharedMesh=mesh;
 join.GetComponent<MeshRenderer>().sharedMaterial=slabs[0].sharedMaterial;var block=new MaterialPropertyBlock();slabs[0].GetPropertyBlock(block);join.GetComponent<MeshRenderer>().SetPropertyBlock(block);join.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
 put("BrokenPaving_A",new Vector3(53.58f,0,13.5f),90,.84f);
 put("BrokenPaving_B",new Vector3(53.58f,0,16.5f),90,.84f);
 put("DirtGravel_Transition",new Vector3(52.98f,.009f,15.95f),90,.85f);
 put("DirtGravel_Transition",new Vector3(53.1f,.009f,13.15f),90,.7f);
 put("CrackBranch_A",new Vector3(51.8f,.007f,15.1f),-22,.8f);
 put("CrackBranch_B",new Vector3(53.25f,.007f,10.6f),37,.62f);
 put("WallWeeds_Dust",new Vector3(56.65f,.05f,15.15f),90,.6f);
 put("WallWeeds_Dust",new Vector3(56.65f,.05f,8.8f),90,.45f);
 if(Gameplay(map)!=baseline)throw new Exception("Existing gameplay/colliders changed");
 if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Unexpected decoration collision");
 PrefabUtility.SaveAsPrefabAsset(root,Dir+"/Prefabs/HideoutGroundFinish62.prefab");
 EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
 File.WriteAllText(Out+"/Placement.json",JsonConvert.SerializeObject(new{scene=s.path,hiddenRenderers=hidden,placements,existingGameplayAndCollidersUnchanged=true,newColliders=0},Formatting.Indented));
 return "Hideout entrance saved: 8 module instances; 3 old slabs replaced; gameplay and colliders unchanged";
 }
}
