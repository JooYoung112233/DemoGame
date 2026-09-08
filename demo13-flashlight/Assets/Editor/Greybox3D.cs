#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 맵 그레이박스 3D판 — <see cref="GreyboxBuild"/>와 **같은 시그니처**로 XZ 평면에 세운다.
///
/// 이 프로젝트의 맵 빌더는 전부 `GreyboxBuild` 위에 서 있다(지역1 2223줄, 고철시장 500줄,
/// 실내 15씬). 그래서 3D 전환을 각 빌더마다 손으로 고치는 대신, **프리미티브 계층 하나만**
/// 3D로 바꾸고 빌더는 호출 대상만 바꾸게 했다. 좌표 인자 이름도 <c>cy</c>·<c>ay</c> 그대로 두어
/// 기존 코드를 그대로 옮길 수 있다 — 다만 그 값의 뜻은 **월드 Z**다.
///
/// 2D → 3D에서 새로 필요한 유일한 정보는 <b>높이</b>다. 인자를 하나씩 늘리면 호출부를 전부
/// 고쳐야 하므로, 종류별 기본 높이를 여기에 두고 필요할 때만 <see cref="Heights"/>로 바꾼다.
///
/// 설계: docs/3d-migration.md Stage 3
/// </summary>
public static class Greybox3D
{
    // ── 종류별 기본 높이 ─────────────────────────────────────────────
    /// <summary>맵 종류마다 벽 높이가 다르다(실내는 낮고 거리는 높다). 빌더가 시작할 때 바꾼다.</summary>
    public struct HeightSet
    {
        public float wall;        // 벽 — 시야를 완전히 막는다
        public float barricade;   // 바리케이드 — 앉으면 가려지는 반엄폐
        public float car;         // 차량
        public float prop;        // 일반 프롭
        public float building;    // 건물 외벽
        public float floor;       // 바닥 두께
    }

    public static HeightSet Heights = Default;

    public static HeightSet Default => new HeightSet
    {
        wall = 3.0f, barricade = 1.0f, car = 1.5f, prop = 1.0f, building = 3.6f, floor = 0.1f,
    };

    /// <summary>실내 — 천장이 낮고 벽도 낮다.</summary>
    public static HeightSet Indoor => new HeightSet
    {
        wall = 2.6f, barricade = 0.9f, car = 1.5f, prop = 0.9f, building = 2.6f, floor = 0.1f,
    };

    // ── 재질 ─────────────────────────────────────────────────────────
    static Material _floor, _wall, _barricade, _car, _prop, _door;

    public static void EnsurePalette()
    {
        if (_wall != null) return;
        _floor     = Mat(new Color(0.30f, 0.29f, 0.28f), 0.04f);
        _wall      = Mat(new Color(0.40f, 0.39f, 0.38f), 0.05f);
        _barricade = Mat(new Color(0.38f, 0.31f, 0.24f), 0.06f);
        _car       = Mat(new Color(0.32f, 0.35f, 0.38f), 0.18f);
        _prop      = Mat(new Color(0.44f, 0.41f, 0.36f), 0.06f);
        _door      = Mat(new Color(0.20f, 0.17f, 0.15f), 0.10f);
    }

    static Material Mat(Color c, float smooth)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    // ── 씬 ───────────────────────────────────────────────────────────

    public static GameObject BeginScene(out Scene scene)
    {
        EnsurePalette();
        Heights = Default;
        return GreyboxBuild.BeginScene(out scene);
    }

    public static void EndScene(Scene scene, string path, int placed, string label)
        => GreyboxBuild.EndScene(scene, path, placed, label);

    // ── 프리미티브 ───────────────────────────────────────────────────

    /// <summary>바닥판. 2D의 (cx, cy)는 3D에서 (cx, cz)다.</summary>
    public static int Floor(GameObject p, string name, float cx, float cy, float w, float h)
    {
        // ⚠️ 빌더가 2D BeginScene을 쓰면 여기가 팔레트보다 먼저 불린다 — 재질이 비어 자홍색이 된다.
        EnsurePalette();
        return FloorImpl(p, name, cx, cy, w, h);
    }

    static int FloorImpl(GameObject p, string name, float cx, float cy, float w, float h)
        => Box(p, name, new Vector3(cx, -Heights.floor * 0.5f, cy),
               new Vector3(w, Heights.floor, h), _floor, 0f);

