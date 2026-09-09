# Rendering System

## 2026-09-07 방향 결정

- 질문: 현재 2D 프로젝트를 3D 쿼터뷰로 만들 수 있을까?
- 사용자 결정: 기존 프로젝트의 3D 쿼터뷰 전환을 희망하며, 캐릭터 모델·애니메이션부터 제작한다.
- 1차 산출: 치비 생존자 모델·Idle/Walk/Run + **플레이어 표시만 3D로 교체**(`ChibiPlayerVisual`). 캐릭터 제작 기준은 [char-art.md](char-art.md), 패키지는 [chibi-survivor-3d.md](chibi-survivor-3d.md).

### 전환 방향 확정 (2026-09-07 2차)

| 질문 | 결정 |
|---|---|
| 전환 강도 | **완전 3D** — XZ 평면 + Rigidbody/3D 콜라이더 + URP 3D Renderer + 실제 조명·그림자 |
| 카메라 | **고정 쿼터뷰 · 오소그래픽** (퍼스펙티브·자유회전 폐기) |
| 높이 | **단차·엄폐까지.** 바닥은 평면, 2층·계단·옥상은 이번 범위 밖 |
| 아트 | **전부 3D 재제작**(적·NPC·프랍 63종). 아이템 아이콘 204종은 UI라 유지 |

> ⚠️ **아래 본문(URP 2D)이 여전히 현 구현 상태다.** 전환은 미착수 —
> 실측·단계 계획·리스크는 **[`3d-migration.md`](3d-migration.md)** 가 SSOT.
> 전환 완료 시 이 문서를 3D 기준으로 덮어쓴다.


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

- ~~카메라 = 2D 정렬축(`CameraSortSetup`)~~ **폐기(2026-09-09)** — URP-3D 전환으로 `TransparencySortMode`는 Default(카메라 거리)다. `CameraSortSetup`은 삭제됨.
- 모든 위치/투영/그림자 계산은 **XY 기준**. (구 "XZ 바닥평면 + 90° 눕힌 쿼드" 전제는 폐기.)
- **모든 스프라이트(타일/프롭/캐릭터)는 회전 (0,0,0)** — 카메라 정면을 향하는 순수 2D. 원근은 아트가 담당하므로 트랜스폼 회전·빌보드 불필요.
- **플레이어 카메라 = PlayerRig 프리팹에 포함, 한 세트(2026-06-02 결정 변경).** ~~씬 카메라 자동 장착~~ → **카메라+플레이어+라이트+후처리(Volume)를 `Resources/PlayerRig.prefab` 한 세트로 묶어 DontDestroyOnLoad**로 모든 씬 공유. Bootstrap이 1개만 스폰. `CameraFollow`가 씬 로드 시 **자기(PlayerRig 카메라) 외 다른 Camera/AudioListener를 비활성**해 2개 충돌을 막음. (추적+셰이크/줌·후처리·피격 Volume 모두 프리팹 카메라에 내장.)
  - 빌더 `TopDownPlayerBuilder`(메뉴 `Tools ▸ TopDown ▸ Build ▸ Player Rig`)가 PlayerRig 전체를 코드 조립.
  - → 어느 씬(InGame/Safehouse/MapTool2D)에서 Play해도 동일한 카메라·조명·후처리로 동작.
  - **씬 빌더는 카메라를 안 만든다**(PlayerRig가 제공). 3D에선 깊이가 정렬을 맡으므로 별도 정렬축 설정이 없다.
  - **글로벌 Light2D의 주인 = Systems 부트 씬**(0.22 어둠). PlayerRig는 플레이어 점광(point)만 들고 오므로 글로벌 앰비언트가 없으면 URP 2D가 빛 반경 밖을 **새까맣게** 렌더 → 글로벌이 필요하지만, **게임플레이 씬(InGame/Safehouse)은 글로벌을 안 만든다**(Systems가 공급, 씬은 맵/프롭/스폰만). **자체 글로벌을 갖는 씬은 Systems + MapTool(밝게) + CombatSandbox(테스트)뿐.** 글로벌이 2개 이상 활성이면 URP가 `More than one global light on layer ...` 경고 → **런타임은 `SystemsSceneEnforcer`, 에디트 모드는 `SystemsGlobalLightEditorEnforcer`**(Systems 로드 시 그 글로벌만 남기고 나머지 비활성)가 중복을 막는다.

## 조명 / 가시성 (시야 FOV) — 2026-06-02 전환

> **손전등(주광원) 개념 폐기 → 좀보이드식 시야(FOV).** 기존 손전등 코드(`FlashlightController`/`FlashlightBeam` 셰이더/손전등 Light2D)는 **완전 제거 후 시야 시스템 신규 작성**.

- **가시성 모델 = 하이브리드** (확정):
  - 평소엔 적당히 보이되, **멀거나 그늘·실내·밤**은 어둑.
  - 플레이어가 **바라보는 방향 부채꼴(시야 콘) 밖의 적은 안 보임/어둑** — "어디서 적이 튀어나오나"의 긴장.
  - 가시성은 **지역/시간대별로 다름**(`WorldRegionCatalog`/`RegionTimeManager`/`DayNightCycle` 연동).
