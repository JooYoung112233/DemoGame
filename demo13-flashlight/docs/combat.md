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
- **안전구역(레이드 아님 = 안전가옥/은신처)에선 스태미너 무한** — 항상 가득 + 탈진 없음, 달리기/공격/구르기 무소모. 레이드(활성 지역) 진입 시에만 위 소모·탈진 규칙 적용. (판정: `RegionTimeManager.ActiveRegionId` 비어있음 = 안전구역, CharacterPanelUI와 동일 기준)

### 달리기 (Shift)
- 속도 = 이동속도 × `sprintSpeedMultiplier`(기본 1.6). 초당 `sprintStaminaCost`(12) 소모, `sprintMinStamina`(10) 미만이면 불가.
- 달리기 모션 속도/거리는 아래 "모션 스탯"의 `run` 항목에서 조절.

### 모션 스탯 (애니 속도/거리) — 플레이어·적·NPC 동일 규칙
- **규칙: 모든 모션은 애니 재생 속도를 따로 조절하고, 움직이거나 거리가 있는 모션(run/roll 등)은 distance도 따로 둔다.**
- 데이터: `PlayerStatData.motions` / `UnitStatData.motions` = `List<MotionStat>`. 각 항목 = `{ anim(논리 키), animSpeed(재생 배율, 기본 1), distance(m, 0=미사용) }`. **논리 키로 조회**(스켈레톤 실제 애니 이름이 attack1 등으로 달라도 무관).
- 플레이어 키: `idle/walk/run/crouch/crouch_walk/attack/roll`.
  - `animSpeed` — 해당 모션 애니 재생 속도(이동속도와 별개). walk/run은 이동속도 비례 토글(`animCadenceMatchesSpeed`)과 곱해짐.
  - `run.distance` — 한 번에 달릴 수 있는 최대 거리(m). 0=무제한(스태미너로만 제한). >0이면 그만큼 달리면 끊기고 멈추면 이동속도 2배로 회복.
  - `roll.distance` — 구르기 이동거리(m). 0이면 기존 `dodgeDistance`(3) 사용, >0이면 그 값으로 override.
- **적/NPC(`UnitStatData.motions`, 키 idle/walk/attack/hit/death)**: ⚠️ **데이터만 존재**. 적은 아직 Spine 애니 시스템이 없어(그레이박스 스프라이트) `EnemyController`가 사용하지 않음 — **적 애니 도입 시 동일 규칙으로 와이어링 예정**.
- 전부 **Control Panel ▸ StatDB ▸ Player Stat / Units**에서 조절(`motions` 리스트 자동 노출). [→ balance.md](balance.md)

### 그로기 (적)
- 숨겨진 게이지 (최대 100), 초당 8 자연 감소
- 가득 차면 2초간 무방비 상태
- 강공격이 주요 축적 수단

### 적 공격 예고 + 캔슬
- 적 공격 전 0.8초 예비동작 (빨간색 깜빡임)
- 예비동작 중 강공격 적중 → 공격 캔슬 + 긴 경직

### 적 스폰 (SpawnZone → EnemySpawner) — 2026-06-19
- **`SpawnZone`**(씬 배치): 영역(폭 size.x·높이 size.z, 2D XY) + `enemyCount` + `unitKey`(StatDB). 기즈모로 영역 표시.
- **`EnemySpawner`**(런타임, 부팅 시 자가 생성·DontDestroyOnLoad): 게임플레이 씬이 로드되면 그 씬의 SpawnZone들을 읽어 존마다 **`round(enemyCount × GameTuning.enemySpawnCountMult)`** 마리를 영역 랜덤 위치에 스폰(씬당 1회, 언로드 시 가드 해제→재입장 재스폰). 안전구역은 존이 없어 0기.
  - 유닛 프리팹(`UnitStatData.generatedPrefab`) 있으면 그걸, 없으면 **런타임 그레이박스 적**(붉은 사각 + Rigidbody2D/Collider/Hurtbox/Health/CombatFeedback/EnemyController) 생성 후 `SetUnitKey`.
  - 스폰 적은 레이드 씬으로 이동(`MoveGameObjectToScene`) → 씬과 함께 정리.
