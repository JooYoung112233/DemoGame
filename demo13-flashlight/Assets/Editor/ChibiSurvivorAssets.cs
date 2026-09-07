using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Import and bake only the standalone chibi art package; no scene or player changes.</summary>
public sealed class ChibiSurvivorImporter : AssetPostprocessor
{
    internal const string Folder = "Assets/ChibiSurvivor";
    internal const string Model = Folder + "/ChibiSurvivor.fbx";

    void OnPreprocessModel()
    {
        if (assetPath != Model) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    void OnPreprocessAnimation()
    {
        if (assetPath != Model) return;
        var importer = (ModelImporter)assetImporter;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            // Blender FBX take names can include the armature prefix.
            clip.name = clip.name.Split('|').Last();
            clip.loopTime = true;
            clip.loopPose = true;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips;
    }

    void OnPostprocessMaterial(Material material)
    {
        if (assetPath != Model) return;
        Color color = material.HasProperty("_Color") ? material.color : Color.white;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return;
        material.shader = shader;
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .18f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .18f);
    }
}

public static class ChibiSurvivorAssets
{
    [MenuItem("Tools/TopDown/Art/Bake Chibi Survivor Assets")]
    public static void Bake()
    {
        const string folder = ChibiSurvivorImporter.Folder;
        AssetDatabase.ImportAsset(ChibiSurvivorImporter.Model, ImportAssetOptions.ForceUpdate);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ChibiSurvivorImporter.Model);
        if (model == null) throw new InvalidOperationException("Chibi survivor FBX import failed.");
        var clips = AssetDatabase.LoadAllAssetsAtPath(ChibiSurvivorImporter.Model)
            .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        string[] names = { "Idle", "Walk", "Run" };
        var baked = new AnimationClip[3];
        for (int i = 0; i < names.Length; i++)
        {
            var source = clips.Single(c => c.name == names[i]);
            if (!source.isLooping || source.length <= 0)
                throw new InvalidOperationException("Invalid loop: " + source.name);
            string path = folder + "/" + names[i] + ".anim";
            baked[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (baked[i] == null)
            {
                baked[i] = UnityEngine.Object.Instantiate(source);
                AssetDatabase.CreateAsset(baked[i], path);
            }
            else EditorUtility.CopySerialized(source, baked[i]);
            baked[i].name = names[i];
            EditorUtility.SetDirty(baked[i]);
        }
        string controllerPath = folder + "/ChibiSurvivor.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        // Preserve a controller already customized by an artist; initial bake creates it once.
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var state = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            for (int i = 0; i < baked.Length; i++) tree.AddChild(baked[i], i);
            controller.layers[0].stateMachine.defaultState = state;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            if (animator.avatar == null || !animator.avatar.isValid)
                throw new InvalidOperationException("Missing/invalid Generic avatar.");
            var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null || skin.sharedMesh == null || skin.bones.Length < 15)
                throw new InvalidOperationException("Missing skinned mesh or bone bindings.");
            if (skin.sharedMaterials.Any(m => m == null))
                throw new InvalidOperationException("Unassigned palette material.");
            foreach (var clip in baked)
            {
                clip.SampleAnimation(instance, 0);
                var before = instance.GetComponentsInChildren<Transform>().Select(t => t.localToWorldMatrix).ToArray();
                clip.SampleAnimation(instance, clip.length);
                var after = instance.GetComponentsInChildren<Transform>().Select(t => t.localToWorldMatrix).ToArray();
                float error = before.Zip(after, (a, b) => Enumerable.Range(0, 16).Max(j => Mathf.Abs(a[j] - b[j]))).Max();
                if (error > .001f) throw new InvalidOperationException("Imported loop seam: " + clip.name + " " + error);
                Debug.Log("CHIBI_CLIP_VALID " + clip.name + " seconds=" + clip.length + " seam=" + error);
            }
            // Store the FBX rest pose, never a sampled running pose.
            UnityEngine.Object.DestroyImmediate(instance);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            animator = instance.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(instance, folder + "/ChibiSurvivor.prefab");
        }
        finally { if (instance != null) UnityEngine.Object.DestroyImmediate(instance); }
        AssetDatabase.SaveAssets();
        Debug.Log("CHIBI_UNITY_VALIDATION_SUCCESS: Generic rig, materials, 3 looping clips, controller, prefab.");
    }
}
