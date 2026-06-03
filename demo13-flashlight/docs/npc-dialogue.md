# NPC 호감도 & 대화 시스템

## 시스템 개요
NPC와의 관계를 3축 호감도로 관리하고, 혼합형 대화 시스템으로 스토리를 전달한다.

---

## 1. 호감도 시스템 (3축)

### 1-1. 호감도 (Affinity)
- **정의**: NPC가 플레이어를 얼마나 좋아하는가
- **상승**: 선물 제공, 퀘스트 완료, 호의적 대화 선택지
- **하락**: 나쁜 대화 선택지 선택 시
- **효과**: 상점 품목 확장, 추가 퀘스트 해금, 엔딩 단서 제공

### 1-2. 신뢰도 (Trust)
- **정의**: NPC가 플레이어를 얼마나 믿는가
- **상승**: 약속 이행, 중요 임무 성공, 위험 상황에서 NPC 구출
- **하락**: 임무 실패, 배신 행위, 거짓말 선택지
- **효과**: 핵심 정보 공개 (밤 지역 힌트, 동생 단서), 고급 제작 레시피 해금, 시설 업그레이드 할인

### 1-3. 두려움 (Fear)
- **정의**: NPC가 플레이어를 얼마나 두려워하는가
- **상승**: 위협/강압 대화 선택지, 폭력적 행동
- **하락**: 없음 (한번 쌓인 두려움은 유지)
- **효과 (복종형)**:
  - 가격 할인, 즉각적 정보 제공
  - **대가**: 대화 톤이 차갑고 위축됨, 호감도/신뢰도 기반 콘텐츠 잠김
  - 호감도·신뢰도 루트에서만 나오는 퀘스트/엔딩 단서를 영구 차단할 수 있음

### 1-4. 축 간 관계
| 행동 | 호감도 | 신뢰도 | 두려움 |
|------|--------|--------|--------|
| 퀘스트 완료 | +2 | +3 | — |
| 선물 제공 | +3 | +1 | — |
| NPC 구출 | +2 | +5 | — |
| 약속 이행 | +1 | +3 | — |
| 임무 실패 | — | -3 | — |
| 위협 선택지 | -2 | -1 | +3 |
| 나쁜 선택지 | -2 | — | — |

> 수치는 밸런싱 단계에서 조정. 현재는 상대적 비율만 표현.

---

## 2. 대화 시스템 (혼합형)

### 2-1. 기본 구조
- **평상시**: 상태 기반 대화 — 호감도/신뢰도/두려움 수치 + 퀘스트 진행도에 따라 NPC가 다른 대사 출력
- **중요 이벤트**: 선택지 분기 — 2~3개 선택지 팝업, 선택에 따라 호감도 변화 + 스토리 분기

### 2-2. 대화 흐름
```
[플레이어가 NPC에게 접근 → E키 상호작용]
    │
    ├─ 일반 대화 (상태 기반)
    │   └─ 조건에 맞는 대사 1~3줄 출력
    │   └─ [계속] 버튼으로 진행
    │
    └─ 이벤트 대화 (선택지 분기)
        └─ NPC 대사 출력
        └─ 선택지 2~3개 표시
        └─ 선택 → 결과 대사 + 호감도 변동 + (선택적) 퀘스트 트리거
```

### 2-3. 대화 우선순위
대화 시작 시 아래 순서로 체크:
1. **긴급 이벤트 대화** (퀘스트 완료 보고, 스토리 트리거 등)
2. **신규 퀘스트 제안** (조건 충족 시)
3. **상태 기반 일반 대화** (호감도 구간별 대사풀에서 랜덤)

---

## 3. 대화 UI

### 3-1. 레이아웃 (하단 대화창)
```
┌──────────────────────────────────────────────┐
│                                              │
│              (게임 화면)                       │
│                                              │
├──────────────────────────────────────────────┤
│ [NPC 이름]                                    │
│ "대사 내용이 여기에 표시됩니다..."               │
│                                    [▶ 계속]   │
├──────────────────────────────────────────────┤
│  ① 호의적 선택지                               │
│  ② 중립 선택지                                 │
│  ③ 위협 선택지 (두려움 +)                       │
└──────────────────────────────────────────────┘
```

