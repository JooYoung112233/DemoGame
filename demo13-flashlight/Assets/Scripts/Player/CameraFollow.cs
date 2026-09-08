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
    [Tooltip("카메라 추적 반응 속도. 클수록 즉각적. 지수 감쇠 계수라 프레임레이트와 무관하다.")]
    [SerializeField] float smoothSpeed = 14f;
    [Tooltip("3D 쿼터뷰에서 카메라가 타깃 뒤로 물러나는 거리(m). " +
             "⚠️ 그림자 거리(URP 에셋 기본 50m) 안이어야 그림자가 렌더된다.")]
    [SerializeField] float followDistance = 25f;

    Vector3 offset;

    // ── 연출 포커스 ──
    // 시설 UI를 열 때 피사체를 화면 한쪽으로 밀어 UI 자리를 비운다(캐릭터를 가리지 않기 위해).
    Transform _focus;
    Vector2   _focusBias;     // 화면 비율. (0,+0.25) = 피사체를 화면 위쪽으로
    float     _focusSize;     // 0이면 오소 크기 유지
    bool      _hasFocus;
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
            // 3D 쿼터뷰: 카메라는 **시선 축을 따라** 타깃 뒤에 선다.
            // 구 2D 방식(transform.right/up으로 화면 중앙 맞추기)은 카메라가 정면을 볼 때만
            // 성립한다 — 55°로 기울면 up이 Z 성분을 가져 오프셋이 어긋난다.
            offset = -transform.forward * followDistance;
            transform.position = target.position + offset;
            _basePos = transform.position;
            offsetInitialized = true;
        }
    }

    /// <summary>시설 UI 등에서 피사체를 화면 한쪽으로 밀어 UI 자리를 비운다.
    /// <paramref name="screenBias"/>는 화면 비율 — (0, 0.25)면 피사체가 화면 위쪽 1/4쯤으로 올라간다.
    /// 카메라는 스무딩으로 부드럽게 이동한다(순간이동 아님).</summary>
    public void SetFocus(Transform focus, Vector2 screenBias, float orthoSize = 0f)
    {
        _focus = focus; _focusBias = screenBias; _focusSize = orthoSize; _hasFocus = true;
    }

    /// <summary>연출 포커스 해제 — 다시 플레이어를 화면 중앙에 둔다.</summary>
    public void ClearFocus()
    {
        _hasFocus = false; _focus = null; _focusBias = Vector2.zero;
        if (cam != null && baseOrthoSize > 0f) _focusSize = baseOrthoSize;
    }

    /// <summary>화면 비율 편향을 월드 이동으로 바꾼다. 카메라가 기울어 있으므로
    /// 화면 '위'는 월드에서 카메라 forward를 지면에 눕힌 방향이다.</summary>
    Vector3 FramingShift()
    {
        if (!_hasFocus || cam == null) return Vector3.zero;
        float half = cam.orthographic ? cam.orthographicSize : 10f;
        Vector3 right = transform.right;
        Vector3 up    = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        // 피사체를 위로 올리려면 카메라가 볼 지점을 아래로 내린다 → 부호 반전
        return (-right * _focusBias.x - up * _focusBias.y) * (half * 2f);
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

        Vector3 anchor = _hasFocus && _focus != null ? _focus.position : target.position;
        Vector3 desired = anchor + offset + FramingShift();
        // ⚠️ Lerp(a, b, k*dt)는 **프레임레이트에 의존한다** — 같은 smoothSpeed라도 fps에 따라
        //    따라오는 속도가 달라진다. 지수 감쇠 1-exp(-k*dt)가 프레임레이트와 무관한 정식이다.
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
        _basePos = Vector3.Lerp(_basePos, desired, t);
        if (cam != null && _hasFocus && _focusSize > 0f)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, _focusSize, t);
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
