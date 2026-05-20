# 아이소메트릭 맵툴 기획 결정 기록

## 현재 상태

### 에디터 형태
- **Unity Editor Extension** (EditorWindow + EditorTool + SceneView 오버레이)
- 별도 앱이 아닌 Unity 에디터 내장 방식

### 좌표 시스템 (3D 공간 + 2D 스프라이트, 좀보이드 스타일)
- **3D 공간**: 타일은 XZ 평면에 배치, Y축 = 높이
- 카메라: 3D 오쏘그래픽, `Euler(30, 45, 0)`
- 빌보드 스프라이트: `Quaternion.Euler(90, 0, 0)` (카메라를 향함)
- `float tileSize = 1f` (단일 크기, tileWidth/tileHeight 제거)
- GridToWorld: `Vector3(cell.x * tileSize, 0, cell.y * tileSize) + originOffset`
- WorldToGrid: XZ 좌표에서 역산
- 소팅: `(cell.x + cell.y) * 10 + layerOffset + floor * 1000`
- Scene View 마우스: XZ 평면(Y=0) ray-plane intersection
- 층(floor) 시스템: API에 예약됨, 추후 구현 예정

### 건물 상태 전환
- **범용 상태 시스템** (BuildingVariantSet)
- 낮/밤, 시즌, 파괴 단계, 커스텀 등 모든 상태 전환 통합
- Props(가로등 등)에도 동일 시스템 재사용

### 인테리어 시스템
- 건물별 **별도 ScriptableObject**로 내부 맵 관리
- 외부 맵과 독립된 좌표계
- 진입/출구 ConnectionPoint로 연결
- 인테리어에도 Props/Harvestable/Walkability 동일 적용
- 건물간 내부 연결 (터널, 스카이워크) 지원

### 배치 오브젝트 (Props)
- 가로등, 나무, 표지판 등 배치 가능
- footprint(차지 셀 수), blocksWalkability(자동 이동불가) 설정

### 파밍 요소 (Harvestable)
- 확률/조건부 노출 시스템 (SpawnCondition)
- 조건 타입: Always, Probability, TimeOfDay, Season, Weather, QuestComplete, PlayerLevel, Custom
- 드롭 테이블 (아이템ID, 수량 범위, 확률)
- 리스폰 설정 (시간 기반 / 맵 리로드 / 1회성)

### 이동 가능 영역 (Walkability)
- 셀 단위 페인팅 방식
- 타입: Walkable, Blocked, SlowZone, Hazard, TriggerZone

### 탈출구 시스템
- 인테리어 내 탈출 포인트 설정
- 타입: Door, Window, SecretPassage, Emergency
- 열쇠 필요, 숨겨진 통로, 조건부 사용 가능

### 에디터-인게임 연동
- LivePreviewManager: 에디터 수정 → Scene View 즉시 반영
- EditorPlaySync: Play 모드 진입 시 현재 맵 자동 로드
- MapRuntimeBootstrapper: 원스톱 런타임 초기화
- MapSceneGenerator: SO → Unity Scene 자동 생성 (Phase 9)
- BuildPreprocessor: 빌드 전 자동 검증 (Phase 9)

### 데이터 형식
- 에디터: ScriptableObject (Undo, Inspector 네이티브 지원)
- 익스포트: JSON (디버깅 편리, 버전 마이그레이션)

### 구현 순서
1. 코어 그리드 + 타일 페인팅 + 연동 기반 (MVP) ← **완료**
2. 건물 + 멀티타일 ← **완료**
3. 건물 상태 전환 ← **완료**
4. 도로 오토타일링 ← **완료**
5. Walkability + 이동 불가 영역 ← **완료**
6. 배치 오브젝트 + 파밍 요소 ← **완료**
7. 인테리어 맵 + 진입/탈출 ← **완료**
8. 건물간 내부 연결 ← **완료**
9. 익스포트 + 빌드 파이프라인 ← **완료**
10. 샘플 + 문서 ← **완료**

---

## 변경 로그

| 날짜 | 변경 내용 |
|------|-----------|
| 2025-05-20 | 초기 기획 결정 기록. Phase 1 구현 시작. |
| 2026-05-20 | 전체 Phase 1~9 구현 완료. 좌표 시스템을 2D→3D로 전면 리팩터 (좀보이드 스타일: 3D 공간 + 2D 빌보드 스프라이트). tileWidth/tileHeight → float tileSize. Vector2→Vector3. Rect→Bounds. Scene View mouse: ray-plane intersection on XZ. 카메라: 3D orthographic Euler(30,45,0). 층(floor) API 예약. |
