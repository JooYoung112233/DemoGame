# 인벤토리 시스템 기획

## 개요
타르코프 스타일 격자 기반 인벤토리. 세로형 패널, 아이템 드래그앤드롭, 다양한 크기(1x1~2x3).

## 격자 시스템

### 컨테이너 (Inventory Grid)
- 모든 인벤토리는 **세로형 격자 컨테이너**
- 플레이어 가방, 창고, 루팅 상자, 바닥 드롭 모두 동일한 Grid 구조 사용
- 격자 크기는 컨테이너마다 다름

| 컨테이너 | 격자 크기 | 비고 |
|---|---|---|
| 플레이어 휴대 격자 | **장착 백팩이 결정** | 백팩 = 격자 제공 아이템 (아래) |
| (백팩 없음 = 포켓만) | 2x2~2x3 최소 | 가방 안 메면 소량만 |
| 안전가옥 창고 | 8x12 | 시설 강화로 확장 |
| 루팅 상자 (소) | 3x3 | 파밍 오브젝트 |
| 루팅 상자 (중) | 4x5 | |
| 루팅 상자 (대) | 5x6 | 레어 |
| 바닥 드롭 | 3x3 | 아이템을 땅에 버릴 때 생성 |

### 백팩 = 휴대 격자 제공 아이템 (확정 2026-06-10)

타르코프식 — **장착한 백팩이 레이드 중 휴대 격자(칸 수)를 결정**. 더 좋은 가방 = 더 많은 칸.

| 백팩 itemId | 표시명 | 제공 격자 | 등급 |
|---|---|:---:|---|
| (없음) | 맨몸 포켓 | 2×2 | - |
| `backpack_sling` | 슬링백 | 4×4 | Common |
| `backpack_school` | 학생 가방 | 5×5 | Common |
| `backpack_hiking` | 등산 배낭 | 5×7 | Uncommon |
| `backpack_military` | 군용 배낭 | 6×8 | Rare |
| `backpack_tactical` | 택티컬 러크 | 7×9 | Epic |

- 백팩 자체는 **장비 슬롯(백팩)**에 장착 → 그 격자가 메인 인벤이 됨
- 가방 교체 시 안에 든 게 안 들어가면 바닥/창고로 밀려남
- 안전가옥 창고(stash)는 별도·대형, 백팩과 무관
- (옵션) 체스트리그/포켓류 = 추가 소형 격자 — 추후

### 아이템 크기
- 모든 아이템은 **가로 x 세로** 격자 점유
- 가로: 1~3칸, 세로: 1~3칸
- 최대 크기: 3x3 (예: 대형 배낭)

| 크기 | 예시 아이템 |
|---|---|
| 1x1 | 탄약, 열쇠, 진통제, 건전지 |
| 1x2 | 붕대, 통조림, 부목 |
| 2x1 | 단검, 손전등 |
| 2x2 | 구급상자, 수류탄 |
| 1x3 | 파이프 |
| 2x3 | 라이플, 대형 공구 |
| 3x1 | 야구방망이 |
| 3x2 | 대형 가방 |

## 아이템 데이터 (ItemData)

### ScriptableObject 기반
```
ItemData (ScriptableObject)
├── itemId        : string (고유 ID, "bandage", "pipe_weapon" 등)
├── displayName   : string (표시명)
├── description   : string (설명)
├── icon          : Sprite (인벤토리 아이콘)
├── gridWidth     : int (격자 가로 칸)
├── gridHeight    : int (격자 세로 칸)
├── category      : ItemCategory (enum)
├── maxStack      : int (최대 스택 수, 1이면 스택 불가)
├── weight        : float (무게, kg)
├── sellPrice     : int (전당포 판매가, 루디. 0=판매 불가)
├── buyPrice      : int (상점 구매가. 0=구매 불가)
├── rarity        : ItemRarity (enum)
├── isUsable      : bool (우클릭 사용 가능 여부)
├── useEffect     : ItemUseEffect (enum, 사용 시 효과 타입)
└── effectValue   : float (효과 수치)
```

### 카테고리
```
ItemCategory
├── Weapon      : 무기
├── Medical     : 의료 (붕대, 부목, 진통제, 구급상자)
├── Consumable  : 소비 (음식, 음료, 건전지)
├── Material    : 재료 (고철, 나사, 천 조각)
├── Valuable    : 귀중품 (전당포 판매용)
├── Key         : 열쇠
└── Misc        : 기타
```

### 가격 (루디)

