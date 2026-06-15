---
tags:
  - moc
  - ai
  - reference
aliases:
  - AI 하네스 위키
  - 에이전트 하네스 인덱스
  - AI 문서관리 방법론 인덱스
description: 현대 AI 코딩 에이전트 하네스·컨텍스트관리·CLAUDE.md·Skills·MCP·문서관리 방법론을 신뢰도 등급과 함께 정리한 레퍼런스 위키의 진입점(MOC)
created: 2026-06-03
updated: 2026-06-03
---

# AI 에이전트 하네스 & 문서관리 방법론 — 인덱스(MOC)

> 현대 AI 코딩 에이전트의 **"하네스(harness)"** 와, AI가 분석·정리한 **문서/지식을 관리하는 방법론**을, 다출처 교차검증 + 신뢰도 등급과 함께 정리한 레퍼런스 위키.
> 핵심 질문: **"이게 바이럴 과장인가, 실제 근거가 있는가?"** → 모든 항목에 출처 URL과 신뢰도 등급을 붙였다.

---

## 신뢰도 등급 범례 (전 노트 공통)

| 배지 | 등급 | 의미 |
|---|---|---|
| 🟢 | **공식** | 벤더 1차 출처 (Anthropic / OpenAI 공식 docs·엔지니어링 블로그·명세) |
| 🔵 | **실증** | 동료심사 논문·다중 벤더 재현·공개 벤치마크 |
| 🟡 | **실무합의** | 저명 실무자(Karpathy·Simon Willison 등)·잘 관리되는 OSS의 폭넓은 합의 |
| 🟠 | **일화/벤더보고** | 단일 사례 또는 벤더 자체 내부 벤치마크 (독립 재현 없음) |
| 🔴 | **바이럴-과장** ⚠️ | 출처 약한 "N배 빨라진다" 류 — 인용 시 주의 |

> ⚠️ **전체 한계 1줄**: 이 분야의 1차 근거는 대부분 **단일 벤더(Anthropic)** 에서 나온다. "공식"이라고 해서 "독립 검증됨"은 아니다 — 특히 **정량 효과 수치**(39%·84%·90.2% 등)는 벤더 내부 벤치마크다. 자세한 건 [[14_Credibility_Matrix]].

---

## 방법론 노트 (14)

### A. 하네스 · 컨텍스트 (엔진)

| # | 노트 | 핵심 한 줄 | 대표 신뢰도 |
|---|------|-----------|------|
| 02 | [[02_Agent_Loop_and_Harness]] | 에이전트 루프 = "도구를 루프에서 쓰는 LLM"; workflow vs agent; 5대 패턴; 단순성 우선 | 🟢🔵 |
| 03 | [[03_Context_Engineering]] | 컨텍스트 엔지니어링 정의·context rot·JIT 로딩·메모리 외부화 | 🟢🔵 |
| 04 | [[04_Token_Context_Management]] | /clear·/compact·context editing·프롬프트 캐싱·25k 절단; 토큰=비용 | 🟢 |

### B. 설정 · 확장 (스킬들)

| # | 노트 | 핵심 한 줄 | 대표 신뢰도 |
|---|------|-----------|------|
| 05 | [[05_CLAUDE_md_and_AGENTS_md]] | 메모리 파일 4계층·200줄 목표·넣을것/뺄것·AGENTS.md 표준(60k+ repos) | 🟢 |
| 06 | [[06_Agent_Skills]] | SKILL.md·progressive disclosure 3단계·언제 만드나 | 🟢 |
| 07 | [[07_MCP]] | "AI용 USB-C"·Tools/Resources/Prompts·생태계·보안(tool poisoning) | 🟢🔵 |
| 08 | [[08_Subagents_and_Multi_Agent]] | 컨텍스트 격리·멀티에이전트 효율 **논쟁**(90.2%↑ vs "만들지 마라") | 🟢🟡 |
| 09 | [[09_Hooks]] | 결정론적 강제(PreToolUse 등)·CLAUDE.md(권고)와의 차이 | 🟢 |
| 10 | [[10_Plan_Mode]] | 탐색→계획→구현 분리; 언제 쓰고 언제 건너뛰나 | 🟢 |

### C. 문서·지식 관리 (위키)

| # | 노트 | 핵심 한 줄 | 대표 신뢰도 |
|---|------|-----------|------|
| 11 | [[11_Knowledge_Management_Methods]] | Karpathy LLM Wiki(3계층·3연산)·MOC·frontmatter·PARA·Zettelkasten | 🟡 |
| 12 | [[12_Tool_Comparison_Obsidian_Notion]] | Obsidian vs Notion vs 순수MD vs Logseq — **AI 친화도** 기준 권고 | 🟡🟢 |
| 13 | [[13_Code_Docs_Consistency]] | 코드↔문서 stale 감지·source_refs·docs-as-code | 🟡🟢 |

### D. 종합

| # | 노트 | 핵심 한 줄 |
|---|------|-----------|
| 01 | [[01_Executive_Summary]] | **한눈 요약** — 무엇이 진짜이고 무엇이 과장인가 |
| 14 | [[14_Credibility_Matrix]] | 전체 신뢰도 종합표 + 바이럴 ⚠️ + METR 역설 |

---

## 발표 자료

- `Agent_Harness_Deck.html` — 한눈에 보는 HTML 풀덱 (이 폴더 안, 브라우저로 열기)
- `examples/dashbord/Dashbord_Harness_Compare.html` — dashbord 하네스 **Before/After 비교 덱** (전체화면 시 자동 확대)


---

## 연관 노트 (원인·해결책 겹침)

- [[02_Agent_Loop_and_Harness]] ↔ [[08_Subagents_and_Multi_Agent]] (orchestrator-workers 패턴이 멀티에이전트의 공식 근거)
- [[03_Context_Engineering]] ↔ [[04_Token_Context_Management]] (context rot → 토큰 관리 동기)
- [[03_Context_Engineering]] ↔ [[11_Knowledge_Management_Methods]] (메모리 외부화 = 노트로 컨텍스트 관리)
- [[05_CLAUDE_md_and_AGENTS_md]] ↔ [[09_Hooks]] (CLAUDE.md=권고 / Hook=결정론적 강제, 상호보완)
- [[11_Knowledge_Management_Methods]] ↔ [[12_Tool_Comparison_Obsidian_Notion]] ↔ [[13_Code_Docs_Consistency]] (AI 지식관리 3종 세트)

---

## 연구 방법 (재현 가능)

- **다출처 교차검증**: 5각도 병렬 웹검색 → 28+소스 fetch → 135주장 추출 → 상위 25주장 **3표 반론검증**(2/3 반박 시 폐기) → 종합. (deep-research 하네스 2회 + 1차 출처 직접 인용)
- **출처 우선순위**: Anthropic/OpenAI 공식 → arXiv/동료심사 → 저명 실무자 → 커뮤니티(검증 필요).
- 작성일 **2026-06-03** 기준. 이 분야는 빠르게 진화(다수 기능이 2025-09~2026-01 beta) → 버전·임계치는 변할 수 있음.
