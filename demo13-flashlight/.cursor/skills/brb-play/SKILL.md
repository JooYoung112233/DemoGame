---
name: brb-play
description: demo13-flashlight(BRB) 프로젝트 전용 게임플레이 시스템. 다른 프로젝트에서 사용하지 않음. /brb-play 입력 시.
disable-model-invocation: true
paths:
  - docs/**
  - Assets/**
  - CLAUDE.md
metadata:
  project: demo13-flashlight
---

# BRB 게임플레이

## 프로젝트 한정 (필수)

`docs/MASTER.md` 없으면 중단 — **demo13-flashlight 전용** 스킬.

## 담당 문서

`combat.md` · `inventory.md` · `items.md` · `crafting.md` · `region-loot.md` · `economy.md` · `medical.md` · `survival.md`

## 핵심 코드

`TopDownPlayer.cs` · `EnemyController.cs` · `PlayerInventory.cs` · `CraftingSystem.cs` · `CurrencyManager.cs` · `PlayerMedicalSystem.cs` · `StatDB`

## 워크플로우

1. 사용자 키워드에 맞는 문서만 읽기 (없으면 combat+inventory 우선)
2. [reference.md](../brb-shared/reference.md) 규칙 준수
3. 추가 지시 없으면 해당 시스템 요약·상태·미완 보고

키워드 예: `combat`, `inventory`, `crafting`, `economy`, `medical`
