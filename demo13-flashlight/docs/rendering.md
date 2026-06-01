# Rendering System

> **현 상태 = 진실.** 2026-06-02 **아이소메트릭(2.5D 하이브리드 3D) → 순수 탑다운 2D**로 전환 완료.
> 전환 배경·단계는 [`topdown-migration.md`](topdown-migration.md) 참고. 아트 생성 스펙은 [`topdown-art-spec.md`](topdown-art-spec.md).

## 한눈에 (확정)

| 항목 | 결정 |
|---|---|
| **렌더 파이프라인** | URP **2D Renderer**(Renderer2D). `URP-2D.asset` |
| **카메라** | **2D Orthographic, Z축 정면 직시** (3D 틸트 없음) |
| **원근감** | 카메라가 아니라 **스프라이트 아트에 미리 베이크**(near-overhead 시점, 레퍼: Darkwood/Hotline Miami) |
| **좌표계** | 월드 = **XY 평면**, 깊이 = **sortingOrder** (정사영이라 Z는 화면에 안 보이고 정렬용) |
| **이동/충돌** | **Rigidbody2D + Collider2D** (NavMesh 폐기) |
| **조명** | URP **2D Light**(Light2D) — 글로벌(밤 앰비언트) + 손전등(Point/Spot) |
| **빛 차폐** | **ShadowCaster2D** (벽/구조물에 부착, Light2D 차단) |
| **맵** | **Unity Tilemap**(바닥/벽) + TilemapCollider2D + CompositeCollider2D, 프롭은 **Prop2D 카탈로그** |

## 왜 탑다운 2D인가

- 아이소(2.5D 하이브리드)는 깊이정렬·접지·2D-in-3D 이질감을 오래 싸웠고, AI로 아이소 모듈 스프라이트를 뽑으면 투영각이 매번 달라 맞물리지 않음(2인 제작 비현실).
- 탑다운 2D는 Unity 네이티브 2D(Sprite/Rigidbody2D/Tilemap/Light2D)로 단순·견고. 각도/정렬/떠보임 문제가 원천 소멸.
- 입체감/높이감은 약간 포기하고 **명료함·제작 용이**를 택함. 분위기는 **손전등 콘 + 어둠 + 밀도**가 책임(밤 룩으로 검증).

## 카메라 / 좌표계

- 카메라 = 표준 2D 셋업. `CameraSortSetup`이 `TransparencySortMode.CustomAxis`, 정렬축 `(0,1,0)` 설정 → **Y가 낮을수록(화면 아래) 앞**.
- 모든 위치/투영/그림자 계산은 **XY 기준**. (구 "XZ 바닥평면 + 90° 눕힌 쿼드" 전제는 폐기.)
- 플레이어 추적: `CameraFollow`.

## 조명

- **글로벌 Light2D**: 밤 앰비언트. intensity ≈ 0.04(거의 암흑), 차가운 색(≈0.3,0.35,0.4). → 손전등 콘이 사실상 유일한 주광원.
- **손전등 Light2D**: 플레이어 자식 피벗에 부착, 마우스 방향으로 회전(`TopDownPlayer.UpdateFlashlight`). 따뜻한 색(≈1.0,0.92,0.75). inner ≈20° / outer ≈50°. 배터리 기반 on/off: `FlashlightController`(낮에는 `DayNightCycle` 이벤트로 자동 off).
- **그림자**: 빛을 막아야 하는 벽/구조물에 **ShadowCaster2D** 부착. (3D 스팟라이트 + ShadowsOnly 3D 박스 방식은 폐기.)
- 낮/밤 반응 컴포넌트(`DayNightCycle.OnPhaseChanged` 구독): `PostProcessController`, `NeonSign`, `RainController` 등. **Editor State Preservation** 규칙 유지 — `Start()`에서 값을 적용하지 않고 이벤트로만 변경.

## 맵 / 프롭

