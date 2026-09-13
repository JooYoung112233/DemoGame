using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
public static class StartupSpawnProbe
{
    public static string Run(){TopDownPlayer.Instance.StartCoroutine(Test());return "Checking startup spawn across physics ticks";}
    static IEnumerator Test()
    {
        var p=TopDownPlayer.Instance;var rb=p.GetComponent<Rigidbody>();
        var boot=Object.FindAnyObjectByType<GameBoot>();
        var point=Object.FindObjectsByType<SpawnPoint>().Single(s=>s.gameObject.scene.name=="Safehouse"&&s.PointId=="default");
        var before=p.transform.position;bool virt=GameInput.Virtual;GameInput.Virtual=true;GameInput.VSetMove(Vector2.zero);
        try
        {
            typeof(GameBoot).GetMethod("SnapPlayerToSpawn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(boot,null);
            var immediate=p.transform.position;var body=rb.position;
            yield return new WaitForSecondsRealtime(.3f);
            File.WriteAllText(Path.GetFullPath("../ArtWork/StartupSpawn/Probe.json"),JsonConvert.SerializeObject(new{expected=point.transform.position.ToString(),before=before.ToString(),immediate=immediate.ToString(),bodyImmediate=body.ToString(),after=p.transform.position.ToString(),bodyAfter=rb.position.ToString(),pass=Vector3.Distance(p.transform.position,point.transform.position)<.03f},Formatting.Indented));
        }
        finally{GameInput.Virtual=virt;}
    }
}