- **판매가** `sellPrice`: 전당포·정보 탭 「총 판매가치」 합산에 사용.
- **구매가** `buyPrice`: 상점/NPC 구매용 (미구현 시에도 SO에 선반영).
- 일괄 산정: `tools/ItemPrices.ps1` → `tools/UpdateItemPrices.ps1` (기존 SO 패치).
- 등급 가이드는 `docs/items.md` §7 판매 등급 (D~S)과 동일 축.

| 카테고리 | sell | buy |
|---|---|---|
| Valuable / junk | 희귀도·itemId 오버라이드 | 0 |
| Medical / Consumable | 희귀·효과량 기반 | sell × 2.2~2.5 |
| Material | 희귀 4~48 | sell × 2.5 |
| Weapon / Key / 스토리 | 무기·열쇠 0~55 / 퀘스트 0 | 0 |

### 희귀도
```
ItemRarity
├── Common    : 흰색
├── Uncommon  : 녹색
├── Rare      : 파랑
├── Epic      : 보라
└── Legendary : 금색
```

### 사용 효과
```
ItemUseEffect
├── None          : 효과 없음
├── HealHP        : HP 회복 (effectValue = 회복량)
├── HealInjury    : 부상 치료 (MedicalItemData 연동)
├── RestoreStamina: 스태미너 회복
├── AddBattery    : 배터리 충전
└── Food          : 포만감 (향후 확장)
```

## 아이템 인스턴스 (ItemInstance)

런타임 아이템 개체:
```
ItemInstance
├── data       : ItemData (SO 참조)
├── stackCount : int (현재 스택 수)
├── durability : float (내구도, 0~1, 무기/도구용)
└── uid        : int (고유 인스턴스 ID, 런타임 생성)
```

## 인벤토리 로직 (InventoryGrid)

### 핵심 구조
- `int width, height` — 격자 크기
- `ItemSlot[width, height]` — 각 칸이 어떤 아이템을 참조하는지
- `List<PlacedItem>` — 배치된 아이템 목록

### PlacedItem
```
PlacedItem
├── item      : ItemInstance
├── gridX     : int (좌상단 칸 X)
├── gridY     : int (좌상단 칸 Y)
└── rotated   : bool (90도 회전 여부, 향후 확장)
```

### 핵심 메서드
```
bool CanPlace(ItemInstance item, int x, int y)    // 배치 가능 여부
bool TryPlace(ItemInstance item, int x, int y)    // 배치 시도
bool TryAutoPlace(ItemInstance item)              // 빈 자리에 자동 배치
PlacedItem RemoveAt(int x, int y)                 // 해당 칸의 아이템 제거
PlacedItem GetAt(int x, int y)                    // 해당 칸의 아이템 조회
bool TryStack(ItemInstance item, int x, int y)    // 같은 아이템 스택 시도
List<PlacedItem> GetAll()                         // 전체 아이템 목록
```

## 상호작용

### 드래그 앤 드롭
- 아이템 클릭 → 마우스에 붙음 → 다른 칸에 놓기
- **같은 인벤토리 내 이동**: 칸 간 이동
- **인벤토리 간 이동**: 가방 ↔ 루팅 상자, 가방 ↔ 창고
- **빈 칸에 놓기**: 배치
- **아이템 위에 놓기**: 스택 가능하면 스택, 불가능하면 자리 교환(스왑)
- **인벤토리 밖에 놓기**: 바닥에 드롭

### 우클릭 메뉴 (컨텍스트)
- **사용**: isUsable인 아이템 (의료, 음식 등)
- **검사**: 아이템 상세 정보 표시
- **버리기**: 바닥에 드롭
- **분해**: 재료로 분해 (향후)

### 바닥 아이템 (WorldItem)
- 아이템을 버리면 월드에 3D 오브젝트 생성
- 주우면 인벤토리에 추가 (E키 또는 드래그)
- 루팅 상자와 별도 — **독립된 바닥 아이템**
- 루팅 상자 위에는 바닥 아이템 스폰하지 않음 (기획 결정)

## 아이템 스폰 시스템

### SpawnTable (ScriptableObject)
```
SpawnTable
├── tableId     : string
├── entries[]   : SpawnEntry[]
│   ├── itemData   : ItemData
│   ├── weight     : float (확률 가중치)
│   ├── minCount   : int
│   ├── maxCount   : int
│   └── rarity     : ItemRarity (필터용)
└── rollCount   : int (한 번에 뽑는 횟수)
```

### ItemSpawnPoint (씬 배치 컴포넌트)
```
ItemSpawnPoint
├── spawnTable  : SpawnTable
├── spawnType   : SpawnType (Container / Ground / Fixed)
├── respawn     : bool (재스폰 여부)
├── respawnTime : float
└── hasSpawned  : bool
```