- **시야 콘 구현 방향**: 플레이어 facing(마우스 방향) 기준 부채꼴 Light2D(또는 시야 마스크). `TopDownPlayer.FacingDirection`으로 회전. 적 가시성은 시야 콘 안/밖 판정으로 sprite 알파·표시 토글.
- **글로벌 Light2D**: 앰비언트(밤·실내는 어둑, 낮·야외는 밝게). 하이브리드라 intensity는 지역/시간대별 가변.
- **짙은 현상 ≠ 낮/밤 (✅ 2026-06-04, 풀스크린 오버레이)**: 짙은현상([gdd-core §5.1](gdd-core.md))은 **시간 무관** 빛·공간 침식("어두움 = 시간대가 아니라 침식 정도", 낮에도 밤처럼). **낮/밤과 완전 독립**으로 구현:
  - **`DenseAnomalyController`**(싱글톤) → 카메라 앞 **풀스크린 fog 쿼드**(`BRB/AnomalyFog`) 생성. 화면 위에 어둠+안개를 알파로 덧씌움 → `DayNightCycle`의 글로벌 Light2D를 **안 건드림**(충돌 0, 최종 화면 = 낮밤 × 현상). **낮에도 밤처럼** 어두워짐.
  - 셰이더: **노이즈 주도 패치 안개**(2겹 fbm 드리프트, 월드 앵커) — 방사형 원이 아니라 불규칙 결이라 "동그란 vignette"로 안 보임. 플레이어(`_ClearCenter`) 주변 소프트 클리어를 **노이즈로 경계 깸**(원 안 보이게) + 멀수록 살짝 더. **어두운 톤**(밝은 원반 X) + 미세 밝은 wisp 결(밤엔 움직임으로 인지). `_Density`로 강도, `_FogColor`/`_HazeStrength`/`_NearClear`/`_FarFull`/`_NoiseScale` 튜닝. ⚠️ 옛 방사형 거리 falloff(`_ClearRadius`·edge항·`_EdgeDark`)는 원/도넛 링으로 보여 폐기.
  - 강도 = 수동(`SetIntensity`/이벤트) **max** 지역 기본 침식도(`WorldRegionCatalog.anomaly` 0~1, 5지역 그라데이션: 폐상가 0.1·침묵생활 0.2·묻힌정비창 0.4·기억의극장 0.65·중앙영야 0.95). 부드럽게 lerp. 테스트 토글키 `G`.
  - 범위 = **지역/씬 단위**(활성 지역 anomaly) **+ 구간(`AnomalyZone`)**: 트리거 영역에 플레이어가 들어가면 그 강도로 현상 발동 → 전체 맵이 아니라 골목·건물 등 **구간별 지정**(겹치면 max, `DenseAnomalyController._zones`). 메뉴 `Tools▸TopDown▸Map▸Create Anomaly Zone`(6×6 트리거 생성).
  - 참고(레거시): `WeatherData`의 fog는 `RenderSettings.fog`(3D)라 2D 미적용=死, `PostProcessController.Anomaly`는 후처리 룩(시간에 묶임)이라 현상과 별개.
- **라이트 모양 = 텍스쳐 쿠키(Sprite 라이트).** 플레이어 주변광/콘·프롭 발광 모두 **절차 생성 쿠키**(`LightCookieGenerator` → `Resources/LightCookies/cookie_radial`·`cookie_cone`)를 **Sprite Light2D**에 물려 부드럽게(다크우드). Point/Spot 파라미터 콘(딱딱한 경계)은 폐기. 쿠키는 흰색+알파(모양), 빛 색은 `Light2D.color`로 틴트. 콘 쿠키는 +Y 기준·피벗 하단(꼭지)이라 `lightAngleOffset=-90`로 facing에 맞춤. 프롭 발광은 Prop2D 카탈로그(`emitsLight`)로 프롭별 설정(램프/창문/네온), `lightNightOnly`면 밤에만(`PropLight2D`+DayNightCycle).
- **그림자/차폐**: 벽·구조물에 **ShadowCaster2D** → 시야 콘과 빛을 막음(벽 뒤는 안 보임). (3D 스팟라이트 + ShadowsOnly 박스 방식 폐기.)
- 낮/밤 반응 컴포넌트(`DayNightCycle.OnPhaseChanged` 구독): `PostProcessController`, `NeonSign`, `RainController` 등. **Editor State Preservation** 규칙 유지 — `Start()`에서 값을 적용하지 않고 이벤트로만 변경.
- **랜턴 (장비 기반 시야 강화) — 2026-06-05**: 시야(주변광·콘)의 **크기·밝기를 착용 광원 장비로 가변**.
  - **기본(랜턴 미착용)**: 좁은 주변광(원형 쿠키, ~2칸) + 약한/없는 콘 — 밤엔 코앞만 보임.
  - **랜턴 착용**: 주변광 반경↑·밝기↑(예: 4~5칸) + **콘 쿠키 길이·각도·밝기↑**(facing 부채꼴). 주변광·콘 둘 다 동시 강화.
  - **장비 슬롯**: 랜턴 = 착용 장비(장비창 1슬롯). **토글 아님 — 차고 있으면 상시 점등**(구 손전등 F토글/단일 빔 메커니즘 폐기 계승).
  - **등급**: 약한 랜턴 → 밝은 랜턴(반경·콘·밝기 차등) = 시야 성장 축.
  - **3D 구현(2026-09-09)**: `WornLamp`(PlayerRig의 `PlayerLamp3D`). **점광이 아니라 스포트**다 — 점광은 사방을 고르게 비춰 완전한 원을 만들고, 그러면 몸에 단 등이 아니라 **머리 위에 떠 있는 전등**으로 읽힌다(사용자 지적). 바라보는 방향(`FacingDirection`)을 향하고 34° 아래로 숙여 발 앞을 비춘다. **그림자를 켠다** — 2D의 `ShadowCaster2D`가 하던 "벽에서 빛이 끊긴다"를 대신한다. 끄면 빛이 건물 벽을 통과해 골목 밖까지 새고, 그것도 "공중에 뜬 조명" 느낌의 원인이다. 값: 사거리 5.5m · 콘 96° · 밤 1.5 / 낮 0.2(토글 없음, 밝기만 낮밤에 반응). 등급은 `SetGrade(반경배율, 밝기배율)`로 들어온다.
  - **구현**: 착용 랜턴 등급 파라미터로 `TopDownPlayer`의 주변광/콘 `Light2D`(쿠키) `radius/intensity/cone angle`을 세팅(LanternModifier). 미착용 시 기본값으로 복귀.
  - **연료(옵션, 추후)**: 기름·배터리 소모형으로 자원 압박을 줄지 추후 결정(현재 가안 = 착용 패시브 상시 점등).
  - **현상 예고 점멸 (✅ 2026-06-08 기획)**: 짙은 현상 구간 접근 시 착용 랜턴 `Light2D` intensity **펄스**(가까울수록 빠름). 별도 `phenom_meter` 아이템 **폐기**. `DenseAnomalyController`/구간 거리 → `LanternModifier`. S-013·S-020 튜토리얼. [→ navigation.md §4, story-script.md S-013]
  - **빛 = 노출(추후)**: 밝을수록 멀리·넓게 보지만 적에게도 들키기 쉬운 트레이드오프 여지(§적 은신). 추후.

