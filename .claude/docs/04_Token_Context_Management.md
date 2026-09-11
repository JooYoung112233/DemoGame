---
tags:
  - ai
  - reference
  - context
  - token
aliases:
  - 토큰 관리
  - 컨텍스트 관리
  - 프롬프트 캐싱
  - context rot 실증
description: 토큰/컨텍스트 관리 실전 — context rot 실증(실효 길이<<광고치), /clear·/compact·context editing, 프롬프트 캐싱 비용·지연 수치
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 04. 토큰 / 컨텍스트 관리 (실전)

> **한 줄**: "큰 컨텍스트 윈도우 = 좋음"은 **틀렸다**. 실효 길이는 광고치보다 훨씬 짧고, 토큰은 곧 **비용·지연**이다. 그래서 **적게·정확히** 넣는 게 정답.
> 대표 신뢰도: 🔵 실증(context rot, 다수 동료심사) + 🟢 공식(캐싱 가격).

---

## 1. 왜 관리하는가 — Context Rot 실증 (🔵 강력)

> 이건 바이럴이 아니라 **다중 벤더·동료심사로 입증된 실증**이다. ([[03_Context_Engineering]]의 현상을 수치로)

- **실효 길이 ≪ 광고 길이**: NoLiMa(ICML 2025) — **32K에서 ≥128K 표방 13개 모델 중 11개가 자기 단문 성능의 50% 미만**으로 하락. GPT-4o도 단문 99.3% → 32K **69.7%**. RULER(COLM 2024) — ≥32K 표방 17개 중 **절반만** 32K에서 만족.
- **18개 모델·4벤더**(Anthropic·OpenAI·Google·Alibaba, Chroma): 입력이 길수록 **비균일 저하**. 단순 NIAH(건초더미 바늘찾기) 만점은 실제 장문 능력을 **과대평가**.
- **U자형 위치 의존**(Lost in the Middle, TACL): 정보가 **중간**에 있으면 정확도 급락. **윈도우만 키워선 해결 안 됨.**
- **초점 프롬프트 우세**: 관련 근거만 담은 짧은 프롬프트가 전체 장문 프롬프트를 **모든 모델 계열에서** 능가(LongMemEval: ~300토큰 vs ~113k토큰, 30~60%p 격차). → **"가장 작은 고신호 토큰셋" 원칙은 실증됨.**
  - ⚠️ 한정: 이는 "무관/방해 컨텍스트 제거"의 근거 — **"무조건 공격적으로 잘라내면 항상 이득"은 아님**.

---

## 2. 토큰 절감 기법 (운영)

| 기법 | 무엇 | 비고 |
|---|---|---|
| `/clear` | 무관 작업 사이 컨텍스트 **전체 리셋** | 가장 강력·단순 |
| `/compact [지시]` | 히스토리 **요약**(코드·결정 보존, 중복 출력 폐기) | auto-compaction은 한도 임박 시 자동 |
| **context editing** | 한도 임박 시 stale 도구호출/결과 **자동 정리**(서버사이드) | beta opt-in([[03_Context_Engineering]]) |
| 요약 체크포인트 | `Esc Esc`/rewind로 일부 구간만 요약 | |
| **메모리 외부화** | NOTES.md·claude-progress.txt로 컨텍스트 **밖** 저장 | [[11_Knowledge_Management_Methods]] |
| **subagent** | 탐색을 격리해 요약만 회수 | [[08_Subagents_and_Multi_Agent]] |
| 도구응답 절단 | Claude Code 도구응답 기본 **25,000토큰** 절단 | `MAX_MCP_OUTPUT_TOKENS` |

> 실패 패턴(공식): "주방 싱크대 세션"(무관 작업 누적), "2회 교정 후에도 틀림"(실패 접근이 컨텍스트 오염) → **`/clear` 후 더 나은 프롬프트로 새 세션**이 긴 세션보다 거의 항상 낫다.

---

## 3. 프롬프트 캐싱 — 토큰=비용 직접 절감 (🟢 공식)

> 변하지 않는 접두부(시스템 프롬프트·도구 정의·대용량 컨텍스트)를 캐시해 반복 호출 비용·지연을 줄인다.

### Anthropic
- **캐시 읽기(hit) = 기본 입력가의 0.1x (90% 할인)**. 예: Opus $5→$0.50, Sonnet $3→$0.30, Haiku $1→$0.10 (/MTok).
- **TTL 기본 5분**(재사용 시 무료 갱신), 선택 1시간(`cache_control ttl:"1h"`, 추가비용).
- 캐시 **쓰기는 프리미엄**(5분 1.25x / 1시간 2x) → 5분 캐시는 **1회 읽기**부터, 1시간 캐시는 **2회**부터 이득.

### OpenAI
- **자동 적용**(1024토큰↑), **지연 최대 80%↓·입력비용 최대 90%↓**.
- 할인율 **모델별 차등**: GPT-4o/4o-mini **50%**, GPT-4.1 **75%**, GPT-5.x 계열 **90%**.
- ⚠️ "90%"를 OpenAI 전체로 일반화 금지(GPT-4o는 ~50%). "최대"치는 짧은 프롬프트엔 미미.

> 💡 코딩 에이전트 함의: **안정적 부분을 프롬프트 앞에** 배치(캐시 적중↑), 변동 부분을 뒤로.

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| context rot·실효 길이·U자형·초점 우세 | 🔵 실증 (동료심사 다수, 3-0) |
| 캐싱 할인율·TTL·손익분기 | 🟢 공식 (1차 가격 docs, 3-0) |
| 정확한 수치(25k·200줄·90%·5분) | ⏱️ 시간민감 — 버전·날짜에 따라 변동(2026-06 기준) |

---

## 출처

- Chroma, *Context Rot* — https://www.trychroma.com/research/context-rot
- *Lost in the Middle* (TACL 2024) — https://arxiv.org/abs/2307.03172 · *NoLiMa* (ICML 2025) — https://arxiv.org/abs/2502.05167 · *RULER* (COLM 2024) — https://arxiv.org/abs/2404.06654
- Anthropic, *Prompt caching* / *Pricing* — https://platform.claude.com/docs/en/build-with-claude/prompt-caching
- OpenAI, *Prompt Caching* — https://openai.com/index/api-prompt-caching/
- Claude Code, *Best practices(컨텍스트 관리)* — https://code.claude.com/docs/en/best-practices
