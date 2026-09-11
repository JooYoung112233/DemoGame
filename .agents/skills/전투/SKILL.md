---
name: 전투
description: demo13-flashlight 전투 체감 재정립 작업을 이어서 진행한다 — 2026-07-11 전수 진단으로 확정한 원인·수정분·잔여 구멍을 복원하고 다음 항목을 구현한다. "전투 이어서", "전투 계속", "전투 이상한 거 더 고쳐", "때리는 느낌 손봐" 같은 요청에 사용.
---

# 전투 체감 재정립 — 이어서

demo13-flashlight(Unity 탑다운 2D)의 근접 전투를 사용자 피드백 기반으로 재정립하는 작업.
**SSOT = `demo13-flashlight/docs/combat.md`** (2026-07-11 항목 두 개 = 진단·1차·2차 전문).

## 0. 대전제 (이걸 모르면 헛수고한다)

- **전면 재작성 금지.** 전수 진단 결과 원인 비중이 **데이터 55% / 코드 45%**이고, 코드 문제도 구조가 아니라 국소 결함이었다. 재작성하면 데이터 원인이 그대로 재생산된다. 이미 사용자와 합의된 방향.
- **공격 판정의 진실원 = `StatDB` 수치 + `TopDownPlayer.MakeAttack()` 하드코딩 공식.**
  `PlayerRig.prefab`의 `lightCombo/heavyAttack/heavyFullAttack`이 전부 null이고 `WeaponData` 에셋이 0개라 **100% 자동 생성 경로**로 돈다. `Resources/Attack.asset`은 아무도 참조 않는 **고아 에셋**(고쳐도 게임에 영향 0 — 속지 말 것).
- **수치를 바꿨는데 체감이 안 변하면 씬을 의심하라.** 씬에 박힌 `unitKey`가 StatDB에 없으면 `GetUnit`이 null → **인스펙터 기본값으로 조용히 폴백**한다(HP·데미지가 StatDB와 완전히 다른 값이 됨).

## 1. 상태 로드 (먼저)

```bash
git -C <repo> fetch && git -C <repo> status -s
```
- `docs/combat.md`의 **2026-07-11** 항목 2개를 읽어 무엇이 이미 고쳐졌는지 확인(아래 §2와 대조).
- ⚠️ **병렬 자동커밋 프로세스**가 `git add -A`로 내 미커밋 변경을 쓸어담는다. 커밋은 **항상 경로 지정**, 커밋 직전 `git diff --cached --name-only` 확인. `Assets/GPT/` 삭제분은 **절대 커밋 금지**.

## 2. 이미 완료된 수정 (중복 작업 금지)

**데이터(StatDB)** — `moveSpeed 1→4`(적 2.5보다 느려 카이팅 불가였음) · `sprintMult 2.1→1.6` · `lightRange 2→1.2` · `heavyRange 2.5→1.7` · `lightCooldown 0.4→0.12` · `dodgeInv 0.2→0.26`

**적 3종** — `bandit_melee_1`(기본) / `bandit_ranged`(견제·HP28·사거리3.4) / `bandit_tank`(중장·HP110·캔슬불가). Zone1 배치가 위험 곡선(아케이드→돔→타워 최심부).

