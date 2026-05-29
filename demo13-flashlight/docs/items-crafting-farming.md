# 아이템 — 제작 · 획득 · 파밍 매핑

> [items.md](items.md)에서 분리. 아이템 **목록/데이터**는 items.md, 여기는 **어떻게 만들고 어디서 얻는가**.
> 관련: [crafting.md](crafting.md)(레시피 시스템), [region-loot.md](region-loot.md)(지역 드롭).

---
## 11-A. 음식 재료 & 조리 음식 (Consumable — 조리대 연동)

**2026-05-26 결정**: 조리대에서 음식 재료 → 버프 음식 제작. 레시피는 맵에서 루팅하여 해금.

### 음식 재료 (맵 파밍)

**SO 구현 (2026-05-26)**: `Assets/Resources/Items/Consumable/` — 카테고리 Consumable, `isUsable=0`.

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 희귀 | 주요 출처 |
|:---:|---|---|:---:|:---:|:---:|---|---|
| P2 | `ingredient_meat` | 고기 조각 | 1×1 | 5 | 0.3 | Common | 주방, 냉장고, 식당 |
| P2 | `ingredient_vegetable` | 마른 채소 | 1×1 | 8 | 0.1 | Common | 주방, 캐비닛 |
| P2 | `ingredient_salt` | 소금 | 1×1 | 10 | 0.05 | Common | 주방, 편의점 |
| P2 | `ingredient_herb` | 허브 | 1×1 | 6 | 0.05 | Uncommon | 약품함, 화장실 |
| P2 | `ingredient_flour` | 밀가루 | 1×2 | 3 | 0.5 | Common | 편의점, 주방 |
| P2 | `ingredient_sugar` | 설탕 | 1×1 | 8 | 0.1 | Common | 편의점, 주방 |

### 조리 결과물 (버프 음식)

**SO 구현 (2026-05-26)**: `Consumable/` — `isUsable=1`, `Food`/`RestoreStamina`(임시). 레시피 SO·에디터 방법: [`crafting.md`](crafting.md).

| 우선 | itemId | 표시명 | 격자 | 스택 | 무게 | 레시피 재료 (예시) | 버프 효과 |
|:---:|---|---|:---:|:---:|:---:|---|---|
| P2 | `cooked_stew` | 고기 스튜 | 1×1 | 2 | 0.4 | 고기+채소+소금 | 피로도 대폭 회복, 포만감↑ |
| P2 | `cooked_bread` | 구운 빵 | 1×1 | 3 | 0.2 | 밀가루+설탕 | 포만감↑, 체력 소량 재생 |
| P2 | `herbal_tea` | 허브차 | 1×1 | 2 | 0.15 | 허브+물병 | 수분↑↑, 피로 회복↑ |
| P2 | `energy_soup` | 활력 수프 | 1×1 | 2 | 0.3 | 채소+소금+물병 | 스태미나 회복 속도 버프 (일정 시간) |
| P3 | `special_meal` | 특제 요리 | 1×2 | 1 | 0.5 | 고급 레시피 | 복합 버프 (피로+수분+체력재생) |

### 레시피 아이템 (맵 루팅 → 해금)

**SO 구현 (2026-05-26)**: `Key/` — `isUsable=1`, `CraftingSystem.TryUnlockFromItem`으로 해금.

| 우선 | itemId | 표시명 | 격자 | 희귀 | 해금 요리 | 출처 |
|:---:|---|---|:---:|---|---|---|
| P2 | `recipe_stew` | 스튜 레시피 | 1×1 | Common | `cooked_stew` | 주방, 식당 |
| P2 | `recipe_bread` | 빵 레시피 | 1×1 | Common | `cooked_bread` | 편의점, 주방 |
| P2 | `recipe_tea` | 허브차 레시피 | 1×1 | Uncommon | `herbal_tea` | 약품함, 사무실 |
| P2 | `recipe_soup` | 수프 레시피 | 1×1 | Uncommon | `energy_soup` | 캐비닛, 주방 |
| P3 | `recipe_special` | 특제 요리법 | 1×1 | Rare | `special_meal` | 잠긴 상자, NPC 보상 |

- 레시피는 **사용 시 영구 해금** (소비 후 사라짐)
- 같은 레시피 중복 획득 시 → NPC에게 판매 가능
- 카테고리: `Key` (레시피 문서류) 또는 `Misc`

---

## 11-B. 의료대 제작 레시피 (일회용만)

**2026-05-26 결정**: 의료대는 일회용 치료템만 제작 가능. 고급(구급상자, 수술 키트 등)은 NPC 구매 전용.

**SO 구현 (2026-05-26)**: `Data/Recipes/` 5종 — `station=MedicalBench`, **`unlockedByDefault` 체크**, `unlockRecipeItemId` 비움. 상세 [`crafting.md`](crafting.md).

