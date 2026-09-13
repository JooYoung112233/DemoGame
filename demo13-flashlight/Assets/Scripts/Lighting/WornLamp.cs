using UnityEngine;

/// <summary>
/// 착용 랜턴 — **옷에 달린 작은 등.** 플레이어 몸에 붙어 늘 켜져 있다.
///
/// 왜 손전등이 아닌가(2026-09-09 사용자 결정): <b>들 손이 없다.</b> 근접무기·총을 드는
/// 손과 광원을 드는 손이 겹친다. 그래서 빛은 손이 아니라 <b>옷(리그)에</b> 달린다.
/// 이건 2026-06-02에 이미 정한 "손전등 F토글 폐기 → 착용 랜턴 상시 점등"의 3D 구현이며
/// (docs/rendering.md §랜턴), 그때 미완으로 남아 있던 항목이다.
///
/// <b>토글이 없다.</b> 플레이어가 켜고 끄지 않는다 — 차고 있으면 켜져 있다.
/// 다만 밝기는 낮/밤에 따라 달라진다. 대낮에 랜턴 밝기를 그대로 두면 발밑에만
/// 이유 없는 노란 얼룩이 생긴다. 이건 토글이 아니라 <b>주변 밝기에 대한 반응</b>이다.
///
/// 이 등이 있어야 하는 진짜 이유는 분위기가 아니라 <b>가독성</b>이다. 밤 앰비언트를
/// 어둡게 깔아 둔 상태에서 이게 없으면 <b>조종하는 캐릭터가 검은 덩어리</b>가 된다.
///
/// 등급(밝은 랜턴을 주우면 반경·밝기가 는다)은 <see cref="SetGrade"/>로 들어온다 —
/// 장비창에 랜턴 슬롯이 생기면 그쪽에서 부르면 된다. 지금은 기본값(미착용)만 산다.
///
/// 설계: docs/rendering.md §조명/가시성 — 랜턴, docs/3d-migration.md Stage 2
/// </summary>
[RequireComponent(typeof(Light))]
public class WornLamp : MonoBehaviour
{
    [Header("기본(랜턴 미착용) — 문서상 '좁은 주변광 ~2칸, 밤엔 코앞만'")]
    [Tooltip("빛이 닿는 거리(m).")]
    [SerializeField] float baseRange = 10f;
    [Tooltip("밤 밝기. 캐릭터 실루엣이 읽힐 만큼만 — 주변을 훤히 밝히면 시야 콘이 의미를 잃는다.")]
    [SerializeField] float nightIntensity = 3.4f;
    [Tooltip("낮 밝기. 0이 아니라 아주 낮은 값 — '늘 켜져 있다'가 사실이어야 한다.")]
    [SerializeField] float dayIntensity = 0.2f;

    [Header("몸에 달린 느낌")]
    [Tooltip("3D 캐릭터의 Chest 뼈를 따라 옷 앞섶에 광원을 배치한다. 모델이 없으면 씬에 배치된 위치를 유지한다.")]
    [SerializeField] bool followAnimatedBody = true;
    [Tooltip("가슴 뼈 회전 기준 미터 단위 오프셋. FBX 뼈의 100배 스케일은 적용하지 않는다.")]
    [SerializeField] Vector3 bodyMountOffset = new Vector3(.12f, -.04f, .22f);
    [Tooltip("빛이 퍼지는 각도. 좁을수록 '가슴에 단 등'으로 읽힌다. 360°에 가까우면 머리 위 전등처럼 보인다.")]
    [Range(20f, 170f)][SerializeField] float coneAngle = 56f;
    [Tooltip("콘 안쪽(풀 밝기) 각도 비율. 가장자리를 부드럽게 풀어 준다.")]
    [Range(0.1f, 0.95f)][SerializeField] float innerRatio = 0.74f;

    [Header("주변광 (좀보이드식 — '내가 있는 자리는 보인다')")]
    // 2D 시절의 방식을 그대로 옮긴 것이다. 그때는 플레이어에 점 Light2D를 두고
    // 벽의 ShadowCaster2D가 가렸다(`FlashlightController.ambientGlow`, glowAlwaysOn).
    // 3D에서 같은 성질을 내는 건 **그림자를 켠 포인트 라이트**다.
    [Tooltip("주변광 반경(m). 2D 때 core 반경 3.0이 기준이었다. 내가 선 방 정도가 읽히는 크기.")]
    [SerializeField] float spillRange = 4.6f;
    [Tooltip("빔 밝기 대비 주변광 비율. 너무 키우면 빔의 방향감이 죽는다.")]
    [Range(0f, 1f)][SerializeField] float spillRatio = 0.04f;
    [Tooltip("낮·실내에서도 최소한 이만큼은 켜 둔다 — '내 주변은 늘 보인다'가 사실이어야 한다.")]
    [SerializeField] float spillFloor = 0.08f;
    [Tooltip("등 기준 주변 퍼짐광의 월드 높이 차이(m). 가방 윗면을 직접 밝히지 않고 발밑을 읽게 한다.")]
    [SerializeField] float spillHeightOffset = -0.65f;
    [Tooltip("⚠️ 끄면 주변광이 벽을 통과한다 — 위에서 내려다보듯 장애물 건너편까지 밝아진다. "
           + "2D의 ShadowCaster2D 가림에 해당하는 자리다.")]
    [SerializeField] bool spillCastsShadows = true;

