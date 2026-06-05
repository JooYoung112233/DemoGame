# 아이템 기획 리스트

## 개요

**BRB** 컨셉에 맞춘 아이템 목록 (v2).  
근접 생존 루팅 + 15분 탈출 + 낮(정보) / 밤(루디·고위험) + 부위별 의료 + 안전가옥 성장을 전제로 한다.

**v2 방향**: 치료템·잡템·전당포 판매용 귀중품 비중 확대 → 파밍 시 「쓸모없어 보이지만 팔면 돈 되는 것」과 「다음 출전을 위한 치료 선택」이 늘어나도록.

- 데이터 형식: `ItemData` SO (`Assets/Resources/Items/{분류}/`)
- 의료 전용 효과: `MedicalItemData` 연동 (`HealInjury`)
- 상세 격자/인벤 규칙: `docs/inventory.md`
- 파밍 오브젝트 출처: 기획서 v2 §12.1

### 우선순위 범례

| 표기 | 의미 |
|---|---|
| **P0** | Stage 3~4 데모 — 인벤·줍기·사용 검증용 최소 세트 |
| **P1** | 폐상가 1맵 파밍 + 전투 루프 연동 |
| **P2** | 안전가옥 제작/전당포/밤 전용 |
| **P3** | 스토리·이상현상·후반 확장 |

### itemId 규칙

- 소문자 + 언더스코어 (`bandage`, `ruby_shard`)
- 스토리/열쇠: `key_`, `note_`, `map_` 접두
- 무기: `{무기명}` 또는 `{무기명}_worn` (내구도 낮은 드롭)
- 잡템: `junk_` 접두 (판매 전용·제작 불가)
- 귀중품: `val_` 접두 (전당포 고가)

### 분량 요약 (v2)

| 분류 | 종 수 (약) |
|---|---:|
| §1 치료·생존 | **5** (축소) |
| §2 소비·유틸 | 8 |
| §3~6 무기·방어·재료·루디 | 28 |
| §7 귀중품·화폐 | 28 |
| §8 정보·열쇠 | 8 |
| §9 잡템 | 38 |
| §10 이상현상·스토리 | 5 |
| **합계 (기획)** | **~147** |
| **합계 (SO 구현)** | **~172** (`Assets/Resources/Items/` 하위 분류 폴더) |

---

## 1. 치료·생존 (Medical) — **핵심 5 + 일회용 8 (SO 13종)**

**2026-05-25 결정**: 의료 32종 → **5종** (핵심만).  
**2026-05-25 보완**: 일회용(스택) 변형 **8종** 복구 → **총 13종**. 외상/수술 키트·수혈·제작재료 등은 보류.

### 핵심 (5)

| itemId | 표시명 | 격자 | 소비 | 비고 |
|---|---|:---:|---|---|
| `bandage` | 붕대 | 1×2 | 스택 3 | 출혈. medical: 붕대 |
| `splint` | 부목 | 1×2 | 스택 2 | 골절. medical: 부목 |
| `painkiller` | 진통제 | 1×1 | 스택 5 | 통증. medical: 진통제 |
| `first_aid_kit` | 구급상자 | 2×2 | **내구 300/50 (6회)** | HealHP +40/회 |
| `ifak_injury` | 부상 IFAK | 2×2 | **내구 300/60 (5회)** | HealInjury, 구급상자 medical |

### 일회용·변형 (8, 스택)

| itemId | 표시명 | 격자 | 스택 | 비고 |
|---|---|:---:|---:|---|
| `gauze_roll` | 거즈 롤 | 1×1 | 5 | 가벼운 출혈 |
| `bandage_compress` | 압박 붕대 | 1×2 | 2 | 출혈 강화 |
| `hemostatic_powder` | 지혈 분말 | 1×1 | 4 | 즉시 지혈 (희귀) |
| `tourniquet` | 지혈대 | 1×2 | 2 | 사지 출혈 |
| `splint_makeshift` | 임시 부목 | 1×2 | 3 | 골절 응급 |
| `morphine_ampule` | 모르핀 앰플 | 1×1 | 2 | 강한 통증 (레어) |
| `disinfectant` | 소독약 | 1×1 | 3 | 감염 예방 |
| `antidote` | 해독제 | 1×1 | 2 | 중독·부상 키트 계열 |

**삭제 유지 (SO 없음)**: 외상키트·수술키트·수혈팩·HP 소형키트·의료 제작재료 등

