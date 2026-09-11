#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 3D 씬의 **조명 셋업 한 곳**. 태양(디렉셔널) · 앰비언트 · 실내 천장등을 여기서만 만든다.
///
/// 2D 시절 조명은 `Light2D` 글로벌 하나가 화면 전체를 덮는 방식이었고, 맵 빌더는 조명을
/// 아예 만들지 않았다(<see cref="SceneLightingBuilder"/>가 나중에 얹었다). 3D로 넘어오며
/// 그 전제가 통째로 깨졌다 — 메시는 디렉셔널 라이트가 없으면 앰비언트만 받아 납작해지고,
/// <see cref="Greybox3D.Room"/>이 지붕을 씌우면서 실내는 **진짜로 캄캄해졌다**.
///
/// 마을·은신처는 각자 자기 파일 안에서 태양을 세우고 있었다(Safehouse3DLayout /
/// Hideout3DLayout). 같은 값을 두 군데서 손보는 상태라 여기로 모은다 — 레이드 맵까지
/// 세 번째 사본을 만들지 않기 위해서다.
///
/// <b>런타임 낮밤은 이 값을 덮어쓴다.</b> <see cref="DayNightCycle"/>이 씬의 디렉셔널을
/// 찾아 WeatherData 값으로 몰기 때문에, 여기 값은 "씬을 열었을 때의 모습"이자
/// 낮밤이 없는 씬(은신처)의 최종 모습이다.
///
/// 설계: docs/3d-migration.md Stage 2, docs/rendering.md
/// </summary>
public static class Lighting3D
{
    /// <summary>씬 성격에 따른 조명 프리셋.</summary>
    public enum Preset
    {
        /// <summary>야외 맵(마을·지역1·고철시장). 태양 + 하늘 앰비언트.</summary>
        Outdoor,
        /// <summary>별도 실내 씬(Int_*). 태양은 창으로 들어오는 정도로 죽이고 앰비언트를 올린다.</summary>
        Indoor,
        /// <summary>은신처 디오라마. 낮밤을 타지 않고 항상 환하다.</summary>
        Hideout,
    }

    // ── 태양 ─────────────────────────────────────────────────────────

    /// <summary>씬에 디렉셔널 라이트 하나를 보장한다. 이미 있으면 그것을 프리셋 값으로 맞춘다
    /// — 빌드를 두 번 돌려도 태양이 두 개가 되지 않는다(두 개면 그림자가 겹쳐 지저분해진다).</summary>
    public static Light Sun(GameObject parent, Preset preset)
    {
        var sun = FindDirectional();
        if (sun == null)
        {
            var go = new GameObject("Sun3D");
            if (parent != null) go.transform.SetParent(parent.transform, false);
            sun = go.AddComponent<Light>();
        }
        sun.type = LightType.Directional;

        switch (preset)
        {
            case Preset.Indoor:
                // 실내는 태양이 벽에 막힌다. 방향광을 남기되 약하게 — 완전히 끄면
                // 면끼리 명암 차가 사라져 벽·바닥·프롭이 한 덩어리로 보인다.
                sun.intensity = 0.45f;
                sun.color = new Color(0.86f, 0.88f, 1f);
                sun.shadows = LightShadows.None;
                sun.transform.rotation = Quaternion.Euler(60f, 25f, 0f);
                break;

            case Preset.Hideout:
                // 컨테이너 내부 — 천창으로 새어드는 찬 빛. 밝기는 매달린 전구가 낸다.
                sun.intensity = 0.55f;
                sun.color = new Color(0.80f, 0.86f, 1f);
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(62f, -30f, 0f);
                break;

            default:
                sun.intensity = 1.05f;
                sun.color = new Color(1f, 0.95f, 0.86f);
                sun.shadows = LightShadows.Soft;
                // 45도 근처를 피한다 — 쿼터뷰 카메라와 각이 겹치면 그림자가 물체 뒤에 숨어
                // 입체감이 사라진다. 옆에서 비스듬히 들어와야 벽 두께가 읽힌다.
                sun.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
                break;
        }

        // 낮밤이 몰 대상임을 표시한다. 표시가 없으면 DayNightCycle이 "먼저 찾은 디렉셔널"을
        // 몰게 되고, 그게 화면을 밝히는 태양이 아니면 밤이 와도 맵은 대낮 그대로다.
        var mark = sun.GetComponent<SunLight>() ?? sun.gameObject.AddComponent<SunLight>();
        mark.followDayNight = preset != Preset.Hideout;   // 은신처는 시간과 무관하게 고정
        return sun;
    }

