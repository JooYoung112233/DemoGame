# 아이소메트릭 맵 에디터 사용 가이드

> **좌표 시스템**: 좀보이드 스타일 — 3D 공간(XZ 평면)에 2D 빌보드 스프라이트 배치  
> **카메라**: 3D 오쏘그래픽, `Euler(30, 45, 0)`  
> **타일 크기**: `float tileSize` (기본 1.0, Unity 월드 단위)

---

## 1. 시작하기

### 1-1. 에디터 씬 생성 (권장 — 처음 1회)
1. 메뉴: **Tools > Isometric Map > Create Editor Scene**
2. 자동 생성:
   - 3D 오쏘그래픽 카메라 (쿼터뷰 `Euler(30,45,0)`)
   - IsometricCameraController (팬/줌)
   - MapRuntimeBootstrapper (런타임 초기화)
   - 기본 MapData (16×16, Ground/Objects/Roof 레이어)
3. 씬 저장 경로 선택 → 맵 에디터 & 타일 팔레트 자동 오픈

### 1-2. 기존 씬에 세팅 추가
- 메뉴: **Tools > Isometric Map > Setup Current Scene**
- 기존 씬에 카메라/부트스트래퍼를 추가하고 쿼터뷰로 전환

### 1-3. 샘플 에셋 생성
- 메뉴: **Tools > Isometric Map > Create Sample Tiles**
- `Assets/IsometricMapEditor/SampleData/`에 생성:
  - 타일 6종 (Grass, Dirt, Stone Floor, Brick Wall, Flower, Shallow Water)
  - 건물 1종 (Small House 2×2)
  - Prop 1종 (Street Light)
  - Harvestable 1종 (Berry Bush, 확률 50%, 드롭: berry 1~3개)
- ⚠️ 스프라이트는 각 에셋 Inspector에서 직접 할당해야 함

### 1-4. 맵 에디터 열기
- 메뉴: **Tools > Isometric Map Editor**
- 또는 에디터 씬 생성 시 자동 오픈

### 1-5. 새 맵 만들기
1. 에디터 창에서 **"Create New Map"** 버튼 클릭
2. 저장 경로 선택 (예: `Assets/Maps/MyCity.asset`)
3. 자동으로 Ground 레이어가 생성됨

### 1-6. 타일 에셋 만들기
1. Project 창 우클릭 → **Create > Isometric Map > Tile Definition**
2. Inspector에서 설정:
   - **Tile ID**: 고유 식별자 (예: `grass_01`)
   - **Sprite**: 아이소 타일 스프라이트 할당
   - **Category**: Ground / Road / Decoration / Wall / Water / Custom
   - **Walkable**: 이동 가능 여부
   - **Size**: 차지하는 셀 수 (기본 1x1)
3. 에디터 창에서 **Refresh** 버튼 → 팔레트에 표시됨

---

## 2. 타일 페인팅 (기본 맵 제작)

### 2-1. 타일 배치
1. 에디터 창에서 **Paint** 도구 선택
2. 하단 **Tile Palette**에서 타일 클릭
3. **Scene View**에서 맵 위를 클릭/드래그 → 타일 배치
   - 마우스 → XZ 평면(Y=0) ray-plane intersection으로 그리드 좌표 계산

### 2-2. 타일 삭제
1. **Erase** 도구 선택
2. Scene View에서 클릭/드래그 → 모든 레이어의 타일 제거

### 2-3. 레이어 관리
- **+ Layer** 버튼으로 레이어 추가
- 눈 아이콘(체크박스)으로 레이어 표시/숨김
- 레이어 이름 클릭 → 활성 레이어 변경 (Paint는 활성 레이어에 배치)

### 2-4. 그리드 설정
- **Tile Size**: 타일 월드 크기 (기본 1.0, Unity 단위)
- **Map Width / Height**: 그리드 셀 수 (기본 16×16)

---

## 3. 건물 배치