### 1-0. 소비 방식 — 스택 vs 내구도 (구현 반영)

코드·기획 확정: `docs/inventory.md` §내구도, `ItemData.hasDurability` / `PlayerInventory.UseItem`.

| 방식 | ItemData | 사용 시 | UI | 대표 |
|---|---|---|---|---|
| **스택** | `hasDurability=false`, `maxStack`≥2 | `stackCount--` | `x3` 표시 | 붕대, 부목, 진통제 |
| **내구도** | `hasDurability=true`, `maxStack=1` | `durability -= durabilityCostPerUse` | 현재/최대 바 | 구급상자, 부상 IFAK |

**내구도 아이템 공통 규칙**
- `maxStack` = **1** (스택 불가, `ItemInstance.CanStackWith` 거부)
- 남은 횟수 ≈ `floor(durability / durabilityCostPerUse)` (`RemainingUses`)
- `durability <= 0` → 격자에서 **파괴·제거**
- 드롭/루팅 시 **개별 인스턴스**마다 잔량 다를 수 있음 (풀 내구도 vs 반쯤 쓴 것)

**분류 기준 (치료템)**

| 등급 | 소비 | 이유 |
|---|---|---|
| 소형 전용 (붕대·진통제·앰플) | 스택 | 1회 1개 쓰고 버리는 느낌, 타르코프 붕대 팩 |
| 중형 키트 (2×2 이상, IFAK급) | 내구도 | 한 칸에 여러 번 — 가방 효율 vs 무게 트레이드 |
| 주사·앰플 1회극 | 스택 | `morphine_ampule`, `adrenaline_shot` |
| 봉합·정형·수술 키트 | 내구도 | 도구 상자; 여러 처치 분량 |

**구현 참고 (`first_aid_kit`)**

| 필드 | 현재 SO 값 | 기획 의도 |
|---|---|---|
| `useEffect` | HealHP | 전투 중 HP 급회복 (IFAK). 부상 치료용 키트는 HealInjury + `medicalData` |
| `effectValue` | 40 | 1회당 HP 회복량 |
| `maxDurability` | 300 | |
| `durabilityCostPerUse` | 50 | **6회** 사용 |
| `maxStack` | 1 | 내구도형 |

부상 치료 전용 구급상자를 추가할 때는 별도 itemId(예: `ifak_injury`)로 HealInjury + medical 연동 권장.

---

### (보류) 확장 의료 목록

압박 붕대·외상 키트·수술 키트·모르핀·의료 제작재 등은 문서에 아이디어로만 남기고 SO 미생성. 복구 시 Git/CSV 이력 참고.

---

## 2. 소비·유틸 (Consumable)

랜턴(시야)·스태미너·탈출 15분 루프와 직결.

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | useEffect | 비고 |
|:---:|---|---|:---:|:---:|:---:|---|---|---|
| P0 | `battery_aa` | 건전지 | 1×1 | 5 | 0.05 | Common | AddBattery | 장비 전력 (랜턴 연료는 추후 옵션) |
| P0 | `lantern_basic` | 랜턴 | 1×2 | - | 0.8 | Common | Equip(Light) | 착용 시 시야(주변광+콘) 확대·증광. 토글X·상시. → rendering.md |
| P0 | `canned_food` | 통조림 | 1×1 | 3 | 0.3 | Common | Eat | 허기 +40. → survival.md |
| P0 | `water_bottle` | 물병 | 1×2 | 2 | 0.5 | Common | Drink | 수분 +45. → survival.md |
| P1 | `energy_bar` | 에너지바 | 1×1 | 4 | 0.1 | Common | Food + RestoreStamina | 약한 회복 복합 |
| P1 | `flashlight_bulb` | 전구 (랜턴 부품) | 1×1 | 2 | 0.1 | Uncommon | None | 랜턴/작업대 수리 재료 |
| P1 | `smoke_bomb` | 연막탄 | 1×1 | 2 | 0.2 | Uncommon | None | 3초 시야 차단(추후 전투) |
| P2 | `coffee` | 커피 | 1×1 | 3 | 0.1 | Common | RestoreStamina | 스태미너 회복↑, 수면 디버프↓ |
| P2 | `antidote` | 해독제 | 1×1 | 2 | 0.15 | Rare | HealInjury | 오염/이상현상 디버프(추후) |

---

## 3. 근접 무기 (Weapon)

