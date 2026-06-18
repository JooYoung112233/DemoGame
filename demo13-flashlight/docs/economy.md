# Economy (화폐/경제)

## 이원 경제 — 스크랩 + 루디 (둘 다 존재)

| | **스크랩** | **루디** |
|---|-----------|----------|
| **정체** | 일상 **화폐**(동전) | 짙은 현상 **특수 자원**(`ruby_shard` 아이템) |
| **획득** | 잡템 판매, 민간 의뢰·퀘스트 보상, 루디 납품 보수 | 짙은 현상 활성 구간에서만 |
| **용도** | 생필품·소액 거래(떠돌이·전당포 잡템) | 탐지등 연료, 전당포 **납품**, 시설·고가 거래 |
| **표시** | HUD 우상단 `◈ N` (`CurrencyManager`) | 인벤토리 아이템(격자·스택) |

- **스크랩**과 **루디**는 서로 다른 축이다. 스크랩으로 루디를 사지 않음 — 루디는 레이드에서 직접 회수.
- 루디 납품(전당포) → **스크랩** 보상 + 평판 (S-017). [→ story-script S-016~S-017]

## 스크랩 (Scrap) — 일상 화폐

병뚜껑·금속 조각 등 붕괴 후 합의된 **소액 교환재**.

| 용도 | 예 |
|------|-----|
| 상점 구매·판매 | 떠돌이 상인 생필품, 전당포 잡템 |
| 퀘스트·업적 보상 | MQ-001 보수(스크랩 동전), DQ·업적 |
| 레이드 이벤트 | PostRaidEvent 보상/페널티 |

- 구현: `CurrencyManager` (`balance` = 스크랩)

## 루디 (Rudi) — 특수 자원

`ruby_shard`(루디 조각) = **짙은 현상에서만 나오는, 충전·방전·마모가 있는 활성 자원**(단순 발광체 아님).
탐지등 연료, 전당포 납품 대상. 모든 세력이 원하는 핵심 동기. [→ gdd-core §5.2/§5.3](gdd-core.md), [items.md](items.md)

- 인벤토리 **아이템**으로 보유·운반 (화폐 잔액과 별도)
- 구현: `ruby_shard` / `rudi_shard` itemId, `RaidManager`·`StoryTriggerManager` 루디 소지 체크

### 거래가치 — 충전 상태·순도 반영 〔확정 방향 · 수치 TBD〕

루디는 단순 화폐가 아니라 상태값을 가진 자원이라, 거래/가공 가치가 **충전 상태**와 **순도**에 따라 달라진다. [→ gdd-core §5.3](gdd-core.md)

- **충전 상태**: 빛/전력으로 쓰면 방전 → 충전량이 가치에 반영. 충전 0이면 빛이 꺼진 **「죽은 루디」**(둔탁한 돌) = 화폐가치 급락, **거래/가공 재료**로만 취급(전당포 헐값·작업대 분해/합성 재료).
- **순도**: 현상이 짙고 오래 머문 자리일수록 크고 순도 높은 결정 → 순도↑ = 용량↑ = 고가. 순도 높은 루디엔 기억 잔향(스토리 단서) → 특수 거래·이벤트 가치.
- **마모 → 지속 수요(루디 싱크)**: 루디는 반복 충전 시 최대 용량이 줄다 결국 영구 사망한다 → 보유 루디가 시간이 지나며 소모되는 **싱크**. 따라서 **신선한 루디 회수가 계속 필요** → 레이드·납품 동기가 마르지 않음(경제 순환 유지). 마모율 = `GameTuning` TBD.
- 수치(충전·순도별 가격 배율, 죽은 루디 헐값률)는 미정 — `GameTuning`/ShopData TBD. **확인 필요**: 충전/순도를 ItemInstance 상태값으로 둘지 별도 itemId(`ruby_dead` 등)로 둘지 = 구현 판단 대기.

## 상점 & 떠돌이 상인 (2026-06-05)

- **떠돌이 상인**: 지역 상점(건물) 해금 전까지의 **임시 보급책**. 안전가옥 인근에 출현. 생필품을 **스크랩**으로 판매. (npcId: `wandering_merchant`)
- **가격(ShopData)** — 떠돌이는 "바가지 행상":

  | 배율 | 값(가안) | 의미 |
  |------|:---:|------|
  | buyRate (내가 살 때) | **1.4** | 정가 +40% 할증 |
  | sellRate (내가 팔 때) | **0.45** | 후려치기(전당포 0.6보다 짬) |

  - 예: 통조림 정가 3 → 떠돌이 구매가 ≈ 4 스크랩 / 물병 2 → ≈ 3 스크랩.
- **평판 게이트**: 평판(이름값)↑ → **지역 상점(건물) 해금** → 정가 거래(buyRate≈1.0, sellRate 0.6) → 떠돌이보다 유리. [→ quest.md 평판, safehouse.md 시설 슬롯]

