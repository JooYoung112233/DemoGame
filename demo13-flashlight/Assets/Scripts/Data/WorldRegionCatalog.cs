using UnityEngine;

/// <summary>
/// 밤의 도시 월드맵 지구 정의 (docs/world-map.md).
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
        public string dayFeature;
        public string nightFeature;
        public int difficulty;
        public Color accentColor;
        public float timeOffsetSeconds;

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

    /// <summary>시계 방향(상단부터) + 중앙 코어. 1차 데모: scrap_market만 출전 가능.</summary>
    public static readonly RegionDefinition[] All =
    {
        new RegionDefinition
        {
            regionId = "silence_living",
            displayName = "침묵 생활 지구",
            subzone = "외곽 주거지 / 생존자 시장",
            sceneName = "",
            spawnId = "",
            dayFeature = "NPC·쪽지·정보 수집에 유리",
            nightFeature = "밴딧 약탈·생존자 시장 위험",
            difficulty = 2,
            accentColor = new Color(0.35f, 0.55f, 0.95f),
            timeOffsetSeconds = 30f,
        },
        new RegionDefinition
        {
            regionId = "scrap_market",
            displayName = "폐상가 교역 지구",
            subzone = "무너진 상가 / 검문소",
            sceneName = "InGameScene",
            spawnId = "default",
            dayFeature = "기본 파밍·NPC·쪽지 (1차 데모)",
            nightFeature = "밴딧·몬스터 혼합·네온 골목",
            difficulty = 2,
            accentColor = new Color(0.95f, 0.55f, 0.2f),
            timeOffsetSeconds = 0f,
        },
        new RegionDefinition
        {
            regionId = "industrial",
            displayName = "공업 설비 지구",
            subzone = "발전소 / 정제 시설",
            sceneName = "",
            spawnId = "",
            dayFeature = "부품·재료·루디 단서",
            nightFeature = "강한 밴딧·기계 이상현상",
            difficulty = 3,
            accentColor = new Color(0.35f, 0.85f, 0.45f),
            timeOffsetSeconds = 90f,
        },
        new RegionDefinition
        {
            regionId = "entertainment",
            displayName = "공연 오락 지구",
            subzone = "폐극장가 / 네온 거리",
            sceneName = "",
            spawnId = "",
            dayFeature = "스토리·쪽지·미스터리 단서",
            nightFeature = "보스·괴현상·LUNA 네온 거리",
            difficulty = 4,
            accentColor = new Color(0.75f, 0.35f, 0.95f),
            timeOffsetSeconds = 150f,
        },
        new RegionDefinition
        {
            regionId = "railway_scrap",
            displayName = "철도 폐기 지구",
            subzone = "폐철도 / 스크랩 야드",
            sceneName = "",
            spawnId = "",
            dayFeature = "스크랩·기계 부품·탈출 루트 힌트",
            nightFeature = "기차 노선 이벤트·밀수·고철 야드",
            difficulty = 3,
            accentColor = new Color(0.95f, 0.85f, 0.25f),
            timeOffsetSeconds = 220f,
        },
        new RegionDefinition
        {
            regionId = "sanctuary_memorial",
            displayName = "성역 추모 지구",
            subzone = "대성당 / 추모 공원",
            sceneName = "",
            spawnId = "",
            dayFeature = "종교·추모·동생 단서 후보",
            nightFeature = "짙은 현상·미스터리 이벤트",
            difficulty = 3,
            accentColor = new Color(0.35f, 0.9f, 0.85f),
            timeOffsetSeconds = 280f,
        },
        new RegionDefinition
        {
            regionId = "eternal_night_core",
            displayName = "중앙 영야 심장",
            subzone = "흑월 첨탑 / 붕괴 관측소",
            sceneName = "",
            spawnId = "",
            dayFeature = "정부 붕괴 흔적·스토리 클라이맥스",
            nightFeature = "최고 위험·방역 현상 불가 예측",
            difficulty = 5,
            accentColor = new Color(0.55f, 0.25f, 0.85f),
            timeOffsetSeconds = 360f,
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
                    $"☀ 낮: {r.dayFeature}\n☾ 밤: {r.nightFeature}";

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
