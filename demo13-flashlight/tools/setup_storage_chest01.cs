const string folder="Assets/Art/Props/StorageChest01";
const string fbx=folder+"/StorageChest01.fbx";
UnityEditor.AssetDatabase.ImportAsset(fbx,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
if(!UnityEditor.AssetDatabase.IsValidFolder(folder+"/Materials"))UnityEditor.AssetDatabase.CreateFolder(folder,"Materials");
var shader=UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
if(shader==null)throw new System.Exception("URP Lit shader missing");
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
importer.importCameras=false;importer.importLights=false;importer.importAnimation=true;
importer.animationType=UnityEditor.ModelImporterAnimationType.Generic;
importer.avatarSetup=UnityEditor.ModelImporterAvatarSetup.NoAvatar;
importer.optimizeGameObjects=false;importer.isReadable=true;
var palette=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/Palette.json"));
foreach(var entry in palette.Properties()){
 string name="Chest_"+entry.Name,path=folder+"/Materials/"+name+".mat";
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
 if(m==null){m=new UnityEngine.Material(shader);UnityEditor.AssetDatabase.CreateAsset(m,path);}
 m.shader=shader;m.name=name;var c=entry.Value;
 m.SetColor("_BaseColor",new UnityEngine.Color((float)c[0],(float)c[1],(float)c[2],1).gamma);
 m.SetFloat("_Smoothness",entry.Name=="Hardware"||entry.Name=="Pin"?.33f:.17f);
 m.SetFloat("_Metallic",entry.Name=="Hardware"||entry.Name=="Pin"?.45f:0);
 UnityEditor.EditorUtility.SetDirty(m);
 importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),name),m);
}
importer.SaveAndReimport();
var defaults=importer.defaultClipAnimations;
if(defaults.Length!=1)throw new System.Exception("Expected one source take, got "+defaults.Length);
var source=defaults[0];float first=source.firstFrame;
importer.clipAnimations=new[]{
 new UnityEditor.ModelImporterClipAnimation{name="Closed",takeName=source.takeName,firstFrame=first,lastFrame=first,loopTime=false},
 new UnityEditor.ModelImporterClipAnimation{name="Open",takeName=source.takeName,firstFrame=first,lastFrame=first+41,loopTime=false},
 new UnityEditor.ModelImporterClipAnimation{name="Opened",takeName=source.takeName,firstFrame=first+49,lastFrame=first+49,loopTime=false},
 new UnityEditor.ModelImporterClipAnimation{name="Close",takeName=source.takeName,firstFrame=first+57,lastFrame=first+99,loopTime=false}
};
importer.SaveAndReimport();
var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToDictionary(c=>c.name);
foreach(var name in new[]{"Closed","Open","Opened","Close"})if(!clips.ContainsKey(name))throw new System.Exception("Missing clip: "+name);
string controllerPath=folder+"/StorageChest01.controller";
var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if(controller==null){
 controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("IsOpen",UnityEngine.AnimatorControllerParameterType.Bool);
 var sm=controller.layers[0].stateMachine;
 var closed=sm.AddState("Closed");closed.motion=clips["Closed"];sm.defaultState=closed;
 var opening=sm.AddState("Opening");opening.motion=clips["Open"];
 var opened=sm.AddState("Opened");opened.motion=clips["Opened"];
 var closing=sm.AddState("Closing");closing.motion=clips["Close"];
 var t=closed.AddTransition(opening);t.hasExitTime=false;t.duration=0;t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"IsOpen");
 t=opening.AddTransition(opened);t.hasExitTime=true;t.exitTime=1;t.duration=0;
 t=opened.AddTransition(closing);t.hasExitTime=false;t.duration=0;t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot,0,"IsOpen");
 t=closing.AddTransition(closed);t.hasExitTime=true;t.exitTime=1;t.duration=0;
}
var previewScene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
object report=null;
try{
 var wrapper=new UnityEngine.GameObject("StorageChest01");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wrapper,previewScene);
 var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx);
 var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,previewScene);instance.transform.SetParent(wrapper.transform,false);
 var animator=instance.GetComponent<UnityEngine.Animator>();if(animator==null)animator=instance.AddComponent<UnityEngine.Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
 clips["Closed"].SampleAnimation(instance,0);
 var pivot=instance.GetComponentsInChildren<UnityEngine.Transform>(true).Single(t=>t.name=="Lid_Pivot");
 var closedRotation=pivot.localRotation;var closedPosition=pivot.position;
 clips["Open"].SampleAnimation(instance,clips["Open"].length);
 float openingAngle=UnityEngine.Quaternion.Angle(closedRotation,pivot.localRotation);
 float pivotDrift=UnityEngine.Vector3.Distance(closedPosition,pivot.position);
 clips["Close"].SampleAnimation(instance,clips["Close"].length);
 float closeError=UnityEngine.Quaternion.Angle(closedRotation,pivot.localRotation);
 clips["Closed"].SampleAnimation(instance,0);
 if(openingAngle<100||openingAngle>110||pivotDrift>.001f||closeError>.1f)throw new System.Exception("Hinge validation failed: "+openingAngle+" / "+pivotDrift+" / "+closeError);
 var renderers=instance.GetComponentsInChildren<UnityEngine.MeshRenderer>();
 var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
 if(bounds.size.x<.8f||bounds.size.x>1.0f||bounds.size.y<.4f||bounds.size.y>.5f)throw new System.Exception("Unexpected model scale: "+bounds.size);
 foreach(var r in renderers)foreach(var m in r.sharedMaterials)if(m==null||m.shader!=shader)throw new System.Exception("Unmapped material: "+r.name);
 // Hollow collision shell; no invisible box across the opening.
 var colRoot=new UnityEngine.GameObject("Body_Colliders");colRoot.transform.SetParent(wrapper.transform,false);
 foreach(var data in new[]{new[]{0f,.047f,0f,.79f,.05f,.45f},new[]{0f,.205f,.220f,.80f,.28f,.020f},new[]{0f,.205f,-.220f,.80f,.28f,.020f},new[]{.39f,.205f,0f,.020f,.28f,.42f},new[]{-.39f,.205f,0f,.020f,.28f,.42f}}){
  var col=colRoot.AddComponent<UnityEngine.BoxCollider>();col.center=new UnityEngine.Vector3(data[0],data[1],data[2]);col.size=new UnityEngine.Vector3(data[3],data[4],data[5]);
 }
 var lidMesh=instance.GetComponentsInChildren<UnityEngine.MeshFilter>().Single(m=>m.name=="Lid_SteelShell");
 var lidCol=lidMesh.gameObject.AddComponent<UnityEngine.BoxCollider>();lidCol.center=lidMesh.sharedMesh.bounds.center;lidCol.size=lidMesh.sharedMesh.bounds.size;
 var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(wrapper,folder+"/StorageChest01.prefab");
 if(prefab==null)throw new System.Exception("Prefab save failed");
 report=new {prefab=folder+"/StorageChest01.prefab",meshes=renderers.Length,openingAngle,pivotDrift,closeError,size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},clips=clips.Keys.ToArray(),materialsValidated=true};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(previewScene);}
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllText(folder+"/UnityValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
