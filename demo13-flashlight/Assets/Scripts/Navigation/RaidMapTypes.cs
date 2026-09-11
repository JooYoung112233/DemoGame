using UnityEngine;

// 내비게이션(시계 나침반 + 미니맵/맵) 공용 타입.
// 설계: docs/navigation.md

/// <summary>통로(문) 상태 — 사일런트 힐식 자동 주석용. (docs/navigation.md §3.4)</summary>
public enum PassageState
{
    Open,     // 통과 가능
    Locked,   // 잠김(열쇠/퀘스트) — 수동 배치, 영속 주석
    Blocked,  // 막힘(잔해) — 확률 배치, 이번 판 한정
    OneWay,   // 일방통행(반대편만 열림)
}

/// <summary>나침반 타겟 우선순위. 값이 클수록 우선. (docs/navigation.md §1.1)</summary>
public enum CompassPriority
{
    Exit = 0,      // 탈출구(폴백)
    Story = 10,    // 스토리 목표
    Quest = 20,    // 활성 퀘스트 목표
    Tutorial = 30, // 튜토리얼 단계
}

/// <summary>레이드 맵의 한 구역(존). 월드 XY 영역 + fog 상태.</summary>
[System.Serializable]
public class MapZone
{
    public string id;
    public string displayName;
    public Rect bounds;     // 월드 XY (xMin, yMin, width, height)
    public bool discovered; // 이번 레이드 시점의 표시 여부 (영속 복원됨)

    public MapZone(string id, string displayName, Rect bounds)
    {
        this.id = id;
        this.displayName = displayName;
        this.bounds = bounds;
        this.discovered = false;
    }

    public bool Contains(Vector2 worldPos) => bounds.Contains(worldPos);
    public Vector2 Center => bounds.center;
}

// ── 세이브 DTO (2026-07-11) ─────────────────────────────────────────────
// 지역별 "발견한 존 / 알게 된 통로" 누적 지식. JsonUtility 직렬화 대상이라
// Dictionary/HashSet을 못 쓰므로 리스트 쌍으로 편다.

[System.Serializable]
public class RaidMapRegionEntry
{
    public string regionId;
    public System.Collections.Generic.List<string> ids = new System.Collections.Generic.List<string>();
}

[System.Serializable]
public class RaidMapSaveData
{
    public System.Collections.Generic.List<RaidMapRegionEntry> discovered = new System.Collections.Generic.List<RaidMapRegionEntry>();
    public System.Collections.Generic.List<RaidMapRegionEntry> passages   = new System.Collections.Generic.List<RaidMapRegionEntry>();
}
