# 다녀올게 (Be Right Back) — 마스터 문서

> **이 파일은 demo13-flashlight의 모든 기획·기술 문서를 연결하는 인덱스다.**
> 새 세션에서 이 파일부터 읽으면 프로젝트 전체 상태를 파악할 수 있다.

---

## 게임 정보

| 항목 | 내용 |
|------|------|
| **제목** | 다녀올게 (Be Right Back) |
| **프로젝트명** | **BRB** (Be Right Back). 구 "Night City / 밤의 도시" 코드네임 폐기 |
| **장르** | **3D 루팅 RPG 슈터** — 돈벌이·좋은 아이템 루팅 + 총격전 (2026-09-11 방향, 레퍼런스 낙원) |
| **엔진** | Unity 6.6 (6000.6.0f1), URP **3D** (Forward, `URP-3D.asset`) |
| **언어** | C# |
| **아트** | **3D 로우폴리**(치비 2.5등신) — 플레이어·밴딧·마을·은신처. 게임 전용 셰이더 `BRB/GameLit` + 포스트 프로세싱. 카메라 **오소 정면 탑다운 62°**(방위 0°) |
| **텍스처 방향** | **B 만화풍** — 진한 표면 선과 직접 그린 색면 그림자. 승인된 플레이어 B를 유지하고 NPC·현용 적·장비·실내외 모델에 아틀라스 6종/공용 표면 8종 적용. 마을 생활 프랍 38개 추가 배치(2026-09-12). [rendering.md](rendering.md)·[char-art.md](char-art.md)·[safehouse.md](safehouse.md) |
| **핵심 루프** | 마을(안전구역) → 지역 선택 → 20분 레이드(루팅/총격전) → 탈출 → 정산 → 마을·은신처 |
| **핵심 감정** | "한 번만 더 들어갈까?" / "지금 나갈까, 한 개 더 주울까?" |

---

## 현재 개발 상태

**2026-09-11 기준 — 이어서 작업할 땐 [`dev-handoff.md`](dev-handoff.md)부터.**

