---
name: brb-map
description: demo13-flashlight(BRB) 프로젝트 전용 맵·렌더링·빌드 도구. 다른 프로젝트에서 사용하지 않음. /brb-map 입력 시.
disable-model-invocation: true
paths:
  - docs/**
  - Assets/**
  - CLAUDE.md
metadata:
  project: demo13-flashlight
---

# BRB 맵·기술

## 프로젝트 한정 (필수)

`docs/MASTER.md` 없으면 중단 — **demo13-flashlight 전용** 스킬.

## 담당 문서

`architecture.md` · `rendering.md` · `map-tool.md` · `map-tool-guide.md` · `tooling.md` · `destructible.md` · `topdown-migration.md`

## 핵심 코드

`SystemsScene.cs` · `Prop2DCatalogEditor` · `GameSceneBuilder` · `Assets/Editor/*`

## 워크플로우

1. 사용자 키워드에 맞는 문서만 읽기 (없으면 architecture+map-tool 우선)
2. [reference.md](../brb-shared/reference.md) 규칙 + Unity 메뉴 표 참고
3. 빌드 요청 시 `Tools/TopDown/` 메뉴 경로 안내
4. 추가 지시 없으면 해당 영역 요약·상태·미완 보고

키워드 예: `build`, `greybox`, `rendering`, `prop`, `systems`
