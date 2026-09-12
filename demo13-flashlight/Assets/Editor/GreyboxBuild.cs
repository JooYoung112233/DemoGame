#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 그레이박스 '조각' 빌더 공유 유틸 — 모든 프리미티브를 <see cref="Greybox3D"/>로 3D로 짓는다.
///
/// 핵심: **건물 = 벽 링(4벽) + 문 갭** (gb_door). 튜토의 '솔리드 채워 채널 파기'와 달리,
///   조각은 명시적 건물 링 + 그 사이 열린 길 → 간단·정확(복잡한 여집합 타일링 불필요).
///
/// 사용 패턴:
///   var map = GreyboxBuild.BeginScene(out var scene);   // 빈 씬 + Map 루트
///   int n = 0;
///   n += GreyboxBuild.Floor(map,"Floor", cx,cy,w,h);
///   n += GreyboxBuild.WallSeg(map,"Wall_S", x0,y0,x1,y1);          // 사각형 영역 벽
///   n += GreyboxBuild.Building(map,"Pharmacy", 18,6,30,22,'W',13); // 링+문(서벽 Y13에 2m 문)
///   n += GreyboxBuild.Marker(map,"gb_crate","Box", 22,10);
///   GreyboxBuild.EndScene(scene, "Assets/Scenes/Pharmacy_GB.unity", n, "약국 조각");
///
/// 좌표 = 평면(x, y) 1unit=1m. 3D 배치(축 교환·PlanScale)는 Greybox3D가 한다.
///
/// 2026-09-12 시스템 정리 5단계: 2D 경로와 gb_* 팔레트 프리팹(`Resources/Props2D`)을 없앴다.
///   "gb_*" 문자열은 이제 Greybox3D가 모양·높이를 고르는 **종류 키**일 뿐이고 프리팹을 불러오지 않는다.
///   기능 컴포넌트(상자·출구·쪽지·스폰 등)는 전과 같이 각 레이아웃 코드가 직접 붙인다.
/// </summary>
public static class GreyboxBuild
{
    /// <summary>항상 3D. 레이아웃 파일의 `if (GreyboxBuild.Use3D)` 분기를 그대로 두기 위한 호환 속성(2026-09-12 — 2D 경로 삭제).</summary>
    public static bool Use3D => true;

    // ── 씬 시작/종료 ──────────────────────────────────────────────────────
    static Scene _prevActive;   // BeginScene→EndScene 간 복구용(빌더는 순차 실행)

    public static GameObject BeginScene(out Scene scene)
    {
        scene = EditorSceneBuildUtil.NewDetachedScene(out _prevActive);  // 현재 씬 유지(폴더에만 생성)
        return new GameObject("Map");
    }