### 3-1. 건물 에셋 만들기
1. **Create > Isometric Map > Building Definition**
2. Inspector에서 설정:
   - **Building ID / Display Name**: 식별자와 표시 이름
   - **Base Sprite**: 건물 본체 스프라이트
   - **Roof Sprite**: 지붕 스프라이트 (진입 시 사라짐)
   - **Footprint**: 차지하는 셀 수 (예: 2x3)
   - **Is Enterable**: 진입 가능 여부
   - **Interior Map ID**: 연결할 인테리어 맵 ID

### 3-2. 건물 배치하기
1. 메뉴: **Window > Isometric Map > Tile Palette** 열기
2. **Buildings** 탭 선택 → 건물 **Select** 클릭
3. 메인 에디터에서 **Build** 도구 선택
4. Scene View에서 클릭 → 건물 배치 (풋프린트 겹침 자동 차단)

---

## 4. 건물 상태 전환 (낮/밤/시즌/파괴)

### 4-1. 상태 세트 만들기
1. **Create > Isometric Map > Building Variant Set**
2. 메뉴: **Window > Isometric Map > Variant Editor** 열기
3. 생성한 VariantSet 할당

### 4-2. 상태(Variant) 추가
1. **+ Add Variant** 클릭
2. 각 상태 설정:
   - **ID**: `day`, `night`, `winter`, `damaged` 등
   - **Base/Roof Sprite**: 해당 상태의 스프라이트
   - **Tint Color**: 색조 (밤=어둡게)
   - **Transition**: Instant / CrossFade / Dissolve

### 4-3. 자동 전환 룰 추가
1. **+ Add Rule** 클릭
2. 설정:
   - From: `day` → To: `night`
   - Condition: `TimeOfDay`, Param: `18-6` (18시~6시)
   - Auto: 체크
3. BuildingDefinition의 **Variant Set**에 할당

---

## 5. Walkability (이동 가능 영역)

### 5-1. 페인팅
1. 에디터 창에서 **Walk** 도구 선택
2. Scene View에서 클릭/드래그 → 셀 타입 변경

### 5-2. 셀 타입
| 타입 | 색상 | 설명 |
|------|------|------|
| **Walkable** | 초록 | 이동 가능 (기본) |
| **Blocked** | 빨강 | 이동 불가 (벽, 장애물) |
| **SlowZone** | 노랑 | 이동 가능하지만 감속 |
| **Hazard** | 주황 | 이동 가능하지만 데미지 |
| **TriggerZone** | 파랑 | 이동 가능 + 이벤트 트리거 |

### 5-3. Scene View 오버레이
- Walk 도구 활성화 시 자동으로 XZ 평면 위에 컬러 오버레이 표시

---

## 6. Props (배치 오브젝트: 가로등, 나무 등)

### 6-1. Prop 에셋 만들기
1. **Create > Isometric Map > Prop Definition**
2. 설정:
   - **Sprite**: 오브젝트 스프라이트
   - **Footprint**: 차지하는 셀 수
   - **Blocks Walkability**: 체크 시 자동으로 해당 셀 이동 불가
   - **Has Variants**: 상태 전환 필요 시 (가로등 on/off)

### 6-2. 배치하기
1. **Window > Isometric Map > Tile Palette** → **Props** 탭
2. Prop 선택 → 에디터 **Prop** 도구
3. Scene View 좌클릭: 배치 / 우클릭: 제거

---

## 7. 파밍 요소 (Harvestable)

### 7-1. 파밍 에셋 만들기
1. **Create > Isometric Map > Harvestable Definition**
2. 설정:
   - **Sprite / Depleted Sprite**: 일반/채집 후 스프라이트
   - **Drop Table**: 드롭 아이템 목록 (아이템ID, 수량, 확률)
   - **Respawn Time**: 리스폰 시간 (0 = 1회성)
   - **Spawn Conditions**: 노출 조건 목록

