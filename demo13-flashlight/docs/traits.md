# 캐릭터 특성(퍽) 시스템

> **현 상태 = 진실.** 진행형 퍽 트리 — 회수꾼이 살아남고 깊이 들어갈수록 특성을 "찍는다". 낙원(Last Paradise)·좀보이드식 빌드 다양화를, **이 게임의 세계관(현상·빛의역설·루디·시계)**에 맞춰 설계.
>
> 연동: 스탯 = [`StatDB`/PlayerStatData](inventory.md) · 평판/경제 = [economy.md](economy.md) · 의료 = [medical.md](medical.md) · 현상·루디·시계 = [gdd-core.md §5](gdd-core.md)·[anomaly.md](anomaly.md).
> **아래 수치는 1차 초안 — 밸런싱([balance-tuner])에서 `GameTuning`으로 확정.**

---

## 1. 모델 (확정 2026-06-17)

- **진행형 퍽 트리**: 캐릭터 생성 시 몰아주지 않는다. **레이드·평판으로 퍽 포인트(PP)를 벌어** 점차 해금.
- **긍정 퍽 + 부정 특성(낙인) 둘 다**:
  - **긍정 퍽** = PP 소모로 해금(카테고리 트리, 티어 선행조건).
  - **부정 특성(낙인)** = 영구 약점을 **자청**하면 **PP 환급** → 더 강한 빌드. (좀보이드 트레이드오프를 진행형에 이식)
  - **⚖ 트레이드오프 퍽** = 강한 효과 + 작은 대가 내장(선택의 무게).
- **리스펙**: 기본 불가(영구 빌드). 거점 고급 시설/희귀 아이템으로 **제한적 1회성 초기화**만 — 추후.

## 2. 포인트(PP) 경제 〔1차 초안〕

| 항목 | 1차값 | 비고 |
|---|---|---|
| 회수꾼 평판 레벨업 | **+1 PP** / 레벨 | 주 공급원 |
| 마일스톤 보너스 | **+1 PP** | 첫 짙은 현상 생환 · 각 지역 첫 입성 · 핵심 단서 마일스톤 등 일시 |
| 부정 특성 환급 | **+1~3 PP** | 강도별(약1·중2·강3) |
| 부정 특성 보유 상한 | **3개** | 남발 방지 |
| 티어 비용 | **T1=1 · T2=2 · T3=3 PP** | 상위 티어 = 하위 퍽/평판 선행 |

> 데모(1지역)에서 도달 가능 PP ≈ 4~6 → "전부는 못 찍고 방향을 고른다"가 목표.

## 3. 카테고리 트리  *(PP = 비용 / 부정은 +환급. 수치 1차)*

### 3.1 전투 (Combat) [→ combat.md]
| 퍽 | 효과(1차) | T | PP |
|---|---|:--:|:--:|
| 끈질긴 폐활량 | 스태미너 최대 +15% · 회복 +15% | 1 | 1 |
| 냉정한 손 | 강공·차지 그로기 누적 +20% | 1 | 1 |
| 무기 숙련 | 근접 내구 소모 −25% · 수리 효율 +20% | 1 | 1 |
| 받아넘기기 ⚖ | 구르기 무적 프레임 +30% / 스태 소모 +10% | 2 | 2 |
| 약점 간파 | 배후·그로기 처형 피해 +25% | 2 | 2 |
| 일격필살 | 차지 강공 첫 명중 피해 +40% (선행: 냉정한 손) | 3 | 3 |
| (부정) 유리 어깨 | 받는 피해 +15% | — | +2 |
| (부정) 욱하는 성미 | 피격 후 ~1초 조준 흔들림 | — | +1 |

### 3.2 생존·신체 (Survival) [→ medical.md, survival.md]
| 퍽 | 효과(1차) | T | PP |
|---|---|:--:|:--:|
| 강골 | 골절 확률 −30% · 출혈 지속 −25% | 1 | 1 |
| 통증 내성 | 통증 이동·조준 페널티 −40% | 1 | 1 |
| 잡식 | 상한 음식·물 페널티 무시 | 1 | 1 |
| 빠른 회복 | 거점 수면 HP·기력 회복 +30% | 2 | 2 |
| 불굴 | HP 20% 이하일 때 받는 피해 −15% (선행: 강골) | 3 | 3 |
| (부정) 허약한 위장 | 허기·탈수 +25% 빠름 | — | +2 |
| (부정) 악몽 | 수면 회복 −30% (서사: 흐릿한 기억) | — | +1 |