총기 없음. 내구도는 `ItemInstance.durability` (향후).

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 비고 |
|:---:|---|---|:---:|:---:|:---:|---|---|
| P0 | `knife` | 나이프 | 2×1 | 1 | 0.3 | Common | 빠름, 짧은 사거리, 스태미너↓ |
| P0 | `pipe` | 파이프 | 2×1 | 1 | 0.9 | Common | 기본 균형형 |
| P1 | `axe` | 도끼 | 3×2 | 1 | 2.2 | Uncommon | 강공 그로기↑, 느림 |
| P1 | `hammer` | 망치 | 2×2 | 1 | 2.0 | Uncommon | 방어 파괴·그로기 특화 |
| P1 | `long_sword` | 장검 | 2×3 | 1 | 1.8 | Rare | 사거리·안정 |
| P2 | `spear` | 창 | 1×3 | 1 | 1.5 | Uncommon | 거리 유지, 실내 약함 |
| P2 | `bat` | 야구방망이 | 3×1 | 1 | 1.2 | Common | 넓은 스윙, 좁은 통로 불리 |
| P2 | `pipe_worn` | 녹슨 파이프 | 2×1 | 1 | 0.9 | Common | 내구도 낮은 드롭 전용 |
| P3 | `anomaly_blade` | 오염 칼날 | 2×2 | 1 | 1.0 | Epic | 이상현상 잔해 제작, 특수 패시브 |

**데모 P0**: 나이프 + 파이프만 구현해도 전투 검증 가능.

---

## 4. 방어구 (Weapon 카테고리 또는 Misc — 장비 슬롯 연동 전까지 Misc)

장비 슬롯 UI 전까지는 인벤에만 들고 있는 **착용 예정** 아이템.

| 우선 | itemId | 표시명 | 격자 | 무게 | 희귀 | 비고 |
|:---:|---|---|:---:|:---:|---|---|
| P1 | `coat_light` | 낡은 코트 | 2×2 | 1.5 | Common | 피격 데미지 소폭↓ |
| P1 | `gloves_work` | 작업 장갑 | 1×1 | 0.2 | Common | 강공 차징 안정(추후) |
| P1 | `boots_rubber` | 고무장화 | 1×2 | 0.6 | Common | 물웅덩이·오염 지형 |
| P2 | `vest_scrap` | 고철 조끼 | 2×2 | 2.5 | Uncommon | 강공 피해↓, 이동↓ |
| P2 | `helmet_bucket` | 양동이 헬멧 | 2×2 | 1.0 | Common | 머리 부위 보호(의료 연동) |
| P3 | `armor_night` | 밤순찰 방어구 | 2×3 | 3.5 | Rare | 밤 맵 전용 드롭 |

---

## 5. 제작 재료 (Material)

안전가옥 작업대·의료대·발전기 강화용.

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 주요 출처 |
|:---:|---|---|:---:|:---:|:---:|---|---|
| P0 | `scrap_metal` | 고철 조각 | 1×1 | 10 | 0.2 | Common | 서랍, 작업대, 시체 |
| P0 | `cloth_rag` | 천 조각 | 1×1 | 10 | 0.05 | Common | 캐비닛, 시체 |
| P0 | `screw` | 나사 | 1×1 | 20 | 0.02 | Common | 작업대, 편의점 창고 |
| P1 | `wire` | 전선 | 1×1 | 8 | 0.1 | Common | 이상현상 잔해, 전기 관련 |
| P1 | `tool_part` | 공구 부품 | 1×2 | 5 | 0.3 | Uncommon | 작업대, 잠긴 상자 |
| P1 | `chemical_flask` | 화학 플라스크 | 1×1 | 4 | 0.2 | Uncommon | 약품함, 실험실 |
| P2 | `wood_plank` | 판자 | 2×1 | 5 | 0.8 | Common | 방어시설 업그레이드 |
| P2 | `fuel_can` | 연료통 | 2×2 | 2 | 1.5 | Uncommon | 발전기 |
| P2 | `circuit_board` | 회로 기판 | 1×1 | 3 | 0.15 | Rare | 루디 관련 제작 |

---

## 6. 루디 (Valuable — 핵심 자원)

