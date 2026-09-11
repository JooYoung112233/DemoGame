using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

public static class BackpackValidate
{
    public static string Run()
    {
        var lamp=UnityEngine.Object.FindAnyObjectByType<WornLamp>();var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
        var so=new SerializedObject(lamp);var lights=lamp.GetComponentsInChildren<Light>();
        var spill=lights.Single(l=>l.name=="LampSpill");var glow=lights.Single(l=>l.name=="BodyGlow");var beam=lamp.GetComponent<Light>();
        float originalGlow=glow.intensity;bool prior=cycle.IsNight;
        var late=typeof(WornLamp).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        bool near(float a,float b)=>Mathf.Abs(a-b)<.0001f;
        bool day=false,night=false,indoor=false,grade=false,height=false,newLamp=false;
        GameObject probe=null;
        try{
            cycle.SetNight(false);day=near(spill.intensity,.08f)&&near(beam.intensity,.2f);
            cycle.SetNight(true);night=near(spill.intensity,.136f)&&near(beam.intensity,3.4f);
            lamp.SetIndoorPresentation(true);indoor=near(spill.intensity,.0204f)&&near(beam.intensity,.51f);lamp.SetIndoorPresentation(false);
            lamp.SetGrade(2,2);grade=near(spill.intensity,.272f)&&near(beam.intensity,6.8f)&&near(spill.range,4.8f);lamp.SetGrade(1,1);
            late.Invoke(lamp,null);height=near(spill.transform.position.y-lamp.transform.position.y,-.65f)&&near(spill.transform.position.x,lamp.transform.position.x)&&near(spill.transform.position.z,lamp.transform.position.z);
            // Exercise initialization on a newly created lamp; the live player still exists.
            probe=new GameObject("BackpackLampProbe");var test=probe.AddComponent<WornLamp>();test.ForcePhase(true);
            late.Invoke(test,null);var ps=test.GetComponentsInChildren<Light>().Single(l=>l.name=="LampSpill");newLamp=near(ps.transform.position.y-test.transform.position.y,-.65f);
        }finally{
            if(probe!=null)UnityEngine.Object.DestroyImmediate(probe);
            lamp.SetIndoorPresentation(false);lamp.SetGrade(1,1);cycle.SetNight(prior);
        }
        bool fields=near(so.FindProperty("spillFloor").floatValue,.08f)&&near(so.FindProperty("spillRatio").floatValue,.04f)&&near(so.FindProperty("spillHeightOffset").floatValue,-.65f);
        var shaderErrors=ShaderUtil.GetShaderMessages(Shader.Find("BRB/GameLit")).Count(m=>m.severity.ToString()=="Error");
        bool pass=fields&&day&&night&&indoor&&grade&&height&&newLamp&&near(glow.intensity,originalGlow)&&shaderErrors==0;
        var result=new {pass,fields,day,night,indoor,grade,worldHeight=height,newLampHeight=newLamp,bodyGlowRestored=near(glow.intensity,originalGlow),shaderErrors};
        var json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/BackpackLight62/Validation.json")),json);
        if(!pass)throw new Exception(json);return json;
    }
}