    // 주변광은 가슴 높이 **메시 안**에 있어서, 그림자를 켜면 캐릭터가 자기 몸을 가려 새까매진다.
    // 2D 때도 같은 이유로 `ambientGlow`를 따로 뒀다. 아주 짧게(벽 너머로 샐 거리가 아니다)
    // 그림자 없이 — 몸을 읽히게 하는 것만 담당한다.
    [Tooltip("캐릭터 자신을 밝히는 글로우 반경(m). 짧아서 벽 너머로 새지 않는다.")]
    [SerializeField] float bodyGlowRange = 2.6f;
    [Tooltip("캐릭터 글로우 밝기. 실루엣이 읽힐 만큼만 — 키우면 캐릭터만 떠 보인다.")]
    [SerializeField] float bodyGlowIntensity = 0.9f;
    [Tooltip("글로우를 등 위치에서 위로 몇 m에 둘지. 2026-09-11부터 머리 위가 아니라 가슴 높이(0.1) — 위에서 내리꽂으면 '머리 위 태양'이 된다.")]
    [SerializeField] float bodyGlowHeight = 0.1f;
    [Tooltip("글로우를 카메라 쪽으로 몇 m 내밀지 — 카메라가 보는 면(얼굴·앞섶)만 옅게 밝힌다. 메시 안에 두면 밖 면이 전부 광원을 등진다.")]
    [SerializeField] float bodyGlowTowardCamera = 0.8f;

    Light _light;
    DayNightCycle _dayNight;
    TopDownPlayer _player;
    Transform _bodyAnchor;
    float _gradeRange = 1f, _gradeIntensity = 1f;   // 등급 배율(미착용 = 1)
    bool _indoorPresentation;

    /// <summary>Lit hideout presentation: keep the worn lamp on, but avoid washing out the character.</summary>
    public void SetIndoorPresentation(bool indoor)
    {
        if (_indoorPresentation == indoor) return;
        _indoorPresentation = indoor;
        Apply(_dayNight != null && _dayNight.IsNight);
    }

    void Awake()
    {
        _light = GetComponent<Light>();

        // ⚠️ **점광이 아니라 스포트다.** 점광은 사방을 고르게 비춰 완전한 원을 만든다 —
        //    그러면 몸에 단 등이 아니라 **머리 위에 떠 있는 전등**으로 읽힌다(사용자 지적).
        //    가슴에 단 등은 바라보는 쪽을 비춘다.
        _light.type = LightType.Spot;
        _light.spotAngle = coneAngle;
        _light.innerSpotAngle = coneAngle * innerRatio;

        // ⚠️ **그림자를 켠다.** 2D 시절엔 `ShadowCaster2D`가 벽에서 빛을 끊어 줬다.
        //    끄고 두면 빛이 건물 벽을 그대로 통과해 골목 밖까지 새어 나가고,
        //    그것 역시 "공중에 뜬 조명" 느낌의 원인이다. 플레이어 광원 하나뿐이라 값은 감당된다.
        _light.shadows = LightShadows.Soft;
        _light.shadowStrength = 0.9f;
        // 자기 몸에 생기는 그림자 얼룩(shadow acne)을 피한다 — 등이 몸에 붙어 있어 특히 잘 생긴다.
        _light.shadowNearPlane = 0.35f;
        _light.shadowBias = 0.08f;
        _light.shadowNormalBias = 0.5f;
    }

    /// <summary>착용자 주변광 — **"내가 서 있는 자리는 보인다."**
    ///
    /// 두 가지를 한다:
    ///   ① 빔만 있으면 캐릭터가 새까만 실루엣이 되어 "어디선가 쏘는 조명"으로 읽힌다. 자기 몸을 밝힌다.
    ///   ② 발밑·바로 옆 사물이 읽히게 한다 — 2D 시절 플레이어에 달려 있던 점 Light2D의 역할이다.
    ///
    /// ⚠️ **그림자를 켠다.** 2D에서는 벽의 `ShadowCaster2D`가 이 빛을 가려 줬는데, 3D로 옮기며
    ///    그림자 없는 점광으로 바꿔 놓아서 **벽 너머까지 밝아졌다.** 좀보이드식 "벽 뒤는 안 보인다"가
    ///    성립하려면 가림이 필수다. 그림자 캐스팅 추가 광원이 하나 늘지만, 이 게임에서 이 빛은
    ///    화면에 **항상 하나뿐**이라 감당할 만하다.</summary>
    Light _spill;
    Light _glow;

