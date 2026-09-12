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

    Material  _mat;          // target의 인스턴스 머티리얼(스프라이트 경로)
    GreyboxLimbs _limbs;     // 3D 상자 몸 경로
    Coroutine _routine;

    // 3D 모델 경로(2026-09-12 — combat.md): 재질 _BaseColor를 잠깐 흰색 쪽으로. 플래시 전 블록을 떠 두었다가
    // 그대로 되돌리므로 다른 시스템이 건 틴트가 지워지지 않는다.
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    bool _use3D;
    Renderer[] _meshes;
    MaterialPropertyBlock[] _saved;
    Color[] _origColor;
    MaterialPropertyBlock _work;

    void Awake()
    {
        // 3D 그레이박스 몸이 있으면 그쪽이 우선이다.
        // ⚠️ 적 몸이 상자로 바뀌면서 FindBodySprite()가 잡던 스프라이트가 사라졌다
        //    (앵커 EnemySprite는 꺼져 있다). 이 갈래가 없으면 **적을 때려도 몸에
        //    아무 반응이 없고** 화면 효과만 남아 "때린 것 같지가 않은" 상태가 된다.
        _limbs = GetComponentInChildren<GreyboxLimbs>(true);
        if (_limbs != null) return;

        // 켜진 바디 스프라이트 + 플래시 셰이더가 있어야 스프라이트 경로. 아니면 3D 모델 경로 —
        // 모델은 이 컴포넌트보다 늦게 붙을 수 있어 렌더러는 첫 Flash 때 모은다.
        if (target == null) target = FindBodySprite();
        var cur = target != null ? target.sharedMaterial : null;
        bool spriteOk = target != null && target.gameObject.activeInHierarchy
                        && ((cur != null && cur.HasProperty(FlashAmountID)) || Shader.Find("BRB/SpriteFlash") != null);
        if (!spriteOk) { _use3D = true; return; }

        // 이미 _FlashAmount를 가진 머티리얼이면 그대로, 아니면 SpriteFlash 인스턴스로 교체
        if (cur != null && cur.HasProperty(FlashAmountID))
        {
            _mat = target.material; // 인스턴스화
        }
        else
        {
            _mat = new Material(Shader.Find("BRB/SpriteFlash"));
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
        if (!enabled) return;
        if (_use3D && !CollectMeshes()) return;
        if (_limbs == null && _mat == null && !_use3D) return;
        if (_routine != null)
        {
            StopCoroutine(_routine);
            if (_use3D) Restore3D();   // 흰 상태를 '원래 색'으로 떠 두지 않게 먼저 되돌린다
        }
        _routine = StartCoroutine(FlashRoutine(Mathf.Clamp01(intensity), Mathf.Max(0.01f, duration)));
    }

    IEnumerator FlashRoutine(float intensity, float duration)
    {
        // 3D 몸: 원래 색을 잡아 두고 흰색 쪽으로 몰았다 되돌린다.
        // ⚠️ 시작할 때 한 번 읽는다. 매 프레임 읽으면 자기가 칠한 흰색을 "원래 색"으로
        //    착각해 플래시가 끝나도 몸이 하얗게 남는다.
        Color baseColor = _limbs != null ? _limbs.BodyColor : default;
        if (_use3D) Snapshot3D();

        Apply(intensity, baseColor);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;       // 히트스탑(timeScale=0) 중에도 보이게
            float pct = 1f - (t / duration);   // 1 → 0 선형 페이드
            Apply(intensity * pct, baseColor);
            yield return null;
        }
        if (_use3D) Restore3D();
        else Apply(0f, baseColor);
        _routine = null;
    }

    void Apply(float amount, Color baseColor)
    {
        if (_limbs != null) _limbs.SetTint(Color.Lerp(baseColor, flashColor, amount));
        else if (_mat != null) _mat.SetFloat(FlashAmountID, amount);
        else if (_use3D && _meshes != null)
        {
            _work ??= new MaterialPropertyBlock();
            for (int i = 0; i < _meshes.Length; i++)
            {
                var r = _meshes[i];
                if (r == null) continue;
                r.GetPropertyBlock(_work);
                _work.SetColor(BaseColorID, Color.Lerp(_origColor[i], flashColor, amount));
                r.SetPropertyBlock(_work);
            }
        }
    }

    /// <summary>3D 모델 렌더러(_BaseColor 있는 메시)를 모은다. 하나도 없으면 false.</summary>
    bool CollectMeshes()
    {
        if (_meshes != null && _meshes.Length > 0) return true;
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if ((r is MeshRenderer || r is SkinnedMeshRenderer)
                && r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorID))
                list.Add(r);
        _meshes = list.ToArray();
        _saved = new MaterialPropertyBlock[_meshes.Length];
        _origColor = new Color[_meshes.Length];
        return _meshes.Length > 0;
    }

    void Snapshot3D()
    {
        for (int i = 0; i < _meshes.Length; i++)
        {
            var r = _meshes[i];
            if (r == null) continue;
            _saved[i] ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(_saved[i]);
            _origColor[i] = _saved[i].HasColor(BaseColorID) ? _saved[i].GetColor(BaseColorID)
                                                             : r.sharedMaterial.GetColor(BaseColorID);
        }
    }

    void Restore3D()
    {
        if (_meshes == null) return;
        for (int i = 0; i < _meshes.Length; i++)
            if (_meshes[i] != null && _saved[i] != null) _meshes[i].SetPropertyBlock(_saved[i]);
    }

    void OnDisable()
    {
        if (_use3D && _routine != null) { StopCoroutine(_routine); _routine = null; Restore3D(); }
    }

    void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}
