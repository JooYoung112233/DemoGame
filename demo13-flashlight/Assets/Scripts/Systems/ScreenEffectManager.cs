using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// 스토리 연출용 화면 이펙트 매니저.
/// PostProcessController(분위기 프리셋)와 별도로, 일회성 연출 이펙트 트리거.
/// - 색수차 펄스
/// - 화면 쉐이크
/// - 프리즈 프레임
/// - 페이드 인/아웃
/// - 화면 플래시
/// </summary>
public class ScreenEffectManager : MonoBehaviour
{
    public static ScreenEffectManager Instance { get; private set; }

    Volume volume;
    ChromaticAberration chromatic;
    Vignette vignette;
    ColorAdjustments colorAdjustments;
    float savedSaturation = 0f;
    bool grayscaleActive;
    Camera mainCamera;

    // 페이드용
    [SerializeField] Canvas fadeCanvas;
    [SerializeField] UnityEngine.UI.Image fadeImage;

    public bool IsGenerated => fadeCanvas != null;

    // 쉐이크 상태
    Vector3 originalCamPos;
    Coroutine shakeCoroutine;
    Coroutine chromaticCoroutine;
    Coroutine vignetteCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!IsGenerated) GenerateUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        CacheVolume();
    }

    void CacheVolume()
    {
        volume = FindFirstObjectByType<Volume>();
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out chromatic);
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out colorAdjustments);
        }
        mainCamera = Camera.main;
    }

    /// <summary>S-018 등 — 화면 흑백(과거/기억 연출).</summary>
    public void SetGrayscale(bool on)
    {
        if (colorAdjustments == null) CacheVolume();
        if (colorAdjustments == null) return;

        if (on)
        {
            if (!grayscaleActive)
                savedSaturation = colorAdjustments.saturation.value;
            colorAdjustments.saturation.Override(-100f);
            grayscaleActive = true;
        }
        else if (grayscaleActive)
        {
            colorAdjustments.saturation.Override(savedSaturation);
            grayscaleActive = false;
        }
    }

    public void GenerateUI()
    {
        var fadeGO = new GameObject("FadeCanvas");
        fadeGO.transform.SetParent(transform);

        fadeCanvas = fadeGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;

        var imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(fadeGO.transform, false);
        fadeImage = imgGO.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;
        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeGO.SetActive(false);
    }

    public void ClearGeneratedUI()
    {
        var child = transform.Find("FadeCanvas");
        if (child != null)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        fadeCanvas = null;
        fadeImage = null;
    }

    // ═══════════════════════════════
    //  색수차 펄스
    // ═══════════════════════════════

    /// <summary>
    /// 색수차를 잠깐 강하게 올렸다가 원래대로 복귀.
    /// S-015_WATCH 시계 이상현상에 사용.
    /// </summary>
    public void ChromaticPulse(float intensity = 1f, float duration = 1.5f)
    {
        if (chromatic == null) CacheVolume();
        if (chromatic == null) return;

        if (chromaticCoroutine != null) StopCoroutine(chromaticCoroutine);
        chromaticCoroutine = StartCoroutine(ChromaticPulseRoutine(intensity, duration));
    }

    IEnumerator ChromaticPulseRoutine(float intensity, float duration)
    {
        float original = chromatic.intensity.value;
        chromatic.intensity.Override(intensity);

        float half = duration * 0.3f; // 빠르게 올라가고 천천히 내려옴
        yield return new WaitForSecondsRealtime(half);

        float t = 0;
        float remaining = duration - half;
        while (t < remaining)
        {
            t += Time.unscaledDeltaTime;
            chromatic.intensity.Override(Mathf.Lerp(intensity, original, t / remaining));
            yield return null;
        }
        chromatic.intensity.Override(original);
        chromaticCoroutine = null;
    }

    // ═══════════════════════════════
    //  비네트 펄스 (피격)
    // ═══════════════════════════════

    /// <summary>
    /// 비네트를 잠깐 강하게 올렸다가 복귀. 플레이어 피격 시 가장자리 붉은 펄스.
    /// color 지정 시 펄스 동안 비네트 색을 덮었다가 원복.
    /// </summary>
    public void VignettePulse(float intensity = 0.4f, float duration = 0.5f, Color? color = null)
    {
        if (vignette == null) CacheVolume();
        if (vignette == null) return;

        if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
        vignetteCoroutine = StartCoroutine(VignettePulseRoutine(intensity, duration, color));
    }

    IEnumerator VignettePulseRoutine(float intensity, float duration, Color? color)
    {
        float origIntensity = vignette.intensity.value;
        Color origColor     = vignette.color.value;

        float peak = Mathf.Max(origIntensity, intensity);
        if (color.HasValue) vignette.color.Override(color.Value);
        vignette.intensity.Override(peak);

        float half = duration * 0.25f; // 빠르게 올라가고 천천히 내려옴
        yield return new WaitForSecondsRealtime(half);

        float t = 0f;
        float remaining = duration - half;
        while (t < remaining)
        {
            t += Time.unscaledDeltaTime;
            vignette.intensity.Override(Mathf.Lerp(peak, origIntensity, t / remaining));
            yield return null;
        }
        vignette.intensity.Override(origIntensity);
        if (color.HasValue) vignette.color.Override(origColor);
        vignetteCoroutine = null;
    }

    // ═══════════════════════════════
    //  화면 쉐이크
    // ═══════════════════════════════

    /// <summary>
    /// 카메라 쉐이크. S-015 루디 획득 순간 등에 사용.
    /// </summary>
    public void ScreenShake(float intensity = 0.15f, float duration = 0.3f)
    {
        // CameraFollow가 있으면 오프셋 레이어로 위임(카메라 추적과 충돌 방지)
        if (CameraFollow.Instance != null) { CameraFollow.Instance.Shake(intensity, duration); return; }

        // 폴백: 추적 카메라가 없을 때만 localPosition 직접 흔들기
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    IEnumerator ShakeRoutine(float intensity, float duration)
    {
        originalCamPos = mainCamera.transform.localPosition;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float dampened = intensity * (1f - elapsed / duration);
            float x = Random.Range(-dampened, dampened);
            float y = Random.Range(-dampened, dampened);
            mainCamera.transform.localPosition = originalCamPos + new Vector3(x, y, 0);
            yield return null;
        }

        mainCamera.transform.localPosition = originalCamPos;
        shakeCoroutine = null;
    }

    // ═══════════════════════════════
    //  프리즈 프레임
    // ═══════════════════════════════

    /// <summary>
    /// 짧은 시간 정지 효과. S-015_WATCH 시계 멈춤에 사용.
    /// unscaledDeltaTime 사용하므로 timeScale=0에서도 동작.
    /// </summary>
    public void FreezeFrame(float duration = 0.5f)
    {
        StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        float prev = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
    }

    // ═══════════════════════════════
    //  페이드 인/아웃
    // ═══════════════════════════════

    /// <summary>
    /// 화면을 검은색으로 페이드 아웃.
    /// </summary>
    public Coroutine FadeOut(float duration = 1f, System.Action onComplete = null)
    {
        return StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
    }

    /// <summary>
    /// 검은 화면에서 페이드 인.
    /// </summary>
    public Coroutine FadeIn(float duration = 1f, System.Action onComplete = null)
    {
        return StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
    }

    /// <summary>즉시 화면을 검게 덮는다(페이드 없이). 씬 전환 커버를 seam 없이 이어받을 때 —
    /// sortingOrder 999 uGUI 오버레이라 HUD/월드를 확실히 덮는다. 이후 fade_in으로 드러낸다.</summary>
    public void CoverInstant()
    {
        if (!IsGenerated) GenerateUI();
        fadeCanvas.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;
        fadeImage.color = new Color(0, 0, 0, 1f);
    }

    IEnumerator FadeRoutine(float from, float to, float duration, System.Action onComplete)
    {
        fadeCanvas.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;
        float t = 0;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, to);

        if (to <= 0f)
        {
            fadeImage.raycastTarget = false;
            fadeCanvas.gameObject.SetActive(false);
        }

        onComplete?.Invoke();
    }

    // ═══════════════════════════════
    //  화면 플래시
    // ═══════════════════════════════

    /// <summary>
    /// 짧은 백색 플래시. 기억 재현 등에 사용.
    /// </summary>
    public void Flash(Color color, float duration = 0.3f)
    {
        StartCoroutine(FlashRoutine(color, duration));
    }

    public void WhiteFlash(float duration = 0.3f)
    {
        Flash(Color.white, duration);
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        fadeCanvas.gameObject.SetActive(true);
        fadeImage.color = color;
        fadeImage.raycastTarget = false;

        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, t / duration);
            fadeImage.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0);
        fadeCanvas.gameObject.SetActive(false);
    }
}
