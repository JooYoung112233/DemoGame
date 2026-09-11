# 전투 시스템

## 무기 구성 결정 — 2026-09-11 (시스템 정리 2단계)

| 날짜 | 질문(선택지) | 사용자 결정 |
|---|---|---|
| 2026-09-11 | 총이 주력이 된 지금 **근접 무기**(방망이·검·도끼 등)를 — 남긴다(총알이 없을 때의 보조 무기) / 없앤다(총 전용) | **남긴다 — 총과 근접 둘 다 주력(좀보이드식)**. 처음엔 "근접 = 보조 무기"로 정리했으나 사용자 정정: "근접도 주력이긴 해, 좀보이드처럼" |
| 2026-09-11 | **구르기**(Space, 짧은 무적 — 총알 회피 수단)를 — 남긴다 / 없앤다 | **유지하되 보류** — "구르기는 모션이 없어서 일단 보류". 시스템은 두고, 구르기 모션이 생기면 다듬는다 |
| 2026-09-11 | 보류 중인 구르기는 지금도 모션 없이 미끄러지듯 동작한다 — 그대로 둔다 / 모션 전까지 꺼 둔다 | **꺼 둔다** — "구르기는 일단 꺼둬". 코드는 지우지 않고 `GameTuning.dodgeEnabled`(기본 끔)로 막는다. 모션이 생기면 켠다 |
| 2026-09-11 | 총·근접을 둘 다 주력으로 쓰려면(지금은 주무기 칸 하나만 전투에 쓰임) — 둘 다 차고 키로 전환 / 지금처럼 하나만 / 퀵슬롯 숫자키 | **퀵슬롯 숫자키** — 무기도 퀵슬롯에 올려 숫자키로 꺼내 든다 |
| 2026-09-11 | 근접 공격 방향(지금은 이동 방향으로 휘두르고 부위만 커서로) — 공격 순간 커서 쪽 / 이동 방향 그대로 / 우클릭 조준 때만 | **공격 순간 커서 쪽** — 클릭하면 그 순간 커서 방향으로 몸을 돌려 휘두른다. 평소 바라보는 방향은 여전히 이동 방향 |
| 2026-09-11 | 총 수치가 아이템·무기 데이터·총기 세트·스탯DB에 흩어진 것을 — 한 곳으로 전부 / 밴딧만 연결 / 나중에 | **한 곳으로 전부** — 총 수치는 `WeaponData` 하나. 밴딧 총도 같은 `WeaponData`를 참조하고 적 전용 배율(피해·탄속·조준 경고·연사)만 StatDB에. 총 모양·모션(`PlayerFirearmSet`)도 `WeaponData`에서 직접 연결. 근접/총 데이터 칸 분리 |
| 2026-09-11 | 그로기 값이 문서(2026-07-11 하향: 밴딧 45·탱크 95)와 실제 데이터(밴딧 80·탱크 160)가 다르다 — 문서 값을 데이터에 / 데이터 유지 / 직접 정함 | **데이터 유지**(80·160) — 실제로 플레이해 온 값. 문서를 고친다. 근접이 주력이 됐으니 플레이하며 다시 조정 |
| 2026-09-11 | (위 결정 직후 확인) 데이터 값이면 **약공만으로는 기절이 절대 안 쌓인다**(약공 5 × 초당 약 1.6회 = +8/s < 감쇠 10/s) — 조정할까 | **조정한다**(사용자 "그로기 조정해주고", 값은 위임) → 약공 14·강공 32·풀차지 58 / 일반 밴딧 60·감쇠 5 / 중장 120·감쇠 4. §그로기 (적) |

> 맨손(주먹) 공격은 같은 날 폐기(§총기 아래 변경 로그). 이 결정에 따라 정리 2단계는 근접 코드를 **지우지 않고 정리**하고, 총기 데이터가 여러 곳에 흩어진 것을 한 곳으로 모은다.

## 현재 상태 (2026-09-11, 코드 기준)

| 축 | 상태 |
|---|---|
| **총** | 플레이어 권총(`WeaponData` Pistol9 — 좌클릭 사격·우클릭 조준·R 장전, 조준원·반동·탄 스펙) · 총기 밴딧(권총·소총, 조준 경고 후 피할 수 있는 총알). 플레이어 소총은 모양·모션만 있고 아이템·수치가 없다 |
| **근접** | 방망이·장검(`WeaponData` 있음) + 칼·도끼·파이프 등 아이템. 좌클릭 약공(콤보 꺼 둠) · 우클릭 홀드 차징 강공. 방향 = 공격 순간 커서 쪽(2026-09-11 결정) |
| **무기 전환** | 퀵슬롯 숫자키(2026-09-11 결정). 전투는 주무기 칸 하나만 읽는다 |
| **구르기** | 꺼 둠(`GameTuning.dodgeEnabled`) — 모션이 생기면 켠다 |
| **스태미너** | 레이드에서만 소모(달리기·근접·구르기). 총은 소모 없음 |
| **그로기** | 적만. 근접·총 모두 쌓이고 가득 차면 기절. 값은 StatDB(밴딧 60·중장 120, 2026-09-11 조정 — 약공 6대면 일반 밴딧 기절) |

> 아래 §확정된 방향·§핵심 메커니즘 이하는 2D 시절(소울라이크 근접) 설계에서 출발했다. **수치가 코드와 다르면 코드(StatDB·GameTuning·WeaponData)가 진실** — 시스템 정리 2단계에서 섹션별로 고치는 중.

## (원안) 프로토타입 구현 완료

## 확정된 방향

~~소울라이크식 실시간 근접 전투~~(2D 시절 원안) → **총과 근접 둘 다 주력, 좀보이드식**(2026-09-11). 턴제 제외.  
레퍼런스: Stoneshard(자원관리 깊이 참고), 기획서 v2(약공/강공/구르기/스태미너/그로기)

## 핵심 메커니즘

### 약공격 (좌클릭)
- ~~3타 콤보~~ → **콤보 꺼 둠**(2026-07-11, `GameTuning.comboEnabled`) — 클릭마다 단타. 콤보 데이터는 남아 있어 켜면 되살아난다
- 근접 무기를 들었을 때만(맨손 공격 폐기). 방향 = 공격 순간 커서 쪽(2026-09-11)
- 빠르고 스태미너 소모 적음
- 그로기 수치 낮음
- 수치: 8/9/12 피해, 6/6/8 스태미너, 5/6/8 그로기

### 강공격 (우클릭 홀드 → 릴리즈) — 근접 무기일 때만(총을 들면 우클릭 = 조준)
- 차징 시스템 (최소 0.6초 → 풀차지 1.5초)
- 느리지만 높은 피해 + 높은 그로기
- 적 예비동작 중 적중 시 공격 캔슬
- 수치: 20(일반)/32(풀차지) 피해, 22/35 스태미너, 25/45 그로기

### 구르기 (Space)
- ⛔ **2026-09-11부터 꺼 둠**(`GameTuning.dodgeEnabled`) — 모션이 없어 달리기 모션으로 미끄러지기만 했다. 켜면 아래대로 동작
- 이동방향 대시 + 무적 프레임 0.26초(StatDB `playerStat`, 특성 `dodge_iframe`이 늘림)
- 스태미너 15 소모
- 차징/약공 중 캔슬 가능
- 쿨다운 0.5초

### 스태미너
- 최대 100, 초당 15 회복 (소모 후 1초 딜레이)
- 0이 되면 0.8초 탈진 (행동 불가)
- **안전구역(레이드 아님 = 안전가옥/은신처)에선 스태미너 무한** — 항상 가득 + 탈진 없음, 달리기/공격/구르기 무소모. 레이드(활성 지역) 진입 시에만 위 소모·탈진 규칙 적용. (판정: `RegionTimeManager.ActiveRegionId` 비어있음 = 안전구역, CharacterPanelUI와 동일 기준)

### 달리기 (Shift)

