using UnityEngine;

/// <summary>
/// 룩 체크 씬 전용 — 숫자키로 카메라를 갈아탄다(1 정면 / 2 측면 / 3 쿼터뷰=게임 설정).
///
/// 정면에서 맞춘 룩이 인게임(오소 55° 부감, 작게)에서 전혀 다르게 보이는 일이 잦아,
/// **같은 씬에서 세 각도를 즉시 비교**하려고 둔다. 이 컴포넌트는 룩 체크 씬에만 있다.
/// </summary>
public class LookDevCameraSwitcher : MonoBehaviour
{
    Camera[] _cams;
    int _idx;

    void Start()
    {
        // 다른 씬이 얹혀 있으면 내린다 — 에디터가 직전 세션의 additive 씬(마을 등)을 그대로
        // 되살려서, 룩 체크 씬에 **마을 가로등 16개**가 섞여 들어온 적이 있다.
        // 그러면 여기서 보는 밝기가 인게임과도 다르고 재현도 안 된다.
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        for (int i = UnityEngine.SceneManagement.SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s == active || !s.isLoaded) continue;
            Debug.Log($"[룩 체크] 외부 씬 '{s.name}' 언로드 — 조명이 섞이면 판단이 안 된다");
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(s);
        }

        // 외곽선용 평균 법선 — 하드 엣지 모델이라 이게 없으면 테두리가 조각난다.
        // 인게임과 같이 **런타임에** 굽는다(에디터에서 구우면 씬 저장 때 메시 참조가 깨진다).
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            OutlineNormals.Apply(root);

        _cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Array.Sort(_cams, (a, b) => string.CompareOrdinal(a.name, b.name));
        FrameLampLane();
        Select(0);

        SetNight(false);   // 기본은 낮 — N키로 전환
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || _cams == null) return;
        if (kb.digit1Key.wasPressedThisFrame) Select(0);
        if (kb.digit2Key.wasPressedThisFrame) Select(1);
        if (kb.digit3Key.wasPressedThisFrame) Select(2);
        if (kb.digit4Key.wasPressedThisFrame) Select(3);
        if (kb.nKey.wasPressedThisFrame) SetNight(!_night);
    }

    // ── 낮/밤 ──────────────────────────────────────────────────────
    //  룩은 낮과 밤에서 완전히 다르게 읽힌다(밤엔 랜턴이 주광원이 된다) — 여기서 바로 바꿔 본다.
    //  값은 **게임과 같은 출처**(Resources/Data/WeatherData)에서 읽는다. 여기만 따로 예쁘게
    //  맞춰 두면 룩 체크가 인게임을 대변하지 못한다.
    bool _night;
    Light _sun;
    WeatherData _weather;

    public void SetNight(bool night)
    {
        _night = night;
        if (_sun == null)
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { _sun = l; break; }
        if (_weather == null) _weather = Resources.Load<WeatherData>("Data/WeatherData");

        if (_sun != null)
        {
            if (_weather != null)
            {
                _sun.intensity = night ? _weather.nightIntensity : _weather.dayIntensity;
                _sun.color     = night ? _weather.nightLightColor : _weather.dayLightColor;
                _sun.transform.rotation = Quaternion.Euler(night ? _weather.nightSunAngle : _weather.daySunAngle);
            }
            else   // WeatherData가 없으면 문서상 값으로 폴백
            {
                _sun.intensity = night ? 0.18f : 1.35f;
                _sun.color = night ? new Color(.55f, .62f, .85f) : new Color(1f, .96f, .89f);
                _sun.transform.rotation = Quaternion.Euler(night ? new Vector3(60, 200, 0) : new Vector3(38, -40, 0));
            }
        }

        Color amb = night ? new Color(.07f, .08f, .11f) : new Color(.52f, .56f, .64f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientLight        = amb;
        RenderSettings.ambientSkyColor     = amb;
        RenderSettings.ambientEquatorColor = amb * 0.8f;
        RenderSettings.ambientGroundColor  = amb * 0.45f;

        foreach (var lamp in FindObjectsByType<WornLamp>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            lamp.ForcePhase(night);
    }

    /// <summary>랜턴 레인 카메라를 **등을 든 사람 기준으로** 자동 배치한다.
    /// 좌표를 손으로 박아 두면 레인 위치를 조금만 옮겨도 화면 밖으로 나간다(실제로 세 번 어긋났다).
    /// 옆 6m·높이 2.2m에서 빔이 뻗는 방향(+Z)의 4m 앞을 본다 = 콘 모양이 옆으로 보인다.</summary>
    void FrameLampLane()
    {
        var lamp = FindAnyObjectByType<WornLamp>();
        if (lamp == null || _cams == null) return;
        Camera lane = null;
        foreach (var c in _cams) if (c.name.Contains("랜턴")) { lane = c; break; }
        if (lane == null) return;

        Vector3 origin = lamp.transform.position;                 // 가슴 높이
        Vector3 beam = lamp.transform.forward; beam.y = 0f;
        if (beam.sqrMagnitude < 1e-4f) beam = Vector3.forward;
        beam.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, beam).normalized; // 빔의 옆

        Vector3 look = origin + beam * 4f - Vector3.up * 0.5f;     // 빔이 바닥에 닿는 언저리
        lane.transform.position = origin + side * 6f + Vector3.up * 1.6f - beam * 1.5f;
        lane.transform.rotation = Quaternion.LookRotation(look - lane.transform.position, Vector3.up);
    }

    /// <summary>i번 카메라만 켠다. 밖(QA·에디터 스크립트)에서도 부를 수 있게 public.</summary>
    public void Select(int i)
    {
        if (_cams == null || _cams.Length == 0) return;
        _idx = Mathf.Clamp(i, 0, _cams.Length - 1);
        for (int k = 0; k < _cams.Length; k++) _cams[k].enabled = (k == _idx);
    }

    public string Current => _cams != null && _cams.Length > 0 ? _cams[_idx].name : "-";

    void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(new Rect(12, 10, 900, 24),
            $"[룩 체크] 카메라: {Current}   ·   {(_night ? "밤" : "낮")}   ·   1 정면 / 2 측면 / 3 쿼터뷰(게임) / 4 랜턴레인 / N 낮밤");
    }
}
