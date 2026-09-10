using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Displays the animated 3D survivor in the existing XY gameplay world.</summary>
public sealed class ChibiPlayerVisual : MonoBehaviour
{
    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
    static readonly int SwordWalkId = Animator.StringToHash("Base Layer.SwordWalk");
    static readonly int SwordSlashId = Animator.StringToHash("Base Layer.SwordSlash");
    static readonly int SlashSpeedId = Animator.StringToHash("SlashSpeed");
    readonly List<Material> ownedMaterials = new List<Material>();
    Transform view;
    Animator animator;
    Renderer sword;
    bool swordEquipped;
    int holdLayer = -1;
    int motionState = LocomotionId;
    float slashRemaining;
    float slashLength = 1.2f;

    public bool HasSwordAnimations => animator != null && sword != null
        && animator.HasState(0, SwordWalkId) && animator.HasState(0, SwordSlashId);

    public bool Initialize(GameObject model, RuntimeAnimatorController controller, Shader shader, float scale)
    {
        if (model == null || controller == null) return false;
        view = new GameObject("Character3D").transform;
        view.SetParent(transform, false);
        // 3D 전환 후: 캐릭터는 발밑 원점에 그대로 선다. 기울기·각도는 카메라가 준다.
        view.localPosition = Vector3.zero;
        view.localScale = Vector3.one * Mathf.Max(.01f, scale);
        view.localRotation = Quaternion.identity;
        GameObject instance = Instantiate(model, view, false);
        instance.name = "ChibiSurvivor";
        animator = instance.GetComponentInChildren<Animator>();
        // Generic 리그는 아바타 없이도 컨트롤러로 재생된다 — 애니메이터만 있으면 통과시킨다.
        // (아바타 필수로 두면 avatarSetup=NoAvatar로 임포트된 모델이 통째로 안 보인다.)
        if (animator == null)
        {
            Destroy(view.gameObject);
            view = null;
            return false;
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        holdLayer = animator.GetLayerIndex("SwordHold");
        foreach (AnimationClip clip in controller.animationClips)
            if (clip.name == "SwordSlash") slashLength = clip.length;
        foreach (Transform child in view.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = gameObject.layer;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name == "Hero_SwordProxy") { sword = renderer; sword.enabled = false; }
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            // A null override keeps the prefab.s editable material assets, including
            // textures, emission and shader settings, visible in play mode.
            if (shader == null) continue;
            OutlineNormals.Apply(renderer.gameObject);   // 카툰 외곽선용 평균 법선(하드 엣지 대응)
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

    public void SetSwordEquipped(bool equipped)
    {
        equipped &= HasSwordAnimations;
        if (swordEquipped == equipped) return;
        swordEquipped = equipped;
        if (sword != null) sword.enabled = equipped;
        if (!equipped) CancelAttack();
    }

    public void PlaySwordSlash(float duration, Vector2 facing)
    {
        if (!HasSwordAnimations) return;
        SetSwordEquipped(true);
        slashRemaining = Mathf.Max(.05f, duration);
        animator.speed = 1f;
        animator.SetFloat(SlashSpeedId, slashLength / slashRemaining);
        if (holdLayer >= 0) animator.SetLayerWeight(holdLayer, 0f);
        if (facing.sqrMagnitude > .0001f)
            view.localRotation = Quaternion.Euler(0f, Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg, 0f);
        animator.CrossFadeInFixedTime(SwordSlashId, .04f, 0, 0f);
        motionState = SwordSlashId;
    }

    public void CancelAttack()
    {
        slashRemaining = 0f;
        if (animator == null) return;
        if (holdLayer >= 0) animator.SetLayerWeight(holdLayer, 0f);
        animator.speed = 1f;
        animator.CrossFadeInFixedTime(LocomotionId, .08f, 0);
        motionState = LocomotionId;
    }

    public void UpdateMotion(Vector2 facing, float actualSpeed, bool sprinting, bool allowLocomotion, float cadence, float deltaTime)
    {
        if (view == null || animator == null) return;
        if (slashRemaining > 0f)
        {
            slashRemaining = Mathf.Max(0f, slashRemaining - deltaTime);
            if (slashRemaining > 0f) return; // Match the attack's locked hit direction.
        }
        if (facing.sqrMagnitude > .0001f)
        {
            // 평면 방향(x=월드X, y=월드Z)을 Y축 yaw로. 기울기는 카메라 몫이다.
            float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;
            view.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        float motion = allowLocomotion && actualSpeed > .05f ? (sprinting ? 2f : 1f) : 0f;
        int target = swordEquipped && motion == 1f ? SwordWalkId : LocomotionId;
        if (target != motionState)
        {
            animator.CrossFadeInFixedTime(target, .12f, 0);
            motionState = target;
        }
        // Reuse the sword walk's upper-body grip over existing Idle/Run.
        // This adds no separate armed idle/run animation asset.
        if (holdLayer >= 0) animator.SetLayerWeight(holdLayer, swordEquipped && target == LocomotionId ? 1f : 0f);
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
