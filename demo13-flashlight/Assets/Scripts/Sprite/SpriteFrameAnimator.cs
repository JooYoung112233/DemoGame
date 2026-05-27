using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 스프라이트 프레임 애니메이션 컨트롤러.
/// Resources/Charater/ 에서 스프라이트를 로드하여 프레임 애니메이션 재생.
/// SkeletonAnimController 대신 사용 가능 — PlayerController와 동일 인터페이스.
/// </summary>
public class SpriteFrameAnimator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float walkFPS = 8f;
    [SerializeField] float runFPS = 12f;
    [SerializeField] string spriteFolderPath = "Charater";

    [Header("References")]
    [SerializeField] SpriteRenderer targetRenderer;

    // --- 로드된 스프라이트 ---
    Sprite[] walkFrames;
    Sprite idleSprite; // walk 첫 프레임을 idle로 사용

    // --- 재생 상태 ---
    string currentAnim = "idle";
    int currentFrame;
    float frameTimer;
    float currentFPS;
    bool isPlaying;

    // --- 방향 ---
    Camera mainCam;

    // --- 원샷 ---
    bool isOneShot;
    Action oneShotCallback;

    public bool IsAnimComplete { get; private set; }

    void Awake()
    {
        // SpriteRenderer 자동 탐색
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        mainCam = Camera.main;

        LoadSprites();
    }

    void LoadSprites()
    {
        // Resources/Charater/ 에서 1, 2, 3 로드
        var sprites = new List<Sprite>();
        for (int i = 1; i <= 10; i++) // 최대 10프레임까지 탐색
        {
            string path = $"{spriteFolderPath}/{i}";
            Sprite s = Resources.Load<Sprite>(path);
            if (s != null)
                sprites.Add(s);
            else
                break;
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[SpriteFrameAnimator] {spriteFolderPath}/ 에서 스프라이트를 찾을 수 없습니다.");
            return;
        }

        walkFrames = sprites.ToArray();
        idleSprite = walkFrames[0]; // 첫 프레임 = idle

        // 초기 스프라이트 설정
        if (targetRenderer != null && idleSprite != null)
            targetRenderer.sprite = idleSprite;

        Debug.Log($"[SpriteFrameAnimator] {walkFrames.Length}프레임 로드 완료 ({spriteFolderPath}/)");
    }

    void Update()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;
        if (targetRenderer == null) return;

        if (!isPlaying)
        {
            // idle: 첫 프레임 고정
            targetRenderer.sprite = idleSprite;
            return;
        }

        // 프레임 업데이트
        frameTimer += Time.unscaledDeltaTime;
        float interval = 1f / currentFPS;

        if (frameTimer >= interval)
        {
            frameTimer -= interval;
            currentFrame++;

            if (isOneShot)
            {
                // 원샷: 한 바퀴 돌고 끝
                if (currentFrame >= walkFrames.Length)
                {
                    currentFrame = 0;
                    isPlaying = false;
                    isOneShot = false;
                    IsAnimComplete = true;
                    oneShotCallback?.Invoke();
                    oneShotCallback = null;
                    targetRenderer.sprite = idleSprite;
                    return;
                }
            }
            else
            {
                // 루프
                currentFrame %= walkFrames.Length;
            }

            targetRenderer.sprite = walkFrames[currentFrame];
        }
    }

    void LateUpdate()
    {
        // 빌보드: 카메라를 향하도록
        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam != null)
            transform.rotation = mainCam.transform.rotation;
    }

    #region ===== Public API (SkeletonAnimController 호환) =====

    /// <summary>연속 애니메이션 재생. idle/walk/run/crouch_idle/crouch_walk 지원.</summary>
    public void Play(string animName)
    {
        if (walkFrames == null || walkFrames.Length == 0) return;
        if (isOneShot) return; // 원샷 재생 중엔 무시

        if (currentAnim == animName) return; // 이미 같은 애니메이션
        currentAnim = animName;

        switch (animName)
        {
            case "idle":
            case "crouch_idle":
                isPlaying = false;
                currentFrame = 0;
                frameTimer = 0;
                break;

            case "walk":
            case "crouch_walk":
                isPlaying = true;
                currentFPS = walkFPS;
                currentFrame = 0;
                frameTimer = 0;
                break;

            case "run":
                isPlaying = true;
                currentFPS = runFPS;
                currentFrame = 0;
                frameTimer = 0;
                break;

            default:
                // 알 수 없는 애니메이션은 idle 처리
                isPlaying = false;
                currentFrame = 0;
                break;
        }
    }

    /// <summary>원샷 애니메이션. 한 사이클 재생 후 콜백.</summary>
    public void PlayOneShot(string animName, Action onComplete = null)
    {
        if (walkFrames == null || walkFrames.Length == 0) return;

        isOneShot = true;
        isPlaying = true;
        IsAnimComplete = false;
        currentFrame = 0;
        frameTimer = 0;
        currentFPS = walkFPS;
        oneShotCallback = onComplete;
        currentAnim = animName;
    }

    /// <summary>방향 설정 — flipX 처리.</summary>
    public void SetDirection(Vector3 worldDir)
    {
        if (targetRenderer == null || mainCam == null) return;

        Vector3 camRight = mainCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        targetRenderer.flipX = Vector3.Dot(worldDir, camRight) < 0;
    }

    #endregion
}
