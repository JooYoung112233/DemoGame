using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 카메라 추적 + 타격감 오프셋 레이어(셰이크/줌 펀치).
/// 추적 위치는 _basePos에 스무딩으로 누적하고, 셰이크/줌은 그 위에 가산 →
/// 카메라 추적과 충돌하지 않음(구 ScreenEffectManager의 localPosition 직접 흔들기 문제 해결).
/// 셰이크/줌 타이머는 unscaledDeltaTime → 히트스탑(timeScale=0) 중에도 보임.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    // 카메라는 PlayerRig 프리팹에 포함되어 DontDestroyOnLoad로 모든 씬 공유.
    // 씬마다 있는 다른 카메라(메인/AudioListener)는 충돌하므로 씬 로드 시 비활성화한다.

    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;

    Vector3 offset;
    Vector3 _basePos;        // 셰이크 제외한 추적 위치(스무딩 누적용)
    Camera cam;
    float baseOrthoSize;
    bool offsetInitialized;

    // ── 셰이크 ──
    float _shakeMag, _shakeDur, _shakeTime;
    // ── 줌 펀치 ──
    float _zoomAmt, _zoomDur, _zoomTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic) baseOrthoSize = cam.orthographicSize;
        FindTarget();
        DisableOtherCameras();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // target은 DontDestroyOnLoad 플레이어라 유지. 새 씬의 다른 카메라만 비활성.
        DisableOtherCameras();
    }

    /// <summary>이 카메라(PlayerRig) 외 씬의 다른 Camera/AudioListener를 비활성 (2개 충돌 방지).</summary>
    void DisableOtherCameras()
    {
        var cams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c == null || c.gameObject == gameObject) continue;
            c.enabled = false;
            var al = c.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
        }
    }

    void FindTarget()
    {
        if (target == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                target = playerGO.transform;
        }

        if (target != null && !offsetInitialized)
        {
            Vector3 localP = transform.InverseTransformPoint(target.position);
            transform.position += transform.right * localP.x + transform.up * localP.y;
            offset = transform.position - target.position;
            _basePos = transform.position;
            offsetInitialized = true;
        }
    }

    /// <summary>카메라를 타깃 위치로 즉시 스냅(스무딩 건너뜀). 스폰/순간이동 직후 호출 — "슉~" 슬라이드 방지.</summary>
    public void SnapToTarget()
    {
        if (target == null || !offsetInitialized) FindTarget();
        if (target == null) return;
        _basePos = target.position + offset;
        transform.position = _basePos;   // 셰이크 가산 없이 즉시
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        Vector3 desired = target.position + offset;
        _basePos = Vector3.Lerp(_basePos, desired, smoothSpeed * Time.unscaledDeltaTime);
        transform.position = _basePos + UpdateShake();
        UpdateZoom();
    }

    // ── 셰이크 ──────────────────────────────────────────────

    /// <summary>카메라 셰이크. 더 강한 셰이크가 들어오면 덮어씀.</summary>
    public void Shake(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f) return;
        if (intensity >= _shakeMag || _shakeTime >= _shakeDur)
        {
            _shakeMag = intensity;
            _shakeDur = duration;
            _shakeTime = 0f;
        }
    }

    Vector3 UpdateShake()
    {
        if (_shakeTime >= _shakeDur || _shakeDur <= 0f) return Vector3.zero;
        _shakeTime += Time.unscaledDeltaTime;
        float damp = 1f - Mathf.Clamp01(_shakeTime / _shakeDur);     // 1→0
        float mag = _shakeMag * damp * damp;                          // 끝에서 더 빠르게 잦아듦
        return new Vector3(Random.Range(-mag, mag), Random.Range(-mag, mag), 0f);
    }

    // ── 줌 펀치 ─────────────────────────────────────────────

    /// <summary>줌 펀치(살짝 당겼다 복귀). amount=최대 줌인 비율(예: 0.06=6%).</summary>
    public void ZoomPunch(float amount, float duration)
    {
        if (cam == null || !cam.orthographic || amount <= 0f || duration <= 0f) return;
        _zoomAmt = amount;
        _zoomDur = duration;
        _zoomTime = 0f;
    }

    void UpdateZoom()
    {
        if (cam == null || !cam.orthographic || baseOrthoSize <= 0f) return;
        if (_zoomDur <= 0f) return;

        if (_zoomTime >= _zoomDur)
        {
            cam.orthographicSize = baseOrthoSize;
            _zoomDur = 0f;
            return;
        }
        _zoomTime += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(_zoomTime / _zoomDur);
        float pulse = Mathf.Sin(p * Mathf.PI);                        // 0→1→0
        cam.orthographicSize = baseOrthoSize * (1f - _zoomAmt * pulse);
    }
}
