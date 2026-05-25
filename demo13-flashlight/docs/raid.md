# 레이드 시스템

## 현재 상태

### 레이드 타이머
- 제한시간: **15분** (900초)
- HUD: 화면 상단 중앙 `MM:SS` 표시
  - 2분 이하: 주황색 경고
  - 30초 이하: 빨간색 깜빡임
- 시간초과: 아이템 50% 랜덤 손실 후 안전가옥 강제 귀환

### 탈출
- ExitPoint 타입 InteractableObject 사용
- `exitWaitTime > 0` 설정 시 카운트다운 대기 (거리 이탈 시 취소)
- 탈출 성공 시 RaidManager에 기록 → Safehouse 복귀 → RaidResultUI 표시

### 루팅
- LootContainer에 `initialLoot` 배열로 초기 아이템 설정 (Inspector)
  - itemId, count, chance(스폰확률) per entry
- 상자 열면(E키) 내부 아이템 전부 자동으로 PlayerInventory로 이동
- 공간 부족 시 남은 아이템은 상자에 유지
- 모든 아이템 획득 시 IsLooted = true (재상호작용 시 "비어있음" 표시)
- 획득한 아이템은 RaidManager에 추적 기록

### 귀환 정산 (RaidResultUI)
- Safehouse 씬 로드 시 자동 표시
- 실제 데이터 표시:
  - 생존 시간
  - 획득 아이템 목록 (희귀도 색상 포함)
  - 아이템 총 개수/무게
  - 아이템 총 가치 (sellPrice 합산)
- Enter/클릭으로 닫기

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-25 | 레이드 시스템 v1 구현. RaidManager(타이머+루트추적), LootContainer(initialLoot+자동루팅), RaidResultUI(실데이터 연동), InteractableObject(탈출 시 RaidManager 알림, Pickup 시 루트 추적). |
