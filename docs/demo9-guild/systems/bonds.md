# 시스템: 친밀도 / Bond

> 같이 굴리고 살아남은 용병끼리 관계가 누적된다. 5인 시너지와는 다른 축의 파티 강화 트랙.
> 음성 특성(고독, 질투 등)이 Bond 누적을 막거나 감소시킨다.
>
> **🟢 구현 완료 (MVP, P1):** 본드 누적 / 티어 stat 보너스 / UI (DeployScene 미리보기, ManualBattleScene 표시, RosterScene 상세, BondScene 관계도). 페어 스킬은 P2.

## 1. 누적 공식

같은 파티로 출전(메인 BattleScene + 서브 파견 둘 다)했을 때, 파티 내 모든 쌍(pair)에 Bond XP 누적.

```javascript
function updateBonds(party, success, sourceType /* 'main' | 'sub' */) {
  for (let i = 0; i < party.length; i++) {
    for (let j = i + 1; j < party.length; j++) {
      const a = party[i], b = party[j];
      const key = getBondKey(a.id, b.id);
      let gain = success ? 4 : 2;
      if (sourceType === 'main') gain *= 2;   // 메인 직접 전투는 2배
      if (a.hasTrait('lone_wolf') || b.hasTrait('lone_wolf')) gain *= 0.3;
      if (a.hasTrait('jealous')   || b.hasTrait('jealous'))   gain -= 1;
      gain = Math.max(0, Math.round(gain));
      gameState.bonds[key] = Math.min(100, (gameState.bonds[key] || 0) + gain);
    }
  }
}

function getBondKey(idA, idB) {
  return idA < idB ? `${idA}_${idB}` : `${idB}_${idA}`;
}
```

| 상황 | 획득량 |
|---|---|
| 서브 파견 성공 | +4 |
| 서브 파견 실패 | +2 |
| 메인 전투 성공 | +8 |
| 메인 전투 실패 | +4 |

약 15~20회 동반 출전 → 티어 4(페어 스킬), 25~30회 → Max(100). 5~6시간에 주력 쌍 Max 도달.

## 2. 티어 (5단계)

| 티어 | Bond XP | 이름 | 효과 (같이 출전 시) |
|---|---|---|---|
| 0 | 0-9 | 낯선 사이 | 없음 |
| 1 | 10-29 | 동료 | 둘 다 +2% ATK, +2% DEF |
| 2 | 30-49 | 형제/자매 | 둘 다 +5% 모든 스탯 |
| 3 | 50-74 | 전우 | 둘 다 +8% 모든 스탯, 한쪽 사망 시 다른 한 명 ATK +20% (1분) |
| 4 | 75-99 | 영혼의 동반자 | 둘 다 +12% 모든 스탯, **페어 스킬 해금** |
| 5 | 100 | 운명 공동체 | +15% 모든 스탯, 페어 스킬 강화판, 사망 시 동반자 1회 부활(HP 30%) |

티어 효과는 두 사람이 **같은 파티로 출전했을 때만** 적용.

## 3. 페어 스킬 (티어 4+ 해금)

클래스 조합별 1개씩 정의. 두 캐릭터가 모두 살아 있고 같은 전투/파견에 있을 때 발동.

### 같은 클래스 (6쌍)

| 조합 | 이름 | 타입 | 효과 |
|---|---|---|---|
| warrior×2 | 방패의 벽 | 패시브 | 둘 다 DEF +20%, 받는 피해 10% 상호 분담 |
| rogue×2 | 이중 암살 | 액티브 20s | 같은 적 동시 공격, 확정 크리 + 회피 1회 |
| mage×2 | 이중 영창 | 액티브 50s | 둘 다 다음 스킬 즉시 발동 + 위력 +30% |
| archer×2 | 일제 사격 | 액티브 25s | 동시 사격 (확정 크리 + 방어 무시) |
| priest×2 | 이중 축복 | 액티브 60s | 전체 즉시 힐 + 10s 리젠 |
| alchemist×2 | 이중 폭격 | 액티브 40s | AoE 폭탄 2연속 + 화상 |

### 다른 클래스 (15쌍)

