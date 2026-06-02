# Rendering System

> **현 상태 = 진실.** 2026-06-02 **아이소메트릭(2.5D 하이브리드 3D) → 순수 탑다운 2D**로 전환 완료.
> 전환 배경·단계는 [`topdown-migration.md`](topdown-migration.md) 참고. 아트 생성 스펙은 [`topdown-art-spec.md`](topdown-art-spec.md).

## 한눈에 (확정)

| 항목 | 결정 |
|---|---|
| **렌더 파이프라인** | URP **2D Renderer**(Renderer2D). `URP-2D.asset` |
| **카메라** | **2D Orthographic, Z축 정면 직시** (3D 틸트 없음) |
| **원근감** | 카메라가 아니라 **스프라이트 아트에 ~80° 틸트를 베이크**(near-overhead, 레퍼: Darkwood). 정면/측면이 살짝 보임 |
| **오브젝트 배치** | 타일·프롭·캐릭터 모두 **회전 (0,0,0)** (순수 2D, 스프라이트가 카메라 정면. 빌보드/눕힘 없음) |
| **좌표계** | 월드 = **XY 평면**, 깊이 = **sortingOrder** (정사영이라 Z는 화면에 안 보이고 정렬용) |
| **이동/충돌** | **Rigidbody2D + Collider2D** (NavMesh 폐기) |
| **조명** | URP **2D Light**(Light2D) — 글로벌(앰비언트) + 시야 콘(플레이어 FOV) |
| **가시성** | **좀보이드식 시야(FOV)** — 적은 플레이어 바라보는 부채꼴 밖이면 안 보임/어둑. 손전등 폐기 |
| **빛 차폐** | **ShadowCaster2D** (벽/구조물에 부착, Light2D·시야 차단) |
| **맵** | **Unity Tilemap**(바닥/벽) + TilemapCollider2D + CompositeCollider2D, 프롭은 **Prop2D 카탈로그** |

## 왜 탑다운 2D인가

- 아이소(2.5D 하이브리드)는 깊이정렬·접지·2D-in-3D 이질감을 오래 싸웠고, AI로 아이소 모듈 스프라이트를 뽑으면 투영각이 매번 달라 맞물리지 않음(2인 제작 비현실).
- 탑다운 2D는 Unity 네이티브 2D(Sprite/Rigidbody2D/Tilemap/Light2D)로 단순·견고. 각도/정렬/떠보임 문제가 원천 소멸.
- 입체감/높이감은 약간 포기하고 **명료함·제작 용이**를 택함. 분위기는 **시야 콘 + 어둠 + 밀도**가 책임.

## 카메라 / 좌표계

- 카메라 = 표준 2D 셋업. `CameraSortSetup`이 `TransparencySortMode.CustomAxis`, 정렬축 `(0,1,0)` 설정 → **Y가 낮을수록(화면 아래) 앞**.
- 모든 위치/투영/그림자 계산은 **XY 기준**. (구 "XZ 바닥평면 + 90° 눕힌 쿼드" 전제는 폐기.)
- **모든 스프라이트(타일/프롭/캐릭터)는 회전 (0,0,0)** — 카메라 정면을 향하는 순수 2D. 원근은 아트가 담당하므로 트랜스폼 회전·빌보드 불필요.
- **플레이어 카메라 = 어느 씬이든 한 세트(자동 장착).** 플레이어가 카메라를 물리적으로 들고 다니지 않고(그러면 씬 카메라와 2개 충돌), `CameraFollow`가 런타임에 그 씬의 메인 카메라(없으면 첫 Camera)를 플레이어 카메라로 **자동 장착**: ①`CameraFollow`(추적+셰이크/줌) ②후처리 ON ③피격 연출 Volume(비네트/색수차) 보장. 이미 장착된 씬(InGame/Safehouse/Sandbox)은 중복 스킵.
  - → 맵툴 씬(`MapTool2D`)에서 Play 시 맵 카메라가 플레이어를 따라가 **이동 테스트 + 피격 연출까지 동일하게** 동작(씬 재생성·수정 불필요).

## 조명 / 가시성 (시야 FOV) — 2026-06-02 전환

> **손전등(주광원) 개념 폐기 → 좀보이드식 시야(FOV).** 기존 손전등 코드(`FlashlightController`/`FlashlightBeam` 셰이더/손전등 Light2D)는 **완전 제거 후 시야 시스템 신규 작성**.

