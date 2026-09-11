---
tags:
  - ai
  - reference
  - credibility
aliases:
  - 신뢰도 매트릭스
  - 바이럴 vs 신뢰
  - METR 역설
description: 전체 방법론의 신뢰도 종합표(공식/실증/실무합의/일화/바이럴-과장)와 바이럴 과장 경고, METR 생산성 역설
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 14. 신뢰도 종합 매트릭스 — 바이럴 vs 진짜

> 사용자 핵심 요구에 대한 답: **무엇이 근거 있는 신뢰이고, 무엇이 바이럴 과장인가.**

---

## 0. 가장 중요한 한 가지 — METR 생산성 역설 🔵

> AI 도구의 효과를 **유일하게 RCT(무작위 대조)** 로 측정한 결과 (METR, 2025-07):

- 숙련 오픈소스 개발자 **16명**, 실제 이슈 **246개**.
- **예측**: AI로 **24% 빨라질 것** → **실제**: **19% 더 느려짐** → **사후에도** 여전히 *"20% 빨라졌다"* 고 **착각**.
- 교훈: **체감 ≠ 실측**. "N배 빨라졌다"는 **자기보고를 신뢰하지 말 것.** (단, 한정: 자기 repo를 잘 아는 숙련자·초기2025 도구 — 모든 맥락 일반화는 금지)

> 이 한 줄이 이 위키 전체의 태도다: **출처와 측정을 요구하라.**

---

## 1. 종합 매트릭스

🟢공식 · 🔵실증 · 🟡실무합의 · 🟠일화/벤더보고 · 🔴바이럴-과장⚠️

| 방법론 / 주장 | 등급 | 근거 한 줄 |
|---|---|---|
| 컨텍스트 엔지니어링(정의) | 🟢 | Anthropic 정식 정의 (3-0) |
| **context rot 현상** | 🔵 | Chroma 18모델·4벤더 + TACL·NoLiMa·RULER 동료심사 |
| context rot **메커니즘**(attention budget/n²) | 🟠 | Anthropic 설명 **은유** — 검증된 인과 아님 ⚠️ |
| JIT 로딩·compaction·메모리 외부화 | 🟢 | Anthropic 공식 |
| 메모리+context editing **+39% / −84%토큰** | 🟠 | **벤더 내부 벤치**, 독립 재현 없음 ⚠️ |
| 에이전트 루프·workflow/agent·5패턴·단순성 우선 | 🟢 | Building Effective Agents |
| ReAct·Reflexion·Toolformer | 🔵 | arXiv 동료심사 |
| CLAUDE.md 4계층·200줄·전달=user message | 🟢 | Claude Code docs |
| **"CLAUDE.md만 잘 쓰면 다 된다"** | 🔴 | 권고일 뿐 강제 아님 → Hook이 강제 ⚠️ |
| AGENTS.md 표준(60k+ repos·30+도구) | 🟢 | agents.md(Linux Foundation) |
| Agent Skills·progressive disclosure | 🟢 | Anthropic eng. post |
| MCP 정의·3요소·생태계 | 🟢 | modelcontextprotocol.io |
| **MCP 보안(tool poisoning)** | 🔵 | OWASP #1·CISA·동료심사 — 미성숙 ⚠️ |
| Subagent 컨텍스트 격리 | 🟢 | Claude Code docs |
| 멀티에이전트 **+90.2%** | 🟠 | 벤더 내부·**breadth-first 한정**(2-1) ⚠️ |
| **"멀티에이전트=N배 빠르다"** | 🔴 | **대부분 코딩에 명시적 반증**(Anthropic 본인) ⚠️⚠️ |
| 토큰이 성능 분산 80% 설명·멀티 15배 | 🟢🔵 | 1차 + 외부재현(arXiv) |
| Hooks 결정론적 강제 | 🟢 | Claude Code docs |
| Plan mode(탐색→계획→구현) | 🟢 | best-practices |
| 프롬프트 캐싱 0.1x·5분 / OpenAI 50–90% | 🟢 | 공식 가격 docs (⏱️시간민감) |
| "초점 토큰셋" 우세 | 🔵 | LongMemEval 등 다수 |
| Karpathy LLM Wiki(3계층·3연산) | 🟡 | gist 1차·널리 재구현 |
| MOC·PARA·Zettelkasten | 🟡 | 확립된 커뮤니티 방법 |
| "위키가 compounding된다" 효과 | 🟠 | 개념적 — 벤치 없음 ⚠️ |
| Obsidian=로컬.md / Notion=API·3req/s | 🟢 | 각 사 공식 docs |
| "코딩엔 로컬MD+git이 최적" | 🟡 | 검증가능 사실 기반 **추론**(벤치 없음) |
| **"Obsidian/Notion 중 하나가 절대 최고"** | 🔴 | 용도 무시한 의견(취향 싸움) ⚠️ |
| docs-as-code·심볼 stale 감지 | 🟡 | 확립된 실천 + 이 vault 구현(오탐 한계) |
| "AI가 코드↔문서 완전 자동 동기화" | 🟠 | aspirational·미성숙 ⚠️ |

---

## 2. 큰 그림 — 3가지 메타 결론

1. **골격은 진짜다** 🟢🔵: 컨텍스트 엔지니어링·context rot·에이전트 루프·CLAUDE.md·Skills·MCP·Hooks·Subagent는 **바이럴이 아니라 공식 문서/동료심사**로 뒷받침. 안심하고 채택.
2. **수치는 의심하라** 🟠: 효과 퍼센트(+39%·+90.2%·−84%)는 대부분 **벤더 내부 벤치**다. 인용 시 "내부·미검증" 꼬리표 필수.
3. **단일 벤더 의존** ⚠️: 이 분야 1차 근거 다수가 **Anthropic**에서 나온다. "공식"=/="중립 검증". 가능하면 **arXiv·다벤더·METR류 독립 측정**으로 교차확인.

---

## 3. "바이럴 과장" 블랙리스트 🔴 (인용 주의)

- "멀티에이전트/병렬이면 무조건 N배 빠르다" → 코딩엔 반증.
- "CLAUDE.md 한 장이면 완벽 제어" → 권고일 뿐, 강제는 Hook.
- "Obsidian(또는 Notion)이 그냥 최고" → 용도로 갈라야.
- "컨텍스트 윈도우 크면 다 넣어도 됨" → context rot로 반증.
- "AI가 알아서 문서까지 항상 최신화" → 감지는 OK, 자동 갱신은 미성숙.
- 자기보고 "AI로 N배 생산성↑" → METR이 체감≠실측 입증.

---

## 출처 (핵심)

- METR, *Measuring the Impact of Early-2025 AI on Experienced OS Developer Productivity* — https://metr.org/blog/2025-07-10-early-2025-ai-experienced-os-dev-study/
- 각 항목의 1차 출처는 해당 주제 노트([[02_Agent_Loop_and_Harness]]~[[13_Code_Docs_Consistency]]) 하단 참조.
