# 시스템: 시너지 14종

> 파티 클래스 구성에 따라 자동 발동하는 조합 보너스.
> 메인 BattleScene + 서브 RunSimulator 양쪽에 적용 (RunSimulator는 `calcSynergyBonus`로 성공률 가산).
>
> ✅ **현재 코드(`data/synergies.js`)와 본 문서 일치 — 14종 구현 완료**.

## 1. 구조

| 분류 | 개수 | 조건 |
|---|---|---|
| 2인 시너지 | 7 | 지정 클래스 2종이 파티에 모두 있을 때 |
| 3인 시너지 | 5 | 지정 클래스 3종이 파티에 모두 있을 때 |
| 5인 보너스 | 2 | 파티 구성 조건 만족 시 |

**합계 14종.** 추가 확장은 P2 이후.

## 2. 2인 시너지 (7종)

| 키 | 이름 | 클래스 | 효과 |
|---|---|---|---|
| `holy_war` | 성전 | warrior + priest | warrior DEF +15%, priest 힐량 +20% |
| `dark_arts` | 암흑술 | rogue + mage | 두 클래스 ATK +15% |
| `poison_arrow` | 독화살 | archer + alchemist | archer 공격 시 출혈 부여 30% |
| `vanguard` | 전위대 | warrior + rogue | 근접 ATK +10% (range ≤ 100) |
| `mystic_barrier` | 신비 결계 | mage + priest | 런 시작 시 전체 보호막 (최대HP 10%) |
| `rapid_fire` | 속사 | archer + rogue | 두 클래스 공격속도 +15% (attackSpeed × 0.85) |
| `medical_team` | 의료팀 | alchemist + priest | 두 클래스 힐량 +15%, 전체 상태이상 저항 +20% |

## 3. 3인 시너지 (5종)

| 키 | 이름 | 클래스 | 효과 |
|---|---|---|---|
| `iron_company` | 철벽 용병단 | warrior + rogue + priest | 전체 HP +15%, 사망 시 1회 HP 10% 유지 (deathSave) |
| `ranged_battery` | 원거리 포격대 | mage + archer + alchemist | 원거리 사거리 +20%, ATK +10% (range > 100) |
| `royal_party` | 왕도 파티 | warrior + mage + priest | 매 전투 시작 시 전체 HP 10% 회복 |
| `assassin_squad` | 암살 전문대 | rogue + archer + alchemist | CRIT +20%, 크리 피해 +30% |
| `guardian_order` | 수호 결사 | warrior + priest + alchemist | 전체 DEF +20%, 10초마다 패시브 리젠 2% |

## 4. 5인 보너스 (2종)

| 키 | 이름 | 조건 | 효과 |
|---|---|---|---|
| `all_round` | 올라운드 | 파티 내 **서로 다른 클래스 5종 이상** | 전체 스탯 +5%, 길드 XP +10% |
| `specialist` | 전문 특화 | 같은 클래스 **3명 이상** 보유 | 해당 클래스 스탯 +25%, 나머지 -10% (트레이드오프) |

## 5. 시너지 충돌 / 중첩

- 동일 파티에서 여러 시너지 동시 발동 가능 (`getActiveSynergies`)
- 모든 시너지는 `apply(units, classKeys)` 함수로 스탯에 곱연산/가산
- `all_round`(보너스)과 `specialist`(특화)는 동시 발동 가능 — 조건이 다름 (분산 vs 집중)
- 시너지 비활성화 음성특성은 [traits.md](traits.md) 및 향후 정의

## 6. RunSimulator 연동

```javascript
function calcSynergyBonus(party) {
  const classKeys = party.map(m => m.classKey);
  const active = getActiveSynergies(classKeys);
  // 2인: +0.03 / 3인: +0.05 / 5인: +0.07 성공률 가산
  let bonus = 0;
  for (const syn of active) {
    if (syn.type === 2) bonus += 0.03;
    else if (syn.type === 3) bonus += 0.05;
    else if (syn.type === 5) bonus += 0.07;
  }
  return Math.min(0.15, bonus);  // 캡 +15%
}
```

## 7. UI

### DeployScene / SetupScene
- 파티 선택 패널 옆에 "활성 시너지" 박스 (실시간 갱신)
- 시너지 호버 → 어떤 클래스가 만족시켰는지 강조

### BattleScene
- 전투 시작 시 시너지 발동 토스트
- HUD 상단 미니 아이콘으로 활성 시너지 표시

## 8. TODO

- 5인 보너스 조건 (5명 출전 가능한 시점) — 길드회관 A3 이상 필요
- 시너지 카운터(=시너지 무력화 음성 특성) 라인업
- 출신/특성 기반 시너지 확장 (P2): 같은 출신 3명, 양성특성 조합 등
