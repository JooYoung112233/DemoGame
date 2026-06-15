---
tags:
  - ai
  - reference
  - knowledge-mgmt
aliases:
  - LLM Wiki
  - Karpathy LLM Wiki
  - MOC
  - PARA
  - Zettelkasten
description: AI가 분석한 문서를 관리하는 방법론 — Karpathy LLM Wiki(3계층·3연산), MOC, frontmatter, PARA·Zettelkasten·Second Brain 비교
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 11. 문서·지식 관리 방법론

> **한 줄**: AI 시대의 핵심은 **"LLM이 유지보수하는 위키"** — 사람이 다 정리하지 말고, 영속적으로 **쌓이는(compounding)** 마크다운 지식베이스를 만들고 LLM에게 정리를 시킨다.
> 대표 신뢰도: 🟡 실무합의 (Karpathy 등 저명 실무자; 단 정량 벤치마크는 없음).

---

## 1. Karpathy "LLM Wiki" 패턴 (이 vault 하네스의 원형)

> Andrej Karpathy의 gist(442a6bf) — 개인 지식베이스를 **LLM이 유지보수**하게 만드는 패턴. 이 사용자의 `AN Games` 하네스 v2가 이걸 기반으로 한다.

### 3계층

| 계층 | 정의 |
|---|---|
| **Raw sources** | 불변 원본 — *"the LLM reads from them but never modifies them. This is your source of truth."* (코드·논문·로그) |
| **Wiki** | LLM이 생성한 markdown 파일들 — *"Summaries, entity pages, concept pages, comparisons, an overview, a synthesis."* |
| **Schema** | 위키 구조·관례·워크플로를 적은 문서 — *"e.g. **CLAUDE.md** for Claude Code or **AGENTS.md** for Codex."* → [[05_CLAUDE_md_and_AGENTS_md]] |

### 3연산

| 연산 | 정의 |
|---|---|
| **Ingest** | 새 소스를 읽고 요약·인덱스·엔티티 페이지 갱신·로그 추가. *"A single source might touch **10-15 wiki pages**."* |
| **Query** | 위키를 검색해 **인용과 함께** 답 합성. *"Good answers can be filed back into the wiki as new pages."* |
| **Lint** | 주기적 헬스체크 — 모순·stale 주장·고아(orphan) 페이지·누락 교차참조·데이터 공백. → [[13_Code_Docs_Consistency]] |

### 부기 파일
- **index.md**: 카탈로그 — *"each page listed with a link, a one-line summary, and optionally metadata."*
- **log.md**: append-only 활동 기록 — *"ingests, queries, lint passes."* 포맷 `## [YYYY-MM-DD] op | title` → `grep "^## \[" log.md` 로 조회.

> 핵심 원칙: *"the wiki is a **persistent, compounding artifact**"* — 매번 다시 도출하지 않고 지식이 **누적·강화**된다.

---

## 2. Obsidian 보조 기법

| 기법 | 내용 |
|---|---|
| **MOC (Map of Content)** | 관련 노트를 묶는 **허브 노트**. 폴더 대신 링크로 구조화. 이 vault의 L1→L2→L3 MOC 계층이 그 예 |
| **Frontmatter** | YAML 메타(tags·aliases·status·created/updated) — 검색·필터·자동화 기반 |
| **Backlink / Wikilink** | `[[파일명]]` 양방향 링크. "이 노트를 참조하는 곳" 자동 수집 |
| **Tags** | `#system/troop` 식 계층 태그로 횡단 분류 |

---

## 3. 전통 PKM 방법론 비교 (AI 이전)

| 방법론 | 창시 | 핵심 | AI 적합성 |
|---|---|---|---|
| **Zettelkasten** | Luhmann | **원자적 노트** + 고유 ID + 촘촘한 링크. 아이디어 단위로 쪼개 연결 | 링크 그래프가 LLM 탐색에 유리하나, 엄격한 형식은 과함 |
| **MOC 방식** | Nick Milo | Zettelkasten의 80/20 — 허브 노트로 느슨하게 묶기 | 실용적, LLM도 MOC를 진입점으로 사용 |
| **PARA** | Tiago Forte | **P**rojects/**A**reas/**R**esources/**A**rchives — *행동가능성* 기준 분류 | 폴더 기반, AI보다 사람 워크플로 중심 |
| **Building a Second Brain** | Tiago Forte | CODE(Capture·Organize·Distill·Express) | 캡처·증류 철학은 ingest와 유사 |

> 차이: 전통 PKM은 **사람이 유지보수**. LLM Wiki는 **LLM이 유지보수**하도록 설계(연산·스키마·로그) — 이게 AI 시대의 결정적 진화.

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| LLM Wiki 3계층·3연산·index/log | 🟡 실무합의 (Karpathy gist 1차, 널리 채택·재구현) |
| MOC·Zettelkasten·PARA 정의 | 🟡 실무합의 (확립된 커뮤니티 방법) |
| "위키가 compounding된다"는 효과 | 🟠 개념적·일화 — 정량 벤치마크 없음 ⚠️ |

> ⚠️ 주의: 이 영역은 **공식 벤더 문서나 동료심사가 거의 없다**(방법론·철학 중심). "이렇게 하면 N배 똑똑"식 주장은 근거가 약하니, **원리(외부화·누적·LLM 유지보수)** 위주로 받아들일 것.

---

## 출처

- Andrej Karpathy, *LLM Wiki* (gist 442a6bf) — https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f
- Nick Milo, *Maps of Content* (Obsidian forum) — https://forum.obsidian.md/t/mocs-vs-zettelkasten/106518
- Tiago Forte, *PARA / Building a Second Brain* — https://fortelabs.com/blog/para/
- (재구현 예) Astro-Han/karpathy-llm-wiki, cablate/llm-atomic-wiki (GitHub)