    /// <summary>⚠️ <b>활성 씬만</b> 본다. 맵 빌더는 기존 씬을 열어 둔 채 새 씬을 additive로
    /// 만들어 굽기 때문에(<see cref="EditorSceneBuildUtil.NewDetachedScene"/>), 씬을 가리지 않고
    /// 찾으면 열려 있던 마을의 태양을 집어 그쪽을 고치고 새 맵은 광원 0개로 저장된다.</summary>
    static Light FindDirectional()
    {
        var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.gameObject.scene == active) return l;
        return null;
    }

    // ── 앰비언트 ─────────────────────────────────────────────────────

    /// <summary>씬 앰비언트(하늘·수평·바닥 3색). 이게 실내 밝기의 바닥값이다 —
    /// 천장등이 닿지 않는 구석이 새까맣게 뭉치지 않도록 0으로 두지 않는다.</summary>
    public static void Ambient(Preset preset)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        switch (preset)
        {
            case Preset.Indoor:
                RenderSettings.ambientSkyColor     = new Color(0.30f, 0.31f, 0.35f);
                RenderSettings.ambientEquatorColor = new Color(0.24f, 0.24f, 0.26f);
                RenderSettings.ambientGroundColor  = new Color(0.14f, 0.13f, 0.13f);
                break;

            case Preset.Hideout:
                // 어둡다. 은신처는 좁고 아늑한 곳이라 앰비언트를 낮게 깔고 전구로 살린다.
                RenderSettings.ambientSkyColor     = new Color(0.20f, 0.22f, 0.26f);
                RenderSettings.ambientEquatorColor = new Color(0.15f, 0.15f, 0.17f);
                RenderSettings.ambientGroundColor  = new Color(0.09f, 0.09f, 0.10f);
                break;

            default:
                RenderSettings.ambientSkyColor     = new Color(0.40f, 0.44f, 0.52f);
                RenderSettings.ambientEquatorColor = new Color(0.30f, 0.30f, 0.32f);
                RenderSettings.ambientGroundColor  = new Color(0.16f, 0.15f, 0.14f);
                break;
        }
    }

    /// <summary>태양 + 앰비언트를 한 번에. 맵 빌더가 끝날 때 부르는 진입점.</summary>
    public static void Apply(GameObject parent, Preset preset)
    {
        Sun(parent, preset);
        Ambient(preset);
    }

    // ── 실내 천장등 ──────────────────────────────────────────────────

    /// <summary>천장등 하나가 감당하는 대략의 바닥 폭(m). 방이 이보다 넓으면 나눠 단다.</summary>
    public const float LampSpan = 9f;

    /// <summary>지붕 있는 방 안에 천장등을 단다. 방이 넓으면 <see cref="LampSpan"/> 간격으로 격자 배치.
    ///
    /// 좌표는 <b>월드</b> 기준이며 <see cref="Greybox3D.PlanScale"/>이 이미 곱해진 값을 받는다 —
    /// 호출자(Room)가 스케일을 적용한 뒤 넘긴다. 여기서 또 곱하면 두 번 곱해진다.</summary>
    /// <returns>단 등의 개수.</returns>
    public static int CeilingLights(GameObject room, float cx, float cz, float w, float d, float h)
    {
        int nx = Mathf.Max(1, Mathf.RoundToInt(w / LampSpan));
        int nz = Mathf.Max(1, Mathf.RoundToInt(d / LampSpan));
        int n = 0;

        for (int ix = 0; ix < nx; ix++)
        for (int iz = 0; iz < nz; iz++)
        {
            // 칸의 중심. 등이 벽에 붙지 않도록 칸을 균등 분할한 뒤 그 가운데에 둔다.
            float px = cx - w * 0.5f + w * (ix + 0.5f) / nx;
            float pz = cz - d * 0.5f + d * (iz + 0.5f) / nz;
            Lamp(room, $"CeilLight_{ix}_{iz}", new Vector3(px, h - 0.35f, pz));
            n++;
        }
        return n;
    }

    /// <summary>씬 전체(=방 하나로 된 실내 씬)에 천장등을 깐다.
    ///
    /// <see cref="CeilingLights"/>는 <see cref="Greybox3D.Room"/>이 방을 만들 때 그 방에만 다는
    /// 것이고, 이쪽은 <b>방 구분 없이 통째로 실내인 씬</b>(<c>Int_*</c>)용이다. 그 씬들은 옛
    /// 2D 실내 빌더가 만든 것이라 Room을 쓰지 않아 등이 하나도 안 달린다 — 태양만 남아
    /// 회색 상자 위를 걷게 된다.
    ///
    /// 넓이는 씬에 놓인 렌더러 전체의 바운즈로 잰다. 빌더마다 맵 크기가 다르고, 그 값을
    /// 여기까지 넘기려면 빌더 15개를 전부 고쳐야 한다.</summary>
    /// <returns>단 등의 개수.</returns>
    public static int FillCeilingLamps(GameObject root, float ceilingHeight)
    {
        if (root == null) return 0;

        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return 0;

        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        // 바운즈에는 벽 두께가 섞여 있다. 가장자리에 등이 붙지 않도록 살짝 줄인다.
        float w = Mathf.Max(1f, b.size.x - 2f);
        float d = Mathf.Max(1f, b.size.z - 2f);

        var holder = new GameObject("CeilingLights");
        holder.transform.SetParent(root.transform, false);
        return CeilingLights(holder, b.center.x, b.center.z, w, d, ceilingHeight);
    }

    /// <summary>천장에 매달린 백열등 하나.</summary>
    public static Light Lamp(GameObject parent, string name, Vector3 worldPos)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.transform.position = worldPos;

        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.90f, 0.74f);      // 낡은 백열 — 폐허 톤에 맞춘다
        l.range = LampSpan * 1.4f;                  // 칸 경계를 살짝 넘겨 겹쳐야 얼룩이 안 생긴다
        l.intensity = 1.5f;
        // ⚠️ 그림자는 끈다. 실내마다 등이 여럿이고 URP는 추가 라이트 그림자 슬롯이 몇 개
        //    안 된다 — 켜면 조용히 일부만 그림자를 만들어 방마다 밝기가 달라 보인다.
        l.shadows = LightShadows.None;
        return l;
    }
}
#endif
