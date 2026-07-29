using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 봇의 **지각** — "AI가 아는 것"과 "실제로 존재하는 것"을 분리한다.
///
/// <para><b>왜 필요한가.</b> 예전 op는 <c>FindObjectsByType&lt;LootContainer&gt;()</c>로 맵의 상자를
/// 전부 즉시 알았다(전지). 그러면 루팅가치는 "사람이 얻을 값"이 아니라 **이론 최대값**이 되고,
/// 벽 뒤에 낀 상자·표식 없는 탈출구 같은 **발견성 결함을 원리적으로 못 잡는다**.
/// 사람은 게임 시작 시 상자도 탈출구도 모른다. AI도 그래야 밸런스가 측정된다.</para>
///
/// <para><b>발견 규칙은 게임 것을 그대로 쓴다</b> — <see cref="PlayerVision.CanSee"/>.
/// QA가 별도 규칙을 쓰면 게임과 다른 것을 검증하게 된다. 덕분에 시야 수치(각도·사거리)
/// 자체도 함께 검증된다: "이 시야로는 상자를 못 찾는다"가 데이터로 나온다.</para>
///
/// <para><b>오라클 모드</b>(<see cref="Omniscient"/>)는 전지 상태를 유지한다 — 도달성·배선 검증용.
/// 두 모드의 차이가 곧 <b>발견율</b> 지표다: 오라클 18개 도달 가능 vs 플레이어 3개 발견 → 17%.</para>
/// </summary>
public class QaPerception
{
    /// <summary>true면 전지(구 동작) — 도달성·배선 검증용. false면 사람처럼 본 것만 안다.</summary>
    public bool Omniscient;

    readonly HashSet<int> _seen = new HashSet<int>();          // 이미 발견한 오브젝트(InstanceID)
    readonly List<LootContainer> _crates = new List<LootContainer>();
    readonly List<InteractableObject> _exits = new List<InteractableObject>();
    readonly List<InteractableObject> _others = new List<InteractableObject>();
    readonly HashSet<Vector2Int> _walked = new HashSet<Vector2Int>();   // 밟은 격자(탐색 목표 선정용)

    string _scene = "";
    float _scanTimer;

    // 발견율 계산용 — 씬에 실제로 존재하는 수(오라클 기준)
    public int TotalCratesInScene { get; private set; }
    public int TotalExitsInScene { get; private set; }

    public IReadOnlyList<LootContainer> KnownCrates => _crates;
    public IReadOnlyList<InteractableObject> KnownExits => _exits;
    public IReadOnlyList<InteractableObject> KnownOthers => _others;

    public int KnownCrateCount => _crates.Count;
    public float CrateDiscoveryRate =>
        TotalCratesInScene <= 0 ? 0f : (float)_crates.Count / TotalCratesInScene;

    /// <summary>씬이 바뀌면 기억을 비운다 — 다른 맵의 상자를 알고 있을 수 없다.</summary>
    public void OnSceneChanged(string scene)
    {
        _scene = scene;
        _seen.Clear();
        _crates.Clear();
        _exits.Clear();
        _others.Clear();
        _walked.Clear();
        TotalCratesInScene = 0;
        TotalExitsInScene = 0;
    }

    public string Scene => _scene;

    /// <summary>매 프레임 호출. 시야에 들어온 것을 '발견'으로 등록한다(한 번 보면 기억).</summary>
    public void Tick(float dt)
    {
        string now = SceneManager.GetActiveScene().name;
        if (now != _scene) OnSceneChanged(now);

        var player = TopDownPlayer.Instance;
        if (player == null) return;

        // 밟은 칸 기록 — "안 가본 쪽"을 고를 때 쓴다
        _walked.Add(Cell(player.transform.position));

        _scanTimer += dt;
        if (_scanTimer < 0.2f) return;   // 4~5Hz면 충분(매 프레임 전수 스캔은 낭비)
        _scanTimer = 0f;

        Scan();
    }

    static Vector2Int Cell(Vector2 p) =>
        new Vector2Int(Mathf.FloorToInt(p.x / 4f), Mathf.FloorToInt(p.y / 4f));   // 4m 격자

