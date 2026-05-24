using UnityEngine;

/// <summary>
/// 부상 시각 효과.
/// 화면 효과: 출혈 비네트, 골절 흔들림, 통증 흐림
/// 캐릭터 효과: 출혈 파티클, 골절 절뚝거림, 통증 깜빡임
/// Player에 부착.
/// </summary>
public class InjuryVFX : MonoBehaviour
{
    [Header("출혈 — 화면 비네트")]
    [SerializeField] Color bleedVignetteColor = new Color(0.5f, 0, 0, 0.6f);
    [SerializeField] float bleedPulseSpeed = 2f;

    [Header("골절 — 화면 흔들림")]
    [SerializeField] float fractureShakeIntensity = 0.02f;
    [SerializeField] float fractureShakeSpeed = 15f;

    [Header("통증 — 화면 흐림 (비네트 + 밝기 저하)")]
    [SerializeField] Color painOverlayColor = new Color(0.2f, 0.1f, 0.3f, 0.3f);

    [Header("캐릭터 효과")]
    [SerializeField] float bleedParticleInterval = 0.8f;
    [SerializeField] Color bleedParticleColor = new Color(0.8f, 0.05f, 0.05f);
    [SerializeField] float painBlinkInterval = 1.5f;
    [SerializeField] float limpBobAmount = 0.04f;

    // 레퍼런스
    PlayerMedicalSystem medical;
    Transform visualRoot;
    SpriteRenderer spriteRenderer;
    Camera cam;

    // 내부 상태
    Texture2D vignetteTex;
    float bleedPulseTimer;
    float shakeTimer;
    float bleedParticleTimer;
    float painBlinkTimer;
    float limpTimer;
    Vector3 originalCamLocalPos;
    bool camShaking;

    void Start()
    {
        medical = GetComponent<PlayerMedicalSystem>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cam = Camera.main;

        var root = transform.Find("Root");
        visualRoot = root != null ? root : transform;

        vignetteTex = new Texture2D(1, 1);
        vignetteTex.SetPixel(0, 0, Color.white);
        vignetteTex.Apply();
    }

    void Update()
    {
        if (medical == null) return;

        float dt = Time.deltaTime;

        // 출혈 파티클
        UpdateBleedParticles(dt);

        // 통증 깜빡임
        UpdatePainBlink(dt);

        // 골절 절뚝거림 (다리)
        UpdateLimp(dt);

        // 골절 카메라 쉐이크
        UpdateCameraShake(dt);
    }

    void OnGUI()
    {
        if (medical == null) return;

        // 출혈 비네트
        float bleedSeverity = GetTotalSeverity(InjuryType.Bleeding);
        if (bleedSeverity > 0)
            DrawBleedVignette(bleedSeverity);

        // 통증 오버레이
        float painSeverity = GetTotalSeverity(InjuryType.Pain);
        if (painSeverity > 0)
            DrawPainOverlay(painSeverity);
    }

    #region 화면 효과

    void DrawBleedVignette(float severity)
    {
        bleedPulseTimer += Time.unscaledDeltaTime * bleedPulseSpeed;
        float pulse = (Mathf.Sin(bleedPulseTimer) * 0.5f + 0.5f) * 0.4f + 0.6f;
        float alpha = bleedVignetteColor.a * severity * pulse;

        GUI.color = new Color(bleedVignetteColor.r, bleedVignetteColor.g, bleedVignetteColor.b, alpha);

        // 상하좌우 비네트 바
        float thickness = 60f * severity;
        // 상단
        GUI.DrawTexture(new Rect(0, 0, Screen.width, thickness), vignetteTex);
        // 하단
        GUI.DrawTexture(new Rect(0, Screen.height - thickness, Screen.width, thickness), vignetteTex);
        // 좌측
        GUI.DrawTexture(new Rect(0, 0, thickness * 0.7f, Screen.height), vignetteTex);
        // 우측
        GUI.DrawTexture(new Rect(Screen.width - thickness * 0.7f, 0, thickness * 0.7f, Screen.height), vignetteTex);

        GUI.color = Color.white;
    }

    void DrawPainOverlay(float severity)
    {
        float alpha = painOverlayColor.a * severity * 0.7f;
        // 부드러운 펄스
        alpha *= (Mathf.Sin(Time.unscaledTime * 1.5f) * 0.2f + 0.8f);

        GUI.color = new Color(painOverlayColor.r, painOverlayColor.g, painOverlayColor.b, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTex);
        GUI.color = Color.white;
    }

    #endregion

    #region 캐릭터 효과

    void UpdateBleedParticles(float dt)
    {
        float bleedSeverity = GetTotalSeverity(InjuryType.Bleeding);
        if (bleedSeverity <= 0) return;

        bleedParticleTimer -= dt;
        if (bleedParticleTimer <= 0)
        {
            bleedParticleTimer = bleedParticleInterval / bleedSeverity;
            SpawnBleedDrop();
        }
    }

