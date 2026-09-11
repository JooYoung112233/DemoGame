if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop play before scene placement");
const string folder="Assets/Art/Environments/Safehouse01";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Hideout.unity");
if(!scene.isLoaded)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Hideout.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
var map=scene.GetRootGameObjects().Single(g=>g.name=="Map");
if(map.transform.Find("Safehouse01_Placed")!=null)throw new System.Exception("Already placed; inspect before applying again");
var original=map.GetComponentsInChildren<HideoutFacilityAnchor>(true).ToDictionary(a=>a.moduleKey);
if(original.Count!=9)throw new System.Exception("Unexpected facility set; preserve and inspect");
int undo=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Place modular safehouse at original 6 x 4 metre scale");
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/Safehouse01_Cutaway.prefab");
var kit=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);
UnityEditor.Undo.RegisterCreatedObjectUndo(kit,"Place safehouse");
UnityEditor.PrefabUtility.UnpackPrefabInstance(kit,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.AutomatedAction);
kit.name="Safehouse01_Placed";kit.transform.SetParent(map.transform,false);kit.transform.position=new UnityEngine.Vector3(7,0,4.5f);kit.transform.rotation=UnityEngine.Quaternion.Euler(0,215,0);
// Blender positions to imported model space; rotate only the assembled room for the game's camera.
System.Func<float,float,float,UnityEngine.Vector3> location=(x,y,z)=>kit.transform.TransformPoint(new UnityEngine.Vector3(-x,z,-y));
var furniture=kit.transform.Find("Furniture");var small=kit.transform.Find("SmallProps");
System.Func<string,UnityEngine.Transform> find=name=>kit.GetComponentsInChildren<UnityEngine.Transform>(true).First(t=>t.name==name&&t.parent!=null&&(t.parent==furniture||t.parent==small));
var stool=find("Stool01");stool.position=location(.95f,-.20f,0);
var chest=find("StorageChest01");chest.localRotation*=UnityEngine.Quaternion.Euler(0,180,0);
// The front-left dressing crate gives its footprint to the temporary cooking bench.
foreach(UnityEngine.Transform t in small)if(t.name=="WoodCrate01"&&t.localPosition.z>0)t.gameObject.SetActive(false);
var cooking=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/Prefabs/Workbench01.prefab"),scene);
cooking.name="CookingBench_Temporary";cooking.transform.SetParent(furniture,false);cooking.transform.position=location(-2f,-1.63f,0);cooking.transform.localRotation=UnityEngine.Quaternion.Euler(0,180,0);
foreach(var t in cooking.GetComponentsInChildren<UnityEngine.Transform>(true))if(t.name.StartsWith("Vise_")||t.name.StartsWith("Rack_")||t.name.StartsWith("Tool_"))t.gameObject.SetActive(false);
var targets=new System.Collections.Generic.Dictionary<string,UnityEngine.Transform>{
 {"bed",find("Cot01")},{"radio",find("Radio01")},{"workbench",find("Workbench01")},{"stash",chest},
 {"medical",find("MedicalBox01")},{"cooking",cooking.transform},{"dispatch",find("MapBoard01")},{"generator",find("Generator01")},{"idle",stool}
};
var stands=new System.Collections.Generic.Dictionary<string,UnityEngine.Vector3>{
 {"bed",location(-1.12f,.05f,0)},{"radio",location(-.55f,.70f,0)},{"workbench",location(.25f,.70f,0)},
 {"stash",location(-.73f,-.62f,0)},{"medical",location(2.08f,1.0f,0)},{"cooking",location(-2f,-.94f,0)},
 {"dispatch",location(.30f,.62f,0)},{"generator",location(1.72f,-.72f,0)},{"idle",location(.55f,-.38f,0)}
};
foreach(var pair in targets){
 var old=original[pair.Key];var go=pair.Value.gameObject;
 var io=go.AddComponent<InteractableObject>();UnityEditor.EditorUtility.CopySerialized(old.GetComponent<InteractableObject>(),io);
 var anchor=go.AddComponent<HideoutFacilityAnchor>();UnityEditor.EditorUtility.CopySerialized(old,anchor);
 var stand=new UnityEngine.GameObject("FacilityStand");stand.transform.SetParent(go.transform,false);stand.transform.position=stands[pair.Key];anchor.standPoint=stand.transform;
 // Current controller has standing idle but no sitting/lying clips: keep feet on the floor beside furniture.
 anchor.pose=HideoutFacilityAnchor.Pose.Stand;
 if(pair.Key!="idle"){
  var label=go.AddComponent<FacilityLabel>();var oldLabel=old.GetComponent<FacilityLabel>();if(oldLabel!=null)UnityEditor.EditorUtility.CopySerialized(oldLabel,label);
  label.worldSize=.022f;
  var rs=go.GetComponentsInChildren<UnityEngine.Renderer>();if(rs.Length>0){float highest=rs.Max(r=>r.bounds.max.y);label.heightOffset=highest-rs[0].bounds.max.y+.10f;}
 }
}
foreach(var old in original.Values)UnityEditor.Undo.DestroyObjectImmediate(old.gameObject);
// Keep old shell objects for rollback; they contain no facility scripts.
foreach(UnityEngine.Transform t in map.transform){
 if(t.name=="Floor"||t.name.StartsWith("Wall_")||t.name.StartsWith("Rib_")){UnityEditor.Undo.RecordObject(t.gameObject,"Hide original greybox");t.gameObject.SetActive(false);}
}
var focus=new UnityEngine.GameObject("RoomCenter_ModelReview");UnityEditor.Undo.RegisterCreatedObjectUndo(focus,"Room framing");focus.transform.SetParent(map.transform,false);focus.transform.position=new UnityEngine.Vector3(7,.68f,4.5f);
var dio=map.GetComponentInChildren<HideoutDiorama>();var ds=new UnityEditor.SerializedObject(dio);ds.FindProperty("roomCenter").objectReferenceValue=focus.transform;ds.FindProperty("roomOrthoSize").floatValue=3.6f;ds.ApplyModifiedProperties();
var tuning=UnityEditor.AssetDatabase.LoadAssetAtPath<GameTuning>("Assets/Resources/Data/GameTuning.asset");UnityEditor.Undo.RecordObject(tuning,"Match hideout camera to 6 x 4 room");
var ts=new UnityEditor.SerializedObject(tuning);ts.FindProperty("hideoutRoomOrtho").floatValue=3.6f;ts.FindProperty("hideoutRoomHalfX").floatValue=.42f;ts.FindProperty("hideoutRoomHalfY").floatValue=.42f;ts.ApplyModifiedProperties();
var spawn=map.GetComponentInChildren<SpawnPoint>();UnityEditor.Undo.RecordObject(spawn.transform,"Place hideout arrival");spawn.transform.position=stands["idle"];
var bulb=map.transform.Find("Bulb");if(bulb!=null){UnityEditor.Undo.RecordObject(bulb.GetComponent<UnityEngine.Light>(),"Room lighting");bulb.GetComponent<UnityEngine.Light>().intensity=.5f;bulb.GetComponent<UnityEngine.Light>().range=6;bulb.position=new UnityEngine.Vector3(7,3.1f,4.5f);}
foreach(var lamp in kit.GetComponentsInChildren<UnityEngine.Light>()){lamp.intensity=.35f;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);}
UnityEngine.Physics.SyncTransforms();
var report=targets.Select(p=>new{key=p.Key,model=p.Value.name,position=p.Value.position.ToString(),stand=stands[p.Key].ToString(),interaction=p.Value.GetComponent<InteractableObject>().Type.ToString()}).ToArray();
UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(scene);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();UnityEditor.Undo.CollapseUndoOperations(undo);
UnityEditor.Selection.activeGameObject=kit;
if(UnityEditor.SceneView.lastActiveSceneView!=null)UnityEditor.SceneView.lastActiveSceneView.LookAt(focus.transform.position,UnityEngine.Quaternion.Euler(55,0,0),5);
System.IO.File.WriteAllText(folder+"/ScenePlacement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,roomSize="6 x 4 metres",scale=kit.transform.localScale.ToString(),facilities=report},Newtonsoft.Json.Formatting.Indented));
return report;
