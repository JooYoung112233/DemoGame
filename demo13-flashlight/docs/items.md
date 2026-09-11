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
| **합계 (SO 구현)** | **204** (`Assets/Resources/Items/` 하위 분류 폴더, 2026-06-10 실측) — 아이콘 0/204 할당. 카테고리별: Misc 69·Valuable 59·Consumable 24·Material 17·Medical 13·Key 12·Weapon 10. 아이콘 작업 목록 [`item-icon-checklist.csv`](item-icon-checklist.csv), 현황 [`art-needs.md`](art-needs.md) |

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

> ⚠️ **빛의 역설 (광원 아이템)**: **탐지등(루디 광원)만 안전**. 비루디 광원(플레어·횃불·발견한 전등·화염 무기 등)은 시야·온기에 즉효지만 그 자리에 **침식 게이지**를 쌓는 양날 — 불 계열은 가중치 최대. 광원/화염 계열 아이템 비고에 「침식 리스크」 표기. [→ gdd-core §5.2](gdd-core.md), rendering.md

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | useEffect | 비고 |
|:---:|---|---|:---:|:---:|:---:|---|---|---|
| P0 | `battery_aa` | 건전지 | 1×1 | 5 | 0.05 | Common | AddBattery | 장비 전력. ✅ id 정합 완료(2026-06-10): SO `Battery.asset` itemId=`battery_aa`로 통일 |
| P0 | `lantern_basic` | 탐지등 | 1×2 | - | 0.8 | Common | Equip(Light) | 회수꾼 소싯적 양도(S-013). 착용 시 시야(주변광+콘) 확대·증광·현상 근접 시 점멸. 토글X·상시. **루디 광원(빛의 역설 예외=안전)**. 레이드 중 **약하게 방전 → 여분 루디로 교체**(가혹X). → rendering.md, [gdd-core §5.2/§5.3](gdd-core.md) |
| P0 | `canned_food` | 통조림 | 1×1 | 3 | 0.3 | Common | Eat | 허기 +40. → survival.md |
| P0 | `water_bottle` | 물병 | 1×2 | 2 | 0.5 | Common | Drink | 수분 +45. → survival.md |
| P1 | `energy_bar` | 에너지바 | 1×1 | 4 | 0.1 | Common | Food + RestoreStamina | 약한 회복 복합 |
| P1 | `flashlight_bulb` | 전구 (랜턴 부품) | 1×1 | 2 | 0.1 | Uncommon | None | 랜턴/작업대 수리 재료 |
| P1 | `smoke_bomb` | 연막탄 | 1×1 | 2 | 0.2 | Uncommon | None | 3초 시야 차단(추후 전투) |
| P2 | `coffee` | 커피 | 1×1 | 3 | 0.1 | Common | RestoreStamina | 스태미너 회복↑, 수면 디버프↓ |
| P2 | `antidote` | 해독제 | 1×1 | 2 | 0.15 | Rare | HealInjury | 오염/이상현상 디버프(추후) |
| P2 | `repair_kit` | 수리 키트 | 2×2 | - | 1.0 | Uncommon | RepairEquipment | **무기·방어구 내구도 현장 회복**. 내구 100/25(4회), 작업대 없이 1회 +durability. ⚠️ 장비 내구도 시스템 연동 필요 |
| P3 | `ammo_rounds` | 탄약 | 1×1 | 30 | 0.02 | Uncommon | None | **거래/판매 전용(총기 미구현)**. 추후 총기 추가 시 소비 전환. 밴딧 노획·물물교환 가치 |

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

> **2026-06-05 아트/스코프 결정**: 데모 무기 아이콘은 **쇠파이프 + 나무 각목 2종만** 우선 제작. 장검·창·도끼 등 판타지·확장 무기는 세계관(현대 폐도시 즉석무기)에 맞게 추후 재정비. 현 단계는 즉석 무기 2종으로 전투 검증.

---

## 4. 방어구 (Misc 카테고리 + 장비 슬롯 연동, 2026-06-18)

