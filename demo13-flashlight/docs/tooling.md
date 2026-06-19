# 에디터 도구 (Editor Tools)

> 모든 커스텀 에디터 메뉴는 **`Tools/TopDown/`** 한 루트 아래 카테고리로 묶음 (2026-06-02 통합).
> 이전엔 `BRB` / `TopDown 2D` / `TopDown Combat` / `Dev Tools` 4개 루트로 흩어져 있었음.
> (Spine 등 서드파티 메뉴, `Create ▸ …` 에셋 생성 메뉴는 범위 밖.)

## 카테고리별 메뉴 (`Tools ▸ TopDown ▸ …`)

### 🏗 Build — 프리팹·씬 생성
| 메뉴 | 클래스 | 역할 |
|---|---|---|
| Build ▸ Player Rig | `TopDownPlayerBuilder` | `Resources/PlayerRig.prefab` — **카메라+플레이어+라이트+후처리 한 세트**(DontDestroyOnLoad, 모든 씬 공유) |
| Build ▸ Enemy Prefab | `CombatSandboxBuilder` | `Resources/Enemy.prefab` — 바디 스프라이트+Hurtbox+Health+CombatFeedback+EnemyController(+NavAgent 자동) |
| Build ▸ InGame Scene | `GameSceneBuilder` | 인게임 레이드 씬 골격(라이트·Tilemap·SpawnPoint) |
| Build ▸ Safehouse Scene | `GameSceneBuilder` | 안전가옥 씬 골격 |
| Build ▸ Combat Sandbox Scene | `CombatSandboxBuilder` | 타격감/히트박스 테스트 아레나(적 3기 + NavGrid + 우회 벽 2개) |
| Build ▸ Map Tool Scene | `MapTool2DSceneBuilder` | 맵 편집용 씬(Grid + Floor/Walls Tilemap) |

### ⚔ Combat — 전투
| 메뉴 | 클래스 | 역할 |
|---|---|---|
| Combat ▸ Attack Editor | `AttackDataEditorWindow` | **히트박스 타임라인 에디터** — AttackData / AttackComboData 편집 (상세 아래) |

### 🗺 Map — 맵·프롭·조명
| 메뉴 | 클래스 | 역할 |
|---|---|---|
| Map ▸ Prop Catalog | `Prop2DCatalogEditor` | Prop2D 정의 등록/편집 + 콜라이더 미리보기 + 씬 클릭 배치 |
| Map ▸ Setup Scene Lighting | `SceneLightingBuilder` | 현재 씬에 어두운 글로벌 Light2D 셋업(Darkwood 룩) |

### 📊 Data
| 메뉴 | 클래스 | 역할 |
|---|---|---|
| Data ▸ Stat DB Editor | `StatDBEditor` | `StatDB`(플레이어/유닛 스탯) 키 기반 편집 |

### 🔧 Utility
| 메뉴 | 클래스 | 역할 |
|---|---|---|
| Utility ▸ Color to Alpha | `ColorToAlpha` | 지정색(흰색)→투명 텍스처 전처리. Project 우클릭 `Assets ▸ Color → Alpha`도 가능 |

---

## 전투 에디터 (Attack Editor) 상세
`Tools ▸ TopDown ▸ Combat ▸ Attack Editor` (클래스 `AttackDataEditorWindow`)
편집 대상: **`AttackData`**(단일 공격) / **`AttackComboData`**(연속 콤보 체인). 기능이 늘어 카테고리로 정리:

| 카테고리 | 기능 |
|---|---|
| **모드** | 단일(Single) / 콤보(Combo) 토글. 콤보는 [1타][2타]… 단계 탭으로 전환 |
| **타이밍** | 프레임 그리드 타임라인(칸=1프레임) · 프레임 스크러버 · `cancelFromFrame` 캔슬 마커 |
| **히트 윈도우** | `startFrame~endFrame` 막대 · Box/Circle · facing 기준 offset·크기·회전 · `damageMult`/`groggyMult` · 추가/삭제 |
| **프리뷰** | 2D 탑다운(facing=오른쪽) · 현재 프레임 활성 윈도우 진하게 · 중심 핸들 드래그로 offset 편집 |
| **타격감** | `hitstop` / `hitstopDuration`(강공 적중 시 짧은 정지) — 2026-06-02 추가 |

> 만든 `AttackData`를 무기(`WeaponData`)나 `TopDownPlayer` 인스펙터의 heavyAttack 등에 연결하면 전투에 반영. 상세 전투 흐름은 [`combat.md`](combat.md).

---

