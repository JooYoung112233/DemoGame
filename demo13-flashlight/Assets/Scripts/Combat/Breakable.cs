using UnityEngine;
using System.Collections;

/// <summary>
/// 파괴 가능한 오브젝트(상자·항아리·판자벽 등). 때리면 단계별로 부서지고 마지막 단계에 파괴된다.
///
/// "모든 셰이더와 같이 쓸 수 있는" 구조: 손상 비주얼은 베이스 스프라이트 위에 덧씌우는
/// <b>자식 오버레이</b>(BRB/DamageOverlay)로 그린다 → 베이스 셰이더(Wall/Prop/Floor/스프라이트-Lit 등)를
/// 전혀 건드리지 않으므로 어떤 셰이더든 호환. (필요시 단계별 스프라이트 교체·베이스 어둡게도 옵션)
///
/// 데미지 입력:
///   • Health 컴포넌트가 있고 syncWithHealth면 전투 데미지에 <b>자동 연동</b>(OnDamaged→단계, OnDeath→파괴).
///   • 없으면 자체 내구도(maxHp). 공격 측에서 <see cref="Hit"/> 또는 <see cref="ApplyDamage"/> 호출.
///
/// 에디터에서 previewStage로 단계 미리보기 가능([ExecuteAlways], 오버레이만 — 스프라이트 교체는 런타임만).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class Breakable : MonoBehaviour
{
    [Header("Stages (단계)")]
    [Tooltip("손상 단계 수. 마지막 단계 도달 = 파괴. 예: 3이면 1→2→3(파괴).")]
    [Min(1)] public int stageCount = 3;
    [Tooltip("총 내구도(데미지 합). Hit() 1회 = maxHp/stageCount(한 단계분). " +
             "전투 연동 시 부서지는 타수 ≈ maxHp / 공격 데미지(약공 ~8). 예: 24면 약 3대.")]
    [Min(0.01f)] public float maxHp = 24f;
    [Tooltip("Health 컴포넌트가 있으면 그걸로 단계 구동(전투 데미지 자동 연동). 없으면 자체 내구도.")]
    public bool syncWithHealth = true;

    [Header("Visual · Damage Overlay (모든 셰이더 호환)")]
    [Tooltip("자식 오버레이로 균열/그을음을 덧씌움(BRB/DamageOverlay). 베이스 셰이더 무관.")]
    public bool useOverlay = true;
    [Range(0f, 2f)] public float overlayIntensity = 1f;
    public Color crackColor = new(0.02f, 0.02f, 0.03f, 1f);
    public Color grimeColor = new(0.15f, 0.13f, 0.10f, 1f);

    [Header("Visual · Optional Sprite Swap")]
    [Tooltip("단계별 베이스 스프라이트 교체(있으면). [0]=1단계 … 비우면 오버레이만 사용. (런타임만 적용)")]
    public Sprite[] stageSprites;

    [Header("Visual · Base Darken")]
    [Tooltip("단계가 오를수록 베이스를 어둡게(_Brightness, BRB 셰이더 공통). 베이스에 _Brightness 없으면 무시.")]
    public bool darkenBase = true;
    [Range(0f, 1f)] public float darkenAmount = 0.45f;

    [Header("Hit Feedback")]
    public bool shakeOnHit = true;
    [Range(0f, 0.4f)] public float shakeAmount = 0.07f;
    [Tooltip("HitFlash 컴포넌트가 있으면 피격 시 흰 플래시.")]
    public bool flashOnHit = true;

    [Header("Break (파괴)")]
    [Tooltip("true=파괴 시 오브젝트 제거. false=잔해로 남김(콜라이더 끔, rubbleSprite로 교체).")]
    public bool destroyOnBreak = true;
    [Tooltip("destroyOnBreak=false일 때 남길 잔해 스프라이트(선택).")]
    public Sprite rubbleSprite;
    [Tooltip("파괴 시 튀는 파편 개수(절차적). 0=없음.")]
    [Min(0)] public int debrisCount = 6;
    public float debrisLifetime = 0.8f;
    [Tooltip("파괴 VFX 프리팹(선택). 있으면 절차적 파편 대신 이걸 스폰.")]
    public GameObject breakVfxPrefab;

    [Header("Loot on Break (파괴 시 드랍)")]
    [Tooltip("파괴 시 바닥에 드랍할 고정 아이템(있으면).")]
    public ItemData dropItem;
    [Min(1)] public int dropCount = 1;
    [Tooltip("켜면 지역 루트 테이블(Ground)로도 드랍 — 임시 ItemSpawnPoint를 스폰해 기존 파이프라인 사용.")]
    public bool dropRegionLoot = false;

    [Header("Editor Preview")]
    [Tooltip("에디터에서 손상 단계 미리보기(오버레이만). 0=온전.")]
    [Range(0, 8)] public int previewStage = 0;

    /// <summary>단계가 바뀔 때(int=새 단계).</summary>
    public event System.Action<int> OnStageChanged;
    /// <summary>파괴되는 순간.</summary>
    public event System.Action OnBroken;

    public int CurrentStage => _stage;
    public bool IsBroken => _broken;

    float _hp;
    int _stage;
    bool _broken;
    SpriteRenderer _baseSR;
    Health _health;
    HitFlash _flash;
    MaterialPropertyBlock _mpb;
    GameObject _overlayGO;
    SpriteRenderer _overlaySR;
    Material _overlayMat;
    float _baseBrightness = 1f;
    Vector3 _basePos;
    Coroutine _shake;

    static readonly int IdDamage = Shader.PropertyToID("_Damage");
    static readonly int IdIntensity = Shader.PropertyToID("_Intensity");
    static readonly int IdCrackColor = Shader.PropertyToID("_CrackColor");
    static readonly int IdGrimeColor = Shader.PropertyToID("_GrimeColor");
    static readonly int IdBrightness = Shader.PropertyToID("_Brightness");

    void OnEnable()
    {
        _baseSR = GetComponent<SpriteRenderer>();
        if (!gameObject.scene.IsValid()) return;
        _mpb ??= new MaterialPropertyBlock();
        CacheBaseBrightness();

        if (Application.isPlaying)
        {
            _hp = maxHp;
            _broken = false;
            _stage = 0;
            _basePos = transform.localPosition;
            _flash = GetComponent<HitFlash>();
            _health = GetComponent<Health>();
            if (syncWithHealth && _health != null)
            {
                _health.OnDamaged += OnHealthDamaged;
                _health.OnDeath += HandleHealthDeath;
            }
        }

        if (useOverlay) BuildOverlay();
        ApplyPreviewOrRefresh();
    }

    void OnDisable()
    {
        if (Application.isPlaying && syncWithHealth && _health != null)
        {
            _health.OnDamaged -= OnHealthDamaged;
            _health.OnDeath -= HandleHealthDeath;
        }
        CleanupOverlay();
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled || !gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled || Application.isPlaying) return;
            CleanupOverlay();
            CacheBaseBrightness();
            if (useOverlay) BuildOverlay();
            ApplyPreviewOrRefresh();
        };