- **가시성 모델 = 하이브리드** (확정):
  - 평소엔 적당히 보이되, **멀거나 그늘·실내·밤**은 어둑.
  - 플레이어가 **바라보는 방향 부채꼴(시야 콘) 밖의 적은 안 보임/어둑** — "어디서 적이 튀어나오나"의 긴장.
  - 가시성은 **지역/시간대별로 다름**(`WorldRegionCatalog`/`RegionTimeManager`/`DayNightCycle` 연동).
- **시야 콘 구현 방향**: 플레이어 facing(마우스 방향) 기준 부채꼴 Light2D(또는 시야 마스크). `TopDownPlayer.FacingDirection`으로 회전. 적 가시성은 시야 콘 안/밖 판정으로 sprite 알파·표시 토글.
- **글로벌 Light2D**: 앰비언트(밤·실내는 어둑, 낮·야외는 밝게). 하이브리드라 intensity는 지역/시간대별 가변.
- **그림자/차폐**: 벽·구조물에 **ShadowCaster2D** → 시야 콘과 빛을 막음(벽 뒤는 안 보임). (3D 스팟라이트 + ShadowsOnly 박스 방식 폐기.)
- 낮/밤 반응 컴포넌트(`DayNightCycle.OnPhaseChanged` 구독): `PostProcessController`, `NeonSign`, `RainController` 등. **Editor State Preservation** 규칙 유지 — `Start()`에서 값을 적용하지 않고 이벤트로만 변경.

> ⚠️ 데모 폴더명 `demo13-flashlight`는 역사적 이름. 손전등 메커니즘은 폐기됨(시야 FOV로 대체).

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
| `BRB/Pixelated` | 도트/맵 모듈(범용) | Light2D 반응 |
| `BRB/FloorPixel` | 바닥(Tilemap/스프라이트) — Pixelated 동일 픽셀, 불투명·컷아웃 없음, 정점컬러 지원 | Light2D 반응 |
| `BRB/WallPixel` | 벽(Pixelated 동일 픽셀 + 문 흰색뚫기 `_WHITE_CUTOUT` + 오클루전 페이드 `_Alpha`). `_SHADOW_MODE` 켜면 같은 셰이더가 벽 그림자(투영+밑동접지)로도 동작 | Light2D 반응 / 그림자모드 Unlit |
| `BRB/PropPixel` | 프롭(픽셀+색단계+외곽선+알파 컷아웃) | Light2D 반응 |
| `BRB/PlayerSprite` | 플레이어(시트UV+컷아웃+아웃라인) | Light2D 반응 |
| `BRB/SpriteSheet` | 스프라이트 시트 UV | Light2D 반응 |
| `BRB/SpriteBillboard` | SpriteRenderer용(정점컬러+컷아웃+아웃라인) | Light2D 반응 |
| `BRB/SpineLitURP` | Spine(premultiplied 알파+정점컬러) | Light2D 반응 |
| `BRB/ShadowProjector` | 일반 모듈 방향성 투영 그림자(반응형) | Unlit |
| `BRB/OcclusionOutline` | 깊이 가림 외곽선 | Unlit (순수 2D에선 의미 약함) |
| ~~`BRB/FlashlightBeam`~~ | (삭제 예정 — 손전등 폐기) | — |

