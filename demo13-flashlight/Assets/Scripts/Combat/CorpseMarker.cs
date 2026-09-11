using UnityEngine;

/// <summary>시체 루팅 표시 — 안에 든 **가장 좋은 물건의 희귀도 색** 빛기둥
/// (docs/combat.md §배그식 사망 루팅 ①, 2026-09-11 사용자 결정).
/// 다 비우면 꺼지고 머리 위 라벨이 "빈 시체"가 된다. 플레이어 시야콘 밖에선 숨긴다(멀리서 위치가 새지 않게).
/// 시체는 다른 필드 상자처럼 루팅 목록(LootListUI — 옛 수색 연출 → 전부/하나씩)으로 열린다(2026-09-11).</summary>
public sealed class CorpseMarker : MonoBehaviour
{
    LootContainer _box;
    UnitLabel _label;
    Transform _beam;
    Material _mat;
    Light _light;
    Transform _anchor;          // 래그돌 엉덩이 — 몸이 밀려나도 표시가 몸 위에 선다
    bool _empty, _emptyLabelApplied, _visible = true;
    float _nextVisCheck;

    const float BeamHeight = 2.2f;
    const float GlowIntensity = 1.2f;

    public bool IsEmpty => _empty;

    public void Init(LootContainer box, UnitLabel label)
    {
        _box = box;
        _label = label;
        Build();
        if (_box != null && _box.Grid != null) _box.Grid.OnChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (_box != null && _box.Grid != null) _box.Grid.OnChanged -= Refresh;
        if (_mat != null) Destroy(_mat);
    }

    void Build()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "LootBeam";
        Destroy(go.GetComponent<Collider>());   // 표시일 뿐 — 총알·시야에 걸리면 안 된다
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, BeamHeight * 0.5f, 0f);
        go.transform.localScale = new Vector3(0.06f, BeamHeight * 0.5f, 0.06f);   // 실린더 기본 높이 2
        var r = go.GetComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var sh = Shader.Find("Universal Render Pipeline/Unlit");   // 어두운 밤에도 색이 읽히게 조명 무관
        if (sh != null) { _mat = new Material(sh); r.sharedMaterial = _mat; }
        _beam = go.transform;

        var lgo = new GameObject("LootGlow");
        lgo.transform.SetParent(transform, false);
        lgo.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        _light = lgo.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.range = 2.2f;
        _light.intensity = GlowIntensity;
        _light.shadows = LightShadows.None;
    }

    /// <summary>내용물이 바뀔 때마다 — 가장 높은 희귀도(가방 속까지)로 색을 정한다. 비면 꺼진다.</summary>
    void Refresh()
    {
        if (_box == null || _box.Grid == null) return;
        int best = -1;
        Color c = Color.white;
        foreach (var p in _box.Grid.GetAll()) Scan(p.item, ref best, ref c);
        _empty = best < 0;
        if (_mat != null) _mat.SetColor("_BaseColor", c);
        if (_light != null) _light.color = c;
    }

    static void Scan(ItemInstance it, ref int best, ref Color c)
    {
        if (it == null || it.data == null) return;
        int r = (int)it.data.rarity;
        if (r > best) { best = r; c = it.data.RarityColor; }
        var inner = it.ContainerGrid;
        if (inner != null)
            foreach (var q in inner.GetAll()) Scan(q.item, ref best, ref c);
    }

    void LateUpdate()
    {
        // 라벨은 한 프레임 늦게 — 사망 처리(OnDeath)가 BecomeCorpse 직후 "시체"로 덮어쓴다.
        if (_empty && !_emptyLabelApplied && _label != null)
        {
            _label.Set("빈 시체", new Color(0.5f, 0.5f, 0.52f));
            _emptyLabelApplied = true;
        }

        if (_anchor == null)
        {
            var rd = GetComponentInChildren<BanditRagdoll>();
            if (rd != null) _anchor = rd.Hips;
        }
        if (_anchor != null)
        {
            var a = _anchor.position;
            var y = transform.position.y;
            _beam.position = new Vector3(a.x, y + BeamHeight * 0.5f, a.z);
            _light.transform.position = new Vector3(a.x, y + 0.7f, a.z);
        }

        if (Time.time >= _nextVisCheck)
        {
            _nextVisCheck = Time.time + 0.2f;
            _visible = PlayerVision.CanSee(_beam.position);
        }
        bool on = !_empty && _visible;
        if (_beam.gameObject.activeSelf != on) _beam.gameObject.SetActive(on);
        if (_light.enabled != on) _light.enabled = on;
        if (on) _light.intensity = GlowIntensity * (0.8f + 0.2f * Mathf.Sin(Time.time * 3f));
    }
}
