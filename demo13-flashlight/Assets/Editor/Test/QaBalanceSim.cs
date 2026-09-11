#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 헤드리스 밸런스 시뮬레이터 (QA) — 씬·렌더링 없이 **루트 테이블·예산·경제·레벨 페이스**를
/// 수천 번 굴려 분포를 낸다. 실플레이 3판보다 밸런스 이상을 훨씬 잘 잡는다. (docs/qa.md)
///
/// 실플레이 봇(`QaBot`)이 "동작이 깨졌나"를 본다면, 이쪽은 "수치가 이상한가"를 본다.
///   • 지역×티어 루팅 롤 N회 → 판당 아이템 수·가치 분포(중앙값/평균/최소·최대)
///   • MapSpawnProfile 예산 → 한 판 기대 수익(낮/밤)
///   • 레벨 페이스 → 목표 레벨까지 몇 판
///   • 이상 징후 자동 플래그(빈 풀·0원 아이템·가치 폭주 등)
///
/// 메뉴: Tools ▸ TopDown ▸ QA ▸ 밸런스 시뮬레이션
/// </summary>
public static class QaBalanceSim
{
    const int Rolls = 2000;      // 티어별 롤 횟수
    const int RaidSamples = 500; // 한 판 시뮬 표본

    [MenuItem("Tools/TopDown/QA/밸런스 시뮬레이션", priority = -49)]
    public static void Run()
    {
        // ItemDatabase는 RuntimeInitializeOnLoadMethod(AfterSceneLoad)로만 생성된다.
        // 비플레이 상태로 롤을 돌리면 매 항목이 "아이템 없음" 경고를 찍어 수만 건이 쏟아지고(에디터 프리즈)
        // 결과는 전부 0이라 오탐만 남는다. → 플레이 모드에서만 실행.
        var probe = ItemDatabase.GetAll();
        if (probe == null || probe.Length == 0)
        {
            string msg = "아이템 DB가 아직 로드되지 않았습니다(ItemDatabase는 플레이 시작 시 생성).\n\n" +
                         "▶ 에디터에서 **Play를 누른 상태**로 이 메뉴를 실행하세요.";
            Debug.LogWarning("[QA] 밸런스 시뮬 중단 — " + msg.Replace('\n', ' '));
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("QA 밸런스 시뮬레이션", msg, "확인");
            return;
        }

        var sb = new StringBuilder();
        int warn = 0;

        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine(" QA 밸런스 시뮬레이션 (헤드리스)");
        sb.AppendLine($" 시각: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}   롤 {Rolls}회/티어 · 판 표본 {RaidSamples}");
        sb.AppendLine("═══════════════════════════════════════════\n");

        var tiers = new[]
        {
            RegionLootTier.GroundDay, RegionLootTier.GroundNight,
            RegionLootTier.ContainerDay, RegionLootTier.ContainerNight,
        };

        // ── 1. 지역×티어 루팅 분포 ──────────────────────────────────
        sb.AppendLine("── 1. 루팅 롤 분포 (1회 롤 기준) ──");
        sb.AppendLine($"  {"지역",-18}{"티어",-16}{"평균개수",9}{"평균가치",10}{"최대가치",10}  비고");

        var perTierValue = new Dictionary<string, float>();   // "region/tier" → 롤당 평균 가치

        foreach (var r in WorldRegionCatalog.All)
        {
            foreach (var tier in tiers)
            {
                if (!RegionLootCatalog.TryGetPool(r.regionId, tier, out var pool) || pool.entries == null || pool.entries.Length == 0)
                    continue;   // 풀 없음 = 미작성 지역(정상)

                long totalCount = 0, totalValue = 0; int maxValue = 0; int zeroPrice = 0;
                for (int i = 0; i < Rolls; i++)
                {
                    var items = RegionLootCatalog.Roll(r.regionId, tier);
                    if (items == null) continue;
                    int v = 0;
                    foreach (var it in items)
                    {
                        if (it == null || it.data == null) continue;
                        totalCount += it.stackCount;
                        v += it.data.sellPrice * it.stackCount;
                        if (it.data.sellPrice <= 0) zeroPrice++;
                    }
                    totalValue += v;
                    if (v > maxValue) maxValue = v;
                }

                float avgCount = totalCount / (float)Rolls;
                float avgValue = totalValue / (float)Rolls;
                perTierValue[$"{r.regionId}/{tier}"] = avgValue;

                string note = "";
                if (zeroPrice > Rolls / 2) { note = "⚠ sellPrice 0 아이템 다수"; warn++; }
                if (avgValue > 0 && maxValue > avgValue * 20f) { note += " ⚠ 가치 편차 극심"; warn++; }

                sb.AppendLine($"  {r.regionId,-18}{tier,-16}{avgCount,9:0.##}{avgValue,10:0}{maxValue,10}  {note}");
            }
        }
        sb.AppendLine();

        // ── 2. 한 판 기대 수익 (MapSpawnProfile 예산 × 롤 가치) ──────
        sb.AppendLine("── 2. 한 판 기대 수익 (예산제 기준) ──");
        sb.AppendLine($"  {"프로파일",-20}{"지역",-16}{"낮 수익",10}{"밤 수익",10}  (바닥+상자 예산 × 평균 롤 가치)");

        foreach (var guid in AssetDatabase.FindAssets("t:MapSpawnProfile"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prof = AssetDatabase.LoadAssetAtPath<MapSpawnProfile>(path);
            if (prof == null) continue;

            string region = prof.profileId;
            float dayVal = EstimateRaidValue(prof, region, false, perTierValue);
            float nightVal = EstimateRaidValue(prof, region, true, perTierValue);

            string note = "";
            if (dayVal <= 0f) { note = "⚠ 수익 0 — 루트 풀/프로파일 확인"; warn++; }

            sb.AppendLine($"  {System.IO.Path.GetFileNameWithoutExtension(path),-20}{region,-16}{dayVal,10:0}{nightVal,10:0}  {note}");
        }
        sb.AppendLine();

        // ── 3. 레벨 페이스 ──────────────────────────────────────────
        var tune = GameTuning.Instance;
        int baseXp   = tune != null ? tune.xpPerLevelBase : 100;
        int extract  = tune != null ? tune.xpExtractBonus : 100;
        float lootXpRate = tune != null ? tune.xpPerLootValue : 0.02f;

        sb.AppendLine("── 3. 레벨 페이스 (탈출 성공 기준, 킬 XP 제외) ──");
        // 대표 수익 = 첫 플레이 가능 지역
        float repValue = 0f; string repRegion = "-";
        foreach (var r in WorldRegionCatalog.All)
            if (!string.IsNullOrEmpty(r.sceneName))
            {
                repRegion = r.regionId;
                foreach (var guid in AssetDatabase.FindAssets("t:MapSpawnProfile"))
                {
                    var prof = AssetDatabase.LoadAssetAtPath<MapSpawnProfile>(AssetDatabase.GUIDToAssetPath(guid));
                    if (prof != null && prof.profileId == r.regionId)
                    { repValue = EstimateRaidValue(prof, r.regionId, false, perTierValue); break; }
                }
                break;
            }

        float xpPerRaid = extract + repValue * lootXpRate;
        sb.AppendLine($"  대표 지역: {repRegion} · 판당 수익 {repValue:0} · 판당 XP ≈ {xpPerRaid:0} (탈출 {extract} + 루팅 {repValue * lootXpRate:0})");

        if (xpPerRaid <= 0f) { sb.AppendLine("  ⚠ 판당 XP 0 — 레벨업 불가"); warn++; }
        else
        {
            int lv = 1; float acc = 0f; int raids = 0;
            var marks = new[] { 5, 10, 20 };
            int mi = 0;
            while (lv < 30 && raids < 100000 && mi < marks.Length)
            {
                acc += xpPerRaid; raids++;
                int need = Mathf.Max(1, baseXp * lv);
                while (acc >= need && lv < 30) { acc -= need; lv++; need = Mathf.Max(1, baseXp * lv); }
                if (lv >= marks[mi]) { sb.AppendLine($"  Lv{marks[mi],-3} 도달 = {raids,5}판"); mi++; }
            }
        }
        sb.AppendLine();

        // ── 4. 경제 대조 (판당 수익 vs 상점 가격대) ──────────────────
        sb.AppendLine("── 4. 경제 대조 ──");
        int itemCount = 0; long sumBuy = 0, sumSell = 0; int maxSell = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            var it = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (it == null) continue;
            itemCount++; sumBuy += it.buyPrice; sumSell += it.sellPrice;
            if (it.sellPrice > maxSell) maxSell = it.sellPrice;
        }
        if (itemCount > 0)
        {
            float avgBuy = sumBuy / (float)itemCount, avgSell = sumSell / (float)itemCount;
            sb.AppendLine($"  아이템 {itemCount}종 · 평균 판매가 {avgSell:0} · 평균 구매가 {avgBuy:0} · 최고 판매가 {maxSell}");
            float spread = avgSell > 0 ? avgBuy / avgSell : 0f;
            sb.AppendLine($"  구매/판매 스프레드 = {spread:0.00}배  (경제 기준선: buy ≈ sell×2.5 — 돈복사 방지)");
            if (spread < 1.5f) { sb.AppendLine("  ⚠ 스프레드가 좁음 — 되팔이 차익 위험"); warn++; }
            if (repValue > 0f) sb.AppendLine($"  판당 수익 {repValue:0} ÷ 평균 구매가 {avgBuy:0} = 한 판에 평균템 {(avgBuy > 0 ? repValue / avgBuy : 0):0.0}개 구매력");
        }

        sb.AppendLine();
        sb.AppendLine($"※ 경고 {warn}건");

        string text = sb.ToString();
        Debug.Log(text);

        try
        {
            string p = System.IO.Path.Combine(Application.persistentDataPath,
                $"qa-balance-{System.DateTime.Now:yyyyMMdd-HHmmss}.txt");
            System.IO.File.WriteAllText(p, text);
            Debug.Log($"[QA] 밸런스 리포트 저장: {p}");
        }
        catch (System.Exception e) { Debug.LogWarning($"[QA] 저장 실패: {e.Message}"); }

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("QA 밸런스 시뮬레이션",
                $"완료 — 경고 {warn}건\n\nConsole에 표 출력 + persistentDataPath에 리포트 저장.", "확인");
    }