> ⚠️ 데모 폴더명 `demo13-flashlight`는 역사적 이름. **손전등(F토글 단일 빔) 메커니즘은 폐기** — 빛은 시야 FOV + **착용 랜턴**(위)으로 대체.

### FOV 시야콘 구현 1차 (2026-06-19)
> **결정: 좀보이드식 시야콘 — 정면 부채꼴 + 근접 360° 밖의 적은 안 보임(몸체 렌더러 숨김). AI·충돌은 유지(적은 존재하되 안 보일 뿐).**

- **`PlayerVision`**(신규, 부팅 자가생성): 매 프레임(LateUpdate) `EnemyController.All` 순회 → 가시성 판정 → `EnemyController.SetVisionVisible(bool)`(spriteRenderer + HP바 토글). 안전구역은 적이 없어 무영향.
- **판정**: `사거리 안 && (근접반경 안 || 콘각도 안) && (LOS 켜졌으면 벽 안 막힘)`. 콘은 `TopDownPlayer.FacingDirection`(마우스) 기준. LOS = `Physics2D.Linecast`(Player/Enemy/IgnoreRaycast 제외, 트리거 무시 → 솔리드 벽만 차단).
- **튜닝(GameTuning)**: `visionEnabled`(킬스위치) / `visionFovDegrees`(150) / `visionRange`(9) / `visionNearRadius`(2.2) / `visionLineOfSight`(true). Control Panel에서 조절.
- **2026-06-05 '항상 은신 + 말풍선' 실험 정리**: FOV가 가시성 권위가 되며 `EnemySpeechBubble.Enabled` **기본 OFF**(둘 다 bodySprite를 만져 동시 ON 금지). 말풍선을 off-screen 단서로 FOV와 **결합**하려면 EnemySpeechBubble의 body-hide를 떼고 PlayerVision에만 맡기는 방향(후속 옵션).
- **⚠️ 미완(다음 FOV 단계)**: 시각적 어둠 오버레이(콘 밖 어둑/포그 — 현재는 적 몸체만 숨김, 화면 자체는 안 어두움). 지역/시간대별 가시성 가변. 착용 랜턴(Light2D) 연동.

### 적 은신 + 머리 위 말풍선 (2026-06-05, 실험 토글)

> 시야 콘을 "적을 드러내는" 장치로 쓰지 않고, **적은 항상 안 보이게(몸체 숨김) 하되 "말할 때"만 머리 위 말풍선으로 존재를 흘리는** 방향 실험.

- **몸체 = 항상 숨김**: 적(밴딧/몬스터) `EnemySprite` 렌더러를 끔 → 플레이어는 적 몸을 못 봄.
- **말풍선 = "말할 때만"**: 콘 안/밖과 무관하게 적이 말하는 순간에만 머리 위에 뜸. **플레이어를 "만나는"(발견 = 순찰→추격) 순간에만 또렷한 대사**(`encounterLines`), 그 외 평소엔 **순찰 중 근접 혼잣말 중얼거림**(`mutterLines`, 멀면 침묵 → 위치 노출 방지)만. 공격/피격/스턴/사망 전용 대사는 두지 않음.
- **내용 = 콘 의존**: 플레이어 시야 콘 **안**이면 **대사 전문**, **밖**이면 **"..."** 만. 말하는 도중 콘 안/밖이 바뀌면 실시간 갱신.
- **on/off 토글**: 전역 `EnemySpeechBubble.Enabled`(기본 ON). 디버그 키 **B**(DebugTestUI가 단독 폴링) + 전투 탭 버튼. OFF면 몸체 보이고 말풍선 끔.
- **구현**: `EnemySpeechBubble`(적별 컴포넌트, `EnemyController.Awake`가 자동 부착 → 프리팹/씬 수정 불필요). 콘 판정은 **게임플레이용 별도 파라미터**(`ConeHalfAngleDeg=45`, `ConeRange=8`)로 `TopDownPlayer.FacingDirection` 기준 — 실제 라이트 렌더(콘 쿠키)와 분리. 대사는 컴포넌트 SerializeField 풀(데이터화, 적별 교체 가능; 추후 `UnitStatData`/`NPCData`로 이관 여지). 한글은 프로젝트 표준 `LegacyRuntime.ttf`(`TextMesh`+동적폰트 머티리얼).
- ⚠️ 미구현/후속: 벽 차폐(LoS) 미적용 — 콘 안이면 벽 너머도 전문 표시됨, 추후 ShadowCaster/레이캐스트 보강. 콘 판정값을 실제 콘 라이트 각도와 동기화할지 추후 결정. HP/그로기 바는 은신과 무관하게 유지(피격 시에만 노출).

## 맵 / 프롭

- **바닥·벽 = Unity Tilemap**. 벽 타일맵에 `TilemapCollider2D` + `CompositeCollider2D`(Static Rigidbody2D)로 이동 차단. 씬 골격은 `GameSceneBuilder`(에디터 메뉴)가 생성.
- **프롭 = Prop2D 카탈로그**(개별 스프라이트 + Collider2D). 막힘(못 가는 곳) = 비-트리거 Collider2D. walkability 그리드/NavMesh 없음.
  - `Prop2DDefinition`(SO): 스프라이트·머티리얼·sortingOffset + **콜라이더 모드**(None/Box/Polygon/Composite, 프롭마다 선택).
  - `Prop2DBuilder`(static): 정의→GameObject 런타임·에디터 공용 생성. Polygon은 `sprite.GetPhysicsShape`로 외곽선 자동.
  - 맵 도구는 [`map-tool.md`](map-tool.md) 참고.

## 캐릭터 렌더 (Spine 제거됨, 2026-09-07)

> **현 상태 = 진실.** 플레이어 비주얼 = **3D 치비 모델**(`ChibiPlayerVisual`). **Spine은 완전 제거됨.**

- **Spine 제거(2026-09-07)**: Unity 6.6(6000.6) 업그레이드에서 Spine 4.2 소스가 `Object.GetInstanceID()`
  obsolete-as-error(CS0619)로 컴파일 불가. 3D 전환 계획상 어차피 제거 대상이라 앞당겨 들어냄.
  삭제: `Assets/Spine`(689파일) · `Assets/Resources/Charater`(cha) · `Editor/SpinePlayerSetup.cs` ·
  `Shaders/SpineLitURP.shader` · `PlayerRig.prefab`의 `PlayerSpine` 노드 · asmdef 참조.
