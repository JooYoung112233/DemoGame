using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
public static class StartupSpawnValidate
{
    public static string Run(){SceneTransitionManager.Instance.StartCoroutine(Test());return "Verifying fresh startup and Hideout-to-Safehouse spawn persistence";}
    static IEnumerator Test()
    {
        var p=TopDownPlayer.Instance;var rb=p.GetComponent<Rigidbody>();
        var origin=p.transform.position;bool virt=GameInput.Virtual,suppress=SaveManager.SuppressWrites;
        var checks=new Dictionary<string,bool>();var details=new List<object>();
        GameInput.Virtual=true;GameInput.VSetMove(Vector2.zero);SaveManager.SuppressWrites=true;
        Vector3 Default()=>Object.FindObjectsByType<SpawnPoint>().Single(s=>s.gameObject.scene.name=="Safehouse"&&s.PointId=="default").transform.position;
        try
        {
            checks["fresh_start_at_default"]=Vector3.Distance(origin,Default())<.03f;
            checks["fresh_start_body_matches"]=Vector3.Distance(rb.position,Default())<.03f;
            rb.linearVelocity=new Vector3(3,0,2);
            SpawnPoint.PlacePlayer(p.gameObject,Default());
            checks["spawn_clears_velocity"]=rb.linearVelocity.sqrMagnitude<.0001f;
            yield return new WaitForSecondsRealtime(.3f);
            checks["spawn_survives_physics"]=Vector3.Distance(p.transform.position,Default())<.03f;
            var stm=SceneTransitionManager.Instance;
            stm.TransitionTo("Hideout","default");
            float deadline=Time.realtimeSinceStartup+10;
            while(stm.IsTransitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            checks["hideout_transition_completed"]=!stm.IsTransitioning&&SceneManager.GetSceneByName("Hideout").isLoaded;
            if(!checks["hideout_transition_completed"])throw new System.Exception("Hideout transition timeout");
            stm.TransitionTo("Safehouse","default");deadline=Time.realtimeSinceStartup+10;
            while(stm.IsTransitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            checks["safehouse_transition_completed"]=!stm.IsTransitioning&&SceneManager.GetSceneByName("Safehouse").isLoaded;
            checks["return_transform_at_default"]=Vector3.Distance(p.transform.position,Default())<.03f;
            checks["return_body_at_default"]=Vector3.Distance(rb.position,Default())<.03f;
            details.Add(new{initial=origin.ToString(),returned=p.transform.position.ToString(),body=rb.position.ToString(),expected=Default().ToString()});
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("../ArtWork/StartupSpawn/StartAtHome.png"));yield return null;
            File.WriteAllText(Path.GetFullPath("../ArtWork/StartupSpawn/Validation.json"),JsonConvert.SerializeObject(new{pass=checks.Values.All(x=>x),checks,details},Formatting.Indented));
        }
        finally{GameInput.Virtual=virt;SaveManager.SuppressWrites=suppress;}
    }
}
