using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 남의 패널을 도크 안으로 **끌어들이고 되돌리는** 부분.
///
/// 실제 재배치(껍데기 통과·제목 제거·세로 쌓기·스크롤)는 <see cref="DockedLayout"/>이 하고,
/// 여기서는 그 결과를 도크 자리에 앉히고 도크 자신의 버튼을 정리한다.
/// ⚠️ 되돌리기를 빠뜨리면 은신처 밖에서 그 UI가 찌그러진 채 뜬다.
/// </summary>
public partial class HideoutDockPanel
{
    /// <summary>남의 패널을 도킹 안으로 끌어들인다 — 전체화면 UI를 덮어쓰지 않고 **자리만** 옮긴다.
    /// 각 UI가 구조가 제각각이라 개별 접근자를 만들지 않고, 방금 켜진 Canvas의 내용을 통째로 받는다.</summary>
    public bool Adopt(RectTransform panel)
    {
        if (panel == null || _content == null) return false;
        Release();

        // 도크 크기가 바뀐 직후라 _content의 rect가 아직 갱신 전일 수 있다.
        // 재배치는 폭 하나로 결정되므로, 여기서 강제로 한 번 계산시킨다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        var area = _content.rect.size;

        _session = DockedLayout.Prepare(panel, _module, area);
        var placed = _session.Placed;
        if (placed == null) { _session = null; return false; }

        // ⚠️ 크기는 **옮기기 전에** 읽어야 한다. 남의 패널은 1920 부모 기준 앵커를 갖고 있어서,
        //    도크(808) 밑으로 옮긴 뒤 rect를 읽으면 이미 줄어든 값이 나온다.
        Vector2 nativeSize = placed.rect.size;

        placed.SetParent(_content, false);

        if (_session.KeepsOwnSize)
        {
            // 이미 도크에 맞게 줄여 놨다 — 다시 늘리면 배치가 깨진다.
            placed.anchorMin = placed.anchorMax = placed.pivot = new Vector2(0.5f, 0.5f);
            placed.anchoredPosition = Vector2.zero;
            placed.sizeDelta = nativeSize;
        }
        else
        {
            // 도크 자리를 **꽉 채운다.** 제 크기로 가운데 두면 배경만 넓고 내용이 갑갑해 보인다.
            // 스크롤이든 아니든 자리는 똑같이 채우고, 세로로 넘치는 몫만 스크롤이 흡수한다.
            placed.anchorMin = Vector2.zero;
            placed.anchorMax = Vector2.one;
            placed.pivot     = new Vector2(0.5f, 0.5f);
            placed.offsetMin = Vector2.zero;
            placed.offsetMax = Vector2.zero;
            placed.localScale = Vector3.one;
        }

        // 입양하면 설명·상태·열기 버튼은 그 패널이 대신한다.
        _body.gameObject.SetActive(false);
        _statusText.gameObject.SetActive(false);
        _upgradeBtn.gameObject.SetActive(false);
        _openBtn.gameObject.SetActive(false);
        _useBtn.gameObject.SetActive(false);
        return true;
    }

    /// <summary>들고 있던 패널을 원래 자리·원래 모양으로 돌려준다.
    /// ⚠️ 재배치한 것까지 전부 되돌려야 한다 — 안 그러면 은신처 밖에서 그 UI가 찌그러진 채 뜬다.</summary>
    public void Release()
    {
        if (_session == null) return;

        var root = _session.Root;
        _session.Restore();
        _session = null;

        // ⚠️ 되돌려만 놓고 끄지 않으면, 도크를 닫은 뒤에도 그 패널이 전체화면으로
        // 화면에 남는다(=캐릭터를 덮는다). 게다가 계속 켜져 있어서 다음에 같은
        // 시설을 열 때 '새로 켜진 패널'로 안 잡혀 입양이 실패한다.
        if (root != null) root.gameObject.SetActive(false);

        if (_body != null) _body.gameObject.SetActive(true);
        if (_statusText != null) _statusText.gameObject.SetActive(true);
    }

    /// <summary>시설 상태(레벨·다음 비용)를 패널 안에 직접 보여준다.
}
