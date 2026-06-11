# 다녀올게 (Be Right Back) — 마스터 문서

> **이 파일은 demo13-flashlight의 모든 기획·기술 문서를 연결하는 인덱스다.**
> 새 세션에서 이 파일부터 읽으면 프로젝트 전체 상태를 파악할 수 있다.

---

## 게임 정보

| 항목 | 내용 |
|------|------|
| **제목** | 다녀올게 (Be Right Back) |
| **프로젝트명** | **BRB** (Be Right Back). 구 "Night City / 밤의 도시" 코드네임 폐기 |
| **장르** | 탑다운 2D 근접 생존 루팅 액션 |
| **엔진** | Unity 6 (URP **2D** Renderer) |
| **언어** | C# |
| **아트** | 탑다운 2D 스프라이트 (near-overhead 원근을 아트에 베이크), Spine 애니. 카메라는 2D Orthographic |
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
| **기획서 마스터 (3분할, 가장 먼저 읽을 것)** | 게임 전체 설계. ① [`gdd-core.md`](gdd-core.md) 코어 시스템(전투·루팅·낮밤·세계관) · ② [`gdd-progression.md`](gdd-progression.md) 진행·스토리(안전가옥·NPC·메인스토리·엔딩·월드) · ③ [`gdd-demo.md`](gdd-demo.md) 데모 범위·우선순위 | 확정 |
| [`dev-roadmap.md`](dev-roadmap.md) | 10단계 개발 로드맵 + 단계 상세 + **미구현/마무리 필요 목록**. 현재 Stage 2 | 진행 중 |
| [`backlog-ui.csv`](backlog-ui.csv) | **UI 제작 백로그** — 신규/확장/기존 UI 18종(우선순위·시스템·문서) | 2026-06-10 |
| [`backlog-impl.csv`](backlog-impl.csv) | **구현/테스트 백로그** — 시스템·데이터·밸런스 27건(의존·우선순위·문서) | 2026-06-10 |
| [`tooling.md`](tooling.md) | **에디터 도구 인덱스** — 모든 커스텀 메뉴를 `Tools/TopDown/`(Build/Combat/Map/Data/Utility)로 통합. Attack Editor 상세 | 참고 |
| [`concept-art-reference.md`](concept-art-reference.md) | 컨셉 아트 3종(피치/맵모듈/적) 해석·정리 | 참고 |
| [`art-needs.md`](art-needs.md) | **아트(VIEW) 필요 목록** — 시스템별 아트 에셋 집계(캐릭터/VFX/아이콘/맵/HUD), 스타일 기준선, 프랍/UI 카탈로그, P1~P6 제작 로드맵, 완료/필요 현황 | 정리 (2026-06-10) |
| [`prop-catalog.md`](prop-catalog.md) | **프랍 변형 카탈로그** — 실내/옥상/외벽/도로/차량/랜드마크 프랍을 기본219종×재질·방향변형으로 전부 펼침. 손그림 vs 셰이더 처리 원칙 + [`prop-list.csv`](prop-list.csv)(285행 작업 리스트) | 정리 (2026-06-10) |
| [`char-art.md`](char-art.md) | **캐릭터·적 애니메이션 리스트** — 플레이어/적/NPC 모션을 전부 펼침. 인간형 리그공유+스킨교체 원칙 + [`char-anim-list.csv`](char-anim-list.csv)(46행) | 정리 (2026-06-10) |
| **아트 작업 체크리스트(CSV)** | [`item-icon-checklist.csv`](item-icon-checklist.csv) 아이템 204종 · [`prop-list.csv`](prop-list.csv) 프랍 285행 · [`char-anim-list.csv`](char-anim-list.csv) 캐릭터 46행 | 생성됨 |

### ⚔️ 전투

| 문서 | 내용 | 상태 |
|------|------|------|
| [`combat.md`](combat.md) | 근접 전투 시스템 — 약공(3타 콤보)/강공(차징)/구르기/스태미너/그로기/적 캔슬. 수치 확정 | 프로토타입 완료 |

핵심 코드: `TopDownPlayer.cs` (이동·조준·손전등), `EnemyController.cs` (Rigidbody2D AI 상태머신), `StatDB` (스탯 DB)

### 🎒 인벤토리 · 아이템 · 제작