- **현재 플레이어 표시**: `TopDownPlayer.character3DPrefab`이 물려 있으면 `ChibiPlayerVisual`이 3D 모델을
  띄우고 SpriteRenderer를 끈다. 없으면 스프라이트 → 그레이박스 몸통 순으로 폴백.
- ⚠️ **이월된 한계**: `HitFlash`(피격 흰 플래시)·`InjuryVFX`(통증 깜빡임)가 **SpriteRenderer 바디를 가정**한다.
  3D 표시에선 그 SpriteRenderer가 꺼져 있어 **플레이어 몸에 피격 연출이 안 보인다**(화면 효과는 정상).
  Spine 시절과 같은 구멍이 그대로 넘어옴 → 3D 전환 Stage 4에서 메시 렌더러 기준으로 재배선 필요.
- ⚠️ **미제작 모션**: 3D 캐릭터는 idle/walk/run 3종뿐. **공격·피격·사망·앉기 없음.**
  구 Spine 구동부가 갖고 있던 상태→모션 매핑(구르기=`roll`, 약/강공격=`attack`, 앉기=`sit`/`sit_walk`,
  이동=`walk`/`run`, 정지=`idle`)은 3D 애니메이터로 다시 구현해야 한다 → `3d-migration.md` Stage 4.

## 셰이더 (네임스페이스 `BRB/`)

URP 2D 렌더러는 `Tags{ "LightMode"="Universal2D" }` 패스만 그린다. 유지 셰이더는 모두 Universal2D 패스 보유(2026-06-02).

**맵 (사용 중)** — 픽셀화 + 색단계 + Light2D. (픽셀 외곽선 기능은 2026-06-02 전부 제거)
| 셰이더 | 용도 | 조명 |
|---|---|---|
| `BRB/FloorPixel` | 바닥(타일) — 불투명·컷아웃 없음, 정점컬러 | Light2D 반응 |
| `BRB/WallPixel` | 벽 — 알파 컷아웃 + 문 흰색뚫기 `_WHITE_CUTOUT` + 오클루전 페이드 `_Alpha`. `_SHADOW_MODE`는 `GroundShadow2D`(정적 발밑)가 재사용 | Light2D 반응 / 그림자모드 Unlit |
| `BRB/PropPixel` | 프롭·오브젝트 — 알파 컷아웃 | Light2D 반응 |
| `BRB/DecalPixel` | 데칼(바닥 오버레이) — 핏자국·그을음·발자국·금·포스터·발광. Transparent 큐, sortingOrder로 앞뒤. 블렌드 프리셋 Alpha/Multiply/Additive(`DecalPixelGUI` 원클릭) + `_Alpha`·정점컬러로 페이드 | Light2D 반응(토글) |
| `BRB/DamageOverlay` | 파괴/폐허 오버레이 — 베이스 위에 덧씌우는 균열·그을음(절차적, 아트 불필요). `_Damage`(0~1)로 단계별 진해짐, 베이스 알파로 마스킹. `Breakable`(부서짐)·`Weathered`(상시 낡음/녹) 둘 다 자식 오버레이로 자동 설치 → **모든 베이스 셰이더 호환** | Light2D 반응(토글) |
| `BRB/AnomalyFog` | 짙은 현상 전체화면 오버레이 — 카메라 앞 풀스크린 fog+어둠(`DenseAnomalyController`가 생성). 시간 무관 침식, 낮/밤 글로벌 라이트와 독립. 애니 노이즈+중앙 클리어+가장자리 짙음 | 언릿(화면 최상단) |

**캐릭터/이펙트 (사용 중)**
| 셰이더 | 용도 | 조명 |
|---|---|---|
| `BRB/PlayerSprite` | 플레이어/캐릭터(표준 SpriteRenderer) — 외곽선 + 최소광(어둠 가독성) + 픽셀화/색단계(옵션) + `_FlashAmount`(HitFlash 연동). 수동 시트UV 제거 | Light2D 반응 |
| `BRB/SpriteSheet` | 스프라이트 시트 UV | Light2D 반응 |
| `BRB/SpriteBillboard` | SpriteRenderer용 | Light2D 반응 |
| `BRB/SpriteFlash` | 타격감 흰 플래시(HitFlash 런타임 설치) | Light2D 반응 |

**삭제됨(2026-06-02)**: `Pixelated`(PropPixel/FloorPixel로 대체), `OcclusionOutline`(iso 잔재), `FlashlightBeam`(손전등 폐기), `ShadowProjector`(2D 미렌더). 0 참조 확인 후 제거.

