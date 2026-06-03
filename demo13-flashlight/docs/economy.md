# Economy (화폐/경제)

## 핵심 자원: 루디(Rudi)

루디는 **짙은 현상 지역에서만 얻는 특수 자원이자 게임의 기본 화폐**.
전기가 사라진 세계에서 장치·조명을 움직이는 공급 재료이며 모든 세력이 원함.
플레이어가 밤 지역에 들어가야 하는 핵심 동기(자원 회수 루프).
세계관 상세는 `gdd-core.md` 5.3 참조.

## 화폐 시스템 구조 (2026-05-30 구현)

모든 루디 흐름은 `CurrencyManager` 싱글톤 하나를 거친다.

| 요소 | 위치 | 역할 |
|------|------|------|
| `CurrencyManager` | `Systems/CurrencyManager.cs` | 잔액 중앙 관리. `Add/Spend/Lose/CanAfford`, `OnBalanceChanged(newBalance, delta)` 이벤트 |
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
| 2026-05-30 | 화폐 시스템이 없어 퀘스트/업적/레이드 이벤트 보상이 전부 Debug.Log만 찍고 실제로 안 들어감 | **`CurrencyManager` 싱글톤 신설**로 루디 잔액 중앙 관리. 3개 보상 지점 + 세이브 + HUD(우상단 ◈ 카운터) 연동 | 보상 루프가 끊겨 있던 가장 시급한 미완 시스템. 중앙 매니저 하나로 흐름 일원화, 이후 상점/거래도 `Spend`로 확장 |
| 2026-06-02 | 루디 경제가 보상만 있고 쓸 곳(거래)이 없음 | **상점(전당포) 거래 시스템 신설.** `ShopData`(SO): 판매 목록(`stock`) + 가격 배율(`buyRate`/`sellRate`, 전당포는 sellRate 0.6으로 후려침). `ShopUI`(코드 생성 Canvas, UIManager 자식): 좌=팔기(내 가방, sellPrice>0)/우=사기(상점 stock), 상단 루디 잔액. 팔기=`CurrencyManager.Add`+`Grid.Remove`, 사기=`Spend` 성공 시 인벤 추가(공간 부족 시 환불). **진입: NPC 대화 선택지** — `DialogueChoice.openShop` + `NPCData.shopData` → `DialogueUI.TryOpenShop` → `UIManager.ShowShop`. | 루팅한 잡템·귀중품을 팔아 루디를 얻고, 보급품을 사는 핵심 경제 순환. 대화에서 자연스럽게 거래 진입. |
