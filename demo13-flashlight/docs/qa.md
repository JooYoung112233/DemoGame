# QA 시스템 — Claude 지휘 자동 플레이 + 자동 검증

> ## ⚠️ 2026-09-09 — 게임 런타임에서 **분리**됨 (`QA_ENABLED`)
> 볼륨 축소([`scope-cut.md`](scope-cut.md) 6번). QA 봇 4,215줄이 게임 런타임 어셈블리에 그대로
> 실려 있던 걸 **별도 어셈블리 `Game.QA`**(`Assets/Scripts/QA/Game.QA.asmdef`)로 떼어내고
> `defineConstraints: ["QA_ENABLED"]`를 걸었다. **평소 빌드에는 QA 코드가 한 줄도 안 들어간다.**
>
> **켜는 법**: `Tools ▸ TopDown ▸ QA ▸ QA 자동화 켜기 (QA_ENABLED)` 토글
> (= Player Settings의 Scripting Define Symbols에 `QA_ENABLED` 추가/제거).
> 끈 상태에서는 `QaBot`/`QaBridge` 등이 아예 컴파일되지 않으므로 F9·`-qa-serve`도 동작하지 않는다.
>
> **삭제가 아니라 분리인 이유**: `QaBot`은 **빌드된 게임 안에서** 돌아야 해서(`-qa-serve`)
> 에디터 전용 어셈블리로 옮길 수 없고, 지우면 `/qa`·`/qa-loop`·`qa-runner`↔`dev-fixer` 워크플로가
> 통째로 죽는다. 게임 코드는 QA를 **한 곳도 참조하지 않으므로**(역참조 0) 분리는 무손실이다.
> 완전 삭제를 원하면 `Assets/Scripts/QA/` 폴더 + `tools/qa_*` 제거로 끝난다.

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
| **마더(총괄 GUI)** | `tools/qa_mother_gui.py` | 차일드 N개 등록·기동·투입·감시 + 판정/이상/원본로그를 **마더가 직접 읽음** | `tools/QA-Mother.bat` |
| **마더(CLI)** | `tools/qa_orchestrator.py` | 같은 기능의 명령줄판(에이전트가 `--json`으로 사용) | `py tools/qa_orchestrator.py status` |
| **외부 GUI** | `tools/qa-dashboard.ps1` | PASS/FAIL 통계·실행중 인스턴스·문제좌표(웹) | `localhost:8787` |
| **에이전트 루프** | `qa-runner` ↔ `dev-fixer` | QA→수정→재QA 자동 순환 + 라운드별 보고 | 스킬 `/qa-loop` |

### 마더 ↔ 차일드

```
마더(창 1개) ──명령 파일──▶ 차일드 N개 (Unity 에디터 F9 / 빌드 exe -qa-serve)
             ◀─상태·결과──┘
```

차일드가 떨구는 파일을 마더가 **1초마다 스스로 읽는다** — 사람이 로그를 옮겨 붙일 필요 없다.

| 파일 | 방향 | 내용 |
|------|------|------|
| `{인스턴스-}qa-command.json` | 마더→차일드 | 투입할 시나리오 |
| `{인스턴스-}qa-status.json` | 차일드→마더 | 1초 하트비트(상태·스텝·씬·좌표·최근 이벤트) |
| `{인스턴스-}qa-result-*.json` | 차일드→마더 | 판정(PASS/FAIL)·체크 8종·이상·공간 셀 |
| `{인스턴스-}qa-report-*.txt` | 차일드→마더 | 사람이 읽는 **전체 로그**(마더 "원본 로그" 탭) |
| `{인스턴스-}qa-blocked.json` ↔ `qa-resume.json` | 양방향 | 차일드가 막히면 질문 → 마더 배너에서 재시도/건너뛰기/중단 |

