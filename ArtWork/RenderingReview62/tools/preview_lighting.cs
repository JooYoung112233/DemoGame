if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play required");
var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.type==LightType.Directional&&l.name=="Sun3D");sun.color=new Color(1,.90f,.79f);sun.intensity=.8f;DayNightCycle.ApplyAmbient(new Color(.34f,.35f,.37f));
foreach(var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None)){
 if(v.name!="PlayerRig Volume")continue;var p=v.profile;
 if(p.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var c)){c.contrast.Override(10);c.saturation.Override(-8);c.postExposure.Override(0);}
 if(p.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var b)){b.intensity.Override(.18f);b.threshold.Override(1.1f);}
 if(p.TryGet<UnityEngine.Rendering.Universal.FilmGrain>(out var g))g.intensity.Override(.06f);
 if(p.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vgn))vgn.intensity.Override(.20f);
}
var bulb=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l=>l.name=="Bulb"&&l.gameObject.scene.name=="Safehouse");bulb.color=new Color(1,.91f,.80f);bulb.intensity=.8f;bulb.range=9;
return "Runtime-only lighting preview, no assets saved.";
