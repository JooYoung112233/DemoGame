using UnityEngine;

/// <summary>
/// <see cref="Prop3DDefinition"/> 하나를 실제 GameObject로 세운다.
/// 맵 빌더(에디터)와 런타임 스폰이 같은 경로를 쓰도록 여기 하나로 모은다 —
/// 두 곳에서 따로 조립하면 "에디터에선 막히는데 런타임엔 통과"가 생긴다.
///
/// 모델이 없으면 상자로 세운다(<see cref="Prop3DDefinition.IsGreybox"/>).
/// 맵을 먼저 짓고 모델을 나중에 끼우기 위한 것이며, 콜라이더·높이·엄폐 판정은
/// 그레이박스 상태에서도 **진짜와 똑같이** 동작한다.
///
/// 설계: docs/3d-migration.md Stage 3
/// </summary>
public static class Prop3DBuilder
{
    /// <summary>공유 그레이박스 메시(원점 중심 1m 정육면체). 프롭마다 만들면 낭비다.</summary>
    static Mesh _cubeMesh;

    static Mesh CubeMesh()
    {
        if (_cubeMesh != null) return _cubeMesh;
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);
        return _cubeMesh;
    }

    /// <summary>정의대로 프롭을 만든다. 부모·위치는 호출자가 정한다.
    ///
    /// 구조: 루트(콜라이더·파괴) + 자식 "Visual"(메시). 바닥 정렬을 루트 위치로 처리하면
    /// 호출자가 위치를 지정하는 순간 덮어써지므로, 띄우는 것은 **자식 안에서** 한다.</summary>
    public static GameObject Build(Prop3DDefinition def)
    {
        if (def == null) return null;

        var go = new GameObject(string.IsNullOrEmpty(def.displayName) ? def.propId : def.displayName);

        // 프롭 원점은 **바닥**이다(맵 빌더가 지면 좌표로 놓는다). 상자 메시는 중심 원점이라
        // 절반만큼 올려야 바닥에 놓인다. 이걸 빠뜨리면 프롭이 지면에 반쯤 묻힌다.
        float lift = def.groundOffset + (def.IsGreybox ? def.size.y * 0.5f : 0f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, lift, 0f);
        // 그레이박스는 1m 정육면체이므로 size가 곧 스케일이다.
        // 메시가 있으면 제 크기를 믿는다 — 스케일로 억지로 늘리면 모델이 찌그러진다.
        if (def.IsGreybox) visual.transform.localScale = def.size;

        var mf = visual.AddComponent<MeshFilter>();
        var mr = visual.AddComponent<MeshRenderer>();
        mf.sharedMesh = def.mesh != null ? def.mesh : CubeMesh();
        mr.sharedMaterial = def.material != null ? def.material : GreyMaterial(def.greyColor);
        mr.shadowCastingMode = def.castShadow
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;

        ApplyCollider(go, def, visual.transform);
        ApplyBreakable(go, def);
        ApplyLight(go, def);
        return go;
    }

    // ── 콜라이더 ─────────────────────────────────────────────────────

    static void ApplyCollider(GameObject go, Prop3DDefinition def, Transform visual)
    {
        // 콜라이더는 **루트**에 붙인다 — 자식 Visual의 스케일을 타면 크기가 두 번 곱해진다.
        // 프롭 원점이 바닥이므로 상자·캡슐 중심은 항상 반 높이만큼 올린다.
        Vector3 local = def.size;
        Vector3 off = def.boxOffset + new Vector3(0f, def.groundOffset + def.size.y * 0.5f, 0f);

        switch (def.colliderMode)
        {
            case Prop3DDefinition.ColliderMode.None:
                return;

            case Prop3DDefinition.ColliderMode.Box:
            {
                var c = go.AddComponent<BoxCollider>();
                c.size = Vector3.Scale(local, def.boxSizeScale);
                c.center = off;
                c.isTrigger = def.isTrigger;
                break;
            }

            case Prop3DDefinition.ColliderMode.Capsule:
            {
                var c = go.AddComponent<CapsuleCollider>();
                c.radius = Mathf.Max(local.x, local.z) * 0.5f;
                c.height = local.y;
                c.center = off;
                c.direction = 1;                    // Y축
                c.isTrigger = def.isTrigger;
                break;
            }

            case Prop3DDefinition.ColliderMode.Mesh:
            {
                // ⚠️ 메시 콜라이더는 메시와 **같은 트랜스폼**에 있어야 한다 — 루트에 붙이면
                //    자식의 오프셋·스케일이 반영되지 않아 실제 모양과 어긋난다.
                var c = visual.gameObject.AddComponent<MeshCollider>();
                c.sharedMesh = def.mesh != null ? def.mesh : CubeMesh();
                // 볼록(convex)으로 굽는다. 오목 메시 콜라이더는 정적일 때만 쓸 수 있고
                // 트리거로도 못 쓴다. 프롭은 부서지거나 트리거가 될 수 있으므로 볼록이 안전하다.
                c.convex = true;
                c.isTrigger = def.isTrigger;
                break;
            }

            case Prop3DDefinition.ColliderMode.Composite:
            {
                foreach (var b in def.compositeBoxes)
                {
                    var c = go.AddComponent<BoxCollider>();
                    c.size = b.size;
                    c.center = b.center + new Vector3(0f, def.groundOffset, 0f);
                    c.isTrigger = def.isTrigger;
                }
                break;
            }
        }
    }

    // ── 파괴 ─────────────────────────────────────────────────────────

    static void ApplyBreakable(GameObject go, Prop3DDefinition def)
    {
        if (!def.breakable) return;

        var br = go.AddComponent<Breakable>();
        br.stageCount     = def.breakStages;
        br.maxHp          = def.breakHp;
        br.destroyOnBreak = def.breakDestroy;

        if (!def.breakHittable) return;

        // 맞아서 부서지려면 **맞을 몸**이 있어야 한다 — Health + Hurtbox.
        // AttackPerformer가 Hurtbox를 스캔하므로 이게 없으면 때려도 아무 일이 없다.
        if (go.GetComponent<Health>() == null) go.AddComponent<Health>();
        if (go.GetComponent<Hurtbox>() == null) go.AddComponent<Hurtbox>();
    }

    // ── 조명 ─────────────────────────────────────────────────────────

    static void ApplyLight(GameObject go, Prop3DDefinition def)
    {
        if (!def.emitsLight) return;

        var lightGO = new GameObject("Light");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, def.lightHeight, 0f);

        var l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = def.lightColor;
        l.range = def.lightRange;
        l.intensity = def.lightIntensity;
        l.shadows = LightShadows.None;   // 프롭 램프까지 그림자를 켜면 값이 안 나온다
    }

    // ── 그레이박스 머티리얼 ──────────────────────────────────────────

    static readonly System.Collections.Generic.Dictionary<Color, Material> _greyCache = new();

    /// <summary>색만 다른 단색 머티리얼. 같은 색은 재사용한다 — 프롭마다 만들면
    /// 드로우콜이 색 수가 아니라 프롭 수만큼 늘어난다.</summary>
    static Material GreyMaterial(Color c)
    {
        if (_greyCache.TryGetValue(c, out var cached) && cached != null) return cached;

        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh) { name = $"p3_grey_{ColorUtility.ToHtmlStringRGB(c)}" };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.06f);
        _greyCache[c] = m;
        return m;
    }
}