    public static void EndScene(Scene scene, string path, int placed, string label)
    {
        // 저장 직전에 조명을 얹는다 — 3D는 태양이 없으면 앰비언트만 받아 납작해진다
        // (실제로 지역1·고철시장·실내 전부 광원 0개로 구워져 있었다).
        Lighting3D.Apply(null, Greybox3D.ScenePreset);
        if (Greybox3D.ScenePreset == Lighting3D.Preset.Indoor)
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Map") { Lighting3D.FillCeilingLamps(go, Greybox3D.Heights.building - 0.4f); break; }

        Selection.activeObject = null;
        bool saved = EditorSceneBuildUtil.SaveAndClose(scene, path, _prevActive);  // 저장 후 닫기(현재 씬 유지)
        AssetDatabase.SaveAssets();
        if (saved)
            Debug.Log($"<color=cyan>[Greybox]</color> {label} 생성 완료: {path} — 그레이박스 {placed}개.");
        else
            Debug.LogError($"[Greybox] 씬 저장 실패: {path}");
        // ⚠️ ContentBuildAll.Quiet을 함께 봐야 한다. 이걸 빠뜨려서 일괄 빌드가 씬마다 모달에
        //    걸려 멈췄다(자동화에는 누를 사람이 없다). 다른 빌더는 전부 Quiet을 본다.
        if (saved && !Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Greybox 조각", $"{label} 생성 완료.\n{path}\n그레이박스 {placed}개 배치.\n\nSystems 씬 additive로 Play.", "확인");
    }

    // ── 배치 헬퍼 ─────────────────────────────────────────────────────────
    /// <summary>바닥 판.</summary>
    public static int Floor(GameObject p, string name, float cx, float cy, float w, float h)
        => Greybox3D.Floor(p, name, cx, cy, w, h);

    /// <summary>벽 막대: 중심+크기.</summary>
    public static int Wall(GameObject p, string name, float cx, float cy, float lenX, float thickY)
        => Bar("gb_wall", p, name, cx, cy, lenX, thickY);

    public static int Barricade(GameObject p, string name, float cx, float cy, float lenX, float thickY)
        => Bar("gb_barricade", p, name, cx, cy, lenX, thickY);

    /// <summary>차량(도로 엄폐물) — **벽(회색)·막힘(주황)과 색이 다르다.** 통과 불가지만 인터랙션은 없다.</summary>
    public static int Car(GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg = 0f)
        => Bar("gb_car", p, name, cx, cy, lenX, thickY, angleDeg);

    /// <summary>잡프랍(자판기·드럼통·쓰레기통…) — 작은 엄폐 + 길의 표정.</summary>
    public static int Prop(GameObject p, string name, float cx, float cy, float sx, float sy)
        => Bar("gb_prop", p, name, cx, cy, sx, sy);

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

    static int Line(string kind, GameObject p, string name, float ax, float ay, float bx, float by, float thick)
    {
        float dx = bx - ax, dy = by - ay;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return 0;
        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        return Bar(kind, p, name, (ax + bx) * 0.5f, (ay + by) * 0.5f, len, thick, ang);
    }

    /// <summary>사각형 영역 [ax,bx]×[ay,by]를 채우는 벽(좌표→중심·크기). 길이 0이하면 스킵.</summary>
    public static int WallSeg(GameObject p, string name, float ax, float ay, float bx, float by)
    {
        if (bx - ax <= 0.001f || by - ay <= 0.001f) return 0;
        return Wall(p, name, (ax + bx) * 0.5f, (ay + by) * 0.5f, bx - ax, by - ay);
    }

    static int Bar(string kind, GameObject p, string name, float cx, float cy, float lenX, float thickY, float angleDeg = 0f)
        => Greybox3D.Bar(kind, p, name, cx, cy, lenX, thickY, angleDeg);

    /// <summary>마커/오브젝트(스케일 1). kind = "gb_*" 종류 키.</summary>
    public static int Marker(GameObject p, string kind, string name, float x, float y)
        => Greybox3D.Marker(p, kind, name, x, y);

    /// <summary>빈 앵커(컴포넌트는 호출자가 붙인다). 좌표만 필요한 마커용 —
    /// 빌더가 직접 <c>new GameObject</c>로 만들면 3D 평면 변환(축 교환·PlanScale)을 통째로
    /// 건너뛴다(지역1 바닥 루트 앵커 30개가 실제로 XY평면 z=0에 남아 아이템이 허공에 떨어졌다).</summary>
    public static GameObject Point(GameObject p, string name, float x, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p.transform, false);
        go.transform.localPosition = Greybox3D.Plan(x, y);
        return go;
    }

    /// <summary>평면 크기(폭·깊이) → 배치용 3D 크기. 3D는 맵을 PlanScale배로 굽기 때문에
    /// 영역(적 스폰 존 등)도 같이 키우지 않으면 맵만 커지고 존은 그대로 좁아진다.</summary>
    public static Vector3 PlanSize(float w, float h)
        => new Vector3(w * Greybox3D.PlanScale, 0f, h * Greybox3D.PlanScale);

    /// <summary>쪽지 + 내용 주입(읽으면 NoteUI 전체화면). 그레이박스 상자로 세우고 상호작용을 직접 얹는다.</summary>
    public static int Note(GameObject p, string name, float x, float y, string title, string content)
    {
        if (Greybox3D.Marker(p, "gb_note", name, x, y) == 0) return 0;
        var t3 = p.transform.Find(name);
        if (t3 == null) return 1;
        var io3 = t3.GetComponent<InteractableObject>() ?? t3.gameObject.AddComponent<InteractableObject>();
        io3.SetNote(content, title, "읽기");
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
}
#endif
