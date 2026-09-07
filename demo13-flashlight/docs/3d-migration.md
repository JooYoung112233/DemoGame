# 3D 쿼터뷰 전환 (2D → 3D)

> 2026-09-07 결정. **URP 2D Renderer / XY 평면 / Rigidbody2D**를 들어내고
> **URP 3D Renderer / XZ 평면 / Rigidbody + 실제 조명·그림자**로 재구축한다.
> 게임 로직(인벤·아이템·제작·퀘스트·NPC·스토리·경제·세이브·UI)은 **유지**한다.
>
> - 현 구현 상태의 진실은 여전히 [`rendering.md`](rendering.md) — 이 문서는 **계획**이다.
> - 전환이 끝나면 rendering.md를 3D 기준으로 덮어쓰고 이 문서는 이력으로 남긴다.
> - 이전 방향 전환(아이소 3D → 탑다운 2D, 2026-06-02) 기록은 [`topdown-migration.md`](topdown-migration.md).

## 결정

| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-09-07 | 전환 강도 — 물리·좌표계까지 바꾸나? | **완전 3D.** XZ 평면 + Rigidbody/3D 콜라이더 + URP 3D Renderer + 실제 조명·그림자. 하이브리드(3D 모델 + 2D 물리) 유지안 폐기 |
| 2026-09-07 | 카메라 | **고정 쿼터뷰 · 오소그래픽.** 각도 고정, 직교 투영 → 화면 어디서나 거리감 동일(루팅·전투 판정 공평). 퍼스펙티브·자유회전 폐기 |
| 2026-09-07 | 높이(Y축) 도입 범위 | **단차·엄폐까지.** 바닥은 평면 유지, 난간·차량·낮은 벽 뒤 엄폐를 높이로 표현. 2층·계단·옥상 풀세트는 **이번 범위 밖**(후속 판단) |
| 2026-09-07 | 캐릭터 외 아트 범위 | **전부 3D 재제작.** 적·NPC·프랍 63종/80프리팹까지 Blender 스크립트로 생성. 빌보드 스프라이트 혼용 폐기. 단 **아이템 아이콘 204종은 UI라 그대로 유지** |

## 왜 이번엔 되는가

2026-06-02에 아이소메트릭(2.5D 하이브리드)을 버리고 2D로 후퇴한 이력이 있다. 당시 사유와 지금 상황:

| 당시 폐기 사유 | 2026-09-07 현재 |
|---|---|
| AI로 아이소 스프라이트를 뽑으면 **투영각이 매번 달라 안 맞물림** | **해소.** 아트를 Blender 스크립트로 직접 생성한다(`build_reclaimer.py`). 투영각이라는 개념 자체가 없어짐 — 모델 하나를 어느 각도로든 렌더 |
| 깊이 정렬·접지가 계속 깨짐(2D-in-3D 이질감) | **해소.** 전부 진짜 3D 메시가 되면 Z버퍼가 정렬을 맡는다. `sortingOrder` 수동 관리 소멸 |
| 2인 제작에 비현실적 | **완화.** 로우폴리 단색 팔레트(텍스처 없음) + 절차 생성 스크립트 파이프라인이 이미 검증됨 |

반대로 **새로 생기는 비용**은 아래 "리스크"에 정리한다. 특히 카메라 오클루전(건물이 플레이어를 가림)은 2D에 없던 신규 문제다.

## 실측 — 무엇이 걸려 있나

2026-09-07 기준 `Assets/Scripts` + `Assets/Editor`:

| 항목 | 수치 | 의미 |
|---|---|---|
| 전체 C# | **66,933 LOC** / 207+45 파일 | |
| 2D API를 건드리는 코드 | **21,565 LOC (32%)** | 나머지 68%는 뷰 무관 로직 — 손 안 댐 |
| `Rigidbody2D` | **9 파일** | 물리 전환의 실제 표면은 좁다 |
| `Physics2D.*` 호출 | **10곳** | OverlapBox 2 · SyncTransforms 2 · Raycast/RaycastNonAlloc/OverlapPointAll/OverlapCircle/Linecast/CircleCast 각 1 |
| `Collider2D` | 19 파일 | |
| `Light2D` | 11 파일 | 조명 재작성 대상 |
| `ShadowCaster2D` | 3 파일 | 3D에선 메시가 알아서 그림자를 던짐 → 삭제 대상 |
| `SpriteRenderer` | 33 파일 | **대부분 HP바·라벨·아이콘** — 빌보드로 살거나 그대로 감 |
| `sortingOrder`/`sortingLayer` | 64 파일 | 월드 깊이용은 삭제, UI/오버레이용은 유지 — **선별 필요** |
| 씬 | 21개 | 대부분 **코드 생성물**(아래) |
| Prop2D 카탈로그 | SO 63 / 프리팹 80 | 3D 재제작 대상 |

