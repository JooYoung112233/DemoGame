# UI 프리팹화 + 시안 스타일 적용 계획

> 작성 2026-06-26. 목적: 현재 **코드 절차 생성** UI를 **프리팹 기반**(씬/프리팹에서 직접 편집)으로 전환하고,
> `Assets/Resources/UI/Image/`의 낡은 종이(Tarkov 톤) 스프라이트로 시안 룩을 입힌다.
> ⚠️ 아직 **구현 전 — 계획만**. (집에서 이어서 진행)

## 0. 사용자 요구 (원문 요지)
- `Assets/Resources/UI/Image/`에 UI 이미지(스프라이트) + `시안.png`(목업) 넣어둠.
- 시안 보면서 **현재 UI에 적용**, `Assets/Resources/UI/`에 **전체 프리팹으로 생성**해 프리팹의 UI를 쓰도록 구조 변경.
- 이미지로 **모자랄 수 있으니 억지로 적용 X** — 시안 보고 적당히. 없는 곳은 기존 테마색 유지.
- **프리팹이어야 씬에서 수정 가능** → 지금처럼 *게임 시작 시 코드로 제작하는 방식 폐기*.

## 1. 현재 구조 / 문제
- 모든 UI(`CharacterPanelUI`, `ShopUI`, `ItemDetailUI`, `GameHUD`, …)가 `Awake/BuildUI`에서 **uGUI를 100% 코드 생성** + 필드에 ref 연결 후 로직 사용.
- Systems 씬(`Assets/Scenes/Systems.unity`)이 UIManager+패널 보유(자기부트스트랩은 폴백).
- 문제: 레이아웃/스프라이트를 **에디터에서 못 만짐**. 색·간격 하나 바꾸려면 코드 수정·재컴파일.

## 2. 목표 구조
- `Assets/Resources/UI/` 아래 **패널별 프리팹**: `CharacterPanel.prefab`, `ShopPanel.prefab`, `ItemDetail.prefab`, (+공용 셀/버튼 프리팹).
- 프리팹 = 전체 비주얼 계층(루트 + 자식 + 스프라이트 9-slice + 레이아웃)을 **에디터에서 배치**.
- **바인딩 = 직렬화 참조**(find-by-name X). 패널 스크립트를 **프리팹 루트에 부착**하고, 지금 코드가 `new GameObject`로 만들던 ref를 전부 `[SerializeField]` 필드로 교체.
  - 로직 클래스 = 뷰 컴포넌트(합침). 부트스트랩은 `AddComponent + BuildUI` 대신 **프리팹 Instantiate**.
- **동적 콘텐츠(격자 셀/리스트 항목)는 절차 유지** — 단, 직렬화된 컨테이너 transform(슬롯 루트)에 Instantiate. 셀은 소형 프리팹(`Cell.prefab`) 또는 기존 `GridPanel` 렌더 유지.
- 배치 위치: 패널 프리팹 **인스턴스를 Systems 씬 UIManager 하위에** 둠 → 사용자가 씬/프리팹 모드 양쪽에서 편집.

## 3. 이미지 자산 → 용도 매핑 (`Assets/Resources/UI/Image/`)
| 파일 | 성격 | 적용처 | 비고 |
|------|------|--------|------|
| `box.png` | 어두운 패널 프레임 | 패널/팝업 배경 | 9-slice(테두리 보존) |
| `storage.png` | 창고 패널 배경 | STORAGE/창고 큰 패널 | 9-slice |
| `itembox.png` | 어두운 정사각 프레임 | 아이템 상세 **아이콘 박스** | 9-slice |
| `item.png` / `storage_box.png` | 어두운 격자 셀 | 빈 슬롯 셀 | 9-slice or 단순 |
| `btn.png` | 종이(밝은) 버튼 | **PRIMARY** 버튼(EQUIP/구매 등) | 9-slice 가로 |
| `btnb.png` | 어두운 버튼 | **SECONDARY** 버튼(DROP/취소 등) | 9-slice 가로 |
| `name.png` / `name_icon.png` | 찢긴 종이 태그 | 패널 **제목 태그**(ITEM NAME 등) | 비대칭 — 9-slice 주의 |
| `title.png` / `title2.png` | 제목 배너 | 상단 타이틀/헤더 | 변형 2종 |
| `storage_btn_on.png` / `_off.png` | 탭 on/off | 창고/상점 **탭**(ALL·WEAPONS…) | 토글 스프라이트 |
| `시안.png` | 목업(참고용) | — | 빌드 제외해도 됨 |

