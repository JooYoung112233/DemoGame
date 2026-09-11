if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var rows=new System.Collections.Generic.List<object>();
foreach(var b in UnityEngine.Object.FindObjectsByType<BuildingInterior>(FindObjectsSortMode.None)){
 if(b.gameObject.scene.name!="Safehouse")continue;
 var roof=b.transform.Find("Roof_Art02");if(roof==null)continue;
 var so=new UnityEditor.SerializedObject(b);var list=so.FindProperty("roof");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=roof.gameObject;
 if(b.name=="Pawnshop") {var interior=b.transform.parent.Find("Pawnshop_Interior");if(interior!=null){var lights=interior.GetComponentsInChildren<Light>(true);var lp=so.FindProperty("interiorLights");lp.arraySize=lights.Length;for(int i=0;i<lights.Length;i++)lp.GetArrayElementAtIndex(i).objectReferenceValue=lights[i];}}
 so.ApplyModifiedPropertiesWithoutUndo();rows.Add(new{building=b.name,roof=roof.name,renderers=roof.GetComponentsInChildren<Renderer>(true).Length});
}
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("Safehouse");UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
// Systems is already open. Keep it active at launch to avoid an extra bootstrap load.
UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.SceneManager.GetSceneByName("Systems"));
System.IO.File.WriteAllText("D:/Demo/ArtWork/StoryNPC62/UnityIntegration/RoofLinks.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
return Newtonsoft.Json.JsonConvert.SerializeObject(rows);
