# Unity 이식 가이드

> **현재**: Phaser 3 (JavaScript, Canvas, 브라우저) — 기획 검증 프로토타입
> **최종**: Unity (C#, 본 게임 출시 플랫폼)
> 본 문서는 Unity 이식 시 고려할 사항 + 데이터/시스템 매핑 정리.

## 1. 단계별 접근

### 1.1 현재 (Phaser 프로토타입)
- 기획 검증 + 핵심 루프 동작 확인
- 빠른 반복 (브라우저 새로고침)
- 밸런스 시뮬레이션 (Node.js)
- 모든 시스템 md = **엔진 독립적** 작성

### 1.2 이식 시점
- Phaser 프로토타입에서 12시간 페이싱 검증 완료 후
- 모든 시스템 디자인 확정 후
- 또는 기획만 끝나면 즉시 Unity로 시작 (Phaser 단계 생략 가능)

### 1.3 Unity 이식 후
- 본 게임 개발 진행
- Phaser 프로토타입은 참고용 보존

## 2. 데이터 이식

기존 `src/data/*.js`는 그대로 Unity에서 활용 가능 (JSON 변환).

| Phaser 파일 | Unity 이식 |
|---|---|
| `data/balance.js` | `Resources/Data/Balance.json` 또는 `ScriptableObject` |
| `data/units.js` (CLASS_DATA) | `ScriptableObject` (ClassData.asset 6개) |
| `data/zones.js` | `ZoneData.asset` 3개 |
| `data/cards.js` | `CardData.asset` |
| `data/synergies.js` | `SynergyData.asset` 14개 |
| `data/facilities.js` | `FacilityData.asset` |
| `data/events.js` | `EventData.asset` |
| `data/items.js` | `ItemData.asset` |
| `data/recipes.js` | `RecipeData.asset` |

**권장**: Unity ScriptableObject 활용. 인스펙터에서 직접 편집 가능.

## 3. 시스템 매핑

### 3.1 씬 (Scene)

| Phaser 씬 | Unity Scene |
|---|---|
| `BootScene` | `Bootstrap.unity` |
| `TownScene` | `Town.unity` |
| `BattleScene` (BP) | `Battle_BloodPit.unity` 또는 단일 `Battle.unity` + 상태 관리 |
| `BattleScene` (Cargo) | `Battle_Cargo.unity` |
| `BattleScene` (Blackout) | `Battle_Blackout.unity` |
| `RosterScene` | `Roster.unity` (또는 UI 패널) |
| `DeployScene` | UI 패널 (씬 분리 불요) |
| `RecruitScene` | UI 패널 |
| `AffinityScene` | UI 패널 |

**권장**: Unity는 메인 메뉴 / 마을 / 전투 정도만 씬 분리. 나머지는 UI 패널.

### 3.2 매니저 / 시스템

| Phaser 클래스 | Unity 클래스 |
|---|---|
| `GuildManager` | `GuildManager.cs` (싱글톤 또는 ScriptableObject) |
| `MercenaryManager` | `MercenaryManager.cs` |
| `SaveManager` | `SaveManager.cs` (JSON serialize) |
| `CombatSystem` | `CombatSystem.cs` (MonoBehaviour) |
| `SkillSystem` | `SkillSystem.cs` |
| `ExpeditionManager` | `ExpeditionManager.cs` (실시간 갱신) |
| `RunSimulator` | `RunSimulator.cs` (static utility) |
| `GuildHallManager` | `GuildHallManager.cs` |
| `BondManager` | `BondManager.cs` |

### 3.3 엔티티

| Phaser | Unity |
|---|---|
| `Character.js` (전투) | `BattleUnit.cs` (MonoBehaviour) |
| `Mercenary.js` (데이터) | `Mercenary.cs` (직렬화 가능 클래스) |
| `BattleUnit.js` | `BattleUnit.cs` |

## 4. Unity 특화 활용

### 4.1 ScriptableObject
- 모든 데이터를 ScriptableObject로 정의
- 인스펙터에서 디자이너가 직접 편집
- 빌드 시 자동 포함

### 4.2 Addressable
- 장비/UI/이펙트는 Addressable로 관리
- 메모리 효율 + 패치 용이

### 4.3 UI Toolkit
- 마을/메뉴는 UI Toolkit (UXML/USS)
- 전투 UI는 UGUI (캔버스 기반)

### 4.4 DOTween
- 애니메이션/트윈은 DOTween 권장
- 데미지 팝업, HP바, UI 전환 등

### 4.5 Save System
- JSON 직렬화 (Newtonsoft.Json)
- 또는 BinaryFormatter (간단, 단 비추)
- 클라우드 저장 고려 (Steam Cloud 등)

## 5. 데이터 구조 호환

### 5.1 gameState 매핑

[game-state.md](game-state.md)의 JavaScript 구조 → C# 클래스:

```csharp
[System.Serializable]
public class GameState {
    public GuildHall guildHall;
    public int guildReputation;
    public List<Expedition> activeExpeditions;
    public List<ExpeditionResult> pendingResults;
    public Dictionary<string, int> stamina;
    public Dictionary<string, int> bonds;
    // ... 모든 필드
}

[System.Serializable]
public class GuildHall {
    public int operations;
    public int infrastructure;
    public int recovery;
    public int automation;
    public int intel;
    public int pit_control;
    public int cargo_control;
    public int dark_control;
}
```

### 5.2 Mercenary 클래스

```csharp
[System.Serializable]
public class Mercenary {
    public string id;
    public string name;
    public string classKey;
    public Rarity rarity;
    public int level;
    public int xp;
    public Equipment equipment;
    public Traits traits;
    public Dictionary<string, ZoneAffinity> zoneAffinity;
}
```

## 6. 시각/오디오 자산

### 6.1 캐릭터
- Phaser: 프로시저럴 Graphics 렌더링
- Unity: 스프라이트 또는 3D 모델 (선택)
- 권장 시작: 2D 픽셀 또는 일러스트 스프라이트

### 6.2 일러스트
- NPC, 이벤트, 보스 일러스트 필요
- AI 생성(SD/Midjourney) 또는 외주

### 6.3 사운드
- 전투 효과음, BGM (구역별)
- 마을 BGM, UI 효과음

## 7. 입력 시스템

- Unity New Input System 권장
- 키보드 + 마우스 + 게임패드 지원
- 단축키: 스킬 발동 1~7, ESC 메뉴 등

## 8. 빌드 타깃

- **1차**: Windows PC (Steam)
- **2차**: macOS, Linux
- **선택**: 모바일(터치 UI 추가 작업), 콘솔

## 9. Phaser → Unity 마이그레이션 워크플로

```
1. balance-config.md → Balance.cs ScriptableObject 생성
2. 모든 data/*.js → ScriptableObject 변환 (수동 or 스크립트)
3. 시스템 매니저 클래스 1:1 변환
4. 씬 구조 단순화 (UI 패널로 통합)
5. 전투 시스템 재작성 (Unity 물리/충돌 활용)
6. 시각 자산 교체 (Graphics → Sprites)
7. 사운드/이펙트 추가
8. Steam SDK 통합
```

## 10. 기획서 엔진 독립성 체크

본 기획서들은 **엔진 독립적** 작성됨. 확인된 항목:

- ✅ 모든 수치/공식 = 순수 로직 (엔진 무관)
- ✅ 시스템 간 데이터 흐름 = 추상적
- ✅ UI 흐름 = 화면 단위 설명 (구현 무관)
- ⚠️ Phaser 코드 참조는 일부 있음 (`scenes/`, `systems/` 경로 등)
- ⚠️ JavaScript 코드 예시는 의사 코드로 봐도 무방 (C# 변환 가능)

## 11. TODO (Unity 이식 시점)

- [ ] Phaser 프로토타입에서 12시간 플레이 검증
- [ ] 모든 데이터를 ScriptableObject로 변환
- [ ] 매니저 클래스 1:1 변환
- [ ] 전투 씬 재구성 (Unity 물리)
- [ ] 시각 자산 제작/구입
- [ ] 사운드 디자인
- [ ] Steam SDK 통합 (도전과제, 클라우드 저장)
- [ ] 빌드/배포 파이프라인 (Github Actions + Steamworks)
