using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using Newtonsoft.Json;
public static class GroundPlaytest {
 public static string Run(){
 if(!Application.isPlaying)throw new Exception("Play mode required");
 SceneTransitionManager.Instance.StartCoroutine(Check());return "Cross paving, walk through hideout trigger, exit via UI started";
 }
 static IEnumerator Check(){
 bool cross=false,entered=false,returned=false;float distance=0;
 try{
 UIManager.Instance.CloseAll();if(TitleScreen.Instance!=null)typeof(TitleScreen).GetMethod("Hide",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(TitleScreen.Instance,null);
 Time.timeScale=1;if(!SceneManager.GetSceneByName("Safehouse").isLoaded)SceneTransitionManager.Instance.TransitionTo("Safehouse","default");yield return new WaitForSecondsRealtime(2.5f);
 var p=TopDownPlayer.Instance;var rb=p.GetComponent<Rigidbody>();
 if(!p.enabled||rb.isKinematic)throw new Exception("Town player not movable");
 GameInput.Virtual=true;
 rb.position=new Vector3(51.8f,.02f,17);p.transform.position=rb.position;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();
 var start=rb.position;GameInput.VSetMove(TopDownPlayer.WorldToInput(Vector2.right));yield return new WaitForSeconds(3.2f);GameInput.VSetMove(Vector2.zero);
 distance=rb.position.x-start.x;cross=distance>3.5f;
 if(!cross)throw new Exception("Paving crossing blocked at "+rb.position);
 rb.position=new Vector3(54.5f,.02f,12);p.transform.position=rb.position;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();
 GameInput.VSetMove(TopDownPlayer.WorldToInput(Vector2.right));float deadline=Time.realtimeSinceStartup+8;
 while(!SceneManager.GetSceneByName("Hideout").isLoaded && Time.realtimeSinceStartup<deadline)yield return null;
 GameInput.VSetMove(Vector2.zero);GameInput.Virtual=false;yield return new WaitForSecondsRealtime(2);
 entered=SceneManager.GetSceneByName("Hideout").isLoaded;if(!entered)throw new Exception("Hideout trigger did not transition");
 var exit=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.name=="ExitButton");exit.onClick.Invoke();yield return null;
 var yes=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.name=="Yes" && b.gameObject.activeInHierarchy);yes.onClick.Invoke();
 deadline=Time.realtimeSinceStartup+8;while(!SceneManager.GetSceneByName("Safehouse").isLoaded && Time.realtimeSinceStartup<deadline)yield return null;
 yield return new WaitForSecondsRealtime(2);
 returned=SceneManager.GetSceneByName("Safehouse").isLoaded && TopDownPlayer.Instance.enabled && !TopDownPlayer.Instance.GetComponent<Rigidbody>().isKinematic;
 }
 finally{
 GameInput.Virtual=false;
 var shader=Shader.Find("BRB/GroundFadeLit");bool shaderErrors=shader==null||ShaderUtil.ShaderHasError(shader);
 File.WriteAllText(Path.GetFullPath("../ArtWork/GroundFinish62/Unity/Playtest.json"),JsonConvert.SerializeObject(new{crossedPaving=cross,distanceMetres=distance,enteredHideout=entered,returnedToTown=returned,shaderErrors,pass=cross&&entered&&returned&&!shaderErrors},Formatting.Indented));
 }
 }
}
