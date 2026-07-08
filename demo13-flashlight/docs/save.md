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
| `InventoryChanged()` | 상점 구매·판매·위탁·수배(ShopUI 5곳) / F1 디버그 아이템 변경(DebugTestUI 4곳) | Record | Commit |

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

## 7.5 새 게임 상태 초기화 (2026-07-08)

**문제**: 세이브 로드는 파일에서 각 시스템 `LoadSaveData`로 상태를 씌우지만, **새 게임은 세이브가 없어 아무것도 안 씌운다.** 그런데 매니저들은 `DontDestroyOnLoad`라 **같은 세션에서 '새 게임'을 다시 누르면 이전 판의 인메모리 상태(돈·특성·레벨·평판·인벤·퀘스트…)가 그대로 이월**됐다(첫 실행만 깨끗). `OnNewGame`은 `DeleteSave` + `ResetSession`만 했지 인메모리를 안 건드림.

**해결**: `SaveManager.ResetToNewGame()` — `Load()`가 복원하는 **영속 시스템 전체를 기본값으로 초기화**. `TitleScreen.OnNewGame`에서 `DeleteSave` 뒤 호출.
- 대상(=Load 목록 1:1): Currency/Reputation/PlayerProgress/Trait, NPCRelationship/Achievement/HideoutModule/DailyQuest/Quest(+flags)/StoryPlayer playedScenes/TutorialPrompt, ArbeitBoard·QuestBoard 런타임, MainStash·ShopUI(위탁/트레이), 플레이어 장비 해제·격자 클리어·생존/체력/의료 복귀, QuickSlot.
- **씬-로컬(안전가옥 가구 `SafehouseStorage` 등)은 제외** — DontDestroyOnLoad 아님 → 새 안전가옥 로드 시 빈 격자로 자연 재생성.
- **null-guard 함정**: 일부 `LoadSaveData`는 `data==null`이면 clear 없이 early-return(NPCRel/Achievement/Daily/Trait) → 각 시스템에 명시 `ResetForNewGame()` 추가(또는 Trait은 `new TraitSaveData()`). ⚠ **Load에 새 영속 시스템을 추가하면 ResetToNewGame에도 반드시 대응 추가.**

## 8. 기존 자동저장과의 관계 (중복 방지 메모)

- `SleepUI.DoSleep` 의 직접 `SaveManager.AutoSave()` → `SaveCheckpoints.BedSleepSave()`로 교체(없으면 폴백).
- `StoryTriggerManager.OnBedRest()` / `StoryPlayer`(스토리 트랙 소유)는 여전히 자체 `AutoSave()`를 호출 — 모두 **안전가옥/내러티브 안전 시점**이라 추가 Commit이어도 무해(같은 안전 상태를 다시 씀). 레이드 중 폭주 저장은 없음(레이드 이벤트는 Record만).
- 비-레이드 Commit은 이벤트성(진입/완료/수면)으로만 발생 — 프레임 단위 저장 폭주 아님.

---

## 9. 저장 슬롯 (2026-07-08)

- **질문**: 저장 슬롯 개념 도입 — 새 게임 시 슬롯 리스트. 슬롯 3개. 슬롯 카드엔 캐릭터 레벨·특성·돈 표시. '이어하기'도 슬롯 쓰나?
- **결정**: **슬롯 3개. 새 게임·이어하기 둘 다 슬롯 화면.** 새 게임=슬롯 선택 후 시작(찬 슬롯이면 덮어쓰기 확인), 이어하기=찬 슬롯만 로드(빈 슬롯 비활성). 슬롯 카드 표시 = **Lv N · 특성 M개 · ◈ 돈 · 저장 시각**(빈 슬롯="비어 있음").
- **구현**:
  - `SaveManager`: 파일 슬롯화 `save_{0..2}.json`, `CurrentSlot`/`SetSlot(int)`, `HasSave(slot)`/`DeleteSave(slot)`, **`PeekSlot(slot)`**(전체 로드 없이 요약만 역직렬화 → level/특성수/currency/saveTime). 자동저장·`Load()`·`ResetToNewGame()`은 CurrentSlot 대상. 구 단일 `save.json`은 슬롯0로 1회 마이그레이션.
  - 슬롯 화면 = **TitleScreen 런타임 오버레이**(매번 재생성·닫으면 파괴 — 재베이크 의존 없음, 프리팹 지뢰 회피). 새 게임=슬롯 선택→`SetSlot`+`DeleteSave`+`ResetToNewGame`→프롤로그, 이어하기=슬롯 선택→`SetSlot`→`Load`. `GameStartHandler`는 `HasSave(CurrentSlot)`로 로드/프롤로그 분기.

