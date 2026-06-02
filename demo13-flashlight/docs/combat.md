# 전투 시스템

## 현재 상태: 프로토타입 구현 완료

## 확정된 방향

소울라이크식 실시간 근접 전투. 턴제 제외.  
레퍼런스: Stoneshard(자원관리 깊이 참고), 기획서 v2(약공/강공/구르기/스태미너/그로기)

## 핵심 메커니즘

### 약공격 (좌클릭)
- 3타 콤보 (1타→2타→3타, 콤보 윈도우 내 재입력)
- 빠르고 스태미너 소모 적음
- 그로기 수치 낮음
- 수치: 8/9/12 피해, 6/6/8 스태미너, 5/6/8 그로기

### 강공격 (우클릭 홀드 → 릴리즈)
- 차징 시스템 (최소 0.6초 → 풀차지 1.5초)
- 느리지만 높은 피해 + 높은 그로기
- 적 예비동작 중 적중 시 공격 캔슬
- 수치: 20(일반)/32(풀차지) 피해, 22/35 스태미너, 25/45 그로기

### 구르기 (Space)
- 이동방향 대시 + 짧은 무적 프레임(0.2초)
- 스태미너 15 소모
- 차징/약공 중 캔슬 가능
- 쿨다운 0.5초

### 스태미너
- 최대 100, 초당 15 회복 (소모 후 1초 딜레이)
- 0이 되면 0.8초 탈진 (행동 불가)

### 그로기 (적)
- 숨겨진 게이지 (최대 100), 초당 8 자연 감소
- 가득 차면 2초간 무방비 상태
- 강공격이 주요 축적 수단

### 적 공격 예고 + 캔슬
- 적 공격 전 0.8초 예비동작 (빨간색 깜빡임)
- 예비동작 중 강공격 적중 → 공격 캔슬 + 긴 경직

## 시각 피드백 (애니메이션 없이)

| 상태 | 표현 |
|---|---|
| 약공격 | 흰색 플래시 |
| 강공격 차징 | 노랑→빨강 그라데이션 |
| 강공격 풀차지 | 빨간색 고정 |
| 구르기 | 반투명 (alpha 0.3) |
| 탈진 | 파란색 깜빡임 |
| 피격 | 빨간 플래시 |
| 적 예비동작 | 빨간 깜빡임 |
| 적 그로기 | 주황 게이지 바 → 스턴 시 빨간 깜빡 |
| 적 캔슬 당함 | 노란 플래시 |

---

## 타격감 연출 (Hit Feel) — 2026-06-02 설계

> 설계 원칙: ① **어둠/시야가 최대 무기** ② **약공 ≠ 강공**(무게 차이를 몸으로) ③ **플레이어 피격 = 화면 연출 / 적 피격 = 엔티티 연출** 분리 ④ **레이어별 on/off·강도 노브**.
> 토대 재사용: `CombatFeedback.cs`(플래시·스케일펀치·넉백·돌진), `ScreenEffectManager.cs`(셰이크·색수차·플래시·프리즈).

### 레이어 (약공/강공 차등)

| 레이어 | 약공격 | 강공격 | 구현 |
|---|---|---|---|
| 적중 순간 정지(히트스탑) | 없음 | **0.04~0.06s** | 신규(안전 구현) |
| 적 피격 플래시 | 흰색 깜빡(짧게) | 흰색 깜빡(강·길게) | **셰이더 `_FlashAmount`** |
| 스케일 펀치/스쿼시 | 작게 | 크게 | CombatFeedback(기존) |
| 넉백 | 약 | 강 | CombatFeedback(공격 방향 기반으로 개선) |
| 데미지 팝업 | 흰/작 | 주황/큼, 크리=노랑 | DamagePopup(✅ 2D 리워크) |
| 카메라 | 없음 | 줌 펀치 + 방향 셰이크 | 신규(CameraFollow 오프셋 레이어) |

