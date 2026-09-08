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

    public bool Initialize(GameObject model, RuntimeAnimatorController controller, Shader shader, float scale)
    {
        if (model == null || controller == null || shader == null) return false;
        view = new GameObject("Character3D").transform;
        view.SetParent(transform, false);
        // 3D 전환 후: 캐릭터는 발밑 원점에 그대로 선다. 기울기·각도는 카메라가 준다.
        view.localPosition = Vector3.zero;
        view.localScale = Vector3.one * Mathf.Max(.01f, scale);
        view.localRotation = Quaternion.identity;
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
                // 셰이더에 따라 색 프로퍼티 이름이 다르다 — URP/Lit은 _BaseColor, 언릿 계열은 _Color.
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color"))     material.SetColor("_Color", color);
                palette[i] = material;
                ownedMaterials.Add(material);
            }
            renderer.sharedMaterials = palette;
            renderer.shadowCastingMode = ShadowCastingMode.On;   // 3D 전환: 접지감이 필요하다
            renderer.receiveShadows = true;
        }
        return true;
    }

    public void UpdateMotion(Vector2 facing, float actualSpeed, bool sprinting, bool allowLocomotion, float cadence, float deltaTime)
    {
        if (view == null || animator == null) return;
        if (facing.sqrMagnitude > .0001f)
        {
            // 평면 방향(x=월드X, y=월드Z)을 Y축 yaw로. 기울기는 카메라 몫이다.
            float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;
            view.localRotation = Quaternion.Euler(0f, yaw, 0f);
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
