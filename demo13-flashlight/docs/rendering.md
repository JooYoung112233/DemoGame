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
- **플레이어 카메라 = PlayerRig 프리팹에 포함, 한 세트(2026-06-02 결정 변경).** ~~씬 카메라 자동 장착~~ → **카메라+플레이어+라이트+후처리(Volume)를 `Resources/PlayerRig.prefab` 한 세트로 묶어 DontDestroyOnLoad**로 모든 씬 공유. Bootstrap이 1개만 스폰. `CameraFollow`가 씬 로드 시 **자기(PlayerRig 카메라) 외 다른 Camera/AudioListener를 비활성**해 2개 충돌을 막음. (추적+셰이크/줌·후처리·피격 Volume 모두 프리팹 카메라에 내장.)
  - 빌더 `TopDownPlayerBuilder`(메뉴 Build Player Prefab)가 PlayerRig 전체를 코드 조립.
  - → 어느 씬(InGame/Safehouse/MapTool2D)에서 Play해도 동일한 카메라·조명·후처리로 동작.

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
| ~~`BRB/ShadowProjector`~~ | **삭제됨** — URP 2D에서 안 그려져서 폐기, 그림자는 `WallPixel(_SHADOW_MODE)`로 통일 | — |
| `BRB/OcclusionOutline` | 깊이 가림 외곽선 | Unlit (순수 2D에선 의미 약함) |
| ~~`BRB/FlashlightBeam`~~ | (삭제 예정 — 손전등 폐기) | — |

