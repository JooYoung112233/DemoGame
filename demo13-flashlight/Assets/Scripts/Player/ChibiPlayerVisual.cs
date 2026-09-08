using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Displays the animated 3D survivor in the existing XY gameplay world.</summary>
public sealed class ChibiPlayerVisual : MonoBehaviour
{
    static readonly int SpeedId = Animator.StringToHash("Speed");
    readonly List<Material> ownedMaterials = new List<Material>();
    Transform view;
    Animator animator;
    readonly Quaternion cameraTilt = Quaternion.Euler(-35f, 0f, 0f);

    public bool Initialize(GameObject model, RuntimeAnimatorController controller, Shader shader, float scale)
    {
        if (model == null || controller == null || shader == null) return false;
        view = new GameObject("Character3D").transform;
        view.SetParent(transform, false);
        // Keep the entire mesh behind the XY sprite plane. Its own depth buffer resolves
        // face/hair/limb overlap; later-sorted foreground sprites can still cover it.
        view.localPosition = new Vector3(0f, -.35f, 2f);
        view.localScale = Vector3.one * Mathf.Max(.01f, scale);
        view.localRotation = cameraTilt * Quaternion.Euler(0f, 180f, 0f);
        GameObject instance = Instantiate(model, view, false);
        instance.name = "ChibiSurvivor";
        animator = instance.GetComponentInChildren<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isValid)
        {
            Destroy(view.gameObject);
            view = null;
            return false;
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var sorting = view.gameObject.AddComponent<SortingGroup>();
        sorting.sortingOrder = 5;
        foreach (Transform child in view.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = gameObject.layer;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] palette = renderer.sharedMaterials;
            for (int i = 0; i < palette.Length; i++)
            {
                Color color = Color.white;
                if (palette[i] != null)
                    color = palette[i].HasProperty("_BaseColor") ? palette[i].GetColor("_BaseColor") : palette[i].color;
                var material = new Material(shader) { name = "Chibi_InGame_" + i };
                material.SetColor("_Color", color);
                palette[i] = material;
                ownedMaterials.Add(material);
            }
            renderer.sharedMaterials = palette;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return true;
    }

    public void UpdateMotion(Vector2 facing, float actualSpeed, bool sprinting, bool allowLocomotion, float cadence, float deltaTime)
    {
        if (view == null || animator == null) return;
        if (facing.sqrMagnitude > .0001f)
        {
            // Local +Z projects to screen +Y after the camera tilt; +X remains screen +X.
            float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;
            view.localRotation = cameraTilt * Quaternion.Euler(0f, yaw, 0f);
        }
        float motion = allowLocomotion && actualSpeed > .05f ? (sprinting ? 2f : 1f) : 0f;
        animator.SetFloat(SpeedId, motion, .10f, Mathf.Max(0f, deltaTime));
        animator.speed = Mathf.Max(0f, cadence);
    }

    void OnDestroy()
    {
        foreach (Material material in ownedMaterials)
            if (material != null) Destroy(material);
        if (view != null) Destroy(view.gameObject);
    }
}
