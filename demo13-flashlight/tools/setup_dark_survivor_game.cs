// Run with Unity Pipeline eval_file after the FBX export.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop play mode before setup.");
const string folder = "Assets/ChibiSurvivor/Player/DarkSurvivor";
const string fbx = folder + "/DarkSurvivor.fbx";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
if (!UnityEditor.AssetDatabase.IsValidFolder(folder + "/Materials")) UnityEditor.AssetDatabase.CreateFolder(folder, "Materials");
var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
importer.animationType = UnityEditor.ModelImporterAnimationType.Generic;
importer.avatarSetup = UnityEditor.ModelImporterAvatarSetup.CreateFromThisModel;
importer.importAnimation = true;
importer.importCameras = false;
importer.importLights = false;
importer.isReadable = true;
importer.materialImportMode = UnityEditor.ModelImporterMaterialImportMode.ImportStandard;
importer.SaveAndReimport();
var clips = importer.defaultClipAnimations;
foreach (var clip in clips)
{
    clip.name = clip.name.Split('|').Last();
    clip.loopTime = clip.name != "SwordSlash";
    clip.loopPose = false;
    clip.keepOriginalPositionY = true;
    clip.keepOriginalPositionXZ = true;
    clip.keepOriginalOrientation = true;
    clip.lockRootRotation = true;
    clip.lockRootPositionXZ = true;
    clip.lockRootHeightY = true;
}
importer.clipAnimations = clips;
var palette = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder + "/Palette.json"));
var materials = new System.Collections.Generic.Dictionary<string,UnityEngine.Material>();
foreach (var entry in palette["materials"])
{
    var name = (string)entry["name"];
    var path = folder + "/Materials/" + name + ".mat";
    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    // Rerunning setup must preserve the user's shader/material experiments.
    if (mat == null)
    {
        mat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;
        var c = entry["color"];
        mat.SetColor("_BaseColor", new UnityEngine.Color((float)c[0],(float)c[1],(float)c[2],1).gamma);
        mat.SetFloat("_Smoothness", 1f-(float)entry["roughness"]);
        mat.SetFloat("_Metallic", (float)entry["metallic"]);
        var strength = (float)entry["emissionStrength"];
        if (strength > 0)
        {
            var e = entry["emission"];
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",new UnityEngine.Color((float)e[0],(float)e[1],(float)e[2],1)*strength);
            mat.globalIlluminationFlags = UnityEngine.MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        UnityEditor.AssetDatabase.CreateAsset(mat,path);
    }
    materials.Add(name,mat);
    importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),name),mat);
}
importer.SaveAndReimport();
var motions = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
foreach (var expected in new[]{"Idle","Walk","Run"}) if (!motions.Any(c=>c.name==expected)) throw new System.Exception("Missing animation: "+expected+" / "+string.Join(",",motions.Select(c=>c.name)));
var controllerPath = folder + "/DarkSurvivor.controller";
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if (controller == null)
{
    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
    controller.AddParameter("Speed",UnityEngine.AnimatorControllerParameterType.Float);
    var tree = new UnityEditor.Animations.BlendTree { name="Locomotion",blendType=UnityEditor.Animations.BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false };
    UnityEditor.AssetDatabase.AddObjectToAsset(tree,controller);
    tree.AddChild(motions.Single(c=>c.name=="Idle"),0f);
    tree.AddChild(motions.Single(c=>c.name=="Walk"),1f);
    tree.AddChild(motions.Single(c=>c.name=="Run"),2f);
    var state=controller.layers[0].stateMachine.AddState("Locomotion");state.motion=tree;
    controller.layers[0].stateMachine.defaultState=state;
    UnityEditor.EditorUtility.SetDirty(controller);
}
var prefabPath = folder + "/DarkSurvivor.prefab";
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx);
foreach(var expected in new[]{"SwordWalk","SwordSlash"}) if(!motions.Any(c=>c.name==expected))throw new System.Exception("Missing "+expected);
if(!controller.parameters.Any(p=>p.name=="SlashSpeed"))controller.AddParameter("SlashSpeed",UnityEngine.AnimatorControllerParameterType.Float);
var machine=controller.layers[0].stateMachine;
foreach(var name in new[]{"SwordWalk","SwordSlash"})
{
    var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name) ?? machine.AddState(name);
    state.motion=motions.Single(c=>c.name==name);
    if(name=="SwordSlash"){state.speedParameter="SlashSpeed";state.speedParameterActive=true;}
}
if(!controller.layers.Any(l=>l.name=="SwordHold"))
{
    var mask=new UnityEngine.AvatarMask();mask.name="SwordUpperBody";
    mask.AddTransformPath(model.transform,true);
    for(int i=0;i<mask.transformCount;i++)mask.SetTransformActive(i,mask.GetTransformPath(i).Contains("/Chest"));
    UnityEditor.AssetDatabase.CreateAsset(mask,folder+"/SwordUpperBody.mask");
    controller.AddLayer("SwordHold");
    var layers=controller.layers;var layer=layers[layers.Length-1];layer.avatarMask=mask;layer.defaultWeight=0;
    layer.blendingMode=UnityEditor.Animations.AnimatorLayerBlendingMode.Override;
    var hold=layer.stateMachine.AddState("Hold");hold.motion=motions.Single(c=>c.name=="SwordWalk");hold.speed=0;
    layer.stateMachine.defaultState=hold;controller.layers=layers;
}
UnityEditor.EditorUtility.SetDirty(controller);
var item=UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Resources/Items/Weapon/LongSword.asset");
if(item.weaponData==null)
{
    var weaponPath=folder+"/Weapon_LongSword.asset";
    var weapon=UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
    if(weapon==null){weapon=UnityEngine.ScriptableObject.CreateInstance<WeaponData>();weapon.weaponId="long_sword";UnityEditor.AssetDatabase.CreateAsset(weapon,weaponPath);}
    item.weaponData=weapon;UnityEditor.EditorUtility.SetDirty(item);
}
item.weaponData.useTwoHandSwordAnimations=true;UnityEditor.EditorUtility.SetDirty(item.weaponData);
var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model);
UnityEngine.GameObject prefab;
try
{
    instance.name="DarkSurvivor";
    var animator=instance.GetComponentInChildren<UnityEngine.Animator>();
    if (animator==null) animator=instance.AddComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
    animator.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    foreach (var renderer in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        renderer.updateWhenOffscreen=true;
        if(renderer.name=="Hero_SwordProxy")renderer.enabled=false;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
        if (renderer.sharedMaterials.Any(m=>m==null || !materials.Values.Contains(m))) throw new System.Exception("Material remap failed: "+renderer.name);
    }
    prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,prefabPath);
}
finally { UnityEngine.Object.DestroyImmediate(instance); }
var rigPath="Assets/Resources/PlayerRig.prefab";
var root=UnityEditor.PrefabUtility.LoadPrefabContents(rigPath);
try
{
    foreach (var player in root.GetComponentsInChildren<TopDownPlayer>(true))
    {
        var so=new UnityEditor.SerializedObject(player);
        so.FindProperty("character3DPrefab").objectReferenceValue=prefab;
        so.FindProperty("character3DController").objectReferenceValue=controller;
        so.FindProperty("character3DShader").objectReferenceValue=null;
        so.FindProperty("character3DScale").floatValue=1;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,rigPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
var updated=new System.Collections.Generic.List<string>();
foreach (var player in UnityEngine.Object.FindObjectsByType<TopDownPlayer>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None))
{
    if (!player.gameObject.scene.IsValid()) continue;
    var so=new UnityEditor.SerializedObject(player);
    UnityEditor.Undo.RecordObject(player,"Use Dark Survivor");
    so.FindProperty("character3DPrefab").objectReferenceValue=prefab;
    so.FindProperty("character3DController").objectReferenceValue=controller;
    so.FindProperty("character3DShader").objectReferenceValue=null;
    so.FindProperty("character3DScale").floatValue=1;
    so.ApplyModifiedProperties();
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(player);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
    updated.Add(player.gameObject.scene.path);
}
UnityEditor.AssetDatabase.SaveAssets();
foreach (var path in updated.Distinct()) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path));
UnityEditor.Selection.activeObject=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder+"/Materials");
return new {prefab=prefabPath,materials=materials.Count,clips=motions.Select(c=>new {c.name,c.length}).ToArray(),scenes=updated};