**2026-06-18: equipSlot 연동 완료.** 방어구 .asset에 `equipSlot` 설정 → PlayerEquipment가 슬롯 장착 처리. 슬롯 enum: None=0, Head=1, Armor=2, Rig=3, Backpack=4.

| 우선 | itemId | 표시명 | 격자 | 무게 | 희귀 | equipSlot | 비고 |
|:---:|---|---|:---:|:---:|---|:---:|---|
| P1 | `coat_light` | 낡은 코트 | 2×2 | 1.5 | Common | 2 Armor | 피격 데미지 소폭↓ |
| P1 | `gloves_work` | 작업 장갑 | 1×1 | 0.2 | Common | 0 None | 대응 슬롯 없음(장갑 슬롯 미정) |
| P1 | `boots_rubber` | 고무장화 | 1×2 | 0.6 | Common | 0 None | 대응 슬롯 없음(신발 슬롯 미정) |
| P2 | `vest_scrap` | 고철 조끼 | 2×2 | 2.5 | Uncommon | 2 Armor | 강공 피해↓, 이동↓ |
| P2 | `helmet_bucket` | 양동이 헬멧 | 2×2 | 1.0 | Common | 1 Head | 머리 부위 보호(의료 연동) |
| P3 | `armor_night` | 밤순찰 방어구 | 2×3 | 3.5 | Rare | 2 Armor | 밤 맵 전용 드롭 |

### 4-A. 가방·리그 (장착 컨테이너, Misc / 2026-06-18 신규)

장착 시 인벤토리 격자를 확장하는 컨테이너 아이템. `equipSlot=Backpack(4)` 장착 시 `PlayerInventory.OnBackpackChanged`가 `containerWidth/Height`로 메인 격자를 교체.
저장 위치: `Assets/Resources/Items/Gear/`.

| itemId | 표시명 | footprint(격자) | 무게 | 희귀 | equipSlot | 컨테이너(W×H) | buy/sell | 비고 |
|---|---|:---:|:---:|---|:---:|:---:|:---:|---|
| `backpack_basic` | 기본 배낭 | 4×4 | 1.5 | Common | 4 Backpack | 6×6 | 3500 / 1400 | 기본 적재 확장 |
| `backpack_large` | 대형 배낭 | 5×5 | 3.0 | Uncommon | 4 Backpack | 7×8 | 9000 / 3600 | 대용량·무거움 |
| `rig_tactical` | 전술 리그 | 3×3 | 1.2 | Common | 3 Rig | 4×3 | 2500 / 1000 | 가슴 리그. ⚠️ Rig 슬롯 격자 확장은 코드 미연동(현재 Backpack 슬롯만 OnBackpackChanged 호출) — 슬롯 점유만 동작 |

> ⚠️ **Rig 격자 확장 미구현**: `PlayerEquipment.Equip`은 `EquipSlot.Backpack`일 때만 `inventory.OnBackpackChanged`를 호출. 리그는 슬롯에 장착되지만 인벤 격자는 늘지 않음. 리그 격자 확장 필요 시 코드 보강 대상(별건).
> ⚠️ **아이콘 미연결**: 3종 모두 `icon: {fileID: 0}` — 에디터에서 스프라이트 연결 필요.

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

전당포·안전가옥·스토리 진행의 중심. **충전·방전·마모가 있는 활성 자원**(단순 발광/화폐 아님). [→ gdd-core §5.2/§5.3](gdd-core.md)

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 비고 |
|:---:|---|---|:---:|:---:|:---:|---|---|
| P0 | `ruby_shard` | 루디 조각 | 1×1 | 5 | 0.1 | Uncommon | 밤 파밍 기본 단위 |
| P1 | `ruby_crystal` | 루디 결정 | 2×2 | 1 | 0.5 | Rare | 고가치, 가방 압박 |
| P1 | `ruby_dust` | 루디 가루 | 1×1 | 20 | 0.02 | Common | 제작·거래 소량 |
| P2 | `ruby_core` | 루디 코어 | 2×2 | 1 | 1.0 | Epic | 보스/이상현상, 안전가옥 Lv업 |
| P3 | `ruby_corrupted` | 오염 루디 | 1×1 | 3 | 0.15 | Epic | 이벤트·위험 거래 |

