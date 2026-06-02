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

## 변경 로그
| 날짜 | 내용 |
|---|---|
| 2026-06-02 | 흩어진 메뉴 4루트(`BRB`/`TopDown 2D`/`TopDown Combat`/`Dev Tools`) → **`Tools/TopDown/` 단일 루트 + 카테고리**(Build/Combat/Map/Data/Utility)로 통합. 코드 11개 `MenuItem` + 인라인 주석 갱신. `ExecuteMenuItem` 의존 0(안전). 에셋 생성 메뉴(`Create ▸ …`)·데이터 생성기는 범위 밖. |
