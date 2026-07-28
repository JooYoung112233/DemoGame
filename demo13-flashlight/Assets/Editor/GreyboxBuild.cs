#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 그레이박스 '조각' 빌더 공유 유틸. (ScrapMarket=튜토는 자체 빌더 / 신규 조각은 이걸 공유)
///
/// 핵심: **건물 = 벽 링(4벽) + 문 갭** (gb_door). 튜토의 '솔리드 채워 채널 파기'와 달리,
///   조각은 명시적 건물 링 + 그 사이 열린 길 → 간단·정확(복잡한 여집합 타일링 불필요).
///
/// 사용 패턴:
///   var map = GreyboxBuild.BeginScene(out var scene);   // 빈 씬 + Map 루트 + 팔레트 자동보장
///   int n = 0;
///   n += GreyboxBuild.Floor(map,"Floor", cx,cy,w,h);
///   n += GreyboxBuild.WallSeg(map,"Wall_S", x0,y0,x1,y1);          // 사각형 영역 벽
///   n += GreyboxBuild.Building(map,"Pharmacy", 18,6,30,22,'W',13); // 링+문(서벽 Y13에 2m 문)
///   n += GreyboxBuild.Marker(map,"gb_crate","Box", 22,10);
///   GreyboxBuild.EndScene(scene, "Assets/Scenes/Pharmacy_GB.unity", n, "약국 조각");
///
/// 좌표 = XY평면 Z0, 1unit=1m. 인스턴스는 PrefabUtility.InstantiatePrefab으로 gb_* 링크 유지.
/// </summary>
public static class GreyboxBuild
{
    public const string PrefabRoot = "Props2D/Prefabs/";

    // 조각이 쓰는 핵심 gb_* (하나라도 없으면 팔레트 자동 생성).
    static readonly string[] Core =
    {
        "gb_floor", "gb_wall", "gb_barricade", "gb_door", "gb_crate",
        "gb_shelf", "gb_note", "gb_spawn", "gb_exit", "gb_enter", "gb_enemy",
    };

    // ── 씬 시작/종료 ──────────────────────────────────────────────────────
    static Scene _prevActive;   // BeginScene→EndScene 간 복구용(빌더는 순차 실행)

    public static GameObject BeginScene(out Scene scene)
    {
        scene = EditorSceneBuildUtil.NewDetachedScene(out _prevActive);  // 현재 씬 유지(폴더에만 생성)
        EnsurePalette();
        return new GameObject("Map");
    }

    public static void EndScene(Scene scene, string path, int placed, string label)
    {
        Selection.activeObject = null;
        bool saved = EditorSceneBuildUtil.SaveAndClose(scene, path, _prevActive);  // 저장 후 닫기(현재 씬 유지)
        AssetDatabase.SaveAssets();
        if (saved)
            Debug.Log($"<color=cyan>[Greybox]</color> {label} 생성 완료: {path} — 그레이박스 {placed}개.");
        else
            Debug.LogError($"[Greybox] 씬 저장 실패: {path}");
        if (saved && !Application.isBatchMode)
            EditorUtility.DisplayDialog("Greybox 조각", $"{label} 생성 완료.\n{path}\n그레이박스 {placed}개 배치.\n\nSystems 씬 additive로 Play.", "확인");
    }

    /// <summary>핵심 gb_* 중 하나라도 없으면 팔레트 전체 자동 생성(별도 메뉴 불필요).</summary>
    public static void EnsurePalette()
    {
        foreach (var id in Core)
        {
            if (Resources.Load<GameObject>(PrefabRoot + id) == null)
            {
                Debug.Log($"<color=cyan>[Greybox]</color> 프리팹 '{id}' 없음 → 팔레트 자동 생성(GreyboxPaletteBuilder.Generate).");
                GreyboxPaletteBuilder.Generate();
                return;
            }
        }
    }

    // ── 배치 헬퍼 ─────────────────────────────────────────────────────────
    /// <summary>바닥: gb_floor 전체 스케일.</summary>
    public static int Floor(GameObject p, string name, float cx, float cy, float w, float h)
    {
        var go = Spawn("gb_floor", name, p); if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(w, h, 1f);
        CounterScaleLabel(go); return 1;
    }

    /// <summary>벽 막대: 중심+크기.</summary>
    public static int Wall(GameObject p, string name, float cx, float cy, float lenX, float thickY)
        => Bar("gb_wall", p, name, cx, cy, lenX, thickY);

    public static int Barricade(GameObject p, string name, float cx, float cy, float lenX, float thickY)
        => Bar("gb_barricade", p, name, cx, cy, lenX, thickY);

    /// <summary>벽 막대(회전): 중심+크기+각도(도). 콜라이더도 함께 회전.</summary>
    public static int Wall(GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg)
        => Bar("gb_wall", p, name, cx, cy, lenX, thickY, angleDeg);