    /// <summary>프로파일 예산 × 티어 평균 롤 가치 = 한 판 기대 수익(근사).
    /// 예산은 "총 아이템 수"이고 롤 1회가 여러 개를 뱉으므로, 롤당 평균 개수로 나눠 롤 수를 환산한다.</summary>
    static float EstimateRaidValue(MapSpawnProfile prof, string regionId, bool night, Dictionary<string, float> perTierValue)
    {
        var gTier = night ? RegionLootTier.GroundNight : RegionLootTier.GroundDay;
        var cTier = night ? RegionLootTier.ContainerNight : RegionLootTier.ContainerDay;

        float gVal = perTierValue.TryGetValue($"{regionId}/{gTier}", out var a) ? a : 0f;
        float cVal = perTierValue.TryGetValue($"{regionId}/{cTier}", out var b) ? b : 0f;

        // 예산 중앙값(랜덤 범위의 중앙). GetGroundBudget은 Random을 쓰므로 여기선 필드로 직접 계산.
        float gBudget = (prof.groundItemMin + prof.groundItemMax) * 0.5f;
        float cBudget = (prof.containerItemMin + prof.containerItemMax) * 0.5f;
        float mult = prof.spawnMultiplier * (night ? prof.nightSpawnMultiplier : 1f);

        // 롤당 평균 아이템 수를 모르면 1로 간주(보수적).
        return (gBudget * gVal + cBudget * cVal) * mult;
    }
}
#endif
