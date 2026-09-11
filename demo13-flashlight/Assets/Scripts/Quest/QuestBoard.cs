using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게시판 일일 의뢰(BD) 보드 — 로직 전담(UI는 MapSelectUI 의뢰 패널).
/// 설계: docs/quests-region1.md §9.3 「게시판 일일 의뢰 풀 (BD)」.
///  - 풀 = Resources/Data/Quests/Board/BD-*.asset (19종).
///  - 매일(실시간 일 기준, DailyQuestManager와 동일) 2장 노출 — 일자 시드 랜덤이라 재부팅해도 같은 날 같은 의뢰.
///  - 떡밥(★ BD-13/15/16/19/20)은 슬롯당 ~20% 확률로만 섞임.
///  - 동시 수주 = BD 최대 2 (계약 BQ/NQ·전당포 DQ와 별개 슬롯 — §9.3).
///  - 수령/보고 모두 게시판(그레이박스 — 의뢰인 NPC 보고는 후속).
///    수집·납품형 보고 = 보유 검사 + 원자 차감(OwnedItems.TryRemove) 후 완료.
///    처치형 보고 = QuestManager 목표(ReadyToReport) 게이트.
///  - 회전 풀 자동 게이트: 목표 itemId/unitKey가 실존하는 의뢰만 노출(탐색형=POI 시스템 전 제외).
///  - 오늘 완료 슬롯은 소진(런타임 전용 — 재시작 시 재노출되나 반복 의뢰라 무해. 위탁/수배 원칙).
/// </summary>
public static class QuestBoard
{
    const int DAILY_SLOTS = 2;
    const int MAX_ACTIVE_BD = 2;
    const float BAIT_CHANCE = 0.2f;   // ★떡밥 슬롯 확률 (§9.3 "하루 2장 중 ~20%")

    static readonly HashSet<string> BaitIds = new HashSet<string> { "BD-13", "BD-15", "BD-16", "BD-19", "BD-20" };
    // 평판 C+ 게이트 (§9.3 표의 평판C 항목 — SO엔 flag만 있어 코드로 게이트)
    static readonly HashSet<string> RepCGate = new HashSet<string> { "BD-10", "BD-15", "BD-17" };

    static QuestData[] pool;                 // Board 폴더 전체 로드(1회) — BD(일일)+BQ(고정) 공용

    static QuestData[] Pool()
    {
        if (pool == null) pool = Resources.LoadAll<QuestData>("Data/Quests/Board");
        return pool ?? System.Array.Empty<QuestData>();
    }
    static readonly List<QuestData> today = new List<QuestData>();
    static readonly HashSet<string> doneToday = new HashSet<string>();   // 오늘 완료(슬롯 소진) — 런타임 전용
    static int rolledDay = -1;

    /// <summary>세이브 로드/새 게임 시 런타임 상태 초기화.</summary>
    public static void ResetRuntime()
    {
        today.Clear();
        doneToday.Clear();
        rolledDay = -1;
    }

    // ═══════════════════════════
    //  오늘의 의뢰 회전
    // ═══════════════════════════

    static int TodayStamp => DateTime.Now.Year * 1000 + DateTime.Now.DayOfYear;

    /// <summary>오늘 게시된 의뢰(최대 2). 날짜가 바뀌면 재추첨.</summary>
    public static IReadOnlyList<QuestData> TodayOffers
    {
        get { EnsureToday(); return today; }
    }

    public static bool IsDoneToday(string questId) => doneToday.Contains(questId);

    static void EnsureToday()
    {
        int stamp = TodayStamp;
        if (rolledDay == stamp && today.Count > 0) return;
        if (rolledDay != stamp) doneToday.Clear();
        rolledDay = stamp;
        today.Clear();

        var avail = AvailablePool();
        if (avail.Count == 0) return;

        var baits = avail.FindAll(q => BaitIds.Contains(q.questId));
        var normals = avail.FindAll(q => !BaitIds.Contains(q.questId));

        // 일자 시드 — 재부팅/재진입에도 같은 날엔 같은 2장.
        var rng = new System.Random(stamp * 7919);
        for (int slot = 0; slot < DAILY_SLOTS; slot++)
        {
            List<QuestData> src = (rng.NextDouble() < BAIT_CHANCE && baits.Count > 0) ? baits : normals;
            if (src.Count == 0) src = normals.Count > 0 ? normals : baits;
            if (src.Count == 0) break;
            var pick = src[rng.Next(src.Count)];
            today.Add(pick);
            baits.Remove(pick);
            normals.Remove(pick);
        }
    }

