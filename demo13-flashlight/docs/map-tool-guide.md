# 맵툴 사용 설명서 (탑다운 2D)

> **누구나 따라 할 수 있는 단계별 가이드.** 시스템 사양·결정 로그는 [`map-tool.md`](map-tool.md), 씬/부트 구조는 [`architecture.md`](architecture.md) 참고.
> 이 문서는 "어떻게 쓰는가"에 집중한다.

---

## 0. 핵심 개념 (먼저 읽기)

- **맵 = 스프라이트 배치.** 바닥·벽·소품·천장을 전부 **Prop2D**(스프라이트 + 콜라이더) 조각으로 씬에 찍어서 만든다. (Unity Tilemap도 씬에 있지만, 이 프로젝트는 Prop2D 배치가 주력.)
- **막힘 = 콜라이더.** 못 가게 막으려면 그 조각에 **콜라이더(트리거 아님)**를 주면 된다. NavMesh·walkability 격자 없음.
- **깊이 = Sorting.** 위/아래 겹침은 Sorting Layer + Order로 정한다. 화면 아래(Y 작음)가 앞.
- **시점은 아트에 구워져 있다.** 약 80° 틸트는 그림 자체에 그려져 있고, 카메라는 정직한 2D(회전 0). 모든 조각은 회전 0으로 배치.
- **편집은 Play가 아니라 씬 뷰에서.** 맵 그리기는 에디트 모드(씬 뷰 클릭)로 한다.

---

## 1. 빠른 시작 (맵 하나 찍어보기)

1. **맵툴 씬 열기**: 메뉴 `Tools ▸ TopDown ▸ Build ▸ Map Tool Scene` → `Assets/Scenes/MapTool2D.unity` 생성 → 그 씬을 연다.
2. **카탈로그 열기**: `Tools ▸ TopDown ▸ Map ▸ Prop Catalog` (창 제목 "2D Prop Catalog").
3. **에셋 등록**: Project에서 스프라이트 여러 개 선택 → 카탈로그에서 탭 고르고(예: **바닥**) → **일괄 등록** 섹션 펼쳐 → **선택 스프라이트 일괄 등록** 클릭.
4. **브러시 선택**: 팔레트 그리드에서 찍을 조각 **클릭**.
5. **배치 모드 ON**: 팔레트 위 **배치 모드** 버튼 클릭(초록) → 씬 뷰에서 **좌클릭/드래그**로 배치. (반투명 고스트가 커서를 따라옴)
6. **끝내기**: **ESC**로 배치 모드 종료.
7. **저장**: 상단 바 **맵 저장 (프리팹)** → `Assets/Maps/`에 프리팹으로 저장.

이게 전부다. 아래는 각 단계의 상세.

---

## 2. 최초 1회 셋업

| 순서 | 메뉴 | 결과 |
|---|---|---|
| 1 | `Tools ▸ TopDown ▸ Build ▸ Player Rig` (없으면) | `Resources/PlayerRig.prefab` 생성 (플레이어+카메라+조명) |
| 2 | `Tools ▸ TopDown ▸ Build ▸ Systems Scene` | `Systems.unity` 생성(매니저+UI+PlayerRig+조명) + 빌드세팅 등록 |
| 3 | `Tools ▸ TopDown ▸ Build ▸ Map Tool Scene` | `MapTool2D.unity` 생성(맵 그리기 전용 작업 씬) |

> **맵툴 씬은 게임 HUD/플레이어를 자동 스폰하지 않는다**(씬 이름에 "MapTool" 포함 시 스킵). 순수 편집용.

---

## 3. 카탈로그 창 둘러보기

`Tools ▸ TopDown ▸ Map ▸ Prop Catalog`

### 탭 (카테고리) — 종류마다 기본값이 다름
| 탭 | ID 접두어 | 기본 콜라이더 | 기본 그림자 | 비고 |
|---|---|---|---|---|
| 바닥 | `floor_` | None(통과) | OFF | 발밑 바닥 |
| 벽 | `wall_` | Box | ON | 길게는 Tiled 추천 |
| 프롭 | `prop_` | Box | ON | 가구·소품 |
| 오브젝트 | `object_` | Box | OFF | 기능 마커 위주(기본 Interactable) |
| 데칼 | `decal_` | None(통과) | OFF | 바닥/벽 위 장식(핏자국·균열·낙서) |
| 천장 | `ceiling_` | None | OFF | 정렬 자동 "Ceiling", 진입 시 페이드 |

### 상단 바
| 컨트롤 | 동작 |
|---|---|
| **＋ 맵 생성**(초록) | 씬에 `Map` 루트 생성/활성 — 배치본이 그 아래로 들어감 |
| **맵 저장 (프리팹)**(파랑) | `Map` 루트를 `Assets/Maps/`에 프리팹 저장 (+BoxCollider 조각에 ColliderAutoFit 자동 부착) |
| **스냅** | 배치 격자 크기(월드 단위). 0=자유 배치. 드래그 연속배치 간격·고스트 칸에도 적용 |
| **천장 반투명** 토글(+알파 슬라이더) | 편집 중 천장이 바닥을 가릴 때 켜면 반투명(에디터 전용, 저장/플레이엔 영향 없음) |

