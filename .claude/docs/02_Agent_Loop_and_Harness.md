---
tags:
  - ai
  - reference
  - harness
aliases:
  - 에이전트 루프
  - 에이전트 하네스
  - workflow vs agent
description: 에이전트 루프·하네스의 정의, workflow와 agent의 구분, Anthropic 5대 워크플로우 패턴, 단순성 우선 원칙, 학술적 기반(ReAct/Reflexion)
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 02. 에이전트 루프 & 하네스 (Agent Loop & Harness)

> **한 줄**: 에이전트는 "환경 피드백을 받아 도구를 *루프*에서 쓰는 LLM"이고, 하네스는 그 모델을 에이전트로 작동시키는 시스템(scaffold)이다.
> 대표 신뢰도: 🟢 공식(Anthropic 1차) + 🔵 실증(arXiv 기반 패턴). **3-0 만장일치 검증.**

---

## 1. 개념 / 정의

### 1.1 Workflow vs Agent (공식 분류)

Anthropic의 정전적 분류 — *Building Effective Agents*(2024-12, 2026 현재도 표준):

- **Workflow**: *"systems where LLMs and tools are orchestrated through **predefined code paths**."* (사전정의된 코드 경로로 오케스트레이션)
- **Agent**: *"systems where LLMs **dynamically direct their own processes and tool usage**, maintaining control over how they accomplish tasks."* (LLM이 스스로 프로세스·도구 사용을 동적으로 지휘)
- 둘 다 묶어서 **"agentic systems"**.

> 🟢 공식 · `anthropic.com/research/building-effective-agents` · 3-0

### 1.2 에이전트 루프 (Agentic Loop)

- *"They are typically just LLMs using tools based on environmental feedback **in a loop**."*
- 핵심: 매 단계 **환경의 ground truth**(도구 호출 결과·코드 실행)로 진척을 평가해야 한다 — *"it's crucial for the agents to gain ground truth from the environment at each step ... to assess its progress."*

### 1.3 하네스(Harness)란

- Anthropic *Demystifying evals* 정의: *"An agent harness (or scaffold) is the system that enables a model to act as an agent"* — 모델을 에이전트로 만드는 **둘러싼 시스템**(도구·루프·컨텍스트 주입·검증).
- **Claude Agent SDK**를 "범용 에이전트 하네스"로 포지셔닝: *"a powerful, general-purpose agent harness ... running on the Claude Agent SDK **in a loop across multiple context windows**."*
- Agent SDK가 제시하는 루프 4단계(주의: 출처가 `often`으로 헷지 — 경직된 "고정 4단계" 아님, **2-1** 다수결):
  `gather context → take action → verify work → repeat`
  - gather: 에이전트/시맨틱 검색, subagents, compaction
  - action: 도구·bash·MCP
  - verify: 규칙 정의·시각 피드백·LLM-as-judge

> 🟢 공식 · `building-agents-with-the-claude-agent-sdk`, `effective-harnesses-for-long-running-agents`

---

## 2. 어떻게 적용하나

### 2.1 Anthropic이 명명한 5대 워크플로우 패턴

| 패턴 | 한 줄 | subagent 관련 |
|---|---|---|
| **Prompt chaining** | LLM 호출을 순차로 연결, 중간 검증 게이트 | |
| **Routing** | 입력을 분류해 전문 경로로 분기 | |
| **Parallelization** | 독립 하위과제 동시 실행 또는 동일과제 반복 | ← 멀티에이전트 |
| **Orchestrator-workers** | 중앙 LLM이 하위과제를 worker에 위임·종합 | ← **subagent 공식 근거** |
| **Evaluator-optimizer** | 한 모델이 생성, 다른 모델이 피드백·반복 | |

- orchestrator-workers verbatim: *"a central LLM dynamically breaks down tasks, delegates them to worker LLMs, and synthesizes their results."* → 이게 [[08_Subagents_and_Multi_Agent]]의 lead-agent/subagent 구조로 이어진다.