### 7-2. 노출 조건 설정 예시
| 조건 타입 | Param | 의미 |
|-----------|-------|------|
| Always | - | 항상 노출 |
| Probability | `0.3` | 30% 확률 |
| TimeOfDay | `18:00-06:00` | 밤에만 |
| Season | `winter` | 겨울에만 |
| QuestComplete | `quest_01` | 퀘스트 완료 후 |
| PlayerLevel | `>=10` | 레벨 10 이상 |
| Custom | `my_flag` | 코드에서 직접 제어 |

---

## 8. 인테리어 맵 (건물 내부)

### 8-1. 인테리어 맵 만들기
1. **Create > Isometric Map > Interior Map Data**
2. 메뉴: **Window > Isometric Map > Interior Editor** 열기
3. 생성한 InteriorMapData 할당

### 8-2. 탭별 설정
- **Layout**: 그리드 크기 설정, 레이어 확인
- **Entry Points**: 진입/출구 포인트 추가 (위치, 방향, 연결 대상)
- **Escape Points**: 탈출구 추가 (문, 창문, 비밀통로, 비상구)
- **Props**: 인테리어 내 배치 오브젝트
- **Walkability**: 내부 이동 가능 영역

### 8-3. 건물과 연결
1. BuildingDefinition Inspector에서:
   - **Is Enterable**: 체크
   - **Interior Map ID**: 인테리어 맵의 `interiorId` 입력

---

## 9. 건물간 내부 연결 (터널, 스카이워크)

### 9-1. 연결 에디터
1. 메뉴: **Window > Isometric Map > Connection Editor** 열기
2. MapData 할당

### 9-2. 연결 추가
1. **+ Add Connection** 클릭
2. 설정:
   - **Type**: Door / Tunnel / Skywalk / Underground / Elevator
   - **From**: 출발 맵ID + 연결 포인트ID
   - **To**: 도착 맵ID + 연결 포인트ID

### 9-3. Scene View에서 연결
1. 에디터 **Connect** 도구 선택 (또는 Scene View 오버레이 툴바)
2. 첫 번째 건물 클릭 → 두 번째 건물 클릭 → 연결 자동 생성
3. Escape 키로 선택 취소

---

## 10. 도로 오토타일링

### 10-1. 도로 에셋 만들기
1. **Create > Isometric Map > Road Definition**
2. 16개 스프라이트 할당 (4비트 이웃 비트마스크):
   - 0: 단독, 1: 북, 2: 동, 3: 북+동, ... 15: 사방 연결

### 10-2. 도로 페인트
1. Scene View 오버레이 툴바에서 **Road** 모드 활성화
2. 좌클릭: 도로 배치 (주변 도로 자동 연결)
3. 우클릭: 도로 제거 (주변 타일 자동 업데이트)

---

## 11. 익스포트 & 빌드

### 11-1. JSON 익스포트
- 에디터 창 툴바 → **Export** 버튼
- 또는 코드: `MapExporter.ExportToJson(mapData)`

### 11-2. 맵 검증
- 에디터 창 툴바 → **Validate** 버튼
- 또는 Scene View 오버레이 → **Validate** 버튼
- 검사 항목: 맵ID, 건물 겹침, 인테리어 연결 누락, 범위 초과 등

### 11-3. Scene 자동 생성
- 에디터 창 툴바 → **Scene** 버튼
- MapData 기반으로 Unity Scene 자동 생성
  - 타일 (레이어별 빌보드 스프라이트)
  - 건물 (base + roof 분리)
  - Props
  - 3D 오쏘그래픽 카메라 + IsometricCameraController
  - MapRuntimeBootstrapper

### 11-4. 빌드 시 자동 처리
- 빌드 시 `BuildPreprocessor`가 자동 실행
- 모든 맵 검증 → 실패 시 빌드 중단
- 통과 시 JSON으로 StreamingAssets에 자동 익스포트

---

## 12. Play 모드 테스트

