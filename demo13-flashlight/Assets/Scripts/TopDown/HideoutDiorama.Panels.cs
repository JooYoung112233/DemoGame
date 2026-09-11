using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기존 전체화면 UI를 도크로 데려오기 위한 **탐지 도우미**와 시설 설명 문구.
///
/// 각 UI가 구조가 제각각이라 개별 접근자를 만들 수 없어서, 열기 직전 화면을 찍어 두고
/// 새로 켜진 패널을 찾아내는 방식으로 잡는다.
/// </summary>
public partial class HideoutDiorama
{
    // ── 기존 UI 입양 도우미 ─────────────────────────────────────────
    /// <summary>현재 켜져 있는 UI 패널들(= 캔버스의 활성 자식).
    /// ⚠️ 캔버스 자체를 세면 안 된다 — 이 프로젝트 UI는 캔버스를 부팅 때 만들어두고
    /// **자식 패널만 켜고 끄기** 때문에 캔버스 목록은 변하지 않는다.</summary>
    public static List<RectTransform> ActivePanels()
    {
        var list = new List<RectTransform>();
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!c.isActiveAndEnabled) continue;
            if (c.GetComponentInParent<HideoutDockPanel>() != null) continue;   // 우리 것은 제외
            for (int i = 0; i < c.transform.childCount; i++)
            {
                var rt = c.transform.GetChild(i) as RectTransform;
                if (rt != null && rt.gameObject.activeInHierarchy) list.Add(rt);
            }
        }
        return list;
    }

    /// <summary>직전 스냅샷에 없던 = 방금 켜진 패널을 고른다.
    /// ⚠️ UI 하나가 패널을 **여러 개** 켜는 경우가 있다(파견은 딤 오버레이 + 본체).
    /// 첫 번째를 잡으면 딤을 입양해 화면이 회색으로 덮인다. 그래서 **맨 위(마지막)**를
    /// 본체로 보고, 같이 켜진 나머지(딤 등)는 꺼버린다 — 도크 안에서는 전체화면 딤이
    /// 오히려 캐릭터를 가린다.</summary>
    public static RectTransform FindNewPanel(List<RectTransform> before)
    {
        var opened = new List<RectTransform>();
        foreach (var rt in ActivePanels())
            if (!before.Contains(rt)) opened.Add(rt);
        if (opened.Count == 0) return null;

        var main = opened[opened.Count - 1];
        for (int i = 0; i < opened.Count - 1; i++) opened[i].gameObject.SetActive(false);
        return main;
    }

    /// <summary>시설 한 줄 설명 — 도킹 패널 본문.</summary>
    static string DescriptionFor(string key) => key switch
    {
        "bed"       => "잠을 자 체력을 회복하고 시간을 넘긴다.",
        "workbench" => "무기·장비를 만들고 수리한다.",
        "stash"     => "가져온 것을 보관한다. 레이드에 들고 나가지 않는 짐.",
        "radio"     => "바깥 소식을 듣는다. 전력이 필요하다.",
        "cooking"   => "재료로 음식을 만든다. 버프가 붙는다.",
        "medical"   => "붕대·진통제 같은 일회용 치료품을 만든다.",
        "dispatch"  => "사람을 내보낸다. 돌아올 때까지 시간이 걸린다.",
        "generator" => "전력을 켜고 끈다. 라디오·파견의 전제.",
        _           => "",
    };

}