    public static int Barricade(GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg)
        => Bar("gb_barricade", p, name, cx, cy, lenX, thickY, angleDeg);

    /// <summary>두 점 A→B를 잇는 벽(자동 회전·길이). 대각/각진 벽의 기본 프리미티브.</summary>
    public static int WallLine(GameObject p, string name, float ax, float ay, float bx, float by, float thick = 1f)
        => Line("gb_wall", p, name, ax, ay, bx, by, thick);

    /// <summary>두 점 A→B를 잇는 바리케이드(자동 회전·길이).</summary>
    public static int BarricadeLine(GameObject p, string name, float ax, float ay, float bx, float by, float thick = 1f)
        => Line("gb_barricade", p, name, ax, ay, bx, by, thick);

    static int Line(string prefabId, GameObject p, string name, float ax, float ay, float bx, float by, float thick)
    {
        float dx = bx - ax, dy = by - ay;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return 0;
        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        return Bar(prefabId, p, name, (ax + bx) * 0.5f, (ay + by) * 0.5f, len, thick, ang);
    }

    /// <summary>사각형 영역 [ax,bx]×[ay,by]를 채우는 gb_wall(좌표→중심·크기). 길이 0이하면 스킵.</summary>
    public static int WallSeg(GameObject p, string name, float ax, float ay, float bx, float by)
    {
        if (bx - ax <= 0.001f || by - ay <= 0.001f) return 0;
        return Wall(p, name, (ax + bx) * 0.5f, (ay + by) * 0.5f, bx - ax, by - ay);
    }

