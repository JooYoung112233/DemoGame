# 탑다운 2D 전환 (아이소 렌더 제거)

> 2026-06-02 결정. 아이소메트릭 렌더링(3D 벽 큐브 + 빌보드 프랍 + 맵빌더 + 깊이정렬)을
> 전부 들어내고 **탑다운 2D**로 재구축. 게임 로직(인벤·퀘스트·스토리·경제 등)은 유지.
> **이미지는 탑다운으로 그리고, 카메라는 2D(Orthographic).** IsoTools 실험은 폐기 완료.

## 왜
- 아이소(2.5D 하이브리드)의 깊이정렬·접지·2D-in-3D 이질감을 오래 싸웠고, IsoTools 전환도 정렬 한계로 중단
- 탑다운 2D는 Unity 네이티브 2D(Sprite/Rigidbody2D/Tilemap/2D Light)로 단순·견고
- 손전등은 URP **2D Light**로 유지

## 핵심 결정
| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-06-02 | 아이소를 계속? 탑다운으로? | **탑다운 2D 전환.** 기존 아이소 렌더링 제거, 게임 로직 유지 |
| 2026-06-02 | 카메라/아트 | 카메라 2D Orthographic(정탑다운), 아트는 탑다운 스프라이트 |
| 2026-06-02 | 손전등 | URP 2D Light (Renderer2D 파이프라인 사용) |

## 유지 vs 제거 vs 신규

| 🟢 유지 (로직, 뷰 무관) | 🔴 제거 (아이소 렌더) | 🆕 신규 (탑다운 2D) |
|---|---|---|
| 인벤토리·아이템·제작·의료 | WallBuilder·BuildingRenderer (3D 큐브) | URP-2D 파이프라인 |
| 퀘스트·NPC·대화·스토리 | PropQuadBuilder·빌보드 셰이더 | 2D Orthographic 카메라(정탑다운) |
| 경제·레이드·세이브·이벤트 | BlobShadow·IsometricDepthSorter | Rigidbody2D + Collider2D 이동 |
| StatDB·데이터·아이템 | 스텐실 바닥/벽 셰이더 (City*, Ruin*, Road*) | URP **2D Light** 손전등 |
| UI(uGUI)·UIManager | MapBuilder(아이소 맵툴) | **Tilemap** 맵(바닥/벽) |
| Health·전투 상태 로직 | 플레이어/적 빌보드, FlashlightBeam(3D 콘) | 탑다운 스프라이트 |
| | OcclusionOutline·WallOcclusionOutline | TilemapCollider2D |

## 단계별 계획
- [x] **1. 탑다운 토대** — URP-2D 전환, 2D 카메라(정탑다운).
  - [x] 전역 렌더 파이프라인 URP-3D → URP-2D 전환 (GraphicsSettings.asset + QualitySettings.asset, 2026-06-02)
  - [x] 2D 투명 정렬축 설정 (TransparencySortMode=CustomAxis, axis=(0,1,0) — Y 낮을수록 앞. `CameraSortSetup`)
  - [x] `TopDownPlayer.cs` (Rigidbody2D+WASD+마우스 페이싱+손전등) — 구 TopDownPlayer2D/Flashlight2D 통합·대체. `CameraFollow`.
- [x] **2. 2D 손전등** — 플레이어 자식 Light2D, 마우스 방향. 글로벌 Light2D(밤 앰비언트). `FlashlightController`.
- [~] **3. 맵 = Tilemap** — `GameSceneBuilder`가 Grid+Floor/Walls Tilemap(+Tilemap/Composite Collider2D) 생성. 프롭은 `Prop2D` 카탈로그. 실제 맵 페인팅은 진행 중.
- [x] **4. 플레이어/적 탑다운화** — `TopDownPlayer`(신규), `EnemyController` Rigidbody2D 재작성(NavMesh 제거, HP/그로기 바 SpriteRenderer화). 로직 훅 유지.
- [~] **5. 게임 로직 재연결** — 상호작용/문/루팅을 Collider2D로. `IInteractable.Interact(GameObject)` 시그니처 전환. 21개 의존 파일 `// TODO(TopDownPlayer):`로 표시 → TopDownPlayer 재연결 진행 중.
- [x] **6. 아이소 렌더 제거** — 🔴 목록 삭제 완료(MapBuilder/3D 큐브/스텐실 셰이더/스팟라이트/오클루전/BlobShadow/IsometricDepthSorter). 아이소 식별자 0개.
- [~] **7. 씬 재구성** — `GameSceneBuilder`로 InGame/Safehouse 골격 생성기 마련. 콘텐츠 재구성 진행 중.

> **남은 일**: `Resources/TopDownPlayer` 프리팹 생성, `// TODO(TopDownPlayer):` 주석 21곳 재연결, 컴파일 정리, 씬 콘텐츠 채우기.

## 환경
- Unity 6 / URP 17.3. `URP-2D.asset`(Renderer2D) 존재 → 전환만 하면 됨. 2D 패키지(sprite/tilemap/animation, physics2D) 설치됨.
- 새 탑다운 코드는 `Assets/Scripts/TopDown/`에 (Game.Scripts asmdef 내 — 게임 로직 직접 참조 가능, asmdef 충돌 없음)