### 🟢 결정적으로 유리한 세 가지

1. **맵이 코드 생성물이다.** `Zone1.unity`(69,587줄)는 손으로 배치한 게 아니라 `Zone1GreyboxLayout.cs`(2,223 LOC)가 찍어낸다. 좌표가 이미 **1u = 1m 실측 미터** 기준이라 `new Vector3(x, y, 0f)` → `new Vector3(x, 0f, y)` 매핑이 전환의 큰 축이다. 씬 21개를 손으로 옮기는 작업이 아니다.
   - 빌더: `Zone1GreyboxLayout`(2223) · `ScrapMarketGreyboxLayout`(500) · `SafehouseGreyboxLayout`(309) · `InteriorBuild`(246) · `GreyboxBuild`(274) · `HideoutGreyboxLayout`(201) · `PawnshopGreyboxLayout`(200) · `GameSceneBuilder`(203)
2. **세이브에 월드 좌표가 없다.** `GameSaveData`는 아이템·퀘스트·플래그·통화·평판 상태만 담는다. **세이브 마이그레이션 불필요, 기존 세이브 호환.**
3. **URP 3D 에셋이 안 지워지고 남아 있다.** `Assets/Settings/URP-3D.asset` + `ForwardRenderer.asset`. 파이프라인 전환은 에셋 제작이 아니라 **설정 교체**다.

## 유지 / 교체 / 신규

| 🟢 유지 (뷰 무관) | 🔄 교체 (2D → 3D) | 🆕 신규 |
|---|---|---|
| 인벤토리·컨테이너·아이템·무기파츠 | `Rigidbody2D`+`Collider2D` → `Rigidbody`+3D 콜라이더 | **카메라 오클루전 페이드**(건물이 플레이어를 가릴 때) |
| 제작·의료·생존·수면 | `Physics2D.*` 10곳 → `Physics.*` | **단차·엄폐 데이터**(높이 있는 콜라이더 + 엄폐 판정) |
| 퀘스트·NPC·대화·스토리·평판 | `Light2D` → `Light`(Directional 낮밤 + Point/Spot 랜턴) | **3D 프랍 카탈로그**(`Prop3DDefinition`) |
| 경제·상점·레이드·탈출·정산 | `ShadowCaster2D` → 메시 실제 그림자(삭제) | **프랍 Blender 생성 스크립트**(63종) |
| 세이브·체크포인트 (좌표 없음) | `GroundShadow2D`(202) → 실제 그림자 | **적·NPC 3D 모델 + 공격/피격/사망 애니** |
| **UI 전체**(uGUI 스크린스페이스 26패널) | 맵 빌더 좌표 `(x,y,0)` → `(x,0,y)` | 3D용 오소 쿼터뷰 카메라 리그 |
| 아이템 아이콘 204종 | `Prop2DDefinition`/`Prop2DBuilder`(615) → 3D판 | |
| StatDB·GameTuning·밸런스 | `PlayerVision` LOS: `Linecast` → `Physics.Linecast` | |
| 특성·퀵슬롯·도감 | Spine 플레이어/적 → 3D 모델(Spine 런타임 제거) | |
| 나침반·미니맵·지도조각 (XY→XZ만) | 월드 라벨·HP바·말풍선 → 빌보드 | |

## 좌표계 규약 (전환 후 SSOT)

전환이 끝나면 이 표가 `rendering.md`의 "한눈에" 표를 대체한다.

| 항목 | 결정 |
|---|---|
| **월드 평면** | **XZ** (바닥). up = **+Y**. 축척 **1u = 1m** (기존 유지 — 맵 빌더 좌표 그대로 재사용) |
| **좌표 변환** | 기존 2D `(x, y)` → 3D `(x, 0, y)`. 즉 **구 Y가 신 Z** |
| **깊이** | **Z버퍼(실제 3D)**. `sortingOrder` 수동 정렬 폐기 — UI/오버레이 전용으로 격하 |
| **카메라** | **오소그래픽 고정 쿼터뷰.** pitch ≈ 50° (Stage 0에서 확정), yaw는 아래 미결 항목 |
| **물리** | `Rigidbody`(capsule) + `freezeRotation`. 중력 켬(단차 낙하). NavMesh는 후속 선택 |
| **조명** | Directional(낮밤) + Point/Spot(랜턴·프롭 발광) + 실제 그림자 |
| **가시성** | 시야 FOV 유지 — LOS만 `Physics.Linecast`로. 어둠 오버레이는 3D 포그/후처리로 재작성 |
| **아트** | 로우폴리 단색 팔레트, 텍스처 없음. Blender 스크립트 생성 |