> 슬롯 점유색/희귀도/내구도바 등 **시맨틱 색**은 스프라이트로 대체하지 말고 기존 `UITheme` 유지(시안에 없음).

## 4. 단계별 작업 — ⚠️ 우선순위 변경 (2026-06-26)
> **사용자 지시: 스킨(시안 이미지) 적용은 나중. 먼저 "게임 시작 시 코드 생성" → "프리팹 베이크 후 Instantiate" 구조 전환을 *지금 있는 모든 UI*에 대해.**
> 베이크 방식(룩 1:1 보존): ① 뷰 참조 `[SerializeField]` → ② 에디터 `EditorBake()`로 GenerateUI 1회 실행 → `SaveAsPrefabAsset`(`Resources/UI/*.prefab`) → ③ 부트스트랩 `Instantiate`(프리팹 없으면 코드 생성 폴백). 동적 OS 폰트는 직렬화 불가 → Instantiate 후 `ApplyFonts()` 재바인딩. 동적 격자/리스트는 절차 유지.

### 4-A. 프리팹 우선 전환 (현재 작업 — 모든 패널)
> **부트스트랩 2부류**(중요): **A형 lazy 자가부트**(`Ensure()`만, Systems 씬에 없음 — 예: ItemDetailUI) → `Ensure()`를 Instantiate로. **B형 Systems 씬 배치**(대부분 — NoteUI/Narration/Tutorial/Toast 매니저 + UIManager 하위 GameHUD/Shop/CharacterPanel 등) → `SystemsSceneBuilder`가 빈 GO+AddComponent로 박음. **B형은 빌더가 `Resources/UI/<Type>.prefab` 있으면 프리팹 인스턴스로 배치(없으면 폴백)** 하도록 중앙 수정 완료. B형도 패널 자체는 동일 레시피([SerializeField]/ApplyFonts/EditorBake) 필요 + **베이크 후 Systems 씬 재빌드**.

1. ✅ **PoC `ItemDetail`(A형)** (2026-06-26): 뷰 ref 8개 `[SerializeField]` + `ApplyFonts()` + `EditorBake()`, `Ensure()`→Instantiate 폴백. 베이크 툴 `UIPrefabBaker`. + 프리팹 Canvas 비활성 버그 수정. **검증 완료(사용자: 자세히 팝업 정상).**
2. ✅ **`SystemsSceneBuilder` 프리팹 인식화** (2026-06-26): `InstantiateUIOrComponent(type, parent)` — `Resources/UI/<Type>.prefab` 있으면 `PrefabUtility.InstantiatePrefab`, 없으면 `AddComponent` 폴백. ManagerTypes·UiPanels 두 루프에 적용.
3. ✅ **PoC `NoteUI`(B형)** (2026-06-26): 검증 완료(디버그 패널 쪽지 버튼 정상). + 디버그 테스트 버튼.
4. ✅ **팝업 배치 6종**(2026-06-27): NarrationUI·TutorialPrompt·RaidResultUI·PostRaidEventUI(B형, 빌트인 폰트) + PauseMenu·GroundPickupUI(자가부트, Pause는 EnsureEventSystem을 Show로 이동/Ground는 ApplyFonts). 각 `[SerializeField]`+`EditorBake`+Canvas 가드, 베이크 엔트리 추가(+`프리팹 베이크/── 전부 ──`). **사용자 검증 대기**(전부 베이크 → Systems 재빌드 → 각 팝업 확인). 남음: ToastManager(동적 토스트).
5. ✅ **HUD + 중형 패널 8종**(2026-06-27): ToastManager(BuildCanvas)·GameHUD·QuestHUD·QuickSlotBar(RuntimeInit 부트 프리팹화+ApplyFonts)·NavigationHUD(`Scripts/Navigation/`)·MapSelectUI(ApplyFonts)·CraftingUI(`Scripts/Crafting/`)·DialogueUI. 각 `[SerializeField]`(대부분 이미 됨)+`EditorBake`+필요시 Canvas 가드. 베이크 엔트리 추가. **사용자 검증 대기.** (대부분 빌트인 폰트, QuickSlot/MapSelect만 동적폰트→ApplyFonts)
6. ⏭ **대형 패널 `CharacterPanelUI`(3580줄)** — ref 수백 개. 뷰 ref 직렬화 + 격자 슬롯 루트만 직렬화(셀은 GridPanel 절차 유지). 위험 → 단독 진행·검증.
7. ⏭ **대형 패널 `ShopUI`(2441줄)** — 동일.
8. ⏭ (선택) 기타: DispatchUI/RadioUI/QuestLogUI/RaidMapUI/HideoutUI/SleepUI/TitleScreen.