## 전당포(Shop_pawnshop) 진열 — 대폭 확장 (2026-06-18)

`Shop_pawnshop.asset`의 `stock`을 4종 → **66종**으로 확장. "무기/장비가 비어보이지 않게" 거의 모든 거래 가능 아이템을 진열.

- **진열 조건**: ShopUI 구매 탭은 `shop.stock`의 각 아이템 중 `BuyPrice(item) > 0`(= `item.buyPrice > 0`)인 것만 표시. 따라서 stock에 넣을 때 해당 ItemData의 `buyPrice`가 0보다 커야 진열됨.
- **평판 게이트**: 희귀도→평판등급 매핑(`RarityToRepTier`)으로 Common/Uncommon=F(항상 보임), Rare=D, Epic=B, Legendary=A 미만이면 **잠금 셀**로 표시(stock엔 그대로 둠).
- **buyPrice 신규 설정**: 기존 무기·장비는 `buyPrice=0`(상점 미판매)이라 진열 불가였음 → 거래 가능하도록 `buyPrice ≈ sellPrice × 2.5`(전당포 house 비율 ~2.4)로 설정. `useEffect`/능력치는 미변경.

| 분류 | 수 | buyPrice 신규 설정 항목 |
|------|---:|------|
| 무기(근접 10) | 10 | bat/knife/pipe/pipe_worn/wood_club=3000, axe/hammer/spear=7500, long_sword=12000, anomaly_blade=14000 |
| 방어구·장비(6) | 6 | helmet_bucket/boots_rubber/gloves_work/coat_light=4500, vest_scrap=10000, armor_night=16000 |
| 의료(13) | 13 | (기존 buyPrice 보유, 변경 없음) |
| 소비-음식/음료(13) | 13 | special_meal=8000(기존 0) — 나머지 변경 없음 |
| 소비-유틸(6) | 6 | (battery_aa·repair_kit·stim_injector·adrenaline_shot·smoke_bomb·phenom_meter, 기존값) |
| 소비-식재료(6) | 6 | (기존값) |
| 재료(12) | 12 | (기존값) |

- 미진열 의도 제외: **Valuable(귀중품)/Key(열쇠)/Junk·Story·Regional(잡템·스토리·지역품)** 은 buyPrice=0(판매 전용·서사용)이라 진열 대상 아님 — 그대로 둠.

## 화폐 시스템 구조 (2026-05-30 구현)

모든 **스크랩** 흐름은 `CurrencyManager` 싱글톤 하나를 거친다.

| 요소 | 위치 | 역할 |
|------|------|------|
| `CurrencyManager` | `Systems/CurrencyManager.cs` | 스크랩 잔액 중앙 관리. `Add/Spend/Lose/CanAfford`, `OnBalanceChanged(newBalance, delta)` 이벤트 |
| 자동 생성 | `Data/GameBootstrap.cs` | 다른 싱글톤과 함께 `EnsureSingleton<CurrencyManager>` |
| 세이브 | `Systems/SaveManager.cs` | `GameSaveData.currency`(int)로 잔액 영속화 |
| HUD 표시 | `UI/GameHUD.cs` | 우상단 `◈ N` 카운터. 변동 시 흰색 반짝임. 이벤트 구독 |

### 보상/지출 연동 지점

| 흐름 | 위치 | API |
|------|------|-----|
| 퀘스트 보상 | `Quest/QuestManager.cs` `GiveRewards` | `Add(amount, "퀘스트: {title}")` |
| 업적 보상 | `Systems/AchievementManager.cs` `GiveReward` | `Add(currencyReward, "업적: {name}")` |
| 레이드 후 이벤트 보상 | `UI/PostRaidEventUI.cs` `ApplyRewards` | `Add(amount, ...)` |
| 레이드 후 이벤트 페널티 | `UI/PostRaidEventUI.cs` `ApplyPenalties` | `Lose(amount, ...)` — 0 밑으로 안 내려감 |

### API 규칙
- `Add`: 보상. 음수/0 무시
- `Spend`: 거래. 잔액 부족 시 `false` 반환하고 차감 안 함 (상점 등)
- `Lose`: 페널티. 보유분까지만 깎고 음수 방지
- 잔액 변동마다 `OnBalanceChanged` 발행 → HUD/토스트가 구독

## 변경 로그