### SpawnType
- **Container**: 루팅 상자에 아이템 채우기 (상자 열면 내부 격자에 아이템)
- **Ground**: 바닥에 아이템 오브젝트 배치
- **Fixed**: 항상 같은 아이템 (열쇠 등 고정 아이템)

## UI 레이아웃

### 인벤토리 화면 (Tab키로 토글)
```
┌─────────────────────────────────────┐
│          ◈ 인벤토리 ◈              │
├──────────────┬──────────────────────┤
│  [플레이어]  │  [루팅 상자/창고]    │
│   5x8 격자   │   크기 가변 격자     │
│              │                      │
│  ■ □ □ □ □  │  □ □ □ □            │
│  ■ □ ■ ■ □  │  □ □ □ □            │
│  □ □ ■ ■ □  │  □ □ □ □            │
│  □ □ □ □ □  │                      │
│  □ □ □ □ □  │                      │
│  ...        │                      │
├──────────────┴──────────────────────┤
│ 무게: 12.5 / 30.0 kg               │
│ [아이템 정보 패널]                  │
└─────────────────────────────────────┘
```

- 좌측: **캐릭터 장비창** — 착용 슬롯(헬멧/방어구/백팩/무기/포켓 등) + 가방 5×8 격자
- 우측: 상황에 따라
  - 안전구역(Tab) → **창고(stash)** 격자
  - 루팅 상자 열었을 때 → 상자 격자
  - 아무것도 안 열었을 때 → 우측 없음 (장비/가방만)
- 하단: 무게 표시 + 호버 중인 아이템 상세 정보

> **창고는 물리 오브젝트가 아니라 Tab 전체화면 UI.** 안전구역 어디서나 Tab으로 장비창+창고를 연다(컨테이너 내부에 보관함 오브젝트를 두지 않음).

### 창고물품 (컨테이너형 아이템 — '가구' 대체)

가구는 물리 오브젝트가 아니라 **창고(stash) 격자 안에 놓는 컨테이너형 아이템**으로 구현(타르코프 아이템 케이스류).

- **개념**: 창고 칸을 차지하지만 내부에 별도 격자를 제공 → 분류 보관 + 공간 효율.
- **종류**(기존 가구 목록 계승): 아이스박스/냉장고(음식·음료), 잡화함·서랍(잡화), 무기 케이스(무기), 재료함(재료), 금고(귀중품·보안) 등.
- **특수효과**: 아이스박스 = 보관 음식 부패 지연 + 음료 냉장(→ survival.md). 금고 = 도난/이벤트 손실 방지(추후).
- **획득**: 상점·NPC 구매 또는 루팅. 같은 종류 복수 보유 가능.
- 물리 가구 배치(낙원식) 방식은 폐기 — 창고물품으로 일원화. (2026-05-26 가구 기획 대체.)

#### 보관함 사양 — 카테고리 제한 + 칸수 (2026-06-10 확정)

> **핵심**: 보관함은 **외부(창고 차지) < 내부(보관 칸)** = 공간 이득(타르코프 케이스). **카테고리 전용일수록 효율↑, 고가치(귀중) 함은 작게(공간 압박 유지), 부피 큰 카테고리(무기·음식)는 넓게.** 범용은 이득 적음.

> 내부 격자 = **외부 대비 ~1.5배 area**(2026-06-10 상향). 외부는 작게(창고 차지 = 케이스 손맛), 내부는 넉넉히. 내부는 자체 격자(별도 뷰)라 창고 폭 제약 없음(가독상 ≤8폭).

| 보관함 | itemId | 보관 카테고리 | 외부(창고) | **내부 격자** | 순이득 | 특수 |
|--------|--------|--------------|:---:|:---:|:---:|------|
| 범용 상자 | `cont_box` | 전체 (제한 없음) | 2×2 (4) | **6×4 (24)** | +20 | 아무거나, 효율 낮음 |
| 냉장고 | `cont_fridge` | Consumable (음식·음료) | 2×3 (6) | **8×7 (56)** | +50 | 부패 지연·냉장(→survival) |
| 잡화 서랍 | `cont_drawer` | Misc (잡템·소형) | 2×2 (4) | **6×8 (48)** | +44 | |
| 재료함 | `cont_material` | Material | 2×2 (4) | **8×6 (48)** | +44 | |
| 의료함 | `cont_medbox` | Medical | 1×2 (2) | **6×5 (30)** | +28 | |
| 무기 케이스 | `cont_weapon` | Weapon | 2×3 (6) | **8×7 (56)** | +50 | 무기 footprint 커 넓게 |
| 탄약함 | `cont_ammo` | 탄약(Ammo) | 1×2 (2) | **6×5 (30)** | +28 | 총기 도입 시 |
| 문서·열쇠함 | `cont_docs` | Key (열쇠·쪽지·정보) | 1×2 (2) | **5×6 (30)** | +28 | |
| 금고 | `cont_safe` | Valuable (귀중·루디·전력셀) | 2×2 (4) | **6×4 (24)** | +20 | 도난/이벤트 손실 방지, **비쌈** |