### 히트스탑 (강공만, ✅ 구현 2026-06-02)
- 강공 적중에만 0.04~0.06s 정지. 예전 버그(전역 `Time.timeScale` 누수)는 **단일 가드 코루틴 + 항상 원복**(중복 시작 시 이전 취소, 정지 중 0을 캡처하지 않음, `unscaledDeltaTime` 대기)으로 해결.
- **구현**: `Hitstop.cs`(자동 생성 싱글톤, `Hitstop.Do(dur)`). **데이터 주도**: `AttackData.hitstop`/`hitstopDuration` 플래그 → `AttackPerformer.ScanWindow`가 적중 확정 시 켜진 공격만 `Hitstop.Do` 호출. `TopDownPlayer`의 런타임 기본 강공(`heavy` 0.05s / `heavyFull` 0.06s)에 켜짐 → 에셋 없이도 작동. 약공 콤보는 꺼짐.
- 외부 일시정지(안전가옥 `timeScale=0`) 중엔 무시. 히트스탑 중 `HitFlash`(unscaled)는 계속 보여 "흰 번쩍 + 뚝 멈춤"이 겹침.

### 적중 = 적 셰이더 플래시 (✅ 구현 2026-06-02)
- 적중 순간 스프라이트를 **흰색으로 깜빡**. `SpriteRenderer.color`(기존 빨강) 대신 셰이더 `_FlashAmount`로 구동 → 베이스 색·조명·틴트와 독립.
- **구현**: 캐릭터가 URP 2D 기본 머티리얼을 쓰므로 기존 BRB 셰이더 확장 대신 **전용 `BRB/SpriteFlash` 셰이더**(Light2D 반응 + `_FlashColor`/`_FlashAmount`) 신설 + **`HitFlash.cs`**(바디 스프라이트에 머티리얼 자가설치, `_FlashAmount` 가드 코루틴·unscaled). `CombatFeedback`이 기존 `.color` 빨강 플래시를 제거하고 `HitFlash.Flash(intensity, dur)` 호출(데미지 ≥ `heavyDamageThreshold`면 강공 플래시=길게). 플레이어/적 모두 `CombatFeedback`이 `HitFlash` 자동 부착.
- 부수효과: CombatFeedback이 더 이상 `.color`를 만지지 않아 `EnemyController`의 상태 틴트(윈드업/피격)와 충돌 해소.
- 강공은 더 긴 플래시 + 히트스탑(다음 단계)과 동시.

### 플레이어 피격 = 위험 비례 화면 연출 (✅ 구현 2026-06-02)
- **평소(체력 여유)**: 절제 — 가장자리 **비네트 붉은 펄스** + 약한 셰이크.
- **위험(체력 < 35% 또는 부상 보유)**: 풀세트 — **빨강 풀스크린 플래시 + 색수차 펄스 + 강한 셰이크**.
- **구현**: `PlayerHitReaction.cs`(`TopDownPlayer.Awake` 자동 부착). 플레이어 `Health.OnDamaged` 구독 → 위험도 = `Health.Percent < lowHpThreshold` ‖ `PlayerMedicalSystem.HasAnyInjury`. 연출은 `ScreenEffectManager`(Flash·**VignettePulse 신규**·ChromaticPulse·ScreenShake). 구르기 무적 중엔 무시.
- 화면 전체 연출은 *플레이어 피격에만*(적 피격은 엔티티 연출만). ⚠️ 비네트·색수차는 씬에 **post-process Volume**(Vignette/ChromaticAberration override) 필요 — 없으면 무해하게 스킵되고 Flash+셰이크만.

### 카메라 (CameraFollow와 공존, ✅ 구현 2026-06-02)
- 셰이크/줌펀치를 **`CameraFollow`의 가산 오프셋 레이어**로 구현 → 추적 위치(`_basePos`)와 분리, 충돌 없음. 타이머 `unscaledDeltaTime`(히트스탑 중에도 흔들림 보임).
- API: `CameraFollow.Instance.Shake(intensity, dur)` / `ZoomPunch(amount, dur)`(ortho size 펀치). 강공 적중 시 `AttackPerformer`가 히트스탑과 함께 `Shake(0.14,0.18)`+`ZoomPunch(0.05,0.18)` 호출.
- `ScreenEffectManager.ScreenShake`는 `CameraFollow.Instance` 있으면 그쪽으로 위임(스토리 셰이크도 충돌 방지), 없을 때만 구 localPosition 폴백.

### 가시성 연동 (→ `rendering.md`)
- 손전등 폐기 후 **좌보이드식 시야(FOV)**: 적은 플레이어가 바라보는 부채꼴 시야 밖이면 안 보임/어둑. "어디서 적이 튀어나오나"의 긴장이 곧 타격감의 무대. 상세는 [`rendering.md`](rendering.md) 가시성 섹션.

---

## 공격 중 이동 잠금 + 애니메이션 구조 (2026-06-02)