전당포·안전가옥·스토리 진행의 중심. **스택 가능, 판매/강화 전용**(우클릭 사용 X).

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 비고 |
|:---:|---|---|:---:|:---:|:---:|---|---|
| P0 | `ruby_shard` | 루디 조각 | 1×1 | 5 | 0.1 | Uncommon | 밤 파밍 기본 단위 |
| P1 | `ruby_crystal` | 루디 결정 | 2×2 | 1 | 0.5 | Rare | 고가치, 가방 압박 |
| P1 | `ruby_dust` | 루디 가루 | 1×1 | 20 | 0.02 | Common | 제작·거래 소량 |
| P2 | `ruby_core` | 루디 코어 | 2×2 | 1 | 1.0 | Epic | 보스/이상현상, 안전가옥 Lv업 |
| P3 | `ruby_corrupted` | 오염 루디 | 1×1 | 3 | 0.15 | Epic | 이벤트·위험 거래 |

---

## 7. 귀중품·화폐 (Valuable)

전당포 판매 → 정보/장비/루디 교환. **가방 압박 vs 현금화** 루프의 핵심.

**판매 등급 (가안, 전당포 기준)**

| 등급 | 루디 환산 | 예시 |
|---|---:|---|
| D | 1~5 | 동전, 깨진 장식 |
| C | 8~20 | 은 악세, 구형 전자 |
| B | 25~60 | 금속, 시계, 카메라 |
| A | 80~150 | 금목걸이, 희귀 수집품 |
| S | 200+ | 루디 결정급 귀중품, 스토리 |

### 7-A. 화폐·저가 귀중 (스택 많음)

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 등급 | 비고 |
|:---:|---|---|:---:|:---:|:---:|---|:---:|---|
| P0 | `coin_scrap` | 루디 | 1×1 | 50 | 0.01 | Common | D | 가장 흔한 「돈」 |
| P1 | `coin_old` | 구화 | 1×1 | 30 | 0.02 | Common | D | 서랍·시체 |
| P1 | `coin_military` | 군수화 | 1×1 | 20 | 0.02 | Uncommon | C | 밤 밴딧 |
| P1 | `token_subway` | 지하철 토큰 | 1×1 | 10 | 0.03 | Common | D | 수집용, D×3 판매 |
| P1 | `coupon_faded` | 바랜 쿠폰 | 1×1 | 20 | 0.01 | Common | D | 잡템과 혼동 가능 |
| P2 | `val_chip_credit` | 신용 칩 | 1×1 | 5 | 0.02 | Rare | B | 정부 붕괴 전 유물 |

### 7-B. 악세·귀금속

| 우선 | itemId | 표시명 | 격자 | 무게 | 희귀 | 등급 | 비고 |
|:---:|---|---|:---:|:---:|---|:---:|---|
| P1 | `val_ring_copper` | 구리 반지 | 1×1 | 0.03 | Common | D | |
| P1 | `val_ring_silver` | 은반지 | 1×1 | 0.05 | Uncommon | C | |
| P1 | `val_earring_pair` | 귀걸이 한 쌍 | 1×1 | 0.04 | Uncommon | C | |
| P1 | `val_bracelet` | 팔찌 | 1×1 | 0.06 | Uncommon | C | |
| P1 | `val_necklace_gold` | 금목걸이 | 1×1 | 0.08 | Rare | A | 금고 |
| P2 | `val_chain_gold` | 금 사슬 | 1×2 | 0.12 | Rare | A | |
| P2 | `val_dental_gold` | 금니 | 1×1 | 0.02 | Uncommon | B | 시체 전용 |
| P2 | `val_gem_loose` | 빠진 보석 | 1×1 | 0.03 | Rare | B | |
| P3 | `val_ring_wedding` | 결혼 반지 | 1×1 | 0.05 | Epic | A | 스토리 단서 가능 |

### 7-C. 시계·전자 (전기 끊긴 세계의 「옛날 값」)

| 우선 | itemId | 표시명 | 격자 | 무게 | 희귀 | 등급 | 비고 |
|:---:|---|---|:---:|:---:|---|:---:|---|
| P1 | `val_watch_pocket` | 회중시계 | 1×1 | 0.1 | Rare | B | |
| P1 | `val_watch_wrist` | 손목시계 | 1×1 | 0.08 | Uncommon | C | |
| P1 | `val_watch_broken` | 망가진 시계 | 1×1 | 0.12 | Common | D | 부품 분해(추후) |
| P1 | `val_radio_hand` | 휴대 라디오 | 1×2 | 0.4 | Uncommon | C | |
| P2 | `val_camera_film` | 필름 카메라 | 2×2 | 0.6 | Uncommon | B | |
| P2 | `val_phone_old` | 구형 휴대폰 | 1×1 | 0.15 | Common | C | |
| P2 | `val_laptop_dead` | 죽은 노트북 | 2×2 | 1.2 | Uncommon | B | 작업대 분해 |
| P2 | `val_calculator` | 계산기 | 1×1 | 0.1 | Common | D | |
| P3 | `val_watch_brother` | 동생의 시계 | 1×1 | 0.1 | Legendary | S | 스토리, 손실 방지 |

