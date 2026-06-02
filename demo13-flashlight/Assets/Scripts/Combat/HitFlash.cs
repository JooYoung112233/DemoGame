using UnityEngine;
using System.Collections;

/// <summary>
/// 피격 흰 플래시 — 셰이더 `_FlashAmount` 기반.
/// `BRB/SpriteFlash` 머티리얼을 바디 스프라이트에 자가 설치하고,
/// 적중 순간 흰색으로 번쩍였다가 사라진다. (SpriteRenderer.color 틴트/플립과 독립)
/// CombatFeedback에서 호출. 플레이어/적 공용.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Tooltip("플래시 대상 바디 스프라이트 (비우면 자식에서 자동 탐색, 'Bar' 제외)")]
    [SerializeField] SpriteRenderer target;
    [SerializeField] Color flashColor = Color.white;

    static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    static readonly int FlashColorID  = Shader.PropertyToID("_FlashColor");

    Material  _mat;          // target의 인스턴스 머티리얼
    Coroutine _routine;

    void Awake()
    {
        if (target == null) target = FindBodySprite();
        if (target == null) { enabled = false; return; }

        // 이미 _FlashAmount를 가진 머티리얼이면 그대로, 아니면 SpriteFlash 인스턴스로 교체
        var cur = target.sharedMaterial;
        if (cur != null && cur.HasProperty(FlashAmountID))
        {
            _mat = target.material; // 인스턴스화
        }
        else
        {
            var shader = Shader.Find("BRB/SpriteFlash");
            if (shader == null) { enabled = false; return; }
            _mat = new Material(shader);
            target.material = _mat;
        }

        _mat.SetColor(FlashColorID, flashColor);
        _mat.SetFloat(FlashAmountID, 0f);
    }

    /// <summary>첫 번째 바디 스프라이트(이름에 "Bar" 미포함)를 찾는다.</summary>
    SpriteRenderer FindBodySprite()
    {
        var all = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in all)
            if (!sr.name.Contains("Bar")) return sr;
        return all.Length > 0 ? all[0] : null;
    }

    /// <summary>흰 플래시 1회. intensity 0~1, duration 초(unscaled).</summary>
    public void Flash(float intensity = 1f, float duration = 0.08f)
    {
        if (!enabled || _mat == null) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FlashRoutine(Mathf.Clamp01(intensity), Mathf.Max(0.01f, duration)));
    }

    IEnumerator FlashRoutine(float intensity, float duration)
    {
        _mat.SetFloat(FlashAmountID, intensity);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;       // 히트스탑(timeScale=0) 중에도 보이게
            float pct = 1f - (t / duration);   // 1 → 0 선형 페이드
            _mat.SetFloat(FlashAmountID, intensity * pct);
            yield return null;
        }
        _mat.SetFloat(FlashAmountID, 0f);
        _routine = null;
    }

    void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}
