/// <summary>
/// 지역별 루트 테이블 티어 (docs/region-loot.md).
/// </summary>
public enum RegionLootTier
{
    GroundDay,
    GroundNight,
    ContainerDay,
    ContainerNight,
    // 짙은 현상 구간 전용(밤/위협 무관, DenseAnomalyZone 트리거 예정). 루디 회수 = 1지역 중후반 주 소득.
    GroundAnomaly,
    ContainerAnomaly,
}
