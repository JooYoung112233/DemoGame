# 시스템: 카드 23장

> 메인 직접 전투(BattleScene)의 능동 개입 요소. 라운드 시작에 3장 중 1장 선택, 효과는 해당 런 종료까지 누적.
> 동일 카드 중복 획득 불가.

## 1. 카테고리 구조 (23장)

| 카테고리 | 장수 | 컨셉 |
|---|---|---|
| 공격 | 6 | ATK / CRIT 강화 |
| 방어 | 5 | HP / DEF 강화 |
| 유틸 | 5 | SPD / 특수 패시브 |
| 자원 | 4 | 즉시 효과 (회복/골드/드롭) |
| 전설 | 3 | 강력한 트리거형 효과 (등장 확률 ↓) |

## 2. 등장 규칙

```javascript
function generateCardChoices(round, collectedCards, zoneLevel) {
  const available = CARD_KEYS.filter(k => {
    if (collectedCards.includes(k)) return false;
    const card = CARD_DATA[k];
    if (card.tier === 2 && round < 3 && zoneLevel < 2) return false;
    if (card.tier === 3 && (round < 5 || zoneLevel < 3)) return false;
    if (card.category === 'legendary') {
      // 전설은 라운드5+ AND 구역Lv4+ AND 8% 확률 게이트
      if (round < 5 || zoneLevel < 4) return false;
      if (Math.random() > 0.08) return false;
    }
    return true;
  });
  return shuffle(available).slice(0, 3);
}
```

## 3. 공격 (6장)

| 키 | 이름 | Tier | 효과 |
|---|---|---|---|
| `atk_1` | 날카로운 칼날 | 1 | 전체 ATK +5% |
| `atk_2` | 전투 본능 | 2 | 전체 ATK +8% |
| `atk_3` | 살기 | 3 | 전체 ATK +12% |
| `crit_1` | 예리한 눈 | 1 | 전체 CRIT +3% |
| `crit_2` | 약점 포착 | 2 | 전체 CRIT +5% |
| `crit_3` | 급소 간파 | 3 | CRIT +8% + 크리피해 +20% |

## 4. 방어 (5장)

| 키 | 이름 | Tier | 효과 |
|---|---|---|---|
| `hp_1` | 강인한 체력 | 1 | 전체 HP +8% |
| `hp_2` | 생존 의지 | 2 | 전체 HP +12% |
| `hp_3` | 불굴 | 3 | 전체 HP +15% |
| `def_1` | 두꺼운 갑옷 | 1 | 전체 DEF +3 |
| `def_2` | 단단한 방진 | 2 | 전체 DEF +5 |

## 5. 유틸 (5장)

| 키 | 이름 | Tier | 효과 |
|---|---|---|---|
| `spd_1` | 가벼운 발걸음 | 1 | 전체 SPD +8% |
| `spd_2` | 신속 대형 | 2 | 전체 SPD +12% |
| `allstat` | 전투 숙련 | 3 | ATK/DEF/SPD +5% |
| `lifesteal` | 흡혈 본능 | 2 | 처치 시 HP 3% 회복 |
| `laststand` | 최후의 저항 | 3 | HP 25%↓ 시 전투력 +20% |

## 6. 자원 (4장)

| 키 | 이름 | Tier | 효과 |
|---|---|---|---|
| `heal_1` | 응급 치료 | 1 | 즉시 전체 HP 15% 회복 |
| `heal_2` | 전장 치유 | 2 | 즉시 전체 HP 25% 회복 |
| `golden_hands` | 황금손 | 2 | 골드 획득 +20% (런 종료까지) |
| `lucky` | 행운의 기운 | 3 | 드랍 희귀도 +1 (런 종료까지) |

## 7. 전설 (3장)

> **등장 확률**: 라운드 5+ 이상, 구역 Lv4+ 이상, 셔플 시 추가로 8% 게이트 통과해야 풀에 등장.

| 키 | 이름 | Tier | 효과 |
|---|---|---|---|
| `heal_3` | 대치유 | L | 즉시 전체 HP 40% 회복 |
| `critdmg_1` | 치명 강화 | L | 크리 피해 +30% (런 종료까지) |
| `twilight_ward` | 황혼의 가호 ⭐ | L | HP 30%↓ 시 무적 5초 1회 발동 (런 종료까지, 파티 전원) |

⭐ = **신규 추가 카드** (기존 22 → 23 달성)

## 8. 데이터 마이그레이션 노트

기존 `cards.js`의 `category` 필드 → 새 카테고리로 재태깅:

| 기존 | 새 카테고리 |
|---|---|
| `buff` (atk/hp/def/spd/crit) | `attack` 또는 `defense` 또는 `utility` (효과별) |
| `combat` (critdmg_1, lifesteal, crit_3, laststand) | `attack` (crit_3, critdmg_1) / `utility` (lifesteal, laststand) |
| `heal` (heal_1/2/3) | `resource` (heal_1, heal_2) / `legendary` (heal_3) |
| `loot` (golden_hands, lucky) | `resource` |

**구현 시**: `cards.js` 카드 객체에 `category: 'attack' | 'defense' | 'utility' | 'resource' | 'legendary'` 필드 추가하고, 기존 `category` 문자열은 호환을 위해 alias로 둠 (또는 일괄 교체).

## 9. Cargo 칸 카드는 별개

`CARGO_CAR_CARDS` (탄약/의무/발전/화물 칸별 5장 × 4 = 20장)는 Cargo 전용 시스템으로 본 23장 풀과는 별개 트랙. 본 시스템은 BattleScene + BlackoutBattleScene 라운드 카드에만 적용.

## 10. UI

- 라운드 시작 시 카드 선택 패널: 3장 펼치기 → 1장 클릭
- 카드 호버 시 효과 텍스트 + 카테고리 색상 (공격 빨강, 방어 파랑, 유틸 노랑, 자원 초록, 전설 자홍)
- 이번 런 누적 카드 목록 사이드 패널

## 11. TODO

- 신규 카드 `twilight_ward` 코드 구현 (BattleUnit에 invincibility 트리거 추가)
- 카드 카테고리 시각화 (배경색/아이콘)
- 친화도 트리/시너지로 특정 카드 등장 확률 가중치 (예: 핏 노드 `executioner` 보유 시 crit 카드 +10%)