### 7-D. 수집·예술·특이품

| 우선 | itemId | 표시명 | 격자 | 무게 | 희귀 | 등급 | 비고 |
|:---:|---|---|:---:|:---:|---|:---:|---|
| P1 | `val_coin_collect` | 기념주화 세트 | 1×2 | 0.15 | Uncommon | C | |
| P1 | `val_stamp_book` | 우표책 | 1×2 | 0.3 | Uncommon | C | |
| P2 | `val_statue_mini` | 소형 조각상 | 2×2 | 0.4 | Epic | A | 금고 |
| P2 | `val_painting_small` | 소형 그림 | 2×2 | 0.5 | Rare | B | |
| P2 | `val_vinyl_record` | LP 판 | 1×2 | 0.35 | Common | C | |
| P2 | `val_chess_set` | 체스 세트 | 2×2 | 0.8 | Uncommon | B | |
| P2 | `val_whiskey_bottle` | 위스키 빈병 | 1×2 | 0.6 | Uncommon | C | NPC 호의도(추후) |
| P3 | `val_id_government` | 정부 신분증 | 1×1 | 0.02 | Epic | A | 정보 판매 |

### 7-E. 밤·이상현상 연계 고가

| 우선 | itemId | 표시명 | 격자 | 무게 | 희귀 | 등급 | 비고 |
|:---:|---|---|:---:|:---:|---|:---:|---|
| P2 | `val_artifact_shard` | 유물 파편 | 1×1 | 0.1 | Rare | B | 이상현상 잔해 |
| P2 | `val_black_pearl` | 검은 진주 | 1×1 | 0.05 | Epic | A | 전당포 주인 이벤트 |
| P3 | `val_ruby_set` | 루디 세공 세트 | 2×2 | 0.3 | Legendary | S | 루디+금속 복합 |

### 7-F. 도파민 고가 (「이거 챙긴다」)

2×2·Epic/Legendary·금고/밤 전용. 가방 4칸을 쓰지만 전당포에서 한 방에 터지는 느낌.

| itemId | 표시명 | 격자 | 희귀 | 등급 | SO | 비고 |
|---|---|:---:|---|:---:|---|---|
| `ruby_crystal` | 루디 결정 | 2×2 | Epic | S | ✅ | 루디 + 부피 압박 |
| `val_statue_mini` | 소형 조각상 | 2×2 | Epic | A | ✅ | 금고 |
| `val_painting_small` | 소형 그림 | 2×2 | Epic | A~B | ✅ | 금고 |
| `val_black_pearl` | 검은 진주 | 1×1 | Epic | A | ✅ | 이상현상·NPC |
| `val_necklace_gold` | 금목걸이 | 1×1 | Rare | A | ✅ | |
| `val_watch_pocket` | 회중시계 | 1×1 | Rare | B | ✅ | |
| `val_ruby_set` | 루디 세공 세트 | 2×2 | Legendary | S | ✅ | 최고 티어 |
| `val_gold_bar` | 금괴 | 2×1 | Epic | S | ✅ | 2칸 무게 |
| `val_diamond_ring` | 다이아 반지 | 1×1 | Epic | A | ✅ | |
| `val_antique_vase` | 골동 화병 | 2×3 | Epic | A | ✅ | 6칸 압박 |
| `val_platinum_chain` | 백금 목걸이 | 1×2 | Epic | A | ✅ | |
| `val_emerald_brooch` | 에메랄드 브로치 | 1×1 | Epic | A | ✅ | |
| `trauma_kit` | 외상 키트 | 2×3 | Rare | — | ✅ | **내구 5회**, 치료 도파민 |
| `first_aid_kit` | 구급상자 | 2×2 | Uncommon | — | ✅ | **내구 6회** HP |

---

## 8. 정보·열쇠 (Key / Misc)

낮 조사 → 밤 탈출/상자 연동.