- **모듈 그림자(✅ 2026-06-02)**: 드라이버 `FlatShadow.cs`(모듈에 부착)가 자식 그림자(SpriteRenderer/MeshRenderer 자동감지)를 만든다. **2종 독립 토글**:
  - **① 정적 발밑 그림자(`groundShadow`)**: 이미지 발밑에 고정되는 드롭 섀도우. **빛과 무관하게 항상 같은 자리**(LateUpdate에서 안 건드림). ShadowProjector를 **화면 아래(앞) 고정 방향 + `groundLength`만큼 짧게 투영**해 발밑에 깔리게 함. `sortingOrder−2`. ⚠️ `groundLength=0`이면 스프라이트 뒤에 완전히 가려 안 보임(초기 버그 원인) — 0.3~0.5 권장.
  - **② 동적 투영 그림자(`projectedShadow`)**: 매 프레임 Light2D 위치로 방향(XY)·길이 갱신, 빛 반대쪽으로 **늘어나는 반응형**. 벽은 `baseContact` ON → `WallPixel(_SHADOW_MODE)`, 일반은 `ShadowProjector`. `sortingOrder−1`.
  - **방향 기준(`directionMode`)**: `Player`(기본, `FlatShadowDirector.Source`=플레이어 따라) / `NearestLight`(최근접 Point Light2D) / `SpecificLight`(지정 `lightSource`) / `Manual`(`manualDirection` 월드 고정). 프롭마다 카탈로그 "Shadow" 섹션에서 선택.
  - **`FlatShadowDirector`(정적 매니저)**: 그림자 방향 공용 기준. **맵이 먼저 생기고 플레이어가 늦게 스폰**돼도 `TopDownPlayer.Instance`를 **지연 참조**(폴링/FindObjects 없음)하므로, Player 모드 프롭들이 플레이어 생성 즉시 자동 연결. 소스 없을 동안은 `manualDirection`으로 임시 표시. `FlatShadowDirector.SetSource(t)`로 다른 기준(횃불 등) 수동 지정 가능.
  - **autoFlipV**: 동적 투영의 위/아래 앵커를 **라이트(플레이어) 위치로 자동 결정**(라이트가 위→그림자 아래로, 아래→위로). 수동 flipV 안 만져도 플레이어 방향 따라 자연스러움. OFF면 수동 flipV.
  - 둘 다 켜면: 발밑 고정 그림자 + 빛 따라 늘어나는 그림자가 겹쳐 그려짐.
  - **기본값 버튼**(`FlatShadowEditor`): "현재값을 기본값으로 저장"(EditorPrefs) → 새 FlatShadow 추가(`Reset`) 시 자동 적용. "기본값으로 리셋"/"코드 기본값으로"/"저장된 기본값 삭제".
  - **부착 방법**: ① Prop2D는 `Prop2DDefinition.castShadow` ON → `Prop2DBuilder`가 자동으로 `FlatShadow` 부착(베이크된 프리팹에 포함). **맵툴 카탈로그는 `Wall`·`Prop` 카테고리 신규 항목에 `castShadow` 기본 ON**(`DefaultCastShadow`, 바닥/오브젝트마커는 OFF) → 벽·프롭 모두 정적 발밑 + 동적 투영 그림자가 동일하게 들어감. 폼의 "Shadow" 섹션에서 토글·길이·진하기 조절. ② 수동 배치 오브젝트는 `FlatShadow` 컴포넌트 직접 추가. **머티리얼에 Shadow Mode 값만 넣어선 안 생김** — 그림자는 FlatShadow가 만드는 별도 자식임.
  - **에디터 미리보기**: `FlatShadow`는 `[ExecuteAlways]` — Play 안 해도 씬에서 그림자가 보임. 그림자 자식은 `HideFlags.DontSave`라 씬/프리팹에 저장 안 됨(런타임 전용 비주얼).
  - **발밑 접지감은 ①(정적)이 담당.** 셰이더의 in-mesh `_ContactStrength`(투영 메시 내 밑동 darkening)은 빛 따라 같이 움직여서 "고정 그림자"가 아니므로 FlatShadow가 0으로 끔. 고정 그림자는 별도 정적 자식으로 그림.
  - **실루엣 일치**: 그림자 자식은 원본의 `_Cutoff`/`_WHITE_CUTOUT`·Tiled(`drawMode`/`size`)를 복사 → 문 구멍은 그림자도 안 생기고, 타일 벽도 전체 길이로 그림자가 늘어남.
  - ※ URP 2D는 머티리얼당 Universal2D 패스 1개만 그려서, 그림자는 본체 셰이더에 "패스 추가"로는 못 넣고 **별도 오브젝트**로 그림(벽은 `WallPixel`의 `_SHADOW_MODE` 키워드 변형 재사용).
- **타격감 흰 플래시(✅ 2026-06-02)**: 전용 **`BRB/SpriteFlash`**(URP 2D sprite-lit + `_FlashColor`/`_FlashAmount`) 신설. `HitFlash.cs`가 바디 스프라이트에 머티리얼 자가설치 후 `_FlashAmount`로 적중 시 흰색 깜빡. `SpriteRenderer.color` 대신 셰이더 레벨이라 베이스 색·조명·틴트와 무관. (기존 BRB 셰이더는 캐릭터가 안 쓰므로 미수정.) ⚠️ 빌드 시 `Shader.Find` 위해 Always Included Shaders 등록 필요.
- **삭제됨**: `CityBuilding`/`CityWall`/`RuinFloor`/`RoadFloor`/`WetFloor`/`Prop`(3D Forward 전용), `RuinPixel`, 구 `InkCity/*`(SpriteOutline/InkShadow/NightOverlay/PanelWiggle/InkDissolve/InkFloor/DotFloor).
- ⚠️ 스프라이트 임포트: **Mesh Type = Full Rect** 권장(Tight면 시트UV/shear 틀어짐). URP 17.3 / Unity 6.
- ⚠️ `ShadowProjector`/`OcclusionOutline`은 iso 시절 잔재 — 순수 2D 전제에선 대부분 불필요. `FlashlightBeam`은 손전등 폐기로 삭제 예정.

