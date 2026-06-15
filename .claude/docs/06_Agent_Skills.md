---
tags:
  - ai
  - reference
  - skills
aliases:
  - Agent Skills
  - SKILL.md
  - progressive disclosure
description: Agent Skills의 정의, SKILL.md 구조, progressive disclosure 3단계 로딩, 작성 원칙과 효과
created: 2026-06-03
updated: 2026-06-03
---

← [[00_Index]]

# 06. Agent Skills (SKILL.md)

> **한 줄**: "에이전트가 필요할 때 발견·로드하는 명령·스크립트·리소스 폴더". 핵심은 **progressive disclosure**(필요한 만큼만 컨텍스트에 올림).
> 대표 신뢰도: 🟢 공식(Anthropic) + 🟡 실무합의(Simon Willison 호평). **3-0.**

---

## 1. 개념 / 정의

- *"organized folders of instructions, scripts, and resources that agents can **discover and load dynamically** to perform better at specific tasks."*
- 범용 에이전트를 **전문 에이전트**로: 도메인 전문성을 재사용 가능한 리소스로 패키징.
- CLAUDE.md와의 구분: 항상 로드되는 CLAUDE.md와 달리, Skill은 **관련될 때만** 로드 → 매 대화 토큰 부담 없음. → [[05_CLAUDE_md_and_AGENTS_md]]

---

## 2. SKILL.md 구조 & Progressive Disclosure

### 2.1 구조

```markdown
---
name: api-conventions          # 필수
description: REST API 설계 관례  # 필수 (언제 쓸지 판단 근거)
---
# 본문 = 지침/컨텍스트
- 규칙들...
# 참조 파일: forms.md, reference.md (선택적 로드)
```

- 디렉터리 1개 + `SKILL.md` 1개. frontmatter는 **name·description만 필수**.

### 2.2 Progressive Disclosure (핵심 설계 원칙) — 3단계

> *"the core design principle that makes Agent Skills flexible and scalable."*

| 단계 | 무엇이 로드되나 | 비유 |
|---|---|---|
| **① Startup** | 모든 설치된 skill의 **name + description만** 시스템 프롬프트에 사전 로드 — "언제 쓸지" 판단용 | 목차 |
| **② Trigger** | Claude가 관련 판단 시 **전체 SKILL.md**를 컨텍스트로 읽음 | 해당 챕터 |
| **③ On-demand** | 번들된 추가 파일/스크립트를 **필요할 때만** 탐색·로드 | 상세 부록 |

- 효과: 번들 가능한 컨텍스트가 **사실상 무제한** — *"the amount of context that can be bundled into a skill is effectively unbounded"*(파일시스템·코드실행 도구가 있으면 전부를 컨텍스트에 올릴 필요 없음).

### 2.3 적용 (Claude Code)

- 위치: `.claude/skills/<name>/SKILL.md`. 자동 적용되거나 `/skill-name`으로 직접 호출.
- **부수효과 있는 워크플로**(PR 생성 등)는 `disable-model-invocation: true`로 수동 트리거만 허용.
- **이식성**: Claude.ai · Claude Code · Agent SDK · Developer Platform 전반에서 동일 포맷 작동.

---

## 3. 작성 원칙 & 효과

작성 원칙(공식):
1. **평가로 시작** — 대표 과제로 능력 격차 먼저 확인하고 만든다.
2. **확장 구조** — 비대한 SKILL.md는 참조 문서로 분리, 상호배타 컨텍스트는 분리해 토큰 절약.
3. **Claude 관점으로** — 실제 트리거·사용 방식을 관찰하며 반복.
4. **Claude와 함께 반복** — 성공 패턴을 재사용 컨텍스트·코드로 포착.

효과: 결정론적 **코드 실행** + 단순·표준 포맷으로 **지식 공유** → 파편화된 커스텀 에이전트 개발의 대안.

> 🟡 실무합의: Simon Willison은 Skills를 *"이번에 나온 것 중 가장 중요한 발표일 수 있다"*고 호평(개념적 단순함·이식성). — `simonwillison.net/2025/Oct/16/claude-skills/`

---

## 4. 신뢰도 판정

| 주장 | 등급 |
|---|---|
| 정의·SKILL.md·progressive disclosure 3단계 | 🟢 공식 (3-0, eng. post verbatim) |
| 이식성(4개 제품)·작성 원칙 | 🟢 공식 |
| "Skills가 게임체인저" | 🟡 실무합의 (저명 실무자 호평, 단 장기 효과의 독립 벤치마크는 아직 적음) |

---

## 출처

- Anthropic, *Equipping agents for the real world with Agent Skills* — https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills
- Anthropic, *Agent Skills overview* (docs) — https://docs.claude.com/en/docs/agents-and-tools/agent-skills/overview
- Simon Willison, *Claude Skills* — https://simonwillison.net/2025/Oct/16/claude-skills/