### 3.3 회수·파밍 (Scavenging) [→ inventory.md, economy.md]
| 퍽 | 효과(1차) | T | PP |
|---|---|:--:|:--:|
| 빠른 손 | 컨테이너 수색 속도 +30% | 1 | 1 |
| 노새 | 휴대 무게 한도 +20% | 1 | 1 |
| 감정가 | 판매가 +15% + 귀중품·루디 **순도 식별** | 2 | 2 |
| 매의 눈 | 희귀·루디 컨테이너 하이라이트 (선행: 빠른 손) | 2 | 2 |
| 빈손 방지 | 사망/탈출 실패 시 보존 슬롯 +1 | 3 | 3 |
| (부정) 덜렁이 | 사망 시 떨어뜨리는 양 +30% | — | +1 |

### 3.4 잠행·기동 (Stealth/Mobility) [→ rendering.md 가시성, navigation.md]
| 퍽 | 효과(1차) | T | PP |
|---|---|:--:|:--:|
| 그림자 | 적 감지 반경 −20% (웅크림 −35%) | 1 | 1 |
| 고양이 걸음 | 이동 소음 −30% | 1 | 1 |
| 길눈 | 미니맵 발견 +25% · 통로 막힘 −20% | 1 | 1 |
| 도주술 | 탈출 채널링 속도 +25% | 2 | 2 |
| 유령 | 웅크려 정지 3초 후 짧은 비탐지(재진입) (선행: 그림자) | 3 | 3 |
| (부정) 무거운 발 | 기본 이동 소음 +25% | — | +1 |

### 3.5 사회 (Social) [→ npc-dialogue.md, economy.md]
| 퍽 | 효과(1차) | T | PP |
|---|---|:--:|:--:|
| 입담 | NPC 호감·신뢰 획득 +20% | 1 | 1 |
| 단골 | 상점 구매가 −10% · 일일 의뢰 질↑ | 2 | 2 |
| 소문통 | 정보(단서·블랙마켓) 가격 −20% · 해금 빠름 | 2 | 2 |
| 협상가 | 밴딧 협상 성공률↑ · 요구치↓ | 2 | 2 |
| (부정) 수상한 인상 | 초기 두려움↑ · 첫 거래 불리 | — | +1 |

### 3.6 ★ 현상 (Anomaly) — 세계관 고유 트리 [→ gdd-core.md §5, anomaly.md]
> 이 게임만의 정체성. 짙은 현상·빛의 역설·루디·시계와 직접 묶인다. 깊게 파는 자의 트리.

| 퍽 | 효과(1차) | T | PP |
|---|---|:--:|:--:|
| 어둠 적응 | 짙은 현상 시야 페널티 −30% (콘 밖 형체 인식↑) | 1 | 1 |
| 빛 절제 | 비루디 광원 **침식 게이지 누적 −40%**(빛의 역설 완화) | 2 | 2 |
| 루디 감각 | 주변 **루디·현상 노드 핑** 반경↑ + 순도 직감 | 2 | 2 |
| 시계 동조 | "믿을 수 없는 시계" 왜곡 표시 정밀↑ — **진짜 남은 시간 오차↓** | 2 | 2 |
| 현상 친화 ⚖ | 현상권 루디·희귀 +30% / **침식·몬스터 어그로 +25%** | 3 | 3 |
| 되감기 친화 ⚖ | 죽음→시계 회수 시 가방·시간 손실↓ / **기억 단서가 더 흐려진다**(서사: 새벽 기원, §2.4) | 3 | 3 |
| 심연 보행 | **영구 짙은 구역**의 이동·시야 페널티 무시 (선행: 어둠 적응) | 3 | 3 |
| (부정) 현상 과민 | 현상권 스태미너·시야 페널티↑ | — | +2 |
| (부정) 빛 의존 | 탐지등 방전 시 공황 — 정확도·이동↓ | — | +1 |

