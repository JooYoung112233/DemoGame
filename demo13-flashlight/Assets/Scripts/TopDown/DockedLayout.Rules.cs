using UnityEngine;

/// <summary>
/// 시설별 **재배치 규칙** — 도크에서 감출 것, 패널이 스스로 단 제목 줄.
///
/// "창고에 장비창이 같이 나온다", "제목이 두 번 나온다" 같은 것은 전부 여기서 고친다.
/// 새 시설을 붙이거나 패널 구조가 바뀌면 이 표만 손보면 된다.
/// </summary>
public static partial class DockedLayout
{
    // ─────────────────────────────────────────────────────────────
    // 시설별 규칙
    // ─────────────────────────────────────────────────────────────

    /// <summary>도크에서 감출 자식 이름들.
    /// 지도처럼 넓기만 한 것, 도크의 '닫기'와 겹치는 패널 자체 닫기 버튼 등.</summary>
    static string[] HideFor(string moduleKey) => moduleKey switch
    {
        // 지도는 폭 1464를 먹는데, 실제 조작(인력·목적지·보내기)은 전부 왼쪽 폼(360)에 있다.
        "dispatch" => new[] { "MapArea" },
        // 도크에 이미 '닫기'가 있다. 패널 자체 닫기는 중복이고 도크 밖으로 삐져나간다.
        // (라디오의 자체 닫기는 Header 안에 있어 HeaderFor에서 같이 사라진다)
        // 창고는 말 그대로 창고 격자만 있으면 된다. 캐릭터 상태·장비/소지품은
        // Tab 인벤토리에서 보는 것이고, 여기 같이 띄우면 정작 창고가 좁아진다.
        "stash"    => new[] { "CloseBtn", "CharacterPanel", "RightPanel" },
        "bed"      => new[] { "Close" },
        // 제작창 자체 닫기(X)도 도크의 X와 겹친다.
        "workbench" or "cooking" or "medical" => new[] { "CloseBtn", "Close" },
        _          => null,
    };

    /// <summary>패널이 스스로 달고 있는 **제목 줄**. 도크가 좌상단에 시설 이름을 이미 띄우므로
    /// 중복이고, 그 자리만큼 내용이 좁아진다. 여기 적힌 것은 감추고 아래 내용을 끌어올린다.
    ///
    /// ⚠️ 이름 검색은 <b>바로 아래 자식</b>에만 건다. 자손 전체를 뒤지면 목록 항목 안의
    ///    "Title" 같은 것까지 같이 사라진다.</summary>
    static string[] HeaderFor(string moduleKey) => moduleKey switch
    {
        // 제작창: "작업대" + "해금된 레시피 제작 · 수리 탭에서…" 두 줄(y -8~-56).
        "workbench" or "cooking" or "medical" => new[] { "Title", "Desc" },
        // 라디오: Header(64) + 그 아래 구분선.
        "radio" => new[] { "Header", "HeaderDivider" },
        _ => null,
    };

}