### 3-2. UI 요소
- NPC 이름 + 현재 관계 아이콘 (호의/중립/경계/두려움)
- 대사 텍스트 (타이핑 연출)
- 선택지 패널 (이벤트 대화 시에만 표시)
- 선택지에 호감도 변화 힌트 표시 여부: **미표시** (플레이어가 직관으로 판단)

---

## 4. 데이터 구조 (ScriptableObject)

### 4-1. NPCData (ScriptableObject)
```
NPCData
├── npcId: string
├── displayName: string
├── role: string (예: "전당포 주인", "의사")
├── initialAffinity/Trust/Fear: int       // 첫 대면 시 1회 시드 (NPCController→SeedRelationship). §5 '초기 호감도' 데이터화
├── defaultDialogues: DialogueEntry[]     // 상태 기반 기본 대사
├── eventDialogues: EventDialogue[]       // 이벤트/선택지 대화
├── shopData: ShopData (optional)         // 거래 — 선택지 openShop으로 진입
└── availableQuests: QuestData[] · shopInventory: ItemData[] (optional)
```

### 4-2. DialogueEntry
```
DialogueEntry
├── id: string
├── lines: string[]                       // 순차 출력할 대사들
├── conditions: DialogueCondition         // 출력 조건
│   ├── minAffinity: int
│   ├── minTrust: int
│   ├── minFear: int
│   ├── requiredQuest: string (완료 필수 퀘스트)
│   └── requiredFlag: string (범용 플래그)
└── priority: int                         // 같은 조건 충족 시 우선순위
```

### 4-3. EventDialogue
```
EventDialogue
├── id: string
├── triggerCondition: DialogueCondition
├── npcLines: string[]                    // NPC 대사
├── choices: DialogueChoice[]
│   ├── text: string                      // 선택지 텍스트
│   ├── affinityChange: int
│   ├── trustChange: int
│   ├── fearChange: int
│   ├── resultLines: string[]             // 선택 후 NPC 반응
│   ├── triggerQuest: string (optional)
│   ├── setFlag: string (optional)
│   └── openShop: bool                    // 이 선택지로 연결된 ShopData 열기
└── oneShot: bool                         // true면 1회만 발생
```

### 4-4. NPCRelationship (런타임 데이터, 세이브 대상)
```
NPCRelationship
├── npcId: string
├── affinity: int (0~100, 초기값 20)
├── trust: int (0~100, 초기값 10)
├── fear: int (0~100, 초기값 0)
├── completedEvents: HashSet<string>      // 이미 본 이벤트 대화
└── flags: Dictionary<string, bool>       // 범용 플래그
```

---

## 5. NPC별 관계 방향성 (6인)

| NPC | 초기 호감도 | 호감도 루트 핵심 보상 | 두려움 루트 가능? |
|-----|------------|---------------------|----------------|
| 전당포 주인 | 30 (면식 있음) | 주인공 과거 단서, 고급 거래 | O (할인 가능, 단서 차단) |
| 의사 | 20 | 희귀 치료제, 감염 치료 | X (의사는 위협해도 안 통함) |
| 수리공 | 20 | 제작 레시피, 시설 업그레이드 | O (할인 가능, 레시피 차단) |
| 떠돌이 상인 | 15 (경계) | 밤 전용 장비 판매 | O (약간 할인, 희귀품 차단) |
| 전직 공무원 | 10 (은둔) | 정부 붕괴 기록, 금지구역 정보 | X (정보원은 두려움에 입 다묾) |
| 수상한 아이 | 25 | 이상현상 힌트, 숨겨진 경로 | X (아이에게 두려움은 도망 유발) |

> 두려움 루트 불가 NPC는 위협 선택지가 아예 없거나, 선택해도 Fear가 오르지 않고 역효과만 발생.

---

## 6. 호감도 등급 구간

| 구간 | 수치 | 라벨 | 효과 |
|------|------|------|------|
| 1 | 0~19 | 경계 | 최소 대사만, 상점 비쌈 |
| 2 | 20~39 | 중립 | 기본 대화, 기본 상점 |
| 3 | 40~59 | 호의 | 추가 퀘스트, 할인 |
| 4 | 60~79 | 친밀 | 핵심 정보, 고급 거래 |
| 5 | 80~100 | 신뢰 | 엔딩 단서, 최고급 보상 |

> 신뢰도(Trust)도 동일 구간표 사용. 두려움(Fear)은 0~30(무시)/31~60(경계)/61~100(복종) 3단계.

---

## 기획 결정 로그