> 현상 트리는 **서사와 직결**: '되감기 친화'·'빛 의존'이 주인공의 기원(죽음→부활, 빛의 역설, §2.4)을 *플레이로* 만지게 한다. 깊게 파는 빌드일수록 진실에 가까워지는 결.

## 4. effectKey 어휘 · op 규약 (구현 2026-06-19)

`TraitData.effects`(`List<TraitEffect>{effectKey, op, value}`)의 데이터 어휘. CSV `effects` 컬럼 → 생성기 → `.asset` 파이프라인으로 채운다. **런타임 합성은 `TraitManager`가 담당.**

### 4.1 op 규약
| op | 의미 | value 해석 | 합성(TraitManager) | 조회 API |
|---|---|---|---|---|
| `mul` | 비율 | `0.15`=+15%, `-0.30`=−30% | ∏(1+value) → **최종 배수** | `GetModifier(key)` (없으면 1.0) |
| `add` | 가산(절대량) | 단위 그대로 (예: 슬롯 +1) | Σvalue | `GetAdditive(key)` (없으면 0) |
| `flag` | 능력 on/off | `1`=켜짐 | OR (하나라도 켜지면 on) | `HasFlag(key)` |

> 부정 특성은 value에 **페널티 부호**로 표기(예: 받는 피해 +15% = `damage_taken:mul:0.15`, 수면 회복 −30% = `sleep_recovery:mul:-0.30`).
> 호출부 사용 규약: 수치형은 `final = base * GetModifier(key)`, 능력형은 `if (HasFlag(key)) …`.

### 4.2 effectKey 사전 (41개 퍽 1차 매핑)
| 카테고리 | effectKey | op | 1차 value | 출처 퍽 |
|---|---|---|---|---|
| 전투 | `stamina_max` / `stamina_regen` | mul | +0.15 | 끈질긴 폐활량 |
| 전투 | `groggy_buildup` | mul | +0.20 | 냉정한 손 |
| 전투 | `weapon_durability_cost` / `repair_efficiency` | mul | −0.25 / +0.20 | 무기 숙련 |
| 전투 | `dodge_iframe` / `dodge_stamina_cost` | mul | +0.30 / +0.10 | 받아넘기기 ⚖ |
| 전투 | `execute_damage` | mul | +0.25 | 약점 간파 |
| 전투 | `charge_first_hit_damage` | mul | +0.40 | 일격필살 |
| 전투(부정) | `damage_taken` | mul | +0.15 | 유리 어깨 |
| 전투(부정) | `aim_shake_on_hit` | flag | 1 | 욱하는 성미 |
| 생존 | `fracture_chance` / `bleed_duration` | mul | −0.30 / −0.25 | 강골 |
| 생존 | `pain_penalty` | mul | −0.40 | 통증 내성 |
| 생존 | `spoiled_food_immune` | flag | 1 | 잡식 |
| 생존 | `sleep_recovery` | mul | +0.30 | 빠른 회복 |
| 생존 | `low_hp_damage_taken` | mul | −0.15 | 불굴 |
| 생존(부정) | `hunger_thirst_rate` | mul | +0.25 | 허약한 위장 |
| 생존(부정) | `sleep_recovery` | mul | −0.30 | 악몽 |
| 회수 | `search_speed` | mul | +0.30 | 빠른 손 |
| 회수 | `weight_max` | mul | +0.20 | 노새 |
| 회수 | `sell_price` / `purity_id` | mul / flag | +0.15 / 1 | 감정가 |
| 회수 | `rare_container_highlight` | flag | 1 | 매의 눈 |
| 회수 | `keep_slot` | add | +1 | 빈손 방지 |
| 회수(부정) | `death_drop_amount` | mul | +0.30 | 덜렁이 |
| 잠행 | `detect_radius` | mul | −0.20 | 그림자 |
| 잠행 | `move_noise` | mul | −0.30 | 고양이 걸음 |
| 잠행 | `minimap_reveal` / `path_block` | mul | +0.25 / −0.20 | 길눈 |
| 잠행 | `extract_channel_speed` | mul | +0.25 | 도주술 |
| 잠행 | `crouch_vanish` | flag | 1 | 유령 |
| 잠행(부정) | `move_noise` | mul | +0.25 | 무거운 발 |
| 사회 | `npc_affinity` | mul | +0.20 | 입담 |
| 사회 | `buy_price` / `daily_quest_quality` | mul / flag | −0.10 / 1 | 단골 |
| 사회 | `info_price` / `info_unlock_speed` | mul / flag | −0.20 / 1 | 소문통 |
| 사회 | `bandit_negotiate` | flag | 1 | 협상가 |
| 사회(부정) | `initial_fear` / `first_deal_penalty` | mul / flag | +0.20 / 1 | 수상한 인상 |
| 현상 | `vision_anomaly` | mul | −0.30 | 어둠 적응 |
| 현상 | `erosion_buildup` | mul | −0.40 | 빛 절제 |
| 현상 | `rudi_ping` / `purity_id` | flag | 1 / 1 | 루디 감각 |
| 현상 | `timer_accuracy` | mul | +0.30 | 시계 동조 |
| 현상 | `anomaly_loot` / `erosion_buildup` / `monster_aggro` | mul | +0.30 / +0.25 / +0.25 | 현상 친화 ⚖ |
| 현상 | `rewind_loss` / `memory_clue_clarity` | mul | −0.30 / −0.25 | 되감기 친화 ⚖ |
| 현상 | `deep_zone_immune` | flag | 1 | 심연 보행 |
| 현상(부정) | `anomaly_stamina_penalty` / `vision_anomaly` | mul | +0.25 / +0.25 | 현상 과민 |
| 현상(부정) | `light_panic` | flag | 1 | 빛 의존 |

