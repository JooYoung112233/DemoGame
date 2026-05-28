# 다녀올게 (Be Right Back) — 마스터 문서

> **이 파일은 demo13-flashlight의 모든 기획·기술 문서를 연결하는 인덱스다.**
> 새 세션에서 이 파일부터 읽으면 프로젝트 전체 상태를 파악할 수 있다.

---

## 게임 정보

| 항목 | 내용 |
|------|------|
| **제목** | 다녀올게 (Be Right Back) |
| **장르** | 2.5D 아이소메트릭 근접 생존 루팅 액션 |
| **엔진** | Unity 2022+ (URP) |
| **언어** | C# |
| **아트** | 3D 큐브 월드 + 2D 빌보드 스프라이트 (Spine) |
| **핵심 루프** | 안전가옥 → 지역 선택 → 15분 레이드(파밍/전투) → 탈출 → 정산 → 안전가옥 성장 |
| **핵심 감정** | "한 번만 더 들어갈까?" / "지금 나갈까, 한 개 더 주울까?" |

---

## 현재 개발 상태

**로드맵 Stage 2 진행 중** (안전가옥 컨테이너 맵)

| 단계 | 내용 | 상태 |
|:---:|---|---|
| 1 | 캐릭터 이동 + 상호작용 | ✅ 완료 |
| 2 | 안전가옥 컨테이너 맵 | 🔄 진행 중 |
| 3 | 기본 인벤토리 | ⬜ 대기 |
| 4 | 폐상가 낮 맵 + 파밍 | 🔄 그레이박스 프리팹 추가 |
| 5 | 15분 타이머 + 탈출구 | ⬜ 대기 |
| 5.5 | 부위별 의료/치료 | ⬜ 대기 |
| 6 | 근접 전투 기본 | ⬜ 대기 (프로토타입 별도 완료) |
| 7 | 적 AI + 밤 맵 | ⬜ 대기 |
| 8 | 그로기/캔슬 + 전투 다듬기 | ⬜ 대기 |
| 9 | 안전가옥 강화 + 전당포 거래 | ⬜ 대기 |
| 10 | 이상현상 + 스토리 단서 | ⬜ 대기 |

상세: [`docs/dev-roadmap.md`](dev-roadmap.md)

---

## 문서 맵

### 🎯 기획 총괄

| 문서 | 내용 | 상태 |
|------|------|------|
| [`Night_City_System_Draft_v2.md`](Night_City_System_Draft_v2.md) | **기획서 마스터.** 게임 전체 설계 — 전투·루팅·낮밤·안전가옥·NPC·스토리·엔딩 분기·데모 범위. 가장 먼저 읽을 것 | 확정 |
| [`dev-roadmap.md`](dev-roadmap.md) | 10단계 개발 로드맵 + 각 단계 상세. 현재 Stage 2 | 진행 중 |
| [`concept-art-reference.md`](concept-art-reference.md) | 컨셉 아트 3종(피치/맵모듈/적) 해석·정리 | 참고 |

### ⚔️ 전투

| 문서 | 내용 | 상태 |
|------|------|------|
| [`combat.md`](combat.md) | 근접 전투 시스템 — 약공(3타 콤보)/강공(차징)/구르기/스태미너/그로기/적 캔슬. 수치 확정 | 프로토타입 완료 |

핵심 코드: `EnemyController.cs`, `PlayerController.cs` (전투 상태머신), `StatDB` (스탯 DB)

### 🎒 인벤토리 · 아이템 · 제작

| 문서 | 내용 | 상태 |
|------|------|------|
| [`inventory.md`](inventory.md) | 격자 인벤토리 — 컨테이너 크기, 아이템 크기, ItemData SO 구조, 가격 체계, 내구도/스택 규칙 | 기획 확정, 코드 구현 |
| [`items.md`](items.md) | 아이템 전체 목록 (~172종 SO) — 치료·소비·무기·방어·재료·루디·귀중품·정보·잡템·이상현상. 우선순위(P0~P3) 태깅 | 확정, SO 생성 완료 |
| [`crafting.md`](crafting.md) | RecipeData SO 구조, 해금 규칙(기본/문서), 조리대·작업대·의료대 레시피 목록, 무기 수리 규칙 | 확정, 코드 구현 |
| [`region-loot.md`](region-loot.md) | 7지구별 드롭 테이블, 지역 전용 아이템(14종), CSV→SO 파이프라인 | 확정 |

핵심 코드: `PlayerInventory.cs`, `InventoryGrid.cs`, `ItemData` SO, `ItemDatabase.cs`, `CraftingSystem.cs`, `LootContainer.cs`

### 🏥 의료

| 문서 | 내용 | 상태 |
|------|------|------|
| [`medical.md`](medical.md) | 5부위(머리/몸통/양팔/좌다리/우다리), 3부상(출혈/골절/통증), 치료 아이템·메커니즘 | 기획 확정, 코드 구현 |

핵심 코드: `PlayerMedicalSystem.cs`, `MedicalHUD.cs`, `MedicalItemData.cs`

### 🏠 안전가옥

| 문서 | 내용 | 상태 |
|------|------|------|
| [`safehouse.md`](safehouse.md) | 덕코프식 물리 공간 안전가옥 — 뒷골목 맵 레이아웃, 시설 목록, 확장 기획, NPC 배치, 가구 시스템 | 방향 확정 + 확장 기획 |
| [`safehouse-map-prompt.md`](safehouse-map-prompt.md) | 안전가옥 맵 컨셉 아트 프롬프트 | 참고 |

