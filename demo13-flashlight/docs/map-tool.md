# 맵 도구 (탑다운 2D)

> **현 상태 = 진실.** 2026-06-02 아이소메트릭 맵빌더(WallBuilder/PropQuadBuilder/MapBuilderManager 등) **전면 폐기**.
> 탑다운 2D는 Unity 네이티브 **Tilemap** + **Prop2D 카탈로그** + 씬 빌더 에디터로 구성.
> 렌더 전제는 [`rendering.md`](rendering.md), 전환 배경은 [`topdown-migration.md`](topdown-migration.md).

## 구성

| 도구 | 형태 | 역할 |
|---|---|---|
| `GameSceneBuilder` | 에디터 메뉴 (`Tools/BRB/Build Scene/*`) | InGame/Safehouse 씬 골격 자동 생성 — 2D 카메라(CustomAxis 정렬) + CameraFollow, EventSystem, 글로벌 Light2D, Grid + Floor/Walls Tilemap(+TilemapCollider2D/CompositeCollider2D/Static Rigidbody2D), SpawnPoint |
| `MapTool2DSceneBuilder` | 에디터 | 탑다운 맵 제작용 씬 셋업 보조 |
| `Prop2DCatalogEditor` | EditorWindow (`Tools/TopDown 2D/Prop Catalog`) | Prop2D 정의 목록·생성·삭제 + 콜라이더 시각 미리보기. 에셋은 `Assets/Resources/Props2D/` |

## 맵 제작 흐름

1. **씬 골격**: `GameSceneBuilder`로 InGame/Safehouse 씬 생성.
2. **바닥/벽**: Unity **Tilemap**으로 페인트. 바닥 = Floor 타일맵, 벽 = Walls 타일맵(TilemapCollider2D + CompositeCollider2D로 이동 차단).
3. **프롭**: `Prop2DCatalogEditor`로 정의 등록 후 씬에 배치. 각 프롭은 `SpriteRenderer + Collider2D`.
   - 콜라이더 모드(프롭마다 선택): **None**(장식) / **Box**(네모) / **Polygon**(나무·바위, 스프라이트 physics shape 외곽선 자동) / **Composite**(각진 구조물, 박스 2~3개 합성).
4. **막힘 규칙**: walkability 그리드/NavMesh 없음 — **비-트리거 Collider2D**가 곧 이동 차단.

## 좌표 / 정렬

- 월드 = **XY 평면**, 깊이 = **sortingOrder**. 카메라 정렬축 `(0,1,0)`(Y 낮을수록 앞).
- 싱글톤/플레이어는 씬에 배치하지 않고 코드로 자동 스폰(`TopDownPlayer.Bootstrap` 등). 씬에는 SpawnPoint만.

## 핵심 결정 로그

| 날짜 | 결정 |
|---|---|
| 2026-06-02 | 아이소 맵빌더(커스텀 EditorWindow + 3D 큐브 벽 + 빌보드 프롭 + JSON 베이크) 전면 폐기. |
| 2026-06-02 | 탑다운 맵 = Tilemap(바닥/벽) + Prop2D 카탈로그(프롭). 막힘 = 비-트리거 Collider2D 규칙. |
| 2026-06-02 | Prop2D 콜라이더 전략 = "프롭마다 선택"(None/Box/Polygon/Composite). |