> ⚠ 위 수치는 **1차 초안**. 정밀값은 [balance-tuner]→`GameTuning` 확정. 동일 key가 여러 퍽에 걸리면(예: `move_noise`, `sleep_recovery`, `purity_id`, `vision_anomaly`, `erosion_buildup`) `TraitManager`가 규약대로 자동 합성.

## 4b. 구현 메모
- 데이터: `TraitData`(ScriptableObject) — id·이름·설명·카테고리·티어·선행조건·PP비용(또는 환급)·효과(`effects` = §4 어휘). `StatDB`/PlayerStatData 적용은 말단 배선 TODO.
- **(2026-06-19) TraitData SO·생성기 구현됨**: 클래스 `Assets/Scripts/Data/TraitData.cs`(enum `TraitCategory`/`TraitTier`, `effectSummary` + `List<TraitEffect>`), CSV `tools/traits.csv`(§3 전 41개 퍽 + `effects` 컬럼), 생성기 `tools/GenerateTraits.ps1` → `Assets/Resources/Data/Traits/*.asset` 41개.
- **(2026-06-19) TraitManager 런타임 코어 구현됨**: `Assets/Scripts/Data/TraitManager.cs`(싱글톤+DontDestroyOnLoad, NPCRelationshipManager 패턴). 로드/해금상태/PP/부정상한 추적 + 쿼리 API(`GetModifier`/`GetAdditive`/`HasFlag`, 정적 `Mod`/`AddVal`/`Flag`) + 세이브 구조체(`TraitSaveData`). 세이브 배선 완료(`GameSaveData.traits`).
- **(2026-06-29) 말단 배선 1차 = 핵심 캐릭터 스탯 8개 키**: 스태미너(max/regen)·회피(iframe/cost)·무게·허기수분·받는피해(저체력) — `TopDownPlayer`/`PlayerInventory`/`SurvivalStats`/`Health`. **나머지 키(전투 finesse·경제/회수·잠행·현상계)는 미배선** — 결정 로그 2026-06-29 참조. 현상계는 대상 시스템 다수 미구현.
- UI: 캐릭터 패널("01 캐릭터 상태", [→ inventory.md])에 **특성 탭**(트리 뷰 + PP 잔량). 양피지 6패널 톤 통일.
- 효과 = 기존 스탯/시스템 훅 재사용(전투·의료·인벤·현상·시계). 새 수치는 [balance-tuner].
- **미정(TBD)**: PP 곡선 세부·각 퍽 정밀 수치·리스펙 방식·트리 시각·아이콘.