- **모듈 그림자(✅ 2026-06-02, 네이티브 전환)**: 가짜 `FlatShadow`(스프라이트 복제/기울임) 폐기 → **URP 2D 네이티브 동적 캐스트 + 정적 발밑**의 2겹.
  - **① 동적 캐스트 = `ShadowCaster2D` + `Light2D` 그림자**: 엔진이 **실제로** 계산 — 물체가 빛을 막아 빛 반대편에 그림자 영역. 방향·길이는 라이트 위치가 결정, 여러 라이트 각각, 진짜 3D식(좀보이드/다크우드). 프롭/벽에 `ShadowCaster2D`. 에디터 `[ExecuteInEditMode]` Awake가 Collider2D/SpriteRenderer로 shadow shape 자동 설정→프리팹 직렬화. **플레이어 `PlayerConeLight`+`PlayerAmbientLight`에 `shadowsEnabled`+`shadowIntensity=1`**(빌더, 완전 차단).
    - **빛 차단량 = 라이트의 `shadowIntensity`(전역, 0~1)**. 1=벽 너머 완전 차단, 0.75=25% 샘. ⚠️ **Global Light2D는 그림자 무시**(전체 균일 조명) → 벽 뒤 어둡게 하려면 글로벌 intensity 낮게.
    - **프롭별 빛 통과 제어**(`Prop2DDefinition`): `shadowCasting`(Casting Option) = CastShadow(완전차단)/NoShadow(완전통과)/SelfShadow 등 + `shadowAlphaCutoff`(스프라이트 알파가 이보다 낮으면 그림자 안 만듦 = **창문/반투명으로 빛 통과**, 값↑=더 통과). ⚠️ **사물별 "부분(%)" 차단은 URP 2D 미지원**(shadowIntensity는 라이트당) — on/off + 알파(창문 구멍)로 표현.
  - **② 정적 발밑 접지 = `GroundShadow2D`**: 물체 바로 밑에 항상 깔리는 어두운 실루엣(빛 무관 고정) → 빛 없는/밝은 곳에서도 안 떠 보임. 자식 SpriteRenderer/Mesh 복제 + `WallPixel(_SHADOW_MODE)`(shear 0, dir 0) 어둡게, `offsetY`만큼 아래, `sortingOrder−2`. `[ExecuteAlways]`(에디터 미리보기), 자식은 `HideFlags.DontSave`.
    - **에디터 라이브 동기화(✅ 2026-06-03)**: 씬에서 베이스 SpriteRenderer(스프라이트·flip·Tiled size·정렬·Cutoff)를 편집하면 `Update()`가 **변경 감지 시에만** 그림자 자식을 인플레이스로 재동기화 → 맵툴 배치본뿐 아니라 **씬에서 손수 수정한 벽**도 그림자가 따라옴. (런타임 비용 0 — 에디트 모드 전용.)
  - **부착**: `Prop2DDefinition.castShadow` ON → `Prop2DBuilder`가 ShadowCaster2D + GroundShadow2D 둘 다 부착. 카탈로그는 `Wall`·`Prop` 기본 ON(바닥/마커 OFF).
  - **왜 네이티브?**: FlatShadow는 탑다운(위에서 봄)에서 스프라이트 기울이면 "쓰러지는" 가짜라 진짜 3D 그림자가 안 됨(쌍둥이/스냅/플립 등 한계). ShadowCaster2D가 정석.
  - **삭제**: `FlatShadow.cs`/`FlatShadowDirector.cs`/`FlatShadowEditor.cs`, `BRB/ShadowProjector`(2D 미렌더). `WallPixel(_SHADOW_MODE)`은 GroundShadow2D가 정적 발밑에만 재사용.