- **바닥·벽 = Unity Tilemap**. 벽 타일맵에 `TilemapCollider2D` + `CompositeCollider2D`(Static Rigidbody2D)로 이동 차단. 씬 골격은 `GameSceneBuilder`(에디터 메뉴)가 생성.
- **프롭 = Prop2D 카탈로그**(개별 스프라이트 + Collider2D). 막힘(못 가는 곳) = 비-트리거 Collider2D. walkability 그리드/NavMesh 없음.
  - `Prop2DDefinition`(SO): 스프라이트·머티리얼·sortingOffset + **콜라이더 모드**(None/Box/Polygon/Composite, 프롭마다 선택).
  - `Prop2DBuilder`(static): 정의→GameObject 런타임·에디터 공용 생성. Polygon은 `sprite.GetPhysicsShape`로 외곽선 자동.
  - 맵 도구는 [`map-tool.md`](map-tool.md) 참고.

## 셰이더 (네임스페이스 `BRB/`)

URP 2D 렌더러는 `Tags{ "LightMode"="Universal2D" }` 패스만 그린다. 유지 셰이더는 모두 Universal2D 패스 보유(2026-06-02).

| 셰이더 | 용도 | 조명 |
|---|---|---|
| `BRB/Pixelated` | 도트/맵 모듈 | Light2D 반응 |
| `BRB/PlayerSprite` | 플레이어(시트UV+컷아웃+아웃라인) | Light2D 반응 |
| `BRB/SpriteSheet` | 스프라이트 시트 UV | Light2D 반응 |
| `BRB/SpriteBillboard` | SpriteRenderer용(정점컬러+컷아웃+아웃라인) | Light2D 반응 |
| `BRB/SpineLitURP` | Spine(premultiplied 알파+정점컬러) | Light2D 반응 |
| `BRB/ShadowProjector` | 방향성 투영 그림자 | Unlit |
| `BRB/FlashlightBeam` | 빔 페이드 | Unlit (Light2D로 대체 가능) |
| `BRB/OcclusionOutline` | 깊이 가림 외곽선 | Unlit (순수 2D에선 의미 약함) |

- **삭제됨**: `CityBuilding`/`CityWall`/`RuinFloor`/`RoadFloor`/`WetFloor`/`Prop`(3D Forward 전용), `RuinPixel`, 구 `InkCity/*`(SpriteOutline/InkShadow/NightOverlay/PanelWiggle/InkDissolve/InkFloor/DotFloor).
- ⚠️ 스프라이트 임포트: **Mesh Type = Full Rect** 권장(Tight면 시트UV/shear 틀어짐). URP 17.3 / Unity 6.
- ⚠️ `ShadowProjector`/`FlashlightBeam`/`OcclusionOutline`은 iso 시절 잔재 — 순수 2D + Light2D + ShadowCaster2D 전제에선 대부분 불필요. 정리 예정.

## 변경 로그

| 날짜 | 결정 | 근거 |
|---|---|---|
| 2026-05-23~31 | (폐기) 벽/건물 = 3D 큐브, 바닥 = 2D Plane 하이브리드. 스텐실 바닥, 스팟라이트 차폐, BuildingInterior 알파 페이드, WallOcclusionOutline 아웃라인, ShadowProxyBuilder, 커스텀 MapBuilder(WallBuilder/PropQuadBuilder/층 시스템). | 전부 2026-06-02 탑다운 2D 전환으로 **삭제**. 상세 이력은 git history 참고. |
| 2026-06-01 | (폐기) 비주얼 = 2D 평면, 빛 차폐 = 안 보이는 3D 박스(ShadowsOnly) 하이브리드. | URP 2D 렌더러 + Light2D 전환으로 ShadowCaster2D가 대체. |
| 2026-06-02 | **탑다운 2D 전환 확정.** 렌더 파이프라인 URP-3D→URP-2D, 카메라 2D Orthographic, 좌표계 XY, 조명 Light2D, 맵 Tilemap, 이동 Rigidbody2D. 아이소 식별자 전면 정리(`Isometric*`→`TopDown*`), 3D 큐브/스텐실/스팟라이트/오클루전/MapBuilder 제거. | [`topdown-migration.md`](topdown-migration.md) 참고. |
| 2026-06-02 | 셰이더 네임스페이스 `InkCity/`·`Custom/`→`BRB/` 통일, 3D Forward 전용 셰이더 삭제, 유지 8개에 Universal2D 패스 추가. | URP 2D 렌더러는 Universal2D 패스만 그림. |
