---
name: qa
description: demo13-flashlight QA 자동 플레이를 지휘한다 — 게임에 플레이 명령을 내리고(qa-command.json), 결과(qa-result-latest.json)를 읽어 부족한 점·이상을 정리한다. 봇이 막히면(qa-blocked.json) 진단해 응답(qa-resume.json)한다. "QA 돌려줘", "5분 플레이시켜봐", "QA 결과 봐줘", "봇 막혔어" 같은 요청에 사용.
---

# QA 지휘 (Claude → 게임 → Claude)

demo13-flashlight의 QA 봇은 **내가 명령을 내리면 게임이 실제로 플레이하고 결과를 파일로 돌려준다.**
설계·프로토콜 전체는 `demo13-flashlight/docs/qa.md`.

## 파일 경로

Unity `Application.persistentDataPath` = **`%USERPROFILE%/AppData/LocalLow/Studio Pod Games/<제품명>/`**
(제품명은 `ProjectSettings/ProjectSettings.asset`의 `productName`. 정확한 경로는 게임 Console 첫 줄 `[QA]` 로그에 찍힌다.)

병렬 실행 시 `-qa-instance=A`를 주면 모든 파일명 앞에 `A-`가 붙는다.

| 파일 | 방향 | 내용 |
|------|------|------|
| `qa-command.json` | **Claude → 게임** | 실행할 시퀀스 |
| `qa-status.json` | 게임 → Claude | 진행 상황 하트비트(1초 갱신) |
| `qa-result-latest.json` | 게임 → Claude | 런 종료 결과(지표·이상 전부) |
| `qa-blocked.json` | 게임 → Claude | **막힘** — 봇이 응답을 기다리는 중 |
| `qa-resume.json` | **Claude → 게임** | 막힘에 대한 지시 |
| `qa-report-*.txt` | 게임 → 사람 | 사람이 읽는 리포트 |

## 1. 명령 내리기

사용자가 "QA 돌려줘" / "5분 플레이시켜" 라고 하면 `qa-command.json`을 쓴다.

```json
{
  "id": "run1",
  "note": "왜 이 시퀀스를 돌리는지 — 결과 리포트에 남는다",
  "scenario": {
    "name": "표준 순환",
    "seed": 20260711,
    "cycles": 3,
    "steps": [
      { "op": "cycle.begin" },
      { "op": "safehouse.ensure", "budgetSec": 25 },
      { "op": "inventory.organize", "budgetSec": 15 },
      { "op": "shop.sell", "ratio": 0.7 },
      { "op": "shop.buy", "param": "Medical", "ratio": 0.4, "count": 2 },
      { "op": "quest.accept", "count": 2 },
      { "op": "raid.enter", "param": "auto", "budgetSec": 25 },
      { "op": "raid.explore", "budgetSec": 150, "count": 6, "wander": true },
      { "op": "raid.extract", "budgetSec": 90 },
      { "op": "settle.verify" },
      { "op": "cycle.end" }
    ]
  }
}
```

**게임은 `-qa-serve`로 떠 있어야 한다.** 안 떠 있으면 사용자에게 실행을 요청:
```
game.exe -qa-serve -qa-instance=A -qa-minutes=5
```
(에디터에서는 F9로 기본 시나리오 1회 실행)

### 사용 가능한 op

| op | 파라미터 | 하는 일 |
|----|---------|--------|
| `cycle.begin` / `cycle.end` | — | 사이클 지표 구간(밸런스 추세용) |
| `title.newgame` / `title.continue` | count(슬롯) | 타이틀에서 새 게임/이어하기 — 신규 유저 경로 |
| `story.skip` | budgetSec | 프롤로그·대화·튜토 넘기기(막히면 STORY_STUCK) |
| `safehouse.ensure` | budgetSec | 안전가옥으로 복귀 |
| `inventory.organize` | — | 가방 정리(비의료·비소비를 창고로), 무게 초과 점검 |
| `shop.sell` | ratio, param(shopId) | 창고 잡템 판매(의료·열쇠 제외) |
| `shop.buy` | param(카테고리), ratio(소지금 비율), count | 상점 구매 |
| `quest.accept` | count | 게시판 의뢰 수주(BD 우선, 없으면 BQ) |
| `raid.enter` | param(regionId/auto), budgetSec | 레이드 진입 |
| `raid.explore` | budgetSec, count(상자 수), wander | 탐색·루팅(+배회) |
| `raid.extract` | budgetSec | 탈출구 이동·탈출 |
| `settle.verify` | — | 정산 확인 + 루팅 가치 집계 |
| `wait` | budgetSec | 대기 |