- **마릿수 조절**: `GameTuning.enemySpawnCountMult` (Control Panel). 0=스폰 안 함, 2=두 배.
- 배치: 고철시장(ScrapMarket_GB) 밴딧 공터(6,40)에 `bandit_melee × 3` 존. **재빌드 필요**(`Tools ▸ TopDown ▸ 빌드 ▸ 지역1` 또는 고철시장 빌더).
- ⚠️ **StatDB에 `bandit_melee` 유닛 등록 필요**: 미등록 시 그레이박스 적 + EnemyController 인스펙터 기본 스탯으로 폴백(동작은 하나 의도 스탯 미적용). Control Panel ▸ StatDB ▸ Units에서 추가. [→ balance.md](balance.md)
- **적 HP**: `EnemyController.Start`가 `unitStat.maxHp`를 Health에 적용(미등록 시 인스펙터 기본).
- **적 처치 전리품(2026-06-19)**: `EnemyController.DropLoot`가 **적별 전용 드랍 테이블(`UnitStatData.drops`) 우선**, 비면 **지상(Ground) 티어 region 루트 폴백**(컨테이너보다 약함). `GameTuning.enemyDropChance`로 전역 게이트.
  - **적별 드랍 테이블** = `List<EnemyDropEntry>{ itemId, chance(0~1), minQty, maxQty }`. **Control Panel ▸ StatDB ▸ Units ▸ 전리품 드랍**에서 유닛별로 직접 편집(비우면 지역 루트). 처치 시 각 항목을 chance로 굴려 `Random(min,max)`개 드랍. [→ balance.md](balance.md) [→ economy.md](economy.md)

## 무기 파츠 (부착물) — 2026-06-19 결정

> **질문**: 무기 파츠를 어떻게 구성/구현할지. **결정: ① 파츠 4종 — 조준경(Scope)/소염기(Muzzle)/탄창(Magazine)/손잡이(Grip). ② 현재 근접 무기뿐(총기 없음) → 시스템 먼저 구축, 총은 나중(파츠 시스템 재사용). ③ 파츠는 무기 개별 인스턴스에 귀속(타르코프식) — 그 무기를 끼웠다 빼도 파츠 유지.**

- **데이터**: `WeaponPartType` enum(None/Scope/Muzzle/Magazine/Grip). `ItemData`에 `weaponPartType` + 보정필드(`partMoveSpeedMult`/`partStaminaMult`/`partRangeBonus`/`partRecoilMult`/`partMagBonus`). `ItemData.IsWeaponPart`.
- **귀속**: `ItemInstance.attachments`(string[4] = [Scope,Muzzle,Magazine,Grip] itemId). `GetAttachment/SetAttachment/HasAnyAttachment/AttachmentWeight`. 무기 인스턴스가 부착물을 들고 다님.
- **장착 보존**: `PlayerEquipment.slotInstances`(슬롯별 실제 인스턴스) + `GetSlotInstance/SetSlotInstance`. 장착/해제/교체 시 인스턴스(부착물) 보존. 부착물 무게는 총 장비 무게에 합산.
- **UI**: 중앙 패널 상단 "무기 파츠 공간"에 장착 무기의 4슬롯 표시(부착=아이콘/이름+클릭 분리, 빈칸=종류 라벨). **부착**=파츠 우클릭 "부착"(종류 슬롯 비어있을 때). **분리**=슬롯 클릭 → 인벤 회수.
- **효과(현재)**: `partMoveSpeedMult`·`partStaminaMult`가 장착 무기 이동/스태미너 보정에 곱연산 적용(`TopDownPlayer` × `PlayerEquipment.WeaponPartMoveMult/WeaponPartStaminaMult`). 사거리/반동/장탄은 **총기 도입 시 활용**(필드만 준비).
- 파츠 4종 SO: `scope_basic`/`muzzle_basic`/`mag_extended`/`grip_tactical` (`Resources/Items/Misc/WeaponPart/`). 아이콘 미연결(에디터에서 연결 시 표시).
- **세이브 영속화(2026-06-19 완료)**: `GridItemEntry.attachments`(인벤 무기) + `GameSaveData.equippedWeaponAttachments`(장착 무기) → 저장/로드 라운드트립. **드래그 부착(완료)**: 파츠를 weaponBox 슬롯에 드래그 = 부착(컨텍스트 "부착"과 별개).
- ⚠️ **미완(총기 설계 필요)**: 총기 무기 + 사거리/반동/장탄(`partRangeBonus`/`partRecoilMult`/`partMagBonus`) 실효과. (현재 필드만 준비, 근접 무기엔 손잡이 이속/스태미너만 적용.)