- **2026-09-11 사용자 결정:** 인벤토리에서 장착한 무기는 정지·걷기 때 들고, 실제 스프린트 때만 몸에 수납한다. 수납은 장착 해제가 아니며 아이템·잔탄을 유지한다. 권총은 오른쪽 다리, 소총·검·방망이는 등 뒤. 조준·재장전·사격 시 스프린트를 중단하고 다시 든다. 회피 모션만으로는 수납하지 않는다. [플레이어 총기 모션·검증](bandit-firearms.md#플레이어-연결-2026-09-11).
- 속도 = 이동속도 × `sprintSpeedMultiplier`(기본 1.6). 초당 `sprintStaminaCost`(12) 소모, `sprintMinStamina`(10) 미만이면 불가.
- 달리기 모션 속도/거리는 아래 "모션 스탯"의 `run` 항목에서 조절.

### 모션 스탯 (애니 속도/거리) — 플레이어·적·NPC 동일 규칙
- **규칙: 모든 모션은 애니 재생 속도를 따로 조절하고, 움직이거나 거리가 있는 모션(run/roll 등)은 distance도 따로 둔다.**
- 데이터: `PlayerStatData.motions` / `UnitStatData.motions` = `List<MotionStat>`. 각 항목 = `{ anim(논리 키), animSpeed(재생 배율, 기본 1), distance(m, 0=미사용) }`. **논리 키로 조회**(스켈레톤 실제 애니 이름이 attack1 등으로 달라도 무관).
- 플레이어 키: `idle/walk/run/crouch/crouch_walk/attack/roll`.
  - `animSpeed` — 해당 모션 애니 재생 속도(이동속도와 별개). walk/run은 이동속도 비례 토글(`animCadenceMatchesSpeed`)과 곱해짐.
  - `run.distance` — 한 번에 달릴 수 있는 최대 거리(m). 0=무제한(스태미너로만 제한). >0이면 그만큼 달리면 끊기고 멈추면 이동속도 2배로 회복.
  - `roll.distance` — 구르기 이동거리(m). 0이면 기존 `dodgeDistance`(3) 사용, >0이면 그 값으로 override.
- **적/NPC(`UnitStatData.motions`, 키 idle/walk/attack/hit/death)**: ⚠️ **데이터만 존재**. 적은 아직 애니 시스템이 없어(그레이박스 스프라이트) `EnemyController`가 사용하지 않음 — **3D 적 애니 도입 시 동일 규칙으로 와이어링 예정**(3d-migration.md Stage 4).
- 전부 **Control Panel ▸ StatDB ▸ Player Stat / Units**에서 조절(`motions` 리스트 자동 노출). [→ balance.md](balance.md)

### 그로기 (적)
- 숨겨진 게이지 — 값은 StatDB(**2026-09-11 조정**, 사용자 "그로기 조정해주고"): 일반 밴딧(근접·권총·소총) 최대 **60**·초당 **5** 감소·기절 **2.5초** / 중장 `bandit_tank` **120**·**4**·**2.0초** / `bandit_ranged`(미배치) **45**·**6**·**2.8초**
- 쌓는 양: 플레이어 약공 **14** · 강공 **32** · 풀차지 **58** — StatDB `playerStat` + 방망이 공격 에셋(`Bat_SingleHit`·`Bat_Light`·`Bat_Heavy`·`Bat_HeavyFull`, 따로 들고 있어 같이 맞춤) · 권총 한 발 **14** · 소총 **10**(`WeaponData.groggy`)
- 체감(약공 약 0.62초 간격 기준): 일반 밴딧 = **약공 6대**(약 3.7초) · 풀차지 1 + 약공 1 · 권총 5발 / 중장 = 약공 11대 · 풀차지 2 + 약공 2
- 조정 전(80·10 / 약공 5)엔 약공만으로는 감쇠를 못 넘어 기절이 **절대** 안 쌓였다(아래 2026-07-11 진단과 같은 구멍)

### 적 공격 예고 + 캔슬
- 적 공격 전 0.8초 예비동작 (빨간색 깜빡임)
- 예비동작 중 강공격 적중 → 공격 캔슬 + 긴 경직

### 적 스폰 (SpawnZone → EnemySpawner) — 2026-06-19 (그레이박스 폴백의 Rigidbody2D·"붉은 사각" 서술은 2D 시절 — 지금은 3D 밴딧 모델)
- **`SpawnZone`**(씬 배치): 영역(폭 size.x·높이 size.z, 2D XY) + `enemyCount` + `unitKey`(StatDB). 기즈모로 영역 표시.
- **`EnemySpawner`**(런타임, 부팅 시 자가 생성·DontDestroyOnLoad): 게임플레이 씬이 로드되면 그 씬의 SpawnZone들을 읽어 존마다 **`round(enemyCount × GameTuning.enemySpawnCountMult)`** 마리를 영역 랜덤 위치에 스폰(씬당 1회, 언로드 시 가드 해제→재입장 재스폰). 안전구역은 존이 없어 0기.
  - 유닛 프리팹(`UnitStatData.generatedPrefab`) 있으면 그걸, 없으면 **런타임 그레이박스 적**(붉은 사각 + Rigidbody2D/Collider/Hurtbox/Health/CombatFeedback/EnemyController) 생성 후 `SetUnitKey`.
  - 스폰 적은 레이드 씬으로 이동(`MoveGameObjectToScene`) → 씬과 함께 정리.
- **마릿수 조절**: `GameTuning.enemySpawnCountMult` (Control Panel). 0=스폰 안 함, 2=두 배.
- 배치: 고철시장(ScrapMarket_GB) 밴딧 공터(6,40)에 `bandit_melee × 3` 존. **재빌드 필요**(`Tools ▸ TopDown ▸ 빌드 ▸ 지역1` 또는 고철시장 빌더).
- ⚠️ **StatDB에 `bandit_melee` 유닛 등록 필요**: 미등록 시 그레이박스 적 + EnemyController 인스펙터 기본 스탯으로 폴백(동작은 하나 의도 스탯 미적용). Control Panel ▸ StatDB ▸ Units에서 추가. [→ balance.md](balance.md)
- **적 HP**: `EnemyController.Start`가 `unitStat.maxHp`를 Health에 적용(미등록 시 인스펙터 기본).
- **적 처치 전리품(2026-06-19 · 2026-09-11 갱신)**: `EnemyController.RollLoot`가 **적별 전용 드랍 테이블(`UnitStatData.drops`) 우선**(지금은 전 유닛 비어 있음), 비면 **이 씬 루팅 지역의 `corpse` 종류 표**(`MapSpawnController.CurrentRegionId` — 실내에서 쓰러뜨린 적은 그 건물의 지역 표, [region-loot.md §2.2](region-loot.md)). `GameTuning.enemyDropChance`로 전역 게이트. (2026-09-11 전: 메서드 이름 `DropLoot`, 폴백 = 활성 지역 바닥 티어)
  - **적별 드랍 테이블** = `List<EnemyDropEntry>{ itemId, chance(0~1), minQty, maxQty }`. **Control Panel ▸ StatDB ▸ Units ▸ 전리품 드랍**에서 유닛별로 직접 편집(비우면 `corpse` 표). 처치 시 각 항목을 chance로 굴려 `Random(min,max)`개 드랍. [→ balance.md](balance.md) [→ economy.md](economy.md)

## 무기 파츠 (부착물) — 2026-06-19 결정 · ⚠️ 2026-09-09 탄창 1종만 남음(조준경·소염기·손잡이 폐기, scope-cut.md)

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

## 시각 피드백 (애니메이션 없이) — (2D 스프라이트 시절 기록)

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

## 타격감 연출 (Hit Feel) — 2026-06-02 설계 · ⚠️ 2D 시절: `BRB/SpriteFlash` 셰이더는 없고(2026-09-10 셰이더 백지화) 부위 의료(`PlayerMedicalSystem`)도 폐기됨. 히트스탑·화면 연출은 유효

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

## 공격 중 이동 잠금 + 애니메이션 구조 (2026-06-02) — (Spine 시절 기록 — 지금은 3D 치비 Animator, 방망이·장검 모션)

### 공격 시 이동 잠금 (다크소울식)
- 공격 애니메이션 재생 중 **이동 불가** (애니 끝나야 다시 이동)
- 효과: 무게감/리스크 → 거리 재기·회피 타이밍 = 신중한 전투
- 기존 구르기 캔슬(`AttackPerformer.CanCancel`)과 결합 → 공격 후 회피로 끊어 답답함 완화

### 상하체 애니 분리 불필요
- 공격 중 이동을 잠그므로 "걸으면서 때리기" 조합이 없음
- → 상체/하체 애니 분리(2트랙 블렌딩) **불필요**, 통짜 전신 모션으로 제작
- (구 Spine 계획) 스켈레톤도 허리 분리 없이 전신 단일 애니로 구성 — 3D에서도 상하체 분리 마스크는 후순위

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

## 적 AI 길찾기 (Pathfinding) — 2026-06-02 · 지금은 `NavGrid`가 3D 콜라이더(`Physics.OverlapBox`)로 굽고 `NavGridBootstrap`이 씬마다 깐다(아래 Tilemap 서술은 2D 시절, [enemy-ai.md](enemy-ai.md))

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

- **내용물**: `EnemyController.RollLoot` — StatDB 해당 유닛 보상 테이블(`UnitStatData.drops`, §적 스폰의 "적 처치 전리품" 규칙)에서 생성. 테이블이 비면(지금 전 유닛) 이 씬 루팅 지역의 **`corpse` 종류 표**([region-loot.md §2.2](region-loot.md)). `GameTuning.enemyDropChance` 게이트(실패 = 빈손 시체 — 뒤질 수는 있음).
- **유지**: 시체는 **레이드 종료까지 유지** — GO를 파괴하지 않고 레이드 씬 소속으로 남김 → 씬 언로드 시 자동 정리.
- 기존 즉시 바닥 드랍(당시 `EnemyController.DropLoot`, 지금은 `RollLoot`) 방식을 시체 컨테이너 루팅으로 대체.
- **구현(2026-07-10)**: `EnemyController.OnDeath` → `BecomeCorpse()` — 같은 GO에 `LootContainer` + `InteractableObject.SetupAsContainer("시체 뒤지기")`(거리 기반 E). 사망 시 그로기 바 숨김 + `_nav.Stop()`(A* 리패스 잔류 방지) + `SetVisionVisible(true)`(시야 밖 사망 시 영구 투명 방지) 후 컨트롤러 disable(All 해제 — 시야/전투 판정 제외).
- **격자 크기 = 내용물에 맞춤(2026-07-10 사용자 결정 — "칸이 너무 작다")**: 고정 3×3 폐기 → `LootContainer.SetupAutoSize` — 4열 고정, 행 = 필요 칸수 올림 +1줄 여유, 2~6행 클램프. 그래도 넘치는 것만 바닥 드랍.
- **시체 가방(타르코프식, 2026-07-10 사용자 결정)**: `GameTuning.corpseBagChance`(기본 0.3) 확률로 시체에 **가방 아이템이 통째로** 들어 있음 — 가방 내부(ContainerGrid)에 `corpse` 표에서 1~2개. **가방째 드래그해 가져갈 수 있고**(중첩 컨테이너·세이브 기존 지원) 내용물은 컨테이너 팝업으로 열람. 드랍 게이트(enemyDropChance)와 독립 롤.
- ~~⚠️ 알려진 제약: 컨테이너(시체 포함)에서 드래그로 가져온 아이템은 수집 퀘스트 카운트에 안 잡힘~~ → **2026-09-11 공통 훅 `LootTake`**(`RaidManager.TrackLoot` + 수집 퀘스트)로 정리. 루팅 목록(`LootListUI`), 캐릭터 패널의 필드 상자 TAKE ALL·드래그(인벤 칸·장비 칸·가방 자동 착용) 모두 기록된다.
- 구현 순서: A+B 통합 10종 중 **2번째** (①설정 → ②**적 시체 루팅** → ③퀵슬롯 → ④무게 → ⑤소음 → ⑥투척물 → ⑦재고 회전 → ⑧시체 회수 → ⑨도감 → ⑩지도+나침반 — dev-roadmap.md 2026-07-10).

> 근거: 익스트랙션 장르 표준. 신규 시스템 0(루트 상자 재사용).

### 배그식 사망 루팅 (2026-09-11 기획 추가 · ✅ 구현 — ①②④⑤, ③은 2단계)

> 사용자: "밴딧들 죽으면 배틀그라운드처럼 바닥에 루팅 가능한 형태로 남게 기획 추가해줘" — 방향성은 [gdd-core §게임 방향성](gdd-core.md)(낙원식 돈벌이·루팅).
> 위의 시체 루팅(LootContainer · 레이드 끝까지 유지 · E "시체 뒤지기" · 드랍 테이블 + 가방)은 그대로 두고, 그 위에 더한다.

| 날짜 | 질문(선택지) | 사용자 결정 |
|---|---|---|
| 2026-09-11 | 무엇을 더할까 — 멀리서 보이는 표시 / 빠른 루팅 목록 / 지닌 장비 그대로 / 현금 드랍 (복수 선택) | **넷 다** |
| 2026-09-11 | 쓰러진 모습 — 몸 그대로 / 배그식 상자 / 몸 + 가방 | **"몸 대신 레그돌 넣어야 할 것 같은데"** → 쓰러질 때 **래그돌**(물리로 무너지는 몸)이 되고, 그 몸을 뒤진다 |
| 2026-09-11 | 현금 형태 — 현금 아이템(탈출 시 자동 정산) / 바로 스크랩 / 파는 아이템 | **현금 아이템** — 가방 1칸·무게 0. 들고 탈출해 안전가옥에 돌아오면 자동으로 ◈스크랩에 더해진다. 죽으면 잃는다 |
| 2026-09-11 | 1명당 금액 — 적게 / 보통 / 많이 | **보통** — 일반 밴딧 50~150 · 총기 밴딧 80~200 · 탱크 150~400 (10명 ≈ 1지역 낮 상자 루팅 기댓값의 약 30%) |

- ① **멀리서 보이는 표시** — 시체 위에 **가장 좋은 내용물의 희귀도 색** 빛. 다 비우면 꺼지고 라벨이 "빈 시체"가 된다. 시야콘 밖에선 숨긴다(위치 노출 방지).
- ② **빠른 루팅 목록** — 열면 목록으로 나열, 클릭 한 번에 가져오기 + "전부 가져가기". 인벤이 슬롯형(2026-09-09)이라 잘 맞는다. → **2026-09-11 모든 필드 상자로 확장**(`LootListUI`): 처음 열면 옛 수색 연출로 하나씩 드러나고, 가치(◈) 표기는 뺐다([region-loot.md §루팅 정리 결정](region-loot.md)).
- ③ **지닌 장비 그대로** — 적이 쓰던 무기(방망이·권총·소총)와 입은 장비가 시체에 들어 있다. 총·탄약은 2026-09-11 "나중에" 결정과 묶어 **2단계**(소총 아이템이 아직 없다).
- ④ **돈 드랍** — ⚠️ **2026-09-11 현금 → 고철 화폐 아이템(`scrap_money`, "◈ 고철")으로 교체**(재화 역할 결정: 현금은 퀘스트·이벤트 전용, [economy.md §적 고철 드랍](economy.md)). 형태·금액은 그대로 — 원래 기록: 현금 아이템(가방 1칸·무게 0, 탈출 후 귀환 시 ◈스크랩 자동 정산, 사망 시 손실). 금액 일반 50~150 · 총기 80~200 · 탱크 150~400. 기준선은 [economy.md](economy.md)에도 기록.
- ⑤ **래그돌** — 사망 순간 기울이는 연출(현재 `BanditEnemyVisual` 사망 85° 기울기) 대신 물리 래그돌로 무너진다. 몸이 멈추면(또는 수 초 뒤) 고정하고 뒤지기 상호작용을 붙인다. 3D 밴딧 리그(SimpleHero_Rig) 기준.
- 구현 순서: 플레이어 총격(에임·반동·총기/탄 스펙) 다음.

**구현 (2026-09-11 — ①②④⑤, ③은 2단계)**
- ① `Combat/CorpseMarker` — 시체 루트에 붙는다. 얇은 빛기둥(2.2m, 조명 무관 Unlit — 밤에도 읽힘) + 작은 점광. 색 = 내용물(가방 속까지) 최고 희귀도의 `RarityColor`. `LootContainer.Grid.OnChanged`로 갱신, 비면 꺼지고 라벨 "빈 시체". 0.2초마다 `PlayerVision.CanSee`로 시야콘 밖이면 숨김. 래그돌이 밀려나면 엉덩이 위로 따라간다.
- ② `UI/LootListUI` (2026-09-11 `CorpseLootUI`를 대체 — [region-loot.md §루팅 정리 결정](region-loot.md)) — `InteractableObject.HandleContainer`가 **모든 필드 상자와 시체**에 연다(창고·보관함은 캐릭터 패널). 화면 오른쪽 목록, 행 = 희귀도 색 + 이름(**가치(◈) 표기 없음** — 사용자). 처음 여는 상자는 옛 수색 연출대로 칸이 "? ? ?"로 가려졌다가 위에서부터 하나씩 드러난다(희귀도별 `GameTuning.searchSec*` ÷ `searchSpeedMult`, 닫아도 진행은 `LootContainer.RevealedCount`에 남음). 다 드러나면 클릭/E 1개, [전부 가져가기 F], [자세히 Tab](캐릭터 패널), Esc·3m 이탈 시 닫힘. 가져오기는 `LootTake`(`RaidManager.TrackLoot` + 수집 퀘스트 카운트)를 탄다. `UIManager`(Esc·CloseAll·IsAnyUIOpen)에 등록.
- ④ 고철 화폐 = `Items/Valuable/ScrapMoney.asset`(itemId `scrap_money`, 1개 = ◈1, 무게 0 — 2026-09-11 현금에서 교체) — `EnemyController.AddScrap`이 `UnitStatData.cashMin~cashMax`만큼 시체에 넣는다. `RaidManager.OnExtractSuccess`가 XP 정산 **뒤**·세이브 커밋 **전**에 `ScrapWallet.SettleFromInventory`로 인벤(가방 속 가방까지)의 고철 화폐를 ◈로 바꾼다. 현금(`cash`)은 정산하지 않는다.
- ⑤ `Combat/BanditRagdoll` — 사망 시 `BanditEnemyVisual.Die(push)`가 애니메이터를 끄고 뼈 11개(엉덩이·가슴·머리·양팔 2마디·양다리 2마디)에 강체·콜라이더·`CharacterJoint`를 런타임으로 붙인다. 크기는 몸 높이 비례, 뼈의 FBX 100배 스케일은 월드 거리로 재 로컬로 환산. 한 몸의 부위끼리는 충돌 무시(겹침 폭발 방지). 레이어 12 `Corpse` — 플레이어·적과 충돌 안 함(런타임 `IgnoreLayerCollision`). "플레이어 → 적" 방향으로 밀려 넘어지고, 멈추거나 2.5초 지나면 굳혀 콜라이더를 끈다(총알·시야 판정에 안 걸리게). 뼈를 못 찾으면 예전 기울기 연출.
- 준비 도구: `tools/setup_corpse_loot.cs`(현금 아이템 · StatDB 현금 · 레이어 이름, 재실행 가능).

---

## 유인 (던진 물건) — 소음 시스템 폐기 후 (2026-09-09)

> **소음 시스템은 폐기됐다.** 걷기·달리기·웅크림의 지속 소음, 타격·총성·문 소음, HUD 귀 아이콘 —
> 전부 삭제(볼륨 축소 결정, [`scope-cut.md`](scope-cut.md)). **적 발견은 시야가 유일한 축이다.**

- **남은 것 = `Distraction`(정적, 47줄)** — 던진 물건이 떨어진 지점만 등록된다. `Report(pos, radius, duration)` / `TrySense(listenerPos, out src)`.
- **유일한 발생원 = 투척물 착탄**(`ThrowSystem`). 이동·타격·총성·문은 이제 아무 소리도 내지 않는다.
- **적 반응** — `EnemyController.State.Investigate`는 그대로: `UpdatePatrol`/`UpdateInvestigate`에서 `TrySense` → 지점 이동 → `noiseInvestigateLook`(2.5s) 두리번 → 순찰 복귀. 도중 시야(`DetectRng`) 발견 시 Chase. 머리 위 '?'(`ShowAlertMark`).
- **수치** — `GameTuning.throwNoiseRadius`(유인 반경 9m) · `noisePulseDuration`(유효 시간 0.6s) · `noiseInvestigateLook`(2.5s). 이름은 SO 직렬화 값 보존을 위해 그대로 뒀다.
- **테스트**: F1 플레이어 탭 — "발밑에 유인 발생" 버튼.

### 삭제된 것
`Combat/PlayerNoise.cs`(102줄) · `Combat/NoiseSystem.cs`(67) · `UI/NoiseHUD.cs`(귀 아이콘 HUD).
GameTuning `noiseIdle/Crouch/Walk/Run/Attack/Door/UiMax`, `barricadeNoiseRadius`, `WeaponData.noiseRadius`,
`ItemData.partNoiseMult`(소염기 소음 배율), `PlayerEquipment.WeaponPartNoiseMult`.
**잠행 특성 `move_noise`는 물릴 시스템이 사라졌다** — 특성 축소(scope-cut 5번)에서 정리 대상.
**바리케이드 강제 돌파의 대가도 시간뿐**이 됐다(예전엔 소음으로 적이 몰려왔다).

---

## 투척물 (✅ 2026-07-11 구현 완료 — 돌 유인)

> 질문: 근접 전투에 원거리 상호작용을 어떻게 최소로 넣나. **결정: 투척물 1차 = 돌 1종 (유인 전용).**

- **동작**: 조준 지점 착탄 → **유인 등록** → 반경 내 적이 **조사(Investigate) 이동**. 데미지 0 — **유인 전용**.
- **2026-09-09 이후**: 소음 시스템이 사라져서 이게 **적을 끄는 유일한 수단**이 됐다.
- **✅ 구현(2026-07-11)**: **G키**(기본 돌) 또는 **퀵슬롯 등록→숫자키/클릭**(투척물, 비안전구역·모달 없음) → 조준 모드(**사거리 원 + 커서 착탄 마커**, 사거리 밖=원 경계로 클램프) → **좌클릭** 착탄 / **우클릭·ESC** 취소. 착탄 순간 `Distraction.Report(착탄, throwNoiseRadius, noisePulseDuration)` → 반경 내 적 조사 이동. 투척물 1개 소모, 조준 중 좌/우클릭 공격은 차단. 비행은 **거리비례 일정 속도**(`throwSpeed`, 착탄까지 = 거리/속도 0.15~1.0s 클램프 — 눈에 보이는 포물선). 파일: `Combat/ThrowSystem.cs`(셀프부트 싱글턴, `TryEnterAim(itemId)` 공용) · 아이템 `Resources/Items/Misc/Stone`(`ItemData.isThrowable`) · 수치 `GameTuning.throwRange/throwNoiseRadius/throwSpeed`. 퀵슬롯 등록 허용(`QuickSlotBar.IsAssignable`에 투척물 추가). F1 디버그 "돌 5개 지급".
- ~~전제 = 소음 시스템~~ → **`Distraction`로 대체**(2026-09-09). 적 '조사' 상태는 그대로 살아 있다 — 상세는 위 **§유인**.
- 구현 순서: A+B 통합 10종 중 **6번째** — 소음(5번째) 직후 동반 (①설정 → ②적 시체 루팅 → ③퀵슬롯 → ④무게 → ⑤소음 → ⑥**투척물** → ⑦재고 회전 → ⑧시체 회수 → ⑨도감 → ⑩지도+나침반 — dev-roadmap.md 2026-07-10).

> 근거: 좀보이드식 유인 — 근접 전투 게임에서 잠행 플레이를 성립시키는 유일한 원거리 수단.

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| **2026-09-11** | **시체 루팅 현행화 (루팅 정리, 결정 = [region-loot.md §루팅 정리 결정](region-loot.md)).** `EnemyController.DropLoot` → 실제 이름 `RollLoot`. 폴백 = 활성 지역 바닥 티어 → **이 씬 루팅 지역의 `corpse` 종류 표**(`MapSpawnController.CurrentRegionId`), 시체 가방도 같은 표. ② 빠른 목록 `CorpseLootUI` → **`LootListUI`**(모든 필드 상자·시체, 옛 수색 연출, 가치 표기 없음). 가져오기 기록 = `LootTake`(알려진 제약 해소). §배그식 사망 루팅 제목의 "구현 대기" → 구현 완료 표시. |
| **2026-09-11** | **전환·방향·총 데이터·그로기 결정 (§무기 구성 결정).** 질문 4개 → 사용자: 무기 전환 = **퀵슬롯 숫자키** · 근접 방향 = **공격 순간 커서 쪽** · 총 수치 = **`WeaponData` 한 곳으로 전부**(밴딧 포함) · 그로기 = **데이터 유지**(80·160, 문서가 틀렸던 것). |
| **2026-09-11** | **밴딧 돈 드랍 = 현금 → 고철 화폐.** 재화 역할 결정(주 재화 = 고철, 현금 = 퀘스트·이벤트 전용)에 따라 사망 루팅 ④를 고철 화폐 아이템(`scrap_money`)으로. 결정·구현은 [economy.md §재화 역할 · §적 고철 드랍](economy.md). |
| **2026-09-11** | **그로기 조정 (사용자 "그로기 조정해주고", 값 위임).** 데이터 값(약공 5 / 밴딧 80·감쇠 10)에선 약공만으로 기절이 절대 안 쌓였다. → 플레이어 약공 5→**14**(콤보 2·3단 6·8→16·20)·강공 25→**32**·풀차지 45→**58**(StatDB + `Bat_*` 공격 에셋 4개) / 일반 밴딧 3종 80·10→**60·5** / 중장 160·8→**120·4** / `bandit_ranged` 55·12→**45·6**. 기절 시간은 그대로. 일반 밴딧 = 약공 6대·권총 5발. |
| **2026-09-11** | **총 수치 `WeaponData` 한 곳으로 통합 (사용자 "한 곳으로 전부").** 총기 밴딧이 총 종류 enum + 절대 수치(`attackDamage`=탄 피해, `projectileSpeed`)를 따로 들던 것을 → StatDB `rangedWeaponData`(플레이어와 같은 에셋) + 적 전용 `rangedDamageMult`·`rangedBulletSpeedMult`로. 배율은 **기존 값이 그대로 나오게**(권총 14×0.643=9 / 42×0.381=16, 소총 12×0.5=6 / 50×0.4=20). 밴딧 모델도 총의 `firearmStance`로. **`Weapon_Rifle545` 신설**(5.45x39·연사 600rpm·피해 12·탄속 50·사거리 20 — **가안**, 플레이어 소총 아이템은 아직 없음). 거리 감쇠 상수 → `GameTuning.gunFalloffStart/EndMult`. `WeaponData`를 공통/근접/총 칸으로 정리 + `WeaponDataEditor`(해당 칸만 표시). 검증: Zone1에서 권총 밴딧 탄 9.00·16.00, 소총 6.00·20.00(통합 전과 같음), 근접 밴딧은 근접 그대로. |
| **2026-09-11** | **근접 = 주력으로 정정.** 사용자: "근접도 주력이긴 해, 좀보이드처럼" — 바로 아래 행의 "근접 = 보조 무기"를 **총과 근접 둘 다 주력**으로 고쳤다. |
| **2026-09-11** | **구르기 꺼 둠.** 사용자: "구르기는 일단 꺼둬" — 모션이 없어 미끄러지기만 하므로. `GameTuning.dodgeEnabled`(기본 false)로 입력을 막고 코드·특성은 그대로 둔다. 모션 제작 후 켠다. |
| **2026-09-11** | **무기 구성 결정 (§무기 구성 결정, 시스템 정리 2단계).** 질문: 근접 무기를 남길지 / 구르기를 남길지 → 사용자: "근접 무기는 남기고 구르기도 유지하자, 근데 구르기는 모션이 없어서 일단 보류". 총 = 주무기, 근접 = 보조 무기. 구르기 시스템은 두되 모션 전까지 다듬지 않는다. |
| **2026-09-11** | **맨손 공격 폐기 + 그레이박스 주먹·총 삭제.** 사용자: "기존에 쓰던 주먹이랑 총 가짜 모델 다 없애줘, 우리 이제 근접 손공격은 없어". 무기가 없으면 좌·우클릭 공격을 받지 않는다(구르기는 유지). `FistVisual`·`GunVisual` 삭제 — 총은 3D 총기 모델. 근접 그레이박스 칼(`MeleeWeaponVisual`)은 유지. |
| **2026-09-11** | **총격전 결정 (§총격전).** 질문: 에임 표시 / 반동 / 탄 스펙 / 조준 시 카메라 → 사용자: **커서 조준원**, 반동은 **퍼짐 누적만**(총마다 스펙), 탄 스펙 **데미지·관통·탄속/사거리·반동/퍼짐 배율 넷 다**, 카메라는 **조준할 때만** 커서 쪽. |
| **2026-09-11** | **배그식 사망 루팅 기획 추가 (§적 시체 루팅 ▸ 배그식 사망 루팅, 구현 대기).** 질문: 무엇을 더할까 / 쓰러진 모습 → 사용자: 멀리서 보이는 희귀도 표시 + 빠른 루팅 목록 + 지닌 장비 그대로(총·탄약은 2단계) + 현금 드랍, 쓰러지면 **래그돌**. 방향성 = 낙원식 RPG 돈벌이·루팅([gdd-core.md §게임 방향성](gdd-core.md)). |
| **2026-09-11** | **총기 밴딧 레이드 배치 (§총기 밴딧).** 질문: 종류·배치·사격 방식·드랍 → 사용자: 권총+소총, Zone1 일부 존 교체, 조준 경고 후 피할 수 있는 총알, 드랍은 기존 밴딧과 같게(탄약·총은 나중). 구현: `UnitStatData` 원거리 필드, `EnemyController` 원거리 분기(조준 고정 0.2초 → 실제 `Projectile`), `BanditEnemyVisual` 총기 모드, Zone1 존 4개(9기). 3점사 간격 0.4초(플레이어 피격 무적창 0.35초). [→ bandit-firearms.md](bandit-firearms.md) |
| **2026-09-09** | **소음 시스템 전면 폐기 (볼륨 축소 2/6).** 질문: "타르코프식 하드코어 요소 중 뭘 자를까" / 사용자 결정: **소음 삭제**. `PlayerNoise`(102줄)·`NoiseSystem`(67)·`NoiseHUD` 삭제, 이동·타격·총성·문 소음 전부 제거, GameTuning 소음 필드 8종 + `barricadeNoiseRadius` + `WeaponData.noiseRadius` + `ItemData.partNoiseMult` 제거. **적 발견 = 시야 단일 축.** 단, 투척물은 유지 결정이라 유인 수단이 사라지면 돌이 무의미해져 **`Distraction`(47줄, 착탄 지점만 등록)** 을 남겼다 — 적 `Investigate` 상태는 그대로. 총의 대가는 탄약 유한성만 남음. 잠행 특성 `move_noise`는 물릴 곳이 없어져 특성 축소에서 정리 예정. [→ scope-cut.md](scope-cut.md) |
| 2026-07-11 | **투척물 후속 조정 2건 (사용자 피드백).** ①**비행 너무 빠름 → 거리비례 일정 속도**: `throwFlightTime`(고정 0.32s) 폐기 → `throwSpeed`(m/s, 기본 10) 신설. 착탄까지 = 거리/속도(0.15~1.0s 클램프) → 가까우면 짧게·멀면 오래 = 일정한 눈에 보이는 포물선. ②**투척물도 퀵슬롯 등록**: `QuickSlotBar.IsAssignable`에 `isThrowable` 추가(드래그·클릭·숫자키 공용) → 퀵슬롯 발동 시 '사용' 대신 `ThrowSystem.TryEnterAim(id)`로 조준 진입(좌클릭 착탄에서 소모). G키·퀵슬롯 공용 `TryEnterAim(itemId)`. | 근거: 던지는 손맛 + 접근성. |
| 2026-07-11 | **소음 시스템 2건 변경 (사용자 요청).** ①**월드 원형 VFX 전면 제거 → HUD 귀 아이콘만.** 발밑 반투명 링(`PlayerNoise` 링)·펄스 파문(`NoiseRipple`)을 제거하고 `NoiseHUD`를 막대→**귀 모양 아이콘**(레벨 따라 색/밝기/음파)으로 교체. "표기하는 정도만". ②**공격 스윙 소음 제거 → 타격 성공 시에만.** `TopDownPlayer` 약공/강공 시작의 `PlayerNoise.AttackNoise()` 제거 → `AttackPerformer.ScanWindow` 적중(landed) 시 `PlayerNoise.Pulse(impact, noiseAttack)` 발생. AttackPerformer가 플레이어·적 공용이라 "때리거나(플레이어 적중)·맞거나(플레이어 피격)" 모두 impact 소음. 헛방·스윙은 무음. 이동·문·돌 착탄 소음은 유지. | 근거: 시각 노이즈 감소 + 소음이 실제 타격에서만 나도록(스텔스 정합). |
| 2026-07-11 | **⑥ 투척물 구현 완료 (돌 유인) — 갭 분석 통합 순서 6번째.** 질문(UI): 조준·발동을 어떻게 표현하나. **결정: 조준 = 사거리 원 + 커서 착탄 마커(사거리 밖 클램프) / 발동 = 전용 키 G.** 구현: `Combat/ThrowSystem.cs`(셀프부트 싱글턴 — PlayerNoise 패턴, executionOrder 100으로 TopDownPlayer 뒤에서 실행해 throw 프레임 공격 중복 차단) — G(돌 보유·비안전구역·비모달)→조준→좌클릭 착탄→`PlayerNoise.Pulse`로 착탄 소음→적 조사. 우클릭/ESC 취소, 돌 1개 소모, 데미지 0. 신규 아이템 `Resources/Items/Misc/Stone.asset`(`ItemData.isThrowable` 필드 신설, category=Misc, maxStack 10). 수치 `GameTuning`(throwRange 8 / throwNoiseRadius 9 / throwFlightTime 0.32). 조준 중 좌·우클릭 공격 차단(TopDownPlayer.HandleCombatInput 가드). F1 디버그에 "돌 5개 지급". 소음 시스템(⑤)이 이미 적 조사 상태를 처리하므로 유인은 착탄 펄스만으로 성립. | 근거: 좀보이드식 유인 — 잠행 플레이 성립. combat.md §투척물 ✅. |
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

## 2026-07-11 — 전투 체감 재정립 (사용자 피드백: "때리기·맞기·범위가 다 이상하다")

> 질문: 전투가 어색한데 처음부터 재작성할까? **결정: 전면 재작성 안 함 — 원인이 코드 구조가 아니라 ①데이터 ②국소 결함이라 재작성하면 데이터 원인이 그대로 재생산됨.** 대신 판정·수치·피격반응·적 거리제어를 전부 재정립.

### 진단 (전수 조사)
공격 판정의 **진실원 = StatDB 수치 + `TopDownPlayer.MakeAttack()` 하드코딩 공식**. `PlayerRig.prefab`의 `lightCombo/heavyAttack/heavyFullAttack`이 전부 null이고 WeaponData 에셋이 0개라 **100% 자동 생성 경로**로 돈다. `Resources/Attack.asset`은 아무도 참조 않는 고아 에셋.

### 확정 원인 → 조치

**데이터 (StatDB)**
| 값 | 전 | 후 | 이유 |
|---|---|---|---|
| `playerStat.moveSpeed` | 1 | **4** | 적 2.5보다 느려 **카이팅·거리조절이 물리적으로 불가**했음. "맞는 게 이상"의 1순위 |
| `sprintSpeedMultiplier` | 2.1 | 1.6 | 이동속도 인상분 상쇄(6.4 m/s) |
| `lightRange` / `heavyRange` | 2 / 2.5 | **1.2 / 1.7** | 근접 사거리로 축소 |
| `lightCooldown` | 0.4 | **0.12** | 1타마다 정지 → 콤보 연결 |
| `dodgeInvincibleDuration` | 0.2 | **0.26** | 구르기 0.3초 중 뒷부분이 무방비였음 |

**코드**
1. **약공 3타 콤보 부활** — 선입력 예약 분기가 `_state != Idle → return` **뒤**에 있어 `_state==LightAttack`일 때 **도달 불가 코드**였다 → `_comboBuffered`가 영원히 false → 항상 1타만. 분기를 얼리 리턴 **앞으로** 이동.
2. **히트박스 리치/폭 분리** — 구 `offset=range*0.5, boxSize=(range, range*0.75)`는 ①사거리를 키우면 폭까지 커지고 ②박스 근접변이 플레이어 원점에 붙어 **옆(90°)·뒤 적까지 정면 판정**에 들어왔다 → `offset=BodyRadius+range*0.5`, 폭은 사거리와 독립.
3. **스윙 방향 고정** — `AttackPerformer`가 매 프레임 facing을 새로 읽어 히트박스가 **마우스를 실시간 추종** → 스윙 중 마우스를 돌리면 **등 뒤 적까지 맞았다**. `Perform()` 시점 스냅샷으로 고정.
4. **적 공격 런지 제거** (사용자 지적 "때릴 때 앞뒤로 움직인다") — Rigidbody2D 위에서 transform을 원위치로 하드 스냅해 고무줄처럼 튕겼다. 제자리 공격 + 히트 표기만.
5. **적 공격 판정 정합** — 구 `AtkRange*1.5` **원형(360°)** → 사거리 일치(×1.05) + `_attackDir` 기준 **±60° 정면 제한**(뒤로 돌아가도 맞던 문제).
6. **플레이어 피격 무적창 0.35s**(`Health`) — 무적이 구르기 중에만 있어 여럿에게 겹쳐 맞으며 "모르게 갈렸다". DoT(silent)는 예외.
7. **그로기 보상 복구** — 스턴 중 피격이 `state=Hit`로 덮여 **첫 타격에 스턴이 풀리고 반격**당했다 → 스턴 중엔 연출만.
8. **적 정지거리(standoff)** — 쿨다운 중에도 전속으로 파고들어 겹쳤다(양쪽 Dynamic RB라 플레이어가 떠밀림) → 사거리 0.85배에서 정지.
9. **약공 히트스탑 부여**(0.03~0.05) — 구 `hitstop:0`이라 약공은 히트스탑·셰이크·줌펀치가 **전부 미발동**("때려도 반응 없음").
10. **구르기 감각** — 등속(순간이동 느낌) → 속도배율 1.35→0.5 감쇠(거리 유지, 강한 시작 + 부드러운 착지).
11. **HeavyCharge 교착 해소** — 버튼 뗀 프레임을 놓치면 차징에 영구 고착(이동 0.4배·스프린트/약공 불가)되던 것 → 버튼이 이미 풀렸으면 발동 + 최대차징 2배 타임아웃.
12. **캔슬 보너스 무효화 수정** — `TryCancelAttack`이 준 1.5배 경직을 직후 `OnDamaged`가 덮어써 항상 무효였다 → 보호 창(0.05s).
13. **판정 잔류 방지** — 캔슬·사망 시 `_performer.Cancel()` 누락 → 추가.
14. **넉백 방향 버그** — 플레이어 피격 시 `dir = self−self = 0` → `Vector3.down` 폴백이라 **항상 아래로** 밀렸다 → 플레이어 넉백 제거, 적 넉백은 `Rigidbody2D.MovePosition` 경로로.
15. **스포너 폴백 키** `bandit_melee` → `bandit_melee_1`.

### ⚠️ 남은 것
- **씬의 `unitKey`가 아직 `bandit_melee`**(`Zone1.unity`/`ScrapMarket_GB.unity`) → StatDB 미스로 적이 **인스펙터 폴백**(HP 40→**100**, 데미지 10→**15**)으로 돈다. **씬 재빌드 필요**(빌더는 이미 수정됨).
- 벽 관통 판정(LOS 체크 없음) · 적끼리 분리(separation) 없음 · `SkeletonAnimController` 스텁(`IsAnimComplete=true`)이라 animController 붙은 적은 피격 경직·공격 후딜이 0프레임으로 무력화됨.

### 2026-07-11 (2차) — 적 3종화 + 잔여 판정 구멍

**적 유닛 3종 확립** (구: 실질 1종이라 위험 곡선이 마릿수로만 났음)
| 유닛 | 성격 | HP | 데미지 | 사거리 | 예비동작 | 이속 | 그로기 | XP |
|---|---|---|---|---|---|---|---|---|
| `bandit_melee_1` | 기본 근접 | 40 | 10 | 1.5 | 0.7 | 2.5 | 80 | 10 |
| `bandit_ranged` | **견제형**(멀리서 찌름·물몸) | 28 | 8 | **3.4** | 0.95 | 2.0 | 55 | 14 |
| `bandit_tank` | **중장형**(느리고 단단·캔슬 불가) | **110** | **18** | 1.9 | **1.15** | 1.9 | **160** | 28 |

- 구분은 tint로: 근접=살구, 견제=연녹, 중장=갈색(+scale 2.6). ※약탈자 보랏빛과 겹치지 않게 선택.
- Zone1 배치(위험 곡선): 아케이드/폐아파트=근접 → 식물원 돔=근접+**견제 1** → **유리타워=견제 2 + 중장 1**(최심부).

**판정 구멍 2건 추가 수정**
- **벽 관통 차단** — `AttackPerformer`에 LOS 검사 신설. 공격자↔대상 사이 **비트리거 솔리드**가 있으면 무효(트리거인 루트 상자·존은 통과, Player/Enemy 레이어는 서로를 막지 않음). 예전엔 벽 너머 사거리 안이면 그냥 맞았다.
- **적끼리 분리(separation)** — 반발이 없어 여러 마리가 한 점에 겹쳐 한 덩어리로 밀려들었다 → 반경 1.1m 안 동료로부터 거리 반비례 반발을 추격 방향에 혼합.

## 2026-07-11 (후속) — "사거리 안인데 안 때리는 적" 2건

> 사용자 보고: *"적이 지금 아군 공격범위 들어오면 공격해야 하는데 안 하는 경우도 있더라."*

**① 분리(separation)가 사거리 안에서도 작동해 서로 밀어냈다 — 같은 날 추가한 기능의 역효과**
- 같은 날 넣은 적끼리 반발(반경 1.1m)이 **거리와 무관하게 항상** 추격 방향에 섞였다.
- 플레이어 옆에 둘이 모이면 서로 밀어내는 성분이 접근 성분을 상쇄해 **둘 다 사거리 밖(1.5m 부근)을 맴돌고**, 쿨다운이 끝나는 프레임에 `dist <= AtkRange`가 아니라서 **아무도 공격하지 않는다.** 한 마리일 땐 안 나타나므로 "경우에 따라"로 보였다.
- **수정**: 분리 가중치를 거리로 페이드 — `dist <= AtkRange`면 **0**(정직한 직진), `2×AtkRange` 이상에서 최대(0.6). 합성 벡터도 크기 1로 클램프해 분리가 추격을 이기지 못하게.
- 원래 목적(겹쳐서 한 덩어리로 밀려오는 것 방지)은 접근 구간에서 그대로 유지된다.

**② 플레이어 참조를 Start에서 한 번만 잡았다**
- `GameObject.FindGameObjectWithTag("Player")`를 `Start()`에서 1회만 호출. 그 시점에 플레이어가 없었거나(씬 로드 순서) 이후 재생성되면 그 개체는 **탐지·추격·공격을 영영 한 번도 안 한다** — 옆에 서 있어도 순찰만 돈다.
- **수정**: `AcquirePlayer()` 분리 + 참조가 비면 **0.5초 간격 재획득**.

> 교훈(이 저장소 반복 패턴): "값이 조용히 무효가 되는" 함정 — 한 번만 캐시한 참조, 나중에 덮어쓰이는 인스펙터 기본값. `.claude/skills/전투/SKILL.md` 참조.

## 2026-07-11 — 그레이박스 칼 휘두르기 (`MeleeWeaponVisual`)

> 사용자: *"밴딧이나 플레이어나 때릴 때 표기가 없는데 칼 스프라이트 하나 만들어서 휘두르기 가능할까? … 콤보는 휘두르기 각도를 살짝씩 바꿔서 3연타 느낌, 강공격은 기 모을 때 칼을 살짝 대각에 뒀다가 놓으면 빠르게 휘두르고, 테일 이펙트 비슷한 거 있으면 더 좋겠다"*

스파인이 들어오면 통째로 교체할 임시 연출. **판정에는 일절 관여하지 않는다** — 히트박스는 `AttackPerformer` 그대로, 이건 보여주기만 한다. (연출이 판정을 건드리기 시작하면 "보이는 것과 맞는 것"이 어긋나 디버깅이 지옥이 된다.)

구조: `owner ─ Pivot(회전) ─ Blade + Guard + Tip(TrailRenderer)`. Pivot의 Z회전 = **바라보는 각 + 스윙 오프셋**이라 8방향 어디를 보든 똑같이 동작한다.

| 동작 | 궤적(바라보는 방향 기준) |
|---|---|
| 평상시 | −38° (몸 옆에 내림) |
| 콤보 1타 | +78° → −42° (위에서 아래 사선) |
| 콤보 2타 | −72° → +55° (아래에서 위 역사선) |
| 콤보 3타 | +105° → −105° (크게 횡베기) |
| 강공 차징 | +128° 고정 + **떨림**(차징률에 비례해 폭 증가) |
| 강공 릴리스 | +128° → −88°(풀차지 −108°), 짧고 빠르게 |

- 스윙 보간은 **ease-out cubic** — 시작이 가장 빠르다(칼이 '터지듯' 나가는 느낌). 지속은 `AttackData.Duration`의 0.6~0.75배라 판정 창과 어긋나 보이지 않는다.
- **테일**: 칼끝 `TrailRenderer`(0.16초, 폭 감쇠, 알파 0.55→0). 스윙 중에만 emitting.
- **적도 같은 칼을 쓴다.** 예비동작이 "붉은 점멸"만이던 걸 **칼을 치켜든 자세 + 떨림**으로 바꿔, 캔슬을 노릴 타이밍이 눈에 보이게 했다.

### 같은 함정을 또 밟지 않으려고 미리 막은 것
칼은 몸통 스프라이트의 **자식이 아니라 루트의 자식**이라, `SetVisionVisible`이 몸통 SpriteRenderer만 꺼도 **칼만 어둠 속에 떠 있게** 된다 — "적" 라벨이 그랬던 것과 똑같은 구조다. `SetVisible(bool)`을 만들어 시야 토글·사망 시 함께 끈다.

> 교훈: **루트에 붙인 자식 렌더러는 FOV 토글에서 항상 누락된다.** 새 연출을 붙일 때마다 `SetVisionVisible` 목록을 함께 갱신할 것.

## 2026-07-11 (후속) — 스윙 단일화 · 적 강공 AI · 적 2종 · 그로기 실동작

> 사용자: *"휘두를 때 우측에서 좌측으로 / 적군은 나를 바라보고 휘두르지 않고 있으니 버그 확인 / 콤보는 일단 빼줘 / 강공격은 우측 칼이 좀 더 뒤로 가서 기 모으다가 놓으면 빠르게 좌측으로 / 적군들도 AI 넣어서 강공격 확률·상황 / 강한 적군(큰 애들) 넣고 공격 딜레이 길게 / 그로기 수치 채워지면 그로기에 빠지게 / 적군 종류 2가지, 색상·크기로 구분"*

### 스윙 = 우 → 좌 한 방향, 콤보 OFF
- 약공 −72° → +72°, 강공 −122° → +82°(풀차지 +104°). **모두 우측에서 좌측.**
  전엔 콤보 단계마다 궤적이 달라 타격 리듬이 안 읽혔다.
- 강공 차징은 −122° = **오른쪽 뒤로 더 당긴 자세** + 차징률 비례 떨림. 릴리스는 약공보다 **짧은 시간에 더 큰 각**을 지나 눈에 '빠르게' 읽힌다.
- 콤보는 **`GameTuning.comboEnabled`(기본 OFF)** 로 게이트. `AttackComboData`는 그대로 두고 '진행'만 막아서 켜면 그대로 되살아난다.

### ★ 그로기가 실제로는 한 번도 안 차고 있었다
배선(`AttackPerformer → Hurtbox.ReceiveHit → EnemyController.TakeHit → AddGroggy`)은 멀쩡했는데 **수치가 불가능했다**:
- 약공 groggy **5** / 밴딧 maxGroggy **80** / 감쇠 **10/s**
- 약공 1회 사이클 ≈ 0.62초 → 초당 +8 vs **감쇠 −10** → **영원히 순증이 안 됨.**

수치 재조정(임의값, 사용자 위임):
| | 구 | 신 |
|---|---|---|
| 플레이어 약공 groggy | 5 | **14** |
| 강공 / 풀차지 | 25 / 45 | **32 / 58** |
| 일반 밴딧 maxGroggy·감쇠·스턴 | 80 / 10 / 2.5s | **45 / 5 / 2.0s** |
| 강한 밴딧 maxGroggy·감쇠·스턴 | 160 / 8 / 2.0s | **95 / 4 / 2.6s** |

> ⚠️ **2026-09-11 확인: 위 "신" 값은 StatDB에 한 번도 반영되지 않았다** — 데이터는 지금도 "구" 열(5·25·45 / 80·10·2.5 / 160·8·2.0)이다. 사용자 결정: **데이터 유지**, 문서를 데이터에 맞춘다(§그로기 (적)). → 같은 날 **조정**(사용자 "그로기 조정해주고") — 현재 값은 §그로기 (적).

→ 일반은 약공 **4타**, 강한 놈은 **7타** 정도에 그로기. 강공을 섞으면 훨씬 빨라진다.

### ★ StatDB의 scale/tintColor가 한 번도 안 읽히고 있었다
데이터에는 유닛별 크기·색이 있는데 **아무도 적용하지 않아 모든 적이 같은 크기·같은 붉은색**이었다("적군 종류를 구분할 수 없다"의 정체). `ApplyUnitLook()` 신설:
- `scale`은 절대값이 아니라 **일반 적(2.0) 기준 배율**로 해석 — 루트에 곧바로 곱하면 콜라이더까지 2배가 돼 물리가 통째로 바뀐다. **몸통 스프라이트·바디 콜라이더·허트박스·칼만** 비례 확대.
- 약탈자(보랏빛 식별색)는 덮어쓰지 않는다.

**적 2종으로 정리** (`bandit_ranged`는 진짜 투사체가 없어 '리치 긴 근접'일 뿐이라 스폰에서 제외):
| | 일반 밴딧 | **강한 밴딧** |
|---|---|---|
| 크기 | ×1.0 | **×1.6** |
| 색 | 붉은색 | **짙은 갈색** |
| HP / 데미지 | 40 / 10 | 110 / 18 |
| 예비동작 | 0.7s | **1.55s**(느리고 무겁게) |
| 캔슬 | 가능 | 불가 |

### 적 강공 AI
공격 시작 시 강공/약공을 고른다.
- `enemyHeavyChance`(0.3) 확률 — **또는** 약공을 `enemyHeavyForceAfter`(3)회 연속 낸 뒤엔 **반드시** 강공. 확률만 두면 한 판 내내 강공이 안 나오는 경우가 생긴다.
- 캔슬 불가 유닛(강한 밴딧)은 **항상** 강공.
- 강공: 예비동작 ×1.9 / 데미지 ×1.9 / 리치 ×1.20 / **캔슬 불가**.
  길게 예고하는 대신 확정으로 나가는 게 압박이다 — 예비동작이 길다고 공짜로 끊기면 강공이 그냥 손해가 된다.
- 예고 구분: 강공은 **주황색·느린 점멸**(9Hz) + 칼을 끝까지 당긴 자세, 약공은 붉은색·빠른 점멸(15Hz) + 얕은 자세.

### "적이 나를 바라보고 휘두르지 않는다"
코드 경로상 적의 칼은 매 프레임 플레이어 방향으로 갱신되고(`UpdateWeaponVisual`) 공격 순간 다시 고정된다(`DoAttack`) — 정적으로는 재현되지 않았다. 다만 **애초에 적이 공격 상태에 못 들어가고 있었을 가능성**이 크다(같은 날 고친 분리 벡터 문제 + 플레이어 참조 유실). 이번 판에서 강공 예고가 눈에 보이므로 재현 여부를 다시 확인할 것.

### 노브
`comboEnabled` / `enemyHeavyChance` / `enemyHeavyForceAfter` / `enemyHeavyWindupMult` / `enemyHeavyDamageMult`

## 총기 — 2026-07-29 결정 (플레이어 먼저)

> **질문 3건과 사용자 결정**
> ① 탄약/장전 모델 → **탄창이 아이템(타르코프식)**. 탄창 자체가 인벤 아이템이고 탄을 담고 있다. 장전 = 탄창 교체. 남은 탄이 든 탄창은 **그대로 보존**된다.
> ② 명중 판정 → **투사체**. 총알이 실제로 날아간다(히트스캔 아님).
> ③ 우클릭 → **조준(정밀 사격)**. 누르고 있으면 탄퍼짐↓·시야콘↑, 이동속도↓.

### 왜 이 조합인가
- **투사체**여야 맵에 깔아 둔 엄폐(차량·잔해·컨테이너)가 *진짜로* 막아 준다. 히트스캔은 "선이 막혔나"로만 작동해서, 둘레형 블록·야적장 컨테이너 열처럼 공들여 만든 엄폐가 의미를 잃는다. 탑다운 줌에서 총알이 날아가는 게 눈에 보이는 것도 크다.
- **탄창=아이템**이라 레이드 전 준비(탄창에 탄 채우기)가 생기고, 교전 중 "반쯤 남은 탄창을 버릴 것인가"라는 판단이 생긴다. 이미 부착물이 **무기 인스턴스에 귀속**(타르코프식)으로 되어 있어 같은 결이다.
- **조준이 우클릭**이면 근접의 강공격 차징 자리를 그대로 물려받아 조작 수가 안 늘어난다.

### 데이터 구조
| 어디 | 무엇 | 비고 |
|---|---|---|
| `WeaponData` | **총 수치의 유일한 출처**(2026-09-11) — `isRanged`, `firearmStance`(총 종류 → 플레이어 총기 모양·모션, 밴딧 모델), `caliber`, `fireMode`, `rpm`, `damage`, `groggy`, `projectileSpeed`, `effectiveRange`, `hipSpreadDeg`/`adsSpreadDeg`, `recoilPerShot`/`recoilRecover`/`recoilMax`, `reloadSeconds`, `adsMoveMult` | 근접 필드와 같은 SO, 칸만 나눔 — 인스펙터(`WeaponDataEditor`)가 근접이면 근접 칸, 총이면 총 칸만 보여 준다. 총기 밴딧도 StatDB에서 이 에셋을 참조 |
| `StatDB` 유닛 | 총기 밴딧의 **적 전용 값만**: `rangedWeaponData`(쥔 총) · `rangedDamageMult` · `rangedBulletSpeedMult` · `preferredRange` · `burstCount`/`burstInterval` · `spreadDeg` + `attackRange`(사거리)·`attackWindup`(조준 경고)·`attackSpeed`(사격 주기) | 2026-09-11 전엔 총 종류 enum과 절대 수치를 따로 들고 있었다 |
| `GameTuning` | 거리 감쇠 `gunFalloffStart`(0.5)·`gunFalloffEndMult`(0.55) — 플레이어·적 공용 | 2026-09-11 `Projectile` 상수에서 옮김 |
| `ItemData` | `magCapacity`(탄창 장탄수) · `magCaliber` · `ammoCaliber`(탄약 아이템) | `magCapacity`는 기존 `partMagBonus`를 **이름만 바꾼 것**(`[FormerlySerializedAs]`로 값 보존). "가산"이 아니라 **탄창 자체의 용량**으로 뜻이 바뀐다 |
| `ItemInstance` | `ammoCount` · `ammoItemId` | **탄창 인스턴스**엔 그 탄창에 든 탄, **총기 인스턴스**엔 장착 탄창에 남은 탄. 중첩 인스턴스를 안 만들려고 같은 필드를 양쪽이 쓴다(유니티 직렬화는 자기참조 타입을 못 다룬다) |

- 장전 = 인벤의 탄창 인스턴스 M을 골라 → 총기의 `ammoCount/ammoItemId`를 M 것으로 교체, 빼낸 탄창은 **남은 탄을 실은 새 인스턴스**로 인벤에 돌아간다. 이래서 반쯤 쓴 탄창이 보존된다.
- 탄창 없는 총은 **발사 불가**("탄창 없음"). 탄창은 주워야 하는 물건이다.

### 파츠 4종의 실효과 (여태 필드만 있던 것) — ⚠️ 2026-09-09 탄창만 남음
| 파츠 | 필드 | 총기에서의 뜻 |
|---|---|---|
| 조준경 Scope | `partRangeBonus` | 유효사거리 +m, 조준 시 시야콘 보정 |
| 소염기 Muzzle | `partRecoilMult` | 반동↓ (~~총성 반경↓~~ — 소음 시스템 폐기 2026-09-09로 소멸) |
| 탄창 Magazine | `magCapacity` | 장탄수 = 탄창 용량 |
| 손잡이 Grip | `partRecoilMult` | 반동↓ |

### ~~소음 — 총기의 핵심 대가~~ (2026-09-09 폐기)
총성이 사람을 부르던 구조는 소음 시스템과 함께 사라졌다. **지금 총의 대가는 탄약 유한성뿐**이다.
"쏠 것인가"의 긴장이 필요해지면 다시 설계할 것 — 되살릴 땐 `Distraction.Report`에 총구 위치를 얹으면 된다.

> 미정(후속): 탄종별 관통/데미지 배율, 약실 1발(chamber) 구분, 연사 중 탄퍼짐 누적 곡선 세부.
> (적 AI의 총기 사용은 2026-09-11 아래 §총기 밴딧으로 해소.)

### 총기 밴딧 — 적이 총을 쓴다 (2026-09-11)

결정 기록: [bandit-firearms.md §레이드 배치 결정](bandit-firearms.md). 여기엔 동작 규칙과 수치(제안값)만 둔다.

| 유닛 | HP | 탄 1발 | 사거리 / 멈추는 거리 | 조준 경고 | 사격 주기 | 탄속 | 점사·간격 / 퍼짐 |
|---|---|---|---|---|---|---|---|
| `bandit_pistol` | 34 | 9 | 9m / 6.5m | 0.7s | 2.0s | 16m/s | 1발 / ±3° |
| `bandit_rifle` | 44 | 6 | 12m / 9m | 0.85s | 2.6s | 20m/s | 3발·0.4s / ±5° |

- 수치: **총 자체는 `WeaponData`**(권총 = 플레이어와 같은 `Weapon_Pistol9`, 소총 = `Weapon_Rifle545`) — 피해·탄속은 StatDB `rangedDamageMult`·`rangedBulletSpeedMult`를 곱한 값이 위 표다(2026-09-11 통합, 표의 값은 그대로 유지). 적 전용 값(`preferredRange`/`burstCount`/`burstInterval`/`spreadDeg` + `attackRange`=사거리, `attackWindup`=조준 경고, `attackSpeed`=사격 주기)은 StatDB 유닛별 — Control Panel ▸ 🎮 스탯 DB. 드랍은 `bandit_melee_1`과 같은 테이블.
- **이동:** 추격 중 사선(벽 없음)이 트이고 사거리 안이면 멈춰 조준, 트였지만 멀면 멈추는 거리까지만 다가간다. 벽에 가리면 길찾기로 돌아 들어온다. 총을 다 꺼내기(0.6초) 전엔 조준하지 않는다.
- **조준 경고 = 공격 예비동작.** 붉은 점멸 + 총구에서 뻗는 붉은 조준선(벽에서 끊김). 끝 0.2초 전에 조준이 **고정**되고(선이 굵어지며 노랗게 깜빡) 그 방향으로 쏜다 — 고정된 선을 보고 옆으로 빠지면 빗나간다. 조준선은 시야콘 밖이어도 보인다(경고가 목적).
- **탄은 실제 `Projectile`** — 플레이어 총과 같은 규칙: 벽·엄폐·다른 적의 몸에 막히고, 사거리 절반 이후 55%까지 감쇠한다. 구르기 무적 중엔 몸에 흡수돼 피해가 없다.
- 조준 중에 맞히면 캔슬된다(근접 약공과 같은 규칙). 총기엔 강공이 없다.
- **점사 간격은 플레이어 피격 무적창(`Health.HurtIFrame` 0.35초)보다 길게.** 0.12초였을 땐 둘째·셋째 탄이 무적창에 먹혀 3점사가 늘 1발만 맞았다.
- Zone1 배치: 폐아파트·주차장 = 권총, 무너진 상가·유리타워 = 소총(손배치 존 4개, 9기). 아케이드(입구)는 근접만. 무리 가중치(`TrimEnemyPacks`)는 근접과 같은 1.
- 검증(2026-09-11, Systems에서 시작 → Zone1): 총기 모델 9기 전부 부착, 권총 6.5m에서 멈춰 조준선 → 발사 → 피격(약 2.7초 주기), 소총 점사 2~3발 적중, 콘솔 에러 0.

### 총격전 — 플레이어 에임·반동·총기/탄 스펙 (2026-09-11 결정 · 구현 중)

> 사용자: "플레이어 총 에임도 있어야 해, 총격전 해야 하니까. 반동도 추가해줘. 총기 스펙이랑 탄 스펙도 넣을 거고" — 방향성 [gdd-core §게임 방향성](gdd-core.md).
> 이미 있던 것(2026-07-29 총기 v1): 우클릭 조준(퍼짐↓·이속↓), 연사 퍼짐 누적(`recoilPerShot`/`recoilMax`/`recoilRecover`), 고정 화면 흔들림 1종, `WeaponData` 총 필드 17개, 탄 = `ItemData.ammoCaliber` + `ammoDamageMult`.

| 날짜 | 질문(선택지) | 사용자 결정 |
|---|---|---|
| 2026-09-11 | 에임 표시 — 커서 조준원 / 레이저 조준선 / 둘 다 | **커서 조준원** — 마우스 위치에 퍼짐만큼 벌어지는 원. 연사하면 벌어지고 우클릭 조준하면 좁아진다 |
| 2026-09-11 | 반동 — 조준이 튐 / 화면 반동 / 총 모션 반동 / 퍼짐 누적만 (복수) | **퍼짐 누적만** — 지금 있는 연사 퍼짐을 유지하고 세기는 총마다 스펙으로 |
| 2026-09-11 | 탄 스펙 — 데미지 배율 / 관통력 / 탄속·사거리 배율 / 반동·퍼짐 배율 (복수) | **넷 다** |
| 2026-09-11 | 조준 시 카메라 — 조준할 때만 커서 쪽 / 항상 / 안 함 | **조준할 때만** 커서 쪽으로 밀어 멀리 본다 |

- 총기 스펙 = `WeaponData`(총마다 1개), 탄 스펙 = 탄 아이템(`ItemData`)의 탄 필드. 값은 사용자가 채운다 — 이 절은 구조와 규칙만 정한다.

**구현 규칙 (2026-09-11)**
- **조준원** (`UI/AimReticle` + `UI/ReticleRing`): 반경 = 플레이어→커서 거리 × tan(현재 퍼짐)을 화면 픽셀로. 현재 퍼짐 = (기본 or 조준 퍼짐 + 연사 누적) × 탄 퍼짐 배율. 선 두께는 화면 픽셀 고정(흰 선 + 어두운 테두리, 밝은 바닥에서도 읽힘), 최소 반경 7px. 총 장착 + UI 닫힘 + 투척 조준 아님 + 생존일 때만, 달리기(총 수납) 중엔 숨긴다. 보이는 동안 OS 커서는 숨긴다. 장전 중엔 흐리게. `PlayerGun`이 자동으로 붙인다.
- **조준 카메라** (`CameraFollow.AimLookAhead`): 우클릭 조준 중에만 커서 쪽으로 (플레이어→커서 × `aimLookAhead` 0.35, 최대 `aimLookAheadMax` 4m). 기존 추적 감쇠로 부드럽게.
- **반동 = 퍼짐 누적만** — 기존 `WeaponData.recoilPerShot`(한 발당 +퍼짐) / `recoilMax`(상한) / `recoilRecover`(초당 회복)를 총마다 스펙으로 쓴다. 조준 튐·화면 반동·총 모션 반동은 넣지 않는다(사용자 결정).

**총기 스펙 (`WeaponData`)** — 기존 필드 그대로: 구경 `caliber`, 연사 방식 `fireMode`(Single/Auto — Burst는 아직 미구현), 분당 발사수 `rpm`, 데미지 `damage`, 그로기 `groggy`, 탄속 `projectileSpeed`, 유효 사거리 `effectiveRange`, 비조준/조준 퍼짐 `hipSpreadDeg`/`adsSpreadDeg`, 퍼짐 누적 `recoilPerShot`/`recoilMax`/`recoilRecover`, 장전 시간 `reloadSeconds`, 조준 이속 `adsMoveMult`.

**탄 스펙 (탄 아이템 `ItemData`)** — 기본값이면 기존과 똑같이 난다.
| 필드 | 뜻 | 기본 |
|---|---|---|
| `ammoCaliber` | 구경(비어 있으면 탄이 아니다) | — |
| `ammoDamageMult` | 데미지 배율 | 1 |
| `ammoPenetration` | 관통력 — 적 **몸**을 몇 명 뚫나. 뚫을 때마다 데미지 × `gunPierceDamageKeep`(0.6). **벽·엄폐는 못 뚫는다**(엄폐의 의미를 지키려고). 같은 몸은 두 번 안 맞는다 | 0 |
| `ammoSpeedMult` | 탄속 배율 | 1 |
| `ammoRangeMult` | 사거리 배율(거리 감쇠도 이 거리 기준) | 1 |
| `ammoSpreadMult` | 반동·퍼짐 배율 — 기본 퍼짐과 누적 퍼짐 모두에 곱한다 | 1 |

- 방어구는 아직 장비 칸만 있고 피해 감소가 없다 — 방어구가 생기면 관통력이 방어구에도 먹게 확장한다.
- ⚠️ 알려진 한계(기존): 총알은 총구 높이로 수평으로 날아 **머리·다리를 노려 맞히지 못한다**(부위 = 맞은 높이). "조준한 곳이 맞는다" 규칙과 어긋나 후속 과제.

### 총기 v1 — 실제로 쏠 수 있는 상태 (2026-07-29)

**콘텐츠 3종 + 무기 SO**

| 에셋 | itemId | 값 |
|---|---|---|
| `Weapon_Pistol9` (WeaponData) | — | 데미지 14 · 그로기 14 · 320RPM · 탄속 42 · 사거리 14m · 탄퍼짐 8°(허리)/1.8°(조준) · 반동 2.6/발(회복 10/s, 상한 10) · 장전 2.0s · **총성 34m** · 조준 이속 0.6 |
| `Pistol9` | `pistol9` | 무기 · 2×1 · 1.1kg · 판매 2500 / 구매 6000 |
| `Mag9x19` | `mag_9x19` | 탄창 15발 · 9x19 · 0.16kg · 판매 220 / 구매 550 |
| `Ammo9x19` | `ammo_9x19` | 탄약 9x19 · 최대 스택 60 · 0.012kg/발 · 판매 9 / 구매 22 |

**밸런스 근거** — 적 40HP(밴딧)·110HP(탱커), 플레이어 약공 8·강공 20 기준.
권총 14 = 밴딧 **3발**, 탱커 8발. 근접보다 확실히 세다. 대가는 **탄약 유한성 하나**뿐 —
낱알 탄약은 쓸모없고 탄창에 채워야 화력이 된다. (~~총성 34m~~는 소음 폐기 2026-09-09로 무효)
그로기 14 → 밴딧(maxGroggy **60**·감쇠 5, 2026-09-11 조정) 기준 **5발**. [→ balance.md](balance.md)

**탄창 채우기 (`GunAmmo`)** — 인벤 우클릭 ▸ **탄약 채우기 / 탄약 비우기**.
- 이미 든 탄이 있으면 **같은 탄종만** 더 들어간다(한 탄창에 탄종이 섞이면 데미지 배율을 말할 수 없다)
- 비우기는 자리가 있는 만큼만 돌려받고 나머지는 탄창에 남는다
- 주머니 → 가방 → 보안 순으로 훑는다(손 가까운 것부터)

**HUD** — 우하단 `탄 / 장탄`. 총 안 들면 아예 안 보인다. 장전 중 진행%, 탄창 없음/탄 없음은 붉게, 1/4 이하는 주황.

**F1 디버그** — "권총 + 탄창2 + 탄약60 지급"(탄창 하나는 **가득 채워서**) / "장착 총에 탄창 물리기".
탄창이 빈 채로 주면 지급 직후 "맞는 탄창 없음"이라 왜 안 나가는지 찾는 데만 시간이 든다.

> 구현 주의 — `GameHUD`는 프리팹 베이크 대상이라 **이미 구워진 프리팹엔 탄약 위젯이 없다**.
> `GenerateUI`는 `IsGenerated` 가드에 막혀 다시 안 돌기 때문에 재베이크 전까지 조용히 안 뜬다.
> → `UpdateAmmo`가 없으면 그 자리에서 만든다(A타입 패널의 "프리팹 없으면 코드 생성 폴백"과 같은 규약).

### 손에 들리는 것 — 3종 비주얼 (2026-07-29)

> 사용자: *"총도 칼처럼 비주얼 만들었나?"* / *"칼은 지금 인벤에 착용하면 비주얼 나오게끔 하는 건가? 아닌 것 같은데"*
> / *"맨손이면 맨손 표시로 약간 장갑처럼 보여주고 때릴 때 앞으로 정권찌르기 같이 해줘"* / *"칼 착용하면 지금처럼 해주고"*

**여태 칼이 장착과 무관하게 항상 붙어 있었다** — `TopDownPlayer` 초기화가 `MeleeWeaponVisual.Attach`를 무조건 불러서, 맨손이어도 칼이 보이고 총을 들면 그냥 숨기기만 했다(빈손으로 사격). 이제 **장착 아이템이 무엇을 보여줄지 정한다.**

| 손 상태 | 판정 기준 | 비주얼 | 약공 | 강공 |
|---|---|---|---|---|
| ~~맨손~~ **빈손** | 주무기 슬롯 비었거나 무기가 아님 | ~~`FistVisual` — 장갑 낀 주먹 둘~~ → **아무것도 안 든다** (2026-09-11 삭제) | ~~정권찌르기~~ → **공격 없음**(구르기만) | — |
| 근접 | 카테고리=무기, `isRanged` 아님 | `MeleeWeaponVisual` (기존 그대로) | 우→좌 스윙 | 대각에서 크게 |
| 총 | `weaponData.isRanged` | ~~`GunVisual` — 총몸+총열+손잡이~~ → **3D 총기 모델**(`ChibiPlayerVisual.SetFirearmEquipped`, 2026-09-11 그레이박스 삭제) | 3D 사격 반동 모션 | — |

> **2026-09-11 사용자 결정** — *"우리 기존에 쓰던 주먹이랑 총 가짜 모델 그거 다 없애줘. 우리 이제 근접 손공격은 없어"*
> → **맨손 공격 폐기**: 무기가 없으면 좌·우클릭(약공·차징 강공)을 받지 않는다(`TopDownPlayer.HandleCombatInput`, 구르기는 그대로).
> → 그레이박스 **주먹(`FistVisual`)·총(`GunVisual`) 스크립트 삭제.** 총은 3D 총기 모델이 맡는다. 근접 무기의 그레이박스 칼(`MeleeWeaponVisual`, 3D 칼·방망이 모션이 없는 무기용)은 남긴다.

- 근접 무기 다수가 `WeaponData`가 없는 구형이라(Bat/Axe 등) **카테고리로 판단**한다. `WeaponData` 유무로 갈랐으면 지금 있는 근접 무기가 전부 맨손 취급됐다.
- 셋 중 **하나만** 보인다. 총 쏘는데 칼이 같이 떠 있는 식을 막는다.
- ~~`GunVisual`은 조준하면 앞으로 내밀고, 장전 중엔 총구를 내렸다가 진행도에 따라 되든다.~~ (2026-09-11 삭제 — 3D 총기 모델로 대체)
- 셋 다 `SetVisible`을 갖는다 — 시야콘 밖에서 **손만 어둠에 떠 있는** 것을 막는다(루트의 자식이라 몸통을 꺼도 자동으로 안 꺼진다).
- 전부 **판정에는 일절 관여하지 않는다.** 히트박스는 `AttackPerformer`, 총알은 `Projectile`이 담당한다 — 연출이 판정을 건드리면 "보이는 것과 맞는 것"이 어긋나 디버깅이 지옥이 된다.

## 부위 피격 — 2026-07-29 결정

> **질문과 결정**
> ① 근접은 어느 부위가 다치나 → 처음엔 "가중 랜덤"이었으나, 이어서 사용자가 **"근접도 조준 똑같이 넣어줘"** → **근접도 총과 같이 조준한 곳이 맞는다.** 가중 랜덤은 **조준점이 없는 쪽(적의 공격)** 의 폴백으로 남는다.
> ② 적에게도 적용하나 → **적도 같이. 헤드샷 보너스.**
> ③ 부위 표시 → **타르코프식 오버레이(테스트용, F1 토글)**. *"다리 노려지는지 이런 거 볼 수 있을지도"*
> ④ 사거리 감쇠 → **넣는다.** *"단검이면 사거리 5 안에서 때려야 하는데 1에서 때리면 100%, 4.5에서 때리면 60% 이런 느낌"*

### 부위는 조준점이 정한다
탑다운이지만 캐릭터는 **선 사람**으로 그려지므로(근-오버헤드 투영) 스프라이트 안에서의 위아래가 곧 몸의 높이다. 위를 노리면 머리, 아래를 노리면 다리 — **조준점만으로 읽힌다.**
- **플레이어**(근접·총 모두): 마우스 월드 좌표 → `BodyZones.FromPoint`
- **적**: 조준점이 없으므로 가중 랜덤 — 몸통 45% / 팔 20% / 왼다리 15% / 오른다리 12% / **머리 8%**

`PlayerMedicalSystem`의 5부위(`BodyPartType`)를 그대로 쓴다 — 판정과 치료가 같은 언어를 쓰게. 여기서 새 부위 개념을 만들지 않는다.

| 부위 | 세로 구간 | 데미지 배율 |
|---|---|---|
| 머리 | 위 22% | **×2.0** |
| 몸통 | 34~78% 중앙 | ×1.0 |
| 팔 | 34~78% 좌우 가장자리 | ×0.8 |
| 왼/오른다리 | 아래 34%, x로 좌우 분할 | ×0.75 |

### 사거리 감쇠
**"닿기만 하면 같은 데미지"면 사거리가 긴 무기가 무조건 이득이라 거리 판단이 사라진다.**
- **근접**: 품 안(사거리의 `meleeFalloffNear` 35% 이내)은 100%, 끝은 `meleeFalloffFar` 60%까지 선형. 둘 다 GameTuning 노브. [→ balance.md](balance.md)
- **총**: 유효사거리 절반까지 100%, 끝에서 55%. 안 그러면 사거리 끝에서만 쏘는 게 항상 정답이 된다.

### 테스트 도구 (`BodyZoneOverlay`)
F1 ▸ "몸 부위 표시". 머리=붉게 / 몸통=노랑 / 팔=파랑 / 다리=초록, 맞은 자리는 잠깐 밝아진다.
**판정과 같은 기하(`BodyZones`)로 그린다** — 표시가 다른 식을 쓰면 "보이는 것과 맞는 것"이 어긋나 없느니만 못하다.
다른 코드가 오버레이를 참조하지 않으므로 나중에 컴포넌트만 지우면 흔적이 안 남는다.

> 미정(후속): 부위 부상이 **적에게도** 디버프로 붙을지(지금은 데미지 배율만). 팔을 맞히면 적 공격이 느려지는 식.

### 적 부위 부상 디버프 — 2026-07-29

> 사용자: *"적 부위 부상 디버프도 넣어줘"*

여태 부위는 **데미지 배율뿐**이라, 다리를 노려도 "조금 덜 아프다" 말고는 아무 일도 안 났다.
그래서 **머리만 노리는 게 항상 정답**이었다. 부위마다 **다른 이득**이 있어야 조준에 선택이 생긴다.

`UnitInjuries` — 부위별 누적 피해가 임계를 넘으면 부상. 임계는 **그 유닛의 최대 HP 기준**(HP 110 탱커와 40 밴딧이 같으면 안 된다).
- 부상 = maxHp × **22%**, 중상 = maxHp × **45%** (중상은 효과 2배)

| 부위 | 부상 시 | 노릴 이유 |
|---|---|---|
| **다리** | 이동 −18%/단계 (양다리 중상이면 −72%) | **못 쫓아온다** — 도망칠 수 있다 |
| **팔** | 예비동작 +22%/단계, 공격력 −20%/단계 | 느리게 때리고 덜 아프다 — 맞고 버틸 수 있다 |
| **머리** | 탐지 −25%/단계, **받는 그로기 +25%/단계** | 잘 못 찾고 쉽게 무너진다 — 기습이 이어진다 |
| **몸통** | 그로기 회복 −30%/단계 | 숨을 못 고른다 — 몰아칠 수 있다 |

**구현 주의** — 배율은 `EnemyController`의 스탯 프로퍼티 **한 곳에서만** 곱한다(`InjMove`/`InjWindup`/`InjAtk`/`InjDetect`/`InjDecay`). 스탯을 읽는 자리마다 따로 곱하면 하나 빠뜨려도 아무도 모른다.

**보이게 하기** — 이름표에 `적 [다]`처럼 붙는다(F1 오버레이를 안 켜도 다리 부순 게 먹혔는지 보인다). 오버레이를 켜면 부상 부위가 짙게 물든다. 매 프레임 TextMesh를 건드리지 않고 **뱃지가 바뀔 때만** 갱신한다.

플레이어의 `PlayerMedicalSystem`과 **같은 부위 enum**을 쓰되 구조는 따로다 — 적은 치료·붕대가 없고 한 판 안에서만 유효하므로 단순 누적으로 둔다.

### 총기 버그 수정 — 2026-07-29

> 사용자: *"탄창에 총알 넣었는데 처리 안 되는 거"* / *"칼인데 파츠 착용? 나오는 버그"* / *"총알 채운 탄창 장착했는데 적용 안 되는 버그"* / *"파츠창에서 누르면 바로 착용해제 되지 않고 인벤/창고랑 똑같이"*

**① 채운 탄창을 끼워도 총은 0발이었다 (핵심)**
부착은 `SetAttachment(type, itemId)`로 **itemId만** 넘기고 탄창 인스턴스를 버렸다. 탄은 인스턴스(`ammoCount`)에 있으므로 **인스턴스가 사라지는 자리에서 반드시 옮겨야** 한다.
→ `CarryMagAmmoIn`을 부착 경로 **둘 다**(드래그·컨텍스트)에 넣고, 분리할 땐 남은 탄이 탄창을 따라 나오게 했다. 이걸 빼먹으면 분리할 때마다 탄이 증발한다.
> 이래서 "탄창에 총알을 넣어도 처리가 안 되는" 것처럼 보였다 — 채우기는 되고 있었고, **끼우는 순간 사라졌다.**

**② 칼에도 조준경·소염기·탄창 슬롯이 떴다**
근접에 탄창은 말이 안 되고, 총기용 파츠가 근접에 붙으면 사거리·반동 보정이 아무 데도 안 쓰여 **조용히 죽는 값**이 된다.
→ 총이면 4종 전부, **근접이면 손잡이만**(이속·스태미너 보정은 근접에서도 의미가 있다).

**③ 파츠 슬롯 좌클릭 = 즉시 분리**
스치기만 해도 파츠가 빠졌다. 인벤과 같은 규약으로 — **좌클릭=정보, 우클릭=메뉴("분리"/"자세히")**.

**④ 잔탄이 어디에도 안 보였다**
- 인벤 이름: `9x19 탄창 [12/15]` — 탄창은 몇 발 들었는지가 이름의 일부다
- 파츠 슬롯: 이름 아래 `12/15`
