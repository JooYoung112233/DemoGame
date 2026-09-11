if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Exit Play mode before applying the player.");
const string folder="Assets/ChibiSurvivor/Player/SimpleHeroStudy";
const string fbx=folder+"/SimpleHero_Stage5_Slash.fbx";
void EnsureFolder(string path){if(!UnityEditor.AssetDatabase.IsValidFolder(path)){EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'));UnityEditor.AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'),System.IO.Path.GetFileName(path));}}
EnsureFolder(folder+"/Materials");EnsureFolder(folder+"/Game");
var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
importer.animationType=UnityEditor.ModelImporterAnimationType.Generic;importer.avatarSetup=UnityEditor.ModelImporterAvatarSetup.CreateFromThisModel;
importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
importer.animationCompression=UnityEditor.ModelImporterAnimationCompression.Off;importer.materialImportMode=UnityEditor.ModelImporterMaterialImportMode.ImportStandard;
importer.SaveAndReimport();
var clipSettings=importer.defaultClipAnimations;
foreach(var clip in clipSettings){clip.name=clip.name.Split('|').Last();clip.loopTime=clip.name!="SwordSlash";clip.loopPose=false;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;clip.keepOriginalOrientation=true;clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;}
importer.clipAnimations=clipSettings;
var mats=new System.Collections.Generic.Dictionary<string,Material>();
foreach(var pair in new[]{new[]{"SimpleHero_Surface","SimpleHero_BaseColor.png"},new[]{"SimpleHero_Gear","SimpleHero_Gear_BaseColor.png"}})
{
 var texturePath=folder+"/Textures/"+pair[1];var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
 ti.sRGBTexture=true;ti.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Bilinear;ti.SaveAndReimport();
 var path=folder+"/Materials/"+pair[0]+".mat";var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
 if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=pair[0];mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.08f);mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");UnityEditor.AssetDatabase.CreateAsset(mat,path);}
 mat.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));UnityEditor.EditorUtility.SetDirty(mat);mats.Add(pair[0],mat);
 importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(Material),pair[0]),mat);
}
importer.SaveAndReimport();
var clips=new System.Collections.Generic.Dictionary<string,AnimationClip>();
foreach(var sourceClip in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
{
 var name=sourceClip.name.Split('|').Last();var copy=UnityEngine.Object.Instantiate(sourceClip);copy.name=name;
 var settings=UnityEditor.AnimationUtility.GetAnimationClipSettings(copy);settings.loopTime=name!="SwordSlash";settings.loopBlend=false;UnityEditor.AnimationUtility.SetAnimationClipSettings(copy,settings);
 var path=folder+"/Game/"+name+".anim";var saved=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
 if(saved==null){UnityEditor.AssetDatabase.CreateAsset(copy,path);saved=copy;}else{UnityEditor.EditorUtility.CopySerialized(copy,saved);UnityEngine.Object.DestroyImmediate(copy);}
 clips.Add(name,saved);
}
foreach(var name in new[]{"Idle","Walk","Run","SwordSlash"})if(!clips.ContainsKey(name))throw new System.Exception("Missing "+name);
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
var chest=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Chest");
var chestPath=UnityEditor.AnimationUtility.CalculateTransformPath(chest,model.transform);
// Temporary armed locomotion: approved walk below the chest + the approved
// slash's first two-hand grip above it. New purchased melee clips can replace it.
var armed=UnityEngine.Object.Instantiate(clips["Walk"]);armed.name="SwordWalk";
foreach(var bind in UnityEditor.AnimationUtility.GetCurveBindings(clips["SwordSlash"]))
 if(bind.path==chestPath||bind.path.StartsWith(chestPath+"/"))
 {float v=UnityEditor.AnimationUtility.GetEditorCurve(clips["SwordSlash"],bind).Evaluate(0);UnityEditor.AnimationUtility.SetEditorCurve(armed,bind,AnimationCurve.Constant(0,clips["Walk"].length,v));}
var armedSettings=UnityEditor.AnimationUtility.GetAnimationClipSettings(armed);armedSettings.loopTime=true;UnityEditor.AnimationUtility.SetAnimationClipSettings(armed,armedSettings);
var armedPath=folder+"/Game/SwordWalk.anim";var existingArmed=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(armedPath);
if(existingArmed==null)UnityEditor.AssetDatabase.CreateAsset(armed,armedPath);else{UnityEditor.EditorUtility.CopySerialized(armed,existingArmed);UnityEngine.Object.DestroyImmediate(armed);armed=existingArmed;}
var controllerPath=folder+"/Game/SimpleHero.controller";var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if(controller==null)controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
if(!controller.parameters.Any(p=>p.name=="Speed"))controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
if(!controller.parameters.Any(p=>p.name=="SlashSpeed"))controller.AddParameter("SlashSpeed",AnimatorControllerParameterType.Float);
var machine=controller.layers[0].stateMachine;
UnityEditor.Animations.AnimatorState State(string name){return machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);}
var locomotion=State("Locomotion");var tree=locomotion.motion as UnityEditor.Animations.BlendTree;
if(tree==null){tree=new UnityEditor.Animations.BlendTree{name="Locomotion",blendType=UnityEditor.Animations.BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};UnityEditor.AssetDatabase.AddObjectToAsset(tree,controller);}
tree.children=new[]{new UnityEditor.Animations.ChildMotion{motion=clips["Idle"],threshold=0,timeScale=1},new UnityEditor.Animations.ChildMotion{motion=clips["Walk"],threshold=1,timeScale=1},new UnityEditor.Animations.ChildMotion{motion=clips["Run"],threshold=2,timeScale=1}};
locomotion.motion=tree;machine.defaultState=locomotion;State("SwordWalk").motion=armed;
var slash=State("SwordSlash");slash.motion=clips["SwordSlash"];slash.speedParameter="SlashSpeed";slash.speedParameterActive=true;
var maskPath=folder+"/Game/SwordUpperBody.mask";var mask=UnityEditor.AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
if(mask==null){mask=new AvatarMask{name="SimpleHero_SwordUpperBody"};mask.AddTransformPath(model.transform,true);UnityEditor.AssetDatabase.CreateAsset(mask,maskPath);}
for(int i=0;i<mask.transformCount;i++){string p=mask.GetTransformPath(i);mask.SetTransformActive(i,p==chestPath||p.StartsWith(chestPath+"/"));}
if(!controller.layers.Any(l=>l.name=="SwordHold"))controller.AddLayer("SwordHold");
var layers=controller.layers;var holdLayer=layers.Single(l=>l.name=="SwordHold");holdLayer.avatarMask=mask;holdLayer.defaultWeight=0;holdLayer.blendingMode=UnityEditor.Animations.AnimatorLayerBlendingMode.Override;
var hold=holdLayer.stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Hold")??holdLayer.stateMachine.AddState("Hold");hold.motion=armed;hold.speed=0;holdLayer.stateMachine.defaultState=hold;controller.layers=layers;
UnityEditor.EditorUtility.SetDirty(tree);UnityEditor.EditorUtility.SetDirty(mask);UnityEditor.EditorUtility.SetDirty(controller);
var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model);GameObject prefab;
try{
 instance.name="SimpleHero";var animator=instance.GetComponentInChildren<Animator>()??instance.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 foreach(var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>()){smr.updateWhenOffscreen=true;if(smr.sharedMaterials.Any(m=>m==null||!mats.Values.Contains(m)))throw new System.Exception("Material remap failed "+smr.name);}
 var socket=instance.GetComponentsInChildren<Transform>().Single(t=>t.name=="HandSocket.R");
 var combinations=new System.Collections.Generic.List<CombineInstance>();
 foreach(var dims in new[]{new[]{0f,-.07f,0f,.043f,.255f,.043f},new[]{0f,.095f,0f,.12f,.025f,.04f},new[]{0f,.462f,0f,.065f,.70f,.022f}}){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);combinations.Add(new CombineInstance{mesh=cube.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.TRS(new Vector3(dims[0],dims[1],dims[2]),Quaternion.identity,new Vector3(dims[3],dims[4],dims[5]))});UnityEngine.Object.DestroyImmediate(cube);}
 var mesh=new Mesh{name="SimpleHero_SwordProxy"};mesh.CombineMeshes(combinations.ToArray());var meshPath=folder+"/Game/SwordProxy.asset";var existingMesh=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
 if(existingMesh==null)UnityEditor.AssetDatabase.CreateAsset(mesh,meshPath);else{UnityEditor.EditorUtility.CopySerialized(mesh,existingMesh);UnityEngine.Object.DestroyImmediate(mesh);mesh=existingMesh;}
 var weapon=new GameObject("Hero_SwordProxy");weapon.transform.SetParent(socket,false);var socketScale=socket.lossyScale;weapon.transform.localScale=new Vector3(1/socketScale.x,1/socketScale.y,1/socketScale.z);weapon.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=weapon.AddComponent<MeshRenderer>();
 var steelPath=folder+"/Materials/SwordProxy.mat";var steel=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(steelPath);if(steel==null){steel=new Material(Shader.Find("Universal Render Pipeline/Lit"));steel.SetColor("_BaseColor",new Color(.22f,.24f,.25f).gamma);steel.SetFloat("_Smoothness",.28f);UnityEditor.AssetDatabase.CreateAsset(steel,steelPath);}renderer.sharedMaterial=steel;renderer.enabled=false;
 prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,folder+"/Game/SimpleHero.prefab");
}finally{UnityEngine.Object.DestroyImmediate(instance);}
void Assign(TopDownPlayer player,bool undo){if(undo)UnityEditor.Undo.RecordObject(player,"Use Simple Hero");var s=new UnityEditor.SerializedObject(player);s.FindProperty("character3DPrefab").objectReferenceValue=prefab;s.FindProperty("character3DController").objectReferenceValue=controller;s.FindProperty("character3DShader").objectReferenceValue=null;s.FindProperty("character3DScale").floatValue=1;s.ApplyModifiedProperties();}
var playerRig=UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Resources/PlayerRig.prefab");
try{foreach(var player in playerRig.GetComponentsInChildren<TopDownPlayer>(true))Assign(player,false);UnityEditor.PrefabUtility.SaveAsPrefabAsset(playerRig,"Assets/Resources/PlayerRig.prefab");}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(playerRig);}
var updated=new System.Collections.Generic.List<string>();
foreach(var player in UnityEngine.Object.FindObjectsByType<TopDownPlayer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
{if(!player.gameObject.scene.IsValid())continue;var scene=player.gameObject.scene;bool wasDirty=scene.isDirty;Assign(player,true);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(player);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!wasDirty&&!string.IsNullOrEmpty(scene.path))UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);updated.Add(scene.path);}
UnityEditor.AssetDatabase.SaveAssets();
return new{prefab=UnityEditor.AssetDatabase.GetAssetPath(prefab),controller=controllerPath,clips=clips.Keys,temporaryArmedWalk=true,updatedScenes=updated,playerRigUpdated=true};
