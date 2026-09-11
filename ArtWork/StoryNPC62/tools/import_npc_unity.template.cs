if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("NPC import requires edit mode.");
const string id="__NPC_ID__";
const string stage="D:/Demo/ArtWork/StoryNPC62";
const string folder="Assets/ChibiSurvivor/NPC/StoryNPC62";
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(stage+"/NPCManifest.json"));
var row=manifest["models"].First(x=>(string)x["npcId"]==id);
string modelName=System.IO.Path.GetFileNameWithoutExtension((string)row["fbx"]),clipName=(string)row["clip"];
void Ensure(string p){if(UnityEditor.AssetDatabase.IsValidFolder(p))return;string parent=System.IO.Path.GetDirectoryName(p).Replace('\\','/');Ensure(parent);UnityEditor.AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(p));}
foreach(var p in new[]{folder+"/Models",folder+"/Textures/"+modelName,folder+"/Materials",folder+"/Animation",folder+"/Visuals"})Ensure(p);
var copied=new System.Collections.Generic.List<string>();
foreach(var kind in new[]{"BaseColor","Normal","Mask"}){
 string from=stage+"/"+(string)row["textures"][kind],to=folder+"/Textures/"+modelName+"/"+modelName+"_"+kind+".png";
 System.IO.File.Copy(from,to,true);UnityEditor.AssetDatabase.ImportAsset(to,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
 var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(to);ti.textureType=kind=="Normal"?UnityEditor.TextureImporterType.NormalMap:UnityEditor.TextureImporterType.Default;ti.sRGBTexture=kind=="BaseColor";ti.alphaSource=UnityEditor.TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=false;ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Bilinear;ti.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();copied.Add(to);
}
string fbx=folder+"/Models/"+modelName+".fbx";System.IO.File.Copy(stage+"/"+(string)row["fbx"],fbx,true);UnityEditor.AssetDatabase.ImportAsset(fbx,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var mi=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
mi.animationType=UnityEditor.ModelImporterAnimationType.Generic;mi.avatarSetup=UnityEditor.ModelImporterAvatarSetup.CreateFromThisModel;mi.importAnimation=true;mi.importCameras=false;mi.importLights=false;mi.isReadable=true;mi.importNormals=UnityEditor.ModelImporterNormals.Import;mi.importTangents=UnityEditor.ModelImporterTangents.CalculateMikk;mi.animationCompression=UnityEditor.ModelImporterAnimationCompression.Off;mi.materialImportMode=UnityEditor.ModelImporterMaterialImportMode.ImportStandard;mi.SaveAndReimport();
var settings=mi.defaultClipAnimations;if(settings.Length!=1)throw new System.Exception("Expected one clip: "+modelName+" got "+settings.Length);
foreach(var c in settings){c.name=clipName;c.loopTime=true;c.loopPose=false;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;c.keepOriginalOrientation=true;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;}
mi.clipAnimations=settings;
var shader=Shader.Find("BRB/GameLit");if(shader==null)throw new System.Exception("GameLit unavailable");
string matPath=folder+"/Materials/"+modelName+".mat";var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
if(mat==null){mat=new Material(shader){name=modelName+"_Surface"};UnityEditor.AssetDatabase.CreateAsset(mat,matPath);}
mat.shader=shader;mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(copied[0]));mat.SetTexture("_BumpMap",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(copied[1]));mat.SetFloat("_BumpScale",.35f);mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.08f);
// Current GameLit exposes constant roughness/metallicity, no packed-mask sampling.
// Keep Mask imported for a future shader update; do not pretend it is connected.
mat.SetFloat("_GrimeObjectSpace",1);mat.SetFloat("_GrimeStrength",.30f);mat.SetFloat("_GrimeScale",3);mat.SetFloat("_ShadowDesaturation",.45f);mat.SetFloat("_RimStrength",.12f);mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");UnityEditor.EditorUtility.SetDirty(mat);
mi.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(Material),modelName+"_Surface"),mat);mi.SaveAndReimport();
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();if(clips.Length!=1||clips[0].name!=clipName)throw new System.Exception("Clip mapping failed");
string controllerPath=folder+"/Animation/"+modelName+".controller";var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);if(controller==null)controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
var machine=controller.layers[0].stateMachine;var state=machine.states.Select(x=>x.state).FirstOrDefault(x=>x.name==clipName)??machine.AddState(clipName);state.motion=clips[0];machine.defaultState=state;UnityEditor.EditorUtility.SetDirty(controller);
var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model);
try{
 instance.name=modelName;var animator=instance.GetComponent<Animator>()??instance.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 foreach(var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)){smr.sharedMaterial=mat;smr.updateWhenOffscreen=true;smr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;smr.receiveShadows=true;if(smr.bones.Length!=23&&smr.bones.Length<15)throw new System.Exception("Unexpected skin bones");}
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,folder+"/Visuals/"+modelName+".prefab");
}finally{UnityEngine.Object.DestroyImmediate(instance);}
UnityEditor.AssetDatabase.SaveAssets();
var result=new{id,modelName,clip=clips[0].name,seconds=clips[0].length,shader=shader.name,controller=controllerPath,visual=folder+"/Visuals/"+modelName+".prefab",maskConnected=false};
System.IO.Directory.CreateDirectory(stage+"/UnityIntegration");System.IO.File.WriteAllText(stage+"/UnityIntegration/"+id+"-import.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return Newtonsoft.Json.JsonConvert.SerializeObject(result);