### 12-1. 즉시 테스트
1. 에디터 창에서 맵 선택된 상태로 **Play** 버튼
2. `EditorPlaySync`가 자동으로 `MapRuntimeBootstrapper` 생성
3. 타일, 건물, Props, 파밍요소, Walkability 모두 자동 로드

### 12-2. 런타임 디버그
- Play 모드에서 **F1** 키 → 디버그 오버레이 토글
- 표시: 3D 그리드 라인, Walkability 컬러, 연결 포인트

### 12-3. 실시간 프리뷰
- 에디터에서 타일/건물 수정 → Scene View에 즉시 반영 (Play 모드 아닐 때)
- `LivePreviewManager`가 변경 감지 후 프리뷰 오브젝트 갱신

---

## 13. Scene View 단축키 & 오버레이

### Scene View 오버레이 툴바
Scene View 상단에 **Isometric Map Tools** 오버레이:
- **Walk**: Walkability 페인트 모드
- **Road**: 도로 오토타일 모드
- **Prop**: 오브젝트 배치 모드
- **Connect**: 건물 연결 모드
- **Validate**: 맵 검증 실행

### 하단 정보 바
- 현재 마우스 위치의 그리드 좌표 (XZ 평면 기준)
- 해당 셀의 Walkability 타입
- 해당 셀의 타일 정보

### 카메라 조작 (Play 모드)
- **WASD / 방향키**: XZ 평면 팬
- **마우스 휠**: 줌 인/아웃

---

## 14. 좌표 시스템 참고

### 3D 공간 구조 (좀보이드 스타일)
```
Y (높이)
│
│   카메라 Euler(30, 45, 0)
│  ╱
│ ╱   오쏘그래픽
│╱
└──────── X
 ╲
  ╲
   Z
```
- **타일**: XZ 평면 (Y=0)에 배치
- **스프라이트**: 빌보드 `Quaternion.Euler(90, 0, 0)` → 카메라를 향함
- **GridToWorld**: `Vector3(cell.x * tileSize, 0, cell.y * tileSize) + originOffset`
- **WorldToGrid**: XZ 좌표에서 역산 (`Mathf.RoundToInt`)
- **소팅**: `(cell.x + cell.y) * 10 + layerOffset`

### 그리드 시각화
- Scene View에서 다이아몬드 형태 그리드 표시 (XZ 평면)
- 각 셀의 4개 코너: GridToWorld로 계산 후 Handles.DrawLine

---

## 자주 묻는 질문

**Q: 처음 시작할 때 뭘 해야 하나요?**
→ `Tools > Isometric Map > Create Editor Scene`으로 에디터 씬 생성이 가장 빠름.
→ 그 후 `Create Sample Tiles`로 샘플 에셋 생성 → 스프라이트 할당 → Paint 시작.

**Q: 타일이 팔레트에 안 보여요**
→ TileDefinition 에셋을 만들었는지 확인. 에디터 Refresh 버튼 클릭.

**Q: Scene View에 그리드가 안 보여요**
→ 에디터 창에서 MapData가 선택되어 있는지 확인.
→ Scene View가 3D 모드인지 확인 (2D 모드 X).

**Q: Play 모드에서 맵이 안 보여요**
→ MapData가 에디터 창에 할당되어 있어야 EditorPlaySync가 동작함.
→ 또는 Scene에 직접 MapRuntimeBootstrapper 컴포넌트 추가 후 MapData 할당.

**Q: 건물 지붕이 안 사라져요**
→ BuildingDefinition에 Roof Sprite가 할당되어 있어야 함.
→ 런타임에서 RoofHideController가 플레이어 거리 기반으로 동작.

**Q: 타일 크기를 바꾸고 싶어요**
→ 에디터 창 Grid Settings에서 **Tile Size** 변경 (기본 1.0).
→ 이미 배치된 타일은 자동으로 새 크기에 맞게 재배치됨.