## 10. 스팀 클라우드 (2026-07-08 — 백엔드 방향 확정)

- **결정**: 출시 시 세이브는 **스팀 클라우드** 사용. 방식은 **Auto-Cloud(경로 기반)** 우선 — 런타임 Steamworks 코드 없이, 지금처럼 **파일로 `persistentDataPath`에 저장**하고 Steamworks 파트너 설정에서 경로/글로브를 등록하면 스팀이 자동 동기화한다. (API 방식 `ISteamRemoteStorage`는 파일 단위 명시 read/write가 필요해 후순위 — Auto-Cloud로 충분.)
- **현재 설계가 이미 클라우드 친화적**: ①파일 기반(`save_0..2.json` + `.tmp` 제외) ②슬롯 3개로 **파일 세트 소수·고정**(쿼터/파일수 안전) ③`persistentDataPath` 상대 경로(절대경로 가정 없음).
- **적용된 하드닝**: `WriteAtomic`(임시파일→`File.Replace`) — 쓰기 도중 크래시/클라우드 동기화가 겹쳐도 **반쪽 파일 미생성**. 클라우드가 파손 세이브를 동기화·전파하는 사고 방지.
- **출시 전 TODO(유니티/파트너 설정 — 코드 아님)**:
  - ✅ `companyName` = **`Studio Pod Games`**(2026-07-08 확정, `DefaultCompany`에서 변경). `persistentDataPath` = `%userprofile%/AppData/LocalLow/Studio Pod Games/demo13-flashlight/`. ⚠ 변경으로 **경로가 바뀌어 구 `DefaultCompany` 경로의 세이브는 새 경로에서 안 보임**(개발 세이브라 무해, 필요 시 수동 이동). productName·`applicationIdentifier`(스팀 번들ID)는 게임 정식 타이틀 확정 시 별도.
  - Steamworks 파트너: Auto-Cloud 루트 = `WinAppDataLocalLow`(또는 플랫폼별), 패턴 `save_*.json`. 쿼터·파일수 상한 설정.
  - `.tmp` 파일은 클라우드 글로브에서 **제외**(패턴을 `save_?.json`로 좁혀 자동 제외됨).
  - 충돌 해결 UI(같은 슬롯 다른 기기 동시 편집)는 스팀 기본 처리에 의존 — 필요 시 후속.
- **호환 유지 규칙**: 세이브는 항상 파일 기반·`persistentDataPath`·소수 고정 파일명으로 유지. 절대경로·다수 동적 파일·프로세스 종료 시점 쓰기(OnApplicationQuit)는 클라우드와 상성이 나쁘니 금지(현 모델도 종료 저장 no-op이라 정합).

## 변경 로그
- 2026-07-08: **스팀 클라우드 백엔드 방향 확정(§10)** + 원자적 쓰기(`WriteAtomic`) 도입. Auto-Cloud(경로 기반) 전제 — 파일 기반 유지, 출시 전 companyName·파트너 설정 TODO.
- 2026-07-08: **저장 슬롯 3개 + `ResetToNewGame` 이월 차단.** SaveManager 슬롯화(save_0..2, CurrentSlot, PeekSlot 요약), TitleScreen 슬롯 오버레이(새 게임/이어하기 공용). §7.5·§9 참조.
- 2026-06-30: **`InventoryChanged()` 훅 추가** — 상점 거래(구매/판매/위탁 정산/수배 매입)·F1 디버그 아이템 변경(인벤·창고 비우기/지급) 시 `RecordOrCommit()`(안전구역=디스크 커밋, 레이드=인메모리). 기존엔 거래·F1 변경이 자동 저장 안 돼 다음 로드 시 유실되던 것 보완. ShopUI 5곳·DebugTestUI 4곳 연결.
- 2026-06-18: 최초 작성. 저장 체크포인트 모델(안전=Commit/레이드=Record/크래시=복구커밋/강제종료=레이드시작복귀) + CombatStateTracker + SaveManager 직렬화·디스크 분리.