**op를 조합해 "무엇을 검증할지" 설계하는 게 핵심이다.** 예:
- 경제 검증 → cycles 5~10, shop.sell/buy 비중↑ → 돈 인플레·구매력 추세
- 길찾기 검증 → raid.explore budgetSec↑, wander=true, count↑ → STUCK/UNREACHABLE 다발 지점
- 루프 안정성 → cycles 3, 전 스텝 → 씬 전환·정산 누락

## 2. 결과 읽고 정리

`qa-result-latest.json` 구조:
- `cycles[]` — 사이클별 소지금/레벨/XP/루팅가치/구매·판매/상자 수/시간/완주 여부
- `anomalies[]` — Warn/Error만. `kind`로 분류

**이상 kind 사전**

| kind | 뜻 | 보통 원인 |
|------|----|---------|
| `EXCEPTION` / `LOG_ERROR` | 런타임 예외·에러 | 코드 버그 |
| `STUCK` | 이동 입력에도 3초 정지 | 벽 끼임·콜라이더 구멍 |
| `UNREACHABLE` / `NO_LOOT_REACHED` | 목표 도달 실패 | 길막힘·섬 지형 |
| `ITEM_LOST` | 아이템 개수 불일치 | 격자 로직 버그 |
| `SCENE_TIMEOUT` / `EXTRACT_TIMEOUT` | 씬 전환 실패 | 빌드세팅 미등록·탈출 배선 |
| `NO_EXIT` | 활성 탈출구 없음 | 스폰/탈출 매치 실패 |
| `EMPTY_RAID` / `LOOT_DROUGHT` | 루팅 가치 0 | 예산제·루트테이블 미동작 |
| `MONEY_INFLATION` | 매 사이클 순증가 | 소모처 부족 |
| `LEVEL_STALL` | XP 벌었는데 레벨 정체 | XP 곡선 과다 |
| `OVERWEIGHT` | 정리 후에도 무게 초과 | 가방 용량 밸런스 |
| `TOO_EXPENSIVE` | 예산으로 아무것도 못 삼 | 초반 경제 |
| `UNKNOWN_OP` | 모르는 op | 시나리오 오타 |

**정리 형식** — 사용자는 "부족한 점 / 없는 점"을 원한다. 다음 3덩이로 보고:
1. **깨진 것**(Error) — 고쳐야 진행 가능
2. **밸런스 신호**(Warn 추세) — 수치 조정 후보, GameTuning 필드까지 짚기
3. **없는 것** — 시나리오를 돌려보니 게임에 아예 빠져 있는 요소

## 3. 막힘 대응

`qa-blocked.json`이 있으면 봇이 **대기 중**(최대 90초). 읽고 진단한 뒤 `qa-resume.json`을 쓴다:

```json
{ "action": "retry", "note": "탈출구 비활성 원인 수정함 — 재시도" }
```

| action | 효과 |
|--------|------|
| `retry` | 계속 진행(같은 스텝 이어감) |
| `skip` | 넘어감(기본. 무응답 타임아웃도 skip) |
| `abort` | 런 중단 |

가능하면 **원인을 코드에서 찾아 고친 뒤** retry를 준다. 그게 이 루프의 목적이다.

## 4. 단계별 진행 원칙 (사용자 합의)

> 5분 돌려보고 → 부족한 점 정리 → 사용자가 보고 추가 → 또 진행.

- 한 번에 완성하려 하지 말고 **짧은 런 → 보고 → 확장**을 반복한다.
- 매 런마다 **직전에 없던 검증을 하나씩 추가**한다(op 조합 or 신규 op 제안).
- 신규 op가 필요하면 `Assets/Scripts/QA/QaSteps.cs`의 `Ops` 딕셔너리에 등록하고 사용자에게 알린다.
- QA는 **게임 코드를 건드리지 않는다**(입력은 `GameInput` 가상층 경유). 게임 버그를 고칠 땐 게임 코드를, QA 한계는 QA 코드를 고친다.
