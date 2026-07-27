# QA 시스템 — Claude 지휘 자동 플레이 + 자동 검증

> **목표**: "우리가 의도한 플레이가 실제로 그렇게 동작하는가"를 **Claude가 명령을 내려 확인**한다.
> 2026-07-11 게임성 검토가 지목한 3대 구멍 중 하나 — *"풀 루프를 엔진에서 완주한 기록이 없음"* — 의 해답.
>
> **진행 방식(사용자 합의)**: 짧게 돌려보고(5분) → 부족한 점 정리 → 사용자가 보고 추가 → 또 진행. **단계별 확장.**

## 전체 구성

| 축 | 도구 | 무엇을 잡나 | 실행 |
|---|------|------------|------|
| **지휘 플레이** | `QaBot` + `QaSteps` | **Claude가 시퀀스를 명령** → 게임이 실제 플레이 → 결과 회신. 동작 이상 + 밸런스 추세 | `game.exe -qa-serve` / F9 |
| **공간 분석** | `QaHeatmap` | **"어디서"** — 재미없는 파밍 구역·반복 지루함 시점·막히는 좌표·안 가는 공간 | 봇에 내장(자동) |
| **정적** | `IntegrityValidator` | 씬·데이터 **배선** 버그 | `Tools ▸ TopDown ▸ QA ▸ 정합성 검증` |
| **수치** | `QaBalanceSim` | 루트·경제·레벨 **분포**(헤드리스 수천 롤) | `Tools ▸ TopDown ▸ QA ▸ 밸런스 시뮬레이션` |
| **외부 GUI** | `tools/qa-dashboard.ps1` | PASS/FAIL 통계·실행중 인스턴스·문제좌표 | `localhost:8787` |
| **에이전트 루프** | `qa-runner` ↔ `dev-fixer` | QA→수정→재QA 자동 순환 + 라운드별 보고 | 스킬 `/qa-loop` |

> ⚠️ **2026-07-11 현재: Unity에서 컴파일·실행 미검증.** 첫 작업은 반드시 컴파일 확인 → 정합성 검증 → F9 1회.
> 첫 런은 거의 확실히 FAIL(지역1 미검증 + 봇 첫 실전) — 1~2라운드는 **QA 자체 교정**에 쓴다.
> 이어서 하려면 스킬 **`/QA이어서`**.

---

## 1. 가상 입력층 (`GameInput`) — 봇의 토대

**`GameInput`이 게임 전체 입력의 단일 관문**이라, 여기만 가로채면 봇이 **게임 코드 수정 0**으로 사람과 똑같은 경로로 조작한다(호출처 95곳 무변경).

```csharp
GameInput.Virtual = true;          // 켜면 실제 키보드/마우스/패드 무시(런 오염 방지)
GameInput.VSetMove(dir);           // 이동 축(아날로그)
GameInput.VTapKey(KeyCode.E);      // 1프레임 탭
GameInput.VSetMousePos(screenPos); // 조준
GameInput.VEndFrame();             // ★ LateUpdate에서 — 1프레임 플래그 수거
```

- **엄격한 옵트인**: `Virtual=false`(기본)면 기존 동작 그대로. 끄면 상태 자동 초기화.
- `VEndFrame`은 **LateUpdate** — 모든 Update 소비자가 Down을 본 뒤 걷어야 입력이 유실되지 않는다.
- `Virtual` 중엔 `Tick()`이 `PadActive=false` 고정 → 페이싱이 `mousePosition` 경로를 타고 봇이 조준한다.

## 2. 명령 프로토콜 (Claude ↔ 게임)

**게임 빌드에 API 키를 넣지 않는다.** 게임이 직접 API를 부르면 키가 빌드에 박히고 비용·오프라인 문제가 생긴다.
대신 **파일 채널**: 게임 ↔ 개발 머신의 Claude Code. QA 머신이 여러 대여도 같은(공유) 폴더만 보면 된다.

위치 = `Application.persistentDataPath` (Console 첫 `[QA]` 로그에 실경로가 찍힌다).
`-qa-instance=A`를 주면 모든 파일에 `A-` 접두 → **병렬 실행 충돌 없음**.

| 파일 | 방향 | 내용 |
|------|------|------|
| `qa-command.json` | **Claude → 게임** | 실행할 시퀀스(`{id, note, scenario}`) |
| `qa-status.json` | 게임 → Claude | 1초 하트비트(현재 스텝·사이클·소지금·경과) |
| `qa-result-<id>.json` / `-latest` | 게임 → Claude | 런 결과 — 사이클 지표 + 이상 전부 |
| `qa-blocked.json` | 게임 → Claude | **막힘**(최대 90초 대기) + 재현 정보 |
| `qa-resume.json` | **Claude → 게임** | `{action: retry\|skip\|abort, note}` |
| `qa-report-*.txt` | 게임 → 사람 | 사람이 읽는 리포트 |