**루디 = 활성 자원 〔확정 방향 · 수치 TBD〕** ([→ gdd-core §5.3](gdd-core.md))
- **충전·방전**: 빛/전력으로 쓰면 방전 → 충전량 0이면 **「죽은 루디」**(둔탁한 돌, 거래/가공 재료). 탐지등·시설 연료로 소비.
- **순도**: 현상이 짙고 오래 머문 자리일수록 크고 순도↑(용량↑·고가). 순도 높은 루디엔 기억 잔향(스토리 단서, → §10·story.md).
- **마모**: 반복 충전 시 최대 용량↓ → 결국 영구 사망 → 신선한 루디 회수 지속 필요(경제 동기). [→ economy.md](economy.md)
- **충전 위치**: 현장(현상 노드, 빠름·위험) + 거점(루디 충전/가공대, 느림·안전). [→ crafting.md](crafting.md), [safehouse.md](safehouse.md)
- **확인 필요**: 충전량·순도를 위 itemId의 ItemInstance 상태값으로 둘지 별도 itemId(`ruby_dead` 등)로 둘지 = 구현 판단 대기. ("우클릭 사용 X / 판매·강화 전용"이던 옛 서술은 충전/가공 상호작용이 생기므로 재검토 대상.)

---

## 7. 귀중품·화폐 (Valuable)

전당포 판매 → **스크랩** 획득·정보/장비 교환. **가방 압박 vs 현금화** 루프의 핵심.

**판매 등급 (가안, 전당포 기준)**

| 등급 | 스크랩 환산 | 예시 |
|---|---:|---|
| D | 1~5 | 동전, 깨진 장식 |
| C | 8~20 | 은 악세, 구형 전자 |
| B | 25~60 | 금속, 시계, 카메라 |
| A | 80~150 | 금목걸이, 희귀 수집품 |
| S | 200+ | 루디 결정급 귀중품, 스토리 |

### 7-A. 화폐·저가 귀중 (스택 많음)

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 등급 | 비고 |
|:---:|---|---|:---:|:---:|:---:|---|:---:|---|
| ~~P0~~ | ~~`coin_scrap`~~ | 스크랩 동전 | 1×1 | 50 | 0.01 | Common | D | **2026-09-11 고철 화폐(`scrap_money`, "◈ 고철")로 합침** — 맵 스폰은 고철 화폐 ×100으로, 세이브에 남은 코인은 탈출 시 1개 = ◈100 정산([economy.md §재화 역할](economy.md)) |
| P1 | `coin_old` | 구화 | 1×1 | 30 | 0.02 | Common | D | 서랍·시체 |
| P1 | `coin_military` | 군수화 | 1×1 | 20 | 0.02 | Uncommon | C | 밤 밴딧 |
| P1 | `token_subway` | 지하철 토큰 | 1×1 | 10 | 0.03 | Common | D | 수집용, D×3 판매 |
| P1 | `coupon_faded` | 바랜 쿠폰 | 1×1 | 20 | 0.01 | Common | D | 잡템과 혼동 가능 |
| P2 | `val_chip_credit` | 신용 칩 | 1×1 | 5 | 0.02 | Rare | B | 도시 봉쇄 전 유물 |

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
| P2 | `note_nameplate` | 이름 적힌 명패 | 1×1 | Uncommon | **BD-19 회수 대상**. 폐상가에서 죽은 자의 분실물(의뢰인 안 나타남). 회수꾼 동료 테마와 간접 공명 |
| P2 | `pkg_marked` | 표식 봉투 | 1×1 | Uncommon | **BD-20 회수 대상**(익명 의뢰). 내용물 불명, 전달처마다 표식(▲) 다름. 떡밥용 |
| P3 | `key_gov_card` | 정부 키카드 | 1×1 | Epic | **지역4 게이트 키.** 지역3 「묻힌 정비창」 봉인문(M5)에서만 회수 → 연구소 본부(극장 지하) 출입증. 손실 방지(스토리). [→ story.md §4 지역3·지역4] |

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

## 지하철 수리 부품 (진행 게이트 아이템, 2026-06-10)

