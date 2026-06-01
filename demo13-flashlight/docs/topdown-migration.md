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
- [~] **1. 탑다운 토대** — URP-2D 전환, 2D 카메라(정탑다운), 새 씬에 플레이어(Rigidbody2D+Collider2D+WASD) + 마우스 페이싱. **검증**
  - [x] 전역 렌더 파이프라인 URP-3D → URP-2D 전환 (GraphicsSettings.asset + QualitySettings.asset, 2026-06-02)
  - [x] 2D 투명 정렬축 설정 (TransparencySortMode=CustomAxis, axis=(0,1,0) — Y 낮을수록 앞)
  - [x] TopDownPlayer2D.cs (Rigidbody2D+WASD+마우스 페이싱), TopDownFlashlight2D.cs 작성 완료
  - [ ] (에디터 작업) 탑다운 테스트 씬에 2D Orthographic 카메라 + Player(Rigidbody2D+Collider2D+SpriteRenderer+TopDownPlayer2D) 배치 후 이동/페이싱 **검증**
- [ ] **2. 2D 손전등** — URP 2D Spot Light, 플레이어 자식, 마우스 방향. 밤 앰비언트(글로벌 2D Light)
- [ ] **3. 맵 = Tilemap** — Unity Tilemap으로 바닥/벽 페인트 + TilemapCollider2D. (커스텀 맵빌더 대체)
- [ ] **4. 플레이어/적 탑다운화** — 기존 PlayerController/EnemyController의 이동·비주얼을 2D로 교체(로직 훅 유지). NavMesh→Rigidbody2D
- [ ] **5. 게임 로직 재연결** — 상호작용/루팅/문/NPC를 2D 트리거(Collider2D)로
- [ ] **6. 아이소 렌더 제거** — 위 🔴 목록 삭제 (검증 후)
- [ ] **7. 씬 재구성** — Safehouse/InGame을 탑다운 Tilemap으로

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