**코드 (15건)**
1. 약공 3타 콤보 부활 — 선입력 분기가 얼리 리턴 뒤라 **도달 불가 코드**였음
2. 히트박스 리치/폭 분리 + 몸 반경만큼 전방 오프셋
3. 스윙 방향 **스냅샷 고정**(마우스 실시간 추종 → 등 뒤 적 맞던 문제)
4. 적 공격 런지 제거(transform 하드 스냅 = 앞뒤 고무줄)
5. 적 즉시타격: `AtkRange×1.5` 원형 → `×1.05` + **±60° 정면**
6. 플레이어 피격 **무적창 0.35s**(`Health`, DoT 예외)
7. 그로기 스턴 중 피격이 상태를 덮어 **첫 타격에 스턴 풀리던** 버그
8. 적 **정지거리**(사거리 0.85배) — 쿨다운 중 파고들어 겹치던 문제
9. 약공 **히트스탑** 부여(0.03~0.05) — 구 `hitstop:0`이라 셰이크·줌펀치 전부 미발동
10. 구르기 등속 → 1.35→0.5 감쇠(강한 시작 + 부드러운 착지)
11. `HeavyCharge` 영구 교착 해소(+타임아웃)
12. 예비동작 캔슬 1.5배 경직이 직후 `OnDamaged`에 덮이던 버그
13. 캔슬·사망 시 `_performer.Cancel()` 누락
14. 넉백 방향 버그(플레이어 피격 시 `dir=0` → 항상 아래로) → 플레이어 넉백 제거
15. **벽 관통 차단**(LOS) · **적끼리 분리**(반경 1.1m 반발)

**빌더** — 무제 미저장 씬 때문에 지역1/내부 빌더가 전부 실패하던 것 수정(`EditorSceneBuildUtil.NewDetachedScene`).

## 3. 잔여 구멍 (다음 작업 후보 — 진단서 확정분)

| # | 항목 | 증상 | 위치 |
|---|---|---|---|
| A | **`SkeletonAnimController` 스텁** | `IsAnimComplete`가 항상 true라 animController 붙은 적은 **피격 경직·공격 후딜이 0프레임**으로 무력화. 지금은 그레이박스 적이 null이라 잠복 중이지만 `generatedPrefab` 경로 붙는 순간 조용히 깨짐 | `Assets/Scripts/Sprite/SkeletonAnimController.cs` |
| B | `AttackPerformer._buf` 16 고정 | 광역 판정 시 조용히 잘림 | `AttackPerformer.cs` |
| C | `CombatFeedback` 매 피격 `FindGameObjectWithTag` | 성능 | `CombatFeedback.cs:141` |
| D | `HitFlash` 머티리얼 교체 | `BRB/SpriteFlash`(Unlit)로 갈아 Light2D 반응이 사라짐 | `HitFlash.cs:35` |
| E | 적 원거리 실제 투사체 | `bandit_ranged`가 지금은 "사거리 긴 근접"(투사체 시스템 없음) | `EnemyController` |

## 4. 작업 규칙

- **수정 전 반드시 원인 확인** — 증상만 보고 고치지 말 것. 이 프로젝트는 "수치가 조용히 무효화되는" 함정이 많다(RangeInt 미직렬화, unitKey 미스, 고아 에셋 등).
- **밸런스 수치는 `StatDB`**(전투) / `GameTuning`(그 외). 코드 하드코딩 금지. → `docs/balance.md` 색인.
- **결정은 즉시 `docs/combat.md`에 기록**(날짜/질문/결정). 완료 시 해당 항목에 반영.
- 정적 감사(중괄호·시그니처 실재·using) 후 **`unity-reviewer` 리뷰** 돌리고 지적 반영.
- Unity 실행 검증은 사용자 몫 — **QA 봇**(`/qa` skill)이 플레이해준다. 수정 후 "무엇을 확인해야 하는지" 짧게 알려줄 것.
- 커밋·푸시는 **사용자 요청 시만**. 메시지 끝: `Co-Authored-By: Codex Opus 4.8 <noreply@anthropic.com>`

## 5. 사용자에게 검증 요청할 때 (매번 확인)

씬 재빌드가 필요한 변경(적 배치·unitKey·스폰)을 했으면 **반드시 안내**:
```
Tools ▸ TopDown ▸ 빌드 ▸ 지역1
Tools ▸ TopDown ▸ 빌드 ▸ 내부 ▸ ── 전부 ──
```
안 돌리면 씬에 옛 값이 남아 **수정이 체감되지 않는다**(가장 자주 발생하는 "고쳤는데 그대로" 원인).
