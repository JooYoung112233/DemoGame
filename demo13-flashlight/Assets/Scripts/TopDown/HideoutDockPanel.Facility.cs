using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시설 하나하나의 **내용** — 상태 표시(레벨·비용), 기능 버튼 이름, 버튼이 실제로 여는 UI.
///
/// 시설을 추가하거나 "이 시설을 누르면 무엇이 열리는가"를 바꾸려면 여기만 보면 된다.
/// 제작 3종은 <see cref="CraftingStation"/> enum으로 갈라지고 나머지는 각자의 UI를 쓴다.
/// </summary>
public partial class HideoutDockPanel
{
    /// <summary>시설 상태(레벨·다음 비용)를 패널 안에 직접 보여준다.
    /// 전체화면 HideoutUI로 넘어가지 않고 여기서 건설·업그레이드까지 끝난다.</summary>
    void RefreshModule()
    {
        var mm = HideoutModuleManager.Instance;
        if (string.IsNullOrEmpty(_module) || mm == null)
        {
            _statusText.text = "";
            _upgradeBtn.gameObject.SetActive(false);
            return;
        }

        int lv = mm.GetLevel(_module);
        string state = lv <= 0 ? "<미건설>" : "Lv " + lv + (mm.IsMaxed(_module) ? " (최대)" : "");
        string cost = "";
        var next = mm.NextCost(_module);
        if (next.HasValue)
        {
            cost = "\n다음 단계: 스크랩 " + next.Value.scrap;
            if (next.Value.mats != null)
                foreach (var m in next.Value.mats)
                    cost += "  " + m.id + " ×" + m.qty;
        }
        _statusText.text = state + cost;

        bool can = mm.CanUpgrade(_module, out string reason);
        _upgradeBtn.gameObject.SetActive(!mm.IsMaxed(_module));
        _upgradeBtn.interactable = can;
        var lbl = _upgradeBtn.GetComponentInChildren<Text>();
        if (lbl != null) lbl.text = lv <= 0 ? "건설" : "업그레이드";
        if (!can && !string.IsNullOrEmpty(reason)) _statusText.text += "\n\n" + reason;

        // 기능 버튼 — 건설(Lv≥1)된 시설만 뜬다. 라벨은 시설마다 다르다.
        string useLabel = UseLabelFor(_module);
        bool built = lv >= 1 && !string.IsNullOrEmpty(useLabel);
        _useBtn.gameObject.SetActive(built);
        var ul = _useBtn.GetComponentInChildren<Text>();
        if (ul != null) ul.text = useLabel;
    }

    /// <summary>시설별 기능 버튼 이름. 빈 문자열이면 기능 없음.</summary>
    static string UseLabelFor(string key) => key switch
    {
        "workbench" => "제작·수리",
        "cooking"   => "요리",
        "medical"   => "조제",
        "stash"     => "창고 열기",
        "radio"     => "청취",
        "dispatch"  => "파견",
        "bed"       => "휴식",
        "generator" => "전력 전환",
        _           => "",
    };

    /// <summary>시설 기능 실행 — 레시피·스테이션 배선.
    /// 제작 3종은 <see cref="CraftingStation"/> enum으로 갈라지고, 나머지는 각자의 UI를 쓴다.
    /// 여기서 여는 UI들은 아직 전체화면이라, 열릴 때 도킹으로 입양된다(Adopt).</summary>
    void UseFacility()
    {
        var um = UIManager.Instance;

        // 발전기는 UI가 아니라 토글이다 - 입양할 패널이 없다.
        if (_module == "generator")
        {
            var gm = HideoutModuleManager.Instance;
            if (gm == null) return;
            bool ok = gm.ToggleGeneratorPower(out string why);
            ToastManager.Show(ok ? (gm.GeneratorPowered ? "전력 켬" : "전력 끔") : why,
                              ok ? ToastManager.ToastType.Info : ToastManager.ToastType.Warning);
            RefreshModule();
            return;
        }

        // ⚠️ 여기서 여는 UI는 전부 전체화면 전제라, 그냥 띄우면 캐릭터를 덮는다.
        // 열기 직전 화면을 찍어두고 새로 켜진 패널을 도크 안으로 끌어들인다.
        var before = HideoutDiorama.ActivePanels();

        switch (_module)
        {
            case "workbench": if (um != null) um.ShowCrafting(CraftingStation.Workbench);    break;
            case "cooking":   if (um != null) um.ShowCrafting(CraftingStation.CookingBench); break;
            case "medical":   if (um != null) um.ShowCrafting(CraftingStation.MedicalBench); break;
            case "stash":     if (um != null) um.ShowCharacterPanelWithStash();              break;
            case "radio":     RadioUI.Show();     break;
            case "dispatch":  DispatchUI.Show();  break;
            case "bed":       SleepUI.Show();     break;
            default: return;
        }

        var opened = HideoutDiorama.FindNewPanel(before);
        if (opened != null) Adopt(opened);          // Adopt가 기능 버튼까지 정리한다
    }
}
