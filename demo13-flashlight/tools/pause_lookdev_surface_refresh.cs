var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/MapTool_LookDev.unity")throw new System.Exception("Expected LookDev only");
var sw=UnityEngine.Object.FindFirstObjectByType<LookDevCameraSwitcher>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var state=new{playing=UnityEditor.EditorApplication.isPlaying,index=sw!=null?(int)typeof(LookDevCameraSwitcher).GetField("_idx",flags).GetValue(sw):0,night=sw!=null?(bool)typeof(LookDevCameraSwitcher).GetField("_night",flags).GetValue(sw):false};
System.IO.File.WriteAllText("Library/hero-lookdev-return.json",Newtonsoft.Json.JsonConvert.SerializeObject(state));
UnityEditor.EditorApplication.isPlaying=false;return state;
