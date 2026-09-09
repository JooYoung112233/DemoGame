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


    // ── 평면 배율 ────────────────────────────────────────────────────
    /// <summary>맵 **평면(XZ) 좌표·크기에 곱하는 배율.** 높이는 곱하지 않는다 —
    /// 벽 3m·사람 1.8m는 이미 실제 치수라 늘리면 거인의 세계가 된다.
    ///
    /// 왜 필요한가: 맵 레이아웃 좌표가 빌더에 상수로 박혀 있다(지역1만 2223줄).
    /// 맵을 키우려고 그 숫자를 다 고치면 실수만 늘어난다. 대신 좌표가 지나가는
    /// **한 지점**에서 곱한다 — 도로·블록·건물·스폰이 비율을 유지한 채 함께 커진다.
    ///
    /// 배경(2026-09-08): 2D 시절 건물 상한 `MaxSpan=13m`은 **스프라이트 아트 제약**이었고
    /// (한 장으로 그릴 수 있는 크기), 그래서 실내는 별도 씬(최대 22×18m)으로 뺐다.
    /// 3D 그레이박스엔 그 제약이 없는데 상한만 남아, 건물이 실내 씬의 절반이 됐다.
    /// 배율로 건물까지 함께 키워 실내를 그 안에 담는다.</summary>
    public static float PlanScale = 1f;

    /// <summary>이번에 굽는 씬의 조명 성격. <see cref="GreyboxBuild.EndScene"/>이 저장 직전에 읽는다.
    /// 높이 세트로 실내/야외를 추측하지 않고 빌더가 명시한다 — 추측은 높이 값을 손보는 순간 틀린다.</summary>
    public static Lighting3D.Preset ScenePreset = Lighting3D.Preset.Outdoor;

    static float S(float v) => v * PlanScale;

    /// <summary>평면 좌표(2D 빌더의 x,y) → 3D 로컬 위치. 지면은 XZ, 높이는 Y.</summary>
    public static Vector3 Plan(float x, float y) => new Vector3(S(x), 0f, S(y));
    // ── 재질 ─────────────────────────────────────────────────────────
    static Material _floor, _wall, _barricade, _car, _prop, _door;

    public static void EnsurePalette()
    {
        if (_wall != null) return;
        _floor     = Mat(new Color(0.30f, 0.29f, 0.28f), 0.04f);
        // ⚠️ 키를 넘는 것은 **컷어웨이 재질**을 쓴다. 안 그러면 쿼터뷰에서 벽·지붕 뒤로
        //    들어가는 순간 플레이어가 통째로 가려진다(Stage 0에서 정한 화면공간 컷어웨이).
        //    마을은 처음부터 이 재질이었는데 레이드 맵만 빠져 있었다 — 정작 벽이 많은 쪽이다.
        _wall      = Occ(new Color(0.40f, 0.39f, 0.38f));
        _car       = Occ(new Color(0.32f, 0.35f, 0.38f));   // 차량 1.5m — 앉은 몸을 가린다
        // 낮은 것들은 애초에 시야를 안 막으므로 컷어웨이가 필요 없다(1m = 반엄폐).
        _barricade = Mat(new Color(0.38f, 0.31f, 0.24f), 0.06f);
        _prop      = Mat(new Color(0.44f, 0.41f, 0.36f), 0.06f);
        _door      = Mat(new Color(0.20f, 0.17f, 0.15f), 0.10f);
    }

    /// <summary>컷어웨이가 필요 없는 것들의 재질 — **Stage 0에서 정한 스타일라이즈드 룩**을 쓴다.
    ///
    /// ⚠️ `BRB/Stylized`는 Stage 0에서 룩을 확정하며 만들어 놓고 **어디에도 안 걸려 있었다.**
    ///    맵도 캐릭터도 기본 URP/Lit이라, 정해 둔 아트 방향(무광·부드러운 명암·림라이트)이
    ///    화면에 전혀 반영되지 않았다. 특히 림라이트는 Stage 0 함정 ③(어두운 팔레트가
    ///    배경에 묻힌다)에 직접 듣는 항목이다.
    ///
    /// <paramref name="smooth"/>는 URP/Lit 폴백에서만 쓰인다 — 스타일라이즈드는 무광이 전제다.</summary>
    static Material Mat(Color c, float smooth)
    {
        var sh = Shader.Find("BRB/Stylized");
        if (sh != null)
        {
            var s = new Material(sh);
            s.SetColor("_BaseColor", c);
            return s;
        }

        var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(lit);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    /// <summary>컷어웨이 재질 — <c>CutawayDriver</c>가 갱신하는 전역 유니폼을 읽어
    /// 플레이어를 가리는 부분에 화면 공간 구멍을 뚫는다. 재질별 배선은 필요 없다.</summary>
    static Material Occ(Color c)
    {
        var sh = Shader.Find("Spike/OccluderFX");
        if (sh == null)
        {
            Debug.LogWarning("[Greybox3D] Spike/OccluderFX 셰이더 없음 — 컷어웨이 없이 굽는다(벽 뒤에서 플레이어가 가려진다).");
            return Mat(c, 0.05f);
        }
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
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
        => Box(p, name, new Vector3(S(cx), -Heights.floor * 0.5f, S(cy)),
               new Vector3(S(w), Heights.floor, S(h)), _floor, 0f);

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
        return Box(p, name, new Vector3(S(cx), height * 0.5f, S(cz)), new Vector3(S(sx), height, S(sz)), mat, yaw);
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
                go.transform.localPosition = new Vector3(S(x), 0f, S(y));
                var sp = go.AddComponent<SpawnPoint>();

                // ⚠️ **`pointId`를 채운다.** 오브젝트 이름만 바꾸면 필드는 기본값 "default"로 남아
                //    맵의 스폰이 전부 "default"를 자칭한다 — 어느 출입구로 들어와도 가장 먼저
                //    만들어진 스폰으로 떨어진다(마을에서 실제로 그랬다).
                var so = new SerializedObject(sp);
                so.FindProperty("pointId").stringValue = name;
                so.ApplyModifiedPropertiesWithoutUndo();
                return 1;
            }

            case "gb_door":
                // 문 '표시' — 통과는 출구 발판이 담당하므로 콜라이더는 두지 않는다.
                Box(p, name, new Vector3(S(x), 1.2f, S(y)), new Vector3(2.2f, 2.4f, 0.15f), _door, 0f);
                Strip(p, name);
                return 1;

            // 상자·쪽지·적·탈출구 — 아직 모델이 없다. 크기만 다른 그레이박스로 세우고
            // 기능(콜라이더·컴포넌트)은 호출부가 얹는다. 위치가 맞는 것이 지금 단계의 목표다.
            case "gb_crate":  return Standing(p, name, x, y, 0.9f, 0.9f, 0.9f, _prop, 0f);
            case "gb_note":   return Standing(p, name, x, y, 0.4f, 0.4f, 0.15f, _prop, 0f);
            case "gb_enemy":  return Standing(p, name, x, y, 0.6f, 0.6f, 1.8f, _barricade, 0f);
            // 탈출 발판 = **바닥 표식**이다. 콜라이더를 남기면 8cm짜리 판때기가 벽이 된다 —
            //   캡슐은 턱을 못 넘으므로 발판 위에 설 수 없고, 반경 1.66m(판 절반 1.36 + 몸 0.3)
            //   밖에서 튕긴다(QA 봇이 Exit_Fixed 1.6m 앞에서 7번 끼인 것이 이것).
            //   상호작용은 콜라이더가 아니라 InteractableObject.All 거리 판정이라 떼도 그대로 동작한다.
            case "gb_exit":
            {
                int r = Standing(p, name, x, y, 1.6f, 1.6f, 0.08f, _door, 0f);
                if (r > 0) Strip(p, name);
                return r;
            }

            default:
            {
                var go = new GameObject(name);
                go.transform.SetParent(p.transform, false);
                go.transform.localPosition = new Vector3(S(x), 0f, S(y));
                return 1;
            }
        }
    }


    /// <summary>방금 만든 상자에서 콜라이더를 뗀다(문 표시처럼 통과해야 하는 장식용).</summary>
    static void Strip(GameObject p, string name)
    {
        var t = p.transform.Find(name);
        if (t == null) return;
        var c = t.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    // ── 걸어 들어가는 방 ─────────────────────────────────────────────

    /// <summary>실내를 걸어 다닐 수 있는 **최소 발자국**(m). 이보다 작으면 방이 아니라
    /// 사람이 낄 상자다 — 그런 건 그냥 막힌 덩어리로 둔다.</summary>
    public const float MinRoomSpan = 7f;

    /// <summary>이 발자국이 방이 될 수 있는가.</summary>
    public static bool CanBeRoom(float x0, float z0, float x1, float z1)
        => (x1 - x0) >= MinRoomSpan && (z1 - z0) >= MinRoomSpan;

    /// <summary>**걸어 들어가는 건물** — 바닥 + 벽 4면(문 쪽은 갈라서 비움) + 지붕.
    ///
    /// 별도 실내 씬으로 넘기지 않고 같은 맵 안에서 들어간다(2026-09-08 결정).
    /// 쿼터뷰라 지붕이 있으면 안이 안 보이므로, 진입 판정 볼륨 + <see cref="BuildingInterior"/>가
    /// 들어온 순간 지붕을 끈다. 로딩이 없어 레이드의 긴장이 끊기지 않는 것이 요점이다.
    ///
    /// <paramref name="side"/> = 문이 뚫릴 면('S'/'N'/'W'/'E'),
    /// <paramref name="doorAt"/> = 그 면을 따라 문이 시작하는 좌표.</summary>
    public static int Room(GameObject parent, string name,
                           float x0, float z0, float x1, float z1,
                           char side, float doorAt, float doorW = 2.4f)
    {
        EnsurePalette();

        var root = new GameObject(name);
        root.transform.SetParent(parent.transform, false);
        int n = 1;

        const float T = 1f;                 // 벽 두께
        float h = Heights.building;

        // 실내 바닥 — 바깥 지면보다 살짝 올려 문턱을 만든다.
        n += Box(root, name + "_Floor", new Vector3(S((x0 + x1) * 0.5f), 0.03f, S((z0 + z1) * 0.5f)),
                 new Vector3(S(x1 - x0), 0.06f, S(z1 - z0)), _floor, 0f);

        // 벽 4면. 문이 있는 면만 두 토막으로 갈라 가운데를 비운다.
        var prev = Heights;
        Heights = new HeightSet { wall = h, barricade = prev.barricade, car = prev.car,
                                  prop = prev.prop, building = h, floor = prev.floor };

        n += side == 'S' ? Gap(root, name + "_S", x0, x1, z0, z0 + T, true,  doorAt, doorW)
                         : WallSeg(root, name + "_S", x0, z0, x1, z0 + T);
        n += side == 'N' ? Gap(root, name + "_N", x0, x1, z1 - T, z1, true,  doorAt, doorW)
                         : WallSeg(root, name + "_N", x0, z1 - T, x1, z1);
        n += side == 'W' ? Gap(root, name + "_W", z0, z1, x0, x0 + T, false, doorAt, doorW)
                         : WallSeg(root, name + "_W", x0, z0, x0 + T, z1);
        n += side == 'E' ? Gap(root, name + "_E", z0, z1, x1 - T, x1, false, doorAt, doorW)
                         : WallSeg(root, name + "_E", x1 - T, z0, x1, z1);

        Heights = prev;

        // 지붕 — 이름이 "Roof"로 시작해야 BuildingInterior가 자동으로 찾는다.
        n += Box(root, "Roof", new Vector3(S((x0 + x1) * 0.5f), h + 0.15f, S((z0 + z1) * 0.5f)),
                 new Vector3(S(x1 - x0) + T, 0.3f, S(z1 - z0) + T), _wall, 0f);

        // 진입 판정 + 지붕 끄기. 볼륨은 실내 공간을 덮는다.
        var trg = root.AddComponent<BoxCollider>();
        trg.isTrigger = true;
        trg.center = new Vector3(S((x0 + x1) * 0.5f), h * 0.5f, S((z0 + z1) * 0.5f));
        trg.size   = new Vector3(S(x1 - x0) - T * 1.5f, h, S(z1 - z0) - T * 1.5f);
        root.AddComponent<BuildingInterior>();

        // 천장등. 지붕을 씌운 순간 실내는 태양이 닿지 않아 **캄캄해진다** — 방을 만든
        // 그 자리에서 같이 달아야 "지붕은 있는데 조명은 없는 방"이 생기지 않는다.
        n += Lighting3D.CeilingLights(root,
                 S((x0 + x1) * 0.5f), S((z0 + z1) * 0.5f),
                 S(x1 - x0), S(z1 - z0), h);

        return n;
    }

    /// <summary>한 면을 문 폭만큼 비워 두 토막으로 세운다.</summary>
    static int Gap(GameObject p, string name, float a0, float a1, float b0, float b1,
                   bool alongX, float doorAt, float doorW)
    {
        float d0 = Mathf.Clamp(doorAt, a0, a1);
        float d1 = Mathf.Clamp(doorAt + doorW, a0, a1);
        int n = 0;
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