Claude 쪽 사용법은 스킬 **`/qa`** (`.claude/skills/qa/SKILL.md`).

### 실행 모드

```bash
game.exe -qa-serve -qa-instance=A -qa-minutes=5   # 명령 대기(권장) — Claude가 지휘
game.exe -qa                                       # 시나리오 1회
game.exe -qa -qa-quit                              # 무인/CI (오류 시 exit 1)
```
에디터에서는 **F9** 시작/중단.

## 3. 시나리오 = 플레이 시퀀스

`QaScenarioDef` (JSON). 로드 우선순위: `persistentDataPath/qa-scenario.json` → `Resources/QA/qa-scenario.json` → 코드 내장.
한 사이클 = **정비(인벤·상점·퀘스트) → 모험(레이드) → 귀환(정산)**. `cycles`회 반복하며 사이클 지표를 쌓아 **밸런스 추세**를 본다.

| op | 파라미터 | 하는 일 |
|----|---------|--------|
| `cycle.begin` / `cycle.end` | — | 사이클 지표 구간 |
| `safehouse.ensure` | budgetSec | 안전가옥 복귀 |
| `inventory.organize` | — | 비의료·비소비를 창고로, 무게 초과 점검 |
| `shop.sell` | ratio, param(shopId) | 창고 잡템 판매(의료·열쇠 제외, `sellRate` 적용) |
| `shop.buy` | param(카테고리), ratio, count | 구매(`BuyPrice` 적용) |
| `quest.accept` | count | 게시판 수주(BD→BQ) |
| `raid.enter` | param(regionId/auto) | 레이드 진입 |
| `raid.explore` | budgetSec, count, wander | 탐색·루팅(+배회) |
| `raid.extract` | budgetSec | 탈출 |
| `settle.verify` | — | 정산 + 루팅 가치 집계 |
| `wait` | budgetSec | 대기 |

**자유도**: 상자 선택은 "가까운 3개 중 무작위", `wander`면 사이사이 무작위 배회, 구매도 후보 중 무작위.
전부 **시드 고정**이라 같은 시드 = 같은 플레이(재현 가능).

## 4. 감지하는 이상

**동작** — `EXCEPTION`/`LOG_ERROR` · `STUCK`(3초 정지, 자동 옆빠지기 시도) · `UNREACHABLE`/`NO_LOOT_REACHED` · `ITEM_LOST` · `SCENE_TIMEOUT`/`EXTRACT_TIMEOUT` · `NO_EXIT` · `NAN_POS` · `UNKNOWN_OP`

**밸런스(추세, 사이클 누적)** — `LOOT_DROUGHT`(완주했는데 가치 0) · `MONEY_INFLATION`(매 사이클 순증가 + 5배 초과) · `LEVEL_STALL`(XP 벌었는데 레벨 정체) · `CYCLE_SLOWDOWN`(시간 3배 증가) · `RAID_INCOMPLETE` · `OVERWEIGHT` · `TOO_EXPENSIVE` · `ZERO_PRICE`

## 5. 제거·차단 (나중에)

- **런타임 차단**: 인자 없으면 봇은 아무것도 안 한다(`-qa*` 필요). F9도 개발 편의용 — 릴리스에서 빼려면 `QaBot.Update`의 F9 블록 제거.
- **코드 제거**: `Assets/Scripts/QA/` 폴더 통째로 삭제 가능. **게임 코드가 QA를 참조하지 않는다**(봇이 자가부트, 단방향 의존) → 삭제해도 컴파일 깨지지 않음.
- **입력층**: `GameInput`의 가상 블록(~70줄)만 게임 코드에 남는다. `Virtual=false`면 완전 무동작. 함께 지우려면 `V*` 멤버 + 각 getter의 `if (_virtual)` 조기 반환 10곳 삭제.
- (선택) `Assets/Scripts/QA/`에 asmdef + `defineConstraints: ["BRB_QA"]`를 두면 define 없는 빌드에서 **어셈블리째 제외**된다.

---

## 결정 로그

| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-07-11 | QA를 어떤 형태로? | **정적 정합성 검증기 먼저**(`IntegrityValidator`) — 지역1 작업에서 놓친 배선 버그 자동화. |
| 2026-07-11 | 빌드된 실행 파일을 실제 플레이하는 QA | **가상 입력층 + `QaBot`** — `GameInput` 단일 관문을 가로채 게임 코드 수정 0. |
| 2026-07-11 | 봇 1차 시나리오 범위 | 루프 스모크 + 인벤 조작. |
| 2026-07-11 | 밸런스 검증 방식 | **헤드리스 시뮬 병행**(`QaBalanceSim`) — 실플레이는 표본이 적어 밸런스 판단에 불리. |
| 2026-07-11 | **재정의**: QA = 정해둔 시퀀스로 모험·정비를 순환하며 밸런스를 보는 것. **Claude가 명령을 내려** 의도대로 도는지 확인. 여러 개 병렬. 5분 돌려보고 부족한 점 정리 → 추가 → 반복. | **명령 프로토콜 채택** — `qa-command/status/result/blocked/resume` 파일 채널 + `-qa-serve` 대기 모드 + `-qa-instance`로 병렬. 시나리오는 op 조합 JSON(재컴파일 불필요). Claude 쪽은 스킬 `/qa`. **API 키는 빌드에 넣지 않음.** |

---

## 구축 기록 (2026-07-11 세션)

사용자 요구가 대화 중 3번 확장됐고, 그때마다 설계를 갈아엎지 않고 **얹는 방식**으로 대응했다.

| 단계 | 사용자 요구 | 대응 |
|---|---|---|
| 1 | "QA 프로그램 하나 만들어보자" | 정적 배선 검증기(`IntegrityValidator`) — 직전 지역1 작업에서 놓친 부류(스폰 미주입·유닛키 부재·빌드세팅 누락) 자동화 |
| 2 | "빌드된 파일을 **실제 플레이**해서 밸런스·이상 찾고 인벤 이동도" | `GameInput` **가상 입력층** + `QaBot` — 게임 코드 수정 0으로 사람과 같은 경로 조작 |
| 3 | "**내가 명령**해서 의도대로 도는지 확인. 여러 개 병렬. 5분 돌려보고 부족한 점 정리 → 추가 → 반복" | **명령 프로토콜**(qa-command/status/result) + `-qa-serve` 대기 모드 + `-qa-instance` 병렬 + op 조합 시나리오 JSON |
| 4 | "어디서 반복하면 재미없다 / 어디서 파밍하면 재미없다 / 어디서 멈춘다 — 이런 데이터. **반복성·밸런스·맵 밸런싱은 내가 못 한다**" | `QaHeatmap` **공간 분석** — 16m 격자에 체류·획득·스턱·사망 누적 → DEAD_ZONE·NOVELTY_ZERO·STUCK_HOTSPOT·SPARSE_PATH |
| 5 | "**QA 에이전트 ↔ 개발 에이전트** 자동 루프 + 중간 보고 + **외부 GUI**" | `qa-runner`/`dev-fixer` 에이전트 + `/qa-loop` 오케스트레이션 + PowerShell 대시보드(PASS/FAIL 통계) |

### 리뷰가 잡은 치명적 결함 (기록 — 재발 방지)

1. **프레임 순서로 봇 입력 전량 유실** — `Update() → 코루틴 → LateUpdate()`라 코루틴에서 누른 키를 같은 프레임 LateUpdate가 걷어버려 **어떤 소비자도 못 봄**(시나리오 전체 무동작). → **프레임 스탬프**로 이전 프레임 것만 수거.
2. **timeScale=0 영구 행** — `MoveTo`가 `Time.deltaTime` 기반이라 일시정지 시 타임아웃도 못 함 → unscaled로 교체.
3. **`_virtual`이 도메인 리셋에서 누락** — 봇 중단 시 가상 입력이 남아 **실제 키보드·마우스 전체 먹통**.
4. **봇이 진짜 세이브를 덮어씀** — 테스트 아이템·비운 상자가 디스크 커밋됨 → `SaveManager.SuppressWrites`.
5. **릴리스 빌드에서 F9 노출** · **예외 1건에 런 사망** · **밸런스 시뮬 에디트모드 경고 10만건** · **검증기가 사용자 씬 구성 변경**.

### 작업 중 사고 (교훈)

- `perl -0pi -e`로 C# 치환 중 **`$"..."` 문자열 보간의 `$`가 perl 변수로 먹혀** 코드가 깨졌다(`_rep?.Warn(..., 사망 — {..}")`). → **C# 코드 치환에 perl/sed 쓰지 말 것. Edit 도구 사용.**
- 병렬 자동커밋 프로세스가 QA 파일을 다른 커밋(`1bca421`)에 쓸어담았다. 커밋 전 `git diff --cached --name-only` 확인 필수.