| 우선 | itemId | 표시명 | 격자 | 희귀 | 비고 |
|:---:|---|---|:---:|---|---|
| P0 | `key_rusty` | 녹슨 열쇠 | 1×1 | Common | 잠긴 상자 기본 |
| P1 | `key_warehouse` | 창고 열쇠 | 1×1 | Uncommon | 지역 고정 스폰 |
| P1 | `key_rooftop` | 옥상 열쇠 | 1×1 | Uncommon | 조건 탈출구 |
| P1 | `note_scrap` | 낡은 쪽지 | 1×1 | Common | 상호작용만, 인벤 불필요(오브젝트) |
| P1 | `map_fragment` | 지도 조각 | 1×2 | Rare | 루디 위치 후보 표시 |
| P2 | `code_paper` | 암호 메모 | 1×1 | Uncommon | 금고 비밀번호 |
| P2 | `badge_guard` | 경비 배지 | 1×1 | Uncommon | NPC 호의도·문 개방 |
| P3 | `photo_brother` | 흐릿한 사진 | 1×1 | Epic | 스토리, 손실 방지 |

---

## 9. 잡템 (Misc — Junk)

**역할**: 맵을 채우고, 「이건 팔까? 버릴까?」 고민을 만든다. 대부분 `category: Misc`, `isUsable: false`, 전당포 **D~C등급** 일괄 매입.

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 판매 | 주요 출처 |
|:---:|---|---|:---:|:---:|:---:|---|:---:|---|
| P0 | `junk_paper_stack` | 낡은 서류 뭉치 | 1×1 | 5 | 0.1 | Common | D | 서랍 |
| P0 | `junk_plastic_bottle` | 빈 플라스틱 | 1×1 | 8 | 0.05 | Common | D | 편의점 |
| P0 | `junk_glass_shard` | 유리 조각 | 1×1 | 10 | 0.1 | Common | D | 바닥 |
| P1 | `junk_spoon` | 휘어진 숟가락 | 1×1 | 5 | 0.05 | Common | D | 주방 |
| P1 | `junk_fork_bent` | 구부러진 포크 | 1×1 | 5 | 0.05 | Common | D | |
| P1 | `junk_can_empty` | 빈 깡통 | 1×1 | 8 | 0.08 | Common | D | |
| P1 | `junk_cigarette_pack` | 담배 갑 (빈) | 1×1 | 6 | 0.02 | Common | D | 시체 |
| P1 | `junk_lighter_empty` | 기름 없는 라이터 | 1×1 | 4 | 0.03 | Common | D | |
| P1 | `junk_sock_pair` | 짝 맞는 양말 | 1×1 | 3 | 0.05 | Common | D | 캐비닛 |
| P1 | `junk_hat_cap` | 낡은 모자 | 1×2 | 2 | 0.15 | Common | D | |
| P1 | `junk_shoe_single` | 한 짝 신발 | 1×2 | 2 | 0.3 | Common | D | |
| P1 | `junk_book_novel` | 찢어진 소설 | 1×2 | 3 | 0.4 | Common | D | |
| P1 | `junk_magazine` | 잡지 | 1×2 | 4 | 0.25 | Common | D | |
| P1 | `junk_photo_frame` | 액자 (사진 없음) | 1×2 | 2 | 0.5 | Common | D | |
| P1 | `junk_toy_bear` | 곰 인형 | 1×2 | 2 | 0.2 | Common | D | 아동용품 |
| P1 | `junk_doll_head` | 인형 머리 | 1×1 | 2 | 0.15 | Uncommon | D | 공포 연출 |
| P1 | `junk_cassette` | 카세트 테이프 | 1×1 | 5 | 0.08 | Common | D | |
| P1 | `junk_cd_scratched` | 긁힌 CD | 1×1 | 6 | 0.05 | Common | D | |
| P1 | `junk_remote_tv` | TV 리모컨 | 1×1 | 3 | 0.12 | Common | D | |
| P1 | `junk_lightbulb_dead` | 나간 전구 | 1×1 | 4 | 0.1 | Common | D | |
| P1 | `junk_battery_dead` | 방전 건전지 | 1×1 | 6 | 0.08 | Common | D | 배터리와 혼동 주의 |
| P1 | `junk_wire_tangle` | 엉킨 전선 | 1×1 | 4 | 0.15 | Common | D | `wire`와 구분 |
| P1 | `junk_rag_dirty` | 더러운 걸레 | 1×1 | 5 | 0.1 | Common | D | `cloth_rag` 하위 |
| P1 | `junk_rope_short` | 짧은 밧줄 | 1×2 | 3 | 0.25 | Common | C | 제작 재료로 승격 가능 |
| P1 | `junk_nail_box` | 못 상자 | 1×1 | 4 | 0.3 | Common | C | |
| P1 | `junk_tape_duct` | 덕트 테이프 | 1×1 | 3 | 0.15 | Common | C | |
| P1 | `junk_glue_tube` | 접착제 | 1×1 | 4 | 0.12 | Common | C | |
| P2 | `junk_toilet_paper` | 두루마리 휴지 | 1×2 | 4 | 0.2 | Common | D | |
| P2 | `junk_soap_bar` | 비누 | 1×1 | 5 | 0.15 | Common | D | |
| P2 | `junk_toothbrush` | 칫솔 | 1×1 | 4 | 0.05 | Common | D | |
| P2 | `junk_comb` | 빗 | 1×1 | 3 | 0.04 | Common | D | |
| P2 | `junk_mirror_shard` | 거울 조각 | 1×1 | 3 | 0.1 | Uncommon | D | |
| P2 | `junk_ashtray` | 재떨이 | 1×1 | 2 | 0.2 | Common | D | |
| P2 | `junk_bottle_glass` | 유리병 | 1×2 | 4 | 0.35 | Common | D | |
| P2 | `junk_food_moldy` | 곰팡이 음식 | 1×1 | 3 | 0.25 | Common | D | 사용 시 디버프(개그) |
| P2 | `junk_mug_chipped` | 이가 나간 머그 | 1×1 | 4 | 0.2 | Common | D | |
| P2 | `junk_kettle` | 주전자 | 1×2 | 2 | 0.6 | Common | C | |
| P2 | `junk_pan_rust` | 녹슨 프라이팬 | 2×2 | 2 | 0.9 | Common | C | |
| P2 | `junk_typewriter` | 타자기 | 2×2 | 3.5 | Uncommon | C | 사무실 |
| P2 | `junk_printer` | 고장 프린터 | 2×3 | 1 | 4.0 | Uncommon | B | 분해 시 `screw` 다량 |
| P2 | `junk_bike_wheel` | 자전거 바퀴 | 2×2 | 1 | 1.5 | Uncommon | C | |
| P2 | `junk_sign_road` | 도로 표지판 조각 | 2×1 | 1 | 2.0 | Uncommon | C | 야외 |
| P3 | `junk_mannequin_arm` | 마네킹 팔 | 1×3 | 1 | 0.8 | Rare | C | 밤 공포 |

