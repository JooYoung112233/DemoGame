# 시스템: 장비 + 자동화

## 1. 장비 슬롯 (4슬롯)

| 슬롯 | 주 효과 |
|---|---|
| weapon | ATK, CRIT |
| armor | DEF, HP |
| trinket | 특수 (CRIT/SPD/특성 강화) |
| consumable (1회용) | 전투 시작 시 자동 소비 (포션 등) |

## 2. 자동 장착 모드

- 클래스 추천 (warrior=HP/DEF, mage=ATK/CRIT...)
- 최고 ATK 우선
- 최고 HP+DEF 우선
- 균형
- 수동

**발동 시점:**
- 새 장비 획득 즉시 (옵션)
- 파견 출발 직전 (체크박스)
- 수동 "로스터 최적화" 버튼

## 3. 장비 잠금

자물쇠 표시 → 자동 처분/교체에서 제외.

## 4. 자동 처분

규칙 (체크박스):
- common 자동 판매
- uncommon 자동 판매
- 잠금 안된 중복 자동 판매
- 보관함 80% 차면 common부터

처분 방식: 즉시 판매 (70% 가치) 또는 위탁 등록.

## 5. 일괄 작업 패널

```
[전체 최적화] [잡템 일괄판매] [전원 휴식] [전원 치유]
[자동장착 ON/OFF] [자동판매 ON/OFF]
```

## 6. 비교 툴팁

장비 호버 시 현재 장착과 스탯 +/- 색상 비교.

## 7. gameState

```javascript
autoEquipMode: 'manual',  // 'manual' | 'class' | 'atk' | 'hpdef' | 'balanced'
autoSellRules: {
  sellCommon: false,
  sellUncommon: false,
  sellDuplicates: false,
  sellOnOverflow: false
},
lockedItems: []
```
