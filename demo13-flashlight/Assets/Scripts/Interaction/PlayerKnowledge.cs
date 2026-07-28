using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 **알아낸 것**들 — 금고 비밀번호, 뒷문 위치, 소문 따위.
/// (docs/level-scrapmarket.md 2026-07-11 — 보석상 금고 코드는 "다른 데서 주운 쪽지"로 얻는다)
///
/// 아이템이 아니라 **지식**인 것이 핵심이다.
///   • 인벤토리를 차지하지 않는다 — 코드 종이를 들고 다닐 필요가 없다
///   • **잃어버리지 않는다** — 죽어서 짐을 다 털려도 알아낸 코드는 남는다.
///     (타르코프식 레이드에서 "코드 적힌 종이를 잃어서 다시 못 연다"는 최악의 반복 노동)
///   • 획득 경로가 자유롭다 — 쪽지 파밍이든 퀘스트 보상이든 같은 플래그를 세우면 된다
///
/// 저장: <see cref="GetSaveData"/>/<see cref="LoadSaveData"/>로 세이브에 실어 나른다
/// (JsonUtility가 HashSet을 못 다뤄서 List로 주고받는다 — RaidMapManager와 같은 관용구).
/// </summary>
public static class PlayerKnowledge
{
    static readonly HashSet<string> _known = new HashSet<string>();

    /// <summary>새 게임/도메인 리로드 대비 초기화.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => _known.Clear();

    public static bool IsKnown(string id) => !string.IsNullOrEmpty(id) && _known.Contains(id);

    /// <summary>알아냈다고 기록. 처음 알아낸 경우에만 true(토스트 중복 방지).</summary>
    public static bool Learn(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (!_known.Add(id)) return false;
        Debug.Log($"[Knowledge] 알아냄: {id}");
        return true;
    }

    public static void Forget(string id) { if (!string.IsNullOrEmpty(id)) _known.Remove(id); }

    public static int Count => _known.Count;

    // ── 세이브 연동 ──
    public static List<string> GetSaveData() => new List<string>(_known);

    public static void LoadSaveData(List<string> ids)
    {
        _known.Clear();
        if (ids == null) return;
        for (int i = 0; i < ids.Count; i++)
            if (!string.IsNullOrEmpty(ids[i])) _known.Add(ids[i]);
    }

    public static void ResetForNewGame() => _known.Clear();
}