## 변경 로그

| 날짜 | 결정 | 근거 |
|---|---|---|
| 2026-05-23~31 | (폐기) 벽/건물 = 3D 큐브, 바닥 = 2D Plane 하이브리드. 스텐실 바닥, 스팟라이트 차폐, BuildingInterior 알파 페이드, WallOcclusionOutline 아웃라인, ShadowProxyBuilder, 커스텀 MapBuilder(WallBuilder/PropQuadBuilder/층 시스템). | 전부 2026-06-02 탑다운 2D 전환으로 **삭제**. 상세 이력은 git history 참고. |
| 2026-06-01 | (폐기) 비주얼 = 2D 평면, 빛 차폐 = 안 보이는 3D 박스(ShadowsOnly) 하이브리드. | URP 2D 렌더러 + Light2D 전환으로 ShadowCaster2D가 대체. |
| 2026-06-02 | **탑다운 2D 전환 확정.** 렌더 파이프라인 URP-3D→URP-2D, 카메라 2D Orthographic, 좌표계 XY, 조명 Light2D, 맵 Tilemap, 이동 Rigidbody2D. 아이소 식별자 전면 정리(`Isometric*`→`TopDown*`), 3D 큐브/스텐실/스팟라이트/오클루전/MapBuilder 제거. | [`topdown-migration.md`](topdown-migration.md) 참고. |
| 2026-06-02 | 셰이더 네임스페이스 `InkCity/`·`Custom/`→`BRB/` 통일, 3D Forward 전용 셰이더 삭제, 유지 8개에 Universal2D 패스 추가. | URP 2D 렌더러는 Universal2D 패스만 그림. |
| 2026-06-02 | **아트 각도·배치 회전 확정.** 아트에 **~80° 틸트 베이크**(카메라는 2D 정면, 안 기울임), 타일·모든 오브젝트는 **회전 (0,0,0)** 배치(순수 2D, 빌보드/눕힘 없음). | [`topdown-art-spec.md`](topdown-art-spec.md) 참고. 카메라·트랜스폼은 단순 유지, 입체감은 아트가 전담. |
| 2026-06-02 | **손전등 폐기 → 좀보이드식 시야(FOV).** 적은 플레이어 바라보는 부채꼴 시야 밖이면 안 보임/어둑. 어둠=하이브리드(지역/시간대별 가변). 손전등 코드(FlashlightController/FlashlightBeam/손전등 Light2D) 완전 제거 후 시야 시스템 신규. | 손전등 단일 주광원보다 FOV 가시성이 긴장감·전투 무대로 더 강함. 타격감 설계와 연동([`combat.md`](combat.md)). |
| 2026-06-02 | **플레이어 상시 라이트 + 글로벌 어둠 (FOV 전 단계).** 손전등 컨트롤러 제거 → 플레이어 자식 `PlayerLight`(Point Light2D, 상시, 반경 0.6~4.5, 따뜻한 흰색)가 주변 상시 밝힘. 씬엔 어두운 `Global Light 2D`(intensity 0.22, 차가운 밤색)로 대비 → "주변만 밝고 나머지 어둠"(Darkwood 룩). 두 라이트 모두 **모든 Sorting Layer 타겟**. 빌더: `TopDownPlayerBuilder`(PlayerLight 상시, FlashlightController 미부착·PlayerEquipment 추가), `SceneLightingBuilder`(Tools▸TopDown 2D▸Setup Scene Lighting). | 사용자 요청(이미지 레퍼). FOV 시야 콘 전까지 원형 주변광으로 분위기 확보. FlashlightController 컴포넌트는 프리팹에서 제외(클래스는 GameHUD 배터리바 호환 위해 잔존). |
| 2026-06-02 | **플레이어 라이트 2종(다크우드식): 주변 원형 + 앞 부채꼴.** 단일 원형을 2개로 분리 — ①`PlayerAmbientLight`(Point 360°, intensity 0.6, 반경 0.2~2.3, 중심 고정): 본인 바로 주변 은은하게. ②`PlayerConeLight`(Point 부채꼴 inner 35°/outer 80°, intensity 1.3, 반경 0.4~6.5): 마우스 방향으로 회전 + 원점 앞 오프셋(`lightForwardOffset` 0.45)으로 앞을 멀리·밝게. `TopDownPlayer.lightPivot`=콘(회전 대상), 원형은 고정. 콘 방향 안 맞으면 `lightAngleOffset` 조정. | 다크우드는 "주변 약한 원형 + 앞 부채꼴" 둘 다임. 부채꼴만 두면 본인 주변이 너무 깜깜. |
