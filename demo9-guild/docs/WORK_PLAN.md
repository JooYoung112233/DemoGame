# Demo9 — 작업 계획 (v2 신규 기획 적용)

> **기준 문서**: [OVERVIEW.md](OVERVIEW.md) + [systems/](systems/)
> **갱신 룰**: 기획 변경 시 해당 시스템 md를 같은 작업에서 동기화.

## 1. 현 상태 요약 (2026-05-15 기준)

| # | 시스템 | 코드 상태 | 기획 상태 | 주요 파일 |
|---|---|---|---|---|
| 1 | 길드 회관 (8×12) | ❌ 미구현 | ✅ 확정 | systems/GuildManager.js |
| 2 | RunSimulator v2 | 🟡 v1 수준 | ✅ 확정 | systems/RunSimulator.js |
| 3 | activeExpeditions (다중 파견) | ❌ 미구현 | ✅ 확정 | — |
| 4 | 피로도 (Stamina) | ❌ 미구현 | ✅ 확정 | entities/Mercenary.js |
| 5 | 친밀도 / Bond | ❌ 미구현 | ⬜ TODO | — |
| 6 | NPC 호감도 / 매물 | ❌ 미구현 | ✅ 확정 | data/merchants.js |
| 7 | 자동화 (장착/판매) | ❌ 미구현 | ✅ 확정 | — |
| 8 | 친화도 트리 | 🟡 UI만 | ⬜ TODO | scenes/AffinityScene.js |
| 9 | 카드 23장 | 🟡 17/23 | ⬜ TODO | data/cards.js |
| 10 | 시너지 14종 | 🟡 12/14 | ⬜ TODO | data/synergies.js |
| 11 | 마을 이벤트 40+ | 🟡 16/40 | 🟡 골격 | data/events.js |

## 2. 작업 마일스톤 (제안 순서)

### M1 — 기획 P0 완성 (문서) ✅ 완료

1. ✅ **친화도 트리** ([systems/affinity-tree.md](systems/affinity-tree.md)) — 구역별 11 정의 / 9 획득 노드 × 3 = 정의 33, 획득 27
2. ✅ **Bond 시스템** ([systems/bonds.md](systems/bonds.md)) — 5티어 + 21쌍 페어 스킬(우선 구현 5쌍)
3. ✅ **카드 23장** ([systems/cards.md](systems/cards.md)) — 공격 6 / 방어 5 / 유틸 5 / 자원 4 / 전설 3 (`twilight_ward` 1장 신규)
4. ✅ **시너지 14종** ([systems/synergies.md](systems/synergies.md)) — 기존 코드(`data/synergies.js`)와 일치, 문서화만 진행

### M2 — 핵심 시스템 코드 (게임 작동에 필요)

5. **gameState 마이그레이션** — `SaveManager.js`에 신규 필드 추가
   (`guildHall`, `guildReputation`, `activeExpeditions`, `pendingResults`,
    `stamina`, `bonds`, `npcFavor`, `npcInventory`, `rumors`, `autoEquipMode`,
    `autoSellRules`, `lockedItems`)
6. **길드 회관 시스템** — `systems/GuildHallManager.js` + `scenes/GuildHallScene.js`
   - 카테고리 8 × 단계 12 데이터 (`data/guildHallUpgrades.js`)
   - 비용/게이트/효과 조회 API
   - `getZoneControlBonus(state, zone, key)` 헬퍼 (RunSimulator가 사용)
7. **RunSimulator v2 재작성** — 기존 단순 확률 → 파워 비율 / 시그모이드 / 부분 성공
   - `calcPartyPower`, `calcSuccessRate`, `calcRoundsCompleted`,
     `calcDeathChance`, `calcRewards`, `calcExpeditionTime`
8. **다중 파견 슬롯 (activeExpeditions)** — 시간 경과형
   - `systems/ExpeditionManager.js` (Date.now 기반 갱신, 만료 시 결과 반환)
   - `TownScene` HUD에 진행 바, 결과 알림
9. **피로도 시스템** — `Mercenary.stamina` + 회복 로직
   - 마을 자동 회복(분당), 술집/신전 시설, 메인 전투/파견 차감 훅

### M3 — UX/풍성함

10. **자동화 토글** — 자동 장착 / 자동 판매 / 자동 재파견
11. **NPC 호감도/매물 시스템** — 호감도 누적, 매물 인벤 로테이션
12. **친화도 트리 코드 적용** — AffinityScene에 정의된 노드 연결
13. **카드/시너지 정의 23/14로 확장**
14. **마을 이벤트 16 → 40+ 보강**

### M4 — 통합 시뮬 & 밸런싱

15. 클래스 베이스 스탯, 희귀도 풀, 골드/XP 커브 재산출
16. 12시간 페이싱 정밀 시뮬

## 3. 다음 작업

M1 기획이 끝났으므로 **M2 코드 작업**으로 이동.

추천 순서:
1. **gameState 마이그레이션** — 모든 신규 필드 한 번에 추가 (`SaveManager.js`)
2. **길드 회관** — `GuildHallManager.js` + `data/guildHallUpgrades.js` + `scenes/GuildHallScene.js` + `getZoneControlBonus` 헬퍼
3. **RunSimulator v2 재작성** — 파워 비율 / 시그모이드 / 부분 성공
4. **다중 파견 슬롯** — `ExpeditionManager.js` + Town HUD 통합
5. **피로도** — `Mercenary.stamina` + 시설 회복 훅
6. **자동화 토글** — 자동 장착/판매/재파견
7. **NPC 호감도/매물**
8. **친화도 트리 코드 적용**
9. **신규 카드 `twilight_ward` 코드 추가**

(M3 풍성함, M4 통합 시뮬은 이후)

## 4. 룰 재확인

- 기획 md 변경 시 코드 작업과 같은 작업/커밋 안에서 동기화.
- 시스템 md는 시스템 단위로만 (큰 통합 문서 금지).
- 새 시스템 추가 시 `INDEX.md`에 한 줄 등록.