| 문서 | 내용 | 상태 |
|------|------|------|
| [`inventory.md`](inventory.md) | 격자 인벤토리 — 컨테이너 크기, 아이템 크기, ItemData SO 구조, 가격 체계, 내구도/스택 규칙 | 기획 확정, 코드 구현 |
| [`items.md`](items.md) | 아이템 **목록/데이터** (~172종 SO) — 치료·소비·무기·방어·재료·루디·귀중품·정보·잡템·이상현상. 우선순위(P0~P3) 태깅 | 확정, SO 생성 완료 |
| [`items-crafting-farming.md`](items-crafting-farming.md) | 아이템 **제작·획득** — 음식/조리·의료대 레시피·파밍 오브젝트 매핑·데모 구현 순서 (items.md에서 분리) | 확정 |
| [`crafting.md`](crafting.md) | RecipeData SO 구조, 해금 규칙(기본/문서), 조리대·작업대·의료대 레시피 목록, 무기 수리 규칙 | 확정, 코드 구현 |
| [`region-loot.md`](region-loot.md) | 7지구별 드롭 테이블, 지역 전용 아이템(14종), CSV→SO 파이프라인 | 확정 |

핵심 코드: `PlayerInventory.cs`, `InventoryGrid.cs`, `ItemData` SO, `ItemDatabase.cs`, `CraftingSystem.cs`, `LootContainer.cs`

### 💰 경제 · 화폐

| 문서 | 내용 | 상태 |
|------|------|------|
| [`economy.md`](economy.md) | **이원 경제** — 스크랩(일상 화폐, `CurrencyManager`) + 루디(`ruby_shard` 특수 자원). 퀘스트·상점·HUD(◈) 연동 | 기획 확정, 코드 구현 |

핵심 코드: `CurrencyManager.cs`, `GameBootstrap.cs`(자동 생성), `SaveManager.cs`(영속화), `GameHUD.cs`(표시)

### 🏥 의료

| 문서 | 내용 | 상태 |
|------|------|------|
| [`medical.md`](medical.md) | 5부위(머리/몸통/양팔/좌다리/우다리), 3부상(출혈/골절/통증), 치료 아이템·메커니즘 | 기획 확정, 코드 구현 |

핵심 코드: `PlayerMedicalSystem.cs`, `MedicalHUD.cs`, `MedicalItemData.cs`

### 🏠 안전가옥

| 문서 | 내용 | 상태 |
|------|------|------|
| [`safehouse.md`](safehouse.md) | 덕코프식 물리 공간 안전가옥 — 뒷골목 맵 레이아웃, 시설 목록, 확장 기획, NPC 배치, 가구 시스템 | 방향 확정 + 확장 기획 |
| [`safehouse-intel.md`](safehouse-intel.md) | **탐사 정보 루프** — 자원 생산 금지 원칙, NPC 랜드마크 파견(인텔 수집), 라디오(루디 가동), 랜드마크 재방문 확장 + Phase A~E 작업 계획 | 기획 확정, 구현 전 |
| [`safehouse-map-prompt.md`](safehouse-map-prompt.md) | 안전가옥 맵 컨셉 아트 프롬프트 | 참고 |
| [`safehouse-asset-list.md`](safehouse-asset-list.md) | 컨셉아트 기반 에셋 목록 — 바닥/펜스/프랍 분류 + 구현 우선순위 | 정리 완료 |
| [`safehouse-tile-prompt.md`](safehouse-tile-prompt.md) | 바닥/펜스/프랍 에셋 생성 AI 프롬프트 — 레퍼 첨부용 | 작성 완료 |

핵심 코드: `SafehouseStorage.cs`, `FurnitureData.cs`, Scene: `Safehouse.unity`

### 🗺️ 월드 · 레이드