    /// <summary>일일 회전(BD) 노출 가능 풀 — BD 전용(같은 폴더의 BQ 제외) + 해금 flag + 평판 게이트 + '지금 완수 가능한 목표'만(자동 게이트).</summary>
    static List<QuestData> AvailablePool()
    {
        var list = new List<QuestData>();
        var qm = QuestManager.Instance;
        foreach (var q in Pool())
        {
            if (q == null || q.objectives == null || q.objectives.Length == 0) continue;
            if (!q.questId.StartsWith("BD-")) continue;   // BQ 고정 의뢰는 별도 섹션(BqOffers)

            // 해금 flag (prologue_complete 등)
            string flag = q.unlockCondition != null ? q.unlockCondition.requiredFlag : null;
            if (!string.IsNullOrEmpty(flag) && (qm == null || !qm.GetFlag(flag))) continue;

            // 평판 C+ 게이트
            if (RepCGate.Contains(q.questId))
            {
                var rep = ReputationManager.Instance;
                if (rep == null || rep.Tier < ReputationTier.C) continue;
            }

            if (!IsImplementable(q)) continue;
            list.Add(q);
        }
        return list;
    }

    /// <summary>레이드 맵에 QuestPoiZone으로 배치된(=완수 가능한) 탐색 POI id.
    /// ScrapMarketGreyboxLayout에 존이 있는 것만. 짙은 현상(anomaly_edge/deep_seal)은 그 맵 생기면 추가.</summary>
    static readonly HashSet<string> ImplementedPois = new HashSet<string>
    {
        "poi_warehouse_noise", "poi_collapsed_shop", "poi_north_road", "poi_signal_source", "poi_farm_sweep",
    };

    /// <summary>목표가 현재 콘텐츠로 완수 가능한가 — 수집/납품: itemId 실존, 처치: unitKey 실존,
    /// 탐색: POI 존이 배치된 poiId만(미배치 anomaly 등은 자동 제외).</summary>
    static bool IsImplementable(QuestData q)
    {
        foreach (var obj in q.objectives)
        {
            switch (obj.type)
            {
                case ObjectiveType.CollectItem:
                    if (ItemDatabase.Get(obj.targetId) == null) return false;
                    break;
                case ObjectiveType.KillEnemy:
                    if (StatDB.Instance == null || StatDB.Instance.GetUnit(obj.targetId) == null) return false;
                    break;
                case ObjectiveType.ReachPoint:
                    if (!ImplementedPois.Contains(obj.targetId)) return false;
                    break;
                default:
                    return false;   // TalkToNPC — 보고 시스템 후 합류
            }
        }
        return true;
    }

    // ═══════════════════════════
    //  BQ 고정 의뢰 (평판 티어 게이트 — quests-region1 §9.3)
    // ═══════════════════════════

    /// <summary>BQ questId → 요구 평판 티어 ("BQ-E01" → E). 포맷 밖이면 null.</summary>
    public static ReputationTier? RequiredTier(string questId)
    {
        if (string.IsNullOrEmpty(questId) || !questId.StartsWith("BQ-") || questId.Length < 4) return null;
        switch (questId[3])
        {
            case 'E': return ReputationTier.E;
            case 'D': return ReputationTier.D;
            case 'C': return ReputationTier.C;
            case 'B': return ReputationTier.B;
            case 'A': return ReputationTier.A;
            default: return null;
        }
    }

    /// <summary>현재 평판 티어로 노출되는 BQ 고정 의뢰(회전 아님 — 항상 게시). 해금 flag + 자동 게이트 동일 적용.</summary>
    public static List<QuestData> BqOffers()
    {
        var list = new List<QuestData>();
        var qm = QuestManager.Instance;
        var rep = ReputationManager.Instance;
        var tier = rep != null ? rep.Tier : ReputationTier.F;

        foreach (var q in Pool())
        {
            if (q == null || q.objectives == null || q.objectives.Length == 0) continue;
            var req = RequiredTier(q.questId);
            if (req == null)
            {
                // "BQ-" 접두인데 티어 문자 파싱 실패 = 에셋 ID 오타 — 조용히 증발하는 계열이라 경고
                if (q.questId.StartsWith("BQ-"))
                    Debug.LogWarning($"[QuestBoard] BQ ID 포맷 이상({q.questId}) — 'BQ-{{E|D|C|B|A}}nn' 규칙 필요, 게시판에 노출되지 않음");
                continue;
            }
            if (tier < req.Value) continue;       // 평판 티어 미달

            string flag = q.unlockCondition != null ? q.unlockCondition.requiredFlag : null;
            if (!string.IsNullOrEmpty(flag) && (qm == null || !qm.GetFlag(flag))) continue;
            if (!IsImplementable(q)) continue;
            list.Add(q);
        }
        list.Sort((a, b) => string.Compare(a.questId, b.questId, System.StringComparison.Ordinal));
        return list;
    }

    /// <summary>수주 중인 계약 의뢰(BQ/NQ — 계약 슬롯 1 공유, §9.3).</summary>
    public static QuestInstance ActiveContract()
    {
        if (QuestManager.Instance == null) return null;
        return QuestManager.Instance.ActiveQuests.Find(q => q?.data != null &&
            (q.data.questId.StartsWith("BQ-") || q.data.questId.StartsWith("NQ-")));
    }