    void SpawnBleedDrop()
    {
        // 심플한 피 방울 — 작은 오브젝트 생성 후 자동 파괴
        var drop = new GameObject("BloodDrop");
        drop.transform.position = transform.position + new Vector3(
            Random.Range(-0.2f, 0.2f), 0.1f, Random.Range(-0.2f, 0.2f));

        var sr = drop.AddComponent<SpriteRenderer>();
        sr.color = bleedParticleColor;
        // 기본 스프라이트 없으면 머티리얼 색만 보임 — 작은 쿼드로 대체
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(drop.transform);
        quad.transform.localScale = Vector3.one * 0.08f;
        quad.transform.localRotation = Quaternion.Euler(90, 0, 0);

        var quadRenderer = quad.GetComponent<MeshRenderer>();
        if (quadRenderer != null)
        {
            quadRenderer.material = new Material(Shader.Find("Sprites/Default"));
            quadRenderer.material.color = bleedParticleColor;
        }

        // 콜라이더 제거
        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Destroy(sr); // SpriteRenderer는 불필요
        Destroy(drop, 1.5f); // 1.5초 후 자동 파괴

        // 간단한 낙하 — Rigidbody 없이 코루틴 대신 별도 스크립트
        drop.AddComponent<BloodDropFall>();
    }

    void UpdatePainBlink(float dt)
    {
        float painSeverity = GetTotalSeverity(InjuryType.Pain);
        if (painSeverity <= 0 || spriteRenderer == null) return;

        painBlinkTimer -= dt;
        if (painBlinkTimer <= 0)
        {
            painBlinkTimer = painBlinkInterval;
            // 잠깐 어두워졌다 복귀
            StartCoroutine(PainBlinkRoutine());
        }
    }

    System.Collections.IEnumerator PainBlinkRoutine()
    {
        if (spriteRenderer == null) yield break;

        Color orig = spriteRenderer.color;
        spriteRenderer.color = new Color(orig.r * 0.6f, orig.g * 0.5f, orig.b * 0.7f, orig.a);
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null)
            spriteRenderer.color = orig;
    }

    void UpdateLimp(float dt)
    {
        if (visualRoot == null) return;

        bool hasLegFracture = medical.LeftLeg.HasInjury(InjuryType.Fracture) ||
                              medical.RightLeg.HasInjury(InjuryType.Fracture);

        if (!hasLegFracture) return;

        // 절뚝거리는 추가 바운스 (비대칭)
        limpTimer += dt * 4f;
        float limp = Mathf.Abs(Mathf.Sin(limpTimer * 2.3f)) * limpBobAmount;
        Vector3 pos = visualRoot.localPosition;
        pos.y += limp * (Mathf.Sin(limpTimer * 5f) > 0 ? 1f : -0.5f);
        // 직접 수정하지 않고 다른 시스템(WalkBounce)과 충돌할 수 있으므로
        // 여기서는 scale을 살짝 비대칭으로 만들어 절뚝 느낌
        Vector3 scale = visualRoot.localScale;
        float skew = Mathf.Sin(limpTimer) * 0.03f;
        visualRoot.localScale = new Vector3(scale.x, 1f + skew, scale.z);
    }

    void UpdateCameraShake(float dt)
    {
        float fractureSeverity = GetTotalSeverity(InjuryType.Fracture);
        if (fractureSeverity <= 0 || cam == null) return;

        // 이동 중에만 흔들림
        if (medical.GetComponent<PlayerController>()?.CurrentState != PlayerController.CombatState.Idle)
            return;

        shakeTimer += dt * fractureShakeSpeed;
        float intensity = fractureShakeIntensity * fractureSeverity;

        // CameraFollow가 위치를 매 프레임 덮어쓰므로, 약간의 rotation으로 표현
        float shakeZ = Mathf.Sin(shakeTimer) * intensity * 2f;
        cam.transform.localRotation = Quaternion.Euler(
            cam.transform.localRotation.eulerAngles.x,
            cam.transform.localRotation.eulerAngles.y,
            shakeZ);
    }

    #endregion

    #region 유틸

    float GetTotalSeverity(InjuryType type)
    {
        if (medical == null) return 0;
        float total = 0;
        var parts = medical.GetAllParts();
        for (int i = 0; i < parts.Length; i++)
            total += parts[i].GetSeverity(type);
        return Mathf.Clamp01(total);
    }

    #endregion
}

/// <summary>
/// 피 방울 낙하 간단 스크립트.
/// </summary>
public class BloodDropFall : MonoBehaviour
{
    float speed = 0;

    void Update()
    {
        speed += Time.deltaTime * 2f;
        transform.position += Vector3.down * speed * Time.deltaTime;

        // 바닥 아래로 가면 정지
        if (transform.position.y < -0.1f)
        {
            enabled = false;
        }
    }
}
