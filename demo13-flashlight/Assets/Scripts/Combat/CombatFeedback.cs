using UnityEngine;
using System.Collections;

/// <summary>
/// 전투 피드백 통합: 피격 반응 (HitFeedback) + 공격 돌진 (AttackLunge).
/// Player, Enemy 모두에 사용 가능한 공용 컴포넌트.
/// </summary>
public class CombatFeedback : MonoBehaviour
{
    // ─── 피격 흰 플래시 (셰이더 _FlashAmount — HitFlash에 위임) ───
    [Header("Hit Flash (셰이더 흰 플래시)")]
    [SerializeField] float lightFlashDuration   = 0.06f;
    [SerializeField] float heavyFlashDuration   = 0.12f;
    [Tooltip("이 데미지 이상이면 강공 플래시(길게)")]
    [SerializeField] float heavyDamageThreshold = 18f;

    // ─── 스케일 펀치 ───
    [Header("Scale Punch")]
    [SerializeField] float punchScale = 1.25f;
    [SerializeField] float punchDuration = 0.15f;
    [SerializeField] AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ─── 넉백 ───
    [Header("Knockback")]
    [SerializeField] float knockbackDist = 0.15f;
    [SerializeField] float knockbackDuration = 0.1f;

    // ─── 히트스탑 (비활성화 — Time.timeScale 조작 제거) ───
    // [Header("Freeze Frame")]
    // [SerializeField] float freezeDuration = 0.05f;

    // ─── 약공격 돌진 ───
    [Header("Attack Lunge - Light")]
    [SerializeField] float lightLungeDist = 0.3f;
    [SerializeField] float lightLungeDuration = 0.08f;
    [SerializeField] float lightReturnDuration = 0.12f;

    // ─── 강공격 돌진 ───
    [Header("Attack Lunge - Heavy")]
    [SerializeField] float heavyLungeDist = 0.6f;
    [SerializeField] float heavyLungeDuration = 0.06f;
    [SerializeField] float heavyReturnDuration = 0.18f;

    // 내부 참조
    Health health;
    HitFlash hitFlash;
    Transform visualRoot;
    Vector3 originalScale;

    // 코루틴 핸들
    Coroutine punchRoutine;
    Coroutine knockbackRoutine;
    Coroutine lungeRoutine;


    void Awake()
    {
        health = GetComponent<Health>();

        // Root 자식 찾기 (Player/Enemy 공통 구조)
        var root = transform.Find("Root");
        visualRoot = root != null ? root : transform;
        originalScale = visualRoot.localScale;

        // 흰 플래시 — HitFlash 컴포넌트 (없으면 자동 부착)
        hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDamaged += OnHit;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= OnHit;

    }

    // ────────────────────────────────────────
    // 피격 처리
    // ────────────────────────────────────────

    void OnHit(float damage)
    {
        // 무적 체크 (플레이어 구르기 중)
        var player = GetComponent<TopDownPlayer>();
        if (player != null && player.IsInvincible) return;

        // 흰 플래시 (셰이더, 강공일수록 길게)
        float flashDur = damage >= heavyDamageThreshold ? heavyFlashDuration : lightFlashDuration;
        hitFlash?.Flash(1f, flashDur);

        // 스케일 펀치
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine());

