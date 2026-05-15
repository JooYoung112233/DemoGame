# Demo9 — 길드 경영 v2 문서 인덱스

> **유지 룰**
> 1. 기획 변경 시 해당 시스템 md를 같은 작업 안에서 동기화한다 (drift 금지).
> 2. 거대한 단일 문서 대신 시스템별 분리 파일을 유지한다. 시스템 간 관계/전반 정책은 `OVERVIEW.md`에만 둔다.
> 3. 새 시스템 추가 시 `docs/systems/<system>.md` 생성 + 이 인덱스에 한 줄 추가.

---

## 상위 문서

- [OVERVIEW.md](OVERVIEW.md) — 디자인 철학, 문서 간 불일치 정리, 메인 vs 서브 구조, 12시간 페이싱 가이드, TODO 진실원본
- [WORK_PLAN.md](WORK_PLAN.md) — 현재 코드/기획 갭, 마일스톤(M1~M4), 작업 우선순위
- [SYSTEM_REDESIGN_V2.md](SYSTEM_REDESIGN_V2.md) — **(아카이브)** 분리 이전의 통합 문서. 신규 변경은 분리된 파일에 반영.

## 시스템별 문서 (`docs/systems/`)

| # | 시스템 | 파일 | 상태 |
|---|---|---|---|
| 4 | 길드 회관 (Guild Hall) | [systems/guild-hall.md](systems/guild-hall.md) | ✅ 확정 |
| 5 | RunSimulator v2 (서브 파견) | [systems/run-simulator.md](systems/run-simulator.md) | ✅ 확정 |
| 6 | 피로도 (Stamina) | [systems/stamina.md](systems/stamina.md) | ✅ 확정 |
| 7 | 장비 + 자동화 | [systems/equipment-automation.md](systems/equipment-automation.md) | ✅ 확정 |
| 8 | 특성 제거/변환 | [systems/traits.md](systems/traits.md) | ✅ 확정 |
| 9 | NPC 상인 | [systems/npc-merchants.md](systems/npc-merchants.md) | ✅ 확정 |
| 10 | 마을 이벤트 확장 | [systems/town-events.md](systems/town-events.md) | 🟡 골격만 |
| 11 | 친화도 트리 vs 구역 통제 | [systems/affinity-tree.md](systems/affinity-tree.md) | ✅ 확정 (M1) |
| — | 친밀도 / Bond | [systems/bonds.md](systems/bonds.md) | ✅ 확정 (M1) |
| — | 카드 23장 | [systems/cards.md](systems/cards.md) | ✅ 확정 (M1) |
| — | 시너지 14종 | [systems/synergies.md](systems/synergies.md) | ✅ 확정 (M1, 코드 일치) |
| — | 길드 평판 | [systems/reputation.md](systems/reputation.md) | 🟡 간이 정의 |
| — | gameState 스키마 | [systems/game-state.md](systems/game-state.md) | ✅ 확정 |

## 참고 문서

- [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md) — (구) 1차 통합 설계서. 일부 수치는 v2에서 변경됨, 참고용.
- [BALANCE_DESIGN.md](BALANCE_DESIGN.md) — (구) 밸런스 설계서. v2와 불일치 부분은 `OVERVIEW.md` §불일치 정리 참고.
