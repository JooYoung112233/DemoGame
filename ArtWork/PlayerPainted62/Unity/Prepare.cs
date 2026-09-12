using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PlayerPaintPrepare {
 public static string Run(){if(!Application.isPlaying)throw new Exception("Play required");SceneTransitionManager.Instance.StartCoroutine(Ready());return "Preparing Safehouse preview";}
 static IEnumerator Ready(){
 UIManager.Instance.CloseAll();if(TitleScreen.Instance!=null)typeof(TitleScreen).GetMethod("Hide",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(TitleScreen.Instance,null);
 Time.timeScale=1;if(!SceneManager.GetSceneByName("Safehouse").isLoaded)SceneTransitionManager.Instance.TransitionTo("Safehouse","default");yield return new WaitForSecondsRealtime(2.5f);
 var p=TopDownPlayer.Instance;var rb=p.GetComponent<Rigidbody>();rb.position=new Vector3(52,.02f,12);p.transform.position=rb.position;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();
 }
}
