if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Use edit mode for persistent NPC replacement.");
const string stage="D:/Demo/ArtWork/StoryNPC62",folder="Assets/ChibiSurvivor/NPC/StoryNPC62";
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(stage+"/NPCManifest.json"));
var rows=manifest["models"].ToDictionary(x=>(string)x["npcId"]);
var changes=new System.Collections.Generic.List<object>();
void Backup(string path){string to=stage+"/UnityIntegration/Before/"+path;System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to));if(!System.IO.File.Exists(to))System.IO.File.Copy(path,to);}
Bounds SkinBounds(GameObject go){var points=new System.Collections.Generic.List<Vector3>();foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=r.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var matrices=r.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();for(int i=0;i<vertices.Length;i++){var w=weights[i];var v=vertices[i];points.Add(matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3);}}if(points.Count==0)throw new System.Exception("No skinned vertices");var bounds=new Bounds(points[0],Vector3.zero);foreach(var v in points)bounds.Encapsulate(v);return bounds;}
void Apply(NPCController npc,bool sceneInstance){
 string id=npc.Data==null?null:npc.Data.npcId;if(id==null||!rows.ContainsKey(id))return;
 var root=npc.transform;var originalData=npc.Data;var interaction=npc.GetComponent<InteractableObject>();var beforeInteraction=UnityEditor.EditorJsonUtility.ToJson(interaction);
 float ground=sceneInstance&&npc.GetComponent<MeshRenderer>()!=null?npc.GetComponent<MeshRenderer>().bounds.min.y:root.position.y;
 foreach(var r in npc.GetComponents<Renderer>()){UnityEditor.Undo.RecordObject(r,"Replace NPC visual");r.enabled=false;}
 var label=root.Find("Label");if(label!=null){UnityEditor.Undo.RecordObject(label.gameObject,"Hide legacy NPC label");label.gameObject.SetActive(false);}
 var previous=root.Find("StoryVisual");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
 var wrapper=new GameObject("StoryVisual");UnityEditor.Undo.RegisterCreatedObjectUndo(wrapper,"Install story NPC");wrapper.transform.SetParent(root,false);
 var scale=root.lossyScale;if(Mathf.Abs(scale.x*scale.y*scale.z)<.00001f)throw new System.Exception("Invalid NPC root scale");
 wrapper.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);wrapper.transform.rotation=Quaternion.Euler(0,180,0);
 string model=System.IO.Path.GetFileNameWithoutExtension((string)rows[id]["fbx"]);
 var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Visuals/"+model+".prefab");
 var visual=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,wrapper.transform);visual.name=model;
 var animator=visual.GetComponent<Animator>();var clip=animator.runtimeAnimatorController.animationClips.Single();clip.SampleAnimation(visual,0);
 var bounds=SkinBounds(visual);if(bounds.size.y<.8f||bounds.size.y>2.5f)throw new System.Exception("NPC height outside expected metres: "+bounds);wrapper.transform.position+=Vector3.up*(ground-bounds.min.y);bounds=SkinBounds(visual);
 // Gameplay capsule remains on the original NPC root. Resource prefabs were 2D
 // placeholders with no 3D blocking volume, so give these a standing capsule.
 if(!sceneInstance&&npc.GetComponent<Collider>()==null){var capsule=npc.gameObject.AddComponent<CapsuleCollider>();capsule.radius=.3f;capsule.height=1.8f;capsule.center=new Vector3(0,.9f,0);}
 var marker=npc.GetComponent<NPCQuestMarker>()??npc.gameObject.AddComponent<NPCQuestMarker>();
 var so=new UnityEditor.SerializedObject(marker);so.FindProperty("heightOffset").floatValue=(bounds.max.y+.22f-root.position.y)/scale.y;so.FindProperty("markerScale").floatValue=.30f;so.ApplyModifiedPropertiesWithoutUndo();
 if(npc.Data!=originalData||UnityEditor.EditorJsonUtility.ToJson(interaction)!=beforeInteraction)throw new System.Exception("NPC gameplay data changed");
 changes.Add(new{id,scene=sceneInstance?root.gameObject.scene.path:"resource prefab",position=root.position.ToString("F3"),ground,boundsMin=bounds.min.ToString("F3"),boundsMax=bounds.max.ToString("F3"),height=bounds.size.y,clip=clip.name,gameplayPreserved=true});
}
foreach(var id in rows.Keys){string path="Assets/Resources/NPC/"+id+".prefab";Backup(path);var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);try{Apply(root.GetComponentInChildren<NPCController>(true),false);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}}
var safe=UnityEngine.SceneManagement.SceneManager.GetSceneByName("Safehouse");if(!safe.IsValid()||!safe.isLoaded)throw new System.Exception("Safehouse must be loaded");
Backup(safe.path);
var sceneNpcs=safe.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NPCController>(true)).Where(n=>n.Data!=null&&rows.ContainsKey(n.Data.npcId)).ToArray();
if(sceneNpcs.Length!=4)throw new System.Exception("Expected four safehouse NPCs");
foreach(var npc in sceneNpcs)Apply(npc,true);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(safe);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(safe);UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllText(stage+"/UnityIntegration/Applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(changes,Newtonsoft.Json.Formatting.Indented));
return Newtonsoft.Json.JsonConvert.SerializeObject(changes);