        // 넉백 (공격자 반대 방향)
        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackRoutine());

        // 히트스탑은 공격 측에서 처리 — AttackPerformer가 강공 적중 시 Hitstop.Do() 호출(안전 재도입).
    }

    IEnumerator PunchRoutine()
    {
        float t = 0;
        float halfDur = punchDuration * 0.4f;  // 빠르게 커지고
        float restDur = punchDuration * 0.6f;  // 천천히 돌아옴

        // 커지기
        while (t < halfDur)
        {
            t += Time.deltaTime;
            float pct = t / halfDur;
            float s = Mathf.Lerp(1f, punchScale, pct);
            visualRoot.localScale = originalScale * s;
            yield return null;
        }

        // 돌아오기
        t = 0;
        while (t < restDur)
        {
            t += Time.deltaTime;
            float pct = punchCurve.Evaluate(t / restDur);
            float s = Mathf.Lerp(punchScale, 1f, pct);
            visualRoot.localScale = originalScale * s;
            yield return null;
        }

        visualRoot.localScale = originalScale;
    }

    IEnumerator KnockbackRoutine()
    {
        // 공격자(플레이어) 위치에서 반대 방향으로 밀기
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) yield break;

        // 2026-07-11: **플레이어는 넉백하지 않는다.**
        //   구: dir = self - player = 0 → `Vector3.down` 폴백이라 **플레이어가 맞을 때마다 항상 아래로** 밀렸다
        //   (방향이 무의미한 버그). 사용자 요청도 "맞으면 히트 표기만" → 이동 연출 제거.
        if (GetComponent<TopDownPlayer>() != null) yield break;

        Vector3 dir = (transform.position - playerGO.transform.position);
        dir.z = 0; // 2D XY 평면
        if (dir.sqrMagnitude < 0.01f) yield break;   // 방향을 못 구하면 밀지 않는다(엉뚱한 방향 금지)
        dir.Normalize();

        // Rigidbody가 있으면 물리 경로로 이동 — transform 직접 대입은 Dynamic RB에서 지터·벽 관통을 만든다.
        var rb = GetComponent<Rigidbody>();
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + dir * knockbackDist;

        float t = 0;
        while (t < knockbackDuration)
        {
            t += Time.deltaTime;
            float pct = 1f - Mathf.Pow(1f - (t / knockbackDuration), 2); // ease out
            Vector3 p = Vector3.Lerp(startPos, endPos, pct);
            if (rb != null) rb.MovePosition(p);
            else            transform.position = p;
            yield return null;
        }
    }


    // ────────────────────────────────────────
    // 공격 돌진
    // ────────────────────────────────────────

    /// <summary>약공격 돌진</summary>
    public void DoLightLunge(Vector3 direction)
    {
        DoLunge(direction, lightLungeDist, lightLungeDuration, lightReturnDuration);
    }

    /// <summary>강공격 돌진</summary>
    public void DoHeavyLunge(Vector3 direction)
    {
        DoLunge(direction, heavyLungeDist, heavyLungeDuration, heavyReturnDuration);
    }

    void DoLunge(Vector3 direction, float dist, float forwardDur, float returnDur)
    {
        if (lungeRoutine != null)
            StopCoroutine(lungeRoutine);
        lungeRoutine = StartCoroutine(LungeRoutine(direction.normalized, dist, forwardDur, returnDur));
    }

    IEnumerator LungeRoutine(Vector3 dir, float dist, float forwardDur, float returnDur)
    {
        Vector3 startPos = transform.position;
        Vector3 lungeTarget = startPos + dir * dist;

        // 전진 (ease out cubic — 처음에 빠르고 끝에 느림)
        float t = 0;
        while (t < forwardDur)
        {
            t += Time.deltaTime;
            float pct = t / forwardDur;
            float eased = 1f - Mathf.Pow(1f - pct, 3f);
            transform.position = Vector3.Lerp(startPos, lungeTarget, eased);
            yield return null;
        }

        // 복귀 (ease in-out)
        t = 0;
        Vector3 lungedPos = transform.position;
        while (t < returnDur)
        {
            t += Time.deltaTime;
            float pct = t / returnDur;
            float eased = pct * pct * (3f - 2f * pct);
            transform.position = Vector3.Lerp(lungedPos, startPos, eased);
            yield return null;
        }

        transform.position = startPos;
        lungeRoutine = null;
    }

}

// ════════════════════════════════════════════
// 데미지 팝업 — 2D 월드 플로팅 텍스트
// ════════════════════════════════════════════

