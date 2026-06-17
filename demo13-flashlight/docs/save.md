# 저장 / 체크포인트 시스템 (save.md)

> 세이브 스커밍(전투/거래 직전 저장 → 결과 나쁘면 재로드) 방지 + 크래시 복구를 함께 만족하는 저장 모델.
> 핵심 코드: `Systems/SaveManager.cs`(직렬화·디스크 I/O), `Systems/SaveCheckpoints.cs`(체크포인트 정책), `Systems/CombatStateTracker.cs`(전투 감지).

## 1. 설계 결정 (2026-06-18)

질문: 자동 저장을 매 이벤트마다 디스크에 쓰면 세이브 스커밍이 가능하고(전투 직전 저장 후 재시도), 매번 안 쓰면 크래시 시 진행을 잃는다. 어떻게 양립시키나?

결정: **"안전 맥락에서만 디스크에 쓴다 + 레이드 진행은 인메모리 스냅샷 + 크래시 시에만 인메모리를 디스크로 커밋"** 의 3겹 모델.

- 디스크 저장(Commit)은 **안전한 시점**에만: 레이드 시작/종료, 안전가옥 이벤트(건물·퀘스트), 침대 수면.
- **레이드 중**(InRaid)의 진행 이벤트는 **인메모리 스냅샷(Record)** 만 갱신 — 디스크엔 쓰지 않는다.
  - → 레이드 중 죽거나 강제 종료하면 디스크엔 **레이드 시작본**만 남아 있어, 다음 로드 시 **레이드 시작 시점**으로 복귀(세이브 스커밍 차단).
- 단, **예외(크래시)** 발생 시에는 인메모리 스냅샷을 **1회** 디스크로 커밋(의도치 않은 크래시로 진행을 통째로 잃지 않게).
- **침대 수면**은 안전가옥 시설이므로 명시적 수동 저장(항상 Commit).

근거: Tarkov류 게임의 핵심 긴장은 "레이드 중 결과를 되돌릴 수 없음"에서 온다. 인메모리 스냅샷은 크래시 복구만 담당하고, 정상 종료/사망은 항상 레이드 시작 기준점으로 되돌린다.

## 2. InRaid(위험 맥락) 판정

`SaveCheckpoints.InRaid` = 현재 게임플레이 씬이 **레이드 씬**인가.

```
InRaid = SystemsScene.IsGameplayScene(scene) && scene.name ∉ { Safehouse, Hideout, Pawnshop }
```

- 안전가옥(`Safehouse`) / 은신처(`Hideout`) / 전당포 실내(`Pawnshop`) = **비-레이드(안전 맥락)**.
- 그 외 게임플레이 씬(`ScrapMarket_GB` / `Zone1` / `InGameScene` 등 레이드 맵) = **레이드**.
- `SceneManager.sceneLoaded` 구독으로 게임플레이 씬 로드 시 갱신. (Systems/맵툴 씬 로드는 무시)
- 명시 훅(`RaidStarted`/`RaidEnded`)이 `_inRaid`를 직접 토글하므로, 씬 판정은 보조/초기화 역할.

## 3. 이벤트별 정책

| 이벤트 | 호출원 | 레이드 중(InRaid) | 안전 맥락 |
|---|---|---|---|
| `RaidStarted()` | RaidManager.Start | **Commit** (시작 기준점) + InRaid=true, 스냅샷 비움 | — |
| `RaidEnded()` | RaidManager.OnExtractSuccess | **Commit** (정산 확정) + 스냅샷 비움, InRaid=false | — |
| `BuildingEntered()` | (트랙 B) 건물 진입 | Record | Commit |
| `BuildingExited()` | (트랙 B) 건물 퇴장 | Record | Commit |
| `QuestAccepted()` | QuestManager.AcceptQuest 성공 | Record | Commit |
| `QuestCompleted()` | QuestManager.CompleteQuest 성공 | Record | Commit |
| `CombatStarted()` | CombatStateTracker | **항상 Record** | 항상 Record |
| `CombatEnded()` | CombatStateTracker | **항상 Record** | 항상 Record |
| `BedSleepSave()` | SleepUI.DoSleep | **Commit** (수동 저장) | Commit |

