---
tags:
  - ai
  - reference
  - plan-mode
aliases:
  - Plan mode
  - 계획 모드
  - explore-plan-code
description: Plan mode의 동작(탐색→계획→구현 분리), 언제 쓰고 언제 건너뛰나, 효과
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 10. Plan Mode (계획 모드)

> **한 줄**: 코딩 전에 "읽고·계획만" 하는 모드. **엉뚱한 문제를 푸는 것**을 막는다. 단, 작은 작업엔 오히려 오버헤드.
> 대표 신뢰도: 🟢 공식(Claude Code docs). **3-0.**

---

## 1. 개념 / 동작

- Plan mode에서 Claude는 **파일을 읽고 질문에 답할 뿐 변경하지 않는다.**
- 권장 워크플로 4단계 — **"Explore first, then plan, then code"**:
  1. **Explore**: plan mode 진입, 관련 파일·세션 흐름 파악 (변경 X).
  2. **Plan**: 상세 구현 계획 작성. `Ctrl+G`로 에디터에서 직접 수정 가능.
  3. **Implement**: plan mode 해제 후 계획대로 코딩, 계획 대비 검증.
  4. **Commit**: 서술적 메시지로 커밋·PR.

---

## 2. 언제 쓰고 언제 건너뛰나 (공식 가이드)

| 쓰면 좋음 | 건너뛰기 |
|---|---|
| 접근법이 불확실 | 한 문장으로 diff를 설명할 수 있는 작업 |
| 여러 파일 수정 | 오타·로그 추가·변수명 변경 |
| 익숙하지 않은 코드 | 범위 명확·수정 작음 |

> *"Plan mode is useful, but also adds overhead. ... If you could describe the diff in one sentence, skip the plan."*

연관: 큰 기능은 Claude가 **AskUserQuestion으로 먼저 인터뷰** → `SPEC.md` 작성 → 새 세션에서 깨끗한 컨텍스트로 실행하는 패턴도 공식 권장(스펙은 파일·인터페이스 명시 + 종단 검증 단계 포함).

---

## 3. 효과

- **핵심 효과**: 탐색/계획과 구현을 분리해 *"solving the wrong problem"* 을 예방. 계획을 사람이 검토·수정한 뒤 실행하므로 재작업↓.
- 검증과 결합: 구현은 항상 **검증 수단(테스트·빌드·스크린샷)** 과 함께 — 검증 없으면 출하 금지. → [[02_Agent_Loop_and_Harness]]의 "verify work" 단계.

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| 동작·4단계·언제쓰나·오버헤드 균형 | 🟢 공식 (best-practices verbatim) |
| "항상 계획부터" | 🟡 조건부 — 공식도 작은 작업엔 건너뛰라 명시(과적용 주의) |

---

## 출처

- Claude Code, *Best practices — Explore, plan, code, commit* — https://code.claude.com/docs/en/best-practices
- Claude Code, *Permission modes — Plan mode* — https://code.claude.com/docs/en/permission-modes