---

## 기획 결정 로그

### 2026-06-29 — 특성 효과 말단 배선 1차 (핵심 캐릭터 스탯)
- **무엇**: 해금만 되고 효과 미적용이던 특성을 **실제 스탯 read-site에 배선** 시작. `TraitManager`에 정적 null-safe 접근자 `Mod(key)`/`AddVal(key)`/`Flag(key)` 추가(매니저 없으면 1/0/false) → 호출부는 `final = base * TraitManager.Mod(key)` 한 줄.
- **배선된 8개 키(배치 1)**:
  - `TopDownPlayer` 게터: `stamina_max`·`stamina_regen`(MaxStam/StamRegen), `dodge_iframe`(DodgeInvDur), `dodge_stamina_cost`(DodgeCost).
  - `PlayerInventory.MaxWeight`: `weight_max`(노새). 과적 판정도 `MaxWeight` 기준으로 정합.
  - `SurvivalStats` 소모율: `hunger_thirst_rate`(허약한 위장 — 수분·포만 둘 다).
  - `Health.TakeDamage`: `damage_taken`(유리 어깨) + `low_hp_damage_taken`(불굴, 체력 ≤30%일 때). **플레이어 한정** — 적 공용 Health라 `TopDownPlayer` 캐시(`_tp`)로 게이팅. 저체력 임계 `0.3`은 placeholder(→ GameTuning 이관 가능).
- **남은 배치(TODO)**: ①전투 finesse(`execute_damage`·`charge_first_hit_damage`·`groggy_buildup`·`weapon_durability_cost`·`repair_efficiency`·`fracture_chance`·`bleed_duration`·`pain_penalty`·`spoiled_food_immune`·`sleep_recovery`) ②경제/회수(`sell_price`·`buy_price`·`info_price`·`search_speed`·`keep_slot`·`death_drop_amount`·`rare_container_highlight`·`npc_affinity`) ③잠행(`detect_radius`·`move_noise`·`crouch_vanish`·`minimap_reveal`·`extract_channel_speed`) ④현상계(`vision_anomaly`·`erosion_buildup`·`rudi_ping`·`rewind_loss`·`memory_clue_clarity`·`deep_zone_immune`·`timer_accuracy` 등 — **대상 시스템 다수 미구현**, 시스템 생길 때 동반 배선).
- **별개 TODO**: PP 획득 루트(레이드/평판 → `GrantPP`) 연결(현재 디버그 +10PP만).
- **검증**: 정적 컴파일 감사 통과(브레이스·호출 정합). 효과 체감은 Unity 플레이 검증 필요.

### 2026-06-24 — 특성 탭 UI 구현 (`TraitPanelUI`)
- **결정**: 캐릭터 특성을 **독립 패널(K 토글)**로 우선 구현(캐릭터 패널 탭 통합은 추후 — 패널 비대화 방지). 카테고리×티어 목록 + PP 잔량 + **행 클릭 해금**(TraitManager.CanUnlock/Unlock 그대로 사용 — 선행·비용·부정상한 준수) + 디버그 +10PP. **세이브 훅**(`GameSaveData.traits`) 추가. unity-reviewer 통과.
- **미배선(다음)**: 특성 **효과 스탯 말단 read-site**(이속/스태미너/시야 등 `GetModifier`/`HasFlag` 적용 — 현재 해금만 되고 효과 미적용), PP 획득 루트(레이드/평판→GrantPP) 연결. UI 아트는 그레이박스(추후 양피지 톤).