    void EnsureSpill()
    {
        if (_spill != null) return;
        var go = new GameObject("LampSpill");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        _spill = go.AddComponent<Light>();
        _spill.type = LightType.Point;
        _spill.shadows = spillCastsShadows ? LightShadows.Soft : LightShadows.None;
        _spill.shadowStrength = 0.85f;
        // 점광 그림자는 6면을 굽는다 — 근평면이 너무 작으면 자기 몸에 얼룩이 진다.
        _spill.shadowNearPlane = 0.30f;
        _spill.shadowBias = 0.10f;
        _spill.range = spillRange;
        _spill.color = _light != null ? _light.color : Color.white;
        _spill.renderMode = LightRenderMode.ForcePixel;

        var g = new GameObject("BodyGlow");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = Vector3.zero;
        _glow = g.AddComponent<Light>();
        _glow.type = LightType.Point;
        _glow.shadows = LightShadows.None;   // 몸을 밝히는 게 목적 — 그림자를 켜면 다시 자기를 가린다
        _glow.range = bodyGlowRange;
        _glow.color = new Color(0.72f, 0.78f, 0.92f);   // 2D 때 ambientGlow와 같은 계열(찬 색)
        _glow.renderMode = LightRenderMode.ForcePixel;

        // 그림자 해상도 — 점광·스포트 그림자가 추가 광원 아틀라스의 작은 칸을 받아 바닥에 **깍두기처럼** 떨어졌다
        //   (2026-09-11 사용자 "바닥에 무슨 모듈처럼 그림자가 나온다"). 플레이어 등은 화면에 늘 하나라 높은 등급을 준다.
        HighShadowTier(_light);
        HighShadowTier(_spill);
    }

    static void HighShadowTier(Light l)
    {
        if (l == null) return;
        if (!l.TryGetComponent(out UnityEngine.Rendering.Universal.UniversalAdditionalLightData ad))
            ad = l.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        ad.additionalLightsShadowResolutionTier = UnityEngine.Rendering.Universal.UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
    }

    void Start()
    {
        EnsureSpill();
        _dayNight = FindFirstObjectByType<DayNightCycle>();
        if (_dayNight != null) _dayNight.OnPhaseChanged += OnPhaseChanged;
        // 이 컴포넌트는 **Start에서 값을 적용한다.** 프로젝트의 Editor State Preservation
        // 규칙(이벤트로만 바꾼다)에서 벗어나는데, 이건 연출이 아니라 가독성 장치라
        // 첫 프레임부터 맞아 있어야 하기 때문이다. 안 그러면 T를 누르기 전까지 캄캄하다.
        Apply(_dayNight != null && _dayNight.IsNight);
    }

