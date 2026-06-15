---
tags:
  - ai
  - reference
  - knowledge-mgmt
  - tooling
aliases:
  - Obsidian vs Notion
  - 지식관리 도구 비교
  - AI 친화도
description: "AI가 읽고·쓰고·수정하기"에 가장 효율적인 지식관리 방식 비교 — Obsidian/Notion/순수 MD+git/Logseq, 객관적 AI 접근성 기준 권고
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 12. 도구 비교 — Obsidian vs Notion vs 순수 MD (AI 친화도)

> **사용자 질문**: *"Obsidian이 AI로 분석·코딩한 걸 관리하는 데 가장 효율적인가? Notion·순수 MD·다른 프로그램이 더 낫지 않나?"*
>
> **한 줄 답**: **코딩 에이전트가 읽고·쓰고·수정하는** 용도라면 **"로컬 평문 마크다운 + git"** 이 가장 효율적이고, **Obsidian은 바로 그 위에 얹힌 인간용 UI**라 잘 맞는다. Notion은 **비개발 협업·구조화 DB**엔 강하지만 **코딩 에이전트엔 API 마찰**이 크다.
> 신뢰도: 🟡 추론(검증가능 사실 기반) — ⚠️ **head-to-head 벤치마크는 없음**. "X가 최고"식 단정은 대부분 의견(바이럴).

---

## 1. 먼저, 무엇이 "의견"이고 무엇이 "사실"인가

- 🔴 **바이럴(의견)**: "Obsidian이 무조건 최고" / "Notion이 다 이긴다" 류 블로그·유튜브 비교글 — 대개 **용도를 안 가린 취향 싸움**. (이번 다출처 검증에서 이 영역은 **검증 통과 주장 0개** = 객관 근거 빈약)
- 🟢 **검증가능 사실**: 각 도구가 데이터를 **어디에·어떤 형식으로** 저장하는가. 이게 AI 접근성을 **결정**한다.

| 사실 | 근거 |
|---|---|
| **Obsidian**: 노트 = **로컬 폴더의 평문 `.md` 파일**(vault). 독자 포맷 아님 | obsidian.md/help (파일포맷 `.md`) |
| **Notion**: 콘텐츠 = **독자 블록 DB**, 접근은 **API만**. 평균 **3 req/sec**, 페이로드 500KB·블록 1000개, rich text 2,000자 제한 | developers.notion.com/reference/request-limits |
| **순수 MD+git**: 그냥 파일 — diff·grep·버전관리 네이티브. AGENTS.md/LLM Wiki의 기본 substrate | [[05_CLAUDE_md_and_AGENTS_md]], [[11_Knowledge_Management_Methods]] |

> **결정적 차이**: 코딩 에이전트(Claude Code·Codex·Cursor)는 **파일시스템**에서 동작한다. `.md` 파일은 **직접** 읽고/Edit/grep 가능. Notion 콘텐츠는 **API 뒤에** 있어 **간접**(MCP/통합 필요)이고 rate limit·블록 변환(마크다운 round-trip 손실)을 거친다.

---

## 2. AI 친화도 8축 비교

| 축 | 순수 MD+git | Obsidian | Logseq | Foam/Dendron(VS Code) | Notion |
|---|---|---|---|---|---|
| ① 에이전트 **filesystem 직접 R/W** | ✅ | ✅ (.md 그대로) | ✅ | ✅ (에디터가 코드editor) | ❌ API만 |
| ② **markdown 네이티브**(diff친화) | ✅ | ✅ | ◐ (outliner 변형) | ✅ | ❌ 블록DB |
| ③ **git 버전관리** | ✅ | ✅ (폴더라) | ✅ | ✅ | ❌ (API 동기화 필요) |
| ④ **API 의존/rate-limit** | 없음 | 없음 | 없음 | 없음 | ⚠️ 3 req/s |
| ⑤ **백링크/그래프** | ✗(수동) | ✅ 강력 | ✅ | ◐ | ✅ |
| ⑥ **검색·grep** | ✅ ripgrep | ✅ +UI | ✅ | ✅ | ◐ API 검색 |
| ⑦ **포맷 잠금(proprietary)** | 없음 | 없음 | 없음 | 없음 | ⚠️ 있음 |
| ⑧ **협업·비개발 공유** | ✗(git 필요) | ◐(유료 동기화) | ◐ | ✗ | ✅ **강점** |

> ①②③④⑦이 **AI가 읽고·쓰고·수정하기**의 핵심 → 로컬 마크다운 진영이 압도. ⑧만 Notion 우위.

---

## 3. 결론적 권고 (용도별)

| 용도 | 권고 | 이유 |
|---|---|---|
| **AI로 코딩·분석하며 관리** (이 사용자) | **로컬 MD+git**, 인간 UI는 **Obsidian** | 에이전트가 파일 직접 편집, git로 정합성·이력, grep로 회수. 코드 repo와 **같은 git 생태계** |
| 순수 자동화·CLI 중심 | **순수 MD+git** | UI 오버헤드 0, 가장 단순 |
| VS Code에서 코드+노트 같이 | **Foam/Dendron** | 코딩 에이전트와 같은 창 |
| **비개발자와 협업·DB·웹공유** | **Notion** | 구조화 DB·권한·실시간 협업. 단 AI는 MCP/통합으로 **간접** |

> 즉 **"Obsidian이 최고"가 아니라 "로컬 평문 마크다운이 AI 친화적이고, Obsidian은 그 좋은 UI"** 가 정확한 답. 사용자의 현재 셋업(**git 안의 Obsidian vault를 AI가 직접 .md로 편집**)은 사실상 **권고안 그 자체**다.

### Notion을 꼭 AI로 쓰려면
- Notion **MCP/공식 API 통합**으로 간접 접근 — rate limit(3 req/s)·블록 모델·마크다운 손실 감수. 대량 편집·grep·git diff엔 부적합.

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| Obsidian=로컬 .md / Notion=API·블록·3req/s | 🟢 공식 (각 사 docs) |
| "코딩 에이전트엔 로컬 MD+git이 가장 효율적" | 🟡 **추론** — 검증가능 사실(filesystem 접근)에서 도출, 단 head-to-head 벤치마크는 없음 |
| "Obsidian/Notion 중 하나가 절대 최고" | 🔴 바이럴 ⚠️ — 용도 무시한 의견. 위 표처럼 **용도로 갈라야** |

---

## 출처

- Obsidian, *Accepted file formats* — https://obsidian.md/help/file-formats
- Notion, *Request limits* — https://developers.notion.com/reference/request-limits
- Karpathy, *LLM Wiki* (markdown substrate) — https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f
- (관련) AGENTS.md(마크다운 표준) — https://agents.md/
