var state=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/hero-lookdev-return.json"));
if((bool)state["playing"]){
 var sw=UnityEngine.Object.FindFirstObjectByType<LookDevCameraSwitcher>();if(sw==null)throw new System.Exception("LookDev not ready");
 sw.Select((int)state["index"]);sw.SetNight((bool)state["night"]);
}
return new{playing=UnityEditor.EditorApplication.isPlaying,camera=(int)state["index"],night=(bool)state["night"]};