> 지하철 복구 = 이동/지역 해금 진행 요소. 회수해 와야 하는 **대형 중요 물품**.
> 일부는 **일반 가방에 안 들어가는 초대형(8×6)** → 특수 운반/거점 직납(=운반 미션·도파민).

| itemId | 표시명 | 격자 | 비고 |
|---|---|:---:|---|
| `subway_motor` | 전동 견인 모터 | **8×6** | 초대형. 가방 불가 → 특수 운반/직납 |
| `subway_control` | 신호 제어 유닛 | 2×2 | |
| `subway_brake` | 제동 어셈블리 | 2×3 | |
| `subway_railclamp` | 레일 체결 키트 | 2×1 | |
| `subway_pantograph` | 팬터그래프(집전기) | 3×2 | |

> 8×6은 최대 가방(택티컬 7×9)에도 안 맞음 → "들고 못 가는 물건"으로 별도 운반 룰 필요(카트/차량/거점 직납). 운반 자체가 미션.

> **이 시대의 최고가 트레이드품** (타르코프 비트코인 포지션). 전기 대부분 끊긴 세상에서
> **아직 충전이 남은 고용량 전력 셀**은 모든 세력이 탐냄. 실용(전력 공급) + 화폐적 가치.
> 루디(현상 에너지)와 대비/병존 — 루디=현상 자원, 전력 셀=문명 잔재 에너지.

| itemId | 표시명 | 격자 | 등급 | 비고 |
|---|---|:---:|---|---|
| `val_power_cell` | 충전된 전력 셀 | 1×2 | Legendary(S) | 대장 귀중품. 발광 충전 인디케이터 |
| `val_power_core` | 산업용 전력 코어 | 2×2 | Epic(S) | 대형, 더 큰 용량 |
| `val_power_bank` | 휴대 배터리팩 | 1×1 | Rare(A) | 소형 보급형 |
| `val_power_cell_drained` | 방전된 전력 셀 | 1×2 | Uncommon(C) | 충전 0, 저가 (충전/수리 떡밥) |

- 드랍: 밤 레이드·고위험 구역·금고. 희소.
- 확장 여지: 전력 셀로 시설 가동/탐지등 충전/거래 등 (추후 시스템).

## 총기·탄약·파츠 (미래 확장, 2026-06-10)

> **현 데모 = 근접 전투만** (총기 미사용). 아래는 **후반 확장 대비 개념/아이콘 선제작**용. 밸런스·구현 미정.

### 탄약 (Ammo)
| itemId | 표시명 | 격자 | 비고 |
|---|---|:---:|---|
| `ammo_pistol` | 권총탄 | 1×1 | 낱발/스택 |
| `ammo_rifle` | 소총탄 | 1×1 | |
| `ammo_shotgun` | 산탄 | 1×1 | |
| `ammobox_pistol` | 권총탄 상자 | 1×1 | 박스(다량) |
| `ammobox_rifle` | 소총탄 상자 | 1×1 | |
| `ammobox_shotgun` | 산탄 상자 | 1×1 | |

### 총기 파츠 (Gun Parts — 개념만)
| itemId | 표시명 | 격자 |
|---|---|:---:|
| `part_barrel` | 총열 | 2×1 |
| `part_magazine` | 탄창 | 1×2 |
| `part_stock` | 개머리판 | 2×1 |
| `part_grip` | 손잡이 | 1×1 |
| `part_scope` | 조준경 | 2×1 |
| `part_receiver` | 리시버/노리쇠 | 2×1 |
| `part_suppressor` | 소음기 | 2×1 |
| `part_trigger` | 방아쇠 뭉치 | 1×1 |
| `part_barrel_short` | 단총열 | 1×1 |
| `part_mag_drum` | 드럼 탄창 | 2×2 |
| `part_mag_ext` | 확장 탄창 | 1×2 |
| `part_tube_ext` | 산탄 튜브연장 | 1×2 |
| `part_reddot` | 도트 사이트 | 1×1 |
| `part_holo` | 홀로 사이트 | 1×1 |
| `part_scope_hunt` | 사냥 조준경 | 2×1 |
| `part_ironsight` | 기계식 조준기 | 1×1 |
| `part_foregrip` | 수직 손잡이 | 1×1 |
| `part_bipod` | 양각대 | 2×1 |
| `part_muzzle_brake` | 머즐 브레이크 | 1×1 |
| `part_flashhider` | 소염기 | 1×1 |
| `part_stock_fold` | 접이식 개머리판 | 2×1 |
| `part_handguard` | 핸드가드/레일 | 2×1 |
| `part_laser` | 택티컬 레이저/라이트 | 1×1 |
| `part_cleaning_kit` | 총기 청소 키트 | 1×2 |