- **차일드 여러 개**: 이름이 곧 파일 접두(`A-qa-command.json`)라 충돌 없음. 마더 창 `[＋추가]`로 등록하고 `exePath`만 넣으면 N개로 확장. `시드 분산`을 켜면 같은 시나리오라도 차일드마다 다른 플레이가 된다.
- 런이 끝났는데 `qa-blocked.json`이 남으면 **잔재**로 판정해 배너를 띄우지 않는다(`[잔재 정리]`로 삭제).

### 봇 자가 복구 (2026-07-27)

사람이 안 붙어 있어도 런이 끝까지 굴러가야 "어디서 막히나"뿐 아니라
**"막히고 나서 어디까지 가나"** 까지 데이터가 남는다. 막히면 스스로 사다리를 탄다.

| 단계 | 동작 | 로그 |
|---|---|---|
| 0 | 벽 끼임 탈출 넛지(옆으로 밀기) | `STUCK` / `OSCILLATION` |
| 1 | **세이브 로드 → 이어서 재시도** | `RECOVER_LOAD` |
| 2 | **세이브 로드 → 사이클을 스텝0부터 다시** | `RECOVER_RESTART` + `CYCLE_RESTART` |
| 3 | 그래도 안 되면 마더에게 질문(기존 handshake) | `qa-blocked.json` |

- 복구 예산은 **사이클마다 초기화**, 사이클 재시작은 런당 **2회 상한**(`RESTART_CAP`)
- 레이드 중 복구하면 그 레이드는 버리고 안전가옥으로 — 사람이 막혔을 때 하는 것과 같다

**와리가리(제자리 진동) 감지** — 위치가 계속 바뀌므로 STUCK(정지)으로는 안 잡힌다.
6초 창에서 `이동거리 ≥ 8m` 인데 `순이동 < 2m` 이면 `OSCILLATION`.

### 길찾기 격자 자동 기록

씬마다 `NavGrid` 상태를 결과 JSON(`navGrids[]`)에 1회 싣는다. 사람이 콘솔을 뒤질 필요가 없다.
봇이 A* 방향을 받고도 제자리일 때 **원인을 가르는 유일한 값**이다.

| 막힘 % | 뜻 | 볼 곳 |
|---|---|---|
| 격자 없음 | 적·NPC·봇 전부 직선 이동 | `NavGridBootstrap` 미동작 |
| 0% | 장애물을 못 잡음 = 베이크 시점 문제 | 런타임 생성 지오메트리 → `Rebake()` |
| 70%↑ | 과다 팽창으로 통로가 막힘 | `agentRadius` / 셀 크기 |
| 20~40% | 격자 정상 → **스폰·콜라이더 문제** | 맵/스폰 지점 |

마더 결과 탭이 이 표를 그대로 보여준다.

### 마더 창 탭

| 탭 | 무엇을 |
|---|---|
| 실황 | 선택 차일드의 상태 + 이벤트 실시간 흐름 |
| 결과 | 런 목록(PASS/FAIL) → 선택 시 판정 8항목 + 이상 전체 |
| 문제 | 여러 런에 걸친 반복 실패·이상 빈도·문제 좌표 |
| 밸런스 | **GameTuning 76개 노브를 그룹·툴팁·범위와 함께 보고 그 자리에서 수정** |
| 에이전트 | 마더가 본 문제를 지시문으로 만들어 **Claude Code 에이전트에 넘겨 수정시킴** |
| 커버리지 | 게임 시스템별 QA 검증 상태(사각지대 확인) |
| 원본 로그 | 차일드가 떨군 리포트 전문 |

### 밸런스 탭 (`tools/qa_tuning.py`)

게임 수치는 두 에셋에 나뉘어 있어 **소스**로 골라 본다.

| 소스 | 에셋 | 메타(툴팁·범위·기본값) | 예 |
|---|---|---|---|
| GameTuning — 게임 전역 노브 | `GameTuning.asset` | `GameTuning.cs` (76) | 루팅·소음·시야·무게·낮밤 |
| 플레이어 스탯 | `StatDB.asset` `playerStat` | `PlayerStatData.cs` (39) | **`moveSpeed`·`maxStamina`**·공격·구르기 |
| 적 유닛 — `<id>` | `StatDB.asset` `units[]` | `UnitStatData.cs` (29) | HP·공격력·감지범위·이속 |