- **Commit** = `SaveManager.Save()` (디스크 즉시 쓰기).
- **Record** = `_raidSnapshotJson = SaveManager.ToJson(SaveManager.BuildSaveData())` (디스크 X, 인메모리만).

## 4. 크래시 / 강제 종료

- **크래시(예외)**: `Application.logMessageReceived` 구독 → `LogType.Exception` && `InRaid` && 인메모리 스냅샷 존재 시 `SaveManager.WriteJson(_raidSnapshotJson)`로 **1회** 복구 커밋(`_crashCommitted` 가드).
- **강제 종료/전원 차단**: `OnApplicationQuit()` = **no-op**. 레이드 중이면 디스크엔 레이드 시작본만 남아 다음 로드 시 레이드 시작 지점으로 복귀. 비-레이드면 이미 이벤트성 커밋이 끝난 상태이므로 추가 저장 불필요.

## 5. 전투 감지 (CombatStateTracker)

- 0.5초 폴링. 레이드 씬(`SaveCheckpoints.InRaid`)에서만 동작.
- **교전 판정**: 활성 `EnemyController` 중 하나라도 `IsEngagingPlayer`(상태 Chase/AttackWindup/Attack && player 살아있음)면 전투중.
- **시작 에지**: 비전투→전투 전이 시 `CombatStarted()`.
- **종료**: 교전이 끊긴 시점부터 **30초**(`CombatEndGrace`) 경과 후 `CombatEnded()`. 그 사이 다시 교전되면 타이머 리셋.
- `EnemyController.IsEngagingPlayer`(읽기 전용 프로퍼티)만 추가, 상태 머신은 비수정.

## 6. SaveManager 리팩터 (직렬화/디스크 분리)

기존 `Save()`(구성+직렬화+쓰기 결합)를 분리:

| 메서드 | 역할 |
|---|---|
| `GameSaveData BuildSaveData()` | 현재 상태 → 스냅샷 수집 (디스크 X) |
| `string ToJson(GameSaveData)` | 스냅샷 → JSON 문자열 (디스크 X) |
| `void WriteJson(string json)` | JSON 문자열을 그대로 디스크에 기록 |
| `void WriteToDisk(GameSaveData)` | `WriteJson(ToJson(data))` |
| `void Save()` | `WriteToDisk(BuildSaveData())` (기존 호출부 호환 래퍼) |

→ 기존 `Save()`/`AutoSave()` 호출부는 그대로 동작. `Record()`는 `ToJson(BuildSaveData())`로 디스크 없이 스냅샷만, 크래시 복구는 보관해둔 JSON을 `WriteJson()`으로 재수집 없이 커밋.

## 7. 부트스트랩

`SaveCheckpoints` / `CombatStateTracker` 모두:
- `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 자가 부트스트랩(맵툴 씬 제외, 중복 가드) → Systems 씬 / 폴백 양쪽에서 1개 보장.
- 폴백 경로(`GameBootstrap.Init`)에도 `EnsureSingleton`으로 등록.
- `DontDestroyOnLoad`.

## 8. 기존 자동저장과의 관계 (중복 방지 메모)

- `SleepUI.DoSleep` 의 직접 `SaveManager.AutoSave()` → `SaveCheckpoints.BedSleepSave()`로 교체(없으면 폴백).
- `StoryTriggerManager.OnBedRest()` / `StoryPlayer`(스토리 트랙 소유)는 여전히 자체 `AutoSave()`를 호출 — 모두 **안전가옥/내러티브 안전 시점**이라 추가 Commit이어도 무해(같은 안전 상태를 다시 씀). 레이드 중 폭주 저장은 없음(레이드 이벤트는 Record만).
- 비-레이드 Commit은 이벤트성(진입/완료/수면)으로만 발생 — 프레임 단위 저장 폭주 아님.

---

## 변경 로그
- 2026-06-18: 최초 작성. 저장 체크포인트 모델(안전=Commit/레이드=Record/크래시=복구커밋/강제종료=레이드시작복귀) + CombatStateTracker + SaveManager 직렬화·디스크 분리.