- **데칼(✅ 2026-06-03)**: 전용 **`BRB/DecalPixel`**(프롭/바닥과 별개). 바닥 위에 깔리는 오버레이 — 핏자국·그을음·발자국·금·포스터·이상현상 발광 등. `Transparent` 큐, 앞뒤는 `sortingOrder`(보통 바닥 위·프롭 아래). 픽셀화/색단계는 BRB 공통, 소프트 가장자리(`_Cutoff` 기본 0).
  - **블렌드 프리셋(머티리얼별)** — `DecalPixelGUI` 인스펙터 상단 드롭다운 원클릭(`_SrcBlend`/`_DstBlend`/`_MULTIPLY_ON` 한 번에):
    - **Alpha**(기본): 일반 스프라이트 데칼(핏자국·발자국·포스터). Light2D 반응(바닥과 같은 빛).
    - **Multiply**: 얼룩·그을음 — 바닥을 어둡게 물들임. 알파 인식(알파 낮은 곳=흰색=변화 없음). 언릿(바닥의 빛을 곱함). `Blend DstColor Zero`.
    - **Additive**: 발광 데칼(룬·이상현상) — 밝게 더함. 보통 Light2D OFF. `Blend SrcAlpha One`.
    - **Screen**: 검정 배경 밝은 오염을 부드럽게 더함(Additive보다 덜 과함). `Blend OneMinusDstColor One`.
  - **마스크 모드(✅ 2026-06-03, 낡음/녹/폐허)** — `_MASK_MODE` 토글: 텍스처를 **흑백(밝기=세기)** 으로만 쓰고 색은 `_Color`(Tint)로 결정. 검정 배경=효과 없음. **회색조 텍스처 1장으로 녹·이끼·그을음·물때를 만들고 Tint 색만 바꿔 재활용**(에셋 최소·리컬러 자유). 권장 조합 = **Multiply**(바닥/벽을 그 색으로 물들임). 작화 컨벤션 3종 중 **①흑백 마스크+셰이더 틴트**를 표준으로 채택(②컬러 베이크+Alpha, ③검정 배경+Additive/Screen는 그대로 지원).
  - **페이드**: `_Alpha`(머티리얼) 또는 `SpriteRenderer.color.a`(인스턴스)로 시간 경과 사라짐(핏자국 마름 등). `_LIT_ON` 토글로 조명 반응 on/off.
  - ⚠️ 부착/배치 자동화는 아직 없음(머티리얼만). 런타임 스폰(피격 위치 핏자국 등)은 추후 데칼 매니저로.
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
| 2026-06-03 | **데칼 전용 셰이더 `BRB/DecalPixel` 신설(별개).** 바닥 오버레이(핏자국·그을음·발자국·금·포스터·발광). Transparent 큐 + sortingOrder. 블렌드 프리셋 Alpha/Multiply/Additive를 `DecalPixelGUI` 인스펙터 드롭다운으로 원클릭(`_SrcBlend`/`_DstBlend`/`_MULTIPLY_ON` 동시 세팅). Multiply는 알파 인식(흰색 lerp) 언릿, Additive는 보통 언릿. `_Alpha`+정점컬러로 페이드, `_LIT_ON` 토글. 픽셀화/색단계 BRB 공통. | 사용자 요청("데칼용 셰이더 별개로 필요"). 프롭/바닥과 분리해 블렌드·페이드·발광을 데칼 전용으로. 배치 자동화/런타임 스포너는 추후. |
| 2026-06-04 | **짙은 현상 = 낮/밤과 독립 레이어 구현(풀스크린 fog 오버레이).** `DenseAnomalyController`(싱글톤)가 카메라 앞 풀스크린 쿼드(`BRB/AnomalyFog`)로 화면에 어둠+안개를 덧씌움 → `DayNightCycle` 글로벌 Light2D 안 건드림(충돌 0, "낮에도 밤처럼"). 애니 노이즈+중앙 클리어 버블+가장자리 짙음, 월드 앵커. 강도 = 수동(`SetIntensity`)·이벤트 max 지역 침식도(`WorldRegionCatalog.anomaly` 신규 필드, 성역 0.7/중앙영야 0.9 등). 범위=지역/씬 단위, 테스트키 G. | 사용자 결정: 짙은현상은 시간 무관(낮밤과 별개) fog+어둠. 글로벌 라이트와 충돌 피하려 화면 오버레이(곱 합성)로. gdd-core §5.1. |
| 2026-06-04 | **플레이어 셰이더 `BRB/PlayerSprite` 재작성.** 옛 버전은 수동 스프라이트시트 UV(`_Columns/_Rows/_CurrentFrame`)라 일반 SpriteRenderer에 쓰면 텍스처 한 칸만 잘려 사실상 못 씀 → **표준 SpriteRenderer**(단일 스프라이트·유니티 애니 호환)로 재작성. Light2D 2D 패스 + 외곽선(어둠 속 가독성) + `_MinLight`(어두워도 최소 가시) + 픽셀화/색단계(옵션·기본 OFF) + `_FlashColor/_FlashAmount`(HitFlash가 SpriteFlash 교체 없이 그대로 사용). 정점색 곱(틴트/페이드). 메뉴 `Tools▸TopDown▸Setup▸Create & Assign Player Material`로 머티리얼 생성+PlayerRig 프리팹 PlayerSprite에 적용. | 플레이어가 전용 셰이더 없이(또는 깨진 시트UV로) 렌더돼 "셰이더 없음" 상태였음. |
| 2026-06-03 | **바닥 타일 반복 깨기 `FloorPixel._BREAKUP_ON`.** 월드 좌표 fbm 노이즈로 바닥 명암을 변주 → 같은 타일이 깔려도 격자 반복감이 사라짐(`_BreakupScale`/`_BreakupAmount`). 화면이 아닌 **월드 기준**이라 카메라 이동에도 안정. 2D 패스에 worldXY 배리잉 추가. + 데칼 스캐터 브러시(`DecalScatterBrush`)·가장자리 자동 디테일은 [`map-tool.md`](map-tool.md). | 통짜로 그린 참조 맵의 불규칙 바닥을 Tilemap에서도. 스플랫맵은 무거워 제외, 노이즈+데칼 조합 채택. |
| 2026-06-03 | **상시 풍화 `Weathered` 신설 — 이미지 0장 낡음/녹/폐허.** 부서짐과 별개로, 부서지지 않아도 항상 절차적 녹·그을음을 덧씌우는 컴포넌트(`BRB/DamageOverlay` 재사용, `_Damage` 고정 + `tint`). 색만 바꿔 녹(주황갈)/이끼(초록)/그을음(검정)/물때(갈색). 카탈로그 "Aged · 낡음/녹/폐허" 섹션(`Prop2DDefinition.weathered`). | 사용자가 "이미지 없이 녹슨·폐허"를 원함. 텍스처(DecalPixel 마스크) 없이도 분위기 확보 — 셰이더가 이미 색 입힘. 텍스처 마스크와 공존(나중에 업그레이드). |
| 2026-06-03 | **데칼 마스크 모드(`_MASK_MODE`) 추가 — 텍스처 기반 낡음/녹/폐허.** 사용자가 텍스처로 작화하기로 결정. 작화 컨벤션 3종(①흑백 마스크+셰이더 틴트 ②컬러 베이크+Alpha ③검정 배경+Additive/Screen) 중 **①을 표준 채택**(회색조 1장→Tint 색만 바꿔 녹·이끼·그을음 재활용). `DecalPixel`에 `_MASK_MODE`(루미넌스=세기, 색=Tint) + GUI에 Screen 프리셋 추가. 권장 = 마스크 모드 + Multiply. | 색별 텍스처를 따로 안 그려도 됨(리컬러 자유·에셋 최소). 별도 셰이더 안 만들고 데칼에 통합 — 데칼 하나로 핏자국·그을음·녹·이끼·폐허 전부. |
| 2026-06-03 | **파괴 시스템 `Breakable` + `BRB/DamageOverlay` 신설.** 상자 등을 때리면 1→2→3 단계로 부서지고 마지막에 파괴(파편). 손상 비주얼 = 베이스 위 **자식 오버레이**(균열·그을음 절차적) → 베이스 셰이더 안 건드림 = **모든 셰이더 호환**. Health 있으면 자동 연동, 없으면 `Hit()`/`ApplyDamage()`. 카탈로그(`Prop2DDefinition.breakable`)에서 프롭별 ON. 상세 [`destructible.md`](destructible.md). | 사용자 요청("모든 셰이더에 같이 쓸 수 있게 부서짐/폐허 효과, 단계별로"). 오버레이 방식이라 셰이더별 수정 불필요. 절차적이라 아트 없이도 동작. |
| 2026-06-03 | **글로벌 Light2D 주인 = Systems 씬으로 확정 + 에디터 중복 경고 해결.** "글로벌은 씬이 둔다"(이전 메모) 정정 → **Systems 부트 씬이 글로벌을 단독 소유**, 게임플레이 씬(InGame/Safehouse) 빌더는 글로벌을 안 만듦(이미 그러함). 자체 글로벌은 Systems + MapTool + CombatSandbox(단독 실행용)만. 글로벌 2개↑ 활성 시 URP `More than one global light on layer ...` 경고 → 런타임 `SystemsSceneEnforcer`에 더해 **에디트 모드용 `SystemsGlobalLightEditorEnforcer`(신규, `[InitializeOnLoad]`)** 추가: Systems 로드 시 그 글로벌만 남기고 다른 씬 글로벌을 에디터에서도 비활성(Systems 언로드 시 복구, 강제 저장 안 함). | 에디터에서 Systems+게임플레이/샌드박스 씬을 함께 열면 enforcer(런타임 전용)가 안 돌아 씬뷰 repaint마다 경고. PlayerRig는 점광만 가져오므로 글로벌 owner는 PlayerRig가 아니라 Systems 씬. |
| 2026-06-04 | **텍스쳐(쿠키) 라이트 전환 — 플레이어 + 프롭 발광.** Point/Spot 파라미터 라이트의 딱딱한 콘 → URP 2D **Sprite 라이트 + 절차적 쿠키**(부드러운 그라데이션, 다크우드 룩). `LightCookieGenerator`(Tools▸TopDown▸Map▸Generate Light Cookies)가 `Resources/LightCookies/`에 cookie_radial(중앙 피벗)·cookie_cone(+Y·하단 피벗=꼭지) PNG 절차 생성(흰색+알파, 색은 Light2D.color, PPU=256→1유닛·스케일로 크기). PlayerRig 빌더: 주변광=라디얼·콘=콘 쿠키로 교체(회전·그림자 유지, lightAngleOffset=-90로 +Y→facing). 프롭=Prop2D 카탈로그 통합: `Prop2DDefinition`에 emitsLight/shape/color/intensity/radius/offset/angle/castsShadows/nightOnly 추가, `Prop2DBuilder.ApplyLight`가 Sprite Light2D 자식 부착(쿠키·정렬레이어는 URP 공개 setter 없어 reflection으로, 넣은 뒤 enable 토글로 메시 갱신), 밤전용은 `PropLight2D`(DayNightCycle 연동). | 사용자: 콘이 딱딱해 다크우드 느낌 안 남 + 램프/창문 발광 '기능' 필요. 텍스쳐 라이트가 2D 분위기 조명 표준. 절차 생성이라 무에셋·튜닝·스왑(직접 그린 PNG로 교체) 가능. |
| 2026-06-05 | **적 은신 + 머리 위 말풍선(실험 토글).** 시야 콘이 적을 "드러내는" 대신, 적 몸체(`EnemySprite`)는 항상 숨기고 적이 "말할 때"만 머리 위 말풍선. 콘 안=대사 전문 / 콘 밖="..."(말하는 중 실시간 갱신). 플레이어를 "만나는"(발견=순찰→추격) 순간에만 또렷한 대사, 그 외 평소엔 순찰 중 근접 혼잣말 중얼거림만(공격/피격/스턴/사망 전용 대사 없음 — 2026-06-05 결정). 전역 `EnemySpeechBubble.Enabled`(기본 ON, 디버그 B키/전투탭 토글). `EnemyController.Awake` 자동 부착, 콘 판정은 게임플레이용 별도 파라미터(라이트 렌더와 분리). | 사용자 요청: 적을 못 보게 하고 말풍선으로만 존재를 흘리는 긴장감. 콘은 "무슨 말인지 알아듣는" 범위로 의미. 나중에 on/off 원함. 벽 차폐(LoS)는 후속. |
| 2026-06-04 | **건물 개념 변경 — 천장 컷어웨이 폐기 → 건물=씬 전환.** "천장 없음 + 건물은 무조건 전환 이벤트" 확정 → 천장 시스템(Ceiling 카테고리·`CeilingFader`·반투명 토글·트리거 크기/오프셋·`gb_roof`/ceiling 에셋) **전면 삭제**. 건물 = **프롭 + `Function.Trigger`**(영역 밟으면 `MapTriggerZone2D`→`SceneTransitionManager.TransitionTo` 자동 전환). 입구가 80° 틸트 밑둥에 있을 수 있어 Trigger에 `triggerOffset` 추가(영역을 입구로 이동). 천장 컷어웨이 관련 항목들은 폐기됨(아래 천장 로그는 히스토리). | 사용자 결정: 실내를 같은 화면에 보여주는 컷어웨이 대신, 건물 진입=별도 씬 전환(타르코프식)으로 단순화. |
| 2026-06-04 | _(폐기)_ **천장 컷어웨이 트리거 크기/오프셋 override.** 80° 틸트 아트라 건물 **밑둥(앞면)이 아래로 길어** 입구로 들어와도 지붕 footprint 트리거 밖이라 페이드가 늦음 → `Prop2DDefinition.ceilingTriggerSize`(0,0=footprint 자동)·`ceilingTriggerOffset` 추가. 세로를 키우거나 Y-오프셋을 음수로 내려 입구/밑둥까지 덮으면 **진입 즉시 페이드**. `CeilingFader.OnDrawGizmosSelected`가 트리거 영역을 청록 박스로 표시(시각 튜닝). 배치본의 BoxCollider2D를 씬에서 직접 늘려도 됨. | 사용자: 밑둥이 길어 입구 진입 시 바로 천장 투명 원함. |
| 2026-06-03 | **천장(지붕) 컷어웨이 시스템 신설.** 옛 3D `BuildingInterior 알파 페이드`는 탑다운 전환 때 삭제됐고 현재 없음 → 새로 구축. **새 `Ceiling` 카테고리**(카탈로그 천장 탭, enum 끝에 추가, ID `ceiling_`): 콜라이더 None(막힘X)·그림자 OFF·**최상단 정렬(`Ceiling` Sorting Layer)**. 빌더가 천장 프롭에 **트리거 콜라이더(스프라이트/타일 footprint)** + `CeilingFader` 자동 부착. 동작: 플레이어가 건물 안(트리거)에 들어오면 지붕 알파 **1→0 부드럽게 페이드아웃**, 나가면 복귀. **건물 단위 그룹화**(`ceilingGroupId` 같은 조각들이 한꺼번에 페이드 — 한 조각 트리거에만 들어와도 그룹 전체). 정렬은 데칼(엔티티 아래)과 정반대(엔티티 위)라 전용 레이어. | 사용자 결정(질문 3): 새 천장 탭 / 진입 시 부드러운 페이드아웃 / 건물 단위. 좀보이드·타르코프식 실내 진입 가시성. FOV "실내 어둑"과 상보적(추후 연동). |
| 2026-06-18 | **Spine-Unity 런타임 4.2 고정(다운그레이드).** 캐릭터 에셋 `cha`(`Assets/Resources/Charater/cha.json·atlas·png`)가 Spine 4.2.43 익스포트인데 프로젝트엔 4.3.81 런타임 → `Data version 4.2.43 / Required 4.3` 로드 에러. **데이터 재익스포트 대신 런타임을 공식 `spine-unity 4.2.120`(`spine-unity-4.2-2026-05-29.unitypackage`)로 다운그레이드**해 맞춤. `Assets/Spine`·`Assets/Spine Examples` 전체 교체(asmdef GUID 동일→참조 유지). **앞으로 Spine 익스포트는 4.2 타깃 유지.** 선택지: ⓐ 데이터 4.3 재익스포트 vs ⓑ 런타임 4.2 다운그레이드 → **ⓑ 채택**. | `cha` 데이터 버전을 못 바꾸는 상황이라 런타임을 데이터에 맞춤. |
| 2026-06-18 | **플레이어 비주얼 = 단일 SpriteRenderer → Spine 스켈레톤(`cha`).** `PlayerRig.prefab`에 `PlayerSpine`(SkeletonAnimation) 자식 추가, 기존 `PlayerSprite`는 렌더러만 끔(오브젝트 유지). `TopDownPlayer`가 이동/전투 상태로 Spine 애니 구동(걷기=walk·달리기=run·약/강공격=attack·구르기=roll; `idle` 없어 정지 시 셋업 포즈), 좌우 플립=Skeleton.ScaleX 부호. 적용 메뉴 `Tools/TopDown/초기설정/Spine 플레이어 적용 (cha)`. **알려진 한계(후속)**: `BRB/SpineLitURP`에 flash 프로퍼티 없어 HitFlash 흰 플래시·InjuryVFX 통증 깜빡임이 플레이어 바디엔 미표시(화면 효과는 정상) → 셰이더에 flash 지원 추가 필요. | CLAUDE.md 명시 원래 방향(2D 스프라이트 + Spine animation)대로 플레이어를 Spine으로. |
| 2026-06-05 | **랜턴(장비 기반 시야 강화) 도입.** 손전등 F토글/단일 빔 폐기 계승. 시야(주변광 원형 + facing 콘)의 **반경·각도·밝기를 착용 랜턴 등급으로 가변** — 미착용=좁은 주변광(코앞)/착용=주변광·콘 동시 확대·증광. 토글 아닌 착용 패시브(상시 점등). 연료 소모·'빛=노출' 트레이드오프는 추후 옵션. | 사용자 결정: '손전등 폐기, 랜턴 차면 라이트·콘 커지고 밝아짐'. 시야가 장비 성장 축이 됨. 구현은 PlayerRig 주변광/콘 Light2D(쿠키) 파라미터를 LanternModifier로 조절. [→ items.md 랜턴, story-script S-013/S-020] |

