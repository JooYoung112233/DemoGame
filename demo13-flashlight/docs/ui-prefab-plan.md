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

## 4. 단계별 작업 (집에서 이어서)
1. **자산 셋업**: 스프라이트 import 설정 — Sprite(2D), `name/title` 류는 Sprite Editor에서 **9-slice border** 지정, 필터/압축 확인. `btn/box/storage` 9-slice 보더 설정.
2. **공용 UI 키트**: 재사용 프리팹 — `UI/Button_Primary.prefab`(btn), `UI/Button_Secondary.prefab`(btnb), `UI/Cell.prefab`(storage_box), `UI/Tab.prefab`(storage_btn on/off), `UI/TitleTag.prefab`(name). 각자 Image(Sliced)+Text(또는 TMP) 자식.
3. **PoC 1개 전환** — 권장: **ItemDetail**(시안 ITEM NAME과 1:1, 작고 독립적). `ItemDetailUI`를 프리팹+직렬화 ref로 전환해 패턴 검증.
4. **Storage/인벤(`CharacterPanelUI`)** 전환 — 가장 큼. 프레임/탭/버튼/슬롯 루트만 프리팹화, 격자 셀은 `GridPanel` 렌더 유지(슬롯 루트는 직렬화).
5. **Shop(`ShopUI`)** 전환 — 3컬럼·구매/판매 박스·트레이. 인벤과 공용 키트 재사용.
6. (선택) `GameHUD`/자원바(RESOURCES) 등 작은 HUD.
7. **절차 생성 코드 제거** — 각 패널 전환 완료분의 `BuildUI` 삭제, 부트스트랩을 Instantiate로 교체.

## 5. 패널별 시안 매핑
- **RESOURCES 바**(좌상단): 무게/부피/스크랩 — `box` 배경 + 아이콘. (현재 GameHUD 자원 표시와 연결)
- **BUTTONS**: PRIMARY=`btn`, SECONDARY=`btnb`, SMALL=같은 스프라이트 축소.
- **SLOTS**: 단일/가로/격자 = `storage_box`(or `item`) 셀.
- **ITEM NAME 패널**: `box` 배경 + `name` 제목태그 + `itembox` 아이콘 + TYPE/WEIGHT/STACK/설명/STAT/DURABILITY바/SELL VALUE + EQUIP/DROP/SCRAP(btn/btnb).
- **STORAGE 패널**: `storage` 배경 + `name` 제목태그("STORAGE") + 무게표시 + **탭**(ALL/WEAPONS/ARMOR/CONSUMABLES/MATERIALS/ETC = storage_btn) + 격자 + TAKE ALL/SORT(btn).

## 6. 확인 필요 (집에서 결정)
- **TMP vs legacy Text**: 현재 legacy `Text`. 시안 폰트 느낌 살리려면 TMP+커스텀 폰트 권장 → 전환 범위 결정 필요.
- **창고 탭 카테고리 필터**(ALL/WEAPONS/…): 시안엔 있으나 현 창고엔 없음 — 이번에 같이 구현할지.
- **격자 셀**: `GridPanel` 절차 렌더 유지 vs `Cell.prefab` Instantiate 중 택1.
- **HUD(RESOURCES)**: 이번 범위 포함 여부.
- 바인딩: 패널 스크립트 1개에 합칠지 vs `XxxView`(직렬화 ref) + 로직 분리.

## 7. 리스크 / 주의
- `CharacterPanelUI`(~3300줄)·`ShopUI`(~2400줄)는 생성+로직 결합 → **한 번에 X, 패널 단위 점진 전환**.
- 전환 중 **이중 상태**(코드 생성 + 프리팹 공존) 피하려 패널별로 완결.
- 9-slice 보더 안 맞으면 종이 질감 늘어남 → 스프라이트별 border 값 튜닝.
- Systems 씬은 **사용자 에디터 상태** → 프리팹 인스턴스 배치는 사용자와 합의/사용자가 직접.

## 변경 로그
| 날짜 | 질문 | 결정 | 근거 |
|------|------|------|------|
| 2026-06-26 | 코드 절차 생성 UI를 프리팹 기반(씬 편집 가능)으로 + 시안 스프라이트 적용 | **프리팹화 방향 확정(계획).** Resources/UI에 패널 프리팹 + 직렬화 ref 바인딩, 동적 격자만 절차 유지, 패널 단위 점진 전환. 이미지는 매핑표대로 자연스러운 곳만, 시맨틱 색은 UITheme 유지. | 에디터 비편집 문제 해소. 큰 두 패널은 점진 전환으로 리스크 관리. 구현은 후속. |