    public static int Wall(GameObject p, string name, float cx, float cy, float lenX, float thickY)
        => Wall(p, name, cx, cy, lenX, thickY, 0f);

    /// <summary>벽 막대(회전). 2D의 Z축 회전은 3D에서 **Y축 yaw**다.</summary>
    public static int Wall(GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg)
        => Standing(p, name, cx, cy, lenX, thickY, Heights.wall, _wall, angleDeg);

    public static int Barricade(GameObject p, string name, float cx, float cy, float lenX, float thickY)
        => Barricade(p, name, cx, cy, lenX, thickY, 0f);

    public static int Barricade(GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg)
        => Standing(p, name, cx, cy, lenX, thickY, Heights.barricade, _barricade, angleDeg);

    public static int Car(GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg = 0f)
        => Standing(p, name, cx, cy, lenX, thickY, Heights.car, _car, angleDeg);

    public static int Prop(GameObject p, string name, float cx, float cy, float sx, float sy)
        => Standing(p, name, cx, cy, sx, sy, Heights.prop, _prop, 0f);

    /// <summary>두 점 A→B를 잇는 벽(자동 회전·길이).</summary>
    public static int WallLine(GameObject p, string name, float ax, float ay, float bx, float by, float thick = 1f)
        => Line(p, name, ax, ay, bx, by, thick, Heights.wall, _wall);

    public static int BarricadeLine(GameObject p, string name, float ax, float ay, float bx, float by, float thick = 1f)
        => Line(p, name, ax, ay, bx, by, thick, Heights.barricade, _barricade);

    static int Line(GameObject p, string name, float ax, float ay, float bx, float by,
                    float thick, float height, Material mat)
    {
        float dx = bx - ax, dz = by - ay;
        float len = Mathf.Sqrt(dx * dx + dz * dz);
        if (len < 0.001f) return 0;
        // ⚠️ 2D는 +X에서 반시계로 재는 각(atan2(dy,dx))이고, Unity yaw는 +Z에서 시계로 잰다.
        //    그대로 넘기면 벽이 90도 틀어진다. yaw = atan2(dx, dz).
        float yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        // 길이를 Z(전방)로 두므로 폭·길이를 바꿔 넣는다.
        return Standing(p, name, (ax + bx) * 0.5f, (ay + by) * 0.5f, thick, len, height, mat, 0f, yaw);
    }

    /// <summary>사각형 영역 [ax,bx]×[ay,by]를 채우는 벽. 길이 0 이하면 스킵.</summary>
    public static int WallSeg(GameObject p, string name, float ax, float ay, float bx, float by)
    {
        if (bx - ax <= 0.001f || by - ay <= 0.001f) return 0;
        return Wall(p, name, (ax + bx) * 0.5f, (ay + by) * 0.5f, bx - ax, by - ay);
    }

    /// <summary>바닥에 서는 상자. 원점이 바닥이 되도록 반 높이만큼 올린다.</summary>
    static int Standing(GameObject p, string name, float cx, float cz, float sx, float sz,
                        float height, Material mat, float angleDeg2D, float yawOverride = float.NaN)
    {
        float yaw = float.IsNaN(yawOverride) ? -angleDeg2D : yawOverride;
        return Box(p, name, new Vector3(cx, height * 0.5f, cz), new Vector3(sx, height, sz), mat, yaw);
    }

