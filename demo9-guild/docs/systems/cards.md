# 시스템: 전투 카드

> 범용 카드 풀은 사용하지 않음. 각 구역의 전투 메카닉에 맞는 **구역 전용 카드**를 별도 설계한다.

## 구역별 카드 시스템

| 구역 | 전투 방식 | 카드 방향 | 상태 | 문서 |
|---|---|---|---|---|
| Blood Pit | 라운드 + 뱀서식 선택지 | 3택1 임시 능력 (런 한정) | ✅ 확정 | [zone-bloodpit.md](zone-bloodpit.md) |
| Cargo | 열차 칸 방어 + 덱빌딩 | 카드 3택1 획득 → 역 정차 시 활성화 | ✅ 확정 | [zone-cargo.md](zone-cargo.md) |
| Blackout | 타일 뒤집기 탐색 | 방 발견 시 이벤트 선택지 | ✅ 확정 | [zone-blackout.md](zone-blackout.md) |

### Cargo 칸 카드 (기존)

`CARGO_CAR_CARDS`: 탄약/의무/발전/화물 칸별 5장 × 4종 = 20장. 기존 `cards.js`에 구현됨.

### Blood Pit / Blackout 카드

각 구역의 핵심 메카닉과 연동되는 전용 카드로 별도 기획 예정.
- BP: 핏 게이지 조작, 관중 반응, 투기장 룰 변경 등
- BO: 저주 레벨 조작, 시야 변경, 탐색 보너스 등

## 기존 코드 참고

`cards.js`의 22장(CARD_DATA)과 20장(CARGO_CAR_CARDS)은 기존 코드. 구역별 재설계 시 참고용으로 유지.
