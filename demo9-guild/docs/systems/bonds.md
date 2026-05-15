# 시스템: 친밀도 / Bond

> 같이 굴리고 살아남은 용병끼리 관계가 누적된다. 5인 시너지와는 다른 축의 파티 강화 트랙.
> 음성 특성(고독, 질투 등)이 Bond 누적을 막거나 감소시킨다.

## 1. 누적 공식

같은 파티로 출전(메인 BattleScene + 서브 파견 둘 다)했을 때, 파티 내 모든 쌍(pair)에 Bond XP 누적.

```javascript
function updateBonds(party, success, sourceType /* 'main' | 'sub' */) {
  for (let i = 0; i < party.length; i++) {
    for (let j = i + 1; j < party.length; j++) {
      const a = party[i], b = party[j];
      const key = getBondKey(a.id, b.id);
      let gain = success ? 3 : 1;
      if (sourceType === 'main') gain *= 2;   // 메인 직접 전투는 2배
      if (a.hasTrait('lone_wolf') || b.hasTrait('lone_wolf')) gain *= 0.3;
      if (a.hasTrait('jealous')   || b.hasTrait('jealous'))   gain -= 1;
      gain = Math.max(0, Math.round(gain));
      gameState.bonds[key] = Math.min(100, (gameState.bonds[key] || 0) + gain);
    }
  }
}

// 키 정렬 — id1_id2 형태로 단방향 정규화
function getBondKey(idA, idB) {
  return idA < idB ? `${idA}_${idB}` : `${idB}_${idA}`;
}
```

서브 파견 1런당 약 1-3, 메인 1전투당 약 6 → Bond 50 도달까지 약 20-30 동반 출전.

## 2. 티어 (5단계)

| 티어 | Bond XP | 이름 | 효과 (같이 출전 시) |
|---|---|---|---|
| 0 | 0-9 | 낯선 사이 | 없음 |
| 1 | 10-29 | 동료 | 둘 다 +2% ATK, +2% DEF |
| 2 | 30-49 | 형제/자매 | 둘 다 +5% 모든 스탯 |
| 3 | 50-74 | 전우 | 둘 다 +8% 모든 스탯, 한쪽 사망 시 다른 한 명 ATK +20% (1분) |
| 4 | 75-99 | 영혼의 동반자 | 둘 다 +12% 모든 스탯, **페어 스킬 해금** |
| 5 | 100 | 운명 공동체 | +15% 모든 스탯, 페어 스킬 강화판, 사망 시 동반자 1회 부활(HP 30%) |

티어 효과는 두 사람이 **같은 파티로 출전했을 때만** 적용. 따로 출전 중이면 비활성.

## 3. 페어 스킬 (티어 4+ 해금)

클래스 조합별로 1개씩 정의. 두 캐릭터가 모두 살아 있고 같은 전투/파견에 있을 때 자동 발동(쿨다운 있음).

| 조합 | 페어 스킬 | 효과 |
|---|---|---|
| warrior + warrior | **방패의 벽** | 둘 다 DEF +30%, 받는 피해 10% 상호 분담 (30초 쿨) |
| warrior + priest | **성기사의 맹세** | priest 힐량 +50% + warrior에 즉시 실드 (40초 쿨) |
| warrior + rogue | **양동 작전** | rogue 다음 공격 100% 크리 + warrior 도발 1초 (25초 쿨) |
| warrior + mage | **메테오 슬램** | warrior 위치에 AoE (mage ATK × 3) (45초 쿨) |
| warrior + archer | **방패 보호** | archer가 warrior 뒤에 있으면 archer 피격 무효 (조건부, 쿨 없음) |
| warrior + alchemist | **포션 콤보** | alchemist가 warrior에 즉시 회복 포션 사용 (60초 쿨) |
| rogue + rogue | **이중 암살** | 같은 적에 동시 공격 시 확정 크리 + 다음 1회 회피 (20초 쿨) |
| rogue + priest | **그림자 가호** | rogue 회피 +30%, priest 힐 시 rogue 스텔스 1초 (35초 쿨) |
| rogue + mage | **암흑 술책** | mage 다음 스킬 + rogue 출혈 같이 적용 (30초 쿨) |
| rogue + archer | **연환 사격** | archer 공격 후 rogue 즉시 추격타 (15초 쿨) |
| rogue + alchemist | **독 비수** | rogue 다음 공격에 독 부여 (강한 DoT) (25초 쿨) |
| mage + mage | **이중 영창** | 둘 다 다음 스킬 즉시 발동 (1회) (60초 쿨) |
| mage + priest | **성스러운 폭염** | mage AoE에 priest 힐 부수 효과 (40초 쿨) |
| mage + archer | **마법화살** | archer 다음 화살이 관통 + 마법 피해 (30초 쿨) |
| mage + alchemist | **연금 폭발** | alchemist 폭탄이 mage 속성 폭발 (35초 쿨) |
| archer + archer | **일제 사격** | 둘 다 동시에 같은 적 사격 (확정 크리) (30초 쿨) |
| archer + priest | **거리 가호** | archer 사거리 +30%, priest 원거리 힐 가능 (패시브) |
| archer + alchemist | **독화살 강화** | archer 화살에 독 + 폭발 효과 (30초 쿨) |
| priest + priest | **이중 축복** | 전체 힐 +100% 1회 (90초 쿨) |
| priest + alchemist | **종합 회복** | 전체 힐 + 디버프 해제 (60초 쿨) |
| alchemist + alchemist | **이중 폭격** | AoE 폭탄 2회 동시 투하 (45초 쿨) |

총 21쌍. **첫 출시에 다 구현은 무리** — 우선 [warrior+priest, rogue+mage, mage+priest, archer+rogue, priest+priest] 5쌍 우선 구현, 나머지는 P2로 미룸.

## 4. 음성 특성 상호작용

| 음성 특성 | 효과 |
|---|---|
| `lone_wolf` (고독한 늑대) | Bond 누적 ×0.3, 티어 보너스 받지 않음 (페널티 1.0배) |
| `jealous` (질투) | Bond 누적 -1 / 매번, 다른 동료가 페어 스킬 발동 시 자신 ATK -5% (10초) |
| `hothead` (성마름) | 50% 확률로 Bond 누적량 -50% |
| `coward` (겁쟁이) | 동반자 사망 시 자신 30% 패닉 (5초 행동 불가) |

음성 특성 라인업 전체 정의는 [traits.md](traits.md) (TBD).

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

- 한쪽이 사망하면 Bond 값은 **그대로 보존** (영구 기록). 동반자 사망 트리거(티어 3+: ATK +20%, 티어 5: 부활)는 사망 즉시 1회 발동.
- 사망한 용병과의 Bond는 새로운 용병에게 양도 불가.

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
| 1-2시간 | 0-20 | 0 |
| 3-5시간 | 30-50 (티어 2) | 0 |
| 6-8시간 | 50-75 (티어 3) | 0-1 |
| 9-11시간 | 75-100 (티어 4-5) | 2-3 |
| 11-12시간 | 100 다수 | 4-5 |

→ 후반에 페어 스킬이 점점 켜지는 게임플레이.

## 9. TODO

- 페어 스킬 5쌍 1회차 구현 후 나머지 16쌍 확장 (P2)
- 신전 "관계 재정립" — Bond 한 쌍 초기화 (혹시 잘못된 조합 풀고 싶을 때, 비용 비쌈) — 검토
- Bond 그래프 시각화 (RosterScene)