## 시야 (FOV) — 2026-06-19 결정
- **결정: 좀보이드(Project Zomboid)와 동일한 시야콘** — 플레이어 정면 부채꼴 밖은 가려짐(적/오브젝트 비가시), 지역/시간별 어둠 혼합. 기존 손전등-주광 방식 폐기. 후속 구현(렌더링/가시성 시스템). [→ rendering.md](rendering.md)

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

| 그립 클래스 | 예시 무기 | 데모 무기(2026-06-15) | 자세 |
|---|---|---|---|
| 한손 (one-hand) | 단검, 한손 도구 | **단검(knife), 몽둥이(나무 각목/wood_plank)** | 한 손에 무기, 팔 자연스럽게 내림 |
| 양손 (two-hand) | 야구방망이, 장검 | **도끼(axe/hatchet)** | 양손 그립, 어깨 쪽에 들고 |
| 총 (gun) | 라이플 | - | 양손, 총을 앞으로 든 준비 자세 |

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

## 적 시체 루팅 (2026-07-10 확정 · ✅ 구현 완료)

> 질문: 처치한 적의 전리품을 어떻게 주나. **결정: 적 사망 → 시체가 `LootContainer`로 전환(기존 루트 상자 시스템 재사용).**

- **내용물**: StatDB 해당 유닛 보상 테이블(`UnitStatData.drops`, §적 스폰의 "적 처치 전리품" 규칙)에서 생성. 테이블 비면 지역(GroundDay) 루트 폴백, `GameTuning.enemyDropChance` 게이트(실패 = 빈손 시체 — 뒤질 수는 있음).
- **유지**: 시체는 **레이드 종료까지 유지** — GO를 파괴하지 않고 레이드 씬 소속으로 남김 → 씬 언로드 시 자동 정리.
- 기존 즉시 바닥 드랍(`EnemyController.DropLoot`) 방식을 시체 컨테이너 루팅으로 대체.
- **구현(2026-07-10)**: `EnemyController.OnDeath` → `BecomeCorpse()` — 같은 GO에 `LootContainer` + `InteractableObject.SetupAsContainer("시체 뒤지기")`(거리 기반 E). 사망 시 그로기 바 숨김 + `_nav.Stop()`(A* 리패스 잔류 방지) + `SetVisionVisible(true)`(시야 밖 사망 시 영구 투명 방지) 후 컨트롤러 disable(All 해제 — 시야/전투 판정 제외).
- **격자 크기 = 내용물에 맞춤(2026-07-10 사용자 결정 — "칸이 너무 작다")**: 고정 3×3 폐기 → `LootContainer.SetupAutoSize` — 4열 고정, 행 = 필요 칸수 올림 +1줄 여유, 2~6행 클램프. 그래도 넘치는 것만 바닥 드랍.
- **시체 가방(타르코프식, 2026-07-10 사용자 결정)**: `GameTuning.corpseBagChance`(기본 0.3) 확률로 시체에 **가방 아이템이 통째로** 들어 있음 — 가방 내부(ContainerGrid)에 지역 루트 1~2개. **가방째 드래그해 가져갈 수 있고**(중첩 컨테이너·세이브 기존 지원) 내용물은 컨테이너 팝업으로 열람. 드랍 게이트(enemyDropChance)와 독립 롤.
- ⚠️ **알려진 제약**: 컨테이너(시체 포함)에서 드래그로 가져온 아이템은 **수집 퀘스트(CollectItem) 카운트에 안 잡힘** — 훅이 `WorldItem.TryPickup`(바닥 줍기)에만 있음. 기존 씬 배치 상자도 동일 한계(회귀 아님 → 루팅 이전 지점 공통 훅으로 일괄 해결 예정, CharacterPanelUI).
- 구현 순서: A+B 통합 10종 중 **2번째** (①설정 → ②**적 시체 루팅** → ③퀵슬롯 → ④무게 → ⑤소음 → ⑥투척물 → ⑦재고 회전 → ⑧시체 회수 → ⑨도감 → ⑩지도+나침반 — dev-roadmap.md 2026-07-10).