    /// <summary>화면에 들어와 있는가 — **상자·상호작용물의 발견 기준**.
    ///
    /// 게임의 FOV 시야콘(150°·9m)은 <b>적만</b> 가린다(PlayerVision이 EnemyController에만 적용).
    /// 상자·문·탈출구는 화면에 있으면 사람 눈에 그냥 보인다. 그런데 2026-07-28에 내가
    /// 상자 발견에도 시야콘을 적용해서, AI가 사람보다 훨씬 좁게 보고 상자를 거의 못 찾았고 —
    /// 그래서 <b>파밍을 아예 안 했다</b>. 발견 기준은 대상별로 게임과 같아야 한다.</summary>
    static bool OnScreen(Vector3 world)
    {
        var cam = Camera.main;
        if (cam == null) return true;                 // 판정 불가 — 막지 않는다

        Vector3 v = cam.WorldToViewportPoint(world);

        // ★ 직교(2D) 카메라에서는 z 부호로 앞뒤를 가리면 안 된다.
        //   탑다운 2D는 카메라와 오브젝트가 사실상 같은 평면이라 v.z가 0이나 음수로 나오고,
        //   `v.z > 0` 조건이 **항상 거짓**이 되어 모든 것이 화면 밖으로 판정된다.
        //   2026-07-28: 그 탓에 맵을 37% 돌았는데 상자 30개 중 0개 발견 → 파밍이 원천 봉쇄됐다.
        bool depthOk = cam.orthographic || v.z > 0f;

        return depthOk && v.x >= -0.02f && v.x <= 1.02f && v.y >= -0.02f && v.y <= 1.02f;
    }

    /// <summary>발견 판정 — **화면 안 AND 시야 안**.
    ///
    /// 두 조건이 다 필요하다:
    ///   • 화면 밖   → 애초에 눈에 안 들어온다(카메라가 안 비춘다).
    ///   • 시야콘 밖 → <see cref="VisionDarkness"/> 오버레이가 <c>visionDarkAlpha</c>(기본 0.72)로
    ///                 덮어버려 **사람 눈에도 안 보인다**(2026-07-28 커밋 8f4b078).
    ///
    /// 처음엔 시야콘만 썼다가 "콘은 적만 가린다"고 판단해 화면 판정으로 바꿨는데,
    /// 그 사이 어둠 오버레이가 들어와 이제는 콘이 **모든 것**을 가린다. 둘 다 봐야 맞다.
    /// 판정 수치는 오버레이와 같은 GameTuning을 읽는 <see cref="PlayerVision.CanSee"/>를 그대로 쓴다.</summary>
    static bool CanNotice(Vector3 world) => OnScreen(world) && PlayerVision.CanSee(world);

    void Scan()
    {
        // 씬 전체 수는 '발견율'의 분모라 오라클로 센다(AI는 이 값을 쓰지 않는다).
        var allCrates = Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None);
        TotalCratesInScene = allCrates.Length;

        foreach (var box in allCrates)
        {
            if (box == null || !box.gameObject.activeInHierarchy) continue;
            int id = box.GetInstanceID();
            if (_seen.Contains(id)) continue;
            if (!Omniscient && !OnScreen(box.transform.position)) continue;
            _seen.Add(id);
            _crates.Add(box);
        }

        int exits = 0;
        foreach (var io in Object.FindObjectsByType<InteractableObject>(FindObjectsSortMode.None))
        {
            if (io == null || !io.gameObject.activeInHierarchy) continue;
            bool isExit = io.Type == InteractableObject.InteractType.ExitPoint;
            if (isExit) exits++;

            int id = io.GetInstanceID();
            if (_seen.Contains(id)) continue;
            if (!Omniscient && !OnScreen(io.transform.position)) continue;
            _seen.Add(id);
            if (isExit) _exits.Add(io); else _others.Add(io);
        }
        TotalExitsInScene = exits;

