using UnityEngine;

/// <summary>
/// BRB 월드맵 지구 정의 (docs/world-map.md).
/// 지도판(MapSelectUI) · RegionTimeManager 공통 데이터.
/// </summary>
public static class WorldRegionCatalog
{
    public struct RegionDefinition
    {
        public string regionId;
        public string displayName;
        public string subzone;
        public string sceneName;
        public string spawnId;
        public string safeFeature;
        public string anomalyFeature;
        public int difficulty;
        public Color accentColor;
        public float timeOffsetSeconds;
        /// <summary>짙은 현상 기본 침식도(0~1, 시간 무관). DenseAnomalyController가 fog+어둠 강도로 사용.</summary>
        public float anomaly;

        public bool IsPlayable => !string.IsNullOrEmpty(sceneName);

        public string DifficultyStars
        {
            get
            {
                int clamped = Mathf.Clamp(difficulty, 1, 5);
                return new string('★', clamped) + new string('☆', 5 - clamped);
            }
        }
    }

    /// <summary>
    /// 5지역 본선(지역1→5 진행 순서). 1차 데모: scrap_market만 출전 가능.
    /// 7→5 압축(2026-06-11, docs/story.md §4): 공업설비+철도 → 묻힌 정비창(industrial),
    /// 공연오락+성역추모 → 기억의 극장(entertainment). regionId는 병합 부모(industrial/entertainment) 유지.
    /// safeFeature/anomalyFeature = 한 레이드 내 '안전 구간 / 짙은 현상 구간' 특징(낮/밤 폐기, docs/story.md §4·raid.md). anomaly = 지역 기본 침식도, 중심에 가까울수록↑.
    /// </summary>
    public static readonly RegionDefinition[] All =
    {
        new RegionDefinition
        {
            regionId = "scrap_market",
            displayName = "폐상가 교역 지구",     // 지역1
            subzone = "무너진 상가 / 검문소",
            // 2026-07-11: 튜토 단독 조각(ScrapMarket_GB) → 본편 도심(Zone1)으로 전환.
            //   Zone1은 SW 코너에 튜토 구역을 그대로 품고 있고(Zone1GreyboxLayout이 ScrapMarketGreyboxLayout.Place 호출),
            //   스폰 랜덤 5·탈출 고정1+반대편2·루팅 예산제·적 밀도 곡선이 여기에만 있다.
            //   spawnId는 비움 — RaidSpawnDirector가 매 판 랜덤 스폰을 지정한다(고정 진입 X).
            sceneName = "Zone1",
            spawnId = "",
            safeFeature = "기본 파밍·회수꾼 허브·오르골 떡밥 (1차 데모)",
            anomalyFeature = "밴딧·몬스터 혼합·짙은 현상 첫 체험",
            difficulty = 2,
            accentColor = new Color(0.95f, 0.55f, 0.2f),
            timeOffsetSeconds = 0f,
            anomaly = 0.1f,
        },
        new RegionDefinition
        {
            regionId = "silence_living",
            displayName = "침묵 생활 지구",       // 지역2
            subzone = "밀집 폐아파트 단지",
            sceneName = "",
            spawnId = "",
            safeFeature = "의사·전직 공무원·동생 첫 단서(사진)",
            anomalyFeature = "약탈 갱단·지하 현상 시드",
            difficulty = 2,
            accentColor = new Color(0.35f, 0.55f, 0.95f),
            timeOffsetSeconds = 60f,
            anomaly = 0.2f,
        },
        new RegionDefinition
        {
            regionId = "industrial",
            displayName = "묻힌 정비창",          // 지역3 (공업설비 + 철도 병합)
            subzone = "지하 산업·화물 복합체 (공업+철도)",
            sceneName = "",
            spawnId = "",
            safeFeature = "수리공·상인 정착·정부 채굴 물증·시계 정체",
            anomalyFeature = "봉인 정부 구역·현상 전용종·시간 왜곡",
            difficulty = 3,
            accentColor = new Color(0.35f, 0.85f, 0.45f),
            timeOffsetSeconds = 120f,
            anomaly = 0.4f,
        },
        new RegionDefinition
        {
            regionId = "entertainment",
            displayName = "기억의 극장 지구",     // 지역4 (공연오락 + 성역추모 병합)
            subzone = "폐극장 지하 본부 / 대성당 추모",
            sceneName = "",
            spawnId = "",
            safeFeature = "연구소 본부·기억 재현·동생 생존 반전·블랙마켓",
            anomalyFeature = "강무진 정체·시계 공명·괴현상",
            difficulty = 4,
            accentColor = new Color(0.75f, 0.35f, 0.95f),
            timeOffsetSeconds = 200f,
            anomaly = 0.65f,
        },
        new RegionDefinition
        {
            regionId = "eternal_night_core",
            displayName = "중앙 영야 심장",       // 지역5
            subzone = "흑월 첨탑 / 잔영 3",
            sceneName = "",
            spawnId = "",
            safeFeature = "도시 봉쇄 흔적·스토리 클라이맥스",
            anomalyFeature = "최고 위험·강무진 희생·엔딩 분기",
            difficulty = 5,
            accentColor = new Color(0.55f, 0.25f, 0.85f),
            timeOffsetSeconds = 360f,
            anomaly = 0.95f,          // 중앙 영야 = 최고 침식
        },
    };

    public static RegionDefinition? GetById(string regionId)
    {
        if (string.IsNullOrEmpty(regionId)) return null;
        for (int i = 0; i < All.Length; i++)
            if (All[i].regionId == regionId)
                return All[i];
        return null;
    }

    public static int IndexOf(string regionId)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].regionId == regionId)
                return i;
        return -1;
    }

    public static string BuildDescription(in RegionDefinition r)
    {
        var lines = $"난이도: {r.DifficultyStars}\n{r.subzone}\n\n" +
                    $"○ 안전 구간: {r.safeFeature}\n● 짙은 현상: {r.anomalyFeature}";

        if (r.IsPlayable)
            lines += "\n\n<color=#88FFAA>▶ 1차 데모 출전 가능</color>";
        else
            lines += "\n\n<color=#888888>🔒 정찰 전 — 아직 출전할 수 없습니다.</color>";

        return lines;
    }

    /// <summary>버튼·하이라이트용 (잠금 시 회색)</summary>
    public static Color GetButtonColor(in RegionDefinition r, bool selected)
    {
        if (!r.IsPlayable)
            return selected ? new Color(0.28f, 0.28f, 0.32f, 0.95f) : new Color(0.18f, 0.18f, 0.2f, 0.85f);

        var c = r.accentColor;
        if (selected)
            return new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.95f);
        return new Color(c.r * 0.22f, c.g * 0.22f, c.b * 0.22f, 0.9f);
    }
}