    void OnDestroy()
    {
        if (_dayNight != null) _dayNight.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(bool isNight) => Apply(isNight);

    /// <summary>낮/밤을 강제로 적용한다 — **룩 체크 씬·QA 전용**.
    /// 그 씬엔 DayNightCycle이 없어 늘 낮(0.2)으로 켜지는데, 그러면 빔이 안 보여 검증이 안 된다.</summary>
    public void ForcePhase(bool night) => Apply(night);

    void Apply(bool isNight)
    {
        if (_light == null) return;
        _light.range = baseRange * _gradeRange;
        float presentation = _indoorPresentation ? .15f : 1f;
        _light.intensity = (isNight ? nightIntensity : dayIntensity) * _gradeIntensity * presentation;
        _light.spotAngle = coneAngle;
        _light.innerSpotAngle = coneAngle * innerRatio;
        EnsureSpill();
        if (_spill != null)
        {
            // 주변광은 빔에 비례하되 **바닥값을 둔다** — 낮이나 실내에서 0에 가까워지면
            // "내 주변은 늘 보인다"가 깨져서, 발밑조차 안 보인다.
            _spill.intensity = Mathf.Max(spillFloor,
                                         (isNight ? nightIntensity : dayIntensity) * _gradeIntensity * spillRatio) * presentation;
            _spill.range   = spillRange * _gradeRange;
            _spill.shadows = spillCastsShadows ? LightShadows.Soft : LightShadows.None;
        }
        if (_glow != null)
        {
            _glow.intensity = bodyGlowIntensity * presentation;
            _glow.range = bodyGlowRange;
        }
    }

    /// <summary>등이 바라보는 쪽을 향하게 한다 — **몸에 달린 등이므로 몸을 따라 돈다.**
    /// 이게 없으면 방향이 고정돼 옆으로 걸을 때 엉뚱한 데를 비추고, 결국 "떠 있는 조명"이 된다.</summary>
    // 아래로 숙인 각. 34°는 **발 앞 1.7m**에 원을 만들어, 캐릭터를 둘러싼 밝은 웅덩이가 됐다
    // = "머리 위에 뜬 조명". 가슴등은 앞을 봐야 한다 — 16°면 빔이 앞으로 뻗는다.
    const float TiltDown = 16f;

    void LateUpdate()
    {
        if (_player == null) _player = TopDownPlayer.Instance;
        if (followAnimatedBody && _player != null)
        {
            if (_bodyAnchor == null)
                foreach (var bone in _player.GetComponentsInChildren<Transform>(true))
                    if (bone.name == "Chest") { _bodyAnchor = bone; break; }
            // Animator 평가 후의 가슴 위치를 사용한다. 루트의 고정 Y는 숙인 머리 안에 남는다.
            if (_bodyAnchor != null)
                transform.position = _bodyAnchor.position + _bodyAnchor.rotation * bodyMountOffset;
        }
        // ⚠️ 몸을 밝히는 글로우는 **등보다 위**, 월드 기준으로 올린다.
        //    등과 같은 자리(가슴)에 두면 점광이 메시 **안**에 갇혀, 밖에서 보이는 면이 전부
        //    광원을 등지게 된다 — 실제로 그렇게 뒀다가 캐릭터가 계속 새까맸다.
        //    쿼터뷰(55° 부감)가 보는 건 윗면이라, 위에서 내리비춰야 어깨·모자·팔이 읽힌다.
        //    ⚠️ 이 줄은 **플레이어 조기 반환보다 앞**에 있어야 한다. TopDownPlayer가 없는
        //       룩 체크 씬에서도 글로우는 제자리를 잡아야 검증이 된다(실제로 그래서 안 보였다).
        // 2026-09-11 사용자 "주변광이 머리 위에 태양 있어요 하는 것 같다" → 머리 위 1m에서 내리비추던 것을
        //   **가슴 높이·카메라 쪽**으로 옮겼다. 카메라가 보는 면만 옅게 밝혀 실루엣이 읽히게 — 해처럼 위에서 내리꽂지 않는다.
        if (_glow != null)
        {
            Vector3 toCam = Vector3.back;
            if (CameraFollow.Instance != null)
            {
                toCam = -CameraFollow.Instance.transform.forward;
                toCam.y = 0f;
                toCam = toCam.sqrMagnitude > 1e-4f ? toCam.normalized : Vector3.back;
            }
            _glow.transform.position = transform.position + Vector3.up * bodyGlowHeight + toCam * bodyGlowTowardCamera;
        }

        // LateUpdate여야 한다 — 플레이어의 회전/조준이 같은 프레임에 갱신되므로
        // Update에서 맞추면 한 프레임 뒤처져 빠르게 돌 때 빛이 끌려다닌다.
        if (_player != null)
        {
            Vector2 f = _player.FacingDirection;
            if (f.sqrMagnitude >= 0.0001f)
            {
                // 평면 방향(x=월드X, y=월드Z) → 월드 전방. 그 상태에서 아래로 숙인다.
                Vector3 fwd = Plan3D.ToWorld(f.normalized);
                transform.rotation = Quaternion.LookRotation(fwd, Vector3.up)
                                   * Quaternion.Euler(TiltDown, 0f, 0f);
            }
        }

        // 회전 완료 뒤 월드 높이를 맞춘다. 빔의 기울기/방향이 바뀌어도 가방 높이로 돌아가지 않는다.
        // 룩 체크처럼 플레이어가 없는 경우에도 발밑 퍼짐광 위치를 유지한다.
        if (_spill != null)
            _spill.transform.position = transform.position + Vector3.up * spillHeightOffset;
    }

    /// <summary>착용한 랜턴 등급을 반영한다(반경·밝기 배율). 미착용은 (1, 1).
    /// 장비창에 랜턴 슬롯이 생기면 여기로 넣으면 된다 — 등급이 곧 시야 성장 축이다.</summary>
    public void SetGrade(float rangeMul, float intensityMul)
    {
        _gradeRange = Mathf.Max(0.1f, rangeMul);
        _gradeIntensity = Mathf.Max(0f, intensityMul);
        Apply(_dayNight != null && _dayNight.IsNight);
    }
}