### 공격 시 이동 잠금 (다크소울식)
- 공격 애니메이션 재생 중 **이동 불가** (애니 끝나야 다시 이동)
- 효과: 무게감/리스크 → 거리 재기·회피 타이밍 = 신중한 전투
- 기존 구르기 캔슬(`AttackPerformer.CanCancel`)과 결합 → 공격 후 회피로 끊어 답답함 완화

### 상하체 애니 분리 불필요
- 공격 중 이동을 잠그므로 "걸으면서 때리기" 조합이 없음
- → 상체/하체 애니 분리(2트랙 블렌딩) **불필요**, 통짜 전신 모션으로 제작
- Spine 스켈레톤도 허리 분리 없이 전신 단일 애니로 구성

### 애니메이션 세트
| 분류 | 애니 | 이동 잠금 |
|---|---|---|
| 이동계 | idle / walk / run | - |
| 행동계 | attack(약공 콤보/강공) / hit / dodge | O |

### 무기 그립별 walk (확정)
- 무기는 손 본(hand bone) 어태치먼트 → walk 애니가 자동으로 무기를 움직임
- 단, 그립 자세가 다른 무기군은 walk 변형 필요. **3종으로 분류**:

| 그립 클래스 | 예시 무기 | 자세 |
|---|---|---|
| 한손 (one-hand) | 단검, 한손 도구 | 한 손에 무기, 팔 자연스럽게 내림 |
| 양손 (two-hand) | 야구방망이, 장검 | 양손 그립, 어깨 쪽에 들고 |
| 총 (gun) | 라이플 | 양손, 총을 앞으로 든 준비 자세 |

- 무기 하나하나마다 walk 만들지 않음 → 그립 클래스 3종만 제작, 같은 클래스 무기는 어태치먼트만 교체
- 맨손 walk = 한손 walk에서 무기 슬롯 비우거나 별도 1종

---

## 적 AI 길찾기 (Pathfinding) — 2026-06-02

> 결정: **Tilemap 그리드 A\*** (자체 구현, 외부 에셋·NavMesh 의존성 0). 맵이 Tilemap+Collider2D라 가장 자연스러움.
> 플레이어는 **WASD 직접 이동 유지**(길찾기 미사용) + 전신 콜라이더만, **적만** 길찾기. 그리드는 플레이어를 장애물로 안 치고 추격 타겟으로만 취급.

### 파이프라인
`NavGrid`(격자 베이크) → `AStarPathfinder`(8방향 A*) → `NavAgent`(경로 추종) → `EnemyController.UpdateChase`

- **`NavGrid`**: 영역을 cellSize(0.5) 격자로 나눠 셀별 막힘 베이크. 막힘 = **비-트리거 Collider2D** 겹침(Player/Enemy 레이어 제외). **에이전트 바디 반경만큼 dilate** → 전신이 벽에 안 끼는 경로만. 씬에 1개, `Instance` 조회, `Rebuild()`로 재베이크.
- **`AStarPathfinder`**: 막힘 격자 위 8방향 A*, octile 휴리스틱, **코너 끼임 방지**(대각 이동 시 양옆 walkable 필수), 이진 최소힙. 월드 웨이포인트 반환.
- **`NavAgent`**(적): `SetDestination(player)` → 경로 추종 `DesiredDirection` 제공. 주기 리패스(0.4s, 에이전트별 스태거), **LOS skip-ahead**(직선으로 보이는 먼 웨이포인트로 당겨 부드럽게). NavGrid/경로 없으면 **직진 폴백**.
- **`EnemyController.UpdateChase`**: 직진 스티어링 → `NavAgent` 경로 방향으로 교체(폴백 직진 유지). 추격 종료 시 `Stop()`.

### 전신 차단 ("탑다운이라 몸통을 막아야")
- **물리**: 플레이어·적 바디 `Collider2D`(원, 비-트리거) + 벽/프롭 Collider2D → 몸통 단위 차단.
- **길찾기**: 그리드 dilate로 경로 중심이 벽에서 바디 반경만큼 떨어짐 → 모서리 끼임 없음.