## CSV 헤더 한글 라벨 (가독성)
`밸런스·컨트롤 ▸ CSV 밸런스` 탭의 컬럼 헤더는 **파일의 영문 헤더(변수명)는 그대로 두고 툴 표시만 한글**로 보여준다.
- 매핑 사전: **`Assets/Editor/KoLabels.cs`** (`KoLabels.Get(fileName, header)` → 한글 또는 `null`).
  - 우선순위: 파일별(PerFile) → 전역(Global). 미등록 헤더는 `null` → **영문 원문 그대로 fallback**(누락돼도 안 깨짐).
  - 같은 헤더라도 파일마다 의미가 다른 것(`id`/`cat`/`type`/`tier`/`min`/`max`)은 파일별 오버라이드로 분기.
- 표시 형태: **2행 헤더** — 1행 한글(굵게) + 2행 영문 원문(회색 작게), 우상단 타입 뱃지(`#`/`✓`/`▼`/`T`). 헤더 클릭=정렬은 동일.
- **좌측 파일 목록**도 동일하게 한글 제목(굵게) + 파일명(회색 작게) 2행 표시. 매핑은 `KoLabels.FileTitle(파일명)`(예: `quests.csv`→"퀘스트", `reputation.csv`→"평판 증감"). 상단 바도 "한글 (파일명)".
- 스크롤: **좌측 파일 목록 = 세로** 독립 스크롤, **우측 그리드 = 가로+세로**. 가로 스크롤은 콘텐츠 폭 확정용으로 그리드를 `Width(totalW)` 세로 그룹으로 감싸 처리(열 너비 합 = `34 + 열수×colW + 94`).
- 신규 CSV 컬럼/파일 추가 시 `KoLabels.cs`에 항목만 추가(헤더=`PerFile`/`Global`, 파일제목=`Files`).