#endif
    }

    void OnDestroy() => CleanupOverlay();

    // ───────────────── 데미지 입력 API ─────────────────

    /// <summary>한 대 때림(count회). 기본 1회 = 한 단계 분량의 데미지.</summary>
    public void Hit(int count = 1)
    {
        if (_broken || count <= 0) return;
        ApplyDamage(count * (maxHp / Mathf.Max(1, stageCount)));
    }

    /// <summary>임의 데미지 적용. Health 연동 시엔 Health로 위임(이벤트로 단계 갱신).</summary>
    public void ApplyDamage(float amount)
    {
        if (_broken || amount <= 0f) return;
        if (syncWithHealth && _health != null)
        {
            _health.TakeDamage(amount);   // → OnHealthDamaged / OnDeath 로 반영
            return;
        }
        _hp = Mathf.Max(0f, _hp - amount);
        float dmg01 = 1f - _hp / maxHp;
        SetStage(StageFromDamage01(dmg01));
        Feedback();
        if (_hp <= 0f) DoBreak();
    }

    void OnHealthDamaged(float _)
    {
        if (_broken) return;
        SetStage(StageFromDamage01(1f - _health.Percent));
        Feedback();
    }

    void HandleHealthDeath() => DoBreak();

    int StageFromDamage01(float d01) =>
        Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(d01) * stageCount), 0, stageCount);

    // ───────────────── 단계/비주얼 ─────────────────

    void SetStage(int s)
    {
        if (s == _stage) return;
        _stage = s;
        RefreshVisual();
        OnStageChanged?.Invoke(_stage);
    }

    void ApplyPreviewOrRefresh()
    {
        if (!Application.isPlaying)
            _stage = Mathf.Clamp(previewStage, 0, stageCount);
        RefreshVisual();
    }

    void RefreshVisual()
    {
        float d = stageCount > 0 ? (float)_stage / stageCount : 0f;

        if (_overlayMat != null)
            _overlayMat.SetFloat(IdDamage, d);

        // 스프라이트 교체는 베이스 에셋을 바꾸므로 런타임만(에디터 직렬화 오염 방지).
        if (Application.isPlaying && stageSprites != null && stageSprites.Length > 0 &&
            _baseSR != null && _stage >= 1)
        {
            int idx = Mathf.Clamp(_stage - 1, 0, stageSprites.Length - 1);
            if (stageSprites[idx] != null) _baseSR.sprite = stageSprites[idx];
        }

        // 베이스 어둡게(_Brightness, MPB — 비파괴). BRB 셰이더 공통 프로퍼티.
        if (darkenBase && _baseSR != null && _baseSR.sharedMaterial != null &&
            _baseSR.sharedMaterial.HasProperty(IdBrightness))
        {
            _mpb ??= new MaterialPropertyBlock();
            _baseSR.GetPropertyBlock(_mpb);
            _mpb.SetFloat(IdBrightness, _baseBrightness * (1f - darkenAmount * d));
            _baseSR.SetPropertyBlock(_mpb);
        }
    }

    void Feedback()
    {
        if (flashOnHit && _flash != null) _flash.Flash(0.8f, 0.07f);
        if (shakeOnHit && shakeAmount > 0f && Application.isPlaying && isActiveAndEnabled)
        {
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(ShakeRoutine());
        }
    }

    IEnumerator ShakeRoutine()
    {
        float t = 0f, dur = 0.12f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = (1f - t / dur) * shakeAmount;
            transform.localPosition = _basePos + (Vector3)(Random.insideUnitCircle * k);
            yield return null;
        }
        transform.localPosition = _basePos;
        _shake = null;
    }

    // ───────────────── 파괴 ─────────────────

    void DoBreak()
    {
        if (_broken) return;
        _broken = true;
        SetStage(stageCount);

        if (_shake != null) { StopCoroutine(_shake); _shake = null; }
        if (Application.isPlaying) transform.localPosition = _basePos;

        SpawnDebris();
        SpawnLoot();
        OnBroken?.Invoke();

        if (destroyOnBreak)
        {
            Destroy(gameObject);
        }
        else
        {
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
            if (rubbleSprite != null && _baseSR != null) _baseSR.sprite = rubbleSprite;
            // 오버레이는 최대 손상치(_Damage=1)로 남아 폐허 느낌 유지.
        }
    }

    void SpawnDebris()
    {
        if (!Application.isPlaying) return;
        if (breakVfxPrefab != null)
        {
            Instantiate(breakVfxPrefab, transform.position, Quaternion.identity);
            return;
        }
        if (debrisCount <= 0 || _baseSR == null || _baseSR.sprite == null) return;

        for (int i = 0; i < debrisCount; i++)
        {
            var g = new GameObject("Debris");
            g.transform.position = transform.position;
            g.transform.rotation = Quaternion.Euler(0, 0, Random.value * 360f);
            g.transform.localScale = Vector3.one * Random.Range(0.15f, 0.30f);

            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = _baseSR.sprite;
            sr.sortingLayerID = _baseSR.sortingLayerID;
            sr.sortingOrder = _baseSR.sortingOrder + 1;
            sr.color = grimeColor;

            g.AddComponent<BreakDebris>()
             .Init(Random.insideUnitCircle.normalized * Random.Range(1.5f, 3.5f), debrisLifetime);
        }
    }

    void SpawnLoot()
    {
        if (!Application.isPlaying) return;

        // 고정 아이템 드랍(바닥)
        if (dropItem != null)
        {
            var pos = transform.position + (Vector3)(Random.insideUnitCircle * 0.2f);
            WorldItem.Drop(new ItemInstance(dropItem, Mathf.Max(1, dropCount)), pos, this);
        }

        // 지역 루트 드랍 — 임시 ItemSpawnPoint(Ground)를 스폰해 기존 루트 파이프라인 재사용.
        // (Breakable은 곧 파괴될 수 있으므로 별도 루트 오브젝트로 분리)
        if (dropRegionLoot)
        {
            var g = new GameObject("BreakLoot");
            g.transform.position = transform.position;
            var sp = g.AddComponent<ItemSpawnPoint>();
            SetPrivate(sp, "spawnType", ItemSpawnPoint.SpawnType.Ground);
            SetPrivate(sp, "useRegionLoot", true);
            // sp.Start()가 다음 프레임에 RollLoot→WorldItem.Drop 수행
        }
    }

    static void SetPrivate(object target, string field, object value)
    {
        var f = target.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(target, value);
    }

    // ───────────────── 오버레이 자식 관리 (GroundShadow2D 패턴) ─────────────────

    void CacheBaseBrightness()
    {
        if (_baseSR != null && _baseSR.sharedMaterial != null && _baseSR.sharedMaterial.HasProperty(IdBrightness))
            _baseBrightness = _baseSR.sharedMaterial.GetFloat(IdBrightness);
        else
            _baseBrightness = 1f;
    }

    /// <summary>손상 오버레이 자식을 강제로 다시 만든다(맵 저장 후 DontSave 자식이 떨어졌을 때 등).</summary>
    public void RebuildOverlay()
    {
        if (!gameObject.scene.IsValid()) return;
        if (_baseSR == null) _baseSR = GetComponent<SpriteRenderer>();
        if (useOverlay) BuildOverlay();
        RefreshVisual();
    }

    void BuildOverlay()
    {
        if (_baseSR == null || _baseSR.sprite == null) return;
        CleanupOverlay();

        // 재컴파일/중복 대비: 기존 고아 자식 제거
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null && c.name == "DamageOverlay") DestroySafe(c.gameObject);
        }

        var shader = Shader.Find("BRB/DamageOverlay");
        if (shader == null) { Debug.LogWarning("[Breakable] BRB/DamageOverlay 못 찾음."); return; }

        _overlayMat = new Material(shader);
        _overlayMat.SetFloat(IdIntensity, overlayIntensity);
        _overlayMat.SetColor(IdCrackColor, crackColor);
        _overlayMat.SetColor(IdGrimeColor, grimeColor);
        _overlayMat.SetFloat(IdDamage, 0f);

        _overlayGO = new GameObject("DamageOverlay");
        _overlayGO.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        _overlayGO.transform.SetParent(transform, false);
        _overlayGO.transform.localPosition = Vector3.zero;

        _overlaySR = _overlayGO.AddComponent<SpriteRenderer>();
        _overlaySR.sprite = _baseSR.sprite;          // 베이스 실루엣/알파를 그대로 마스크로
        _overlaySR.flipX = _baseSR.flipX;
        _overlaySR.flipY = _baseSR.flipY;
        _overlaySR.drawMode = _baseSR.drawMode;
        if (_baseSR.drawMode != SpriteDrawMode.Simple)
        {
            _overlaySR.tileMode = _baseSR.tileMode;
            _overlaySR.size = _baseSR.size;
        }
        _overlaySR.sortingLayerID = _baseSR.sortingLayerID;
        _overlaySR.sortingOrder = _baseSR.sortingOrder + 1;   // 본체 바로 위
        _overlaySR.sharedMaterial = _overlayMat;
    }

    void CleanupOverlay()
    {
        if (_overlayGO != null) DestroySafe(_overlayGO);
        if (_overlayMat != null) DestroySafe(_overlayMat);
        _overlayGO = null; _overlaySR = null; _overlayMat = null;
    }

    static void DestroySafe(Object o)
    {
        if (o == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { DestroyImmediate(o); return; }
#endif
        Destroy(o);
    }
}

/// <summary>파괴 시 튀는 절차적 파편 1개 — 날아가며 감속·축소·페이드 후 자멸.</summary>
public class BreakDebris : MonoBehaviour
{
    Vector2 _vel;
    float _life, _t;
    SpriteRenderer _sr;
    Color _c0;

    public void Init(Vector2 velocity, float life)
    {
        _vel = velocity;
        _life = Mathf.Max(0.05f, life);
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _c0 = _sr.color;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _t += dt;
        transform.position += (Vector3)(_vel * dt);
        _vel *= Mathf.Max(0f, 1f - 3f * dt);                 // 감속
        transform.localScale *= Mathf.Max(0f, 1f - 1.5f * dt); // 축소

        if (_sr != null)
        {
            var c = _c0;
            c.a = Mathf.Clamp01(1f - _t / _life);
            _sr.color = c;
        }
        if (_t >= _life) Destroy(gameObject);
    }
}
