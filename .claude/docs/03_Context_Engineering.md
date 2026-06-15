---
tags:
  - ai
  - reference
  - context
aliases:
  - 컨텍스트 엔지니어링
  - context rot
  - 메모리 외부화
description: 컨텍스트 엔지니어링의 정의(프롬프트 엔지니어링과 차이), context rot의 실증과 한계, JIT 로딩·compaction·노트 외부화 기법
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 03. 컨텍스트 엔지니어링 (Context Engineering)

> **한 줄**: 프롬프트 한 번 잘 쓰는 게 아니라, **추론 내내 컨텍스트 윈도우에 들어가는 토큰 전체를 큐레이션·유지**하는 기술.
> 대표 신뢰도: 🟢 공식(Anthropic 정의) + 🔵 실증(context rot은 다중 벤더·동료심사). **3-0.**

---

## 1. 개념 / 정의

### 1.1 정의 (vs 프롬프트 엔지니어링)

- **컨텍스트 엔지니어링** (Anthropic verbatim): *"the set of strategies for **curating and maintaining the optimal set of tokens (information)** during LLM inference, including all the other information that may land there outside of the prompts."*
- 프롬프트 엔지니어링이 **시스템 프롬프트 문구**에 집중한다면, 컨텍스트 엔지니어링은 멀티턴 에이전트에서 **시스템 명령·도구·MCP·외부 데이터·메시지 히스토리** 전체의 상태를 관리한다.
- 목표: *"the smallest possible set of **high-signal tokens** that maximize the likelihood of some desired outcome."* (최소한의 고신호 토큰셋)

> 🟢 공식 · `effective-context-engineering-for-ai-agents` · 3-0

### 1.2 Context Rot — "컨텍스트가 길수록 성능이 썩는다"

- **현상** 🔵: 토큰 수가 늘수록 모델의 정확한 회수 능력이 **저하**된다.
  - 독립 입증: **Chroma**(2025, 18개 모델 — Claude 4·GPT-4.1·Gemini 2.5·Qwen3 모두 저하), **Lost in the Middle**(Liu et al., TACL 동료심사 — U자형 위치 의존: 중간 정보일수록 놓침).
- **메커니즘 설명**(주의 ⚠️): Anthropic은 *"LLMs have an **attention budget** ... Every new token introduced depletes this budget"* 와 트랜스포머의 *"**n² pairwise relationships**"* 로 설명.
  - → **현상은 高신뢰, 인과 메커니즘은 "설명 은유"**다. Chroma는 메커니즘을 설명하지 못하며 저하가 **비균일(위치 의존)** 이라고 명시. "attention budget"을 검증된 인과로 인용하지 말 것.

> 🔵 실증(현상) · `trychroma.com/research/context-rot`, arXiv 2307.03172

---

## 2. 어떻게 적용하나 — 핵심 기법

| 기법 | 내용 | 근거 |
|---|---|---|
| **적정 고도(altitude) 시스템 프롬프트** | 너무 빡빡한 하드코딩 ↔ 너무 모호한 가이드 사이. XML/Markdown 섹션으로 구조화 | 🟢 |
| **도구 설계 효율** | 자기완결·오류에 강건·용도 명확. 비대한 도구셋 금지 → [[07_MCP]] | 🟢 |
| **JIT(just-in-time) 로딩** | 모든 데이터 선주입 대신 **경량 식별자**(파일경로·쿼리·링크) 보유 후 런타임에 동적 로딩. 최고 에이전트는 **선주입+자율탐색 하이브리드** | 🟢 |
| **Compaction(압축)** | 한도 임박 시 히스토리 요약 — 아키텍처 결정·미해결 버그는 보존, 중복 도구출력은 폐기 → [[04_Token_Context_Management]] | 🟢 |
| **구조적 노트(메모리 외부화)** | 에이전트가 컨텍스트 **밖** 파일(NOTES.md)에 노트 기록 후 나중에 회수 → [[11_Knowledge_Management_Methods]] | 🟢 |
| **Sub-agent 아키텍처** | 전용 에이전트가 깨끗한 컨텍스트에서 작업하고 **요약(1k~2k토큰)만 반환** → [[08_Subagents_and_Multi_Agent]] | 🟢 |

- **Claude Code가 정전 예시**: *"CLAUDE.md files are naively dropped into context up front, while primitives like glob and grep allow it to navigate ... **just-in-time**."* (선주입 = CLAUDE.md / 온디맨드 = grep)

### 2.1 메모리 외부화 — 2가지 공식 메커니즘

1. **Agentic memory / structured note-taking**: *"the agent regularly writes notes persisted to memory **outside of the context window**. These notes get pulled back ... at later times."* (Claude Code의 to-do, NOTES.md, Pokémon 플레이 예시)
2. **Memory tool**(공식 도구): *"store and consult information **outside the context window** through a file-based system"* — 전용 디렉터리에 CRUD(create/view/str_replace/insert/delete/rename), 대화 간 영속, **전적으로 client-side**(저장 백엔드는 개발자 선택).

> 사용자의 Obsidian frontmatter/lint 패턴과 직결되는 공식 사례 → [[13_Code_Docs_Consistency]]

---

## 3. 효과 (주장과 ⚠️ 한계)

- Anthropic **내부 평가**(agentic search): memory tool + context editing = **베이스라인 대비 +39%**, context editing 단독 **+29%**; 별도 100턴 웹검색 평가에서 **토큰 -84%**.
- ⚠️ **반드시 표기**: 이는 **벤더 자체 내부 벤치마크**로 **독립 재현되지 않았다**. 효과 수치를 인용할 땐 "Anthropic 내부, 미검증" 꼬리표 필수. → 🟠 일화/벤더보고.

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| 컨텍스트 엔지니어링 정의, JIT, compaction, 메모리 외부화 기법 | 🟢 공식 (3-0) |
| context rot **현상** | 🔵 실증 (다중 벤더·TACL) |
| context rot **메커니즘**(attention budget/n²) | 🟠 설명 은유 — 검증된 인과 아님 ⚠️ |
| +39%/+29%/-84% 효과 수치 | 🟠 벤더 내부 벤치마크 — 독립 미검증 ⚠️ |

---

## 출처

- Anthropic, *Effective context engineering for AI agents* — https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents
- Anthropic, *Managing context on the Claude Developer Platform* — https://www.anthropic.com/news/context-management
- Anthropic, *Memory tool* docs — https://platform.claude.com/docs/en/agents-and-tools/tool-use/memory-tool
- Chroma, *Context Rot* — https://www.trychroma.com/research/context-rot
- Liu et al., *Lost in the Middle* (TACL) — https://arxiv.org/abs/2307.03172