## 2026-07-11 — FOV 어둠 오버레이 (`VisionDarkness`)

> 사용자 지적: "적군이 제대로 안 보이던데 스프라이트 빠졌나?" → **스프라이트 정상.** `PlayerVision`이 시야콘 밖 적의 렌더러를 끄는 설계대로였는데, **주변이 밝아서 적이 그냥 사라진 것처럼** 보였다. 사용자 결정: *"시야콘이 비추지 않는 곳을 좀 더 어둡게."*

- **신규 `Combat/VisionDarkness.cs`** — 플레이어 중심 부채꼴 메시를 매 프레임 생성해 **시야콘 밖을 어두운 반투명으로 덮는다.** 자가부트 싱글턴(DontDestroyOnLoad), 맵툴 씬 제외, **안전가옥에선 자동 비활성**.
  - 콘 안(정면 ±fov/2) → `visionRange`까지 밝음 / 근접 `visionNearRadius` 안 → 각도 무관 밝음 / 그 외 → 어둠
  - 콘 경계는 `half~half+soft` 구간을 보간해 계단현상 없이 부드럽게
  - **판정 수치를 `PlayerVision`과 동일한 GameTuning 필드에서 읽는다** — 시각과 판정이 어긋나면 그게 더 큰 버그
  - Light2D를 건드리지 않는 **독립 오버레이**(Unlit 반투명, sortingOrder 100)라 글로벌 라이트·낮밤 셋업과 충돌하지 않음
