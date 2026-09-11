// Separate review prefab. No player controller, scene or weapon data is changed.
const string folder="Assets/ChibiSurvivor/Player/AxeChopReview";
const string fbx=folder+"/DarkSurvivor_AxeChop.fbx";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
importer.animationType=UnityEditor.ModelImporterAnimationType.Generic;importer.avatarSetup=UnityEditor.ModelImporterAvatarSetup.CreateFromThisModel;
importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.animationCompression=UnityEditor.ModelImporterAnimationCompression.Off;importer.resampleCurves=false;
importer.SaveAndReimport();
var definitions=importer.defaultClipAnimations;
foreach(var d in definitions){d.name=d.name.Split('|').Last();d.loopTime=false;d.loopPose=false;d.keepOriginalPositionY=true;d.keepOriginalPositionXZ=true;d.keepOriginalOrientation=true;d.lockRootRotation=true;d.lockRootPositionXZ=true;d.lockRootHeightY=true;}
importer.clipAnimations=definitions;
const string surfaceFolder="Assets/ChibiSurvivor/Player/DarkSurvivor/SurfaceReview/Materials";
if(System.IO.Directory.Exists(surfaceFolder))foreach(var path in System.IO.Directory.GetFiles(surfaceFolder,"*.mat")){
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path.Replace('\\','/'));
 importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),"Surface_"+mat.name),mat);
}
foreach(var path in System.IO.Directory.GetFiles("Assets/ChibiSurvivor/Player/DarkSurvivor/Materials","*.mat")){
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path.Replace('\\','/'));
 importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),mat.name),mat);
}
string redPath=folder+"/AxeProxy_Red.mat";var red=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(redPath);
if(red==null){red=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));red.name="AxeProxy_Red";red.SetColor("_BaseColor",new UnityEngine.Color(.24f,.064f,.038f).gamma);red.SetFloat("_Smoothness",.18f);UnityEditor.AssetDatabase.CreateAsset(red,redPath);}
importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),"AxeProxy_Red"),red);importer.SaveAndReimport();
var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
if(clips.Length!=1||clips[0].name!="AxeChop01")throw new System.Exception("Expected only AxeChop01");
var pathController=folder+"/AxeChopReview.controller";var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(pathController);
if(controller==null)controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(pathController);
foreach(var stale in controller.layers[0].stateMachine.states.Where(s=>s.state.name!="AxeChop01").ToArray())controller.layers[0].stateMachine.RemoveState(stale.state);
foreach(var clip in clips){var machine=controller.layers[0].stateMachine;var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==clip.name)??machine.AddState(clip.name);state.motion=clip;machine.defaultState=state;}
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{
 var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx),preview);
 instance.name="DarkSurvivor_AxeChop_Review";var animator=instance.GetComponentInChildren<UnityEngine.Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
 animator.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;
 var bad=instance.GetComponentsInChildren<UnityEngine.Renderer>().Where(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||!m.shader.isSupported)).Select(r=>r.name).ToArray();if(bad.Length>0)throw new System.Exception("Bad materials: "+string.Join(",",bad));
 foreach(var r in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
 var transforms=instance.GetComponentsInChildren<UnityEngine.Transform>();var right=transforms.Single(t=>t.name=="HandSocket.R");var left=transforms.Single(t=>t.name=="HandSocket.L");
 var samples=new System.Collections.Generic.List<object>();float maxSpacingError=0;
 foreach(var clip in clips){
  float maxErr=0;
  for(int i=0;i<=60;i++){clip.SampleAnimation(instance,clip.length*i/60);float spacing=UnityEngine.Vector3.Distance(right.position,left.position);maxErr=UnityEngine.Mathf.Max(maxErr,UnityEngine.Mathf.Abs(spacing-.16f));}
  maxSpacingError=UnityEngine.Mathf.Max(maxSpacingError,maxErr);samples.Add(new{clip=clip.name,seconds=clip.length,maxGripSpacingError=maxErr});
 }
 clips.Single(c=>c.name=="AxeChop01").SampleAnimation(instance,0);
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,folder+"/DarkSurvivor_AxeChop_Review.prefab");
 UnityEditor.EditorUtility.SetDirty(controller);UnityEditor.AssetDatabase.SaveAssets();
 var report=new{clips=samples,invalidMaterials=bad,gameplayChanged=false,maxGripSpacingError=maxSpacingError,status=maxSpacingError<.025f?"Review ready":"Grip interpolation needs attention"};
 System.IO.File.WriteAllText(folder+"/UnityCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
