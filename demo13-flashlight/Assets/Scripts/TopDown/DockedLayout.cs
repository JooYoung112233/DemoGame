using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전체화면 전제로 만들어진 시설 UI를 **좁은 도크 폭에 맞게 재배치**한다.
///
/// 이 프로젝트 UI는 전부 1920×1080 기준이라 도크에 그냥 넣으면 가로로 터진다.
/// 실측(2026-09-08):
/// <list type="bullet">
/// <item>작업대 `CraftRoot` 1920 → 진짜 내용은 `CenterPanel` 520×480 (껍데기만 전체화면)</item>
/// <item>침대 `Panel` 1920 → `Window` 720×460</item>
/// <item>파견 `Body` 1840 = `LeftPanel` 360(조작부) + `MapArea` 1464(지도)</item>
/// <item>라디오 `Root` 1920 = Header + TuneBar + Body(LeftPanel 540 + RightPanel 1276)</item>
/// <item>창고 `PanelRoot` 1920 = 643 + 556 + 556 3열</item>
/// </list>
///
/// 그래서 네 단계로 손본다.
/// <list type="number">
/// <item><b>내려가기</b> — 전체화면 껍데기를 지나 진짜 내용까지. 작업대·침대는 이것만으로 끝난다.</item>
/// <item><b>덜어내기</b> — 도크에서 자리만 잡아먹는 것(파견 지도, 패널 자체 닫기 버튼)을 감춘다.</item>
/// <item><b>세로로 쌓기</b> — 도크보다 넓은 노드는 자식을 한 줄로 쌓는다(웹의 다단→단일 컬럼과 같다).
///       그래도 안 줄어드는 것(고정 크기 그리드 등)만 마지막에 균일 축소한다.</item>
/// <item><b>스크롤</b> — 쌓고 나서 세로로 넘치면 스크롤로 감싼다.</item>
/// </list>
///
/// ⚠️ <b>남의 패널을 건드리므로 바꾼 것은 전부 되돌려야 한다.</b> 안 되돌리면 은신처 밖
/// (레이드 중 인벤토리 등)에서 그 UI가 찌그러진 채로 뜬다. <see cref="Session"/>이 바꾼 값을
/// 들고 있다가 <see cref="Session.Restore"/>에서 원래대로 돌린다.
///
/// 설계: docs/hideout-3d.md
/// </summary>
public static partial class DockedLayout
{
    /// <summary>쌓을 때 항목 사이 간격(px, 1920 기준).</summary>
    const float Gap = 12f;

    /// <summary>줄일 때의 하한. 이보다 작아지면 글씨를 못 읽는다 — 그럴 바엔 스크롤이 낫다.</summary>
    static float MinScale => GameTuning.Instance != null ? GameTuning.Instance.hideoutDockMinScale : 0.72f;

    /// <summary>이 배수까지는 스크롤 대신 살짝 줄여 한 화면에 담는다(하한 축소율 ≈ 0.87).</summary>
    const float SquashLimit = 1.15f;   // 이 배수까지는 스크롤 대신 축소


    /// <summary>쌓기 재귀 깊이 한계. 이 아래로는 축소로 처리한다.</summary>
    const int MaxDepth = 3;

    // ─────────────────────────────────────────────────────────────
    // 본체
    // ─────────────────────────────────────────────────────────────

    /// <summary><paramref name="root"/>(방금 켜진 패널)을 <paramref name="area"/> 크기의
    /// 도크에 들어가도록 재배치한다. 결과는 <see cref="Session.Placed"/>.</summary>
    public static Session Prepare(RectTransform root, string moduleKey, Vector2 area)
    {
        var s = new Session { Root = root };
        if (root == null || area.x <= 1f) { s.Placed = root; return s; }

        var hide = HideFor(moduleKey);
        if (hide != null)
            foreach (var name in hide) HideByName(root, name, s);

        var node = Descend(root, area.x, s);

        // 패널이 스스로 단 제목 줄은 도크 제목과 중복이다 — 지우고 아래 내용을 끌어올린다.
        StripHeader(node, HeaderFor(moduleKey), s);


        Fit(node, node.rect.width, area.x, s, 0);

        s.Record(node);                       // 도크로 옮기기 전 원래 자리를 기억
        // 도크보다 훨씬 좁으면 도크 폭까지 넓힌다. 세로로 긴 폼(파견 조작부 360×964)은
        // 높이 때문에 축소가 걸리는데, 미리 넓혀두면 같은 축소율에서도 훨씬 크게 보인다.
        // stretch 앵커를 쓰는 자식은 따라 넓어지고, 고정 폭 자식은 그대로 남는다(빈자리만 생긴다).
        // ⚠️ 세로가 넘치는 것에만 적용한다. 높이에 여유가 있는 패널(제작 520×480)은
        //    넓히는 대신 통째로 키우는 편이 낫다 — 내부 2열 배치를 흔들지 않는다.
        //    창고 격자(643)처럼 이미 도크 폭의 8할을 쓰는 것도 건드리면 안 된다 — 격자 칸이
        //    고정 크기라 늘려봐야 오른쪽에 빈자리만 생긴다. 확실히 좁은 것(7할 미만)만 넓힌다.
        if (node.rect.height > area.y && node.rect.width < area.x * 0.7f)
        {
            s.Record(node);
            SetBox(node, area.x, node.rect.height);
        }

        s.Placed = WrapIfTall(node, area, s);
        return s;
    }


}