| 날짜 | 질문 | 결정 | 근거 |
|------|------|------|------|
| 2026-05-30 | 화폐 시스템이 없어 퀘스트/업적/레이드 이벤트 보상이 전부 Debug.Log만 찍고 실제로 안 들어감 | **`CurrencyManager` 싱글톤 신설**로 스크랩 잔액 중앙 관리. 3개 보상 지점 + 세이브 + HUD(우상단 ◈ 카운터) 연동 | 보상 루프가 끊겨 있던 가장 시급한 미완 시스템. 중앙 매니저 하나로 흐름 일원화, 이후 상점/거래도 `Spend`로 확장 |
| 2026-06-02 | 스크랩 경제가 보상만 있고 쓸 곳(거래)이 없음 | **상점(전당포) 거래 시스템 신설.** `ShopUI`: 상단 스크랩 잔액. 팔기=`CurrencyManager.Add`+`Grid.Remove`, 사기=`Spend`. **진입: NPC 대화 선택지** — `DialogueChoice.openShop` → `UIManager.ShowShop`. | 루팅한 잡템을 팔아 스크랩을 얻고, 보급품을 사는 핵심 경제 순환. |
| 2026-06-05 | 떠돌이 상인·생필품 거래 필요 | **떠돌이 상인** 신설(buyRate 1.4/sellRate 0.45). 결제 = **스크랩**. 평판↑ 시 지역 상점 해금 → 정가 거래. | 수면→보급 루프. survival.md 수면 트레이드오프와 직결 |
| 2026-06-16 | 루디 = 충전·방전·마모 활성 자원(gdd-core §5.3 캐논)을 경제에 반영 | 루디 거래가치 = **충전 상태·순도** 반영, 충전 0 = **죽은 루디**(거래/가공 재료), **마모 → 루디 싱크 → 지속 회수 수요**(경제 순환 유지) 항목 추가. SSOT 링크(§5.2/§5.3). 수치는 GameTuning TBD. | [→ gdd-core §5.3](gdd-core.md). 충전/순도 상태 구현 방식(ItemInstance vs 별도 itemId)은 확인 필요. |
| 2026-06-08 | 루디와 스크랩 명칭 혼동 | **이원 경제 명확화.** **스크랩** = 일상 화폐(`CurrencyManager`, UI·상점 표기). **루디** = `ruby_shard` 특수 자원(인벤·연료·납품) — **둘 다 존재**, 역할만 분리. | 구현상 화폐 잔액=스크랩만. 루디는 아이템. 납품 시 스크랩 보상. |
| 2026-06-17 | 회수꾼·관리인 NPC 상점 필요 | **Shop_scavenger**(buyRate 1.2/sellRate 0.4, 생필품+재료 8종) + **Shop_warden**(buyRate 0.9/sellRate 0.5, 프리미엄 6종) ShopData 신설. NPC shopData 연결 + 거래 대화 추가. 관리인은 minAffinity 30 게이트. | 떠돌이 외 NPC별 차별화 상점으로 경제 순환 확장. 회수꾼=생존물자 할증, 관리인=신뢰 기반 정가 보급. |
| 2026-06-17 | 떠돌이 상인 ShopData·NPCData 실 에셋 부재(기획만 존재) | **`Shop_merchant.asset`**(shopId=merchant, buyRate 1.4/sellRate 0.45, 생필품 7종: canned_food·water_bottle·energy_bar·soda_can·bandage·battery_aa·painkiller) + **`wandering_merchant.asset`**(NPCData, greet 1 + trade 이벤트 대화: "거래하기"→openShop / "됐어") 신설. NPC shopData→Shop_merchant GUID 연결 완료. | 2026-06-05 기획 떠돌이 상인을 실제 SO로 구현. 배율은 기존 기획값(1.4/0.45) 그대로. |
| 2026-06-18 | 전당포 판매 물량이 너무 적고(stock 4종) 무기·장비가 아무것도 없음 → "우리 있는 아이템 전부 추가" | **`Shop_pawnshop.asset` stock 4종 → 66종 확장.** 무기 10·방어구 6·의료 13·소비(음식/유틸/식재료) 25·재료 12. 진열 안 되던 무기·장비는 `buyPrice=0`→`sellPrice×2.5`로 설정(무기 3000~14000, 장비 4500~16000, special_meal 8000). 능력치·useEffect는 미변경. Valuable/Key/Junk/Story/Regional(buyPrice 0=판매전용)은 제외. **아이템 참조 검증**: 기존 stock 4종 + 신규 62종 전부 실존 ItemData에 연결됨(깨진 GUID 없음), 모두 buyPrice>0 확인. | "장비가 비어보임" 해소. 진열 조건은 buyPrice>0(ShopUI `price<=0 continue`), 희귀도는 평판 잠금셀로만 표시되므로 stock엔 광범위 진열 가능. |
| 2026-06-18 | 상점 위탁(consignment) 탭이 placeholder(준비 중) | **위탁 그레이박스 구현(ShopUI 위탁 탭).** 위탁가 = 직접 판매가(sellRate 기반) **×1.5(올림)**. 슬롯 **3개**(상점 공용, 런타임 전용·세이브 안 함). 가방 판매가능 아이템 클릭 → 빈 슬롯에 올림(그리드 1개 제거) → **30초**(unscaledTime) 후 정산완료 → 클릭 시 `CurrencyManager.Add(위탁가, "위탁 정산")`. | 위탁 = 직접 판매보다 높은 정산이지만 시간이 걸리는 트레이드오프. 30초·1.5배·슬롯3은 그레이박스 placeholder 수치(TBD). |