`.cs`의 `[Header]`/`[Tooltip]`/`[Range]`/기본값을 파싱해 에셋의 현재 값과 짝지어 보여주고,
슬라이더/입력으로 고쳐 저장한다. 기본값과 다른 항목은 색으로 구분된다.

저장은 **해당 줄의 값만 치환**하므로 Unity 서식·메타가 보존된다(없던 필드는 해당 소스 구간 끝에 추가).

> ⚠️ 줄바꿈 판별은 **원본 바이트**로 한다. `read_text`는 universal-newline이라 CRLF를 `\n`으로
> 읽어버려서, 그대로 저장하면 파일 전체 줄끝이 뒤집힌다(`StatDB.asset`은 LF, `GameTuning.asset`은 CRLF).
> 쓰기도 `newline=""`로 열어야 한다. 세 소스 모두 **같은 값 왕복 시 바이트 동일**을 회귀 검증한다.

> 저장 후 **Unity 창을 한 번 클릭**해야 에셋을 다시 읽는다.
> `buildingEnterRatio`를 바꿨다면 `빌드 ▸ 지역1` 재실행이 필요하다.

### 활동 로그

마더가 내린 모든 지시가 화면 하단 도킹 패널 + `tools/qa-mother.log`에 남는다.
투입·수령·기동·종료·응답·잔재정리·밸런스 저장·에이전트 실행·결과 판정, 그리고
**로그를 지운 사실 자체도** 기록해 감사 기록에 구멍이 없다.

`수령`은 차일드가 `qa-command.json`을 집어간 순간(= 지시가 먹힌 시점)이다.

### 에이전트 탭

마더가 본 것(최신 런 판정·실패 항목·이상 전체·문제 좌표·막힌 차일드)을 지시문으로 조립해
`claude -p --agent <에이전트>`로 넘긴다. 출력은 창에서 그대로 흐른다.

- 지시문은 `tools/qa-agent-request.md`로 저장 후 경로로 전달 — 긴 한글이 셸 인용에서 깨지지 않게
- **`파일 수정 허용(acceptEdits)` 체크는 기본 꺼짐** — 켜야 실제로 코드를 고친다. 꺼두면 진단·제안만
- 막힘 배너의 `[🔧 에이전트에 넘기기]`는 막힌 사유를 지시문 맨 앞에 붙여 보낸다
- **실행 전후로 `git diff --numstat HEAD`를 찍어 "무엇이 바뀌었는지"를 파일 단위로 보여준다.**
  안 고쳤으면 "바뀐 파일 없음"이라고 명시한다 — 고쳤는지 아닌지 모르는 상태가 없어야 한다
- 에이전트는 **완전히 새 세션**이라 대화 맥락을 모른다. 그래서 지시문에 문제 전체를 실어 보낸다

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
| `title.newgame` / `title.continue` | count(슬롯) | **신규 유저 경로** — 타이틀에서 새 게임/이어하기(uGUI 버튼은 API 호출) |
| `story.skip` | budgetSec | 프롤로그·대화·튜토를 Space로 넘김. 안 끝나면 STORY_STUCK(신규 유저가 막히는 지점) |
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

---

## 시스템 커버리지 매트릭스 (QA가 아는 것 / 모르는 것)

> **원칙: QA는 게임의 모든 시스템을 알아야 한다.** 새 시스템을 만들면 이 표에 행을 추가하고,
> 검증할 op가 없으면 **op를 먼저 만든다.** 표에 없는 시스템 = QA 사각지대.
>
> 상태: ✅검증됨 · 🟡op는 있으나 미실행 · ⬜op 없음(사각) · ➖게임에 미구현