### 2.2 장기 실행(long-running) 하네스 패턴

- **여러 컨텍스트 윈도우에 걸친 루프**가 핵심 난제: *"maintaining coherent progress across multiple context windows remains an open problem."*
- 통제 실험(claude.ai 클론 빌드)에서: *"**compaction isn't sufficient**"* → 해법은 **외부화 메모리 파일 + git 히스토리**: *"a way for agents to quickly understand the state of work when starting with a fresh context window, ... accomplished with the **claude-progress.txt** file alongside the git history."*
- → 컨텍스트 관리는 [[04_Token_Context_Management]], 메모리 외부화는 [[03_Context_Engineering]]·[[11_Knowledge_Management_Methods]].

### 2.3 학술적 기반 패턴 (🔵 실증 — arXiv 1차)

| 패턴 | 논문 | 아이디어 |
|---|---|---|
| **ReAct** | arXiv 2210.03629 | Reasoning + Acting 교차 — 생각하고(추론) 행동하고(도구) 관찰을 반복. 오늘날 에이전트 루프의 직접 조상 |
| **Reflexion** | arXiv 2303.11366 | 실패를 언어 피드백으로 "반성"해 다음 시도 개선 (자기 교정) |
| **Toolformer** | arXiv 2302.04761 | 모델이 스스로 어떤 API를 언제 부를지 학습 |
| **Generative Agents** | arXiv 2304.03442 | 기억·반성·계획을 가진 에이전트 (메모리 외부화의 학술 원형) |

> 🔵 실증 · 동료심사/arXiv. 현대 "에이전트 루프"는 마케팅 신조어가 아니라 **2022~2023 논문 계보** 위에 있다.

---

## 3. 효과 (주장과 한계)

- **핵심 효과**: 환경 피드백 루프 덕에 모델이 다단계·개방형 과제를 **자율적으로** 수행(탐색→계획→실행→검증). 단, *"Agentic systems often trade latency and cost for better task performance"* — **지연·비용을 성능과 맞바꾼다.**
- **단순성 우선(공식 anti-hype 입장)** 🟢: *"Finding the simplest solution possible, and only increasing complexity when needed"* / *"consider adding complexity only when it demonstrably improves outcomes"* / *"most successful implementations use simple, composable patterns rather than complex frameworks."*
  → 즉 **"에이전트·도구가 많을수록 좋다"는 통념을 벤더가 공식 부정.** 프레임워크 남용보다 단순·합성 가능한 패턴.

---

## 4. 신뢰도 판정

| 주장 | 등급 | 근거 |
|---|---|---|
| workflow/agent 정의, 에이전트 루프 정의 | 🟢 공식 | Anthropic 정의 verbatim, 3-0 |
| 5대 패턴, 단순성 우선 | 🟢 공식 | 복수 독립 분석 동일 해석, 반박 0, 3-0 |
| 하네스=scaffold, Agent SDK 루프 | 🟢 공식 | 단 SDK 4단계는 `often` 헷지(2-1) → "고정"으로 과장 말 것 |
| ReAct/Reflexion 등 기반 패턴 | 🔵 실증 | arXiv 1차 논문 |

**바이럴 주의** 🔴: "에이전트/멀티에이전트 = 만능"은 과장. Anthropic 본인이 단순성·비용 트레이드오프를 명시. 효율 논쟁은 [[08_Subagents_and_Multi_Agent]] 참조.

---

## 출처

- Anthropic, *Building Effective Agents* — https://www.anthropic.com/research/building-effective-agents
- Anthropic, *Building agents with the Claude Agent SDK* — https://www.anthropic.com/engineering/building-agents-with-the-claude-agent-sdk
- Anthropic, *Effective harnesses for long-running agents* — https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents
- ReAct (2210.03629), Reflexion (2303.11366), Toolformer (2302.04761), Generative Agents (2304.03442) — arxiv.org