### 4-B. 시안 스킨 적용 (전환 완료 후 — 보류)
- 9-slice 자산 셋업 + 시안 스프라이트 입히기 + TMP 전환. (이전에 만든 `UIAssetSetup`/`UIKitBuilder`는 이 단계용이었으나, 우선순위 변경으로 **삭제**했고 스킨 단계 진입 시 재도입.) §5 매핑·§6 결정 참조.

## 5. 패널별 시안 매핑
- **RESOURCES 바**(좌상단): 무게/부피/스크랩 — `box` 배경 + 아이콘. (현재 GameHUD 자원 표시와 연결)
- **BUTTONS**: PRIMARY=`btn`, SECONDARY=`btnb`, SMALL=같은 스프라이트 축소.
- **SLOTS**: 단일/가로/격자 = `storage_box`(or `item`) 셀.
- **ITEM NAME 패널**: `box` 배경 + `name` 제목태그 + `itembox` 아이콘 + TYPE/WEIGHT/STACK/설명/STAT/DURABILITY바/SELL VALUE + EQUIP/DROP/SCRAP(btn/btnb).
- **STORAGE 패널**: `storage` 배경 + `name` 제목태그("STORAGE") + 무게표시 + **탭**(ALL/WEAPONS/ARMOR/CONSUMABLES/MATERIALS/ETC = storage_btn) + 격자 + TAKE ALL/SORT(btn).

## 6. 결정됨 (2026-06-26)
- **TMP vs legacy Text → `TMP`로 전환.** 새 키트/프리팹은 `TextMeshProUGUI`, 기존 패널은 전환 시 TMP로 교체.
  - ⚠️ **전제조건**: `Assets/TextMesh Pro` 폴더가 없음 = TMP Essential Resources 미임포트. 사용자가 Unity에서 **Window ▸ TextMeshPro ▸ Import TMP Essential Resources** 1회 실행해야 TMP 텍스트가 렌더됨. (com.unity.ugui 2.0.0에 TMP 번들 → 패키지 추가는 불필요)
- **창고 카테고리 탭(ALL/WEAPONS/…) → 비주얼만 이번에, 필터 로직은 나중.** `Tab.prefab`(storage_btn on/off)으로 탭 UI는 배치하되 클릭 필터링 동작은 후속 단계.
- **격자 셀 → `GridPanel` 절차 렌더 유지.** `Cell.prefab` 안 만듦. 직렬화된 슬롯 루트 transform에만 기존 렌더 부착.
- **RESOURCES HUD → 이번 범위 포함.** GameHUD 자원바(무게/부피/스크랩)도 `box` 배경+아이콘으로 프리팹화 대상에 추가.
- 바인딩: 패널 스크립트 = 뷰 컴포넌트(합침) + `[SerializeField]` 직렬화 ref. (find-by-name 금지)

