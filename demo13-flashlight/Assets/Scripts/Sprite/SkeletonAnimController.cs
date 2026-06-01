using UnityEngine;

/// <summary>
/// 애니메이션 컨트롤러 스텁.
/// 탑다운 전환 중 — Spine 기반 SkeletonAnimController 제거 후 컴파일 유지용.
/// 추후 SpriteFrameAnimator 또는 Animator 기반으로 교체 예정.
/// </summary>
public class SkeletonAnimController : MonoBehaviour
{
    public bool IsAnimComplete { get; private set; } = true;

    public void Play(string animName) { }

    public void PlayOneShot(string animName, System.Action onComplete = null)
    {
        onComplete?.Invoke();
    }

    public void SetDirection(Vector2 dir) { }
    public void SetDirection(Vector3 dir) { }
}