> 근거: 익스트랙션 장르 표준. 신규 시스템 0(루트 상자 재사용).

---

## 소음 시스템 — 최소 버전 (2026-07-10 확정 · ✅ 구현 완료)

> 질문: 잠행/유인을 성립시키는 소음 규칙은. **결정: 소음 이벤트 + 적 '조사' 상태 + 플레이어 소음 UI(발밑 링 + HUD 미터).**

- **① 소음 발생** — 중앙 허브 `NoiseSystem`(정적): 지속 소음(이동)은 `SetPlayerSustained(pos,radius)`, 순간 펄스(타격·문)는 `ReportPulse(pos,radius,dur)`. 적은 `TryHear(listenerPos, out src)` 한 번으로 질의. 행동별 반경 = GameTuning: `noiseIdle 0 / noiseCrouch 1.5 / noiseWalk 5 / noiseRun 11 / noiseAttack 14(펄스) / noiseDoor 8(펄스)`, 펄스 지속 `noisePulseDuration 0.6`.
- **② 발생원**: `PlayerNoise`(자가부트 싱글턴)가 매 프레임 `TopDownPlayer` 이동 상태(웅크림/걷기/달리기)로 지속 반경 계산. 타격 = `TopDownPlayer` 약공/강공에서 `PlayerNoise.AttackNoise()`. 문 = `DoorController.Open`에서 `DoorNoise(pos)`.
- **③ 적 반응** — `EnemyController.State.Investigate` 신설: `UpdatePatrol`에서 `TryHear` → 소음 지점으로 이동 → `noiseInvestigateLook`(2.5s) 두리번 → 순찰 복귀. 도중 시야(`DetectRng`) 발견 시 Chase. 머리 위 '?' 표시(`ShowAlertMark`). **시야 발견과 별개 축.**
- **④ 플레이어 소음 UI (사용자 요청 — "내가 얼마나 시끄러운지")**:
  - **발밑 링** — `PlayerNoise`가 그리는 반투명 원(반경 = 실제 소음 반경, 시끄러울수록 진함) → 감지 범위 직관.
  - **파문 VFX** — 순간 펄스 지점에 퍼지는 링(`NoiseRipple`).
  - **HUD 미터** — `NoiseHUD`(자가부트, 좌하단) 소음 레벨 0~1 막대(조용=초록 → 시끄러움=빨강). `noiseUiMax`(14m) 기준 정규화.
- **⑤ 특성 연동** — `TraitManager.Mod("move_noise")`가 이동·타격 소음 반경에 곱(고양이걸음 −30% / 무거운발 +25%). **공중에 떠 있던 잠행 특성 개통.**
- **⑥ 범위** — 1차 **인간 적만**, 현상 몬스터는 2차. 안전가옥(IsSafehouse)은 무음(면제).
- **테스트**: F1 플레이어 탭 — 현재 소음 레벨/반경 표시 + "큰 소음(타격급)"·"문 소음" 버튼(적 조사 유도).
- 구현 순서: A+B 통합 10종 중 **5번째**. 다음 = ⑥투척물(이 소음 위에 얹음).

> 근거: 좀보이드의 심장. 공중에 떠 있던 잠행 특성들(traits.md)이 실제 시스템에 물리게 됨.

---

## 투척물 (2026-07-10 확정 — 구현 전, 소음 시스템 최소 버전 동반)