- **카테고리 불일치 = 배치 불가**(`FurnitureData.allowedCategories` / 컨테이너 격자 검사). 범용 상자만 무제한.
- **밸런스 의도**: 금고는 내부 가장 작지만(24) 귀중품이 1×1~2×2라 충분 + **의도적 압박**(다 못 넣음 → 팔아라). 무기·음식은 부피 커서 넓은 내부(56). 전용함 효율(>범용)이 "분류 정리"의 보상.
- 가격(가안, §9.7 관리인 판매): 서랍 5만 · 재료함 8만 · 의료함 8만 · 냉장고 12만 · 무기 케이스 15만 · 금고 30만. **비싸게**(거점 정리 = 사치 QoL).

#### 보관함 데이터 구조 — **B: ItemData + 컨테이너 필드 (확정 2026-06-10)**

> 방식 결정: **보관함 = 일반 ItemData**(루팅·판매·인벤·이동 가능)이면서 **자기만의 내부 격자**를 갖는다(타르코프 케이스). 별도 `FurnitureData`(옛 가구 방식)는 **폐기·흡수.**

**ItemData에 추가 필드**
```
isContainer        : bool                // 컨테이너 여부
internalWidth      : int                 // 내부 격자 가로 (예: 냉장고 8)
internalHeight     : int                 // 내부 격자 세로 (예: 냉장고 7)
allowedCategories  : ItemCategory[]      // 빈 배열=전체(범용 상자), 아니면 전용
```
- **외부 footprint** = 기존 `gridWidth/Height`(창고에서 차지하는 칸). **내부** = 위 `internalWidth/Height`. (외부<내부 = 공간 이득)

**런타임**
- 보관함 **인스턴스마다 `InventoryGrid` 1개**(internalW×H). `ItemInstance`에 `containerGrid` 보유(컨테이너일 때만).
- 창고(stash)에 놓인 보관함을 **열면 내부 격자 패널**(중첩 격자). 카테고리 불일치 = **배치 불가**(`allowedCategories` 검사).
- 보관함을 창고에서 빼면 내부 내용물째 이동(레이드 반출 룰은 별도). 같은 종류 복수 보유 가능.

**마이그레이션**
- 기존 `FurnitureData` SO/`SafehouseStorage` 가구 인스턴스 → **컨테이너 ItemData로 일원화**(2026-05-26 가구 기획 대체 마무리).
- `cont_*` 9종(§보관함 사양) = 위 필드 채운 ItemData로 생성 — **필드 추가가 SO 생성의 선행 작업**(현재 미생성 사유).

**구현 순서**
1. `ItemData`에 4필드 + `ItemInstance.containerGrid` 추가.
2. 중첩 InventoryGrid UI(보관함 열기).
3. `cont_*` 9종 SO 생성(칸수·카테고리=§보관함 사양 그대로).
4. 관리인 판매 + 레이드 저확률 드랍 + 의뢰 1개 연결(§9.7).

## 구현 우선순위

1. **ItemData SO** + ItemDatabase
2. **ItemInstance** 런타임 클래스
3. **InventoryGrid** 로직 (UI 없이 데이터만)
4. **InventoryUI** 격자 렌더링 + 드래그앤드롭
5. **WorldItem** 바닥 아이템
6. **SpawnTable + ItemSpawnPoint**
7. **루팅 상자 연동** (InteractableObject.Container)
8. **창고 연동** (안전가옥)

---

## 내구도 시스템 (Durability)

### 개요
의료 아이템(구급상자 등)과 일부 소비 아이템은 **내구도 기반 다회 사용**.
타르코프의 IFAK/살레와처럼 사용할 때마다 내구도가 소모되고, 0이 되면 파괴.

### 아이템 유형별 소비 방식
| 유형 | 소비 방식 | 예시 |
|---|---|---|
| 일회성 | stackCount 감소 | 붕대, 진통제, 건전지, 통조림 |
| 내구도형 | durability 감소 | 구급상자, 수술키트 |

### ItemData 추가 필드
```
hasDurability      : bool    (true면 내구도 기반 소비)
maxDurability      : float   (최대 내구도, 예: 300)
durabilityCostPerUse : float (1회 사용 시 소모량, 예: 50)
```