**잡템 스폰 비율 가안**: 일반 컨테이너 드롭의 **40~55%**를 잡템·D등급 귀중품으로 — 좋은 것 나올 때까지 「쓰레기」가 쌓이게.

---

## 10. 이상현상·스토리 (Misc / Valuable)

밤 전용·미션·단서.

| 우선 | itemId | 표시명 | 격자 | 희귀 | 비고 |
|:---:|---|---|:---:|---|---|
| P2 | `anomaly_residue` | 현상 잔해 | 1×2 | Uncommon | 제작, 이상현상 잔해 스폰 |
| P2 | `mark_chalk` | 표식 분필 | 1×1 | Common | 반복 골목 힌트 |
| P2 | `clock_part` | 멈춘 시계 부품 | 1×1 | Rare | 멈춘 시계 이벤트 |
| P3 | `black_liquid` | 검은 액체 | 1×1 | Epic | 오염, 해독제 상대 |
| P3 | `whisper_tape` | 속삭임 테이프 | 1×1 | Rare | 라디오·스토리 |

> 📦 **제작·획득 분리:** 음식 재료·조리 음식·의료대 레시피·파밍 오브젝트 매핑·데모 구현 순서는 → [items-crafting-farming.md](items-crafting-farming.md)

---

## 13. 구현된 ItemData SO — 폴더 구조

`Assets/Resources/Items/` 아래 **종류별 하위 폴더**. `ItemDatabase`는 `Resources.LoadAll<ItemData>("Items")`로 **하위 폴더 포함** 전부 로드.

```
Items/
├── Weapon/          (9)
├── Medical/         (13)   ← 핵심 5 + 일회용 8
├── Consumable/      (23)   ← 조리 재료 6 + 결과물 5 (§11-A)
├── Material/        (12)
├── Valuable/        (54)
├── Key/             (11)   ← +5 조리 레시피 (§11-A)
└── Misc/
    ├── Junk/        (56)
    ├── Armor/       (6)
    └── Story/       (6)
```

