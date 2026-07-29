using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬마다 <see cref="NavGrid"/>를 자동으로 깔아준다.
///
/// <para><b>왜 필요한가.</b> NavGrid는 CombatSandbox 빌더만 생성해서 실제 게임 씬
/// (Safehouse/Zone1/Int_*12/Pawnshop/Hideout…)에는 <b>하나도 없었다</b>.
/// <see cref="NavAgent"/>는 격자가 없으면 경로를 버리고 목표 직진으로 폴백하므로
/// (NavAgent.Repath), 적·NPC가 A*를 한 번도 쓰지 못하고 벽에 비볐다.
/// 2026-07-27 QA 자동 플레이가 STUCK 8회로 잡아냄.</para>
///
/// <para><b>왜 씬에 안 박고 런타임에 붙이나.</b> 씬 18개 + 앞으로 늘어날 내부 씬까지
/// 매번 손으로 배치하면 또 빠진다. 씬 파일을 안 건드리므로 에디터 상태 보존 규약과도
/// 충돌하지 않는다. 손으로 배치한 NavGrid가 있으면 그쪽을 존중하고 건너뛴다.</para>
///
/// 영역은 씬의 콜라이더 전체를 감싸도록 자동 계산하고, 넓은 맵은 셀을 키워
/// 베이크 비용을 상한선 안에 묶는다.
/// </summary>
public static class NavGridBootstrap
{
    const float CellSize     = 0.5f;   // NavGrid 기본값과 동일(적 바디 기준 적정)
    const float AgentRadius  = 0.35f;
    const float Margin       = 4f;     // 맵 가장자리 바깥 여유 — 벽에 붙은 목표도 격자 안에 들어오게
    // 한 변 셀 상한. 넘으면 cellSize를 키운다.
    // 2026-07-28: 220이면 Zone1(324m)이 셀 1.54m가 돼 팽창이 4.4배로 튀고 통로가 막혔다.
    // 700이면 Zone1도 0.5m를 유지한다(648셀). 베이크는 한 번뿐이고 씬 전환 페이드 뒤에 가려진다.
    const int   MaxCellsSide = 700;

    static bool _hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;   // 맵툴 씬은 게임플레이가 아니다
        if (!_hooked)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _hooked = true;
        }

        // 이미 열려 있는 씬(첫 진입 — sceneLoaded를 놓친다)도 처리
        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureFor(SceneManager.GetSceneAt(i));
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureFor(scene);

    /// <summary>맵이 런타임에 바뀌었을 때(절차적 생성·문 개폐 등) 다시 굽는다.</summary>
    public static void Rebake()
    {
        var g = NavGrid.Instance;
        if (g == null) return;
        Physics2D.SyncTransforms();
        g.Rebuild();
    }

    static void EnsureFor(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name == "Systems") return;      // 매니저·UI·PlayerRig만 — 맵 지오메트리 없음
        if (HasNavGrid(scene)) return;            // 손으로 배치한 격자 존중

        if (!TryComputeBounds(scene, out Bounds b))
            return;                               // 콜라이더가 없는 씬 — 막힐 게 없으니 직선으로 충분

        Vector2 area = (Vector2)b.size + Vector2.one * (Margin * 2f);
        float cell = CellSize;
        float longest = Mathf.Max(area.x, area.y);
        if (longest / cell > MaxCellsSide) cell = longest / MaxCellsSide;

        var go = new GameObject("[NavGrid]");
        SceneManager.MoveGameObjectToScene(go, scene);   // 씬과 함께 소멸 — 다음 맵에 새로 깔린다
        var grid = go.AddComponent<NavGrid>();

        Physics2D.SyncTransforms();                      // 방금 로드된 콜라이더 위치 확정
        grid.Configure(b.center, area, cell, AgentRadius);

        float pct = grid.BlockedRatio * 100f;
        Debug.Log($"[NavGrid] 자동 배치 — {scene.name}  영역 {area.x:0}×{area.y:0}m  "
                  + $"격자 {grid.Width}×{grid.Height}(셀 {cell:0.00}m)  막힘 {pct:0.#}%");

        if (grid.BlockedRatio <= 0f)
            Debug.LogWarning($"[NavGrid] {scene.name}: 장애물을 하나도 못 잡았다. "
                             + "지오메트리가 런타임 생성이면 생성 후 NavGridBootstrap.Rebake() 호출 필요.");
    }

    static bool HasNavGrid(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].GetComponentInChildren<NavGrid>(true) != null) return true;
        return false;
    }

    /// <summary>씬 안 콜라이더 전체를 감싸는 경계. Player/Enemy는 장애물이 아니므로 제외
    /// (플레이어가 맵 밖에 서 있으면 격자가 통째로 어긋난다).</summary>
    static bool TryComputeBounds(Scene scene, out Bounds bounds)
    {
        bounds = default;
        bool any = false;

        int ignore = 0;
        int p = LayerMask.NameToLayer("Player");
        int e = LayerMask.NameToLayer("Enemy");
        if (p >= 0) ignore |= 1 << p;
        if (e >= 0) ignore |= 1 << e;

        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var cols = roots[i].GetComponentsInChildren<Collider2D>(true);
            for (int j = 0; j < cols.Length; j++)
            {
                var c = cols[j];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (((1 << c.gameObject.layer) & ignore) != 0) continue;

                if (!any) { bounds = c.bounds; any = true; }
                else bounds.Encapsulate(c.bounds);
            }
        }
        return any;
    }
}