### 동작 규칙
- `hasDurability = true`인 아이템: 사용 시 `durability -= durabilityCostPerUse`
- `durability <= 0`이면 아이템 파괴 (격자에서 제거)
- 내구도형 아이템은 **스택 불가** (maxStack = 1)
- UI에 현재/최대 내구도 게이지 표시
- 예시: 구급상자 (300/300) → HP 회복 1회 = 내구도 50 소모 → 6회 사용 가능

### 기획 결정
- 날짜: 2025-05-25
- 질문: 의료/HP 회복 아이템에 내구도 시스템 적용?
- 결정: 구급상자, 수술키트 등 고급 의료 아이템은 내구도 기반 다회 사용. 붕대/진통제는 일회성(스택) 유지.
- 근거: 타르코프 IFAK 참고. 고급 아이템의 가치 차별화 + 인벤 공간 효율.

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2025-05-25 | 인벤토리 시스템 초기 기획 확정. 타르코프 스타일 격자, 세로형 패널, 드래그앤드롭, 바닥 드롭, 스폰 시스템. |
| 2025-05-25 | 내구도 시스템 추가. 구급상자 등 고급 의료 아이템은 durability 기반 다회 사용. |
| 2025-05-25 | 아이템 가격 필드 추가 (sellPrice/buyPrice). 루디 기준. 정보 탭에 총 판매가치 표시. |
| 2026-05-25 | 전 ItemData SO(172종)에 sellPrice/buyPrice 일괄 반영. `tools/ItemPrices.ps1` 규칙 + `UpdateItemPrices.ps1`. 신규 SO는 `GenerateFromCsv.ps1`에서 동일 규칙 적용. |
| 2026-05-26 | **루팅 상자 수색 연출 구현.** 타르코프/덕코프 스타일 — 아이템이 1개씩 순서대로 공개. 희귀도별 딜레이(Common 0.4s → Legendary 1.8s). 미공개 아이템은 "?" 표시, 수색 중 아이템은 펄스+프로그레스 바. 미공개 아이템 드래그 불가 가드 추가. 안전가옥 창고는 즉시 전체 공개. |
| 2026-05-26 | **우클릭 컨텍스트 메뉴 구현.** 사용(초록)/검사(파랑)/버리기(주황) 3종. 마우스 위치에 팝업. 검사 시 정보 탭으로 상세 표시(카테고리·크기·무게·가격·내구도·효과). 좌측 격자(상자/창고)에서도 검사 가능. 미공개 아이템은 메뉴 불가. |
| 2026-05-26 | **재화 이름 통일.** "스크랩 코인" → "루디(Rudy)" 전면 변경 (docs, UI 코드, ItemData 헤더, tools). |
| 2026-05-26 | **MedicalData 생성 스크립트.** `Tools > Dev Tools > Data > Generate Medical Data` — 13종 MedicalItemData SO 자동 생성 + ItemData.medicalData 자동 연결. |
| 2026-05-30 | **소비 아이템 스태미너 회복 구현.** `PlayerController.RestoreStamina(amount)` 신설(최대치 클램프 + 탈진 해제). `ItemUseEffect.RestoreStamina`가 `effectValue`만큼 즉시 회복. 기존 TODO(음수 ConsumeStamina) 제거. |
| 2026-06-10 | **백팩 = 휴대 격자 제공 아이템 확정.** 장착한 백팩이 레이드 중 격자 칸 수 결정(슬링4×4~택티컬7×9). 가방 없으면 포켓 2×2만. 좋은 가방 루팅/장착이 곧 적재량 업그레이드 → 루팅 루프 동기 강화. 백팩 5종 아이콘 제작 예정. |
| 2026-06-02 | **인벤토리·창고·장착무기 세이브 영속화.** 루팅한 전리품이 저장되도록 `SaveManager` 확장. `InventoryGrid.GetSaveData/LoadSaveData`(공용) — `GridItemEntry`(itemId·count·durability·격자위치·회전), 로드 시 저장 위치 우선 복원→실패 시 자동배치. 저장 대상: **가방**(`PlayerInventory.Grid`), **창고**(`SafehouseStorage.AllFurniture` static 목록을 uid 매칭으로 각 `FurnitureInstance.grid` 복원), **장착 무기**(`PlayerEquipment.GetSaveData`=itemId). ※ 게임 시작 직후 로드 시 창고 가구가 아직 인스턴스화 전이면 매칭 누락 가능(안전가옥 진입 후 저장/로드는 정상) — 알려진 한계. |