| 결과물 | 재료 (예시) | 비고 |
|---|---|---|
| `bandage` (붕대) | `cloth_rag` ×2 | 기본 치료, 출혈 지혈 |
| `splint` (부목) | `wood_plank` ×1 + `cloth_rag` ×1 | 골절 응급 처치 |
| `gauze_roll` (거즈) | `cloth_rag` ×1 | 출혈 보조 |
| `painkiller` (진통제) | `chemical_flask` ×1 | 통증 완화 |
| `disinfectant` (소독약) | `chemical_flask` ×1 + `water_bottle` ×1 | 감염 방지 |

- NPC 전용 (제작 불가): `first_aid_kit`, `suture_kit`, `field_surgery_kit`, `morphine_ampule` 등

---

## 11-C. 파밍 오브젝트 ↔ 아이템 매핑

기획서 v2 §12.1 기준 **1차 스폰 테이블** 초안.

| 오브젝트 | 흔함 (잡템·D귀중) | 보통 (치료·재료·C귀중) | 희귀 |
|---|---|---|---|
| 서랍 | `junk_paper_stack`, `coin_old`, `junk_plastic_bottle` | `scrap_metal`, `gauze_roll`, `val_ring_copper` | `key_rusty`, `chip_credit` |
| 캐비닛 | `junk_sock_pair`, `junk_magazine`, `coupon_faded` | `bandage`, `cloth_rag`, `disinfectant` | `val_earring_pair` |
| 편의점 진열대 | `junk_can_empty`, `junk_battery_dead`, `canned_food` | `battery_aa`, `painkiller`, `energy_bar`, `ingredient_salt`, `ingredient_flour`, `ingredient_sugar` | `val_watch_wrist`, `recipe_bread` |
| 주방/식당 | `junk_mug_chipped`, `junk_pan_rust`, `junk_food_moldy` | `junk_kettle`, `water_bottle`, `ingredient_meat`, `ingredient_vegetable`, `ingredient_salt`, `recipe_stew` | `val_whiskey_bottle`, `recipe_special` |
| 시체 | `junk_cigarette_pack`, `coin_scrap`, `val_dental_gold` | `val_ring_silver`, `knife`, `splint_makeshift` | `map_fragment`, `morphine_ampule` |
| 잠긴 상자 | `junk_nail_box`, `junk_tape_duct` | `tool_part`, `ruby_shard`, `hemostatic_powder` | `ruby_crystal`, `trauma_kit` |
| 금고 | `val_coin_collect` | `val_necklace_gold`, `val_watch_pocket` | `val_statue_mini`, `val_ruby_set` |
| 작업대 | `junk_wire_tangle`, `junk_glass_shard` | `scrap_metal`, `screw`, `cast_bandage` | `circuit_board`, `field_surgery_kit` |
| 약품함 | `med_craft_gauze`, `gauze_roll` | `bandage`, `painkiller`, `saline_bag` | `first_aid_kit`, `suture_kit` |
| 화장실/세탁 | `junk_toilet_paper`, `junk_soap_bar` | `junk_mirror_shard` | `antidote` |
| 사무실 | `junk_typewriter`, `junk_remote_tv` | `junk_printer`, `val_calculator` | `val_laptop_dead` |
| 버려진 가방 | 잡템 랜덤 60% | 치료·소비 30% | 귀중 10% |
| 이상현상 잔해 | `anomaly_residue` | `ruby_shard`, `val_artifact_shard` | `ruby_corrupted`, `val_black_pearl` |

---

## 12. 데모 구현 권장 순서

### Stage 3 (인벤 검증) — **18종**

1. 치료 3: `bandage`, `splint`, `painkiller`
2. 소비 3: `battery_aa`, `canned_food`, `water_bottle`
3. 재료 3: `scrap_metal`, `cloth_rag`, `screw`
4. 루디 1: `ruby_shard`
5. 무기 2: `knife`, `pipe`
6. 기타 2: `key_rusty`, `coin_scrap`
7. 테스트 1: `first_aid_kit` (구급상자 연동 확인)

### Stage 4 (폐상가 파밍) — **+40종 권장**

- 치료 +10: `gauze_roll`, `first_aid_kit_small`, `hemostatic_powder`, `splint_makeshift`, `pain_patch`, `medkit_hp`, `saline_bag` 등
- 잡템 +15: `junk_*` P1 표에서 서랍·주방·편의점 위주
- 귀중 +10: `coin_old`, `val_ring_silver`, `val_watch_pocket`, `val_bracelet`, `val_camera_film` 등
- 무기·재료·열쇠: 기존 Stage 4 목록 유지

### 이후

- 안전가옥 제작 재료·발전기·의료대
- 이상현상·스토리 Legendary
- `anomaly_blade`, `watch_brother`