> 아이콘만 선제작. 실제 총기 시스템 도입 시 밸런스·제작·조립 룰 별도 설계.

### 무기 부착물 (파츠) — SO 구현 4종 (2026-06-19)

> `ItemData`의 무기 파츠 필드(`weaponPartType` / `partMoveSpeedMult` / `partStaminaMult` / `partRangeBonus` / `partRecoilMult` / `partMagBonus`)를 채운 **실제 SO 4종**. 전부 `category=Misc`, `equipSlot=None`, `isUsable=false`, `hasDurability=false`. 저장 위치 `Assets/Resources/Items/Misc/WeaponPart/`. 총기 시스템은 미도입이라 효과 수치는 도입 시 활용. 아이콘 미할당(에디터 연결 필요).

| itemId | 표시명 | 종류 | 격자 | 희귀도 | 무게 | 판매/구매 | 부착 효과 |
|---|---|---|:---:|---|:---:|:---:|---|
| `scope_basic` | 조준경 | Scope | 2×1 | Uncommon | 0.3 | 40 / 120 | 사거리 +1.0, 반동 ×0.9 |
| `muzzle_basic` | 소염기 | Muzzle | 1×1 | Uncommon | 0.2 | 30 / 90 | 반동/소음 ×0.85 |
| `mag_extended` | 확장 탄창 | Magazine | 1×2 | Uncommon | 0.4 | 35 / 100 | 장탄수 +10 |
| `grip_tactical` | 전술 손잡이 | Grip | 1×1 | Common | 0.2 | 25 / 70 | 이속 ×1.05, 스태미너 ×0.92, 반동 ×0.92 |

### 무기 로스터 (세계관 재정비, 2026-06-10)
> 판타지(장검·창) 폐기 → **현대 폐도시 = 즉석 근접 + 실총기**. 아이콘 선제작, 데모 구현은 단계적.

**근접 (즉석/현실)**
| itemId | 표시명 | 격자 |
|---|---|:---:|
| `knife` | 나이프 | 2×1 |
| `pipe` | 쇠파이프 | 2×1 |
| `wood_plank` | 나무 각목 | 2×1 |
| `machete` | 마체테 | 3×1 |
| `crowbar` | 쇠지렛대 | 3×1 |
| `hatchet` | 손도끼 | 2×2 |
| `cleaver` | 식칼/정육도 | 2×1 |
| `wrench_big` | 큰 렌치 | 2×1 |
| `bat` | 야구방망이 | 3×1 |
| `fire_axe` | 소방도끼 | 2×3 |

**총기 (스캐빈저식)**
| itemId | 표시명 | 격자 |
|---|---|:---:|
| `gun_pistol` | 권총 | 2×1 |
| `gun_revolver` | 리볼버 | 2×1 |
| `gun_smg` | 기관단총 | 3×2 |
| `gun_shotgun` | 펌프 산탄총 | 4×2 |
| `gun_rifle_hunt` | 사냥용 소총 | 5×2 |
| `gun_rifle_assault` | 돌격소총 | 5×2 |
| `gun_pipe` | 즉석 파이프건 | 3×1 |

> 판타지 무기(`long_sword`·`spear`·`axe`·`anomaly_blade`) = 보류/재검토. `anomaly_blade`만 현상 제작 무기로 디자인 현대화 후 유지 검토.

## 아이템 아이콘 아트 (확정 2026-06-05)

