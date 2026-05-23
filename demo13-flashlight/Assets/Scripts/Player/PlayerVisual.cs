using UnityEngine;

/// <summary>
/// 2D 스프라이트 플레이어 비주얼.
/// SpriteRenderer는 같은 GO 또는 자식에 있어야 함 (에디터/프리팹에서 미리 세팅).
/// 빌보드 + 좌우플립만 담당.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : MonoBehaviour
{
    SpriteRenderer sr;
    Camera mainCam;

    float oneShotTimer;
    System.Action oneShotCallback;

    public bool IsAnimComplete { get; private set; } = true;

    void Awake()
    {
        mainCam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // 빌보드: 카메라를 향하도록
        if (mainCam != null && sr != null)
            transform.rotation = mainCam.transform.rotation;

        // 원샷 타이머
        if (oneShotTimer > 0)
        {
            oneShotTimer -= Time.deltaTime;
            if (oneShotTimer <= 0)
            {
                IsAnimComplete = true;
                oneShotCallback?.Invoke();
                oneShotCallback = null;
            }
        }
    }

    public void SetDirection(Vector3 worldDir)
    {
        if (sr == null || mainCam == null) return;
        Vector3 camRight = mainCam.transform.right;
        camRight.y = 0;
        sr.flipX = Vector3.Dot(worldDir, camRight.normalized) < 0;
    }

    public void Play(string animName)
    {
        // TODO: 애니메이션 확장 시 여기서 스프라이트 교체
    }

    public void PlayOneShot(string animName, System.Action onComplete = null)
    {
        IsAnimComplete = false;
        oneShotTimer = 0.3f;
        oneShotCallback = onComplete;
        // TODO: 애니메이션 확장 시 여기서 스프라이트 교체
    }
}