### 2026-06-19 — 🔴 "SO 41개 로드=0" 블로커 **해결** (진단으로 진범 확정)
- **증상**: 자가검증 `[SO 41개 로드] expected=41 actual=0`. 여러 차례 .asset/임포터 수정 시도했으나 안 됨.
- **진단(결정타)**: 자가검증에 카운트 로그 추가 → `Resources TraitData=41 · RecipeData=15 · AssetDatabase=41`. **에셋·로드는 처음부터 정상**(Resources가 41 반환). 즉 손저작 .asset/임포터/YAML이 문제가 아니었음.
- **진범**: `TraitManager.Awake`의 **싱글톤 가드**. 이전 플레이/테스트가 남긴 `Instance`(DontDestroyOnLoad 잔존)가 있으면, 자가검증이 `AddComponent<TraitManager>`한 새 인스턴스가 `if (Instance != null) { Destroy; return; }`로 **LoadDefinitions를 건너뜀** → `AllTraits=0`. 한 번 막히면 그 인스턴스가 안 지워져 매 실행 실패. **런타임(게임)은 줄곧 정상**(싱글톤 1개 → 정상 로드).
- **수정**: 자가검증이 인스턴스 생성 전에 **잔존 `TraitManager.Instance`를 DestroyImmediate로 정리**. (불필요했던 에디터 AssetDatabase 폴백은 제거 — 로드는 정상이라.)
- **교훈**: 싱글톤 + Resources 로드 시스템의 자가검증은 잔존 인스턴스를 먼저 정리. "에셋 0개 로드"라고 로드만 의심하지 말 것.

### 2026-06-19 — effects 어휘·op 규약 + TraitManager 런타임 코어
- **무엇**: §3 41개 퍽의 `effectSummary`를 effectKey/op/value로 1차 매핑(§4 표) + 런타임 합성 코어 신설.
- **op 규약**: `mul`(비율, ∏(1+value)=최종배수) / `add`(절대 가산, Σ) / `flag`(능력 on, OR). 부정 특성은 value 부호로 페널티.
- **산출**: `tools/traits.csv`에 `effects` 컬럼 추가, `tools/GenerateTraits.ps1`가 파싱→`.asset effects` 시퀀스 출력(41개 재생성 완료, GUID 보존). 런타임 `Assets/Scripts/Data/TraitManager.cs`(+.meta) — 로드·해금·PP·부정상한3·쿼리 API(`GetModifier`/`GetAdditive`/`HasFlag`)·세이브 구조체.
- **경계**: StatDB/전투/인벤/SaveManager 등 말단 read-site·UI 트리는 **미배선(TODO)**. TraitManager는 쿼리 API만 제공. 수치는 1차 초안(→balance-tuner).

### 2026-06-19 — TraitData SO·생성기 구현
- **무엇**: §3 트리의 전 퍽(41개)을 실제 ScriptableObject `.asset`으로 데이터화.
- **산출**: `Assets/Scripts/Data/TraitData.cs`(+.meta) / `tools/traits.csv` / `tools/GenerateTraits.ps1` → `Assets/Resources/Data/Traits/*.asset` 41개.
- **근거**: 데이터(콘텐츠 오서링)와 런타임 분리. 효과는 `effectSummary` 문자열로 §3 문구 그대로 보존, 정밀 수치/StatDB 연동·UI 트리는 추후(balance-tuner·구현). 수치는 §2~§3 1차값 무변경.

### 2026-06-17 — 특성 1차 수치 + 트리 확장
- **결정**: §2 PP 경제 수치화(평판 +1/레벨·마일스톤 +1·부정 환급 1~3·상한 3·티어 1/2/3). §3 전 카테고리에 **PP·티어·1차 효과값** 부여 + 퍽 확장(일격필살·불굴·매의 눈·유령·협상가·심연 보행 추가 → 총 ~45개). 데모 도달 PP ≈ 4~6로 "방향 선택" 목표.
- **수치 성격**: 전부 **1차 초안** — 구현·플레이테스트 후 `GameTuning` 확정.

### 2026-06-17 — 캐릭터 특성(퍽) 시스템 신설
- **질문**: 세계관에 맞는 "특성 찍기" 리스트를 낙원식으로. 획득 방식? 부정 특성?
- **결정**: **진행형 퍽 트리**(평판·레이드로 PP → 해금) + **부정 특성(낙인) 환급** 포함. 캐릭터 생성 일괄 배분 아님.
- **카테고리 6**: 전투·생존·회수·잠행·사회 + **★현상(세계관 고유)**. 현상 트리(어둠적응·빛절제·루디감각·시계동조·되감기 친화 등)가 정체성 — 빛의역설/루디/시계/기원과 직접 연동.
- **반영**: traits.md 신설, MASTER 색인 등재. 수치·구현 TBD.
