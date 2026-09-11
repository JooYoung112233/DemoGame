# 게임 튜닝 (Control Panel)

흩어진 밸런스/타이밍 값을 **한 곳에서 보고 조절**하는 중앙 패널. (코드 뒤져서 고치던 것 → 한 창)

## 구조
- **`GameTuning`** (SO, `Resources/Data/GameTuning.asset`) — 단일 튜닝 소스. 각 시스템은 `GameTuning.Instance.X`를 읽고, **에셋이 없으면 자체 기본값으로 폴백**(크래시 없음).
- **Control Panel** (`Tools ▸ TopDown ▸ Control Panel`, `Editor/GameControlPanel.cs`) — GameTuning을 `SerializedObject`로 그려 편집(필드 추가 시 자동 노출) + 열린 씬 오브젝트 통계.
- **플레이 중 실시간**: 수색 속도는 즉시 반영(매 공개 시 읽음). 낮밤/레이드 시간은 다음 init/레이드부터.

## 현재 튜닝 값 (연동 완료)
| 값 | 영향 | 읽는 곳 |
|---|---|---|
| `searchSpeedMult` | 루팅 목록 수색(아이템 공개) 속도. 한 칸 시간 = `searchSec*` ÷ 이 값 → >1 빠르게, <1 느리게 | `LootListUI.RevealDelay` |
| `dayDuration` / `nightDuration` | 지역 낮·밤(현상) 길이(초) | `RegionTimeManager.InitRegions` |
| `raidDuration` | 레이드 제한 시간(초) | `RaidManager.Start` |
| `lootCountMult` | 맵 루팅 예산(뽑기 횟수) 배율 — 바닥·상자 둘 다 | `MapSpawnController.ExecuteSpawn` |

## 값 추가법 (확장)
1. `GameTuning.cs`에 필드 하나 추가.
2. 쓰는 시스템에서 `GameTuning.Instance.필드` 읽기(없으면 기본값).
3. Control Panel에 **자동 노출** — 별도 UI 코드 불필요.

> 예) **문 개방 시간**: 지금 문은 즉시 열림 → `DoorController`에 개방 연출 + `GameTuning.doorOpenSeconds` 추가하면 패널에서 조절. **짙은현상 강도/전환속도**(`DenseAnomalyController`)도 같은 방식으로 편입 가능.

## 맵 통계
열린 씬의 **인터랙터블(종류별)·루트상자·스폰·적·아이템스폰·파괴가능·Light2D·Tilemap·SpriteRenderer** 개수 집계(Systems+게임플레이 씬 같이 열려 있으면 합산).

## 변경 로그
| 날짜 | 질문 | 결정 | 근거 |
|---|---|---|---|
| 2026-09-11 | (문서 정합) 루팅 정리로 읽는 곳이 바뀜 | `searchSpeedMult` → `LootListUI.RevealDelay`(한 칸 시간 = searchSec* ÷ 값, >1 빠름 — 예전 ">1 느리게" 서술은 반대였음). `lootCountMult` → `MapSpawnController` 맵 예산 배율(`RegionLootBootstrap` 삭제) | [region-loot.md §루팅 정리 결정](region-loot.md), 전체 표는 [balance.md §2](balance.md) |
| 2026-06-05 | 수색 속도가 너무 빠르고, 문 개방·현상 전환 등 타이밍/드랍을 코드 뒤져 고쳐야 함 — 한 곳에서 보고 조절·추가하고 싶음 | **중앙 `GameTuning` SO + Control Panel(Tools▸TopDown▸Control Panel)** 신설. 시스템은 `GameTuning.Instance` 읽고 폴백. 1차 연동: 수색속도·낮밤길이·레이드시간·드랍배율. 맵 통계 집계 포함 | 디자이너가 한 창에서 실시간 튜닝, 값 추가가 쉬움(SO 필드만 추가→자동 노출). 단일 소스로 흩어짐 방지. |