| 문서 | 내용 | 상태 |
|------|------|------|
| [`world-map.md`](world-map.md) | 중앙 영야 코어 + 6개 외곽 지구 구조, 컨셉 아트 기반 거시 월드맵 | 컨셉 확정 |
| [`level-scrapmarket.md`](level-scrapmarket.md) | 폐상가 **레벨 디자인** — 상가골목(낮) 튜토 v6(건물기반 그레이박스) + 약국 조각 + 전체 지역1(1000×900) 통합 관계 | 진행 |
| [`level-apartment.md`](level-apartment.md) | **폐아파트** 레벨 디자인 — 랜드마크#2, 수직 4층 side-by-side 그레이박스 + 열쇠 체인 | 설계 |
| [`level-tower.md`](level-tower.md) | **유리 R&D 타워** 레벨 디자인 — 랜드마크#3, 저층→상층 R&D(카드키 게이트), 현상 빈발 | 설계 |
| [`raid.md`](raid.md) | 15분 타이머, 탈출 시스템, 루팅 흐름, 귀환 정산(RaidResultUI), 시간초과 페널티 | 기획 확정, 코드 구현 |
| [`post-raid-event.md`](post-raid-event.md) | 레이드 후 랜덤 이벤트 — 40% 확률, 텍스트 선택지, 보상/페널티 | 기획 확정, 코드 구현 |
| [`replayability.md`](replayability.md) | 반복성·엔드게임 progression — 장비 부품 모딩, 지역 격상, 밴딧 생태계, Co-op 멀티 | 검토 중 (확정 전) |

핵심 코드: `RaidManager.cs`, `SceneTransitionManager.cs`, `PostRaidEventManager.cs`, `WorldRegionCatalog.cs`

### 🧭 내비게이션 · 길찾기

| 문서 | 내용 | 상태 |
|------|------|------|
| [`navigation.md`](navigation.md) | **시계 나침반 + 미니맵/맵 시스템** — 시계 3역할(나침반·공명·시간역행 암시) 통합, RaidMap+fog, 통로 주석(사일런트힐식), 지도 아이템 해금 | 기반 구현(검토 중) |

핵심 코드: `RaidMapManager.cs`, `NavigationHUD.cs`, `MapZoneVolume.cs`, `PassageMarker.cs`, `CompassTarget.cs`, `MapFragmentReveal.cs` (`Assets/Scripts/Navigation/`)

### 👥 NPC · 퀘스트

| 문서 | 내용 | 상태 |
|------|------|------|
| [`npc-dialogue.md`](npc-dialogue.md) | 3축 호감도, 대화 시스템, **§5.5 NPC 배경·성격 프로필**(대사 작성 참조) | 기획 확정, 코드 구현 |
| [`quest.md`](quest.md) | 퀘스트 유형(수집/처치/탐색/배달), QuestData SO 구조, QuestManager 싱글톤 | 기획 확정, 코드 구현 |
| [`quests-region1.md`](quests-region1.md) | 1지역(폐상가) 전용 퀘스트 — MQ-001/002 + 반복 의뢰 8종 | 확정 |

핵심 코드: `NPCController.cs`, `NPCData` SO, `NPCRelationshipManager.cs`, `DialogueUI.cs`, `QuestManager.cs`, `QuestHUD.cs`

### 📖 스토리

| 문서 | 내용 | 상태 |
|------|------|------|
| [`story.md`](story.md) | **스토리 설계**("무엇/왜") — 세계관, 지역 아크, 동생 3분기 엔딩 조건, 단서 시스템 | 확정 |
| [`story-script.md`](story-script.md) | **구현 스크립트**("어떻게") — 1지역 씬별(S-000~) 대사·연출·분기 | 작성 중 |
| [`onboarding.md`](onboarding.md) | **온보딩 = 소식지(신문) 전면 UI** — 메타 시스템 8종 JIT 발행(해금 트리거 1:1), 신문 템플릿+텍스트 교체, 라디오와 짝 | 기획 확정, 구현 전 |

엔딩 3종: A(입양/생존) · B(사망/진실) · C(구출/희망) — 복수 엔딩, NPC 호감도+단서 수집으로 분기

### 🎨 렌더링 · 뷰

| 문서 | 내용 | 상태 |
|------|------|------|
| [`topdown-migration.md`](topdown-migration.md) | **아이소 → 탑다운 2D 전환** 배경·유지/제거/신규 목록·단계 계획 | 진행 중 |
| [`rendering.md`](rendering.md) | 순수 탑다운 2D — URP 2D Renderer, 2D Orthographic 카메라, Light2D, Tilemap, Prop2D, BRB/ 셰이더 10종 | 확정 |
| [`destructible.md`](destructible.md) | 파괴 가능 오브젝트 — `Breakable` + `BRB/DamageOverlay`(오버레이라 전 셰이더 호환), 단계별 부서짐→파괴, Health 자동 연동 | 구현 |
| [`topdown-art-spec.md`](topdown-art-spec.md) | AI 이미지 생성 스펙 — near-overhead 시점, 마젠타 배경, 플랫 라이팅, 엔진 조명값 | 작성 완료 |