        _crates.RemoveAll(x => x == null || !x.gameObject.activeInHierarchy);
        _exits.RemoveAll(x => x == null || !x.gameObject.activeInHierarchy);
        _others.RemoveAll(x => x == null || !x.gameObject.activeInHierarchy);
    }

    /// <summary>아는 것 중 특정 종류의 상호작용 대상(가장 가까운 것).</summary>
    public InteractableObject NearestKnown(InteractableObject.InteractType type, Vector2 from)
    {
        InteractableObject best = null; float bestD = float.MaxValue;
        var pool = type == InteractableObject.InteractType.ExitPoint ? _exits : _others;
        foreach (var io in pool)
        {
            if (io == null || io.Type != type) continue;
            float d = Vector2.Distance(io.transform.position, from);
            if (d < bestD) { bestD = d; best = io; }
        }
        return best;
    }

    public LootContainer NearestKnownCrate(Vector2 from, HashSet<int> skip = null)
    {
        LootContainer best = null; float bestD = float.MaxValue;
        foreach (var box in _crates)
        {
            if (box == null) continue;
            if (skip != null && skip.Contains(box.GetInstanceID())) continue;
            float d = Vector2.Distance(box.transform.position, from);
            if (d < bestD) { bestD = d; best = box; }
        }
        return best;
    }

    /// <summary>안 가본 쪽 방향 — 아는 게 없을 때 "탐색"의 목적지.
    /// 밟은 칸들의 중심에서 **멀어지는** 방향으로 밀되, 길찾기가 알아서 우회한다.</summary>
    public Vector2 UnexploredDirection(Vector2 from)
    {
        if (_walked.Count == 0) return Random.insideUnitCircle.normalized;

        Vector2 center = Vector2.zero;
        foreach (var c in _walked) center += new Vector2(c.x * 4f + 2f, c.y * 4f + 2f);
        center /= _walked.Count;

        Vector2 away = from - center;
        if (away.sqrMagnitude < 1f) away = Random.insideUnitCircle;   // 아직 한 곳에 있음
        return away.normalized;
    }

    public int WalkedCells => _walked.Count;

    // ── 도달 가능 탐색 (BFS) ─────────────────────────────────────────────
    bool[] _bfs;                 // 방문 표시(격자 크기만큼, 씬마다 재사용)
    int _bfsW, _bfsH;
    readonly Queue<int> _bfsQ = new Queue<int>();
    static readonly int[] _dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] _dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

    /// <summary>현재 위치에서 **실제로 걸어갈 수 있는** 미탐색 지점을 찾는다.
    ///
    /// 방향만 잡고 <c>NearestWalkable</c>로 보정하면 <b>벽 반대편의 멀쩡한 빈 칸</b>이 목표가 된다.
    /// 그러면 A*가 매번 실패하고 봇은 그 벽을 바라본 채 제자리를 맴돈다
    /// (2026-07-28: x가 327.7에 고정된 채 60초간 목표 x=333.8 반복, NO_PATH 21회).
    ///
    /// 그래서 격자를 **너비우선으로 실제 전파**해 도달 가능한 칸만 후보로 삼는다.
    /// BFS는 가까운 순서로 퍼지므로, 조건을 만족하는 첫 칸이 곧 "갈 수 있는 가장 가까운 미탐색지"다.</summary>
    public bool TryReachableUnexplored(Vector2 from, out Vector2 dest, float minDist = 7f, int maxVisit = 40000)
    {
        dest = from;
        var grid = NavGrid.Instance;
        if (grid == null || !grid.Ready || grid.Width <= 0) return false;

        int w = grid.Width, h = grid.Height;
        if (_bfs == null || _bfsW != w || _bfsH != h) { _bfs = new bool[w * h]; _bfsW = w; _bfsH = h; }
        else System.Array.Clear(_bfs, 0, _bfs.Length);

        var start = grid.NearestWalkable(grid.WorldToCell(from), 12);
        if (!grid.IsWalkable(start.x, start.y)) return false;

        _bfsQ.Clear();
        int s = start.y * w + start.x;
        _bfs[s] = true;
        _bfsQ.Enqueue(s);

        float minD2 = minDist * minDist;
        int visited = 0;
        Vector2 farthest = from; float farD2 = -1f;
        Vector2 bestUnex = from; float bestUnexD2 = -1f;

        while (_bfsQ.Count > 0 && visited < maxVisit)
        {
            int cur = _bfsQ.Dequeue();
            visited++;
            int cx = cur % w, cy = cur / w;
            Vector2 world = grid.CellToWorld(cx, cy);
            float d2 = (world - from).sqrMagnitude;

            if (d2 > farD2) { farD2 = d2; farthest = world; }

            // 안 밟은 구역 중 **가장 먼 곳**을 고른다.
            //
            // 예전엔 조건을 만족하는 첫 칸에서 즉시 반환했는데, BFS는 가까운 순서로 퍼지므로
            // 늘 "8m 옆의 안 밟은 칸"이 나왔다. 8m 가고 → 그 자리가 밟은 칸이 되고 → 또 8m 옆으로.
            // 결과: 241초에 16m격자 10칸만 밟는 **제자리 기어다니기**(2026-07-28, 상자 40개 중 0개 발견).
            // 사람은 안 가본 '구역'으로 간다 — 그래서 가장 먼 미탐색지를 목적지로 삼는다.
            if (d2 >= minD2 && d2 > bestUnexD2 && !_walked.Contains(Cell(world)))
            { bestUnexD2 = d2; bestUnex = world; }

            for (int i = 0; i < 8; i++)
            {
                int nx = cx + _dx[i], ny = cy + _dy[i];
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int ni = ny * w + nx;
                if (_bfs[ni] || !grid.IsWalkable(nx, ny)) continue;
                _bfs[ni] = true;
                _bfsQ.Enqueue(ni);
            }
        }

        if (bestUnexD2 >= minD2) { dest = bestUnex; return true; }

        // 미탐색지가 없다 = 갈 수 있는 데는 다 가봤다. 그래도 가장 먼 도달점은 준다.
        if (farD2 >= minD2) { dest = farthest; return true; }
        return false;
    }

    /// <summary>안 가본 쪽의 **실제로 갈 수 있는** 목표 지점.
    ///
    /// 방향만 잡아 `현재위치 + 방향×거리`를 목표로 쓰면 **벽 속·맵 밖 좌표**가 나온다.
    /// 2026-07-28 QA: 20×17m짜리 실내(Int_Generic)에서 14m를 밀어 (21.1,17.1) —
    /// 맵 밖 — 을 목표로 잡고 벽에 박힌 채 멈춰 있었다. 그건 AI가 아니다.
    ///
    /// 그래서 ① 씬 크기에 맞춰 거리를 줄이고 ② 길찾기 격자에서 **걸을 수 있는 칸**으로 보정한다.</summary>
    public Vector2 UnexploredTarget(Vector2 from, float preferDist = 14f)
    {
        var grid = NavGrid.Instance;
        Vector2 dir = UnexploredDirection(from);

        // 씬이 좁으면 목표도 가까워야 한다 — 격자 크기가 곧 맵 크기다.
        float dist = preferDist;
        if (grid != null && grid.Ready)
        {
            float halfSpan = Mathf.Min(grid.Width, grid.Height) * grid.CellSize * 0.5f;
            dist = Mathf.Clamp(preferDist, 2.5f, Mathf.Max(3f, halfSpan * 0.8f));
        }

        Vector2 want = from + dir * dist;
        if (grid == null || !grid.Ready) return want;

        // 격자 안으로 넣고, 막힌 칸이면 가장 가까운 걸을 수 있는 칸으로 보정
        var cell = grid.WorldToCell(want);
        cell.x = Mathf.Clamp(cell.x, 0, Mathf.Max(0, grid.Width - 1));
        cell.y = Mathf.Clamp(cell.y, 0, Mathf.Max(0, grid.Height - 1));
        cell = grid.NearestWalkable(cell, 14);

        if (!grid.IsWalkable(cell.x, cell.y))
        {
            // 그 방향은 통째로 막혔다 — 아무 데나가 아니라 '걸을 수 있는 먼 칸'을 찾는다
            var alt = FarWalkable(grid, from);
            if (alt.HasValue) return alt.Value;
            return from;   // 갈 곳이 없다(= 진짜로 갇힘). 호출부가 STUCK으로 잡는다
        }
        return grid.CellToWorld(cell.x, cell.y);
    }

    /// <summary>격자에서 지금 위치와 먼, 걸을 수 있는 칸 하나(성긴 표본).</summary>
    static Vector2? FarWalkable(NavGrid grid, Vector2 from)
    {
        Vector2? best = null; float bestD = 0f;
        int stepX = Mathf.Max(1, grid.Width / 24);
        int stepY = Mathf.Max(1, grid.Height / 24);
        for (int x = 0; x < grid.Width; x += stepX)
            for (int y = 0; y < grid.Height; y += stepY)
            {
                if (!grid.IsWalkable(x, y)) continue;
                Vector2 w = grid.CellToWorld(x, y);
                float d = Vector2.SqrMagnitude(w - from);
                if (d > bestD) { bestD = d; best = w; }
            }
        return best;
    }
}