## 단계 계획

각 단계는 **끝에 돌려볼 수 있는 상태**로 닫는다. 전체 작업은 전용 브랜치에서 한다 — 파이프라인 전환이 전역이라 중간 상태로 main에 섞이면 안 된다.

### Stage 0 — 스파이크 (버리는 실험)
**목표:** 각도·스케일·오클루전을 눈으로 확정한다. 코드 자산이 아니라 **답**을 얻는 단계.
- 새 씬 하나(`Sandbox3D`)에 URP-3D 적용, 치비 프리팹 + 그레이박스 블록 몇 개 + 오소 쿼터뷰 카메라.
- **확정할 것:** 카메라 pitch / yaw(0° vs 45°) / orthographicSize / 캐릭터 스케일 / 벽 높이 기준 / 오클루전 처리 방식(디더 페이드 vs 컷어웨이 vs 지붕 숨김).
- **검증:** 사용자 눈. "이 각도로 15분 레이드를 돌 수 있나."
- **되돌리기:** 씬 하나 삭제. 기존 코드 무영향.

### Stage 1 — 좌표계 · 물리 전환
**목표:** 플레이어와 적이 XZ 평면에서 3D 물리로 움직이고 싸운다.
- `TopDownPlayer`(951) · `EnemyController`(1054) · `AttackPerformer`(215) · `ThrowSystem`(251) · `PlayerGun`(266) · `Breakable`(454) · `InteractableObject`(551) · `DoorController`(288) 물리/좌표 전환.
- `Physics2D.*` 10곳 → `Physics.*` (`OverlapBox`→`OverlapBox`, `CircleCast`→`SphereCast`, `Linecast`→`Linecast`, `OverlapPointAll`→카메라 레이캐스트).
- `FacingDirection`: 마우스 스크린 좌표 → **바닥 평면 레이캐스트**로 조준점 산출.
- `ChibiPlayerVisual`의 임시 기울기 보정 제거(진짜 3D가 되면 불필요).
- **대상 씬:** `CombatSandbox` 하나로 먼저. 지역/안전가옥은 손대지 않음.
- **검증:** 샌드박스에서 이동·구르기·근접·사격·투척·파괴가 전부 동작.

### Stage 2 — 렌더 파이프라인 · 조명 · 가시성
**목표:** URP 3D로 갈아타고 낮밤·시야·어둠이 3D에서 성립한다.
- `GraphicsSettings`/`QualitySettings` → `URP-3D.asset`. `TransparencySortMode` 커스텀축 해제.
- `Light2D` 11파일 → 3D 라이트. `ShadowCaster2D` 3파일 삭제(메시 그림자로 대체). `GroundShadow2D`(202) 삭제.
- `DayNightCycle`(196)·`DayNightLightDriver`·`PropLight2D`·랜턴 강화 로직을 3D 조명 기준 재작성.
- `PlayerVision`(148) LOS 3D화, `VisionDarkness`(155)·`DenseAnomalyController` 어둠/현상 오버레이를 3D 포그·후처리로 재작성.
- **Stage 0에서 정한 오클루전 페이드 구현.**
- `sortingOrder` 64파일 **선별 감사** — 월드 깊이용은 제거, UI/오버레이용은 유지.
- **검증:** 샌드박스에서 낮↔밤 전환, 랜턴 착용, 시야 콘 밖 적 은폐, 벽 뒤 차폐가 전부 동작.

### Stage 3 — 맵 파이프라인
**목표:** 맵이 3D로 생성된다.
- `Prop3DDefinition` + `Prop3DBuilder`(구 615 LOC 대체): 메시·머티리얼·콜라이더(Box/Mesh/Convex) + **높이**.
- **프랍 63종 Blender 생성 스크립트** — 캐릭터와 같은 파이프라인(`tools/`).
- 맵 빌더 좌표 매핑 `(x,y,0)`→`(x,0,y)` + 벽/건물에 높이 부여 + 단차·엄폐 배치.
- **순서:** 안전가옥 → 은신처 → 실내씬(`InteriorBuild`) → 고철시장 → 지역1(가장 큼).
- **검증:** 각 맵 빌더 재실행 → 씬 생성 → 걸어다니며 충돌·엄폐·조명 확인.

