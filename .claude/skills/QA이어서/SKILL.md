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
| `tools/qa-dashboard.ps1` | **외부 GUI** — 통과율·인스턴스·문제좌표. `localhost:8787` |
| `.claude/agents/qa-runner.md` | QA 실행·판정 전용(코드 안 고침) |
| `.claude/agents/dev-fixer.md` | 수정 전용(QA 안 돌림) |
| `.claude/skills/qa/` · `qa-loop/` | 지휘 / 자동 루프 |

**세이프가드(깨지 말 것)**: `SaveManager.SuppressWrites`(봇이 진짜 세이브 덮어쓰기 방지) · F9는 `Application.isEditor \|\| Debug.isDebugBuild`에서만 · `GameInput.ResetState`에 `_virtual=false` · `OnDestroy/OnApplicationQuit`에서 Virtual 해제.

## 3. ⚠️ 아직 검증 안 됨 (최우선)

**Unity에서 단 한 번도 컴파일·실행되지 않았다.** QA 코드가 크므로 첫 작업은 반드시:
1. Unity 컴파일 확인 → 에러 나면 그것부터.
2. `Tools ▸ TopDown ▸ QA ▸ 정합성 검증` 실행.
3. Play → **F9** → 리포트 확인.

첫 런은 **거의 확실히 FAIL**이다(지역1 미검증 + 봇 첫 실전). 1~2라운드는 **QA 자체 교정**에 쓴다 — 게임 버그와 QA 오탐을 구분할 것.

## 4. 남은 작업 (우선순위)

1. **첫 실행 검증** — 위 §3. 컴파일 에러·오탐 잡기.
2. **op 확장** — 현재 없는 것: 전투(`combat.engage`), 특성 해금(`trait.unlock`), 제작(`craft.make`), 하이드아웃 강화(`hideout.upgrade`), 건물 내부 진입(`building.enter`). 사용자가 "여러 방면"을 원함.
3. **판정 체크 확장** — 현재 8개. 밸런스 임계(판당 수익 하한 등)를 시나리오에서 지정 가능하게.
4. **대시보드** — ASCII 히트맵을 캔버스 시각화로, 런 간 추세 그래프.
5. **루프 자동화** — `/qa-loop`가 실제로 도는지 검증(에이전트 왕복).

## 5. 실행 방법

```bash
# 명령 대기 모드(권장 — Claude가 지휘)
game.exe -qa-serve -qa-instance=A -qa-minutes=10
# 외부 GUI
powershell -ExecutionPolicy Bypass -File tools\qa-dashboard.ps1   # localhost:8787
```
에디터는 **F9**(1회 실행). 데이터 폴더 = `%USERPROFILE%\AppData\LocalLow\Studio Pod Games\<제품명>\`
(정확한 경로는 게임 Console 첫 `[QA]` 로그).

## 6. 작업 원칙

- **QA는 고치지 않고, 개발은 판정하지 않는다** — `qa-runner` / `dev-fixer` 역할 분리.
- 새 op는 `QaSteps.Ops` 딕셔너리에 등록 + `docs/qa.md` op 표 + `/qa` 스킬 표에 **3곳 동시 갱신**.
- 신규 .cs는 **.meta 필수**.
- 커밋·푸시는 **사용자 요청 시만**. 메시지 끝 `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- **perl/sed로 C# 문자열 보간(`$"..."`)을 치환하지 말 것** — 이번 작업에서 `$`가 먹혀 코드가 깨진 적 있다. Edit 도구를 쓸 것.
