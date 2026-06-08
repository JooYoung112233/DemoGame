# 퀘스트 시스템

## 시스템 개요
NPC가 제공하는 임무 시스템. 안전가옥 고정 NPC + 레이드 중 랜덤 NPC 양쪽에서 퀘스트 수주 가능.

---

## 1. 퀘스트 유형

### 1-1. 수집 (Collect)
- "특정 아이템 N개를 가져와라"
- 완료 조건: 인벤토리에 해당 아이템 보유 후 NPC에게 보고
- 보고 시 아이템 회수

### 1-2. 처치 (Kill)
- "특정 구역의 적 N마리 처치"
- 완료 조건: 레이드 중 해당 적 처치 카운트 달성

### 1-3. 탐색 (Explore)
- "특정 구역의 특정 지점 도달"
- 완료 조건: 지정된 InteractableObject 상호작용

### 1-4. 배달 (Deliver)
- "아이템을 다른 NPC에게 전달"
- 완료 조건: 대상 NPC에게 대화 시 아이템 자동 전달

---

## 2. 퀘스트 데이터 (ScriptableObject: QuestData)

```
QuestData
├── questId: string
├── title: string                     // 퀘스트 제목
├── description: string               // 설명
├── questType: QuestType              // Collect / Kill / Explore / Deliver
├── giverNpcId: string                // 퀘스트 제공 NPC
├── objectives: QuestObjective[]      // 목표 목록
│   ├── type: ObjectiveType           // CollectItem / KillEnemy / ReachPoint / TalkToNPC
│   ├── targetId: string              // 아이템ID / 적ID / 지점ID / NPC ID
│   ├── requiredCount: int            // 필요 수량
│   └── description: string           // 목표 설명 텍스트
├── rewards: QuestReward[]            // 보상
│   ├── type: RewardType              // Item / Currency / Affinity / Trust / Recipe
│   ├── itemId: string
│   ├── amount: int
│   └── npcId: string                 // Affinity/Trust 보상 대상
├── unlockConditions: QuestCondition  // 해금 조건
│   ├── requiredAffinity: int         // NPC 호감도 최소치
│   ├── requiredTrust: int
│   ├── requiredQuest: string         // 선행 퀘스트 (완료 필수)
│   └── requiredFlag: string
├── region: string                    // 수행 지역 (빈값이면 아무 곳)
├── isRepeatable: bool                // 반복 가능 여부
└── expiresOnRaid: bool               // 레이드 1회 한정 (레이드 NPC 퀘스트용)
```

## 3. 퀘스트 상태 흐름

```
Available (조건 충족, 아직 수주 안 함)
    → NPC 대화에서 수주
Active (진행 중)
    → 목표 달성
ReadyToReport (완료 가능, 보고 대기)
    → NPC에게 보고
Completed (완료, 보상 수령)

Failed (실패 — 레이드 한정 퀘스트에서 탈출/시간초과)
```

## 4. 퀘스트 제공 경로

### 4-1. 안전가옥 NPC (메인)
- 기존 6명 NPC가 호감도/신뢰도 조건에 따라 퀘스트 제안
- 대화 우선순위에서 "신규 퀘스트 제안"이 상태대화보다 우선
- 퀘스트는 다음 레이드에서 수행, 귀환 후 보고

### 4-2. 레이드 중 NPC (랜덤)
- 레이드 맵에 랜덤 스폰되는 NPC
- 즉석 퀘스트 제공 (같은 레이드 내 완료)
- `expiresOnRaid = true`
- 보상은 즉시 지급 또는 귀환 시 우편함

## 5. 퀘스트 추적 (QuestManager)

```
QuestManager (Singleton, DontDestroyOnLoad)
├── activeQuests: List<QuestInstance>
├── completedQuests: HashSet<string>  // 퀘스트ID 기록
├── AcceptQuest(QuestData)
├── UpdateObjective(type, targetId, count)  // 이벤트 수신
├── IsQuestComplete(questId): bool
├── CompleteQuest(questId)            // 보상 지급
└── GetAvailableQuests(npcId): List<QuestData>  // 조건 체크
```

### QuestInstance (런타임)
```
QuestInstance
├── data: QuestData
├── state: QuestState
├── progress: Dictionary<int, int>    // objective index → current count
└── acceptedTime: float
```

## 6. 이벤트 연동
- **아이템 획득** → `QuestManager.UpdateObjective(CollectItem, itemId, 1)`
- **적 처치** → `QuestManager.UpdateObjective(KillEnemy, enemyId, 1)`
- **상호작용** → `QuestManager.UpdateObjective(ReachPoint, pointId, 1)`
- **NPC 대화** → `QuestManager.UpdateObjective(TalkToNPC, npcId, 1)`

## 7. 퀘스트 UI

### 7-1. 퀘스트 수주 (대화 중)
NPC 대화 시스템에 통합. EventDialogue의 triggerQuest 필드 사용.

### 7-2. 퀘스트 로그 (HUD)
- 화면 우측에 현재 활성 퀘스트 간략 표시
- 목표 진행도 표시 (예: "붕대 수집 2/5")
- Tab 키로 전체 퀘스트 로그 열기

### 7-3. 퀘스트 완료 알림
- 목표 달성 시 화면 중앙 상단 "퀘스트 목표 달성!" 팝업
- 보고 가능 시 NPC 위에 ! 아이콘

---

## 기획 결정 로그

### 2026-05-26
- **질문**: NPC 퀘스트 수주/수행 위치?
  - **결정**: 안전가옥 NPC (메인 퀘스트) + 레이드 중 NPC (즉석 퀘스트) 둘 다 지원
- **질문**: 퀘스트 유형?
  - **결정**: 수집(Collect), 처치(Kill), 탐색(Explore), 배달(Deliver) 4종
- **질문**: 구현 우선순위?
  - **결정**: 레이드 후 이벤트와 동시에 작업

### 2026-06-04 — 게시판 의뢰 보드 + 평판 (도입부 재설계)
- **질문**: 출전 방식 — 지도판(지역 선택 UI) vs 의뢰서?
  - **결정**: **지도판 폐기 → 게시판 의뢰 보드.** 게시판에서 의뢰서 수령(수주)하면 목적지·낮밤·목표가 결정됨. 자유 탐색은 "상시 의뢰"가 늘 붙어 있어 반복 가능. (safehouse.md 지도판 제거 연동.)
- **질문**: 의뢰 수령/보고 위치?
  - **결정**: **수령(수주) = 게시판(사물)에서 직접 / 완료 보고 = 의뢰인 NPC에게.** 게시판은 보드일 뿐, 보고는 사람에게. 첫 의뢰 MQ-001 의뢰인 = 베테랑 회수꾼.
- **질문**: 의뢰 해금 게이트?
  - **결정**: **평판(이름값) 신규 축 도입.** 도시·회수꾼 사회 명성. 게시판 의뢰가 평판 구간별로 잠김(신참=루디 회수만, 평판↑ 시 고급 의뢰 해금). NPC 호감도(개인 관계)와 별개 축. 레이드 성공·의뢰 완료로 상승. **구간·BQ 풀·NQ 마일스톤** → [quests-region1.md §9](quests-region1.md).
