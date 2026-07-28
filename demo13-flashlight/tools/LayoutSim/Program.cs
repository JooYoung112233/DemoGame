using System;
using System.Collections.Generic;
using System.Linq;

static class Program
{
    const float X0 = 8f, Y0 = 8f, X1 = 168f, Y1 = 176f;
    const float Cell = 0.25f;
    static int W, H;

    static void Main(string[] argv)
    {
        Zone1GreyboxLayout.Build();

        Console.WriteLine("=== 경고 ===");
        foreach (var w in Rec.Warns) Console.WriteLine("  ! " + w);
        if (Rec.Warns.Count == 0) Console.WriteLine("  (없음)");
        Console.WriteLine();

        var solids = Rec.Solids.Where(b => b.kind != "tutorial").ToList();
        Console.WriteLine($"=== 배치 === 솔리드 {solids.Count} / 마커 {Rec.Points.Count}");
        foreach (var g in Rec.Points.GroupBy(p => p.kind).OrderByDescending(g => g.Count()))
            Console.WriteLine($"  {g.Key,-12} {g.Count()}");
        Console.WriteLine();

        // 랜드마크·유니크 건물이 전부 살아 있나. 이름은 접두어로 찾는다 —
        //   덩어리가 MaxSpan을 넘어 쪼개지면 문 이름이 "Pharmacy_00_Door"처럼 바뀐다.
        string[] must = { "Apt", "Tower", "CollapsedMall", "Police", "Diner", "Jewelry",
                          "Electronics", "Hardware", "Laundry", "Pharmacy", "DomeCore", "AlleyShop" };
        Console.WriteLine("=== 유니크/랜드마크 문 ===");
        foreach (var m in must)
        {
            var doors = Rec.Points.Where(p => p.kind == "gb_door").ToList();
            var hit = doors.FirstOrDefault(p => p.name == m + "_Door");
            // 쪼개진 덩어리는 "Pharmacy_00_Door" 꼴이 된다.
            if (hit.name == null)
                hit = doors.FirstOrDefault(p => p.name.StartsWith(m + "_") && p.name.EndsWith("_Door"));
            Console.WriteLine(hit.name == null ? $"  MISSING  {m}" : $"  ok  {hit.name,-22} ({hit.x:F1},{hit.y:F1})");
        }
        Console.WriteLine();

        foreach (float radius in new[] { 0.35f, 0.4f })
        {
            Console.WriteLine($"=== 연결성 (플레이어 반경 {radius}m) ===");
            var blocked = Raster(solids, radius);
            var seed = Rec.Points.First(p => p.kind == "gb_spawn");
            var seen = Flood(blocked, seed.x, seed.y, out int openCells, out int reached);
            Console.WriteLine($"  스폰 {seed.name} 에서 도달 {reached}/{openCells} 셀 ({100.0 * reached / openCells:F1}%)");

            var bad = new List<string>();
            foreach (var p in Rec.Points)
            {
                if (p.kind == "tutorial") continue;
                if (!NearReachable(seen, blocked, p.x, p.y)) bad.Add($"{p.kind}:{p.name} ({p.x:F1},{p.y:F1})");
            }
            Console.WriteLine($"  도달 불가 오브젝트 {bad.Count}개");
            foreach (var b in bad.Take(40)) Console.WriteLine("    X " + b);
            Console.WriteLine();
        }

        // 스폰 5곳이 서로 통하나(레이드 스폰이 어디든 탈출까지 갈 수 있어야 한다)
        Console.WriteLine("=== 스폰 5 ↔ 탈출 5 ===");
        {
            var blocked = Raster(solids, 0.35f);
            foreach (var sp in Rec.Points.Where(p => p.kind == "gb_spawn"))
            {
                var seen = Flood(blocked, sp.x, sp.y, out _, out _);
                var reach = Rec.Points.Where(p => p.kind == "gb_exit")
                                      .Count(p => NearReachable(seen, blocked, p.x, p.y));
                Console.WriteLine($"  {sp.name,-8} → 탈출 {reach}/5");
            }
        }
        Console.WriteLine();

        Console.WriteLine("=== 안뜰(블록 정체성) ===");
        foreach (var p in Rec.Points.Where(p => p.kind == "gb_note" && p.name.Contains("_Yard")))
            Console.WriteLine($"  {p.name}  ({p.x:F0},{p.y:F0})");
        Console.WriteLine();

        // 특정 구간에 뭐가 깔렸는지 보고 싶을 때: LAYOUTSIM_PROBE="x0,y0,x1,y1" dotnet run
        var probe = Environment.GetEnvironmentVariable("LAYOUTSIM_PROBE");
        if (!string.IsNullOrEmpty(probe))
        {
            var v = probe.Split(',').Select(float.Parse).ToArray();
            Probe.Dump(v[0], v[1], v[2], v[3]);
        }

        Console.WriteLine("=== 지도 (1문자 = 가로 1m / 세로 2m, 남쪽이 아래) ===");
        Ascii(solids);
    }