## 진행 로그
| 날짜 | 내용 |
|------|------|
| 2026-06-02 | 탑다운 전환 결정 + 계획. IsoTools 실험 잔여물 삭제 완료. |
| 2026-06-02 | 방향 확정: **완전 2D 재구축**(IsometricGrid 70° 하이앵글 3D 유지안은 폐기). |
| 2026-06-02 | 전역 파이프라인 URP-3D→URP-2D 전환 + 2D 투명정렬축 설정. MapBuilder 코드 `IsometricMapEditor`→`TopDownMapEditor`, `IsometricGrid`→`TopDownGrid` 네임스페이스/클래스 마이그레이션 완료(잔존 참조 0 확인). |
| 2026-06-02 | **2D 프롭 카탈로그 + 콜라이더 시스템 신설.** 막힘(못 가는 곳)=비-트리거 Collider2D 규칙(NavMesh/walkability 그리드 폐기). `Prop2DDefinition`(SO): 스프라이트·머티리얼·sortingOffset + **콜라이더 모드(프롭마다 선택)**: None/Box(스프라이트 크기, 배율·오프셋)/Polygon(스프라이트 physics shape 외곽선 자동, 다중 path·오목·구멍 지원)/Composite(박스 2~3개 합성). `Prop2DBuilder`(static): 정의→GameObject(SpriteRenderer+Collider2D) 런타임·에디터 공용 생성, Polygon은 `sprite.GetPhysicsShape`로 path 채움(런타임 안전). `Prop2DCatalogEditor`(EditorWindow, Tools▸TopDown 2D▸Prop Catalog): 목록·생성·삭제 + 폼 + **콜라이더 시각 미리보기 오버레이**(Box/Composite=빨강 박스, Polygon=외곽선) + Composite 박스 추가/편집. 에셋은 `Assets/Resources/Props2D/`. | 콜라이더 전략은 "프롭마다 선택"(네모=Box, 나무·바위=Polygon, 각진 구조물=Composite). 맵 바닥/벽은 Tilemap(3단계), 프롭은 개별 스프라이트+콜라이더로 분리. |
| 2026-06-02 | **iso 식별자 전면 정리 + 베이크 맵 삭제.** 클래스명 파일 `git mv` (GUID 보존): `IsometricGrid`→`TopDownGrid`, `IsometricCameraController`→`TopDownCameraController`, `IsometricSortingManager`→`TopDownSortingManager`, `IsometricDepthSorter`→`TopDownDepthSorter`, `IsometricGridTests`→`TopDownGridTests`. Scripts+Editor 전체 79개 파일 `Isometric` 토큰 0개. `CreateAssetMenu "Isometric Map/"`→`"Top-Down Map/"`. `IsoViewDir` 레거시 별칭·iso 시절 주석 제거. **`NewMap.asset`(베이크 맵 데이터) 삭제** — 씬/프리팹 참조 없음 확인. Resources/MapBuilder 카탈로그 SO들의 `m_EditorClassIdentifier` 내 옛 네임스페이스는 GUID 로딩이라 무해, Unity 리시리얼라이즈 시 자동 갱신됨. |
| 2026-06-02 | **렌더 규약 확정(필독): 80° 아트 + 2D 카메라 + 회전 0,0,0.** ①시점 틸트(약 80°)는 **아트 텍스처에 미리 그려넣음**(baked-in) — 코드/카메라로 기울이지 않음. ②카메라 = **2D 직교 뷰**(XY 평면, 회전 0). ③**타일·프롭·모든 물체는 회전 (0,0,0)으로 배치** — 스프라이트가 카메라 정면. (옛 XZ-3D 시절 `SpriteFlatRotation=Euler(90,0,0)`·카메라 틸트는 폐기, 혼동 금지.) 깊이는 sortingOrder + Y 정렬축. `Prop2DBuilder`/카탈로그/씬빌더 모두 회전 미적용으로 준수 확인. 카메라-향 빌보드 코드는 불필요(발견 시 제거). | 사용자 규약("아트 80°, 카메라 2D, 모든 물체 000"). 입체감은 아트가 담당, 트랜스폼 회전은 0 고정. |
| 2026-06-02 | **구 시스템 정리 + 전투 이식 + 3D 잔재 2D화.** (1) 3D 전용 스크립트 12종 삭제(BuildingInterior/BuildingEntryTrigger/BuildingGlow/InkWorldController/ViewCulling/BuildingSortingSetup/GroundSortingSetup/TopDownDepthSorter/FlatShadow/SkeletonAnimController/EnemyOutline/InkDissolveController) + NavMesh 씬 에셋·3D 모델. (2) `EnemyController` NavMesh→Rigidbody2D, `FlashlightController` Light→Light2D, `DoorController` Collider→Collider2D. (3) **전투 이식**: `PlayerController`(3D) 폐기 → `TopDownPlayer`(Rigidbody2D)에 약/강공·구르기·스태미너 재구현, `Physics2D.OverlapCircleAll`→`EnemyController.TakeHit`. 무적/전투차단/HUD 스태미너 연결. 상호작용 계층 `GameObject` 인터페이스 확정. `PlayerController`/`TODO(TopDownPlayer)` 잔재 0. (4) **3D primitive→SpriteRenderer**(`PlaceholderSprite`): WorldItem/NPCQuestMarker/InjuryVFX. `WorldLabelBillboard`·카메라 빌보드 제거(카메라 고정). |
