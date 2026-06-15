---
tags:
  - ai
  - reference
  - hooks
aliases:
  - Hooks
  - Claude Code hooks
  - PreToolUse
description: Claude Code hooks의 결정론적 강제(권고형 CLAUDE.md와의 차이), 이벤트 목록, 설정 구조, 보안 경고
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 09. Hooks (결정론적 자동화)

> **한 줄**: 라이프사이클 지점마다 자동 실행되는 스크립트. CLAUDE.md가 "권고"라면 Hook은 **"보장"**.
> 대표 신뢰도: 🟢 공식(Claude Code docs). **3-0.**

---

## 1. 개념 — 권고 vs 결정론

- *"Hooks run scripts automatically at specific points in Claude's workflow. **Unlike CLAUDE.md instructions which are advisory, hooks are deterministic and guarantee the action happens.**"*
- 그래서 *"반드시·예외 없이 일어나야 하는 행동"* 은 CLAUDE.md가 아니라 Hook으로. → CLAUDE.md 한계를 메우는 **진짜 강제 계층**([[05_CLAUDE_md_and_AGENTS_md]] 참조).

---

## 2. 이벤트 (주요)

| 이벤트 | 언제 | 차단 가능? |
|---|---|---|
| **SessionStart** | 세션 시작/재개 | ✕ |
| **UserPromptSubmit** | 프롬프트 제출 직후, 처리 전 | ✔ |
| **PreToolUse** | 도구 호출 **전** (가장 많이 씀 — 위험 명령 차단) | ✔ |
| **PostToolUse** | 도구 성공 후 (예: 편집 후 lint) | ✔ |
| **PostToolUseFailure** | 도구 실패 후 | ✔ |
| **Stop** | Claude 응답 종료 시 (검증 게이트) | ✔ |
| **SubagentStart / SubagentStop** | 서브에이전트 생성/종료 | 종료만 ✔ |
| **PreCompact / PostCompact** | 컨텍스트 압축 전/후 | PreCompact만 ✔ |
| **SessionEnd / Notification** | 세션 종료 / 알림 | ✕ |

> 그 외 PermissionRequest, FileChanged, InstructionsLoaded, WorktreeCreate 등 다수. (전체는 공식 docs)

---

## 3. 설정 & 적용

```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "Bash",
      "hooks": [{ "type": "command", "if": "Bash(rm *)",
                  "command": "${CLAUDE_PROJECT_DIR}/.claude/hooks/block-rm.sh" }]
    }]
  }
}
```

- 3계층: **이벤트 → matcher(도구명 필터) → 핸들러**.
- 핸들러 타입: `command`(stdin으로 JSON 수신) · `http` · `mcp_tool` · `prompt`(단발 LLM 평가) · `agent`(복잡 검증용 서브에이전트).
- **차단 규약**: exit code **2** = blocking(행동 차단, stderr가 Claude에 전달) / exit 0 = 정상 / 기타 = 비차단.
- 위치: `~/.claude/settings.json`(전역) · `.claude/settings.json`(프로젝트 공유) · `.claude/settings.local.json`(개인).
- Claude가 직접 작성 가능: *"Write a hook that runs eslint after every file edit"*.

대표 용례: 편집 후 자동 lint/format, migrations 폴더 쓰기 차단, 커밋 전 테스트 게이트(Stop hook), 비밀키 유출 검사.

---

## 4. 보안 ⚠️ & 신뢰도

- ⚠️ *"Hooks are deterministic shell commands that run **with the user's credentials**. A malicious hook can read all your files and API keys ... Only use hooks you write yourself or trust completely."*
- 플러그인/외부 hook 설치 시 내용 검토 필수.

| 주장 | 등급 |
|---|---|
| 결정론적 강제·이벤트·exit2 차단·설정 구조 | 🟢 공식 (docs verbatim) |
| 보안 경고(사용자 권한 실행) | 🟢 공식 |

---

## 출처

- Claude Code, *Hooks reference* — https://code.claude.com/docs/en/hooks
- Claude Code, *Get started with hooks* — https://docs.claude.com/en/docs/claude-code/hooks-guide
