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
    [Tooltip("빛이 닿는 반경(m).")]
    [SerializeField] float baseRange = 2.8f;
    [Tooltip("밤 밝기. 캐릭터 실루엣이 읽힐 만큼만 — 주변을 훤히 밝히면 시야 콘이 의미를 잃는다.")]
    [SerializeField] float nightIntensity = 1.6f;
    [Tooltip("낮 밝기. 0이 아니라 아주 낮은 값 — '늘 켜져 있다'가 사실이어야 한다.")]
    [SerializeField] float dayIntensity = 0.25f;

    Light _light;
    DayNightCycle _dayNight;
    float _gradeRange = 1f, _gradeIntensity = 1f;   // 등급 배율(미착용 = 1)

    void Awake()
    {
        _light = GetComponent<Light>();
        _light.type = LightType.Point;
        // ⚠️ 그림자는 끈다. 플레이어를 늘 따라다니는 광원이라 그림자를 켜면 매 프레임
        //    섀도맵을 새로 굽는다 — 얻는 것에 비해 값이 너무 비싸다.
        _light.shadows = LightShadows.None;
    }

    void Start()
    {
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

    void Apply(bool isNight)
    {
        if (_light == null) return;
        _light.range = baseRange * _gradeRange;
        _light.intensity = (isNight ? nightIntensity : dayIntensity) * _gradeIntensity;
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