- **시점**: 약간 위에서 본 3/4 (타르코프 인벤 아이콘식), 전 아이템 동일 각도·조명
- **화풍**: 게임 픽셀 화풍·탈채도 폐허 톤, 그리미한 생존감. 굵은 실루엣, 작은 크기에서 읽힘
- **배경**: 진한 회색 단색(키잉용) 또는 투명
- **희귀도 테두리**: 아이콘에 안 넣음 → 엔진에서 색 프레임(회/파/노/보)
- **격자 비율 준수**: 1×1 정사각, 2×1 가로, 1×2 세로, 2×2 등 footprint에 맞춰
- **앵커**: P0 13종 시트(나이프/파이프/붕대/부목/진통제/구급상자/건전지/통조림/물병/고철/천/루디조각/열쇠)가 화풍 기준. 신규 아이콘은 이 시트 첨부해 통일
- **앵커 트릭 (아이콘 일관성 표준)**: NPC와 동일 — 신규 아이콘 배치 시 **합격한 P0 아이콘 2~3개를 같은 시트 안에 앵커로 함께 생성** → 새 아이콘이 앵커 화풍·디테일에 맞춰짐. 생성 후 앵커는 버리고 새 것만 슬라이스. (따로 뽑으면 디테일/톤 드리프트)
- 제작 순서: 카테고리/우선순위별 시트로 분할 생성 (귀중품 → 무기 → 잡템 → 소비/재료 ...)

## 아이템 도감 (Codex) — ✅ 구현 완료 (2026-07-10 확정 / 2026-07-11 UI 확정·구현)

> 질문: 아이템 수집 기록(도감)을 넣나? **결정: 넣는다 — 1차 = 기록·열람만.**

- **발견 기록**: 아이템 **첫 획득 시** 발견 처리 — 세이브에 발견 itemId set 저장.
- **도감 UI(`CodexUI`)**: 카테고리 탭, **미발견 = 실루엣** 표시. ([backlog-ui.csv](backlog-ui.csv) U20)
- **1차 범위 = 기록·열람만(보상 없음).** 수집률 보상(PP/스크랩)은 **2차 검토**.
- **이상현상 카테고리(§10) 아이템의 도감 설명 = 로어 조각** — 세계관 연결 통로.
- 구현 순서: 갭 분석 통합 순서 **⑨** (dev-roadmap.md 2026-07-10).

> 근거: 낙원식 수집 동기 + 아이템 204종의 가치 표면화.

### UI·구조 결정 (2026-07-11)

| 질문 | 결정 |
|------|------|
| 진입 경로? (독립 패널+단축키 / Tab 패널 탭 / 안전가옥 시설) | **독립 패널 + `U`키** — QuestLogUI(J)·TraitPanelUI(K)와 동일 자가부트 패턴. 레이드 중에도 열람 가능. ※최초 `N` 제안했으나 **`N`은 `RaidMapUI`(레이드 지도)가 선점**(중복 폴링 시 Update 순서 미정의로 어느 쪽이 열릴지 뒤집힘) → `B` 경유 후 **사용자 지정으로 `U` 확정**. 사용 중 키: A/C/D/E/F/G/H/J/K/M/N/P/R/S/T/W (여유: I/L/O/V/X/Y). |
| 레이아웃? (격자 / 리스트) | **좌 카테고리 탭 + 가운데 아이콘 격자 + 우 상세**(클릭 시 설명·수치). 204종 훑어보기에 유리. |
| 미발견 노출 수준? | **실루엣 + 이름 `???`** — 이름·설명 모두 가림. 칸 개수로 "뭔가 더 있다"만 암시(수집 동기 최대). |