### 2025-05-25
- **질문**: 대화 시스템 방식 (선택지 분기 / 키워드 / 상태 기반 / 혼합)?
  - **결정**: 혼합형 (평소 상태 기반 + 중요 이벤트에서 선택지 분기)
- **질문**: 호감도 복잡도 (단순 수치 / 등급 / 다축)?
  - **결정**: 다축형 — 호감도(Affinity), 신뢰도(Trust), 두려움(Fear) 3축
- **질문**: 대화 데이터 관리 방식?
  - **결정**: ScriptableObject
- **질문**: 대화 UI 스타일?
  - **결정**: 하단 대화창 (RPG 전통 스타일)
- **질문**: 축별 하락 규칙?
  - **결정**: 호감도 — 나쁜 선택 시 하락 / 신뢰도 — 실패·배신 시 하락 / 두려움 — 하락 없음 (영구 축적)
- **질문**: 두려움(Fear) 효과 방식?
  - **결정**: 복종형 — 단기 이득(할인·정보) but 호감도/신뢰도 콘텐츠 잠김

---

## 7. NPC 퀘스트 마커 (NPCQuestMarker)

### 7-1. 개요
NPC 머리 위에 3D 마커를 표시하여 퀘스트/대화 상태를 시각적으로 전달.
QuestManager 이벤트를 구독하여 자동 갱신.

### 7-2. 마커 상태 (우선순위 순)

| 우선순위 | 상태 | 시각 | 의미 |
|---------|------|------|------|
| 1 | ReadyToReport | ❗ 금색 느낌표 | 완료된 퀘스트 보고 가능 |
| 2 | Available | ❓ 금색 물음표 | 수주 가능한 퀘스트 있음 |
| 3 | InProgress | ... 회색 | 퀘스트 진행 중 |
| 4 | Story | 💬 하늘색 다이아몬드 | 스토리 씬 대기 |
| 5 | Talk | 회색 점 | 일반 대화 가능 |
| 6 | None | 없음 | 숨김 |

### 7-3. 비주얼
- 3D TextMesh (❗/❓/...) + 그림자 텍스트
- 프리미티브 도형 (Story=다이아몬드, Talk=구)
- Y축 빌보드 + sin 웨이브 위아래 흔들림
- NPCController.Start()에서 자동 부착

### 7-4. 갱신 트리거
- QuestManager.OnQuestAccepted / OnQuestCompleted / OnObjectiveUpdated
- DialogueUI 닫힘 후 (DelayedMarkerRefresh 코루틴)
- StoryTriggerManager 스토리 씬 완료 콜백

### 7-5. 맵 빌더 연동
- MapObjectType.NPC → 스폰 설정 패널에서 NPC ID / 표시 이름 입력
- PlacedMapObject.npcId → Resources/Data/NPC/{npcId}에서 NPCData SO 로드
- MapObjectSpawner가 NPCController + NPCQuestMarker 자동 부착

### 2025-05-28
- **질문**: NPC 머리 위 퀘스트 마커 방식?
  - **결정**: 3D TextMesh/프리미티브 기반 마커. QuestManager 이벤트 구독으로 자동 갱신.
  - **상태 우선순위**: ReadyToReport > Available > InProgress > Story > Talk > None
  - **비주얼**: 금색 ❗/❓, 회색 진행중, 하늘색 스토리, 회색 대화

### 2026-06-04
- **NPC 메이커 추가** (`Tools/TopDown/Content/NPC Maker`): NPCData(대사/이벤트분기/선택지/조건/관계 초기값) + 연결 ShopData를 한 창에서 편집. 버튼: '전당포 프리셋'(ShopData 생성·연결 + sellRate 0.6 + openShop 선택지), 'openShop 선택지 추가', '현재 씬에 NPC 배치'(InteractableObject(NPC)+NPCController+스프라이트, npcData 연결).
- **관계 초기값 데이터화**: `NPCData.initialAffinity/Trust/Fear` 추가 → `NPCController.Start`에서 `NPCRelationshipManager.SeedRelationship`로 첫 대면 1회 시드(이미 있으면 무시). 위 §5의 NPC별 '초기 호감도'(전당포 주인 30 등)를 실제 데이터로 연결.
- 비고: 마커는 빌트인 스프라이트 플레이스홀더(스프라인 아트는 추후). 대화 트리 편집은 SerializedProperty 기본 드로어로 전체 중첩 편집.
