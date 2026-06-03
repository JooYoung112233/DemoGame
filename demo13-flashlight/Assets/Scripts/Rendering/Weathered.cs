using UnityEngine;

/// <summary>
/// 정적 풍화/낡음 오버레이 — 부서지지 않아도 <b>항상</b> 녹·이끼·그을음·물때로 낡아 보이게.
/// `BRB/DamageOverlay`를 자식 오버레이로 깔고 `_Damage`를 고정값(amount)으로 둔다.
/// 베이스 셰이더 무관(이미지 0장, 절차적). 색(tint)만 바꿔 녹(주황갈)/이끼(초록)/그을음(검정)/물때(갈색).
/// Breakable과 함께 쓰면 "원래 낡았는데 더 부서짐"도 됨(자식 이름이 달라 충돌 없음).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class Weathered : MonoBehaviour
{
    [Header("Aged / Weathering (낡음·녹·폐허)")]
    [Tooltip("낡음 정도(0=새것, 1=폐허). 절차적 균열·그을음을 항상 표시.")]
    [Range(0f, 1f)] public float amount = 0.5f;
    [Tooltip("오버레이 세기.")]
    [Range(0f, 2f)] public float intensity = 1f;
    [Tooltip("주된 풍화 색 — 녹=주황갈, 이끼=초록, 그을음=검정, 물때=갈색.")]
    public Color tint = new(0.32f, 0.22f, 0.12f, 1f);

    SpriteRenderer _baseSR;
    GameObject _overlayGO;
    SpriteRenderer _overlaySR;
    Material _overlayMat;

    static readonly int IdDamage = Shader.PropertyToID("_Damage");
    static readonly int IdIntensity = Shader.PropertyToID("_Intensity");
    static readonly int IdCrackColor = Shader.PropertyToID("_CrackColor");
    static readonly int IdGrimeColor = Shader.PropertyToID("_GrimeColor");

    void OnEnable()
    {
        _baseSR = GetComponent<SpriteRenderer>();
        if (!gameObject.scene.IsValid()) return;
        Build();
    }

    void OnDisable() => Cleanup();

    void OnValidate()
    {
        if (!isActiveAndEnabled || !gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled) return;
            Cleanup();
            Build();
        };
#endif
    }

    void OnDestroy() => Cleanup();

    void Build()
    {
        if (_baseSR == null || _baseSR.sprite == null) return;
        Cleanup();

        // 재컴파일/중복 대비: 기존 고아 자식 제거
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null && c.name == "WeatherOverlay") DestroySafe(c.gameObject);
        }

        var sh = Shader.Find("BRB/DamageOverlay");
        if (sh == null) { Debug.LogWarning("[Weathered] BRB/DamageOverlay 못 찾음."); return; }

        _overlayMat = new Material(sh);
        _overlayMat.SetColor(IdGrimeColor, tint);
        _overlayMat.SetColor(IdCrackColor, tint * 0.5f);   // 균열이 따로 튀지 않게 같은 계열 어둡게
        _overlayMat.SetFloat(IdIntensity, intensity);
        _overlayMat.SetFloat(IdDamage, Mathf.Clamp01(amount));

        _overlayGO = new GameObject("WeatherOverlay");
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
        _overlaySR.sortingOrder = _baseSR.sortingOrder + 1;   // 본체 바로 위(Breakable 오버레이와 같은 층, 무해)
        _overlaySR.sharedMaterial = _overlayMat;
    }

    void Cleanup()
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
