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

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-24 | 전투 프로토타입 구현. 약공(콤보)/강공(차징)/구르기/스태미너/그로기/적 캔슬 시스템. 스톤샤드 참고하되 턴제 제외 확정. |
| 2026-06-02 | **탑다운 2D 전투 이식.** 구 `PlayerController`(NavMesh/3D) 폐기 → `TopDownPlayer`(Rigidbody2D)에 전투 전면 재구현: 약공(콤보)·강공(차징)·구르기(무적)·스태미너·탈진을 `StatDB.playerStat` 기반으로. 공격 판정은 `Physics2D.OverlapCircleAll`로 FacingDirection 방향 → `EnemyController.TakeHit(dmg, groggy, knockback)`. `EnemyController`도 Rigidbody2D 상태머신(NavMesh 제거), `TakeHit`/`IsDead` 추가. 무적 체크(Health/CombatFeedback)·전투 중 상호작용 차단(InteractionSystem)·HUD 스태미너 바 연결. 상호작용 계층(Interact/Talk/Open/Pickup)은 `GameObject` 인터페이스로 확정. |
| 2026-06-02 | **프레임 기반 히트박스/허트박스 시스템 + 에디터 툴.** `AttackData`(SO): 공격 1종의 `duration`(초) + `HitWindow[]`(정규화 0~1 활성구간, Box/Circle, facing기준 offset(전방x/좌y), 크기/반경, 회전, damage·groggy 배율). `AttackPerformer`: 시간진행하며 활성 윈도우를 `OverlapBox/CircleNonAlloc(targetMask)`로 스캔 → `Hurtbox.ReceiveHit`(중복 1회). `Hurtbox`(trigger Collider2D): 피격 판정, 적이면 `EnemyController.TakeHit`·아니면 `Health.TakeDamage`. **구르기 무적 = 허트박스 콜라이더 off**(`SetActive(!IsInvincible)`). 플레이어 콤보별/강공 AttackData, 적 `attackData`(없으면 즉시 데미지 폴백). 팀 구분=레이어(Player=6/Enemy=9). **에디터**: `AttackDataEditorWindow`(Tools▸TopDown Combat▸Attack Editor) — 타임라인 스크러버(윈도우 막대), 2D 탑다운 프리뷰(facing→우, 활성 윈도우 진하게, 중심 핸들 드래그), 윈도우 추가/삭제·속성 편집. |
