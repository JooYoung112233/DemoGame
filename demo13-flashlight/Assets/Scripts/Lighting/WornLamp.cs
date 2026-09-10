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
    [Tooltip("빛이 퍼지는 각도. 좁을수록 '가슴에 단 등'으로 읽힌다. 360°에 가까우면 머리 위 전등처럼 보인다.")]
    [Range(20f, 170f)][SerializeField] float coneAngle = 56f;
    [Tooltip("콘 안쪽(풀 밝기) 각도 비율. 가장자리를 부드럽게 풀어 준다.")]
    [Range(0.1f, 0.95f)][SerializeField] float innerRatio = 0.74f;

    Light _light;
    DayNightCycle _dayNight;
    TopDownPlayer _player;
    float _gradeRange = 1f, _gradeIntensity = 1f;   // 등급 배율(미착용 = 1)

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

    /// <summary>착용자 주변 미광 — 등이 몸에 달려 있으면 **자기 몸도 조금은 밝다.**
    /// 빔만 있으면 캐릭터가 새까만 실루엣이 되어 "어디선가 쏘는 조명"으로 읽힌다.
    /// 아주 약하고 짧게, 그림자 없이(비용 0). 빔의 방향감을 해치지 않을 만큼만.</summary>
    Light _spill;

    void EnsureSpill()
    {
        if (_spill != null) return;
        var go = new GameObject("LampSpill");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        _spill = go.AddComponent<Light>();
        _spill.type = LightType.Point;
        _spill.shadows = LightShadows.None;
        _spill.range = 2.6f;
        _spill.color = _light != null ? _light.color : Color.white;
        _spill.renderMode = LightRenderMode.ForcePixel;
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
        _light.intensity = (isNight ? nightIntensity : dayIntensity) * _gradeIntensity;
        _light.spotAngle = coneAngle;
        _light.innerSpotAngle = coneAngle * innerRatio;
        EnsureSpill();
        if (_spill != null)   // 빔의 1/6 — 있는지 모를 정도로만, 몸이 검은 종이가 되지 않게
            _spill.intensity = (isNight ? nightIntensity : dayIntensity) * _gradeIntensity * 0.16f;
    }

    /// <summary>등이 바라보는 쪽을 향하게 한다 — **몸에 달린 등이므로 몸을 따라 돈다.**
    /// 이게 없으면 방향이 고정돼 옆으로 걸을 때 엉뚱한 데를 비추고, 결국 "떠 있는 조명"이 된다.</summary>
    // 아래로 숙인 각. 34°는 **발 앞 1.7m**에 원을 만들어, 캐릭터를 둘러싼 밝은 웅덩이가 됐다
    // = "머리 위에 뜬 조명". 가슴등은 앞을 봐야 한다 — 16°면 빔이 앞으로 뻗는다.
    const float TiltDown = 16f;

    void LateUpdate()
    {
        // LateUpdate여야 한다 — 플레이어의 회전/조준이 같은 프레임에 갱신되므로
        // Update에서 맞추면 한 프레임 뒤처져 빠르게 돌 때 빛이 끌려다닌다.
        if (_player == null) _player = TopDownPlayer.Instance;
        if (_player == null) return;

        Vector2 f = _player.FacingDirection;
        if (f.sqrMagnitude < 0.0001f) return;

        // 평면 방향(x=월드X, y=월드Z) → 월드 전방. 그 상태에서 아래로 숙인다.
        Vector3 fwd = Plan3D.ToWorld(f.normalized);
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up)
                           * Quaternion.Euler(TiltDown, 0f, 0f);
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
