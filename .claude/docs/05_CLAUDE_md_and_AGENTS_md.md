---
tags:
  - ai
  - reference
  - memory-file
aliases:
  - CLAUDE.md
  - AGENTS.md
  - 프로젝트 메모리 파일
description: CLAUDE.md의 4계층·200줄 목표·넣을것/뺄것, 전달 메커니즘(user message)과 강제력 한계, AGENTS.md 표준화 동향과 상호운용
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 05. CLAUDE.md & AGENTS.md (프로젝트 메모리 파일)

> **한 줄**: 에이전트가 매 세션 읽는 "프로젝트 설명서". 짧고 인간이 읽을 수 있게. **길면 오히려 무시당한다.**
> 대표 신뢰도: 🟢 공식(Claude Code docs + agents.md 명세). **3-0.**

---

## 1. CLAUDE.md — 개념

- *"a special file that Claude reads at the start of every conversation"* — 코드만으론 추론 불가한 **영속 컨텍스트**(bash 명령·코드 스타일·워크플로 규칙).
- ⚠️ **전달 메커니즘이 중요**: CLAUDE.md는 시스템 프롬프트의 일부가 **아니라** *"delivered as a **user message** after the system prompt"* → Claude는 이를 *"context, not enforced configuration"* 으로 취급. **엄격한 준수 보장 없음**(특히 모호·충돌 명령).
  - 행동을 **확실히 차단**하려면 CLAUDE.md가 아니라 **PreToolUse hook** → [[09_Hooks]]. (이게 "CLAUDE.md 꿀팁"의 진짜 한계)

### 1.1 4계층 (넓은 범위 → 구체적 순서 로드)

| 계층 | 위치 | 비고 |
|---|---|---|
| Managed policy | `/Library/Application Support/ClaudeCode/CLAUDE.md`, `/etc/claude-code/CLAUDE.md`, `C:\Program Files\ClaudeCode\CLAUDE.md` | 조직 관리 |
| User | `~/.claude/CLAUDE.md` | 모든 세션 적용 |
| Project | `./CLAUDE.md` 또는 `./.claude/CLAUDE.md` | git 공유 |
| Local | `./CLAUDE.local.md` | 개인용(.gitignore) |

- **로드 타이밍**: 작업 디렉터리 **상위**의 파일은 시작 시 **전체 로드**, **하위** 디렉터리 파일은 그곳 파일을 읽을 때 **온디맨드** 로드(모노레포 유용).
- import: `@path/to/import` 문법으로 다른 파일 끌어오기(`@README.md`, `@~/.claude/my-rules.md`).

---

## 2. 어떻게 잘 쓰나 (공식 베스트 프랙티스)

### 2.1 크기 — 200줄 목표

- *"**target under 200 lines** per CLAUDE.md file. Longer files consume more context and reduce adherence."* 길어지면 **path-scoped rules**(경로 한정 규칙)로 분리.
- 핵심 휴리스틱: 각 줄마다 *"Would removing this cause Claude to make mistakes?"* — 아니면 삭제.
- ⚠️ *"Bloated CLAUDE.md files cause Claude to **ignore** your actual instructions!"*

### 2.2 넣을 것 / 뺄 것

| ✅ 넣기 | ❌ 빼기 |
|---|---|
| 추측 불가한 bash 명령 | 코드 읽으면 알 수 있는 것 |
| 기본과 다른 코드 스타일 규칙 | 표준 언어 관례 |
| 테스트 방법·선호 러너 | 상세 API 문서(링크로) |
| 레포 예절(브랜치·PR 규칙) | 자주 바뀌는 정보 |
| 프로젝트 특유 아키텍처 결정 | 파일별 코드베이스 설명 |
| 개발환경 quirk(필수 env) | "깨끗한 코드 작성" 같은 자명한 것 |

### 2.3 튜닝

- `/init`로 초기 생성(빌드·테스트·패턴 자동 감지).
- 강조: `IMPORTANT`, `YOU MUST` 추가하면 준수도↑.
- 도메인 지식·가끔 쓰는 워크플로는 CLAUDE.md 대신 **Skills**로 → [[06_Agent_Skills]] (온디맨드 로드, 매 대화 부담 0).
- "코드처럼" 다뤄라: 잘못되면 리뷰·가지치기·git 체크인.

### 2.4 Auto memory (v2.1.59+, 기본 ON)

- Claude가 **사용자 교정·선호를 보고 스스로** 노트 작성. 저장: `~/.claude/projects/<project>/memory/`, 인덱스 `MEMORY.md`.
- 로드: **MEMORY.md 첫 200줄(또는 25KB)** 만 세션 시작 시 로드, topic 파일은 **온디맨드** → 명시적 JIT/외부화 설계(이 vault의 frontmatter/lint 패턴과 동형).

---

## 3. AGENTS.md — 표준화 동향

- 정의: *"a simple, open format for guiding coding agents ... a **README for agents**: a dedicated, predictable place to provide context and instructions."*
- 채택: **60k+ 오픈소스 repo**, **30+ 도구**(OpenAI Codex, GitHub Copilot, Google Jules/Gemini CLI, Cursor, Zed, Aider, goose, Devin, Windsurf...). 거버넌스: **Linux Foundation**(Agentic AI Foundation).
- README와 분리: README=인간용, AGENTS.md=에이전트용(빌드·관례).

### 3.1 Claude Code와의 상호운용 (공식)

- *"Claude Code reads **CLAUDE.md, not AGENTS.md**."*
- 연결: CLAUDE.md에서 `@AGENTS.md` import, 또는 심링크 `ln -s AGENTS.md CLAUDE.md`.
- `/init`은 기존 `AGENTS.md`·`.cursorrules`·`.windsurfrules`를 읽어 통합.

> 🟢 공식 · `code.claude.com/docs/en/memory`, `agents.md`

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| 4계층·200줄·넣을것/뺄것·전달 메커니즘·auto memory | 🟢 공식 (3-0, docs verbatim) |
| AGENTS.md 60k+ repos·30+ 도구 | 🟢 공식 (agents.md 명세) |
| "CLAUDE.md만 잘 쓰면 다 된다" | 🔴 과장 ⚠️ — 권고일 뿐 강제 아님. xda-developers는 "CLAUDE.md helping is a myth" 반론 게재 → 강제는 [[09_Hooks]] |

---

## 출처

- Claude Code, *Memory (CLAUDE.md)* — https://code.claude.com/docs/en/memory
- Claude Code, *Best practices* — https://code.claude.com/docs/en/best-practices
- AGENTS.md — https://agents.md/
- OpenAI Codex, *AGENTS.md guide* — https://developers.openai.com/codex/guides/agents-md
- (반론) xda-developers, *CLAUDE.md ... is a myth* — https://www.xda-developers.com/claude-md-helping-your-projects-is-myth/
