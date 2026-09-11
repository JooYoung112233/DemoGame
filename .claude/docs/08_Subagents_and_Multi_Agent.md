---
tags:
  - ai
  - reference
  - subagent
  - multi-agent
aliases:
  - Subagents
  - 멀티에이전트
  - 병렬 세션
  - multi-agent 효율
description: 서브에이전트의 컨텍스트 격리, 멀티에이전트 효율 논쟁(Anthropic +90.2% vs Cognition "만들지 마라")의 양측 근거와 종합
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 08. Subagents & 멀티에이전트 (효율 논쟁)

> **한 줄**: 서브에이전트의 **컨텍스트 격리**는 실제 효과. 그러나 **"멀티에이전트=N배 빨라진다"는 조건부**다 — 병렬 가능한 *탐색*엔 이득, **대부분의 코딩엔 손해**(벤더 본인이 인정).
> 대표 신뢰도: 🟢 공식(격리·90.2%) + 🟡 실무합의(Cognition 반론) + 🔵 외부 재현.

---

## 1. Subagent — 컨텍스트 격리 (🟢 효과 확실)

- 정의(공식): *"Each subagent runs in its own context window with a custom system prompt, specific tool access, and independent permissions."*
- 효과: 곁가지 작업(검색결과·로그·파일내용)이 **메인 대화를 오염시키지 않게** 격리하고 **요약만 반환**(보통 1k~2k 토큰). *"the subagent does that work in its own context and returns only the summary."*
- 설정: YAML frontmatter Markdown 파일, **name·description만 필수**, 본문=시스템 프롬프트. 우선순위: managed > `--agents` > project `.claude/agents/` > user `~/.claude/agents/` > plugin.
- → 토큰 절약 관점은 [[04_Token_Context_Management]], 공식 근거는 orchestrator-workers 패턴 [[02_Agent_Loop_and_Harness]].

> 읽기/탐색 격리는 Cognition도 허용. 컨텍스트 절약 효과는 **高신뢰**.

---

## 2. 멀티에이전트 효율 — 양측 근거

### 찬성: Anthropic (멀티에이전트 리서치 시스템)
- **단일 Opus 4 대비 +90.2%** (Opus 리드 + Sonnet 서브에이전트). ⚠️ **단, "여러 독립 방향을 동시 탐색하는 breadth-first 쿼리"에 한정**, 재현 불가능한 **내부 평가** 자가보고(中신뢰, 2-1).
- 메커니즘: *"Multi-agent systems work mainly because they help **spend enough tokens**"* — BrowseComp에서 **토큰량 단독이 성능 분산의 80%** 설명. 멀티에이전트는 chat의 **~15배** 토큰(단일 에이전트는 ~4배).
- → **고가치 작업에서만 경제적**.

### 반대: Cognition (Devin) — "Don't Build Multi-Agents"
- *"in 2025, running multiple agents in collaboration only results in **fragile systems**."* 기본값으로 **단일 스레드 선형 에이전트**를 쓰라.
- 2원칙: ① *"Share context ... full agent traces, not just individual messages"* ② *"Actions carry implicit decisions, and conflicting decisions carry bad results."*
- Flappy Bird 예시: 병렬 서브에이전트가 서로의 작업을 못 봐 **시각/물리 스타일 불일치** → 누적 붕괴.

### 결정적 합의점 (양측이 사실 정렬)
- **Anthropic 본인도 명시**: *"most coding tasks involve fewer truly parallelizable tasks than research"* / *"agents are not yet great at coordinating ... in real time."* → **코딩엔 멀티에이전트 부적합.**
- 중립 요약(smol.ai): 두 글은 *"surprisingly aligned — right tool for the job."*
- 외부 재현(🔵, 마케팅 아님): Tran & Kiela(arXiv 2604.02460) — **동일 토큰 예산이면 단일 에이전트가 멀티를 능가**, 코디네이션 오버헤드가 1차 제약.

### ✅ 올바른 오케스트레이션 vs ❌ 안티패턴

> "서브에이전트를 쓰면 무조건 비효율"이 아니다. **메인에서 plan·논의를 먼저 정의한 뒤 위임**하는 것은 Cognition 원칙①(컨텍스트 공유)을 실행하고 Anthropic orchestrator-workers와 일치하는 **권장 패턴**이다. 효율을 가르는 건 "plan을 했나"가 아니라 ↓.

| 기준 | ✅ 잘 됨 | ❌ 깨짐 |
|---|---|---|
| 하위작업 **독립성** | 독립적·read-heavy(탐색·검색·독립 검증) → 충돌할 결정이 없음 | 출력이 **맞물리는 쓰기**(서로 스타일/구조 결정 충돌) |
| **종합** | 메인(orchestrator)이 요약 받아 충돌 해소 | 동급 에이전트가 서로 덮어씀 |
| **비용 대비 가치** | 고가치·컨텍스트 초과 작업 | 작은 작업에 ~15배 토큰 낭비 |

- 핵심 한정: plan을 미리 줘도 병렬 에이전트는 실행 중 만드는 **"실시간 암묵적 결정"** 까지는 서로 못 본다(Cognition 원칙②). 따라서 **맞물리는 작업은 순차/단일 스레드**, **독립 작업만 병렬**.
- 예: 다출처 리서치 fan-out(독립 검색→요약 회수→메인 종합) = ✅ 정석. "덱을 조각내 동시 작성" = ❌ Flappy Bird식 불일치.

---

## 3. 병렬 세션 / git worktree

- git worktree로 격리된 체크아웃에서 병렬 Claude 세션 실행 가능(공식 기능).
- **속도 이득은 "독립 서브태스크"일 때만 실재** — 의존성·공유 컨텍스트 작업은 코디네이션 비용이 이득을 상쇄.
- ⚠️ wall-clock 단축 배수를 정량화한 **재현 가능 벤치마크는 부재**(메커니즘 수준 근거만).

---

## 4. 신뢰도 판정 — "멀티에이전트 = 빠르다" 검증

| 주장 | 판정 |
|---|---|
| 서브에이전트 컨텍스트 격리 효과 | 🟢 공식 (3-0) |
| +90.2% 향상 | 🟠 벤더 내부·breadth-first 한정 (2-1) ⚠️ |
| 토큰이 성능의 80% 설명·15배 비용 | 🟢🔵 (1차+외부재현) |
| **"멀티에이전트=항상 N배 빠르다"** | 🔴 **바이럴 과장** ⚠️ — 대부분의 코딩에 **명시적 반증** |
| 코딩=단일 스레드+컨텍스트 공유가 기본 | 🟡 실무합의 (Cognition + Anthropic 한정 일치) |

> **실전 결론**: 코드 분석/코딩 관리 = **단일 세션 + 컨텍스트 공유/압축**이 기본. 멀티에이전트는 **독립적·read-heavy 탐색(리서치)** 에 한정, 그것도 토큰 15배를 감당할 고가치 작업일 때.

---

## 출처

- Anthropic, *How we built our multi-agent research system* — https://www.anthropic.com/engineering/multi-agent-research-system
- Cognition, *Don't Build Multi-Agents* — https://cognition.ai/blog/dont-build-multi-agents
- Tran & Kiela, *(single vs multi-agent at fixed budget)* — https://arxiv.org/abs/2604.02460
- Claude Code, *Sub-agents* — https://code.claude.com/docs/en/sub-agents · *Worktrees* — https://code.claude.com/docs/en/worktrees
- smol.ai, *Cognition vs Anthropic* — https://news.smol.ai/issues/25-06-13-cognition-vs-anthropic
