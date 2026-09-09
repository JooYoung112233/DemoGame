# 의료/회복 시스템

> **2026-09-09 볼륨 축소로 부위별 의료(타르코프식)는 전면 폐기됐다.**
> 현재는 **단일 HP + 회복 아이템**(RPG식)만 있다. 자른 근거·범위는 [`scope-cut.md`](scope-cut.md).

## 현 상태 = 진실

### 모델
- 플레이어 체력은 **단일 HP 하나**뿐이다 (`Health`). 부위별 HP·부상 상태는 없다.
- 부상 종류(출혈/골절/통증), 부위별 디버프, 치료 타이머, 치료 중 이동 불가 — **전부 없다**.
- 회복은 **아이템을 쓰면 HP가 즉시 오른다**. 그게 전부다.

### 회복 아이템 (`ItemUseEffect.HealHP`)
`Assets/Resources/Items/Medical/` — 13종. 이름·플레이버는 남기고 효과만 HP 회복으로 통일했다.

| 아이템 | HP 회복 | 아이템 | HP 회복 |
|---|---:|---|---:|
| 붕대 (bandage) | 15 | 지혈제 (hemostatic_powder) | 30 |
| 임시부목 (splint_makeshift) | 15 | 지혈대 (tourniquet) | 30 |
| 거즈롤 (gauze_roll) | 18 | 모르핀 (morphine_ampule) | 35 |
| 소독약 (disinfectant) | 20 | 구급상자 (first_aid_kit) | 40 (내구 300/50 = 6회) |
| 진통제 (painkiller) | 20 | IFAK (ifak_injury) | 50 |
| 압박붕대 (bandage_compress) | 25 | | |
| 해독제 (antidote) | 25 | | |
| 부목 (splint) | 25 | | |

- **스택형**: 대부분 — 사용 시 `stackCount` 감소 (`hasDurability=false`)
- **내구도형**: 구급상자 등 — 사용 시 `durability` 감소, 0이면 파괴 (`maxStack=1`). 내구도 자체는 유지 결정.
- 사망/새 게임 리셋 시 `Health.FullHeal()`만 부른다. 안전가옥 침대 휴식도 HP 회복으로 처리.

### 남아 있는 "부위" 개념
치료가 아니라 **때리는 쪽에만** 남았다 — 폐기 대상이 아니었다.
- `BodyZones` — 조준 지점 → 부위 판정 + **데미지 배율**(머리 2.0 / 몸통 1.0 / 팔 0.8 / 다리 0.75)
- `UnitInjuries` — **적** 부위 부상 디버프(다리=추격 불가, 팔=공속 저하 등). 한 판 안에서만 유효.
- `BodyPartType` enum은 `Combat/BodyZones.cs`로 이관됐다 (원래 `Medical/BodyPart.cs`).

### 삭제된 것 (되살리려면 git 이력)
`Assets/Scripts/Medical/` 전체 — `PlayerMedicalSystem`(395줄) · `BodyPart`(120) · `MedicalHUD`(236) ·
`InjuryVFX`(264) · `MedicalItemData`(45). SO 13종(`Resources/Data/Medical/`).
UI 쪽 부상 표시(GameHUD 부상 아이콘 8개, CharacterPanelUI 부위 상태 5줄), DebugTestUI 의료 패널.
`ItemUseEffect.HealInjury`는 **enum 인덱스 보존용 예약 슬롯**으로만 남았다(삭제하면 기존 SO 인덱스가 밀린다).

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-25 | 의료 시스템 초안 확정. 5부위, 3부상, 키트+전용 혼합 치료. 좀보이드/타르코프 참고. |
| 2026-05-25 | 인벤 내구도 연동 정리. 소형=스택, 키트=내구도. items.md §1-0 참조. |
| 2026-05-25 | 인벤 ItemData 의료 축소: 붕대·부목·진통제·구급상자(HP)·부상IFAK만 SO 유지. |
| **2026-09-09** | **부위별 의료 전면 폐기 → 단일 HP + 회복 아이템(RPG식).** 질문: "볼륨이 큰데 뭘 자를까 — 부위별 치료 같은 타르코프 시스템?" / 사용자 결정: **삭제**. 의료 아이템 13종은 HP 회복량으로 전환(15~50). 부위 피격 배율·적 부위 부상은 전투 감각이라 존치. |
