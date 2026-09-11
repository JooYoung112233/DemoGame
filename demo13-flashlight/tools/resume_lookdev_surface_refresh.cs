if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Expected edit mode");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new System.Exception("Preserve unsaved scene edits first");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MapTool_LookDev.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
var state=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/hero-lookdev-return.json"));
UnityEditor.EditorApplication.isPlaying=(bool)state["playing"];return "Saved LookDev refreshed";