### Stage 4 — 캐릭터 · 적 · 월드 오브젝트
**목표:** 화면에 2D 스프라이트가 남지 않는다.
- 적·NPC 3D 모델 + **공격·피격·사망 애니**(현재 치비는 idle/walk/run만 있음 — 전투 모션 부재가 지금 최대 구멍).
- Spine 런타임·`cha` 에셋 제거.
- 월드 라벨·HP바·말풍선(`EnemySpeechBubble` 292)·퀘스트 마커(`NPCQuestMarker` 319)·`WorldItem`(184)·`InjuryVFX`(264) 빌보드화.
- **검증:** 지역1 레이드 완주 — 진입·전투·루팅·탈출·정산.

### Stage 5 — 정리
- 2D 잔재 제거(`Renderer2D.asset`/`URP-2D.asset`/2D 패키지/`Prop2D*`/`BRB` 2D 셰이더).
- `rendering.md`를 3D 기준으로 덮어쓰기, `topdown-art-spec.md` 폐기·대체, `char-art.md`/`prop-catalog.md`/`art-needs.md` 3D 기준 갱신, MASTER 색인 동기화.

## 리스크

| 리스크 | 대응 |
|---|---|
| **카메라 오클루전** — 2D에 없던 신규 문제. 건물·벽이 플레이어를 가린다 | Stage 0에서 방식을 먼저 정한다. 미정 상태로 Stage 3(맵)에 들어가면 맵을 두 번 만들게 됨 |
| **파이프라인 전환은 전역·비가역적** — 프로젝트 전체 머티리얼 룩이 한 번에 바뀜 | 전용 브랜치. Stage 1은 샌드박스 씬 하나로 한정. main 병합은 Stage 4 완주 후 |
| **전투 모션 부재** — 3D 캐릭터에 공격·피격·사망이 없다. 지금 상태로 전투에 넣으면 때려도 idle | Stage 4 전에 최소 attack/hit/death 3종 확보. Stage 1 검증은 모션 없이 판정만 확인 |
| **프랍 63종 재제작 물량** | 캐릭터 파이프라인이 이미 검증됨. 재질·형태를 파라미터화해 배치 생성. 그레이박스로 먼저 전부 채우고 순차 교체 |
| **적 AI가 평면 전제** — 현재 Rigidbody2D 스티어링, NavMesh 없음 | 단차만 도입하므로 평면 스티어링 유지 가능. 필요해지면 생성된 맵에 NavMesh 베이크(맵이 코드 생성이라 자동화 가능) |
| **중간 상태에서 게임이 안 돌아감** | 각 Stage를 "돌려볼 수 있는 상태"로 닫는다. Stage 경계에서 커밋 |

## 미결 항목

전환을 시작하기 전에 답이 필요한 것:

1. **카메라 yaw 0° vs 45°** — 45°는 고전 아이소 룩이지만 맵 빌더가 축정렬 격자라 건물이 화면에서 사선이 된다. 0°는 빌더 좌표가 화면축과 그대로 맞는다. **Stage 0에서 눈으로 결정.**
2. **오클루전 처리 방식** — 디더 페이드 / 벽 컷어웨이 / 지붕 자동 숨김. **Stage 0.**
3. **2층·계단을 나중에 넣을 것인가** — 이번 범위는 단차·엄폐까지지만, 벽 높이·건물 구조를 지금 어떻게 만드느냐가 나중 2층 도입 비용을 좌우한다. 최소한 "벽은 실제 높이를 가진 메시" 정도는 지켜둘 것.
4. **브랜치·PR 전략** — 현재 `codex/chibi-survivor-3d`가 `origin/main` 기준 430 커밋 앞/1 뒤로 갈라져 있다. 3D 전환 브랜치를 어디서 딸지 정리 필요.

## 변경 로그

| 날짜 | 내용 |
|------|------|
| 2026-09-07 | 3D 쿼터뷰 전환 결정 4건(완전 3D / 오소 고정 쿼터뷰 / 단차·엄폐 / 아트 전부 3D) + 실측 + Stage 0~5 계획 수립. 미착수. |
