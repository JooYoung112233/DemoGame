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
| 데미지 팝업 | 흰/작 | 주황/큼, 크리=노랑 | DamagePopup(2D 리워크 필요) |
| 카메라 | 없음 | 줌 펀치 + 방향 셰이크 | 신규(CameraFollow 오프셋 레이어) |

### 히트스탑 (강공만, 안전 재도입)
- 강공 적중에만 0.04~0.06s 정지. 예전 버그(전역 `Time.timeScale` 누수)는 **단일 가드 코루틴**(중복 시작 시 이전 취소 + `finally`로 항상 원복)으로 해결. UI/연출은 `unscaledDeltaTime` 사용.
- 약공은 정지 없이 플래시+작은 펀치로 가볍게.

### 적중 = 적 셰이더 플래시 (확정)
- 적중 순간 적 스프라이트를 **흰색으로 1~2프레임 깜빡**. `SpriteRenderer.color`(기존 빨강) 대신 셰이더 `_FlashColor`/`_FlashAmount` 프로퍼티로 구동 → 베이스 색·조명과 무관하게 깔끔. `BRB/SpriteBillboard`·`PlayerSprite`·`SpriteSheet`에 flash 프로퍼티 추가.
- 강공은 더 강한/긴 플래시 + 히트스탑과 동시.

### 플레이어 피격 = 위험 비례 화면 연출 (확정)
- **평소(체력 여유)**: 절제 — 가장자리 **비네트 붉은 펄스** + 약한 방향 셰이크.
- **저체력 / 출혈·골절 발생**: 풀세트 — **빨강 풀스크린 플래시 + 색수차 펄스 + 강한 셰이크**. `PlayerMedicalSystem`/`Health` 연동.
- 화면 전체 연출은 *플레이어 피격에만*(적 피격은 엔티티 연출만 — 과하면 정신없음).

### 카메라 (CameraFollow와 공존)
- 셰이크/줌펀치를 카메라 위치 직접 조작이 아니라 **`CameraFollow`가 더하는 오프셋 레이어**로 구현 → 추적과 충돌 없음. (현 `ScreenEffectManager.ScreenShake`는 localPosition 직접 흔들어 충돌 위험 → 리팩터 대상.)

### 가시성 연동 (→ `rendering.md`)
- 손전등 폐기 후 **좌보이드식 시야(FOV)**: 적은 플레이어가 바라보는 부채꼴 시야 밖이면 안 보임/어둑. "어디서 적이 튀어나오나"의 긴장이 곧 타격감의 무대. 상세는 [`rendering.md`](rendering.md) 가시성 섹션.

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-24 | 전투 프로토타입 구현. 약공(콤보)/강공(차징)/구르기/스태미너/그로기/적 캔슬 시스템. 스톤샤드 참고하되 턴제 제외 확정. |
| 2026-06-02 | **탑다운 2D 전투 이식.** 구 `PlayerController`(NavMesh/3D) 폐기 → `TopDownPlayer`(Rigidbody2D)에 전투 전면 재구현: 약공(콤보)·강공(차징)·구르기(무적)·스태미너·탈진을 `StatDB.playerStat` 기반으로. 공격 판정은 `Physics2D.OverlapCircleAll`로 FacingDirection 방향 → `EnemyController.TakeHit(dmg, groggy, knockback)`. `EnemyController`도 Rigidbody2D 상태머신(NavMesh 제거), `TakeHit`/`IsDead` 추가. 무적 체크(Health/CombatFeedback)·전투 중 상호작용 차단(InteractionSystem)·HUD 스태미너 바 연결. 상호작용 계층(Interact/Talk/Open/Pickup)은 `GameObject` 인터페이스로 확정. |
| 2026-06-02 | **타격감 연출 설계 확정.** 강공 히트스탑(0.04~0.06s, 안전 구현), 적중=적 셰이더 흰 플래시(`_FlashAmount`), 플레이어 피격=위험 비례 화면 연출(평소 절제→저체력/부상 풀세트), 카메라 셰이크/줌은 CameraFollow 오프셋 레이어. 약공/강공 차등 레이어 표 추가. `DamagePopup`은 3D 시절 유물(빌보드+Y/Z오프셋)이라 2D 리워크 필요. |
| 2026-06-02 | **가시성 전환: 손전등 폐기 → 좀보이드식 시야(FOV).** 적은 플레이어가 바라보는 부채꼴 시야 밖이면 안 보임/어둑. 어둠=하이브리드(지역/시간대별). 손전등 코드(`FlashlightController`/`FlashlightBeam`/손전등 Light2D) 완전 제거 후 시야 시스템 신규 작성. 상세 `rendering.md`. |
| 2026-06-02 | **프레임 기반 히트박스/허트박스 시스템 + 에디터 툴.** `AttackData`(SO): 공격 1종의 `duration`(초) + `HitWindow[]`(정규화 0~1 활성구간, Box/Circle, facing기준 offset(전방x/좌y), 크기/반경, 회전, damage·groggy 배율). `AttackPerformer`: 시간진행하며 활성 윈도우를 `OverlapBox/CircleNonAlloc(targetMask)`로 스캔 → `Hurtbox.ReceiveHit`(중복 1회). `Hurtbox`(trigger Collider2D): 피격 판정, 적이면 `EnemyController.TakeHit`·아니면 `Health.TakeDamage`. **구르기 무적 = 허트박스 콜라이더 off**(`SetActive(!IsInvincible)`). 플레이어 콤보별/강공 AttackData, 적 `attackData`(없으면 즉시 데미지 폴백). 팀 구분=레이어(Player=6/Enemy=9). **에디터**: `AttackDataEditorWindow`(Tools▸TopDown Combat▸Attack Editor) — 타임라인 스크러버(윈도우 막대), 2D 탑다운 프리뷰(facing→우, 활성 윈도우 진하게, 중심 핸들 드래그), 윈도우 추가/삭제·속성 편집. |
| 2026-06-02 | **프레임 기반 전환 + 연속 공격(콤보) 구조.** ①타이밍 정규화(0~1)→**프레임**: `AttackData.fps`+`totalFrames`, `HitWindow.startFrame/endFrame`. `AttackPerformer`가 `CurrentFrame`으로 윈도우 활성 판정. ②**콤보 체인** `AttackComboData`(SO): 순서대로 이어지는 `AttackData[] steps` + `bufferTime`(선입력). `AttackData.cancelFromFrame`(이 프레임 이후 다음 단계 캔슬 입력 허용), `AttackPerformer.CanCancel`. ③`TopDownPlayer.lightCombo`: 공격 중 캔슬 윈도우에 입력하면 다음 단계 연결 + 선입력 버퍼, 구르기 시 콤보 끊김. ④**에디터** 개편: 단일/콤보 모드 토글, 콤보는 [1타][2타]… 단계 탭, **프레임 그리드 타임라인**(칸=프레임, 윈도우 막대, 캔슬 프레임 마커, 프레임 스크러버), 윈도우 시작/끝 프레임 IntSlider. |