## NPC·상점 탭 한글 라벨
`밸런스·컨트롤 ▸ NPC·상점` 탭(`NPCData`/`ShopData` SO 직접 편집)도 **필드명(C# 변수)은 그대로, 표시만 한글**.
- 매핑: **`KoLabels.Field(필드명)`**(예: `buyRate`→"구매가 배율", `affinityChange`→"호감도 변화", `initialAffinity`→"초기 호감도"). 미등록은 Unity 기본 라벨(영문 nicify) fallback.
- 렌더러: `GameControlPanel`의 `DrawKoRoot`/`DrawKoClass`/`DrawKoProperty` — SO 필드를 재귀로 그리며 라벨만 한글 교체. 영문 변수명은 라벨 툴팁.
  - **레이아웃: `[Header]` 섹션별 색 바 + 스칼라 필드 2열 가로 배치**(세로로만 길던 것 → 가로 활용). `[Header]`는 리플렉션(`GroupsOf`, MetadataToken 정렬)으로 읽어 색 섹션으로 복원. 창이 좁거나 2단 이상 중첩이면 자동 1열.
  - 리프·오브젝트 배열(`stock`/`availableQuests` 등)·문자열 배열(`lines` 등) → **Unity 기본 위젯 유지**(리오더·추가/삭제·TextArea 그대로, 라벨만 한글, 전체폭).
  - 직렬화 클래스 배열(`defaultDialogues`/`eventDialogues`/`choices`/`wanted`) → **수동 재귀로 요소 내부 필드까지 한글**(요소는 `[Header]` 섹션 2열), 값 편집 전용(구조 추가/삭제는 인스펙터·NPC 메이커에서).
  - `GroupsOf` 리플렉션은 **단일 클래스 계층 전용**(base에 직렬화 필드 있는 타입엔 헤더 매핑 어긋날 수 있음).
- 에셋 폴드아웃 제목 = 한글 표시이름(`displayName`/`shopName`) + 파일명(회색).

## 탭 구성 (2026-06-19 기준)
상단 탭 5개: **컨트롤 / NPC·상점 / 몬스터 / 지역 루트 / CSV 밸런스**.
- 플레이어 스탯은 `컨트롤 ▸ 🎮 플레이어 스탯`(StatDB.playerStat)에 둔다.
- **적/몬스터(StatDB.units)는 가짓수가 많아 NPC처럼 별도 `몬스터` 탭으로 분리**(2026-06-19).

## 플레이어 스탯 (컨트롤 탭) 한글 라벨
`밸런스·컨트롤 ▸ 컨트롤 ▸ 🎮 플레이어 스탯` 섹션(`StatDB.playerStat`)은 NPC와 **같은 `KoLabels.Field` 사전 + 2열 섹션 렌더러**(`DrawKoClass`) 공유.
- 펼치면 체력/이동/약공격/강공격/구르기/스태미너 전 필드 한글(`lightDamage`→"약공격 데미지", `dodgeCooldown`→"구르기 쿨다운" 등) + `[Header]` 섹션 2열.

## 몬스터 탭 (마스터-디테일)
`밸런스·컨트롤 ▸ 몬스터` — `StatDB.units`(UnitStatData=적/몬스터)를 **좌측 목록 + 우측 상세** 마스터-디테일로 편집(가짓수 많아 NPC처럼 분리).
- 좌측: 몬스터 목록(이름 굵게 + `id` 회색, 세로 스크롤). 검색 필터(id/이름), 행마다 `✕` 삭제, 행 클릭=선택.
- 우측: 선택 몬스터의 전 필드를 `DrawKoClass`로 `[Header]` 섹션 2열 한글 표시(비주얼/체력/공격/그로기/이동/감지/AI/보상 등).
- 상단 `＋ 새 몬스터`로 추가, `저장`/`에셋 선택`. **구조 변경(추가/삭제)은 레이아웃 스코프 종료 후 적용**(스크롤뷰 불일치 방지 — `monsterAddReq`/`monsterDelReq` 지연 처리, `ExitGUI` 미사용).
- 같은 StatDB.asset을 편집하므로 컨트롤 탭 플레이어 스탯과 동일 에셋.
- 색상(`tintColor` 등)·프리팹 참조 필드는 Unity 기본 위젯 유지(라벨만 한글).

---

## 변경 로그
| 날짜 | 내용 |
|---|---|
| 2026-06-02 | 흩어진 메뉴 4루트(`BRB`/`TopDown 2D`/`TopDown Combat`/`Dev Tools`) → **`Tools/TopDown/` 단일 루트 + 카테고리**(Build/Combat/Map/Data/Utility)로 통합. 코드 11개 `MenuItem` + 인라인 주석 갱신. `ExecuteMenuItem` 의존 0(안전). 에셋 생성 메뉴(`Create ▸ …`)·데이터 생성기는 범위 밖. |
| 2026-06-19 | **몬스터 탭 분리.** 적/몬스터(StatDB.units)를 NPC처럼 상단 별도 `몬스터` 탭으로 분리(가짓수 많음). 좌 목록+우 상세(2열 섹션) 마스터-디테일, 검색·추가·삭제. 플레이어 스탯은 컨트롤 탭에 잔류(StatDB 섹션명 "플레이어 스탯"으로 변경). 탭 5개: 컨트롤/NPC·상점/몬스터/지역 루트/CSV. 구조변경은 지연 적용(ExitGUI 미사용). |
| 2026-06-19 | **NPC·StatDB 탭 2열 가로 레이아웃.** 필드를 한 줄씩 세로로만 쌓던 걸 `[Header]` 섹션 색 바 + 스칼라 2열 가로 배치로 변경(지역루트/GameTuning 느낌). `[Header]`는 리플렉션(`GroupsOf`)으로 복원, 배열·중첩클래스는 전체폭. 좁은 창/깊은 중첩은 자동 1열. 요청: "세로만 말고 데이터 가로로 가독성↑". |
| 2026-06-19 | **컨트롤 탭 StatDB(Player/몬스터) 한글 라벨.** `StatDB.playerStat`(PlayerStatData)·`units`(UnitStatData=적/몬스터)를 NPC와 같은 `KoLabels.Field`+`DrawKoProperty`로 한글 표시. PlayerStatData·UnitStatData 전 필드 추가(약/강공격·구르기·스태미너·그로기·감지·AI·보상 등). 유닛 목록은 `id · displayName` 폴드아웃. 이로써 CSV·NPC·Player·몬스터 전 탭이 동일 사전 공유. |
| 2026-06-19 | **NPC·상점 탭 한글 라벨.** `NPCData`/`ShopData` SO 필드를 `KoLabels.Field` + 재귀 렌더러(`DrawKoProperty`)로 한글 표시(변수명 불변, 영문은 툴팁). 오브젝트/문자열 배열은 Unity 기본 위젯 유지, 직렬화 클래스 배열(대화·선택지·수배)은 값 편집 전용 재귀. CSV 탭과 같은 `KoLabels` 사전 공유 — Player/몬스터는 다음 단계. |
| 2026-06-19 | **CSV 밸런스 탭 헤더 한글화.** `action`/`rep`/`gw`/`wt`/`fx` 등 불특정 영문 헤더 → 툴에서 한글 우선 + 영문 작게(2행 헤더)로 표시. 매핑 사전 `Assets/Editor/KoLabels.cs` 신설(파일별 오버라이드 + 전역, 미등록은 영문 fallback). 변수명·CSV 파일은 불변. **질문**: "CSV로 컨트롤하는 데이터 헤더(rep/action 등)를 지역루트 탭처럼 가독성 좋게, 변수는 영어여도 툴 표시는 한글로?" / **결정**: 한글 우선 + 영문 작게 / 적용 범위는 CSV 탭 먼저(NPC·Player·몬스터는 후속). |
