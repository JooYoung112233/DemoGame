using UnityEngine;
using System.Collections;

/// <summary>
/// 히트스탑 — 적중 순간 짧은 전역 정지(강공 타격감).
/// 안전 재도입: **단일 가드 코루틴 + 항상 원복**(중복 호출 시 이전 것 취소 후 재시작),
/// 대기는 unscaledDeltaTime. 예전 버그(전역 timeScale 누수)를 "복원 보장 + 중복 방지"로 해결.
/// 데이터 주도: AttackData.hitstop이 켜진 공격(강공)이 적중할 때만 AttackPerformer가 호출.
/// </summary>
public class Hitstop : MonoBehaviour
{
    static Hitstop _inst;
    static Hitstop Inst
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("~Hitstop");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<Hitstop>();
            }
            return _inst;
        }
    }

    Coroutine _co;
    float _restoreScale = 1f;

    /// <summary>duration초 동안 정지. 강공 적중 시 호출.</summary>
    public static void Do(float duration)
    {
        if (duration <= 0f) return;
        Inst.Run(duration);
    }

    void Run(float duration)
    {
        bool ours = _co != null;
        // 외부 일시정지(예: 안전가옥 timeScale=0) 중엔 무시. 단, 우리 정지의 재시작은 허용.
        if (!ours && Time.timeScale < 0.01f) return;

        if (ours) { StopCoroutine(_co); Time.timeScale = _restoreScale; }
        _restoreScale = (Time.timeScale > 0.01f) ? Time.timeScale : 1f; // 0(이미 정지)을 캡처하지 않음
        Time.timeScale = 0f;
        _co = StartCoroutine(Routine(duration));
    }

    IEnumerator Routine(float duration)
    {
        float t = 0f;
        while (t < duration) { t += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = _restoreScale;
        _co = null;
    }

    void OnDestroy()
    {
        // 정지 상태로 남지 않도록 복원
        if (_co != null && Time.timeScale < 0.01f) Time.timeScale = _restoreScale;
        if (_inst == this) _inst = null;
    }
}
