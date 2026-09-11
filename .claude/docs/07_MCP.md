---
tags:
  - ai
  - reference
  - mcp
aliases:
  - MCP
  - Model Context Protocol
  - MCP 보안
description: MCP의 정의("AI용 USB-C")·client-server 구조·3요소(Tools/Resources/Prompts)·생태계·보안 이슈(tool poisoning)
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 07. MCP (Model Context Protocol)

> **한 줄**: AI 앱을 외부 데이터·도구·워크플로에 연결하는 **개방 표준** = "AI용 USB-C". 강력하지만 **보안(tool poisoning)** 이 약점.
> 대표 신뢰도: 🟢 공식(정의·구조) + 🔵 실증(보안 연구 arXiv/OWASP). **3-0.**

---

## 1. 개념 / 정의

- *"an **open-source standard** for connecting AI applications to external systems."* (Anthropic, 2024-11 발표)
- 비유: *"Think of MCP like a **USB-C port for AI** applications."* — 한 번 만들면 어디든 연결.
- 가능케 하는 것: 캘린더·Notion 연결, Figma 디자인→웹앱 생성, 다중 DB 분석, Blender 3D 등.

### 1.1 구조 & 3요소

- **client-server**: 호스트(AI 앱) ↔ 클라이언트 ↔ **MCP 서버**(데이터·도구 노출).
- 3 primitives:
  | 요소 | 역할 |
  |---|---|
  | **Tools** | 모델이 호출하는 함수(검색·계산·쓰기 등) |
  | **Resources** | 모델이 읽는 데이터(파일·DB 레코드) |
  | **Prompts** | 재사용 가능한 프롬프트/워크플로 템플릿 |

### 1.2 생태계

- 광범위 지원: Claude, **ChatGPT(OpenAI)**, VS Code, Cursor, Zed 등. → 사실상 업계 횡단 표준으로 자리.
- 도구 응답은 컨텍스트를 많이 먹을 수 있어 **pagination/filtering/truncation** 설계 필요(Claude Code는 MCP 포함 도구 응답을 기본 **25,000 토큰**으로 절단; `MAX_MCP_OUTPUT_TOKENS`로 상향) → [[04_Token_Context_Management]]

---

## 2. 어떻게 적용하나

- `claude mcp add`로 외부 도구(Notion·Figma·DB) 연결. CLAUDE.md/도구 설계는 [[02_Agent_Loop_and_Harness]] 원칙(자기완결·명확) 따름.
- 에이전트용 도구 설계: 여러 하위호출을 **상위 도구로 통합**(예: `get_customer_by_id`+`list_transactions`+... 대신 단일 `get_customer_context`).

---

## 3. 보안 — MCP의 약한 고리 ⚠️

> MCP의 'S'는 Security를 뜻한다는 농담이 돌 정도로 보안이 핵심 리스크.

| 위협 | 내용 | 등급 |
|---|---|---|
| **Tool poisoning** | 도구 **메타데이터(설명)** 에 악성 지시 삽입 → 모델이 따라감. OWASP LLM Top 10 **#1**, DREAD Critical(46.5/50) | 🔵 실증 |
| **Prompt injection** | 외부 데이터/도구 결과에 숨은 지시. 전통 경계보안은 **자연어 의미 수준** 공격에 무력 | 🔵 |
| **정의 변조(rug pull)** | 설치 후 도구가 **자기 정의를 변경** | 🔵 |
| **lethal trifecta** | (Simon Willison) ①민감 데이터 접근 ②비신뢰 콘텐츠 노출 ③외부 통신 — 셋이 겹치면 유출 위험 | 🟡 |

- 업계 대응: **OWASP MCP Top 10**(최초 표준 분류), CISA 공동 가이드(2025-05-22). 방어: 정적 메타데이터 분석·결정경로 추적·이상행동 탐지·사용자 투명성.

> 🔵 실증 · arXiv 2603.22489(MDPI 게재), Simon Willison, Checkmarx/SentinelOne

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| 정의·USB-C 비유·3요소·생태계 | 🟢 공식 (modelcontextprotocol.io) |
| tool poisoning·prompt injection 위협 | 🔵 실증 (OWASP·동료심사·CISA) |
| "MCP만 붙이면 안전하게 만능" | 🔴 과장 ⚠️ — 보안 미성숙. 신뢰 가능한 서버만, 권한 최소화 |

---

## 출처

- *What is MCP?* — https://modelcontextprotocol.io/introduction
- Anthropic, *Introducing the Model Context Protocol* — https://www.anthropic.com/news/model-context-protocol
- MCP Specification (2025-11-25) — https://modelcontextprotocol.io/specification/2025-11-25
- Simon Willison, *MCP has prompt injection security problems* — https://simonwillison.net/2025/Apr/9/mcp-prompt-injection/
- *MCP Threat Modeling ... Tool Poisoning* (MDPI) — https://www.mdpi.com/2624-800X/6/3/84