### 비고
- 순찰(`UpdatePatrol`)은 직진 유지(국소 배회). 길찾기는 추격 전용.
- `NavGrid`는 씬당 1개 필요. **`CombatSandbox` 빌더가 자동 배치**(+ 우회 테스트용 벽 2개). 다른 씬은 NavGrid 오브젝트 1개 두면 됨(없으면 적은 직진 폴백). 적 `NavAgent`는 `EnemyController`가 **자동 부착**.
- `com.unity.ai.navigation`(3D NavMesh) 패키지는 **미사용**(이 시스템과 무관).

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-24 | 전투 프로토타입 구현. 약공(콤보)/강공(차징)/구르기/스태미너/그로기/적 캔슬 시스템. 스톤샤드 참고하되 턴제 제외 확정. |
| 2026-06-02 | **탑다운 2D 전투 이식.** 구 `PlayerController`(NavMesh/3D) 폐기 → `TopDownPlayer`(Rigidbody2D)에 전투 전면 재구현: 약공(콤보)·강공(차징)·구르기(무적)·스태미너·탈진을 `StatDB.playerStat` 기반으로. 공격 판정은 `Physics2D.OverlapCircleAll`로 FacingDirection 방향 → `EnemyController.TakeHit(dmg, groggy, knockback)`. `EnemyController`도 Rigidbody2D 상태머신(NavMesh 제거), `TakeHit`/`IsDead` 추가. 무적 체크(Health/CombatFeedback)·전투 중 상호작용 차단(InteractionSystem)·HUD 스태미너 바 연결. 상호작용 계층(Interact/Talk/Open/Pickup)은 `GameObject` 인터페이스로 확정. |
| 2026-06-02 | **적 길찾기(Tilemap 그리드 A\*) 추가.** `NavGrid`(격자 베이크, 막힘=비-트리거 Collider2D, 바디 반경 dilate)→`AStarPathfinder`(8방향, 코너 끼임 방지, 최소힙)→`NavAgent`(경로 추종, 리패스, LOS 스킵, 직진 폴백)→`EnemyController.UpdateChase` 연동. 플레이어는 WASD 유지·길찾기 미부착(전신 콜라이더만). 적 `NavAgent` 자동 부착, `NavGrid`는 빌더가 배치(샌드박스에 우회 벽 2개). 외부 에셋·NavMesh 의존성 0. |
| 2026-06-02 | **전투 샌드박스 + 적 프리팹 빌더.** `Editor/CombatSandboxBuilder.cs` — ①`Resources/Enemy.prefab` 생성기(바디 스프라이트+Hurtbox(trigger,Enemy레이어)+Health+CombatFeedback+EnemyController, playerMask=Player): "적을 코드로 스폰하는 곳이 없어 바디 스프라이트가 없던" 공백 해소. ②전투 샌드박스 씬 생성기(`Tools▸BRB▸Build Scene▸Combat Sandbox`): 2D카메라+CameraFollow+post-process Volume(비네트/색수차)+밝은 Global Light2D+SpawnPoint+적 3기. 타격감/히트박스 에디터 테스트용 아레나. |
| 2026-06-02 | **타격감 연출 구현(5종).** ①흰 플래시: `BRB/SpriteFlash` 셰이더 + `HitFlash.cs`(머티리얼 자가설치, `_FlashAmount`), `CombatFeedback`이 `.color` 빨강 플래시 제거 후 위임. ②히트스탑: `Hitstop.cs`(안전 싱글톤) + `AttackData.hitstop` 플래그 + `AttackPerformer` 적중 트리거, 강공 런타임 기본값에 켜짐. ③카메라: `CameraFollow`에 가산 셰이크/줌 레이어, `AttackPerformer` 강공 적중 시 호출, `ScreenEffectManager.ScreenShake`도 위임. ④플레이어 피격: `PlayerHitReaction.cs`(위험 비례) + `ScreenEffectManager.VignettePulse` 신규. ⑤`DamagePopup` 2D화(+Z·매프레임 빌보드 제거). |
| 2026-06-02 | **타격감 연출 설계 확정.** 강공 히트스탑(0.04~0.06s, 안전 구현), 적중=적 셰이더 흰 플래시(`_FlashAmount`), 플레이어 피격=위험 비례 화면 연출(평소 절제→저체력/부상 풀세트), 카메라 셰이크/줌은 CameraFollow 오프셋 레이어. 약공/강공 차등 레이어 표 추가. `DamagePopup`은 3D 시절 유물(빌보드+Y/Z오프셋)이라 2D 리워크 필요. |
| 2026-06-02 | **가시성 전환: 손전등 폐기 → 좀보이드식 시야(FOV).** 적은 플레이어가 바라보는 부채꼴 시야 밖이면 안 보임/어둑. 어둠=하이브리드(지역/시간대별). 손전등 코드(`FlashlightController`/`FlashlightBeam`/손전등 Light2D) 완전 제거 후 시야 시스템 신규 작성. 상세 `rendering.md`. |
| 2026-06-02 | **프레임 기반 히트박스/허트박스 시스템 + 에디터 툴.** `AttackData`(SO): 공격 1종의 `duration`(초) + `HitWindow[]`(정규화 0~1 활성구간, Box/Circle, facing기준 offset(전방x/좌y), 크기/반경, 회전, damage·groggy 배율). `AttackPerformer`: 시간진행하며 활성 윈도우를 `OverlapBox/CircleNonAlloc(targetMask)`로 스캔 → `Hurtbox.ReceiveHit`(중복 1회). `Hurtbox`(trigger Collider2D): 피격 판정, 적이면 `EnemyController.TakeHit`·아니면 `Health.TakeDamage`. **구르기 무적 = 허트박스 콜라이더 off**(`SetActive(!IsInvincible)`). 플레이어 콤보별/강공 AttackData, 적 `attackData`(없으면 즉시 데미지 폴백). 팀 구분=레이어(Player=6/Enemy=9). **에디터**: `AttackDataEditorWindow`(Tools▸TopDown Combat▸Attack Editor) — 타임라인 스크러버(윈도우 막대), 2D 탑다운 프리뷰(facing→우, 활성 윈도우 진하게, 중심 핸들 드래그), 윈도우 추가/삭제·속성 편집. |
| 2026-06-02 | **무기 그립별 walk 3종 확정.** 무기=손 본 어태치먼트라 walk가 자동 적용되나 그립 자세가 다른 무기군은 변형 필요 → 한손/양손/총 3종 walk 제작. 무기별 개별 walk는 만들지 않고 그립 클래스 단위로 어태치먼트 교체. |
| 2026-06-02 | **공격 중 이동 잠금 확정 + 상하체 애니 분리 불필요.** 공격 애니 재생 중 이동 불가(다크소울식) → "걸으면서 때리기" 조합이 없으므로 상체/하체 2트랙 분리 불필요, 통짜 전신 모션으로 제작. 구르기 캔슬과 결합해 답답함 완화. 애니 세트: 이동계(idle/walk/run) + 행동계(attack/hit/dodge, 이동 잠금). |
| 2026-06-02 | **프레임 기반 전환 + 연속 공격(콤보) 구조.** ①타이밍 정규화(0~1)→**프레임**: `AttackData.fps`+`totalFrames`, `HitWindow.startFrame/endFrame`. `AttackPerformer`가 `CurrentFrame`으로 윈도우 활성 판정. ②**콤보 체인** `AttackComboData`(SO): 순서대로 이어지는 `AttackData[] steps` + `bufferTime`(선입력). `AttackData.cancelFromFrame`(이 프레임 이후 다음 단계 캔슬 입력 허용), `AttackPerformer.CanCancel`. ③`TopDownPlayer.lightCombo`: 공격 중 캔슬 윈도우에 입력하면 다음 단계 연결 + 선입력 버퍼, 구르기 시 콤보 끊김. ④**에디터** 개편: 단일/콤보 모드 토글, 콤보는 [1타][2타]… 단계 탭, **프레임 그리드 타임라인**(칸=프레임, 윈도우 막대, 캔슬 프레임 마커, 프레임 스크러버), 윈도우 시작/끝 프레임 IntSlider. |
| 2026-06-02 | **무기 장착 → 전투 반영.** `WeaponData`(SO): 무기별 `lightCombo`(콤보)·`heavyAttack`·`heavyFullAttack` + `moveSpeedMult`·`staminaCostMult`. `ItemData.weaponData` 참조(Weapon 카테고리). `PlayerEquipment`(플레이어 컴포넌트): `EquipWeapon`(같은 무기 재장착=해제 토글)/`Unequip`, 세이브용 `GetSaveData`(itemId). `TopDownPlayer.SetWeapon(WeaponData)` → `CurrentLightCombo`/`CurrentHeavy`/`CurrentHeavyFull`·`WeaponMoveMult`·`WeaponStamMult`로 전투 전반 무기 반영(빈 항목/맨손=인스펙터 기본 콤보). 인벤토리 우클릭 Weapon → 장착(소모 없음). 루팅한 무기가 실제 콤보·리치·속도를 바꿈. |
