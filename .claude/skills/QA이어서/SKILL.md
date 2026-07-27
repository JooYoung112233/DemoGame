---
name: QA이어서
description: demo13-flashlight QA 자동화 시스템 구축을 이어서 진행한다 — 2026-07-11에 만든 봇·공간분석·판정·GUI·에이전트 루프의 현재 상태를 복원하고 다음 항목을 구현한다. "QA 이어서", "QA 계속", "QA 시스템 더 만들자", "QA 뭐 남았지" 같은 요청에 사용.
---

# QA 자동화 시스템 — 이어서

demo13-flashlight의 **"Claude가 명령을 내려 게임을 실제로 플레이시키고, 밸런스·막힘 데이터를 뽑아, 자동으로 고치는"** 시스템.
**SSOT = `demo13-flashlight/docs/qa.md`**.

## 0. 대전제 (모르면 헛수고한다)

- **목적은 "동작 검증"이 아니라 사용자가 못 하는 판단을 대신하는 것.** 사용자 원문:
  *"어디서 반복하면 재미없다 / 어디서 파밍하면 재미없다 / 어디서 멈춘다 / 어디선 진행이 안 된다"*, *"반복성이나 이런 건 내가 못하니까, 밸런스도 잘 모르고 맵 밸런싱도 그렇고"*.
  → 총합 지표가 아니라 **위치·반복 축** 데이터가 본체다.
- **입력은 `GameInput` 가상층 하나만 가로챈다.** 게임 코드는 QA를 모른다(단방향 의존). `Assets/Scripts/QA/`를 통째로 지워도 컴파일이 안 깨져야 한다 — 이 성질을 절대 깨지 말 것(사용자가 "나중에 제거·차단" 요구).
- **게임 빌드에 API 키를 넣지 않는다.** Claude 연결은 **파일 채널**(qa-command/status/result/blocked/resume).
- **프레임 순서 함정**: `Update() → 코루틴 재개 → LateUpdate()`. 봇은 코루틴에서 키를 누르므로 같은 프레임에 플래그를 걷으면 **아무 소비자도 못 본다**. `GameInput`은 **프레임 스탬프**로 "이전 프레임 것만" 수거한다 — 이 구조를 건드리지 말 것.

## 0-1. 다음 세션 시작 방법 (2026-07-27 사용자 지시)

> *"집에서 할 땐 화면 공유 할게, 그거 보고 좀 맞춰보자. 지금은 아예 뭐 안 되네."*

**말로 주고받지 말고 화면을 보고 맞춘다.** 순서:

1. **Unity 컴파일부터 확인** — 마지막 커밋(`8e60d0e`)의 C#은 **컴파일 미검증**이다.
   컴파일이 깨져 있으면 F9도 마더도 전부 무의미하다. 여기가 "아예 뭐 안 되는" 1순위 원인.
2. 화면에서 실제로 무엇이 보이는지 확인한 뒤 고친다(설명 요구 금지 — 보고 판단).
3. 그 다음 F9 → 결과 JSON을 **직접 읽고**(§7) 판단.

## 1. 상태 로드 (먼저)

```bash
git -C D:/Demo fetch && git -C D:/Demo status -s
```
- `docs/qa.md`를 읽어 프로토콜·op 목록·결정 로그 확인.
- ⚠️ **병렬 자동커밋 프로세스**가 `git add -A`로 미커밋 변경을 쓸어담는다. 커밋은 **항상 경로 지정**, 직전에 `git diff --cached --name-only` 확인. `Assets/GPT/` 삭제분은 **절대 커밋 금지**.

