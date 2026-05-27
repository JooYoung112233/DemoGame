using UnityEngine;

/// <summary>
/// CommonSoldier 프리팹의 Animator를 제어.
/// BlendTree + Horizontal/Vertical 파라미터로 8방향 자동 전환.
/// </summary>
public class SkeletonAnimController : MonoBehaviour
{
    Animator animator;
    Camera mainCam;

    string currentState = "Idle";
    static readonly int H = Animator.StringToHash("Horizontal");
    static readonly int V = Animator.StringToHash("Vertical");

    public bool IsAnimComplete { get; private set; }

    void Start()
    {
        mainCam = Camera.main;
        animator = GetComponentInChildren<Animator>();
    }

    void LateUpdate()
    {
        // 3D 공간에서 카메라를 향하도록 빌보드
        if (mainCam != null)
            transform.rotation = mainCam.transform.rotation;
    }

    public void SetDirection(Vector3 worldDir)
    {
        if (animator == null || worldDir.sqrMagnitude < 0.01f) return;
        // XZ 평면 방향을 Horizontal/Vertical로 변환
        animator.SetFloat(H, worldDir.x);
        animator.SetFloat(V, worldDir.z);
    }

    public void Play(string animName)
    {
        if (animator == null || currentState == animName) return;

        // 이전 상태 bool 끄기
        ResetBools();

        currentState = animName;
        IsAnimComplete = false;

        switch (animName)
        {
            case "idle":
            case "crouch_idle":
                // 기본 상태 (모든 bool false) — 앉기 idle도 같은 모션 (애니 추가 시 분리)
                break;
            case "walk":
            case "crouch_walk":
                // 앉기 걷기도 walk 사용 (애니 추가 시 분리)
                animator.SetBool("Walk", true);
                break;
            case "run":
                animator.SetBool("Run", true);
                break;
        }
    }

    public void PlayOneShot(string animName, System.Action onComplete = null)
    {
        if (animator == null) return;

        ResetBools();
        currentState = animName;
        IsAnimComplete = false;

        switch (animName)
        {
            case "attack":
                animator.Play("Attack", 0, 0f);
                break;
            case "death":
                animator.Play("Death", 0, 0f);
                break;
            case "gethit":
                animator.Play("GetHit", 0, 0f);
                break;
            case "block":
                animator.SetBool("Block", true);
                break;
            case "stunned":
                animator.SetBool("Stunned", true);
                break;
        }

        if (onComplete != null)
            StartCoroutine(WaitAndCallback(onComplete));
    }

    System.Collections.IEnumerator WaitAndCallback(System.Action callback)
    {
        // 다음 프레임까지 대기 (Animator가 새 상태로 전환)
        yield return null;
        yield return null;

        // 현재 상태의 길이만큼 대기
        var info = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(info.length);

        IsAnimComplete = true;
        callback?.Invoke();
    }

    void ResetBools()
    {
        if (animator == null) return;
        animator.SetBool("Walk", false);
        animator.SetBool("Run", false);
        animator.SetBool("Block", false);
        animator.SetBool("Stunned", false);
    }
}