- **발견 훅** = `InventoryGrid.OnItemPlaced` 콜백(옵트인, 기본 null) → **플레이어 3격자**(가방/주머니/보안)에만 구독 → 줍기·루팅 드래그·자동배치·구매·제작 등 **플레이어가 실제로 손에 넣은 모든 경로**를 한 지점에서 포착.
- **`CodexManager`**(자가부트 싱글턴) = 발견 itemId set 보유 · 신규 발견 시 토스트 · 세이브 영속 · 새 게임 리셋.
- 수집률 = 카테고리 탭에 `발견/전체` 표기(보상 없음, 1차 범위 유지).

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-07-10 | **아이템 도감 확정 (기획, 구현 전).** 질문: 아이템 수집 기록(도감)을 넣나? **결정: 첫 획득 시 발견 기록(세이브에 itemId set) + 도감 UI 패널(카테고리 탭, 미발견=실루엣). 1차 = 기록·열람만(보상 없음), 수집률 보상(PP/스크랩)은 2차 검토. 이상현상 카테고리 아이템의 도감 설명 = 로어 조각(세계관 연결).** §아이템 도감 신설, backlog-ui.csv U20(CodexUI) 등재. 갭 분석 5종 구현 순서 4번째. | 근거: 낙원식 수집 동기, 아이템 204종의 가치 표면화. |
| 2026-06-23 | **소비 아이템 useEffect 오태깅 교정** (`Resources/Items/Consumable/*.asset`). 인게임 "먹기/사용" 미작동 버그 수정 — `PlayerInventory.UseItem`가 처리하는 작동 효과는 HealHP(1)/HealInjury(2)/AddBattery(4)/Food(5)뿐, RestoreStamina(3)는 비구현. useEffect(before→after): `canned_food` 1→5(+effectValue 25→40, §2 "허기+40"), `protein_shake` 1→5, `coffee` 0→5, `energy_soup` 0→5, `stim_injector` 0→5. `adrenaline_shot`은 HealHP(1) 유지(전투 HP 회복). coffee/energy_soup/stim_injector는 본래 RestoreStamina 의도지만 enum 미구현이라 **임시 Food(5) 대체**(포만감+수분 절반 회복) → 스태미너 소비템 효과 재설계는 기획 결정 대기(survival.md §8-A). 식음료 hasDurability는 이미 0(변경 없음). meta/GUID 미변경. |
| 2026-06-19 | **무기 부착물(파츠) SO 4종 신규.** `ItemData` 무기 파츠 필드(`weaponPartType`/`partMoveSpeedMult`/`partStaminaMult`/`partRangeBonus`/`partRecoilMult`/`partMagBonus`)를 채운 실제 SO를 `Assets/Resources/Items/Misc/WeaponPart/`에 생성: `scope_basic`(Scope, 2×1, Uncommon, 사거리+1.0·반동×0.9), `muzzle_basic`(Muzzle, 1×1, Uncommon, 반동×0.85), `mag_extended`(Magazine, 1×2, Uncommon, 장탄+10), `grip_tactical`(Grip, 1×1, Common, 이속×1.05·스태미너×0.92·반동×0.92). 전부 category=Misc·equipSlot=None·isUsable=false·hasDurability=false. ItemDatabase가 `Resources.LoadAll("Items")`로 자동 로드. 근거: 무기 부착물 시스템 도입 대비 콘텐츠 선제작. 총기 시스템 미도입이라 효과 수치는 도입 시 활용, 아이콘 미할당(에디터 연결 필요). |
| 2026-06-18 | **장착 가방·리그 신규 + 방어구 equipSlot 연동.** `Items/Gear/`에 `backpack_basic`(6×6), `backpack_large`(7×8), `rig_tactical`(4×3) 3종 신규 ItemData 생성(§4-A). 기존 방어구 4종에 equipSlot 추가: helmet_bucket=Head(1), vest_scrap/armor_night/coat_light=Armor(2). gloves_work/boots_rubber는 대응 슬롯 없어 None 유지. 3종 모두 `Shop_pawnshop` stock에 진열. 비고: Rig 격자 확장은 코드 미연동(Backpack 슬롯만 OnBackpackChanged), 신규 3종 아이콘 미연결(에디터 연결 필요). |
| 2026-06-16 | **빛의 역설/루디 활성 자원 캐논 정합(gdd-core §5.2/§5.3).** §6 루디 = 충전·방전·마모 활성 자원(죽은 루디·순도·마모·충전 위치) + SSOT 링크. §2 헤더에 비루디 광원 침식 리스크 주석, `lantern_basic` = 루디 광원(예외 안전)·약방전·여분 교체 보강. 새 itemId·수치 미생성(TBD). 충전/순도 상태 구현 방식은 확인 필요. |
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