    static int Box(GameObject p, string name, Vector3 center, Vector3 size, Material mat, float yaw)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(p.transform, false);
        go.transform.localPosition = center;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return 1;
    }


    /// <summary>2D 빌더의 `Bar`에 1:1 대응 — 프리팹 id로 종류를 받아 그에 맞는 높이·재질로 세운다.
    /// 이 하나만 갈아끼우면 Wall·Barricade·Car·Prop과 그 위에 선 WallSeg·WallLine·Building이
    /// 전부 3D로 따라온다.</summary>
    public static int Bar(string prefabId, GameObject p, string name,
                          float cx, float cz, float lenX, float thickZ, float angleDeg = 0f)
    {
        EnsurePalette();
        switch (prefabId)
        {
            case "gb_wall":      return Standing(p, name, cx, cz, lenX, thickZ, Heights.wall,      _wall,      angleDeg);
            case "gb_barricade": return Standing(p, name, cx, cz, lenX, thickZ, Heights.barricade, _barricade, angleDeg);
            case "gb_car":       return Standing(p, name, cx, cz, lenX, thickZ, Heights.car,       _car,       angleDeg);
            default:             return Standing(p, name, cx, cz, lenX, thickZ, Heights.prop,      _prop,      angleDeg);
        }
    }
    // ── 마커 ─────────────────────────────────────────────────────────

    /// <summary>스폰·출구·문 같은 기능 마커. 2D는 프리팹 id로 구분했다 —
    /// 3D에도 같은 id를 받아 대응하는 것을 세운다(빌더 코드를 안 고치기 위해).</summary>
    public static int Marker(GameObject p, string prefabId, string name, float x, float y)
    {
        EnsurePalette();
        return MarkerImpl(p, prefabId, name, x, y);
    }

    static int MarkerImpl(GameObject p, string prefabId, string name, float x, float y)
    {
        switch (prefabId)
        {
            case "gb_spawn":
            {
                var go = new GameObject(name);
                go.transform.SetParent(p.transform, false);
                go.transform.localPosition = new Vector3(x, 0f, y);
                go.AddComponent<SpawnPoint>();
                return 1;
            }
            case "gb_door":
                // 문 '표시' — 통과는 출구 발판이 담당하므로 콜라이더는 두지 않는다.
                return Box(p, name, new Vector3(x, 1.2f, y), new Vector3(2.2f, 2.4f, 0.15f), _door, 0f)
                       + Strip(p, name);
            default:
            {
                var go = new GameObject(name);
                go.transform.SetParent(p.transform, false);
                go.transform.localPosition = new Vector3(x, 0f, y);
                return 1;
            }
        }
    }

    /// <summary>방금 만든 상자에서 콜라이더를 뗀다(장식용).</summary>
    static int Strip(GameObject p, string name)
    {
        var t = p.transform.Find(name);
        if (t != null)
        {
            var c = t.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return 0;
    }

    // ── 건물 ─────────────────────────────────────────────────────────

    /// <summary>폐쇄 '건물' = 둘레 4벽(두께 1) + 한 면에 2m 문 갭.
    /// side: 'S'/'N'/'W'/'E', doorAt = 그 면을 따라 문 시작 좌표(문 = doorAt~doorAt+2).</summary>
    public static int Building(GameObject p, string name, float x0, float y0, float x1, float y1,
                               char side, float doorAt, float doorW = 2f)
    {
        var prev = Heights;
        Heights = new HeightSet
        {
            wall = prev.building, barricade = prev.barricade, car = prev.car,
            prop = prev.prop, building = prev.building, floor = prev.floor,
        };

        int n = 0;
        float t = 1f;   // 벽 두께

        // 문이 없는 면은 통짜, 있는 면은 두 토막.
        n += side == 'S' ? Split(p, name + "_S", x0, x1, y0, y0 + t, true,  doorAt, doorW)
                         : WallSeg(p, name + "_S", x0, y0, x1, y0 + t);
        n += side == 'N' ? Split(p, name + "_N", x0, x1, y1 - t, y1, true,  doorAt, doorW)
                         : WallSeg(p, name + "_N", x0, y1 - t, x1, y1);
        n += side == 'W' ? Split(p, name + "_W", y0, y1, x0, x0 + t, false, doorAt, doorW)
                         : WallSeg(p, name + "_W", x0, y0, x0 + t, y1);
        n += side == 'E' ? Split(p, name + "_E", y0, y1, x1 - t, x1, false, doorAt, doorW)
                         : WallSeg(p, name + "_E", x1 - t, y0, x1, y1);

        Heights = prev;
        return n;
    }

    /// <summary>한 면을 문 갭만큼 비워 두 토막으로 세운다.
    /// <paramref name="alongX"/>면 a0~a1이 X축, 아니면 Z축이다.</summary>
    static int Split(GameObject p, string name, float a0, float a1, float b0, float b1,
                     bool alongX, float doorAt, float doorW)
    {
        int n = 0;
        float d0 = Mathf.Clamp(doorAt, a0, a1);
        float d1 = Mathf.Clamp(doorAt + doorW, a0, a1);

        if (alongX)
        {
            n += WallSeg(p, name + "_a", a0, b0, d0, b1);
            n += WallSeg(p, name + "_b", d1, b0, a1, b1);
        }
        else
        {
            n += WallSeg(p, name + "_a", b0, a0, b1, d0);
            n += WallSeg(p, name + "_b", b0, d1, b1, a1);
        }
        return n;
    }
}
#endif
