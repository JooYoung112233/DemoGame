const string folder="Assets/ChibiSurvivor/Bandit/Bandit01";
const string fbx=folder+"/Bandit01.fbx";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
if(!UnityEditor.AssetDatabase.IsValidFolder(folder+"/Materials"))UnityEditor.AssetDatabase.CreateFolder(folder,"Materials");
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
importer.animationType=UnityEditor.ModelImporterAnimationType.Generic;
importer.avatarSetup=UnityEditor.ModelImporterAvatarSetup.CreateFromThisModel;
importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
importer.SaveAndReimport();
var clips=importer.defaultClipAnimations;
foreach(var clip in clips){clip.name=clip.name.Split('|').Last();clip.loopTime=clip.name!="SwordSlash";clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;clip.keepOriginalOrientation=true;clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;}
importer.clipAnimations=clips;
var palette=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/Palette.json"));
foreach(var e in palette["materials"]){
 string n=(string)e["name"],path=folder+"/Materials/"+n+".mat";
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
 if(m==null){m=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));m.name=n;var c=e["color"];m.SetColor("_BaseColor",new UnityEngine.Color((float)c[0],(float)c[1],(float)c[2],1).gamma);m.SetFloat("_Smoothness",1-(float)e["roughness"]);UnityEditor.AssetDatabase.CreateAsset(m,path);}
 importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),n),m);
}
importer.SaveAndReimport();
var motions=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(folder+"/Bandit01.controller");
if(controller==null){
 controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(folder+"/Bandit01.controller");
 controller.AddParameter("Speed",UnityEngine.AnimatorControllerParameterType.Float);
 var tree=new UnityEditor.Animations.BlendTree{name="Locomotion",blendType=UnityEditor.Animations.BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
 UnityEditor.AssetDatabase.AddObjectToAsset(tree,controller);
 tree.AddChild(motions.Single(c=>c.name=="Idle"),0);tree.AddChild(motions.Single(c=>c.name=="Walk"),1);tree.AddChild(motions.Single(c=>c.name=="Run"),2);
 var state=controller.layers[0].stateMachine.AddState("Locomotion");state.motion=tree;controller.layers[0].stateMachine.defaultState=state;
}
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx);
var obj=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model);
try{
 obj.name="Bandit01";var animator=obj.GetComponent<UnityEngine.Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
 var meshes=obj.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true);
 foreach(var r in meshes){r.updateWhenOffscreen=true;if(r.sharedMaterials.Any(m=>m==null))throw new System.Exception("Missing material "+r.name);if(r.bones.Any(b=>b==null))throw new System.Exception("Missing bone "+r.name);}
 if(animator.avatar==null||!animator.avatar.isValid)throw new System.Exception("Invalid avatar");
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj,folder+"/Bandit01.prefab");UnityEditor.AssetDatabase.SaveAssets();
 return new{success=true,meshes=meshes.Length,avatarValid=animator.avatar.isValid,clips=motions.Select(c=>new{c.name,c.length}),prefab=folder+"/Bandit01.prefab"};
}finally{UnityEngine.Object.DestroyImmediate(obj);}