    static int Bar(string prefabId, GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg = 0f)
    {
        var go = Spawn(prefabId, name, p); if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go, angleDeg); return 1;
    }

    /// <summary>마커/오브젝트(스케일 1).</summary>
    public static int Marker(GameObject p, string prefabId, string name, float x, float y)
    {
        var go = Spawn(prefabId, name, p); if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f); return 1;
    }

    /// <summary>쪽지(gb_note) + 내용 주입(읽으면 NoteUI 전체화면).</summary>
    public static int Note(GameObject p, string name, float x, float y, string title, string content)
    {
        var go = Spawn("gb_note", name, p); if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null) io.SetNote(content, title, "읽기");
        return 1;
    }

    /// <summary>
    /// 폐쇄 '건물' = [x0,y0]~[x1,y1] 둘레 4벽(두께 1) + 한 면에 2m 문 갭(gb_door).
    /// side: 'S'/'N'/'W'/'E', doorAt = 그 면을 따라 문 시작 좌표(문 = doorAt~doorAt+2). 내부는 열림.
    /// </summary>
    public static int Building(GameObject p, string name, float x0, float y0, float x1, float y1,
                               char side, float doorAt, string doorPrefab = "gb_door", string doorName = null)
    {
        int n = 0; const float t = 1f, gap = 2f;
        // 남(y0..y0+t)
        if (side == 'S') { n += WallSeg(p, name+"_wSa", x0, y0, doorAt, y0+t); n += WallSeg(p, name+"_wSb", doorAt+gap, y0, x1, y0+t); }
        else             n += WallSeg(p, name+"_wS",  x0, y0, x1, y0+t);
        // 북(y1-t..y1)
        if (side == 'N') { n += WallSeg(p, name+"_wNa", x0, y1-t, doorAt, y1); n += WallSeg(p, name+"_wNb", doorAt+gap, y1-t, x1, y1); }
        else             n += WallSeg(p, name+"_wN",  x0, y1-t, x1, y1);
        // 서(x0..x0+t)
        if (side == 'W') { n += WallSeg(p, name+"_wWa", x0, y0, x0+t, doorAt); n += WallSeg(p, name+"_wWb", x0, doorAt+gap, x0+t, y1); }
        else             n += WallSeg(p, name+"_wW",  x0, y0, x0+t, y1);
        // 동(x1-t..x1)
        if (side == 'E') { n += WallSeg(p, name+"_wEa", x1-t, y0, x1, doorAt); n += WallSeg(p, name+"_wEb", x1-t, doorAt+gap, x1, y1); }
        else             n += WallSeg(p, name+"_wE",  x1-t, y0, x1, y1);
        // 문(갭 중앙)
        float dx, dy;
        switch (side)
        {
            case 'S': dx = doorAt + gap*0.5f; dy = y0 + t*0.5f; break;
            case 'N': dx = doorAt + gap*0.5f; dy = y1 - t*0.5f; break;
            case 'W': dx = x0 + t*0.5f;       dy = doorAt + gap*0.5f; break;
            default:  dx = x1 - t*0.5f;       dy = doorAt + gap*0.5f; break; // E
        }
        n += Marker(p, doorPrefab, doorName ?? name+"_Door", dx, dy);
        return n;
    }

    /// <summary>
    /// 회전 '건물' = 중심(cx,cy) 기준 w×h 사각을 angleDeg 회전한 4벽 링 + 한 면 2m 문.
    /// doorSide = 로컬 'S'(SW→SE)/'E'(SE→NE)/'N'(NE→NW)/'W'(NW→SW). 비-사각(각진) 건물용.
    /// </summary>
    public static int RotBuilding(GameObject p, string name, float cx, float cy, float w, float h,
                                  float angleDeg, char doorSide, float thick = 1f,
                                  string doorPrefab = "gb_door", string doorName = null)
    {
        float hw = w * 0.5f, hh = h * 0.5f;
        float rad = angleDeg * Mathf.Deg2Rad, ca = Mathf.Cos(rad), sa = Mathf.Sin(rad);
        // 로컬 코너(SW,SE,NE,NW) → 월드
        float[] lx = { -hw, hw, hw, -hw };
        float[] ly = { -hh, -hh, hh, hh };
        var wx = new float[4];
        var wy = new float[4];
        for (int i = 0; i < 4; i++)
        {
            wx[i] = cx + lx[i] * ca - ly[i] * sa;
            wy[i] = cy + lx[i] * sa + ly[i] * ca;
        }
        int n = 0;
        n += Edge(p, name + "_S", wx[0], wy[0], wx[1], wy[1], thick, doorSide == 'S', doorPrefab, doorName);
        n += Edge(p, name + "_E", wx[1], wy[1], wx[2], wy[2], thick, doorSide == 'E', doorPrefab, doorName);
        n += Edge(p, name + "_N", wx[2], wy[2], wx[3], wy[3], thick, doorSide == 'N', doorPrefab, doorName);
        n += Edge(p, name + "_W", wx[3], wy[3], wx[0], wy[0], thick, doorSide == 'W', doorPrefab, doorName);
        return n;
    }

    /// <summary>벽 한 변 A→B. hasDoor면 변 중앙에 2m 갭 + 문 마커(회전 자동).</summary>
    static int Edge(GameObject p, string name, float ax, float ay, float bx, float by, float thick,
                    bool hasDoor, string doorPrefab, string doorName)
    {
        if (!hasDoor) return Line("gb_wall", p, name, ax, ay, bx, by, thick);
        const float gap = 2f;
        float dx = bx - ax, dy = by - ay;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < gap + 0.5f) return Line("gb_wall", p, name, ax, ay, bx, by, thick); // 너무 짧아 문 생략
        float ux = dx / len, uy = dy / len;
        float dc = len * 0.5f, s0 = dc - gap * 0.5f, s1 = dc + gap * 0.5f;
        int n = 0;
        n += Line("gb_wall", p, name + "_a", ax, ay, ax + ux * s0, ay + uy * s0, thick);
        n += Line("gb_wall", p, name + "_b", ax + ux * s1, ay + uy * s1, bx, by, thick);
        n += Marker(p, doorPrefab ?? "gb_door", doorName ?? name + "_Door", ax + ux * dc, ay + uy * dc);
        return n;
    }

    // ── 내부 ──────────────────────────────────────────────────────────────
    static GameObject Spawn(string prefabId, string name, GameObject parent)
    {
        var prefab = Resources.Load<GameObject>(PrefabRoot + prefabId);
        if (prefab == null)
        {
            Debug.LogError($"[Greybox] 프리팹 로드 실패: Resources/{PrefabRoot}{prefabId} (Generate Greybox Palette 필요)");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.name = name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go;
    }

    /// <summary>배치된 gb_* 마커의 머리 글씨를 바꾼다(팔레트 프리팹 라벨 그대로가 부정확할 때).
    /// 예: 내부 씬의 gb_exit는 프리팹상 "탈출"이지만 실제로는 '건물 밖으로'라 "나가기"가 맞다.</summary>
    public static void Relabel(Transform marker, string text)
    {
        if (marker == null || string.IsNullOrEmpty(text)) return;
        var label = marker.Find("Label");
        if (label == null) return;
        var tm = label.GetComponent<TextMesh>();
        if (tm != null) tm.text = text;
    }

    static void CounterScaleLabel(GameObject go, float angleDeg = 0f)
    {
        var label = go.transform.Find("Label");
        if (label == null) return;
        var s = go.transform.localScale;
        float ix = Mathf.Approximately(s.x, 0f) ? 1f : 1f / s.x;
        float iy = Mathf.Approximately(s.y, 0f) ? 1f : 1f / s.y;
        label.localScale = new Vector3(ix, iy, 1f);
        if (!Mathf.Approximately(angleDeg, 0f))
            label.localRotation = Quaternion.Euler(0f, 0f, -angleDeg); // 라벨은 수평 유지
    }
}
#endif