### 🔧 맵 도구

| 문서 | 내용 | 상태 |
|------|------|------|
| [`map-tool.md`](map-tool.md) | 탑다운 2D 맵 도구 **사양·결정 로그** — Prop2D 카탈로그 + 씬 빌더 | 사용 가능 |
| [`map-tool-guide.md`](map-tool-guide.md) | 맵툴 **사용 설명서**(신규 사용자용) — 셋업→등록→배치→기능→저장→테스트 단계별 | 작성 완료 |

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
| TopDownPlayer | 이동(Rigidbody2D), 마우스 조준, 손전등(Light2D). Resources/TopDownPlayer 프리팹 자동 스폰 |
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
| 2026-05-29 | 문서 정리: `Night_City_System_Draft_v2.md`(1203줄) → `gdd-core`/`gdd-progression`/`gdd-demo` 3분할. `items.md` → `items-crafting-farming.md` 분리. story/story-script 역할 명시 + 오프닝 중복 제거. `quests-region1.md` 인덱스 추가. dev-roadmap에 미구현 목록 정리. |
| 2026-05-29 | **프로젝트명 BRB 확정.** "Night City / 밤의 도시" 표현 전면 정리 — 기획서 제목 → "BRB 기획서", 인게임 출전 화면 타이틀 → "다녀올게", `Mood.NightCity` → `Mood.NeonNight`, Unity 메뉴 `Tools/Night City/*` 3건 → `Tools/Dev Tools/*` 관례 통일 + 문서의 stale 메뉴 경로 교정. 한글 제목은 "다녀올게" 유지. |
| 2026-05-30 | 미완 시스템 구현 + 문서화: ① **화폐 시스템**(`CurrencyManager`, `docs/economy.md` 신설) ② **공용 토스트 UI**(`ToastManager`). dev-roadmap 미구현 목록 ✅ 처리. `economy.md`를 문서 맵에 등재(인덱스 누락 교정). + **스태미너 회복 소비 아이템 기획 제거**(불필요 결정, 3개 아이템 효과 None 전환). |
| 2026-05-30 | **건물 입장 트리거 시스템** 구현(`BuildingEntryTrigger` + 맵빌더 Trigger 오브젝트 4종 모드: 씬전환/로컬이동/스토리/커스텀). |
| 2026-06-02 | **아이소메트릭 → 탑다운 2D 전환.** 렌더 파이프라인 URP-3D→URP-2D, 카메라 2D Orthographic, 좌표계 XY+sortingOrder, 이동 NavMesh→Rigidbody2D, 조명 3D 스팟라이트→Light2D, 맵 커스텀 MapBuilder→Tilemap+Prop2D. 플레이어 `PlayerController`→`TopDownPlayer`, 적 Rigidbody2D 재작성. **문서 정리**: `rendering.md` 순수 2D로 재작성, `map-tool.md` 탑다운 도구로 교체, `topdown-migration.md`/`topdown-art-spec.md` 등재. **삭제**: `shader-system.md`(구 InkCity 셰이더 — 전부 삭제됨), `map-tool-guide.md`(구 런타임 빌더), `safehouse-map-prompt.md`(아이소 프롬프트 — `safehouse-tile-prompt.md`로 대체). |
| 2026-06-05 | **내비게이션 설계 신설(`navigation.md`).** 레이드부터 시계를 디제틱 도구로 — 나침반(튜토리얼/퀘스트 방향)·공명(이상현상)·**시간역행 암시(사망 연출)** 3역할 통합. 미니맵/맵 시스템 기반(RaidMap+fog+지도 아이템 해금) 초안. 사망=시간역행 서사 결정은 `story.md` 2026-06-05에 기록. 모두 **제안/검토 중**(구현 전). |
| 2026-06-05 | **내비게이션 기반 구현.** `Assets/Scripts/Navigation/` 7파일 — `RaidMapManager`(자동 스폰, 지역별 영속 발견/주석), `NavigationHUD`(하단중앙 나침반 + M홀드 미니맵 + 사망 역행 암시), `MapZoneVolume`/`PassageMarker`(잠김 자동·막힘 확률)/`CompassTarget`/`MapFragmentReveal`. 나침반은 탈출구 폴백으로 즉시 작동. 검사키 Tab→M(인벤 선점). 세이브 영속·아트 후속. |
| 2026-06-10 | **캐릭터 방향·감정표현 결정.** 플레이어·인간형 적 = **횡(측면)만**(상/하/대각 폐기, 좌우 플립), NPC = **정면만**. **표정 스프라이트 폐기 → 이모트(머리 위 말풍선 8종)로 감정 표현**(퀘스트 마커와 통합). `char-art.md`·`char-anim-list.csv`·`art-needs.md` §1 반영(NPC 포트레이트 표정 6종 계획 대체). |
| 2026-06-10 | **캐릭터·적 애니메이션 리스트 신설(`char-art.md` + `char-anim-list.csv`).** 플레이어 18클립(이동9+행동9·그립4종)·인간형 적 공용리그 10클립+스킨8종·이상체 전용7·NPC3로 펼침(46행). 원칙: 인간형은 Spine 리그 공유+스킨 교체, 측면1방향+좌우플립, 손상=셰이더. 적 로스터=gdd-core §10(낮 배회자/약탈꾼/잠복자, 밤 밴딧군+몬스터). 우선순위 P1 27/P2 11/P3 8. `art-needs.md` §1 연계. |
| 2026-06-10 | **강변 부두 랜드마크 제거(5→4) + 덤불/나무/도시시설/간판 프랍 추가.** ①사용자 결정: **강변 부두는 개발 난이도로 모든 기능 기획에서 제거** → 1지역 랜드마크 4개(상가/아파트/유리타워/식물원). `world-map.md` 정합 갱신(서브존표·스폰 S4 삭제→4·간선 A2/A4 종점 D2코너·§J⑤ 제거·현상차등), `level-scrapmarket.md`·`prop-list.csv`(LM5 8종 제거) 반영. 북쪽 한강은 경계+코너 탈출구로만 유지. ②프랍: 자연 확장(나무/**덤불**/화단 6→25)·도시시설 15·**간판 24**(글자없음). 합계 250종/손그림 347+9시트/334행. |
| 2026-06-10 | **프랍 추가: 도로/도시·차량·랜드마크 + 루팅 표기 제거.** 도로/도시 22(도로타일·신호등·표지판·가드레일 등)·차량 26(승용~버스 가로/세로 방향별·불탄차)·지역1 랜드마크 5개 구성 프랍(상가/아파트/유리타워/식물원돔+습지/강변부두). 루팅은 LootContainer 별도라 기능 열에서 전부 제거. 합계 219종/손그림 298+9시트/285행. |
| 2026-06-10 | **프랍 변형 카탈로그 신설(`prop-catalog.md` + `prop-list.csv`).** 인게임 꾸미기 프랍을 기본 147종 × 재질/변형으로 전부 펼침(177행). 원칙: 재질·의미토글만 손그림, 금간/오염 손상은 DamageOverlay 셰이더 자동 → 손그림 ~188 스프라이트 + 9데칼시트. 우선순위 P1 50/P2 68/P3 59. 추가로 `art-needs.md`에 §12 프랍 카탈로그·§13 UI 화면 전체 리스트, §5 라디오/파견 UI 상세화. |
| 2026-06-10 | **아트(VIEW) 필요 목록 정리(`art-needs.md`).** 전 시스템 문서에 흩어진 아트 요구사항을 시스템별(캐릭터/전투VFX/아이템아이콘/안전가옥/실내/라디오·인텔/레이드레벨/현상/HUD/스토리)로 집계. 스타일 기준선(80°/60°·탈채도·플랫·Prop2D 파이프라인) + `Assets/GPT/` 진행 현황 + P1~P6 제작 로드맵. 상세는 기존 전용 문서로 링크(중복 사양은 옮기지 않고 인덱스 역할). |
| 2026-06-10 | **안전가옥 확장 기획 신설(`safehouse-intel.md`).** 원칙 = 자원 생산 ❌ / 탐사 확장 ⭕ (농사·자동생산 금지). NPC 랜드마크 파견 + 라디오 + 랜드마크 재방문 확장의 3시스템 — 모두 "정보(인텔) → 월드에 새 탐사 목표 생성" 구조. Phase A~E 작업 계획 포함, Stage 9 이후 구현 권장. `safehouse.md` 시설 목록(라디오 보류→확정, NPC 파견 추가)·결정 사항 갱신. |
