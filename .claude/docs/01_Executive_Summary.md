---
tags:
  - ai
  - reference
  - summary
aliases:
  - 한눈 요약
  - Executive Summary
description: AI 에이전트 하네스·문서관리 방법론 전체의 한눈 요약 — 무엇이 진짜이고 무엇이 과장인가
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 01. 한눈 요약 (Executive Summary)

> **질문**: AI 하네스·CLAUDE.md·Obsidian 같은 "AI로 분석·코딩한 걸 관리하는 방법"들, **진짜 효과인가 바이럴인가?**
> **답**: 골격(컨텍스트 엔지니어링·에이전트 루프·CLAUDE.md·Skills·MCP·Hooks)은 **공식 문서/동료심사로 입증된 진짜**다. 반면 **효과 수치와 "멀티에이전트=N배" "도구 최고" 류는 과장**이다.

---

## 🎯 핵심 7줄

1. **컨텍스트가 왕이다.** 모든 베스트 프랙티스의 뿌리는 하나 — *컨텍스트 윈도우는 차고 넘치면 성능이 썩는다(context rot).* 이건 🔵 **18개 모델·4벤더·동료심사로 입증**. → [[03_Context_Engineering]] · [[04_Token_Context_Management]]
2. **그래서 "적게·정확히".** "가장 작은 고신호 토큰셋" 원칙(JIT 로딩·`/clear`·compaction·메모리 외부화)은 🟢🔵 진짜. 큰 윈도우에 다 넣는 건 ⚠️ 반증됨.
3. **에이전트 루프는 학술 계보 위에 있다.** "도구를 루프에서 쓰는 LLM" = ReAct/Reflexion(🔵). Anthropic은 오히려 *"가장 단순한 해법부터"* 라며 **복잡성 남용을 공식 반대**. → [[02_Agent_Loop_and_Harness]]
4. **설정 3종은 역할이 다르다.** **CLAUDE.md**(권고·매세션) / **Skills**(온디맨드 전문지식) / **Hooks**(결정론적 강제). "CLAUDE.md만으로 다 된다"는 ⚠️ 과장 — 강제는 Hook. → [[05_CLAUDE_md_and_AGENTS_md]] · [[06_Agent_Skills]] · [[09_Hooks]]
5. **멀티에이전트는 조건부다.** 리서치성 병렬 탐색엔 +90.2%(🟠 벤더·토큰 15배), **그러나 대부분의 코딩엔 Anthropic 본인이 "부적합"**이라 명시. "병렬이면 N배 빠르다"는 🔴 바이럴. → [[08_Subagents_and_Multi_Agent]]
6. **관리 도구: "로컬 평문 마크다운 + git"이 AI 친화적이다.** Obsidian은 그 위의 좋은 UI. Notion은 API(3 req/s)·블록 모델이라 코딩 에이전트엔 간접·마찰. "어느 게 절대 최고"는 🔴 용도 무시 의견. → [[12_Tool_Comparison_Obsidian_Notion]] · [[11_Knowledge_Management_Methods]]
7. **체감을 믿지 마라.** METR RCT: 개발자들이 AI로 **19% 느려졌는데** 빨라졌다고 **착각**(🔵). → 항상 **출처·측정**을 요구. → [[14_Credibility_Matrix]]

---

## 📊 신뢰도 한눈에

| 등급 | 대표 항목 |
|---|---|
| 🟢 공식 | 컨텍스트 엔지니어링·에이전트 루프·CLAUDE.md 계층·Skills·MCP·Hooks·Plan·캐싱가격·Subagent 격리 |
| 🔵 실증 | context rot·lost-in-the-middle·ReAct/Reflexion·MCP 보안·METR·초점 토큰셋 |
| 🟡 실무합의 | LLM Wiki·MOC·PARA·Zettelkasten·docs-as-code·"로컬MD+git 최적"(추론) |
| 🟠 일화/벤더보고 | +39%·+90.2%·−84% 수치·"위키 compounding"·AI 자동 문서동기화 |
| 🔴 바이럴-과장 ⚠️ | "멀티에이전트=N배"·"CLAUDE.md면 완벽"·"Obsidian/Notion 절대최고"·자기보고 생산성 |

---

## 🧭 어디부터 읽나

- **개념부터**: [[02_Agent_Loop_and_Harness]] → [[03_Context_Engineering]] → [[04_Token_Context_Management]]
- **내 질문(도구 선택)**: [[12_Tool_Comparison_Obsidian_Notion]]
- **바이럴 판별**: [[14_Credibility_Matrix]]
- **발표 자료**: `Agent_Harness_Deck.html` (브라우저로 열기)
