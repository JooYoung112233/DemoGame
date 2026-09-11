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

### 7-4. 의뢰/NPC 퀘스트 UI 룩 방향 (참조 이미지) — 2026-06-17

> 그레이박스 이후 본 UI 룩. 레이아웃·구조만 확정, 아트 추후.

참조 = **메신저(디스코드형) 채팅 UI**.

- **좌측 사이드바**: 의뢰인/NPC **목록**(아바타+이름) — 대화 상대(의뢰 발신자) 리스트. 새 의뢰/읽지 않음 뱃지.
- **우측 본문**: 선택한 NPC와의 **대화 스레드** — 의뢰 내용·목표·보상이 메시지 형태로 표시, 첨부(이미지/지도/아이템) 가능. 하단 입력/응답(수락·거절·보고) 영역.
- 게시판(사물)에서 받는 상시 의뢰와 별개로, **NPC가 주는 의뢰는 이 메신저 뷰**로 통합되는 방향. (전달 채널 = 무전/통신 톤과 정합 — 라디오 §safehouse-intel.)

---

## 기획 결정 로그

### 2026-07-02 — 루디 납품 보상 역전 + 구(舊)스케일 보상 정리
- **질문**: ①DQ-006("후하게 쳐주겠다")의 보상 25,000이 루디 **직판가보다 낮아** 대사와 모순 — 직판 기준가 = 전당포 sellRate **0.6** × `ruby_shard` sellPrice 50,000 = **30,000/개**. DQ-007("특별히 더 쳐주겠다")도 3개 직판 90,000의 절반 수준. ②MQ-001/MQ-002/SQ-001의 Currency 보상(10/150/120)은 ×100 경제 개편 이전 **구스케일 잔재**(현 경제의 ~1/1000).
- **결정**: Currency 보상(`rewards` type:1)만 재조정. 다른 보상(아이템/신뢰/평판)은 불변.

  | 퀘스트 | before | after | 근거 |
  |---|---:|---:|---|
  | DQ-006 (루디 1개 납품) | 25,000 | **40,000** | 직판 30,000 대비 +33% 프리미엄 — "후하게" 대사 성립 |
  | DQ-007 (루디 3개 납품) | 50,000 | **130,000** | 직판 90,000 대비 +44%. 개당 43,333 > DQ-006 개당 40,000 → "특별히 더" 성립 |
  | MQ-001 | 10 | **3,000** | 구스케일 정정. 초반 메인 보상 |
  | MQ-002 (루디 조각 납품) | 150 | **32,000** | 시장가 30,000짜리 아이템을 넘기는 퀘스트 — 시장가 이상 보장 |
  | SQ-001 | 120 | **6,000** | 구스케일 정정. DQ 스케일(6천~1.4만) 하단 |

- **판단 기준(재사용)**: **납품형 의뢰 보상 ≥ 직판가(전당포 sellRate 0.6) + 프리미엄.** 납품 의뢰가 직접 팔기보다 손해면 대사·동기가 모두 깨진다.
- **파급**: `quests-region1.md` §2/§3/§5 보상 표 수치 갱신(등급 라벨 → 실수치). 경제 요약은 economy.md 변경 로그.
- **가정(검증 필요)**: DQ-006/007은 반복 의뢰(`isRepeatable`) — 루디 수급률 실측 후 프리미엄 폭(+33%/+44%)이 스크랩 인플레를 만드는지 플레이테스트 확인.

### 2026-06-18 — 통신함 예시 의뢰 2개 추가 (데모용)
- 스토리 톤을 따와 가용 의뢰 예시 2개 SO 생성(`Resources/Data/Quests/`): **ex_q01 "폐상가 통조림 회수"**(수집, 의뢰인=베테랑 회수꾼, 통조림 `canned_food` ×3 → 스크랩 8,000 + 붕대 ×2), **ex_q02 "점포 구역 정리"**(처치, 의뢰인=구역 관리인 `district_warden`, 밴딧 `bandit_melee` ×3 → 스크랩 12,000 + 고철 ×3 + 신뢰 +3). QuestLogUI(통신함)가 `Resources/Data/Quests`를 가용 의뢰로 이미 로드 → 좌측 발신자 목록 + 우측 스레드에 "신규"로 표시(코드 수정 불필요). 수락/보고는 기존 QuestManager 흐름 유지.

### 2026-06-17 — 의뢰/NPC 퀘스트 UI 룩 (참조 이미지)
- **질문**(사용자, 이미지 첨부): 의뢰 및 NPC에게 받는 퀘스트 UI를 어떤 느낌으로?
  - **결정**: **메신저(디스코드형) 채팅 UI.** 좌측 = 의뢰인/NPC 목록(아바타+이름, 새 의뢰 뱃지), 우측 = 대화 스레드(의뢰 내용·목표·보상 메시지 + 첨부). NPC 의뢰는 이 메신저 뷰로 통합. 레이아웃만 확정, 아트 추후. (§7-4)
  - **근거**: 사용자 제시 참조 이미지. 통신 톤(무전/메신저)으로 의뢰 전달 일관성.

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

### 2026-06-19 — 콘텐츠 분량 확장 기획 (의뢰·보상)
- **질문**: 의뢰 표본·보상이 너무 적다(`QuestData` SO **6개**·그중 예시 ex_q01/02 2개, 보상 대부분 **스크랩 단일**) — 다방면으로 늘리는 기획.
  - **결정(의뢰 SO화)**: [`quests-region1.md`](quests-region1.md)에 이미 설계된 **MQ-003 · SQ-003 · DQ-001~009**를 `QuestData` SO로 구현(예시 ex_q01/02 정리·대체). 의뢰인 = 회수꾼/관리인/전당포/블랙마켓. 게시판 상시의뢰 + NPC 의뢰 혼합, **DQ 일부 `isRepeatable`**(일일·상시 보급). 데모 1지역 ~**12개 내외**(과하지 않게).
  - **결정(보상 다양화)**: 의뢰별 **1~3개 보상 믹스**(`QuestReward[]`·type Item/Currency/Affinity/Trust/Recipe 활용). 스크랩 + (아이템 / 재료 묶음 / **레시피 해금** / 평판 / 루디). 난이도·평판 구간별 보상 등급. **구조는 이미 다중 지원 — 콘텐츠만 채움.**
  - **근거**: 구조 한계 아님(rewards·objectives 배열). 설계는 풍부(15+)인데 SO 구현이 6개뿐 → 백로그 구현 + 보상 다양화. 평판 게이트(2026-06-04)·전당포 수배(economy.md 2026-06-19)와 연동.
  - **구현 완료(2026-06-19)**: `QuestData` SO **11개 신설**(MQ-003, SQ-003, DQ-001~009) + 예시 `ex_q01/02` 삭제. 보상은 의뢰별 1~3개 믹스(Currency+Item+Affinity/Trust). **`QuestRewardType.Recipe` 신설**(itemId=recipeId → `CraftingSystem.UnlockRecipe`, `QuestManager.GiveRewards`/`QuestLogUI`/`DialogueUI` 표시 케이스 추가) — SQ-003에 `cooked_stew` 해금 보상으로 시연. **`DailyQuestManager`**: 일일 풀을 `Data/DailyQuests` + `Data/Quests`의 `isRepeatable`(DQ-*)에서 로드하도록 보강(DQ를 옮기지 않아 id 로드 안 깨짐). 의뢰 로드 = `Resources.LoadAll`(폴더 자동, 레지스트리 없음). 미해결: 의뢰인 id `district_warden`(코드) vs 문서 `zone_manager` 표기 통일은 doc 정리 대상.
