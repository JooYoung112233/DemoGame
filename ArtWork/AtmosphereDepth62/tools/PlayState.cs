using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class AtmosphereDepthPlayState
{
    static string PathName=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/AtmosphereDepth62/PlayState.json"));
    public static string Save()
    {
        var player=UnityEngine.Object.FindAnyObjectByType<TopDownPlayer>();
        var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
        if(player==null||cycle==null)throw new Exception("Player/cycle missing");
        var time=RegionTimeManager.Instance;var region=time==null?null:time.GetRegion(time.ActiveRegionId);
        var pos=player.transform.position;var rot=player.transform.eulerAngles;
        string json=JsonConvert.SerializeObject(new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,x=pos.x,y=pos.y,z=pos.z,yaw=rot.y,night=cycle.IsNight,regionId=time==null?null:time.ActiveRegionId,elapsed=region==null?0:region.elapsed},Formatting.Indented);
        File.WriteAllText(PathName,json);return json;
    }
    public static string Restore()
    {
        var data=JObject.Parse(File.ReadAllText(PathName));
        var player=UnityEngine.Object.FindAnyObjectByType<TopDownPlayer>();
        var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
        var pos=new Vector3((float)data["x"],(float)data["y"],(float)data["z"]);
        var rot=Quaternion.Euler(0,(float)data["yaw"],0);
        player.transform.SetPositionAndRotation(pos,rot);
        if(player.TryGetComponent<Rigidbody>(out var body)){body.position=pos;body.rotation=rot;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        cycle.SetNight((bool)data["night"]);
        var time=RegionTimeManager.Instance;var region=time==null?null:time.GetRegion(time.ActiveRegionId);
        if((time==null?null:time.ActiveRegionId)!=(string)data["regionId"])throw new Exception("Region differs from saved play");
        if(region!=null)region.elapsed=(float)data["elapsed"];
        return "Player position, facing and region time restored; interaction state is not restored.";
    }
}
