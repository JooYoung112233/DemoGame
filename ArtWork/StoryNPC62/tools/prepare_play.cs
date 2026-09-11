if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Already playing");
var safe=UnityEngine.SceneManagement.SceneManager.GetSceneByName("Safehouse");UnityEngine.SceneManagement.SceneManager.SetActiveScene(safe);
return "Safehouse active; saved models ready for runtime validation.";