/// <summary>
/// 2D 월드(XY)에 데미지 숫자 팝업. 화면 위(+Y)로 떠오르며 페이드 아웃.
/// 강공격/그로기 시 크게. 카메라가 고정된 2D라 빌보드는 생성 시 1회만.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    public enum DamageType
    {
        Normal,     // 약공격 — 흰색, 작음
        Heavy,      // 강공격 — 주황, 중간
        Critical,   // 풀차지/그로기 — 노랑, 큼
        Heal        // 힐 — 초록
    }

    float lifetime;
    float maxLifetime;
    float floatSpeed;
    float startScale;
    TextMesh textMesh;
    Color startColor;
    Camera mainCam;
    Vector3 velocity;

    /// <summary>데미지 팝업 생성 (static 팩토리)</summary>
    public static DamagePopup Create(Vector3 worldPos, float damage, DamageType type = DamageType.Normal)
    {
        var go = new GameObject("DmgPopup");
        // 2D: XY 평면. 엔티티 위(+Y)로 살짝, 좌우 랜덤. Z(깊이)는 건드리지 않음.
        go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0.9f, 0f);

        var popup = go.AddComponent<DamagePopup>();
        popup.Init(damage, type);
        return popup;
    }

    void Init(float damage, DamageType type)
    {
        mainCam = Camera.main;

        // TextMesh 생성
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = 0.08f;
        textMesh.fontStyle = FontStyle.Bold;

        // MeshRenderer 설정
        var mr = GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = 100;

        // 타입별 설정
        switch (type)
        {
            case DamageType.Normal:
                textMesh.text = $"-{Mathf.FloorToInt(damage)}";
                textMesh.fontSize = 60;
                startColor = Color.white;
                startScale = 0.8f;
                maxLifetime = 0.7f;
                floatSpeed = 1.5f;
                break;

            case DamageType.Heavy:
                textMesh.text = $"-{Mathf.FloorToInt(damage)}";
                textMesh.fontSize = 75;
                startColor = new Color(1f, 0.6f, 0.1f); // 주황
                startScale = 1.2f;
                maxLifetime = 0.9f;
                floatSpeed = 1.8f;
                break;

            case DamageType.Critical:
                textMesh.text = $"{Mathf.FloorToInt(damage)}!";
                textMesh.fontSize = 90;
                startColor = new Color(1f, 1f, 0.2f); // 노랑
                startScale = 1.6f;
                maxLifetime = 1.1f;
                floatSpeed = 2f;
                break;

            case DamageType.Heal:
                textMesh.text = $"+{Mathf.FloorToInt(damage)}";
                textMesh.fontSize = 55;
                startColor = new Color(0.3f, 1f, 0.3f); // 초록
                startScale = 0.7f;
                maxLifetime = 0.8f;
                floatSpeed = 1.2f;
                break;
        }

        textMesh.color = startColor;
        transform.localScale = Vector3.one * startScale;
        lifetime = maxLifetime;

        // 살짝 랜덤 방향으로 튀기 (XY, Z=0)
        velocity = new Vector3(Random.Range(-0.3f, 0.3f), floatSpeed, 0f);

        // 카메라가 고정된 2D — 빌보드는 생성 시 1회만 (매 프레임 갱신 불필요)
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;

        // 크리티컬/강공은 처음에 크게 시작해서 줄어드는 효과
        if (type == DamageType.Critical || type == DamageType.Heavy)
        {
            transform.localScale = Vector3.one * startScale * 1.5f;
        }
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        float pct = lifetime / maxLifetime; // 1→0

        // 위로 떠오르기 (속도 감소)
        velocity.y = Mathf.Lerp(floatSpeed * 0.2f, floatSpeed, pct);
        transform.position += velocity * Time.deltaTime;

        // 페이드 아웃 (마지막 30%에서)
        float alpha = pct < 0.3f ? pct / 0.3f : 1f;
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        // 스케일 — 처음에 팍 커졌다가 원래로
        float scalePct = Mathf.Lerp(startScale * 0.6f, startScale, Mathf.Pow(pct, 0.5f));
        transform.localScale = Vector3.one * scalePct;
    }
}