## 7. 리스크 / 주의
- `CharacterPanelUI`(~3300줄)·`ShopUI`(~2400줄)는 생성+로직 결합 → **한 번에 X, 패널 단위 점진 전환**.
- 전환 중 **이중 상태**(코드 생성 + 프리팹 공존) 피하려 패널별로 완결.
- 9-slice 보더 안 맞으면 종이 질감 늘어남 → 스프라이트별 border 값 튜닝.
- Systems 씬은 **사용자 에디터 상태** → 프리팹 인스턴스 배치는 사용자와 합의/사용자가 직접.

## 변경 로그
| 날짜 | 질문 | 결정 | 근거 |
|------|------|------|------|
| 2026-06-26 | 코드 절차 생성 UI를 프리팹 기반(씬 편집 가능)으로 + 시안 스프라이트 적용 | **프리팹화 방향 확정(계획).** Resources/UI에 패널 프리팹 + 직렬화 ref 바인딩, 동적 격자만 절차 유지, 패널 단위 점진 전환. 이미지는 매핑표대로 자연스러운 곳만, 시맨틱 색은 UITheme 유지. | 에디터 비편집 문제 해소. 큰 두 패널은 점진 전환으로 리스크 관리. 구현은 후속. |
| 2026-06-26 | §6 열린 질문 4건(TMP / 창고 탭 / 격자 셀 / RESOURCES HUD) | **TMP 전환**(전제: TMP Essential Resources 임포트 필요) · **창고 탭 비주얼만**(필터 후속) · **격자 GridPanel 절차 유지**(Cell.prefab 안 만듦) · **RESOURCES HUD 포함**. | 키트/PoC 진입 위해 설계 확정. 스코프는 비주얼 우선·로직 후속으로 관리. |
| 2026-06-26 | §4-1 자산 9-slice 셋업 / §4-2 공용 키트 | **에디터 빌더 2종 작성.** `UIAssetSetup`(Image/ 13종 스프라이트→Sprite+9-slice 보더+Clamp/Bilinear) · `UIKitBuilder`(Button_Primary/Secondary·Tab·TitleTag 프리팹을 Resources/UI/Kit에 생성, TMP 라벨). Cell.prefab은 §6 결정대로 생략. | 코드 절차→프리팹 전환의 재사용 원자 확보. 사용자가 Tools 메뉴로 실행. |
| 2026-06-26 | **우선순위 변경**: 시안 이미지를 "새 UI"로 만드는 게 아니라 *기존 UI에 입히는 것*. 그리고 그 전에 **모든 UI를 코드 생성→프리팹 베이크/Instantiate 구조로 먼저 전환**. | 스킨(§4-B) 보류, **프리팹 우선 전환(§4-A)을 전 패널에** 우선. 진행 방식 = **PoC 1개로 패턴 확정 후 전체**(Unity 검증 불가로 일괄 위험). 저장 = `Resources/UI/`. | 사용자가 직접 씬에서 UI 편집 가능하게 + 큰 패널 리스크 관리. 이전 `UIAssetSetup`/`UIKitBuilder`는 스킨 단계용이라 삭제(재도입 예정). |
| 2026-06-26 | PoC `ItemDetail` 프리팹화 | **완료·검증.** 뷰 ref 8개 `[SerializeField]`, `ApplyFonts()`·`EditorBake()`, `Ensure()`→Instantiate+폴백. 베이크 툴 `UIPrefabBaker`. + Canvas 비활성 버그 수정(팝업 안 보임). 사용자 "자세히 팝업 정상" 확인. | 베이크→Instantiate 패턴 검증 완료. |
| 2026-06-26 | B형(Systems 씬 배치) 전환 경로 | **부트스트랩 2부류 발견**: A형 lazy(Ensure)·B형 Systems 씬 배치. B형 대응으로 `SystemsSceneBuilder.InstantiateUIOrComponent` 추가(프리팹 있으면 인스턴스, 없으면 AddComponent 폴백). **PoC `NoteUI`** 동일 레시피 전환 + 베이크 엔트리. | 대부분 UI가 B형이라 빌더 1곳 수정으로 전 패널 커버. 폴백으로 미베이크分 안전. 사용자: NoteUI 베이크→Systems 재빌드→쪽지 검증. |
