---
name: brb
description: demo13-flashlight(BRB/다녀올게) 프로젝트 전용 총괄. 다른 프로젝트에서 사용하지 않음. /brb 입력 시.
disable-model-invocation: true
paths:
  - docs/**
  - Assets/**
  - CLAUDE.md
metadata:
  project: demo13-flashlight
---

# BRB 총괄

## 프로젝트 한정 (필수)

워크스페이스에 `docs/MASTER.md`가 **없으면** 즉시 중단하고 안내:

> 이 스킬은 **demo13-flashlight (BRB)** 프로젝트 전용입니다. 해당 프로젝트를 워크스페이스로 열어주세요.

## 워크플로우

1. `docs/MASTER.md`, `CLAUDE.md` 읽기
2. [reference.md](../brb-shared/reference.md) 규칙 준수
3. 추가 지시 없으면: 게임 개요 + Stage 현황 + 아래 커맨드 안내

## 슬래시 커맨드

| 커맨드 | 용도 |
|--------|------|
| `/brb` | 프로젝트 총괄 (이 커맨드) |
| `/brb-roadmap` | 개발 로드맵·우선순위 |
| `/brb-play` | 게임플레이 시스템 (전투·인벤·경제·의료) |
| `/brb-world` | 월드·레이드·레벨·안전가옥 |
| `/brb-story` | 스토리·NPC·퀘스트 |
| `/brb-map` | 맵·렌더링·Unity 빌드 도구 |

키워드 예: `/brb-play combat`, `/brb-world scrapmarket`
