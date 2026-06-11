# 아이콘 ↔ 데이터 매칭 정리 (2026-06-10)

> 그동안 생성한 아이콘 시트 ↔ itemId 매핑 + **데이터에 없는 신규 아트(등록 필요)** + **아트 없는 데이터(보류)** 정리.
> 슬라이스 시 이 문서 보고 itemId 파일명 할당. 신규는 items.csv 등록 대상.

---

## A. 기존 SO(204) 아이콘 = 시트로 커버됨 → 슬라이스+할당만
footprint 묶음 시트로 전부 생성. (2×2 / 1×2 / 2×1+대형 / 1×1 귀중·잡템·소비·재료·스토리)
→ checklist.csv "아이콘=N" 은 **슬라이스·파일할당 전**이라 그런 것. 아트 자체는 있음.

## B. 데이터에 없는 신규 아트 → **items.csv 등록 필요**

### 총기 (Weapon, 미래 확장)
| itemId | 격자 | itemId | 격자 |
|---|:---:|---|:---:|
| gun_pistol | 2×1 | gun_shotgun | 4×2 |
| gun_revolver | 2×1 | gun_rifle_hunt | 5×2 |
| gun_smg | 3×2 | gun_rifle_assault | 5×2 |
| gun_pipe | 3×1 | | |

### 탄약 (Misc/거래, 미래)
ammo_pistol·ammo_rifle·ammo_shotgun (1×1) / ammobox_pistol·ammobox_rifle·ammobox_shotgun (1×1)

### 총기 파츠 (Misc, 미래)
part_barrel(2×1)·part_magazine(1×2)·part_stock(2×1)·part_grip(1×1)·part_scope(2×1)·part_receiver(2×1)·part_suppressor(2×1)·part_trigger(1×1)
part_barrel_short(1×1)·part_mag_drum(2×2)·part_mag_ext(1×2)·part_tube_ext(1×2)·part_reddot(1×1)·part_holo(1×1)·part_scope_hunt(2×1)·part_ironsight(1×1)·part_foregrip(1×1)·part_bipod(2×1)·part_muzzle_brake(1×1)·part_flashhider(1×1)·part_stock_fold(2×1)·part_handguard(2×1)·part_laser(1×1)·part_cleaning_kit(1×2)

### 근접 무기 추가 (Weapon)
machete(3×1)·crowbar(3×1)·hatchet(2×2)·cleaver(2×1)·wrench_big(2×1)·fire_axe(2×3)

### 신규 잡템 (Misc/Junk) — 3시트
junk_radio_broken·junk_flowerpot·junk_pill_bottle·junk_glasses·junk_keyboard·junk_mouse·junk_headphones·junk_spray_can·junk_toy_car·junk_padlock·junk_bowl·junk_paint_can·junk_fan_blade·junk_slipper·junk_phone_cracked·junk_earbuds
junk_whisk·junk_rice_bowl·junk_chopsticks·junk_grater·junk_toothpaste·junk_hand_mirror·junk_shampoo·junk_loofah·junk_stapler·junk_rubber_bands·junk_glue_stick·junk_hole_punch·junk_rubber_ball·junk_dice·junk_yoyo·junk_crayons
junk_rotten_apple·junk_snack_bag·junk_noodle_block·junk_milk_carton·junk_bread_heel·junk_charger·junk_usb·junk_cable·junk_earphone_case·junk_smartwatch·junk_torn_shirt·junk_belt·junk_backpack_strap·junk_torn_glove·junk_scarf·junk_jeans_scrap
(전부 1×1)

### 신규 귀중품 (Valuable)
val_pearl_necklace·val_ruby_ring·val_sapphire_brooch·val_watch_chain·val_jade_bracelet·val_silver_locket·val_figurine_porcelain·val_ivory_seal·val_brass_compass·val_fountain_pen·val_pocket_mirror·val_lighter_vintage·val_film_canister·val_gold_glasses·val_antique_coin·val_gold_figurine (1×1)
**고가 2×2**: val_gold_bullion·val_jewelry_box·val_silver_teaset·val_vase_porcelain·val_bronze_statue·val_oil_painting·val_luxury_watch·val_cash_bundle·val_jade_carving·val_crystal_decanter·val_ceremonial_dagger·val_gold_trophy

### 루디 변형 (Valuable, 발광)
rudi_chunk(1×1)·rudi_geode(2×2)·rudi_ingot(2×1)·rudi_vial(1×2)·rudi_sliver(1×1)·rudi_fused(1×1)·rudi_impure(1×1)·rudi_pure(1×1)·rudi_cluster(2×2)·rudi_anomalous(1×1)·rudi_anomalous_core(2×2)
> 색 등급: 붉음=표준 / 창백=불순 / 진홍=고순도 / 검붉음=오염 / 보라=이상변이

### 개별 누락 보충 (방금 그림)
adrenaline_shot(1×1)·phenom_meter(1×1)·ammo_rounds(1×1)·junk_paper_stack(1×1)·val_chip_credit(1×1)·val_black_pearl(1×1)·junk_toy_bear(1×2)·repair_kit(2×2)·val_ruby_set(2×2)·wood_club(2×1)·axe(3×2)

## C. 아트 없는 데이터 → 보류
- **지역 reg_* 14종** (2~6지역 루팅템) — 현재 불필요, 2지역 작업 시 제작
- 매핑: 데이터 `val_painting_small`/`val_statue_mini` ↔ 신규아트 oil_painting/bronze_statue (둘 중 택1 통합 권장)

## D. 데이터에서 제거 권장
- `long_sword`·`spear` — 현실 무기 로스터 재정비로 폐기 (아이콘 미제작)

---

## 다음 액션
1. B 신규 아트들 → items.csv 등록 (footprint/cat 위 표 기준, stat은 기본값)
2. 시트 슬라이스 → itemId 파일명 할당 → checklist "아이콘=Y"
3. C·D 정리 (regional 보류, long_sword/spear 제거)