- **모듈 그림자(✅ 2026-06-02, 네이티브 전환)**: 가짜 `FlatShadow`(스프라이트 복제/기울임) 폐기 → **URP 2D 네이티브 동적 캐스트 + 정적 발밑**의 2겹.
  - **① 동적 캐스트 = `ShadowCaster2D` + `Light2D` 그림자**: 엔진이 **실제로** 계산 — 물체가 빛을 막아 빛 반대편에 그림자 영역. 방향·길이는 라이트 위치가 결정, 여러 라이트 각각, 진짜 3D식(좀보이드/다크우드). 프롭/벽에 `ShadowCaster2D`(castsShadows=true, selfShadows=false). 에디터 `[ExecuteInEditMode]` Awake가 Collider2D/SpriteRenderer로 shadow shape 자동 설정→프리팹 직렬화. **플레이어 `PlayerConeLight`에 `shadowsEnabled`+`shadowIntensity`** 켜야 보임(빌더에 설정).
  - **② 정적 발밑 접지 = `GroundShadow2D`**: 물체 바로 밑에 항상 깔리는 어두운 실루엣(빛 무관 고정) → 빛 없는/밝은 곳에서도 안 떠 보임. 자식 SpriteRenderer/Mesh 복제 + `WallPixel(_SHADOW_MODE)`(shear 0, dir 0) 어둡게, `offsetY`만큼 아래, `sortingOrder−2`. `[ExecuteAlways]`(에디터 미리보기), 자식은 `HideFlags.DontSave`.
  - **부착**: `Prop2DDefinition.castShadow` ON → `Prop2DBuilder`가 ShadowCaster2D + GroundShadow2D 둘 다 부착. 카탈로그는 `Wall`·`Prop` 기본 ON(바닥/마커 OFF).
  - **왜 네이티브?**: FlatShadow는 탑다운(위에서 봄)에서 스프라이트 기울이면 "쓰러지는" 가짜라 진짜 3D 그림자가 안 됨(쌍둥이/스냅/플립 등 한계). ShadowCaster2D가 정석.
  - **삭제**: `FlatShadow.cs`/`FlatShadowDirector.cs`/`FlatShadowEditor.cs`, `BRB/ShadowProjector`(2D 미렌더). `WallPixel(_SHADOW_MODE)`은 GroundShadow2D가 정적 발밑에만 재사용.
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
| 2026-06-02 | **PlayerRig 한 세트 프리팹 + 다크우드 후처리(결정 변경).** 카메라 "씬 자동 장착" → **카메라+플레이어+라이트+Volume을 `Resources/PlayerRig.prefab` 한 세트**로 묶어 DontDestroyOnLoad(Bootstrap 1개 스폰). `CameraFollow`가 씬 로드 시 다른 Camera/AudioListener 비활성(충돌 방지). 카메라 `allowHDR` + ConeLight intensity 1.6(HDR). **후처리 Volume**(`PlayerRigVolume.asset`): Bloom(threshold 0.5/intensity 1.3/scatter/warm tint) + Vignette 0.42 + ColorAdjustments(따뜻·대비) + FilmGrain — "빛이 번지는 맛". `TopDownPlayer.DontDestroyOnLoad(transform.root)`. | 사용자 결정: 3개 한 세트 일관 동작. "맛없는 빛"은 bloom 미적용(threshold 높음·HDR off)이 원인 → 프리팹에 다크우드 Volume 내장으로 모든 씬 일관. |
| 2026-06-02 | **낮/밤 2D 조명 연동 + 에디터 전환 버튼.** `DayNightCycle`이 3D `directionalLight`/`RenderSettings.fog`만 제어 → **2D `Global Light2D`(intensity·color) 제어 추가**(낮 1.0/밤 0.18, 밤=차가운 파랑). `SetNight(bool)`/`ToggleDayNight()` public(에디터·런타임 공용 — 글로벌 라이트 즉시 적용 + OnPhaseChanged + RegionTime 동기화). `DayNightCycleEditor`(커스텀 인스펙터): ☀낮/🌙밤/⟳토글 버튼 — 씬(에디터)·플레이 둘 다 즉시 미리보기. 수치는 인스펙터에서 조절. `SceneLightingBuilder`가 Global Light2D 생성 시 `DayNightCycle` 부착+연결. T키 유지. | 맵툴/테스트에서 낮↔밤을 씬·인게임 둘 다 즉석 전환·튜닝하려는 사용자 요청. 글로벌 2개(DayNightCycle+RegionTimeManager) 중 시각 적용은 DayNightCycle 담당. |
| 2026-06-02 | **프롭/벽 그림자 방향 = 플레이어 시선(콘) 반대로(`Facing` 모드).** 기존 위치 기반(`Player`/`NearestLight`)은 라이트 "위치"(=플레이어, 보통 아래) 반대로 깔려서, 콘이 위를 비추는데 그림자가 위로 가는 "빛이 아래 있는 듯" 버그. → `FlatShadow.DirMode.Facing` 신설(기본): `dir = -FlatShadowDirector.Facing`(=`TopDownPlayer.FacingDirection` 반대). 밝은 콘 쪽 반대에 그림자. 거리 길이/진하기(`proximityStretch`/`distanceFade`)는 플레이어 거리로. 위치 기반 모드도 옵션 유지. | 콘은 "위치(플레이어)"와 "비추는 방향(시선)"이 달라서, 위치 기반이면 직관과 반대. 사용자가 "콘 비추는 쪽 기준" 선택. |
| 2026-06-02 | **그림자 = URP 2D 네이티브 ShadowCaster2D로 전환(가짜 FlatShadow 폐기).** FlatShadow(스프라이트 복제/기울임)는 탑다운에서 진짜 3D 그림자가 안 됨(쌍둥이/스냅/벽 갇힘) → ① `ShadowCaster2D`+`Light2D`(동적 캐스트, 엔진 계산) ② `GroundShadow2D`(정적 발밑 접지)로 분리. Prop2DBuilder가 castShadow면 둘 다 부착. PlayerConeLight에 shadowsEnabled+shadowIntensity. `FlatShadow`/`FlatShadowDirector`/`FlatShadowEditor`·`ShadowProjector` 삭제, Prop2DDefinition 그림자 필드도 castShadow만 남김. | 사용자: "이건 3D 그림자가 아니다". 네이티브가 방향·길이·다중라이트 전부 정확. 정적 발밑은 빛 없을 때 접지감 유지용으로 별도 유지. |
