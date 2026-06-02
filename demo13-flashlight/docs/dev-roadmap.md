# 개발 로드맵

## 현재 상태: 2단계 진행 중 (안전가옥 컨테이너 맵)

> 단계 표는 "루프 완성" 기준 계획선이다. 실제 코드는 일부 단계를 앞질러 골격이 들어와 있다
> (스토리/NPC/퀘스트/제작/의료/레이드/맵빌더 시스템 구현됨, 마무리·연동 미완).
> 미완성 항목은 아래 [미구현 / 마무리 필요](#미구현--마무리-필요-2026-05-29-코드-점검) 참조.

## 개발 원칙

- "나가서 뭔가 하고 돌아오는 루프"를 최대한 빨리 완성
- 전투보다 루프 흐름을 먼저 구축
- 각 단계마다 플레이 가능한 상태 유지

## 단계별 개발 순서

| 단계 | 내용 | 상태 |
|---:|---|---|
| **1** | 캐릭터 이동 + 상호작용 | ✅ 완료 |
| **2** | 안전가옥 컨테이너 맵 (시설 오브젝트 배치) | 🔄 진행 중 |
| **3** | 기본 인벤토리 (아이템 줍기/보관/꺼내기) | ⬜ 대기 |
| **4** | 폐상가 낮 맵 + 파밍 오브젝트 | 🔄 그레이박스 프리팹·빌더 추가 |
| **5** | 15분 타이머 + 탈출구 | ⬜ 대기 |
| **5.5** | 부위별 의료/치료 시스템 | ⬜ 대기 |
| **6** | 근접 전투 기본 (약공/강공/구르기/스태미너) | ⬜ 대기 |
| **7** | 적 AI + 밤 맵 (밴딧/몬스터, 낮→밤 전환) | ⬜ 대기 |
| **8** | 그로기/캔슬 + 전투 다듬기 | ⬜ 대기 |
| **9** | 안전가옥 강화 + 전당포 거래 | ⬜ 대기 |
| **10** | 이상현상 + 스토리 단서 | ⬜ 대기 |

## 각 단계 상세

### 1단계: 캐릭터 이동 + 상호작용
- 탑다운 WASD 이동
- 오브젝트 앞에서 상호작용 키 (E키 등)
- 카메라 따라가기
- 기본 애니메이션 (이동/대기)

### 2단계: 안전가옥 컨테이너 맵
- 컨테이너 내부 타일맵/오브젝트
- 초기 시설: 침대, 창고, 작업대, 지도판
- 시설 접근 → 상호작용 → 간단한 UI 표시
- 지도판에서 지역 선택 → 출전 흐름
- **[예정]** 지도판 UI: E키 → 지역 선택 팝업 UI → 지역 선택 후 씬 전환 (현재는 즉시 InGameScene 전환)
- **[예정]** 귀환 정산 UI: 필드→안전가옥 복귀 시 획득 아이템/경험치/화폐 정산 팝업 (현재 더미 화면 RaidResultUI 구현됨, 실제 데이터 연동 필요)

### 3단계: 기본 인벤토리
- 칸 기반 인벤토리 UI
- 아이템 데이터 구조
- 줍기 / 버리기 / 보관함 이동
- 가방 용량 제한

### 4단계: 폐상가 낮 맵 + 파밍
- 폐상가 맵 제작 (실내/실외 혼합)
- 파밍 오브젝트 10종 배치
- 아이템 드롭 테이블
- NPC 1명 + 쪽지 배치
- 안전가옥 ↔ 폐상가 씬 전환

### 5단계: 15분 타이머 + 탈출구
- 카운트다운 UI
- 탈출구 3개 (기본/조건/위험)
- 탈출 시 8초 대기
- 시간 초과 시 위험도 증가
- 탈출 성공 → 전리품 정산 → 안전가옥 복귀

### 5.5단계: 부위별 의료/치료 시스템
- 5부위: 머리, 몸통, 양팔, 왼다리, 오른다리
- 부상 3종: 출혈(DoT), 골절(기능저하), 통증(스태미너 저하)
- 치료: 구급상자(범용/느림) + 전용 아이템(붕대/부목/진통제, 빠름)
- UI: 캐릭터 실루엣 + 부위별 색상 상태
- 안전가옥 침대: 전체 완치
- 상세: `docs/medical.md` 참조

### 6단계: 근접 전투 기본
- 약공격 (빠름, 낮은 소모)
- 강공격 (느림, 높은 피해/그로기)
- 구르기 (무적 프레임, 스태미너 소모)
- 스태미너 게이지 (소모/회복)
- 히트박스/피격 판정
- 타격 정지 + 피격 반응

### 7단계: 적 AI + 밤 맵
- 밴딧 2종 AI (순찰/추격/공격)
- 몬스터 1종 AI
- 낮 약한 적 1종
- 폐상가 밤 버전 (조명 변경, 적 배치)
- 낮/밤 선택 UI

### 8단계: 그로기/캔슬 + 전투 다듬기
- 그로기 게이지 시스템
- 적 공격 캔슬 (예비동작 중 강공 적중)
- 처형 모션
- 타격감 세부 조정 (카메라 흔들림, 사운드)
- 무기 4종 차별화

### 9단계: 안전가옥 강화 + 전당포 거래
- 시설 강화 비용/효과
- 루디 수집 → 시설 업그레이드
- 전당포 NPC 거래 UI
- 귀중품 판매
- 강화 시 컨테이너 내 오브젝트 시각 변화

### 10단계: 이상현상 + 스토리 단서
- 반복 골목 이상현상
- 가짜 탈출구
- 멈춘 시계 이상현상
- 쪽지/사진 등 동생 단서
- 메인 미션 2개 + 서브 미션 3개

---

## 미구현 / 마무리 필요 (2026-05-29 코드 점검)

전체 165개 C# 스크립트 점검 결과. 핵심 시스템(전투·의료·제작·스토리·맵빌더)은 골격 완성, 주변 연동이 미흡하다.

> ⚠️ **이 점검은 2026-06-02 탑다운 전환 이전 기준.** 탑다운 전환으로 생긴 남은 작업(맵 페인팅·**시야 FOV 시스템**·**타격감 연출**·손전등 제거 등)은 [`topdown-migration.md`](topdown-migration.md) "남은 일" 참조.

### 🔴 높음 — 기능 동작에 직접 영향

| 항목 | 위치 | 내용 |
|------|------|------|
| ~~화폐(재화) 시스템 부재~~ | ~~`Quest/QuestManager.cs:113`, `Systems/AchievementManager.cs:146`~~ | ✅ 2026-05-30 해결 — `Systems/CurrencyManager.cs` 신설. 퀘스트·업적·레이드 이벤트 보상/페널티 연동 + 세이브 + HUD(우상단 ◈) + 토스트. `docs/economy.md` |
| ~~소비 아이템 스태미너 회복 미구현~~ | ~~`Inventory/PlayerInventory.cs`~~ | ❌ 2026-05-30 **기획 제거** — 스태미너 회복 소비 아이템 불필요 결정. 핸들링·`RestoreStamina()` 삭제, Coffee/EnergySoup/StimInjector 효과 None 전환. enum 값만 인덱스 보존용 유지 |
| 레이드 후 이벤트 조건 필터 미구현 | `Raid/PostRaidEventManager.cs:98` | `// TODO: region, nightOnly 조건 체크` — 지역/밤 조건 무시하고 랜덤 발동 |

### 🟡 중간 — UX·코드 품질

| 항목 | 위치 | 내용 |
|------|------|------|
| 아이템 검사 패널 UI 없음 | `UI/CharacterPanelUI.cs:1782` | `// TODO: 전용 검사 패널 UI (향후)` — 현재 콘솔 로그만 |
| ~~문 상호작용 피드백 토스트 없음~~ | ~~`Interaction/DoorController.cs:258`~~ | ✅ 2026-05-30 해결 — `UI/ToastManager.cs` 공용 토스트 신설(`ToastManager.Show`). 문 잠김/화폐 변동 등 연동 |
| 리플렉션으로 private 필드 접근 | `Interaction/DoorController.cs:245`, `NPC/NPCQuestMarker.cs:166`, `MapBuilder/Integration/MapObjectSpawner.cs:103,305` | `GetField(... NonPublic)` — 취약·리네임 시 무성 실패. 공개 Setter 권장 |
| `goto` 제어 흐름 | `Inventory/PlayerInventory.cs:87` | `goto doneHeal;` — `break`/조건문으로 정리 권장 |
| WorldItem 임시 스프라이트 | `Inventory/WorldItem.cs:40` | 🔸 2026-06-02 탑다운 전환으로 3D 큐브→2D `PlaceholderSprite.Square`(희귀도 색)로 개선. 최종 `worldDropPrefab`/아이템 아이콘 연결만 미완 |

### ⚪ 낮음 — 정리 대상

| 항목 | 위치 | 내용 |
|------|------|------|
| ~~Deprecated 빈 스텁~~ | ~~`MapBuilder/Rendering/BuildingCubeBuilder.cs`~~ | ✅ 2026-05-29 삭제 완료 (전역 `Building/RoofController.cs` 중복본, 빈 폴더 `Visual/`·`Safehouse/`도 함께 정리) |
| ~~미사용 FOW 시스템~~ | ~~`FogOfWar/FogOfWarSystem.cs`~~ | ✅ 2026-06-02 삭제됨(탑다운 전환). ⚠️ 단 **시야 FOV 기획이 새로 확정**(좀보이드식, 손전등 폐기) → 신규 시스템으로 재구현 예정. `rendering.md`/`combat.md` |
| ~~파일명 공백~~ | ~~`Resources/UI/RaidResultUI .prefab`~~ | ✅ 2026-06-02 삭제됨 |

### 서브시스템 완성도 요약

| 시스템 | 완성도 | 비고 |
|--------|:---:|------|
| Combat / Medical / Crafting / Story / MapBuilder / Lighting | 골격 완성 | 이벤트 기반 구조 양호, Editor State Preservation 규칙 준수 |
| Inventory | 부분 | 그리드 자료구조·UI 완성, 스태미너 회복 아이템은 기획 제거, 검사 패널 UI만 미완 |
| NPC / Quest / Raid | 부분 | 진행·추적 로직 구현, 화폐 보상 연동 ✅, **레이드 이벤트 조건 필터 미완** |
| UI 피드백 | 부분 | 공용 토스트(`ToastManager`) ✅, 아이템 검사 패널 미완 |

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-24 | 10단계 개발 로드맵 확정. 루프 완성 우선 원칙. |
| 2026-05-24 | 1단계 착수. IInteractable 인터페이스, InteractableObject, InteractionSystem, InteractableCreator 에디터 생성. |
| 2026-05-24 | 씬 전환 시스템 구현. SceneTransitionManager(페이드+스폰), SpawnPoint, ExitPoint 연결. |
| 2026-05-24 | Player DontDestroyOnLoad 싱글톤 구현. 씬 전환 시 카메라/NavMesh 재연결. InteractionSystem도 씬 로드 대응. |
| 2026-05-25 | 귀환 정산 더미 UI (RaidResultUI) 구현. 부위별 의료 시스템 기획 확정 (5부위/3부상/키트+전용). docs/medical.md 생성. |
| 2026-05-25 | 배터리 HUD, 안전가옥 충전, EventSystem 자동생성, UI 열림 시 입력차단, 안전가옥 timeScale=0 구현. |
| 2026-05-25 | 월드맵 7지구 이름·낮/밤 특성을 지도판에 반영 (`WorldRegionCatalog`, `MapSelectUI`). 1차 출전: 폐상가 교역만. |
| 2026-05-25 | 지역별 독립 시간 시스템 (RegionTimeManager) 구현. 맵보드 UI에 낮/밤 실시간 표시. |
| 2026-05-25 | 지역별 루트 테이블·전용 아이템 (`RegionLootCatalog`, `docs/region-loot.md`). |
| 2026-05-25 | Stage 3 인벤토리 기반 구축: ItemData SO, ItemDatabase, ItemInstance, InventoryGrid, PlayerInventory, WorldItem, SpawnTable, ItemSpawnPoint, LootContainer. docs/inventory.md 생성. |
| 2026-05-29 | 165개 스크립트 전수 점검. 미구현/마무리 항목 위 섹션에 정리(화폐 시스템·소비 스태미너·이벤트 조건·토스트 UI 등). 상태선 2단계로 정정. |
| 2026-05-29 | 죽은 코드 정리: `BuildingCubeBuilder.cs`(deprecated 스텁), `Building/RoofController.cs`(미사용 중복 — 실사용은 `IsometricMapEditor.RoofController` 스텁), 빈 폴더 `Visual/`·`Safehouse/` 삭제. GUID 검증으로 ViewCulling/ScrapMarketMapMetadata는 씬·프리팹 부착 확인 후 보존. |
| 2026-05-30 | 미완 항목 처리: ① **화폐 시스템**(`CurrencyManager` + 퀘스트/업적/이벤트 보상·페널티 연동 + 세이브 + HUD ◈ + `docs/economy.md`), ② **공용 토스트 UI**(`ToastManager`, 문 피드백·화폐 변동 연동). |
| 2026-05-30 | **스태미너 회복 소비 아이템 기획 제거.** 불필요 결정 → 핸들링·`PlayerController.RestoreStamina()` 삭제, Coffee/EnergySoup/StimInjector `useEffect` None 전환. `ItemUseEffect.RestoreStamina` enum 값은 직렬화 인덱스 보존 위해 deprecated로 유지. |