### 팔레트 그리드 (왼쪽)
- **좌클릭** = 브러시 선택
- **우클릭** = 메뉴(배치·편집·복제·삭제·프로젝트에서 보기)
- 상단 버튼: **배치 모드** / **크기조절** / **＋ 새 항목** / **↻**(새로고침)
- **검색** 칸, **분류 필터** 드롭다운으로 좁히기 (필터로 가려지면 "⚠ N개 숨김 · 전체 보기" 안내)

---

## 4. 에셋 등록하기

### 방법 A — 일괄 등록 (빠름, 추천)
1. Project에서 스프라이트 여러 개 선택.
2. 카탈로그에서 **탭** 선택(예: 벽).
3. **일괄 등록** 섹션 펼치고 공통값 설정: 머티리얼 / Sorting Layer / Order / Draw Mode / 그림자 / 분류.
4. **선택 스프라이트 일괄 등록** 클릭.
- ID는 `{접두어}_{0001}`부터 **자동 증가**(이름 충돌 없음). 같은 탭에 **이미 등록된 스프라이트는 건너뜀**. 결과 팝업으로 "신규 N개 / 건너뜀 M개" 표시.

### 방법 B — 개별 등록 (드래프트 방식)
1. **＋ 새 항목** 클릭 → 빈 드래프트가 폼에 뜸(아직 파일 아님).
2. 폼 채우기(아래 섹션).
3. **생성 & 등록**(초록) → `Resources/Props2D/{id}.asset` + 프리팹 생성. (취소하면 버려짐)

### 폼 섹션 요약
- **기본 정보**: 표시 이름, 카테고리(탭), **분류(컬렉션)**(예: "전당포"·"안전구역" — 탭을 가로지르는 테마 태그)
- **스프라이트**: 스프라이트, 머티리얼, Sorting Layer/Order, **Draw Mode**(Simple/Tiled/Sliced)
  - Tiled/Sliced는 **크기(월드단위)** 지정. 스프라이트가 "Tight"면 **스프라이트 Full Rect로 설정** 버튼으로 임포트 교정(안 하면 타일 렌더 깨짐).
- **막힘 영역(콜라이더)**: 모드(None/Box/Polygon/Composite), 트리거 여부, Box 크기배율·오프셋
- **투영 그림자**: 그림자 드리움 + Alpha Cutoff
- **(선택) 파괴/낡음/발광**: 부술 수 있는 오브젝트, 풍화 틴트, 램프·창문 발광(Light2D)
- **기능**: 아래 6장
- **미리보기**: 콜라이더가 **빨강**으로 표시 — 모양 확인용

---

## 5. 맵 그리기 (배치)

1. 팔레트에서 조각 **클릭**(브러시).
2. **배치 모드** 버튼 ON(초록, "● 배치 중 (ESC 종료)").
3. 씬 뷰에서:
   - **좌클릭** = 한 개 배치
   - **좌드래그** = 연속 배치(간격 = `max(0.05, 스냅)`)
   - 반투명 **고스트**가 커서를 따라옴, 격자 와이어 표시
   - **ESC** = 배치 모드 종료
4. **삭제**는 씬에서 해당 조각을 선택해 Delete(일반 Unity 방식).
5. **크기 조절**: **크기조절** 버튼 ON → 씬에서 조각 선택 후 모서리 드래그(Tiled는 size, 그 외는 scale, Shift=대칭).

> **스냅 팁**: 칸 맞춤은 스냅 `1`(1m 격자), 자유 배치는 `0`.

---

## 6. 기능 부여 (배치 = 동작하게)

기능이 필요한 조각은 폼 **기능** 섹션에서 지정. 저장하면 배치/프리팹 빌드 시 해당 컴포넌트가 자동 부착된다. **투명 마커(비주얼 없음)** 토글을 켜면 스프라이트 없이 기능만(에디터엔 기즈모).

| 기능 | 용도 | 핵심 입력 |
|---|---|---|
| **SpawnPoint** | 플레이어 등장 위치 | Spawn Point ID (비우면 `default`) |
| **Interactable** | 상호작용 오브젝트 | 종류(ExitPoint/MapBoard/Bed/Workbench/…), 프롬프트, 인식범위 |
| **LootContainer** | 수색 가능 상자 | 격자 가로/세로, 지역 루트 사용 |
| **ItemDrop** | 바닥 아이템 | 아이템 ID(비우면 지역루트), 수량 |
| **NPC** | NPC 배치 | NPC ID(`Resources/Data/NPC/`) |
| **Door** | 문(잠금) | 잠금(None/Key/Quest), 열쇠/퀘스트 ID |
| **Trigger** | 영역 진입 → 씬 전환 | 전환 씬, 도착 Spawn ID, 영역 크기, 즉시전환 |

**Interactable 종류**: ExitPoint(탈출)·MapBoard(지역선택)·Bed(휴식)·Workbench(제작)·Pickup·Container·NPC·Door·Generic

