---
tags:
  - ai
  - reference
  - knowledge-mgmt
  - consistency
aliases:
  - 코드 문서 정합성
  - stale 감지
  - docs-as-code
  - source_refs
description: 코드↔분석문서 정합성 유지 — docs-as-code, 심볼/git diff 기반 stale 감지, AI 자동 갱신의 효과와 한계(오탐)
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 13. 코드 ↔ 분석문서 정합성 (Stale 방지)

> **문제**: 코드를 분석해 문서로 정리해두면, **코드가 바뀌는 순간 문서는 낡는다(stale)**. 어떤 문서가 재검토 대상인지 사람이 일일이 추적할 수 없다.
> **한 줄 답**: 문서를 **코드처럼 버전관리**하고(docs-as-code), 문서가 참조한 **코드 심볼**을 추적해 **git이 바뀐 문서를 자동 플래그**한다. 단 **감지는 가능, 완전 자동 갱신은 위험**.
> 신뢰도: 🟡 실무합의 + 🟠 일부 일화 — ⚠️ 오탐율 정량 근거는 없음.

---

## 1. docs-as-code (🟡 실무합의)

- 문서를 **코드와 동일하게** 다룬다: 평문(markdown)·git 버전관리·PR 리뷰·CI 검사·코드와 **같은 repo**.
- 효과: 변경 이력 추적, 리뷰로 품질 게이트, 코드 변경과 문서 변경이 **같은 커밋/PR**에 묶임 → 정합성↑.
- 학술 뿌리: **Literate programming**(Knuth) — 코드와 설명을 엮어 하나의 산물로. (개념적 기반)

---

## 2. Stale 감지 메커니즘

### 2.1 심볼/파일 참조 추적 + git diff
패턴(이 vault의 `AN Games` 하네스가 실제 구현):
1. 분석문서 frontmatter에 **참조 코드**를 기록 — `source_refs: ["MKSocket.cs::HandleRallyCreateStartSally"]` (파일+**심볼**, 라인번호 금지).
2. lint이 각 문서에 대해 `git -C <코드repo> log --since=<문서.updated> -- <해소된 경로>` 실행.
3. **문서 작성 후 코드가 바뀐** 문서를 ⚠️ "재검토 후보"로 **보고(report-only)**.

> 이는 Karpathy LLM Wiki의 **Lint 연산**(모순·stale·orphan 점검)의 구체화 → [[11_Knowledge_Management_Methods]].

### 2.2 ⚠️ 한계 — 오탐(false positive)
- **파일 단위 git diff는 오탐이 많다**: 4,400줄짜리 공용 파일의 한 줄만 바뀌어도 그 파일을 참조한 **모든 문서**가 stale로 뜬다.
- 완화: **심볼 단위**로 좁히기(파일+함수/클래스), **report-only**(자동 수정 금지). 그래도 심볼 해소가 불완전하면 오탐 잔존.
- ⚠️ 오탐율을 **정량화한 재현 벤치마크는 없다**(이번 다출처 검증에서도 이 영역 검증 통과 주장 0).

---

## 3. "AI가 코드 바뀌면 문서 자동 갱신" — 효과 vs 한계

| 단계 | 현실성 |
|---|---|
| **감지**(어떤 문서가 stale인가) | 🟡 실용적 — git+심볼 추적으로 가능 |
| **사람에게 플래그** | 🟡 권장 — 우선순위 큐로 |
| **AI가 자동 재작성** | 🟠 위험 — 환각·과수정 가능. **human-in-the-loop 필수**, 자동 커밋 금지 |

> 권고: **감지·플래그는 자동화, 갱신은 사람 승인.** 코드 진실(ground truth)을 다시 읽고([[02_Agent_Loop_and_Harness]]) 문서를 고친 뒤 `updated` 갱신·log 기록.

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| docs-as-code(평문·git·리뷰) | 🟡 실무합의 (확립된 실천) |
| 심볼+git diff stale 감지 | 🟡 추론·구현 사례(이 vault) — 오탐 한계 명시 |
| "AI가 코드↔문서 완전 자동 동기화" | 🟠 일화/aspirational ⚠️ — 자동 갱신은 미성숙 |

---

## 출처

- *Docs as Code* — https://www.writethedocs.org/guide/docs-as-code/
- (개념) Knuth, *Literate Programming*
- 구현 사례: 이 vault `AN Games/Analysis/Analysis_AI_Convention.md`(§12 source_refs·stale 감지), `tools/lint_wiki.py`
- (관련 연구) 코드-주석 일관성 — https://arxiv.org/abs/2307.04291