| 조합 | 이름 | 타입 | 효과 |
|---|---|---|---|
| warrior+rogue | 양동 작전 | 액티브 30s | warrior 도발 + rogue 다음 공격 크리 확정 |
| warrior+mage | 마법 방패 | 액티브 40s | warrior 피해 흡수 + mage 시전 속도 +30% |
| warrior+archer | 방패 엄호 | 패시브 | archer가 warrior 뒤 위치 시 피격 -50% |
| warrior+priest | 성기사의 맹세 | 액티브 35s | warrior 즉시 실드 + priest 힐량 +40% |
| warrior+alchemist | 강화 갑옷 | 액티브 45s | warrior에 방어 물약 (DEF +50%, 10s) |
| rogue+mage | 암흑 연격 | 액티브 30s | rogue 출혈 + mage 마법 복합 피해 |
| rogue+archer | 연환 사격 | 액티브 15s | archer 공격 후 rogue 즉시 추격타 |
| rogue+priest | 그림자 가호 | 패시브 | rogue 회피 +20%, 회피 시 HP 소량 회복 |
| rogue+alchemist | 독 비수 | 액티브 25s | rogue 공격에 강화 독 (DoT + 둔화) |
| mage+archer | 마법 화살 | 액티브 30s | archer 화살에 관통 + 스플래시 부여 |
| mage+priest | 성스러운 폭염 | 액티브 40s | mage AoE에 아군 회복 부수 효과 |
| mage+alchemist | 연금 폭발 | 액티브 35s | 합동 AoE (양쪽 ATK 합산 피해) |
| archer+priest | 축복의 화살 | 패시브 | 사거리 +30%, 적중 시 아군 소량 힐 |
| archer+alchemist | 폭발 화살 | 액티브 30s | 화살에 폭발 효과 (AoE + DoT) |
| priest+alchemist | 만능 치유 | 액티브 50s | 전체 힐 + 디버프 해제 + 면역 5s |

패시브 3쌍 / 액티브 18쌍. 우선 구현 5쌍: warrior+priest, rogue+mage, mage+priest, archer+rogue, priest+priest. 나머지 16쌍은 P2.

## 4. 음성 특성 상호작용

| 음성 특성 | 효과 |
|---|---|
| `lone_wolf` (고독한 늑대) | Bond 누적 ×0.3, 티어 보너스 받지 않음 |
| `jealous` (질투) | Bond 누적 -1 / 매번, 다른 동료 페어 스킬 발동 시 자신 ATK -5% (10초) |
| `hothead` (성마름) | 50% 확률로 Bond 누적량 -50% |
| `coward` (겁쟁이) | 동반자 사망 시 자신 30% 패닉 (5초 행동 불가) |

음성 특성 전체 정의는 [traits.md](traits.md) 참고.

## 5. UI

### RosterScene → Bond 탭

```
관계도 그래프:
  - 노드: 용병 (살아있는 자만)
  - 엣지: Bond 50+ 인 관계만 표시 (티어 색상)
  - 호버: 티어, XP, 페어 스킬 미리보기
```

### DeployScene 출전 시

선택한 파티 내 페어 스킬 발동 가능 조합을 박스로 표시.

## 6. 사망 처리

- 한쪽 사망 시 Bond 값 **영구 보존**. 사망 트리거(티어 3+: ATK +20%, 티어 5: 부활)는 즉시 1회 발동.
- 사망한 용병과의 Bond는 새 용병에게 양도 불가.

## 7. gameState

```javascript
bonds: {
  // 'mercId1_mercId2': bondValue (0-100)
  // 사망자 포함, 영구 보존
},
pairSkillCooldowns: {
  // 'mercId1_mercId2_skillKey': nextAvailableMs
}
```

## 8. 페이싱

| 시점 | 평균 Bond | 활성 페어 |
|---|---|---|
| 1-2시간 | 10-30 (티어 1) | 0 |
| 3-4시간 | 40-60 (티어 2-3) | 0 |
| 5-6시간 | 75-100 (티어 4-5) | 2-3 |
| 7-12시간 | 다수 Max | 4-5+ |

→ 중반(5~6시간)부터 페어 스킬이 켜지기 시작.
