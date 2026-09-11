// Apply only the hideout art. Reuses existing interaction data, module keys and dock settings.
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Editor is playing; apply the art when editing is available.");
const string path="Assets/Scenes/Hideout.unity",folder="Assets/Art/Environments/Hideout02";
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
if(!opened&&scene.isDirty)throw new System.Exception("Hideout has unsaved edits; preserve them before applying the art pass.");
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var old=all.Single(t=>t.name=="Safehouse01_Placed"||t.name=="Hideout02_Placed");
 var originals=old.GetComponentsInChildren<HideoutFacilityAnchor>().ToDictionary(a=>a.moduleKey);
 if(originals.Count!=9)throw new System.Exception("Expected all nine facility anchors before replacing art.");
 var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/KitManifest.json"));
 var kit=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Hideout02_Interior.prefab"),scene);
 UnityEditor.PrefabUtility.UnpackPrefabInstance(kit,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.AutomatedAction);
 kit.name="Hideout02_Placed";kit.transform.SetParent(old.parent,false);kit.transform.position=old.position;kit.transform.rotation=Quaternion.Euler(0,180,0);
 var reports=new System.Collections.Generic.List<object>();
 foreach(var spec in manifest["placements"].Where(p=>(string)p["key"]!=null)){
  string key=(string)spec["key"],name=(string)spec["asset"],group=(string)spec["group"];
  var target=kit.transform.Find(group+"/"+name);var prior=originals[key];
  var io=target.gameObject.AddComponent<InteractableObject>();UnityEditor.EditorUtility.CopySerialized(prior.GetComponent<InteractableObject>(),io);
  var anchor=target.gameObject.AddComponent<HideoutFacilityAnchor>();UnityEditor.EditorUtility.CopySerialized(prior,anchor);anchor.standPoint=target.Find("FacilityStand");
  if(anchor.standPoint==null)throw new System.Exception("Missing stand point "+key);
  var label=prior.GetComponent<FacilityLabel>();
  if(label!=null){var l=target.gameObject.AddComponent<FacilityLabel>();UnityEditor.EditorUtility.CopySerialized(label,l);l.heightOffset=.12f;l.worldSize=.022f;}
  reports.Add(new{key,model=name,stand=anchor.StandPosition.ToString(),interaction=io.Type.ToString()});
 }
 var dio=all.Select(t=>t.GetComponent<HideoutDiorama>()).Single(d=>d!=null);
 var focus=all.FirstOrDefault(t=>t.name=="RoomCenter_ModelReview");
 if(focus==null){focus=new GameObject("RoomCenter_ModelReview").transform;focus.SetParent(dio.transform,false);}
 focus.position=kit.transform.position+Vector3.up*.52f;
 var ds=new UnityEditor.SerializedObject(dio);ds.FindProperty("roomCenter").objectReferenceValue=focus;ds.FindProperty("roomOrthoSize").floatValue=3.6f;ds.ApplyModifiedPropertiesWithoutUndo();
 var spawn=all.Select(t=>t.GetComponent<SpawnPoint>()).FirstOrDefault(s=>s!=null);
 if(spawn!=null)spawn.transform.position=kit.GetComponentsInChildren<HideoutFacilityAnchor>().Single(a=>a.moduleKey=="idle").StandPosition;
 var bulb=all.FirstOrDefault(t=>t.name=="Bulb");if(bulb!=null){var l=bulb.GetComponent<Light>();if(l!=null){l.intensity=.7f;l.range=8;l.color=new Color(1,.82f,.64f);bulb.position=kit.transform.position+new Vector3(-.5f,3.2f,-.5f);}}
 UnityEngine.Object.DestroyImmediate(old.gameObject);
 // Geometric stand-point validation in room-local coordinates; ignores the floor and low rug.
 var anchors=kit.GetComponentsInChildren<HideoutFacilityAnchor>();
 foreach(var a in anchors){
  var local=kit.transform.InverseTransformPoint(a.StandPosition);if(Mathf.Abs(local.x)>2.85f||Mathf.Abs(local.z)>2.85f)throw new System.Exception("Stand outside interior: "+a.moduleKey);
  var sample=a.StandPosition+Vector3.up*.8f;
  foreach(var col in kit.GetComponentsInChildren<BoxCollider>()){
   if(!col.enabled)continue;var q=col.transform.InverseTransformPoint(sample)-col.center;var e=col.size*.5f;
   if(Mathf.Abs(q.x)<e.x&&Mathf.Abs(q.y)<e.y&&Mathf.Abs(q.z)<e.z)throw new System.Exception("Stand point obstructed: "+a.moduleKey+" by "+col.name);
  }
 }
 if(anchors.Length!=9||anchors.Select(a=>a.moduleKey).Distinct().Count()!=9)throw new System.Exception("Facility set changed.");
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Hideout save failed.");
 var report=new{scene=path,roomSize="6 x 6 metres",roomYaw=180,facilities=reports,standPointsClear=true,activeScenePreserved=active==UnityEngine.SceneManagement.SceneManager.GetActiveScene(),combatAssetsUnchanged=true};
 System.IO.File.WriteAllText(folder+"/SceneValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(active);}
