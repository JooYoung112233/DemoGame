# 시스템: 시뮬레이터 명세 (코드 구현 후 작성용)

> `balance/simulator.js` — Node.js 자동 플레이 시뮬레이터.
> 12시간 게임을 자동 진행 후 곡선 검증.

## 1. 입력

```javascript
const simConfig = {
  durationHours: 12,
  tickRateMs: 1000,           // 1초 = 게임 내 1초
  difficulty: 'normal',
  permadeath: false,
  startingState: null,        // null = 기본 시작 상태
  strategy: 'balanced',       // 'aggressive' | 'safe' | 'balanced'
  outputPath: 'sim-result.json'
};
```

## 2. 시뮬 행동 패턴

자동 플레이어 AI:

```
매 분:
  if 가용 모집 슬롯 > 0 AND 골드 > 모집비용 × 2:
    모집 (rarity 우선순위)
  if 모집 슬롯 비어있음 AND 로스터 < max:
    재모집 (10분 후 자동 갱신)

매 5분:
  if 사용 가능한 메인 도전 있음:
    수동 도전 (5-10분 소요)
    클래스 스킬 시뮬 발동

매 분:
  if 빈 서브 파견 슬롯 있음 AND 가용 용병 ≥ 2:
    서브 파견 시작 (적정 난이도 선택)

매 30분 (게임 내):
  if 마을 이벤트 가용:
    이벤트 처리 (전략에 따라 선택)

매 시간:
  if 골드 > 시설 비용:
    가능한 시설 해금
  if 골드 > 길드회관 다음 단계 비용:
    길드회관 업그레이드 (전략 우선순위)
```

## 3. 출력

```json
{
  "summary": {
    "totalHours": 12.0,
    "finalGuildLevel": 8,
    "totalGold": 318000,
    "totalGoldEarned": 412000,
    "mainRuns": 48,
    "subExpeditions": 387,
    "deaths": 2,
    "bossDefeated": ["bp", "cargo", "blackout"],
    "facilities": 10,
    "guildHallTotal": 47
  },
  "timeline": [
    { "time": "0:30", "guildLv": 2, "gold": 1020, "roster": 4 },
    { "time": "1:00", "guildLv": 2, "gold": 1850, "roster": 4 },
    ...
  ],
  "validation": {
    "goldCurve":   { "actual": 320000, "target": 320000, "deviation": "0%", "pass": true },
    "guildLvCurve":{ "lv8At": "11:15",  "target": "11:00", "deviation": "+15min", "pass": true },
    "mainSuccess": { "actual": 0.82,    "target": 0.80, "pass": true },
    "bondMax":     { "topPair": "5:23", "target": "5-6h", "pass": true },
    "deaths":      { "actual": 2, "target": "0-3", "pass": true }
  }
}
```

## 4. 검증 항목

자동으로 PASS/FAIL 판정:

| # | 항목 | 타깃 | 허용 편차 |
|---|---|---|---|
| 1 | 골드 곡선 (0:30~12:00) | 1K → 320K | ±15% |
| 2 | 길드 Lv 8 도달 시점 | 11:00 | ±30분 |
| 3 | 메인 클리어률 | 평균 80% | ±10% |
| 4 | 사망률 | 0~3명 | 5명 이하 |
| 5 | 주력 페어 Bond Max | 5~6시간 | 4-7시간 |
| 6 | 친화도 Lv 8 도달 (1구역) | 11시간 | ±2시간 |
| 7 | NPC 60+ 호감 (NPC 수) | 2~3명 | 1명 이상 |
| 8 | 보스 3종 처치 | 12시간 안 | 필수 |
| 9 | 길드회관 완성도 | 50% | 30~70% |
| 10 | 명예 포인트 획득 | 150~300 | 100 이상 |

## 5. 시뮬 전략 옵션

### 5.1 'aggressive'
- 메인 도전 빈도 높음
- 위험한 zone Lv도 도전
- 사망률 높지만 진행 빠름

### 5.2 'safe'
- 메인 도전 최소
- 서브 파견 위주
- 사망 거의 없지만 진행 느림

### 5.3 'balanced'
- 메인 + 서브 적정 비율
- 대표 플레이어 시뮬

## 6. 사용법

```bash
# 단일 시뮬
node balance/simulator.js --strategy balanced

# 다중 시뮬 (10회 평균)
node balance/simulator.js --strategy balanced --runs 10 --report avg

# 전략 비교
node balance/simulator.js --compare aggressive,safe,balanced --runs 5

# 곡선 차트 출력
node balance/simulator.js --strategy balanced --chart gold,xp
```

## 7. 결과 해석

- **PASS 9/10 이상**: 밸런스 OK
- **PASS 7/10**: 미세 조정 필요
- **PASS 5/10 이하**: 큰 조정 필요

조정 후 재시뮬, 수렴할 때까지 반복.

## 8. TODO (코드 구현 시)

- [ ] `balance/simulator.js` 작성
- [ ] CSV/JSON 결과 출력
- [ ] 차트 라이브러리 연동 (선택)
- [ ] CI 통합 (밸런스 회귀 감지)
- [ ] 자동 튜닝 모드 (`balance.js` 자동 조정 후 재시뮬)