| 항목 | 상태 |
|---|---|
| **3D 전환** | ✅ Stage 0~4 완료(2026-09-07~09 — 좌표계·물리·조명·맵·캐릭터). ⬜ Stage 5(2D 잔재 삭제) 남음 → [`3d-migration.md`](3d-migration.md) |
| **볼륨 축소** | ✅ 2026-09-09 — 타르코프식 하드코어를 RPG식으로(부위 의료·소음·격자 인벤·파견 폐기, 특성 11종) → [`scope-cut.md`](scope-cut.md) |
| **방향** | 2026-09-11 — **낙원식 RPG: 돈벌이·좋은 아이템 루팅 + 전투는 총과 근접 둘 다 주력(좀보이드식).** 맨손 공격 폐기, 구르기는 모션 전까지 꺼 둠 |
| **최근 구현** (PR #16~#22, 병합 대기) | 총기 밴딧(권총·소총) · 플레이어 총격(조준원·반동·탄 스펙) · 배그식 사망 루팅(래그돌·희귀도 빛기둥·고철 드랍) · 게임 전용 셰이더 `BRB/GameLit` + 후처리 · 착용등·낮 주황/저녁 보라 · 카메라 정면 62° |
| **지금** | **시스템 정리 6단계** 진행 중 — 1 입구 문서 ✅ → 2 전투·총기 ✅ → 3 루팅·경제 → 4 거점 → 5 조명 잔재·레이드/실내 → 6 폴더·거대 파일. [`dev-roadmap.md` §시스템 정리](dev-roadmap.md) |

> 옛 「로드맵 Stage 1~10」 표(2D 근접 게임 기준, Stage 2 진행 중)는 현실과 맞지 않아 여기서 뺐다 — 기록은 [`dev-roadmap.md`](dev-roadmap.md)에 남아 있다.

---

## 문서 맵

### 🎯 기획 총괄

| 문서 | 내용 | 상태 |
|------|------|------|
| **기획서 마스터 (3분할, 가장 먼저 읽을 것)** | 게임 전체 설계. ① [`gdd-core.md`](gdd-core.md) 코어 시스템(전투·루팅·낮밤·세계관) · ② [`gdd-progression.md`](gdd-progression.md) 진행·스토리(안전가옥·NPC·메인스토리·엔딩·월드) · ③ [`gdd-demo.md`](gdd-demo.md) 데모 범위·우선순위 | 확정 |
| 🌐 **세계관 SSOT** | 세계 설정·로어의 **단일 출처 = [`gdd-core.md §5`](gdd-core.md)** (시대·무대 / 짙은현상 / **빛의역설** / **루디=충전·방전·마모 활성 자원** / 시간왜곡). 타 문서는 재서술 금지·링크만. story.md §2=스토리 사건 순서, anomaly.md=현상 메커닉, world-map.md=지리 컨셉(모두 §5로 위임) | 확정 |
| [`dev-protocol.md`](dev-protocol.md) | ⭐ **기능 추가 규칙(단일 게이트)** — "기능 하나 = 열고 같은 세션에 닫는다". BEFORE(SSOT 찾기·재서술 금지)/DURING(5대 런타임 규약·GameTuning)/DONE 체크리스트(기획기록·MASTER동기화·로드맵·검증·브랜치PR)/정리 주기 | 규칙 (2026-07-10) |
| [`dev-roadmap.md`](dev-roadmap.md) | 10단계 개발 로드맵 + 단계 상세 + **미구현/마무리 필요 목록**. 현재 Stage 2 | 진행 중 |
| ✂️ [`scope-cut.md`](scope-cut.md) | **볼륨 축소 SSOT (2026-09-09)** — 타르코프식 하드코어를 걷어내고 RPG처럼 단순화한 6건의 결정과 범위. 무엇을 왜 잘랐나 · 자르지 **않기로** 한 것 · 딸려 사라진 것 · 되살리는 법 | 6/6 완료 |
| [`dev-handoff.md`](dev-handoff.md) | **개발 핸드오프** — 다른 PC에서 이어서 작업할 때 "지금 위치" 요약(최신 작업·다음 후보·Unity 검증 대기) | 갱신 중 |
| [`backlog-ui.csv`](backlog-ui.csv) | **UI 제작 백로그** — 신규/확장/기존 UI 18종(우선순위·시스템·문서) | 2026-06-10 |
| [`backlog-impl.csv`](backlog-impl.csv) | **구현/테스트 백로그** — 시스템·데이터·밸런스 27건(의존·우선순위·문서) | 2026-06-10 |
| [`balance.md`](balance.md) | **밸런스 단일 컨트롤 표면** — 어떤 밸런스 값이 어디 있나 색인. GameTuning 필드 전체(시간·레이드·수색·드랍·생존·수면·아노말리) + 데이터 파일(region_loot=맵드랍, StatDB=전투, reputation csv) | 2026-06-18 |
| [`tuning.md`](tuning.md) | **Control Panel 메커니즘** — GameTuning SO + Tools▸TopDown▸컨트롤 패널(필드 자동 노출)·맵 통계. 값 추가법 | 참고 |
| [`editor-tools.md`](editor-tools.md) | **에디터 도구 카탈로그** — Tools▸TopDown 메뉴 전체(밸런스·빌드·맵·초기설정) 무엇/파일 | 2026-06-10 |
| [`ai-images.md`](ai-images.md) | **AI 이미지 정리** — 만든 것(GPT ~143장 인벤토리)·적용상태(아이콘 0/204)·만들 것 요약 | 2026-06-10 |
| [`tooling.md`](tooling.md) | **에디터 도구 인덱스** — 모든 커스텀 메뉴를 `Tools/TopDown/`(Build/Combat/Map/Data/Utility)로 통합. Attack Editor 상세 | 참고 |
| [`concept-art-reference.md`](concept-art-reference.md) | 컨셉 아트 3종(피치/맵모듈/적) 해석·정리 | 참고 |
| [`art-needs.md`](art-needs.md) | **아트(VIEW) 필요 목록** — 시스템별 아트 에셋 집계(캐릭터/VFX/아이콘/맵/HUD), 스타일 기준선, 프랍/UI 카탈로그, P1~P6 제작 로드맵, 완료/필요 현황 | 정리 (2026-06-10) |
| [`prop-catalog.md`](prop-catalog.md) | **프랍 변형 카탈로그** — 실내/옥상/외벽/도로/차량/랜드마크 프랍을 기본219종×재질·방향변형으로 전부 펼침. 손그림 vs 셰이더 처리 원칙 + [`prop-list.csv`](prop-list.csv)(285행 작업 리스트) | 정리 (2026-06-10) |
| [`char-art.md`](char-art.md) | **캐릭터·적 애니메이션 리스트** — 플레이어/적/NPC 모션을 전부 펼침. 인간형 리그공유+스킨교체 원칙 + [`char-anim-list.csv`](char-anim-list.csv)(46행) | 정리 (2026-06-10) |
| [`character-modeling-workflow.md`](character-modeling-workflow.md) | **캐릭터 모델링 재사용 기준** — 시안 확인→비율 승인→부위별 디테일. 승인 원본·비율·실패 방지 기준 | 비율 승인 / 디테일 진행 (2026-09-08) |
| [`ui-reference.md`](ui-reference.md) | ⭐ **UI 룩 SSOT** — 레퍼런스 캐논 = **이중 언어**(종이=문서형 / 프레임=기능형, 앵커 This War of Mine) + **28패널 배분표**(종이9·프레임15·기물1·무프레임3) + 판정 기준 3개(손에 드는가 / 레이드당 10회 이상 만지는가 / 스크롤·격자·드래그) + 미구현 UI 10종 사전 배정 | **확정 (2026-07-29)** |
| [`ui-resources.md`](ui-resources.md) | **UI 리소스 발주 스펙** — 아트가 UI 스프라이트·시스템 아이콘을 그려 납품하기 위한 기준. ⚠️시안 13장 해상도 부족(1366 목업 1:1 컷 → 1920 대비 1.41× 업스케일) 전량 재납품 · 9-slice 규약 · 상태 변형 · 아이콘 64px 그리드 · 네이밍/납품/임포트 + [`ui-asset-list.csv`](ui-asset-list.csv)(~144항목) | 정리 (2026-07-29) |
| **아트 작업 체크리스트(CSV)** | [`item-icon-checklist.csv`](item-icon-checklist.csv) 아이템 204종 · [`prop-list.csv`](prop-list.csv) 프랍 285행 · [`char-anim-list.csv`](char-anim-list.csv) 캐릭터 46행 · [`ui-asset-list.csv`](ui-asset-list.csv) UI 파츠 53+아이콘 17세트 | 생성됨 |
| [`item-icon-list.md`](item-icon-list.md) · [`item-icon-additions.md`](item-icon-additions.md) | **아이콘 제작 리스트(footprint별)** + **아이콘↔데이터 매칭/신규 등록·보류** 정리 | 정리 (2026-06-10) |
| [`prop-production.md`](prop-production.md) | **프랍 생성 진행 추적** — 맥락 씬 1장 생성→개별 슬라이스 방식, 슬라이스 규칙 | 정리 (2026-06-10) |

### 🧱 기술 · 아키텍처

| 문서 | 내용 | 상태 |
|------|------|------|
| [`architecture.md`](architecture.md) | **씬/부트 구조 SSOT** — 영속 `Systems` 씬(매니저+UIManager+모든 UI+PlayerRig) + 게임플레이 씬 additive 교체 로드, 부트스트랩 폴백, **입력 시스템(신 Input System + `GameInput` 셰임)** | 구현 |
| [`controls.md`](controls.md) | **컨트롤/입력 매핑 SSOT** — 키보드·마우스 + **게임패드** 전체 매핑표. 셰임 확장 방식(액션 에셋 없이 `Gamepad.current` 병합), 조준 디바이스 전환(`PadActive`), T2(메뉴 UX)/T3(격자 인벤) 남은 단계 | T0/T1 구현 |
| [`ui-prefab-plan.md`](ui-prefab-plan.md) | **UI 프리팹화 SSOT** — 코드 절차 생성 → 프리팹 베이크(`UIPrefabBaker`)/Instantiate 전환(§4-A 전 패널 완료), `[SerializeField]`/`WireEvents`/`ApplyFonts` 패턴, 시안 스킨(`UISkin`, §4-B 보류) | §4-A 완료 |
| [`save.md`](save.md) | **저장/체크포인트** — 세이브 스커밍 방지(레이드 진행=인메모리 스냅샷, 디스크는 안전 맥락만, 크래시 1회 커밋). `SaveManager`/`SaveCheckpoints`/`CombatStateTracker` | 구현 |
| [`qa.md`](qa.md) | **QA 자동화 SSOT** — `QaBot`(가상 입력층으로 게임 코드 수정 0) · `QaHeatmap` · PASS/FAIL 판정 · 외부 GUI 대시보드 · `qa-runner`↔`dev-fixer` 자동 루프. 이어서 = 스킬 `/QA이어서`. **2026-09-09 게임 런타임에서 분리** — `Game.QA` 어셈블리 + `QA_ENABLED` 정의 심볼(평소 빌드엔 미포함, `Tools ▸ TopDown ▸ QA`로 토글) | 구현·**Unity 미검증** · 분리됨 |

### ⚔️ 전투

- [밴딧 총기 시안](bandit-firearms.md) — 권총·돌격소총 무료 모션 재사용, 수납/조준/이동/사격 컨펌용.

- [밴딧 방망이 손목 진단](bandit-wrist-diagnosis.md) — 2026-09-11, 한 손 휴대 자세의 손목 과회전 수정·31개 검사 통과.

| 문서 | 내용 | 상태 |
|------|------|------|
| [`combat.md`](combat.md) | 전투 — **총과 근접(방망이·검 등) 둘 다 주력, 좀보이드식**(2026-09-11 §무기 구성 결정). 총격(조준원·반동·탄 스펙, §총기), 배그식 사망 루팅(§사망 루팅). 맨손 공격 폐기, **구르기는 모션 전까지 꺼 둠**, 무기 전환 = 퀵슬롯 숫자키, 근접 = 공격 순간 커서 쪽. **총 수치는 `WeaponData` 한 곳**(밴딧도 참조). 2D 시절 섹션은 제목에 표시해 둠. ⚠️ 문서 첫머리는 아직 "소울라이크 근접" — 정리 2단계에서 재작성 | 개정 필요 |
| [`enemy-ai.md`](enemy-ai.md) | 적 AI 설계. ⚠️ 2026-09-09 폐기된 소음 시스템 서술이 남아 있음 — 정리 2단계에서 갱신 | 개정 필요 |
| [`traits.md`](traits.md) | **캐릭터 특성(퍽) 시스템** — 진행형 퍽 트리 + 부정 특성(환급). **2026-09-09 41종 → 11종 축소**(기준 = 코드가 실제로 읽는 `effectKey`가 있는 퍽만). ⚠️ **★현상 트리 7종 전부 삭제** — 기획은 §3.6에 보존, 재개통 1순위 | SO **11종**·`TraitManager`·`TraitPanelUI`(K) 구현 |

핵심 코드: `TopDownPlayer.cs` (이동·바라보는 방향·근접 — 이름만 TopDown), `PlayerGun.cs` (플레이어 총격), `EnemyController.cs` (3D AI 상태머신 — 근접·총기 밴딧), `CorpseMarker.cs`·`BanditRagdoll.cs` (사망 루팅), `StatDB` (스탯 DB)

### 🎒 인벤토리 · 아이템 · 제작

| 문서 | 내용 | 상태 |
|------|------|------|
| [`inventory.md`](inventory.md) | **슬롯형 인벤토리** — **2026-09-09 테트리스식 격자 폐기**(아이템 1개=슬롯 1칸, 90도 회전·컨테이너 수색 연출 제거, 무기 파츠 4종→탄창 1종). **무게 제한은 유지**. ItemData SO 구조, 가격 체계, 내구도/스택 규칙 | 기획 확정, 코드 구현 |
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
| [`medical.md`](medical.md) | **단일 HP + 회복 아이템(RPG식)** — **2026-09-09 부위별 의료 전면 폐기**(5부위 × 출혈/골절/통증). 의료 아이템 13종은 HP 회복 15~50으로 전환. 부위 피격 배율·적 부위 부상은 전투 쪽에 존치 | 기획 확정, 코드 구현 |
| [`survival.md`](survival.md) | 생존 스탯(수분/포만감) — 레이드 중 시간 기반 차감, 0 시 HP DoT, 수면·음식 회복. 차감속도/수면/음식 effectValue 수치(2026-06-18 튜닝) | 코드 구현 |

핵심 코드: `Health.cs` (단일 HP), `SurvivalStats.cs`, `SleepUI.cs` — (`PlayerMedicalSystem`·`MedicalHUD`·`MedicalItemData`는 2026-09-09 삭제)

### 🏠 안전가옥

| 문서 | 내용 | 상태 |
|------|------|------|
| [`safehouse.md`](safehouse.md) | 덕코프식 물리 공간 안전가옥 — 뒷골목 맵 레이아웃, 시설 목록, 확장 기획, NPC 배치, 가구 시스템 | 방향 확정 · 전당포 앞 마감/Play 검수 완료 (2026-09-11) |
| [`safehouse-intel.md`](safehouse-intel.md) | **탐사 정보 루프** — 자원 생산 금지 원칙, 라디오(루디 가동), 랜드마크 재방문 확장 + Phase A~E 작업 계획. ⚠️ **NPC 파견은 2026-09-09 폐기**(인텔 발생원 3→2) | 기획 확정, 구현 전 |
| [`safehouse-asset-list.md`](safehouse-asset-list.md) | 컨셉아트 기반 에셋 목록 — 바닥/펜스/프랍 분류 + 구현 우선순위 | 정리 완료 |
| [`safehouse-tile-prompt.md`](safehouse-tile-prompt.md) | 바닥/펜스/프랍 에셋 생성 AI 프롬프트 — 레퍼 첨부용 | 작성 완료 |

핵심 코드: `SafehouseStorage.cs`, `FurnitureData.cs`, Scene: `Safehouse.unity`

### 🗺️ 월드 · 레이드

| 문서 | 내용 | 상태 |
|------|------|------|
| [`world-map.md`](world-map.md) | 중앙 영야 코어 + 6개 외곽 지구 구조, 컨셉 아트 기반 거시 월드맵 | 컨셉 확정 |
| [`level-scrapmarket.md`](level-scrapmarket.md) | 폐상가 **레벨 디자인** — 상가골목(낮) 튜토 v6(건물기반 그레이박스) + 약국 조각 + 전체 지역1(1000×900) 통합 관계 | 진행 |
| [`level-apartment.md`](level-apartment.md) | **폐아파트** 레벨 디자인 — ⚠️구버전 보존(지역1 랜드마크 제외, 2026-07-07). 지역2 「잠든 주거 단지」 참고용 — 수직 side-by-side + 열쇠 체인 기법 | 구버전 보존 |
| [`level-tower.md`](level-tower.md) | **유리 R&D 타워** 레벨 디자인 — ⚠️구버전 보존(지역1 랜드마크 제외, 2026-07-07). 지역3+ 참고용 — 카드키 게이트·저층→상층 기법 | 구버전 보존 |
| [`raid.md`](raid.md) | 20분 타이머(`GameTuning.raidDuration` 1200초), 탈출 시스템, 루팅 흐름, 귀환 정산(RaidResultUI), 시간초과 페널티 | 기획 확정, 코드 구현 |
| [`post-raid-event.md`](post-raid-event.md) | 레이드 후 랜덤 이벤트 — 40% 확률, 텍스트 선택지, 보상/페널티 | 기획 확정, 코드 구현 |
| [`anomaly.md`](anomaly.md) | **짙은현상 구간 메커닉** — 위험/보상 타임어택(루트·몬스터 스폰→붕괴 증발). 세계관=gdd-core §5.1 위임, 시각=rendering.md `DenseAnomalyController` | 기획(수치 TBD) |
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

엔딩 2종: ① 진엔딩(진짜 모습 — 알파의 정체를 앎) · ② 굿엔딩(동생과의 일상 — 거짓 기억 유지) — 알파 재회에서 **진실 수집률**로 분기 (story.md §5, 둘 다 배드엔딩 아님)

### 🎨 렌더링 · 뷰

| 문서 | 내용 | 상태 |
|------|------|------|
| [`building-interior.md`](building-interior.md) | ⭐ **걸어 들어가는 실내 (3D)** — 같은 맵에서 진입·지붕/벽 가림·층 전환. **2026-09-11 마을 상점 5채 실내 아트**: GPT 원화 기반 가구/내장·62° 검토, 현재 Town02 면적에 배치. 과거 스케일 미결 사항은 본문 참조. | **실내 모델 준비 / Unity 연결 검증 대기** |
| [`hideout-3d.md`](hideout-3d.md) | **은신처 3D — 원화 기반 Hideout02 실내 시안**. 6×6m, 목재 가구·마루/철판 벽, PBR 표면 맵, 시설 9종 배선, 제작·검증 절차 | **1차 아트 패스 (2026-09-11)** |
| [`3d-migration.md`](3d-migration.md) | ⭐ **3D 쿼터뷰 전환 계획 SSOT** — 결정 4건(완전 3D·오소 고정 쿼터뷰·단차/엄폐·아트 전부 3D) + 2D 결합 실측(66,933 LOC 중 32%, Rigidbody2D 9파일·Physics2D 10곳) + 유지/교체/신규 표 + Stage 0~5 + 리스크·미결 | **Stage 0~4 완료 (2026-09-09)**, Stage 5(2D 잔재 삭제) 남음 |
| [`topdown-migration.md`](topdown-migration.md) | **아이소 → 탑다운 2D 전환** 배경·유지/제거/신규 목록·단계 계획 | 기록 (2D 시절 — 이후 3D로 재전환) |
| [`rendering.md`](rendering.md) | **현 구현 = 3D** — URP 3D(Forward)·오소 카메라 62°·게임 전용 셰이더 `BRB/GameLit`·후처리·착용등. 2026-09-11 마을 분위기 보정: 낮 색감/Volume·실용등 낮밤 분리, 밋밋함 피드백 후 밤 중간 명암·전당포 입구 초점/그림자 보완. 저장·재실행 자체 검수. 그 아래 URP 2D·Light2D·Tilemap 본문은 2D 시절 기록 | 구현 (3D) · 마을 분위기 자체 검수 완료 |
| [`destructible.md`](destructible.md) | 파괴 가능 오브젝트 — `Breakable` + `BRB/DamageOverlay`(오버레이라 전 셰이더 호환), 단계별 부서짐→파괴, Health 자동 연동 | 구현 |
| [`topdown-art-spec.md`](topdown-art-spec.md) | AI 이미지 생성 스펙 — near-overhead 시점, 마젠타 배경, 플랫 라이팅, 엔진 조명값 | 작성 완료 |

### 🔧 맵 도구

| 문서 | 내용 | 상태 |
|------|------|------|
| [`map-tool.md`](map-tool.md) | 탑다운 2D 맵 도구 **사양·결정 로그** — Prop2D 카탈로그 + 씬 빌더. ⚠️ 2D 시절 도구 — 3D 맵은 `Zone1GreyboxLayout`·`Map3DBuild`·`Greybox3D` 등이 만든다(문서 갱신은 정리 5단계) | 기록 (2D) |
| [`map-tool-guide.md`](map-tool-guide.md) | 맵툴 **사용 설명서**(신규 사용자용) — 셋업→등록→배치→기능→저장→테스트 단계별 | 작성 완료 |

---

## 씬 구조

```
Systems (영속 부트 씬 — 매니저·UI·PlayerRig. 항상 여기서 Play)
  → Safehouse (= 마을, 걸어다니는 안전구역) ↔ Hideout(은신처 방) · Pawnshop(전당포 실내)
    → MapSelectUI (지역 선택)
      → Zone1 (지역1 레이드, 20분 제한) · Int_* 실내 15씬(건물 문으로 전환)
        → 탈출 성공 → PostRaidEvent (40% 확률)
          → RaidResultUI (정산) → 마을 복귀
```

---

## 싱글톤 (DontDestroyOnLoad)

| 싱글톤 | 역할 |
|--------|------|
| TopDownPlayer | 이동(Rigidbody 3D, 화면 기준 WASD)·바라보는 방향(평소 이동 방향, 조준 중 커서)·총격. `Resources/PlayerRig.prefab`(플레이어+카메라+착용등+후처리)으로 Systems 씬에 배치 |
| SceneTransitionManager | 씬 전환, 페이드, 탈출 카운트다운 |
| UIManager | UI 상태 관리, 플레이어 입력 차단 |
| NPCRelationshipManager | NPC 3축 호감도 추적 |
| QuestManager | 퀘스트 진행/완료 추적 |
| PostRaidEventManager | 레이드 후 랜덤 이벤트 |

---

## 핵심 규칙 (새 세션 필독)

> ⭐ **기능·콘텐츠 추가는 [`dev-protocol.md`](dev-protocol.md)(기능 추가 규칙) 게이트를 따른다** — "기능 하나 = 열고 같은 세션에 코드·값·기획·색인을 닫는다." 아래 규칙들은 그 게이트의 구성요소.

1. **항상 `Systems` 씬에서 Play** — 게임플레이 씬(Safehouse·Zone1 등) 단독 실행 금지(사용자 규칙). 에디터를 여러 세션이 같이 쓰므로 커밋은 내 파일만
2. **Editor State Preservation** — 런타임 스크립트는 `Start()`에서 값을 적용하지 않음. 이벤트(`OnPhaseChanged` 등)로만 변경
3. **UI는 프리팹 베이크 + Instantiate** (2026-06~ 전환) — 각 패널 `EditorBake()`로 `Resources/UI/*.prefab` 굽고 부트스트랩이 Instantiate(없으면 코드 생성 폴백). 뷰=`[SerializeField]`, onClick=`WireEvents`, 동적 폰트=`ApplyFonts`. 상세 [`ui-prefab-plan.md`](ui-prefab-plan.md). 해상도 기준: 1920x1080
4. **기획 결정 즉시 기록** — `docs/` 내 시스템별 md에 날짜 + 질문 + 결정 기록. 세션 끝까지 미루지 않음
5. **StatDB 중앙 집중** — 모든 유닛/플레이어 스탯은 `StatDB.asset` SO에서 관리. 코드에 하드코딩 금지

---

## 변경 로그

| 날짜 | 내용 |
|------|------|
| **2026-09-11** | **입구 문서 현행화 (시스템 정리 1단계).** 사용자 "시스템 하나씩 정리 좀 해보자, 지금 전혀 정리가 안 돼서" → 전수 조사 후 6단계 정리 순서 합의(`dev-roadmap.md` §시스템 정리). 이 파일: 게임 정보(3D 루팅 RPG 슈터·URP 3D·카메라 62°·20분 — 코드 `raidDuration` 1200초 기준, 옛 15분 표기 교정) · 현재 개발 상태(옛 2D 로드맵 표 → 3D 전환·볼륨 축소·방향·PR·정리 단계 표) · 전투/의료 핵심 코드(삭제된 의료 3파일 제거) · 렌더링 행(3d-migration 「미착수」→Stage 0~4 완료, rendering 「현 구현 2D」→3D) · 씬 구조(Systems/마을/은신처/Zone1/Int_*) · 싱글톤 · 핵심 규칙 #1(의미 없던 Phaser Container 금지 → Systems에서 Play) · `enemy-ai.md` 색인 등재. 함께 고친 입구 문서: 두 `CLAUDE.md`·`dev-handoff.md`·`rendering.md`·`architecture.md`. |
| **2026-09-09** | **볼륨 축소 — 타르코프식 하드코어 → RPG식 단순화 (6건 완료, `scope-cut.md` 신설).** 사용자 판단: "코드·문서 다이어트 + 기획적 기능 축소. 부위별 치료 같은 타르코프 시스템 삭제하고 장비창도 간단하게 RPG처럼." ①**부위별 의료 폐기** → 단일 HP + 회복 아이템 ②**소음 시스템 폐기** → 적 발견은 시야 단일 축(투척물 유인만 `Distraction`으로 존치) ③**격자 인벤 폐기** → 슬롯 1칸·회전/수색 제거, **무게는 유지**, 무기 파츠 4→1(탄창) ④**파견 + 아르바이트 보드 삭제** ⑤**특성 41종 → 11종**(코드가 읽는 effectKey만) ⑥**QA 자동화 분리**(`QA_ENABLED` 어셈블리 게이트, 삭제 아님). 관련 SSOT 전부 개정: medical/combat/inventory/traits/economy/safehouse/safehouse-intel/balance/qa. |
| 2026-07-10 | **기능 추가 규칙 신설(`dev-protocol.md`).** 지속 개발 시 방향이 어긋나지 않도록 기존 규율(SSOT·즉시기록·GameTuning·5대 런타임 규약 R1~R5·브랜치PR)을 "기능 하나 = 열고 같은 세션에 닫는다" 단일 게이트로 통합. BEFORE/DURING/DONE 체크리스트 + 정리 주기. 핵심 규칙 섹션 상단·문서 맵(기획총괄)·CLAUDE.md에 등재. 실제 겪은 드리프트(색인 누락 8종·로어 중복 재서술·stale 서술 = 06-30/06-16 교정) 재발 방지. |
| 2026-07-09 | **게임패드 지원 T0/T1 + `controls.md` 신설.** 입력 기반 = GameInput 셰임 확장(신규 액션 에셋 대신 `Gamepad.current` 병합), 범위 = 레이드 조작(T1). 왼쪽 스틱 이동·오른쪽 스틱 조준·버튼 맵(E→A/Space→B/Shift→L3/C→Y/Esc→Start/Tab→Select/1~4→D패드/좌우클릭→RT·LT)·디바이스 전환(`PadActive`). UI 기본 내비는 기존 `InputSystemUIInputModule`. T2(메뉴 포커스·스크롤·글리프)/T3(격자 인벤 스틱 커서) 후속. 매핑 상세 = [`controls.md`](controls.md). |
| 2026-06-30 | **문서 정합성 교정(인덱스·stale 서술).** ① 색인 누락 8종 등재: 새 「🧱 기술·아키텍처」 섹션(`architecture.md`/`ui-prefab-plan.md`/`save.md`) + `anomaly.md`(월드·레이드) + `dev-handoff.md`(기획총괄) + `item-icon-list/additions.md`·`prop-production.md`(아트). ② **죽은 링크 제거**: `safehouse-map-prompt.md`(2026-06-02 삭제분). ③ stale 상태/서술 교정: `traits.md` 상태(기획→SO·매니저·UI·배선 구현), 핵심규칙 #3 「UI 코드 생성」→「프리팹 베이크+Instantiate」. ④ `demo13-flashlight/CLAUDE.md` 동기화: UI Construction 섹션을 프리팹 베이크/Instantiate·`[SerializeField]`/`WireEvents`/`ApplyFonts` 패턴으로 재작성, PlayerInventory 「5x8 30kg」→ 다중 컨테이너(가방+주머니4×1+보안3×3, 무게 trait 보정). 코드 대조로 검증(수치 무변경). |
| 2026-06-16 | **빛의 역설/루디 활성 자원 캐논 정합(gdd-core §5.2/§5.3).** SSOT 색인 행의 「불의역설」→「빛의역설」 교정 + 「루디=충전·방전·마모 활성 자원」 명기. 대상 문서(economy/items/items-crafting-farming은 미해당·safehouse/safehouse-intel/raid/crafting/navigation/anomaly)에 루디 충전·방전·마모·순도·죽은 루디·루디 충전/가공대·비루디 광원 침식 반영 + SSOT 링크 추가. 새 수치 미생성(GameTuning TBD). | gdd-core §5 본문은 이미 캐논(진실). |
| 2026-06-16 | **세계관 통합 정리 — gdd-core §5를 단일 출처(SSOT)로 확정.** §5 상단에 SSOT 배너+하위 색인(5.0~5.4) 추가. 흩어진 로어 재서술 제거: story.md §2 "봉쇄 이후"의 자원작전·불의역설·회수꾼·루디 정의 → 스토리 사건만 남기고 §5로 위임 / world-map.md 상단에 로어 SSOT 위임 노트 + §1 "영구적 밤·붕괴한 날·밤 개장" 옛 표현을 캐논(봉쇄+짙은현상=시간 무관) 용어로 교정. anomaly.md는 이미 §5.1/§5.4 위임(유지). |
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