Unity 재생 시 **202종** 아이템 + **10종** `RecipeData` (`Data/Recipes/`).

| 폴더 | SO 수 | 비고 |
|---|---:|---|
| Valuable/ | 54 | 루디·귀중·도파민 |
| Misc/Junk/ | 56 | 잡템 |
| Consumable/ | 23 | 음식·음료·조리 재료·**조리 결과 5** |
| Data/Recipes/ | 10 | 조리 5 + 의료 5 |
| Material/ | 12 | 재료 |
| Weapon/ | 9 | |
| Key/ | 11 | 열쇠·**조리 레시피 5** |
| Misc/Armor/ | 6 | |
| Misc/Story/ | 6 | |
| Medical/ | **13** | 핵심 5 + 거즈·압박·지혈·지혈대·임시부목·모르핀·소독·해독 |

**재생성**: `tools/items.csv` → `powershell -File tools/GenerateFromCsv.ps1` (분류 폴더에 생성, 기존 itemId 스킵)  
**조리 레시피**: `tools/cooking_recipes.csv` → `powershell -File tools/GenerateCookingRecipes.ps1`  
**의료 레시피**: `tools/medical_recipes.csv` → `powershell -File tools/GenerateMedicalRecipes.ps1`  
**에디터 수동 추가**: [`docs/crafting.md`](crafting.md)  
**지역 루트**: `tools/region_loot.csv` → `Assets/Resources/region_loot.txt` 복사 (또는 `GenerateRegionItems.ps1`)  
**재정리**: 루트에 흩어진 경우 `powershell -File tools/ReorganizeItems.ps1`

**테스트**: 플레이 → DebugTestUI → 「아이템」 탭

---

## 14. 밸런스 가이드 (초안)

| 항목 | 가이드 |
|---|---|
| P0 가방 5×8 | 무기 1 + 치료 2~3 + 소비 1~2 + **잡템/귀중으로 가방 압박** |
| 1×1 재료·잡템 | 무게 0.02~0.2, 스택 5~20 |
| 2×2 이상 | 무게 0.5~4.0, 스택 1~2 |
| 잡템 D등급 | 1칸 ≈ 루디 1~2개 — 10칸 팔아야 통조림 1개 느낌 |
| 귀중 C~A | 「루디 1조각 vs 금목걸이」 고민 — A는 조각 2~3개 분량 |
| 약품함 | 치료템 비율 **높음** (40%), 서랍은 잡템+귀중 **혼합** |
| 밤 15분 | 루디 3~8 / 귀중 B이상 0~2 / 잡템은 출구 근처에서 버리기 유도 |

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-25 | 아이템 기획 리스트 초안 작성. P0~P3 우선순위, 9분류, 파밍 매핑, Stage 3~4 구현 순서. |
| 2026-05-25 | v2: 치료 8→32종, 잡템 §9 신설 38종, 귀중 6→28종, 합계 ~147종. 판매 등급·파밍 매핑 확장. |
| 2026-05-25 | v2.1: 치료 §1-0 내구도 규칙 추가. 키트/IFAK=내구도, 붕대·앰플=스택. `first_aid_kit` SO 수치 반영, `ifak_injury` 분리 제안. |
| 2026-05-25 | §7-F 도파민 고가 추가. `Assets/Resources/Items/`에 SO 27종 신규 생성(총 30종). 내구 5종. §13 구현 목록. |
| 2026-05-25 | 기획표 전량 SO화: `tools/items.csv` + `GenerateFromCsv.ps1`로 **141종 추가 → 총 171종**. 도파민 5종 추가. |
| 2026-05-25 | Items 폴더 종류별 정리: Weapon/Medical/Consumable/Material/Valuable/Key/Misc/{Junk,Armor,Story,Other}. |
| 2026-05-25 | 의료 SO 32→5종 축소. 잡템·귀중품 25종 추가. 총 164종. 파밍 비중: 귀중~33%·잡템~34%·의료~3%. |
| 2026-05-25 | 의료 일회용 8종 복구(거즈·압박·지혈분·지혈대·임시부목·모르핀·소독·해독). Medical 13종, 총 172종. |
| 2026-05-25 | 전 ItemData SO에 sellPrice/buyPrice 반영 (`tools/ItemPrices.ps1`). §7 판매 등급과 연동. |
| 2026-05-25 | 지역 전용 14종 (`reg_*`, `Items/Regional/`). 드롭: `docs/region-loot.md`. |