    /// <summary>BQ 수주 — 계약 슬롯 1개(BQ/NQ 공유) 제한.</summary>
    public static bool AcceptBq(QuestData data, out string msg)
    {
        msg = "";
        if (data == null || QuestManager.Instance == null) return false;
        if (FindActive(data.questId) != null) { msg = "이미 수주한 의뢰다"; return false; }
        var contract = ActiveContract();
        if (contract != null) { msg = $"계약 의뢰는 1건까지 — 진행 중: {contract.data.title}"; return false; }
        if (!QuestManager.Instance.AcceptQuest(data)) { msg = "수주할 수 없다"; return false; }
        msg = $"계약 수주: {data.title} — 완수 후 {ReportNpcName(data)}에게 보고";
        return true;
    }

    /// <summary>보고 NPC 표시명 (giverNpcId → NPCData.displayName, 없으면 id).</summary>
    public static string ReportNpcName(QuestData q)
    {
        if (q == null || string.IsNullOrEmpty(q.giverNpcId)) return "게시판";
        var npc = Resources.Load<NPCData>($"Data/NPC/{q.giverNpcId}");
        return npc != null && !string.IsNullOrEmpty(npc.displayName) ? npc.displayName : q.giverNpcId;
    }

    // ═══════════════════════════
    //  수주 / 보고
    // ═══════════════════════════

    /// <summary>현재 수주 중인 BD 의뢰들.</summary>
    public static List<QuestInstance> ActiveBd()
    {
        var list = new List<QuestInstance>();
        if (QuestManager.Instance == null) return list;
        foreach (var q in QuestManager.Instance.ActiveQuests)
            if (q?.data != null && q.data.questId.StartsWith("BD-")) list.Add(q);
        return list;
    }

    public static QuestInstance FindActive(string questId)
    {
        if (QuestManager.Instance == null) return null;
        return QuestManager.Instance.ActiveQuests.Find(q => q?.data != null && q.data.questId == questId);
    }

    /// <summary>의뢰서 떼기(수주). BD 동시 2장 제한.</summary>
    public static bool Accept(QuestData data, out string msg)
    {
        msg = "";
        if (data == null || QuestManager.Instance == null) return false;
        if (FindActive(data.questId) != null) { msg = "이미 수주한 의뢰다"; return false; }
        if (ActiveBd().Count >= MAX_ACTIVE_BD) { msg = $"게시판 의뢰는 동시에 {MAX_ACTIVE_BD}건까지"; return false; }
        if (!QuestManager.Instance.AcceptQuest(data)) { msg = "수주할 수 없다"; return false; }
        msg = $"의뢰 수주: {data.title}";
        return true;
    }

    /// <summary>보고 가능 여부 + 진행 문자열. 수집/납품 = 현재 보유량 기준(픽업 카운트 아님 — 창고 포함).</summary>
    public static bool CanReport(QuestInstance q, out string progress)
    {
        progress = "";
        if (q?.data == null || q.data.objectives.Length == 0) return false;
        var obj = q.data.objectives[0];
        if (obj.type == ObjectiveType.CollectItem)
        {
            var item = ItemDatabase.Get(obj.targetId);
            int owned = item != null ? OwnedItems.Count(item) : 0;
            progress = $"보유 {owned}/{obj.requiredCount}";
            return owned >= obj.requiredCount;
        }
        // 처치/탐색형 — QuestManager 진행도
        int cur = q.progress.TryGetValue(0, out int v) ? v : 0;
        string verb = obj.type == ObjectiveType.ReachPoint ? "정찰" : "처치";
        progress = $"{verb} {cur}/{obj.requiredCount}";
        return q.state == QuestState.ReadyToReport;
    }

    /// <summary>게시판 보고(완료). 수집/납품형은 소지품 원자 차감 후 완료(force), 처치형은 정상 완료.</summary>
    public static bool Report(QuestInstance q, out string msg)
    {
        msg = "";
        if (q?.data == null || QuestManager.Instance == null) return false;
        // 스테일 인스턴스(이미 완료/제거) 방어 — 차감 전에 활성 소속 확인, 아이템만 증발하는 경로 차단.
        if (FindActive(q.data.questId) == null) { msg = "이미 처리된 의뢰다"; return false; }
        if (!CanReport(q, out string prog)) { msg = $"아직 완수 못 했다 ({prog})"; return false; }

        var obj = q.data.objectives[0];
        if (obj.type == ObjectiveType.CollectItem)
        {
            var item = ItemDatabase.Get(obj.targetId);
            if (item == null || !OwnedItems.TryRemove(item, obj.requiredCount))
            {
                msg = "물건이 모자라다";
                return false;
            }
            if (!QuestManager.Instance.CompleteQuest(q.data.questId, force: true))   // 보유 검사·차감을 여기서 했으므로
            {
                Debug.LogError($"[QuestBoard] {q.data.questId} 완료 실패 — 아이템은 이미 차감됨(활성 확인 후라 도달하면 안 되는 경로)");
                msg = "보고 처리 오류";
                return false;
            }
        }
        else
        {
            if (!QuestManager.Instance.CompleteQuest(q.data.questId)) { msg = "보고 실패"; return false; }
        }

        doneToday.Add(q.data.questId);   // 오늘 슬롯 소진
        msg = $"의뢰 완료: {q.data.title}";
        return true;
    }
}