### 루프에 필요한 최소 구성
- **안전가옥**: SpawnPoint(`default`,`raid_return`,`raid_fail`,`raid_death`) + MapBoard + Bed + Workbench
- **레이드 맵**: SpawnPoint(`default`) + RaidManager + 줍기/상자 몇 개 + ExitPoint(또는 Trigger로 안전가옥 복귀)

> 골격이 필요하면 `Tools ▸ TopDown ▸ Build ▸ InGame/Safehouse Scene`이 위 오브젝트를 자동 배치해 준다(맵 콘텐츠만, 카메라/조명/매니저는 Systems가 공급).

---

## 7. 천장 (건물 진입 컷어웨이)

1. **천장** 탭에서 천장 스프라이트 등록(정렬 자동 "Ceiling").
2. 폼 천장 섹션: **건물 그룹 ID**(같은 ID끼리 같이 사라짐), **숨김 알파**(진입 시), **페이드 속도**, 트리거 크기/오프셋.
3. 배치하면 트리거 + `CeilingFader`가 자동 부착 → 플레이어가 건물에 들어가면 지붕이 부드럽게 사라진다.
4. 편집 중 천장이 거슬리면 상단 **천장 반투명** 토글.

---

## 8. 콜라이더 모드 고르기

| 모드 | 언제 | 비고 |
|---|---|---|
| **None** | 바닥·데칼·장식 | 통과 |
| **Box** | 네모난 벽·가구 | 가장 흔함. 크기배율·오프셋 조정 |
| **Polygon** | 나무·바위 등 불규칙 | 스프라이트 physics shape 외곽선 자동 |
| **Composite** | L자·T자 구조물 | 박스 2~3개 합성 |

벽을 **Tiled**로 길게 늘이면 Box 콜라이더도 길이에 맞춰진다.

---

## 9. 맵 저장 & 테스트

**저장**
1. 상단 **＋ 맵 생성**으로 `Map` 루트가 있는지 확인(배치본이 그 아래에).
2. **맵 저장 (프리팹)** → `Assets/Maps/`에 프리팹. (BoxCollider 조각에 ColliderAutoFit 자동 부착)

**게임 씬에 넣기**
- 새 레이드 맵: `Build ▸ InGame Scene`으로 골격 생성 → 저장한 맵 프리팹을 `Grid` 아래에 배치/연결.
- 안전가옥: `Build ▸ Safehouse Scene`.

**테스트**
- **Systems 씬을 열고 Play**(권장) → GameBoot이 안전가옥을 띄움.
- 게임플레이 씬에서 바로 Play해도 Systems가 자동 additive 로드됨.
- 확인: 스폰 위치, 콜라이더 막힘, 천장 페이드, 상호작용(문·상자·NPC·탈출).

---

## 10. 메뉴 레퍼런스

| 메뉴 | 역할 |
|---|---|
| `Tools ▸ TopDown ▸ Map ▸ Prop Catalog` | **메인 맵툴**(등록·배치·저장) |
| `Tools ▸ TopDown ▸ Map ▸ Decal Scatter Brush` | 데칼 흩뿌리기 브러시(바닥 불규칙화) |
| `Tools ▸ TopDown ▸ Map ▸ Prop ID 인덱스로 정규화` | 옛 이름-기반 ID를 인덱스로 일괄 개명 |
| `Tools ▸ TopDown ▸ Map ▸ Setup Scene Lighting` | 씬 2D 조명 셋업 |
| `Tools ▸ TopDown ▸ Map ▸ Generate Light Cookies` | 발광용 라이트 쿠키 생성 |
| `Tools ▸ TopDown ▸ Build ▸ Map Tool / Systems / InGame / Safehouse / Combat Sandbox Scene` | 각 씬 골격 생성 |
| `Tools ▸ TopDown ▸ Setup ▸ Ensure 'Ceiling' Sorting Layer / 'Destructible' Layer` | 필수 레이어 보장 |

---

## 11. 트러블슈팅 & 팁

- **타일 렌더가 깨짐** → Draw Mode가 Tiled/Sliced인데 스프라이트가 Tight. **스프라이트 Full Rect로 설정** 버튼 클릭.
- **천장이 안 사라짐** → Sorting Layer가 "Ceiling"인지, 트리거 크기/오프셋이 입구를 덮는지 확인.
- **일괄 등록이 0개** → 이미 같은 탭에 등록된 스프라이트(건너뜀). 결과 팝업 확인.
- **막혀야 할 벽을 통과** → 콜라이더 모드가 None이거나 트리거 ON인지 확인.
- **조각이 캐릭터 위로 덮임** → Sorting Layer/Order 조정(데칼은 바닥 위·엔티티 아래 전용 레이어 권장).
- **편집이 안 보임** → 맵툴 씬에 Global Light 2D가 있는지(없으면 스프라이트 검정).
- **탭마다 자주 쓰는 설정이 다름** → 일괄 등록 설정·선택 항목은 **탭별로 기억**된다(도메인 리로드 후에도 유지).

---

## 변경 로그
- 2026-06-04: 탑다운 2D 맵툴(Prop2D 카탈로그 + 씬 빌더) 신규 사용 설명서 작성. (구 아이소 맵빌더 가이드는 폐기됨)