    static bool[] Raster(List<Rec.Box> solids, float pad)
    {
        W = (int)((X1 - X0) / Cell); H = (int)((Y1 - Y0) / Cell);
        var b = new bool[W * H];
        foreach (var s in solids)
        {
            int i0 = Math.Max(0, (int)((s.x0 - pad - X0) / Cell));
            int i1 = Math.Min(W - 1, (int)((s.x1 + pad - X0) / Cell));
            int j0 = Math.Max(0, (int)((s.y0 - pad - Y0) / Cell));
            int j1 = Math.Min(H - 1, (int)((s.y1 + pad - Y0) / Cell));
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                    b[j * W + i] = true;
        }
        return b;
    }

    static bool[] Flood(bool[] blocked, float sx, float sy, out int openCells, out int reached)
    {
        openCells = 0;
        for (int k = 0; k < blocked.Length; k++) if (!blocked[k]) openCells++;
        var seen = new bool[blocked.Length];
        int si = (int)((sx - X0) / Cell), sj = (int)((sy - Y0) / Cell);
        var q = new Queue<int>();
        int start = sj * W + si;
        if (blocked[start]) { start = NearestOpen(blocked, si, sj); }
        if (start < 0) { reached = 0; return seen; }
        seen[start] = true; q.Enqueue(start);
        reached = 1;
        while (q.Count > 0)
        {
            int c = q.Dequeue();
            int ci = c % W, cj = c / W;
            for (int d = 0; d < 4; d++)
            {
                int ni = ci + (d == 0 ? 1 : d == 1 ? -1 : 0);
                int nj = cj + (d == 2 ? 1 : d == 3 ? -1 : 0);
                if (ni < 0 || nj < 0 || ni >= W || nj >= H) continue;
                int nk = nj * W + ni;
                if (seen[nk] || blocked[nk]) continue;
                seen[nk] = true; reached++; q.Enqueue(nk);
            }
        }
        return seen;
    }

    static int NearestOpen(bool[] blocked, int si, int sj)
    {
        for (int r = 1; r < 40; r++)
            for (int dj = -r; dj <= r; dj++)
                for (int di = -r; di <= r; di++)
                {
                    int i = si + di, j = sj + dj;
                    if (i < 0 || j < 0 || i >= W || j >= H) continue;
                    if (!blocked[j * W + i]) return j * W + i;
                }
        return -1;
    }

    /// <summary>오브젝트 자체는 솔리드 위일 수 있으니(문은 벽면), 반경 2.2m 안에 도달 가능한 칸이 있으면 OK.</summary>
    static bool NearReachable(bool[] seen, bool[] blocked, float x, float y)
    {
        int si = (int)((x - X0) / Cell), sj = (int)((y - Y0) / Cell);
        int r = (int)(2.2f / Cell);
        for (int dj = -r; dj <= r; dj++)
            for (int di = -r; di <= r; di++)
            {
                int i = si + di, j = sj + dj;
                if (i < 0 || j < 0 || i >= W || j >= H) continue;
                if (seen[j * W + i]) return true;
            }
        return false;
    }

    static void Ascii(List<Rec.Box> solids)
    {
        int cw = 160, ch = 84;                     // 1m x 2m
        var g = new char[ch, cw];
        for (int j = 0; j < ch; j++) for (int i = 0; i < cw; i++) g[j, i] = ' ';
        foreach (var s in solids)
        {
            if (s.name.StartsWith("RB_") || s.name.StartsWith("SP_") || s.name.StartsWith("MD_")) continue;
            char c = s.kind == "car" ? (char)111 : s.kind == "prop" ? (char)44 : s.kind == "barricade" ? (char)88 : (char)35;
            for (float x = s.x0; x <= s.x1; x += 0.4f)
                for (float y = s.y0; y <= s.y1; y += 0.4f)
                {
                    int i = (int)(x - X0), j = (int)((y - Y0) / 2f);
                    if (i < 0 || j < 0 || i >= cw || j >= ch) continue;
                    if (g[j, i] == ' ' || c == '#') g[j, i] = c;
                }
        }
        foreach (var p in Rec.Points)
        {
            char c = p.kind == "gb_door" ? 'D' : p.kind == "gb_crate" ? 'c'
                   : p.kind == "gb_spawn" ? 'S' : p.kind == "gb_exit" ? 'E'
                   : p.kind == "gb_note" ? 'n' : p.kind == "enemyzone" ? '@' : '?';
            if (c == '?') continue;
            int i = (int)(p.x - X0), j = (int)((p.y - Y0) / 2f);
            if (i < 0 || j < 0 || i >= cw || j >= ch) continue;
            g[j, i] = c;
        }
        for (int j = ch - 1; j >= 0; j--)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < cw; i++) sb.Append(g[j, i]);
            Console.WriteLine(sb.ToString().TrimEnd());
        }
    }
}

static class Probe
{
    public static void Dump(float x0, float y0, float x1, float y1)
    {
        Console.WriteLine($"--- 솔리드 겹침 [{x0},{y0}]~[{x1},{y1}] ---");
        foreach (var s in Rec.Solids)
            if (s.x0 < x1 && x0 < s.x1 && s.y0 < y1 && y0 < s.y1)
                Console.WriteLine($"  {s.kind,-10} {s.name,-24} [{s.x0:F1},{s.y0:F1}]~[{s.x1:F1},{s.y1:F1}]");
    }
}
