---
name: brb-story
description: demo13-flashlight(BRB) 프로젝트 전용 스토리·NPC·퀘스트. 다른 프로젝트에서 사용하지 않음. /brb-story 입력 시.
disable-model-invocation: true
paths:
  - docs/**
  - Assets/**
  - CLAUDE.md
metadata:
  project: demo13-flashlight
---

# BRB 스토리·NPC

## 프로젝트 한정 (필수)

`docs/MASTER.md` 없으면 중단 — **demo13-flashlight 전용** 스킬.

## 담당 문서

`story.md` · `story-script.md` · `npc-dialogue.md` · `quest.md` · `quests-region1.md` · `gdd-progression.md`

## 핵심 코드

`StoryPlayer` · `StoryTriggerManager` · `NPCController.cs` · `NPCRelationshipManager.cs` · `QuestManager.cs` · `DialogueUI.cs`

## 워크플로우

1. 사용자 키워드에 맞는 문서만 읽기 (없으면 story+quest 우선)
2. [reference.md](../brb-shared/reference.md) 규칙 준수
3. 추가 지시 없으면 해당 영역 요약·상태·미완 보고

키워드 예: `story`, `npc`, `quest`, `dialogue`, `S-000`