핵심 코드: `SafehouseStorage.cs`, `FurnitureData.cs`, Scene: `Safehouse.unity`

### 🗺️ 월드 · 레이드

| 문서 | 내용 | 상태 |
|------|------|------|
| [`world-map.md`](world-map.md) | 중앙 영야 코어 + 6개 외곽 지구 구조, 컨셉 아트 기반 거시 월드맵 | 컨셉 확정 |
| [`raid.md`](raid.md) | 15분 타이머, 탈출 시스템, 루팅 흐름, 귀환 정산(RaidResultUI), 시간초과 페널티 | 기획 확정, 코드 구현 |
| [`post-raid-event.md`](post-raid-event.md) | 레이드 후 랜덤 이벤트 — 40% 확률, 텍스트 선택지, 보상/페널티 | 기획 확정, 코드 구현 |

핵심 코드: `RaidManager.cs`, `SceneTransitionManager.cs`, `PostRaidEventManager.cs`, `WorldRegionCatalog.cs`

### 👥 NPC · 퀘스트

| 문서 | 내용 | 상태 |
|------|------|------|
| [`npc-dialogue.md`](npc-dialogue.md) | 3축 호감도(Affinity/Trust/Fear), 혼합형 대화 시스템, NPC 6명 설정 | 기획 확정, 코드 구현 |
| [`quest.md`](quest.md) | 퀘스트 유형(수집/처치/탐색/배달), QuestData SO 구조, QuestManager 싱글톤 | 기획 확정, 코드 구현 |

핵심 코드: `NPCController.cs`, `NPCData` SO, `NPCRelationshipManager.cs`, `DialogueUI.cs`, `QuestManager.cs`, `QuestHUD.cs`

### 📖 스토리

| 문서 | 내용 | 상태 |
|------|------|------|
| [`story.md`](story.md) | 메인 스토리라인 — 세계관, Act 1~3 구조, 전당포 주인 관계, 동생 3분기 엔딩 조건 | 확정 |
| [`story-script.md`](story-script.md) | 상세 스크립트 — Chapter 0(깨어남) 대사·연출·분기를 구현 수준으로 기술 | 작성 중 |

엔딩 3종: A(입양/생존) · B(사망/진실) · C(구출/희망) — 복수 엔딩, NPC 호감도+단서 수집으로 분기

### 🎨 렌더링 · 셰이더

| 문서 | 내용 | 상태 |
|------|------|------|
| [`rendering.md`](rendering.md) | 하이브리드 2D+3D — 바닥 2D Plane, 벽/건물 3D Cube, 라이팅(URP 3D Spot Light), 스텐실 시스템 | 확정, 구현 완료 |
| [`shader-system.md`](shader-system.md) | 잉크 아트 스타일 셰이더 8종(외곽선/그림자/밤오버레이/디졸브/빌보드 등), 머티리얼 네이밍 | 구현 완료 |

### 🔧 맵 도구

| 문서 | 내용 | 상태 |
|------|------|------|
| [`map-tool.md`](map-tool.md) | 아이소메트릭 맵툴 기획 — Unity Editor Extension, 좌표계, 건물 상태 전환 | 기획 확정 |
| [`map-tool-guide.md`](map-tool-guide.md) | 런타임 맵 빌더 사용 가이드 — 타일/벽/프롭/건물 배치, JSON 저장/불러오기 | 사용 가능 |

---

## 씬 구조

```
Safehouse (timeScale=0, 안전 허브)
  → MapSelectUI (지역 선택)
    → InGameScene (레이드 맵, 15분 제한)
      → 탈출 성공 → PostRaidEvent (40% 확률)
        → RaidResultUI (정산)
          → Safehouse 복귀
```

---

## 싱글톤 (DontDestroyOnLoad)

| 싱글톤 | 역할 |
|--------|------|
| PlayerController | 이동, 전투, 손전등, 스프라이트. Resources/Player 프리팹 자동 스폰 |
| SceneTransitionManager | 씬 전환, 페이드, 탈출 카운트다운 |
| UIManager | UI 상태 관리, 플레이어 입력 차단 |
| NPCRelationshipManager | NPC 3축 호감도 추적 |
| QuestManager | 퀘스트 진행/완료 추적 |
| PostRaidEventManager | 레이드 후 랜덤 이벤트 |

---

## 핵심 규칙 (새 세션 필독)

1. **Phaser Container 사용 금지** — 이 프로젝트는 Unity지만, depth 정렬 버그 방지를 위해 Container 패턴 사용 X
2. **Editor State Preservation** — 런타임 스크립트는 `Start()`에서 값을 적용하지 않음. 이벤트(`OnPhaseChanged` 등)로만 변경
3. **UI는 코드로 생성** — uGUI 프로시저럴 빌드, 씬에 배치하지 않음. 해상도 기준: 1920x1080
4. **기획 결정 즉시 기록** — `docs/` 내 시스템별 md에 날짜 + 질문 + 결정 기록. 세션 끝까지 미루지 않음
5. **StatDB 중앙 집중** — 모든 유닛/플레이어 스탯은 `StatDB.asset` SO에서 관리. 코드에 하드코딩 금지

---

## 변경 로그

| 날짜 | 내용 |
|------|------|
| 2026-05-28 | 마스터 문서 생성. 게임 제목 확정: 다녀올게 (Be Right Back) |