> 질문: 근접 전투에 원거리 상호작용을 어떻게 최소로 넣나. **결정: 투척물 1차 = 돌 1종 (유인 전용).**

- **동작**: 조준 지점 착탄 → **소음 이벤트 발생** → 반경 내 적이 **조사(Investigate) 이동**. 데미지 거의 0 — **유인 전용**.
- **⚠️ 전제 = 소음 시스템 최소 버전 동반 구현**: 소음 이벤트 발생/전파 + 적 '조사' 상태(EnemyController 상태머신 확장) — 상세는 위 **§소음 시스템(2026-07-10 확정)**. **잠행 특성 카테고리([traits.md](traits.md))가 이 소음 시스템을 기다리고 있음** — 투척물 구현 시 함께 개통.
- 구현 순서: A+B 통합 10종 중 **6번째** — 소음(5번째) 직후 동반 (①설정 → ②적 시체 루팅 → ③퀵슬롯 → ④무게 → ⑤소음 → ⑥**투척물** → ⑦재고 회전 → ⑧시체 회수 → ⑨도감 → ⑩지도+나침반 — dev-roadmap.md 2026-07-10).

> 근거: 좀보이드식 유인 — 근접 전투 게임에서 잠행 플레이를 성립시키는 유일한 원거리 수단.

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-07-10 | **적 시체 루팅 확정 (기획, 구현 전 — 갭 분석 A그룹).** 질문: 처치한 적의 전리품을 어떻게 주나. **결정: 적 사망 → 시체가 LootContainer로 전환(기존 루트 상자 시스템 재사용). 내용물 = StatDB 해당 유닛 보상 테이블에서 생성. 시체는 레이드 종료까지 유지.** 기존 즉시 바닥 드랍(`EnemyController.DropLoot`)을 대체. §적 시체 루팅 신설. A+B 통합 구현 순서 2번째(dev-roadmap.md 2026-07-10). | 근거: 익스트랙션 장르 표준 + 신규 시스템 0(재사용). |
| 2026-07-10 | **소음 시스템 최소 버전 확정 (기획, 구현 전 — 갭 분석 A그룹).** 질문: 잠행/유인을 성립시키는 소음 규칙은. **결정: ①행동별 소음 반경 — 걷기(소)/달리기(중)/전투·타격(대)/문·셔터(중), 수치=GameTuning. ②반경 내 적 '조사' 상태 신설(소음 지점 이동→두리번→순찰 복귀, 시야 발견과 별개 축). ③traits.md 잠행 카테고리(발소리 반경 감소 등)가 이 시스템에 물림. ④1차 인간 적만, 현상 몬스터 2차.** §소음 시스템 신설 + §투척물이 이 시스템 직후 동반 구현(A+B 통합 순서 5번째). | 근거: 좀보이드의 심장 — 공중에 떠 있던 잠행 특성 문제 해소. |
| 2026-07-10 | **투척물 확정 (기획, 구현 전).** 질문: 근접 전투에 원거리 상호작용을 어떻게 최소로 넣나. **결정: 1차 = 돌 1종 — 조준 지점 착탄 → 소음 이벤트 발생 → 반경 내 적 조사 이동. 데미지 거의 0(유인 전용).** ⚠️ 전제 = 소음 시스템 최소 버전(소음 이벤트 발생/전파 + 적 '조사' 상태) 동반 구현 — 잠행 특성 카테고리(traits.md)가 이 소음 시스템 대기 중. §투척물 신설. 갭 분석 5종 구현 순서 5번째(마지막). | 근거: 좀보이드식 유인 — 근접 전투에서 잠행 플레이를 성립시키는 유일한 원거리 수단. |
| 2026-06-15 | **데모 무기 그립 분류 확정.** 단검(knife)=한손, 몽둥이(나무 각목/wood_plank)=한손, 도끼(axe/hatchet)=양손. 무기 그립 테이블 "데모 무기" 열에 반영. (캐릭터 무기 장착 스프라이트 제작 기준 — 한손은 한 손 그립, 도끼는 양손 그립으로 에셋 제작) |
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