| # | 게임 시스템 | QA op / 검증 수단 | 통과 판정 | 상태 |
|---|---|---|---|---|
| 1 | **타이틀·세이브 슬롯** | `title.newgame` `title.continue` | 안전가옥 도달 | 🟡 |
| 2 | **프롤로그·스토리·튜토** | `story.skip` | 스토리 종료(막히면 STORY_STUCK) | 🟡 |
| 3 | **씬 전환**(허브↔레이드) | `safehouse.ensure` `raid.enter` | 씬 전환 정상 | 🟡 |
| 4 | **레이드 스폰/탈출** | `raid.extract` + `RaidSpawnDirector` | 레이드 탈출 | 🟡 |
| 5 | **루팅·예산제** | `raid.explore` | 루팅 획득 | 🟡 |
| 6 | **인벤토리 격자** | `inventory.organize` | 아이템 유실 없음 | 🟡 |
| 7 | **경제(상점 매매)** | `shop.sell` `shop.buy` | 소지금 추세(MONEY_INFLATION) | 🟡 |
| 8 | **퀘스트/게시판** | `quest.accept` | 수주 성공 | 🟡 |
| 9 | **XP·레벨·PP** | `settle.verify` + 추세 | LEVEL_STALL | 🟡 |
| 10 | **정산(RaidResultUI)** | `settle.verify` | 정산 데이터 존재 | 🟡 |
| 11 | **맵 밸런스**(파밍효율·반복성) | `QaHeatmap` | DEAD_ZONE·NOVELTY_ZERO | 🟡 |
| 12 | **길찾기·지오메트리** | `QaHeatmap` 스턱 클러스터 | 진행 막힘 없음 | 🟡 |
| 13 | **씬·데이터 배선** | `IntegrityValidator` | 오류 0 | ✅ **2026-07-11 통과(49)** |
| 14 | **루트테이블·경제 분포** | `QaBalanceSim` | 경고 0 | 🟡 |
| 15 | **전투**(약/강/구르기/그로기) | — | — | ⬜ **사각** |
| 16 | **의료·부상** | — | — | ⬜ **사각** |
| 17 | **생존**(수분·포만감) | — | — | ⬜ **사각** |
| 18 | **특성(퍽) 해금** | — | — | ⬜ **사각** |
| 19 | **제작·수리** | — | — | ⬜ **사각** |
| 20 | **하이드아웃 강화** | — | — | ⬜ **사각** |
| 21 | **건물 내부 전환** | — | — | ⬜ **사각** |
| 22 | **도감** | `inventory.organize`가 부분 확인 | 발견 훅 동작 | 🟡 부분 |
| 23 | **소음·투척물·시야(FOV)** | — | — | ⬜ **사각** |
| 24 | **약탈자 회수**(사망 루프) | 사망 감지만(`QaHeatmap`) | — | ⬜ 부분 |
| 25 | **낮/밤·짙은현상** | — | — | ⬜ **사각** |
| 26 | **게임패드 입력** | — | — | ⬜ 사각(가상층이 키보드 경로만) |
| 27 | **지도·나침반** | — | — | ➖ 갭분석 ⑩ 미구현 |

### 알려진 구조적 한계 (중요)

- **가상 입력은 uGUI 버튼을 못 누른다.** `GameInput` 폴링 소비자(이동·상호작용·패널 단축키)만 구동하고,
  EventSystem 기반 uGUI 버튼(타이틀·상점·제작 UI 버튼)은 **API로 호출**해야 한다.
  → 버튼 자체의 배선 오류는 이 QA로 못 잡는다. `IntegrityValidator`·프리팹 베이크 규약이 그 자리를 메운다.
- **전투는 아직 봇이 "맞닥뜨리면 대응" 수준** — 의도적 교전·회피·콤보 검증 op가 없다(#15).
- **사각(⬜) 시스템은 QA가 아무것도 보장하지 않는다.** 표를 보고 op를 늘려가는 것이 이 시스템의 확장 방향.
