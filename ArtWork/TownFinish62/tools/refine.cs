if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Preserve Play");var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("Safehouse");if(scene.isDirty)throw new System.Exception("Preserve unsaved changes");var root=scene.GetRootGameObjects().Single(g=>g.name=="Map").transform.Find("Pawnshop_Forecourt62").gameObject;
var placements=new System.Collections.Generic.List<object>();
foreach(var child in root.transform.Cast<Transform>().ToArray()){
 if(child.name=="Repair_Asphalt"||child.name=="Edge_Aggregate")continue;
 var source=UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);if(source==null)continue;
 float yaw=child.eulerAngles.y;child.rotation=Quaternion.Euler(0,yaw,0)*source.transform.localRotation;
 var rs=child.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);child.position+=Vector3.up*(.052f-b.min.y);
 b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 placements.Add(new{child.name,rotation=child.eulerAngles.ToString(),sourceRotation=source.transform.localEulerAngles.ToString(),center=b.center.ToString(),size=b.size.ToString(),bottom=b.min.y});
}
var lightObject=new GameObject("PorchWarmLight");lightObject.transform.SetParent(root.transform,false);lightObject.transform.position=new Vector3(34,2.25f,37.5f);var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.8f,.56f);light.intensity=2.4f;light.range=4.2f;light.shadows=LightShadows.None;
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,"Assets/Art/Environments/TownFinish62/Prefabs/Pawnshop_Forecourt62.prefab");UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);var json=Newtonsoft.Json.JsonConvert.SerializeObject(placements,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("D:/Demo/ArtWork/TownFinish62/RefinedPlacement.json",json);return json;