## 2. 이미 만든 것 (중복 작업 금지)

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Core/GameInput.cs` | **가상 입력층**(프레임 스탬프). `Virtual=true`면 실제 디바이스 무시 |
| `Assets/Scripts/QA/QaBot.cs` | 러너 — 명령 대기(`-qa-serve`)·시나리오 실행·스텝 안전래퍼·**PASS/FAIL 판정**(8체크)·결과 JSON |
| `Assets/Scripts/QA/QaSteps.cs` | op 12종 — cycle/safehouse/inventory/shop.sell·buy/quest/raid.enter·explore·extract/settle |
| `Assets/Scripts/QA/QaScenario.cs` | 시퀀스 JSON 모델·로더(외부 override → Resources → 내장) |
| `Assets/Scripts/QA/QaTelemetry.cs` | 사이클 지표 + 추세 이상(인플레·레벨정체·루팅가뭄) |
| `Assets/Scripts/QA/QaHeatmap.cs` | **공간 분석** — 16m 격자, DEAD_ZONE·NOVELTY_ZERO·STUCK_HOTSPOT·SPARSE_PATH + ASCII 히트맵 |
| `Assets/Scripts/QA/QaBridge.cs` | 파일 채널(SessionJson·CheckJson·blocked/resume) |
| `Assets/Scripts/QA/QaReport.cs` | 리포트 수집·저장 |
| `Assets/Editor/Test/IntegrityValidator.cs` | 정적 배선 검증(메뉴) |
| `Assets/Editor/Test/QaBalanceSim.cs` | 헤드리스 밸런스 분포(메뉴, **플레이 모드 필요**) |
| `Assets/Scripts/Combat/NavGridBootstrap.cs` | **씬마다 NavGrid 자동 배치** — 없으면 적·NPC·봇이 전부 직선 이동 |
| `tools/qa_mother_gui.py` | **마더 GUI**(tkinter, 다크) — 차일드 관리·투입·감시·밸런스·에이전트·활동로그 |
| `tools/qa_orchestrator.py` | 마더 CLI(에이전트가 `--json`으로 사용). GUI와 **같은 함수**를 공유 |
| `tools/qa_tuning.py` | 밸런스 읽기/쓰기 — GameTuning + StatDB(플레이어/유닛) |
| `tools/QA-Mother.bat` | 마더 실행(더블클릭) |
| `tools/qa-dashboard.ps1` | 웹 대시보드(구버전, 보조). `localhost:8787` |
| `.claude/agents/qa-runner.md` | QA 실행·판정 전용(코드 안 고침) |
| `.claude/agents/dev-fixer.md` | 수정 전용(QA 안 돌림) |
| `.claude/skills/qa/` · `qa-loop/` | 지휘 / 자동 루프 |

**세이프가드(깨지 말 것)**: `SaveManager.SuppressWrites`(봇이 진짜 세이브 덮어쓰기 방지) · F9는 `Application.isEditor \|\| Debug.isDebugBuild`에서만 · `GameInput.ResetState`에 `_virtual=false` · `OnDestroy/OnApplicationQuit`에서 Virtual 해제.

## 3. 검증 상태 (2026-07-27 갱신)

- ✅ **정합성 검증기 통과** (오류 0 / 통과 49). 실전에서 진짜 버그 1건을 잡음
  — `ScrapMarket_GB` 씬에 옛 `bandit_melee`가 박혀 StatDB 미스 → 씬 재빌드로 해소.
- ✅ **실플레이 3회 완료**(F9). 봇이 실제로 걷고 상호작용한다.
- ⬜ **마지막 커밋(`8e60d0e`)은 Unity 컴파일 미확인** — 자가복구·와리가리·격자기록이 들어간 판.
  집에서 처음 할 일: **컴파일 확인 → F9 1회 → 결과 JSON 읽기**.

### 실플레이로 확정된 **게임 결함** (아직 미수정)

| 결함 | 근거 | 영향 |
|---|---|---|
| **안전가옥에 Stash 미배치** | `goto NOT_PLACED: Stash` (3회 재현) | 창고 접근 불가 = 인벤 정리 루프 자체가 없음 |
| **MapBoard 도달 불가** | `goto UNREACHABLE` 직선 12.2m | 게시판 = 의뢰 수주 진입점 |
| **수주 가능 의뢰 0** | `quest NO_ACCEPT` | 게시판 생성/평판 게이트 확인 필요 |
| **Zone1 스폰 고착** | 셀 (248,120) **체류 92초 · 스턱 8회 · 획득 0**, 좌표가 (242.5,124.7)로 소수점까지 동일 | 레이드가 시작되지 않음 |

> Zone1 고착의 원인은 **결과 JSON `navGrids[]`의 막힘 %** 로 갈린다(§7).

### QA 자체 결함으로 고친 것 (같은 실수 반복 금지)

- `Analyze()`가 정상 종료 경로에만 있어 **중단된 런은 스턱 핫스팟이 판정에서 통째로 빠졌다**
  (한 칸 스턱 8회인 런이 `진행 막힘 없음 PASS`). → `Finish()`로 이동, `_analyzed`로 1회 보장.
- F9 런은 `commandId`가 없어 `latest`만 남는데 집계가 `latest`를 건너뛰어 **마더가 F9 런을 못 봤다.**
- 막힘 대기 중 종료 시 `qa-blocked.json` 잔재 → 죽은 차일드가 영원히 "응답 대기".
- `QaBridge`가 인스턴스 접두를 안 붙여 빌드 A/B가 같은 파일을 덮어썼다.
- `qa_tuning`: `read_text`가 universal-newline이라 CRLF 감지가 아예 안 됐고 쓰기도 텍스트 모드 →
  **StatDB.asset(LF) 저장 시 265줄 전체가 뒤집힐 뻔.** 원본 바이트로 판별 + `newline=""`.

## 4. 시스템 커버리지 (QA의 사각지대)

**`docs/qa.md` §시스템 커버리지 매트릭스가 진실원.** 게임 시스템 27개 중:
- ✅ 검증됨: 씬·데이터 배선(1개)
- 🟡 op는 있으나 미실행: 타이틀·스토리·씬전환·루팅·인벤·경제·퀘스트·XP·맵밸런스 등 13개
- ⬜ **사각(op 없음)**: 전투 · 의료 · 생존 · 특성 · 제작 · 하이드아웃 · 건물내부 · 소음/투척/시야 · 낮밤/현상 · 게임패드

> **새 게임 시스템을 만들면 이 표에 행을 추가하고, 검증 op가 없으면 op를 먼저 만든다.**
> 표에 없는 시스템 = QA가 아무것도 보장하지 않는 영역.

### 구조적 한계 (반드시 알 것)
**가상 입력은 uGUI 버튼을 못 누른다.** `GameInput` 폴링 소비자(이동·상호작용·패널 단축키)만 구동하고,
EventSystem 기반 버튼(타이틀·상점·제작 UI)은 **API 호출**로 대신한다(`TitleScreen.StartNewGame` 등).
→ 버튼 배선 오류는 이 QA로 못 잡는다.

## 5. 남은 작업 (우선순위)

1. **Zone1 스폰 고착 해소** — `navGrids[]` 막힘 %로 원인 확정 후 수정(§7). 이게 막히면 레이드 데이터가 안 나온다.
2. **Stash 배치 / MapBoard 도달 / 의뢰 0** — 위 표의 확정 결함 3건.
3. **사각 op 채우기** — `combat.engage` · `medical.treat` · `trait.unlock` · `craft.make` · `hideout.upgrade` · `building.enter` · `throw.lure`.
4. **판정 체크 확장** — 현재 8개. 밸런스 임계(판당 수익 하한 등)를 시나리오에서 지정 가능하게.
5. **빌드 차일드 병렬** — 빌드를 뽑아 `qa-instances.json`에 `exePath`만 넣으면 N개로 확장.
6. **루프 자동화** — `/qa-loop` 에이전트 왕복 실검증.

## 6. 실행 방법

```bash
tools\QA-Mother.bat
```
마더 창(다크)에서 차일드 추가·기동·투입·감시·밸런스 수정·에이전트 호출까지 전부 된다.
에디터 차일드는 **F9**(1회 실행, 명령 대기 아님). 빌드 차일드는 `-qa-serve -qa-instance=A`.

- 데이터 폴더는 **PC마다 다르다**(`%USERPROFILE%\AppData\LocalLow\Studio Pod Games\demo13-flashlight`).
  `qa-instances.json`의 `dataDir`가 안 맞으면 orchestrator가 이 PC 기본 경로로 자동 폴백한다.
- 활동 로그 `tools/qa-mother.log`, 에이전트 지시문 `tools/qa-agent-request.md`는 **gitignore**(기계별 산출물).

## 7. 결과를 읽는 법 (사용자에게 묻지 말 것)

사용자 원문: *"내가 너한테 QA 보고서를 주는 게 아니라 니가 알아서 보고 판단하라니까"*.
**직접 파일을 읽어라.** 데이터 폴더에:

| 파일 | 무엇 |
|---|---|
| `qa-result-<stamp>.json` | 판정·체크8·이상·셀·**`navGrids[]`** |
| `qa-report-<stamp>.txt` | 사람이 읽는 전체 로그 + ASCII 히트맵 |
| `qa-status.json` | 1초 하트비트 |

**`navGrids[].blockedPct`가 봇 고착의 원인을 가른다:**

| 값 | 뜻 | 고칠 곳 |
|---|---|---|
| `present:false` | 격자 없음 → 적·NPC·봇 전부 직선 | `NavGridBootstrap` 미동작 |
| 0% | 장애물 못 잡음 = 베이크 시점 | 런타임 생성 지오메트리 → `Rebake()` |
| 70%↑ | 과다 팽창으로 통로 막힘 | `agentRadius`(0.35) / 셀 크기 |
| 20~40% | 격자 정상 → **스폰·콜라이더 문제** | 맵/스폰 지점 |

### 봇 자가 복구 (막혀도 런이 끝까지 간다)

`STUCK`/`OSCILLATION` → 넛지 → `RECOVER_LOAD`(세이브 로드) → `RECOVER_RESTART`(사이클 처음부터)
→ 그래도 안 되면 그때 `qa-blocked.json`으로 마더에게 질문.
복구 예산은 사이클마다 초기화, 사이클 재시작은 런당 2회 상한.

**와리가리**는 STUCK으로 안 잡힌다(위치가 계속 바뀜) → 6초 창에서 이동 8m·순이동 2m 미만이면 `OSCILLATION`.

## 8. 작업 원칙

- **QA는 고치지 않고, 개발은 판정하지 않는다** — `qa-runner` / `dev-fixer` 역할 분리.
- 새 op는 `QaSteps.Ops` 딕셔너리에 등록 + `docs/qa.md` op 표 + `tools/qa-manifest.json`에 **3곳 동시 갱신**.
- 신규 .cs는 **.meta 필수** — Unity가 만들어주면 **같이 커밋**해야 다른 PC에서 GUID가 갈리지 않는다.
- 커밋·푸시는 **사용자 요청 시만**. 메시지 끝 `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.
- **perl/sed로 C# 문자열 보간(`$"..."`)을 치환하지 말 것** — `$`가 perl 변수로 먹혀 코드가 깨진 적 있다. Edit 도구를 쓸 것.
- **파이썬으로 Unity 에셋을 쓸 때는 줄바꿈을 원본 바이트로 판별하고 `newline=""`로 열 것.**
  `read_text`/`write_text` 기본값은 파일 전체 줄끝을 갈아엎는다.
- 윈도우 콘솔은 cp949 — 파이썬 CLI는 `sys.stdout.reconfigure(encoding="utf-8")` 없으면 한글/기호에서 죽는다.
- `git push`가 멈추면 네트워크가 아니라 **`git-credential-manager`의 "Select an account" 창**이
  응답을 기다리는 경우가 있다(`Get-Process`로 `MainWindowTitle` 확인). 인증은 사용자가 직접.