- **노브**: `GameTuning.visionDarkAlpha`(기본 0.72, 0=끔) — 어둠 농도. 기존 `visionEnabled/FovDegrees/Range/NearRadius`와 함께 동작.
- ⚠️ 알려진 별건: `GameTuning.asset`에 시야 필드가 **아직 기록돼 있지 않아** 코드 기본값으로 돈다(패널에서 조절하려면 에셋에 값이 써져야 함).

## 2026-07-11 — 그레이박스 식별 라벨: "적" ↔ "시체" (`UnitLabel`)

> 질문/지적: *"시체는 시체라고 표기해서 적은 적이라고 해주고, 적이 죽으면 시체겠지?"*
> **결정**: 살아있는 적은 머리 위 **"적"**, 죽으면 같은 라벨을 **"시체"** 로 교체한다. 아트가 붙기 전까지의 임시 식별 장치.

- **신규 `Combat/UnitLabel.cs`** — 머리 위 월드 텍스트(TextMesh) 한 곳. `Attach/Set/SetVisible`.
  - 부착 위치 = **몸체 스프라이트의 자식**, 부모 스케일을 보정해 어떤 크기의 적이든 글자 크기 동일(월드 0.13)
  - 색: 적 = 연분홍 / 시체 = 회색. 사망 시 **몸체 스프라이트도 45%로 어둡게** — 라벨만 바뀌면 여전히 붉은 적처럼 보인다
- **생성 주체를 `EnemyController.Awake` 한 곳으로 통일** — 기존엔 `EnemySpawner`가 런타임 그레이박스 적에만 붙여서 **프리팹 적엔 라벨이 없었다.**
- **디버그 시체 스폰(F1)** 도 같은 라벨을 쓴다 — 사망 경로와 눈으로 비교 가능.

### 이번에 같이 잡은 두 버그
1. **라벨이 아예 안 그려짐** — 스크립트로 `AddComponent<TextMesh>()` 하면 `font`가 null이라 머티리얼이 비어 **한 글자도 렌더되지 않는다**(인스펙터로 붙일 때와 다름). `LegacyRuntime.ttf` + 그 머티리얼을 명시 지정해 해결. `EnemySpeechBubble`이 같은 이유로 이미 폰트를 명시하고 있었다.
2. **시야 밖인데 라벨만 떠서 위치 노출** — `PlayerVision`/`SetVisionVisible`은 **SpriteRenderer '컴포넌트'만** 끄므로 자식 MeshRenderer(라벨)는 안 꺼진다. `SetVisionVisible`에서 라벨도 같이 껐다 켜도록 수정. (시체는 항상 보이는 월드 오브젝트라 사망 시 강제 표시.)
