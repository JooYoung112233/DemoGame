# Architecture (씬 / 부트 구조)

## Systems 부트 씬 — 영속 additive (2026-06-03 도입)

게임의 **항상 떠 있는 한 세트**(매니저 + 플레이어 + 카메라 + 라이트 + UI)를
별도 씬 `Assets/Scenes/Systems.unity`에 모아 두고, 게임플레이(맵 콘텐츠) 씬을
그 **위에 additive로 교체 로드**하는 구조.

```
[Systems] (항상 로드, 절대 언로드 안 함)
 ├─ 매니저들(루트 GO): Quest / NPCRelationship / PostRaidEvent / Toast / Narration /
 │   Tutorial / ScreenEffect / DailyQuest / Achievement / Currency / Save /
 │   StoryLocale / StoryPlayer / StoryTrigger / SceneTransitionManager
 ├─ UIManager → GameHUD / RaidResult / MapSelect / CharacterPanel / Crafting /
 │              Shop / Dialogue / PostRaidEvent / Quest   ← 상점 포함 모든 UI가 씬 배치
 ├─ PlayerRig (Player + Main Camera(오소 62°) + 착용등 WornLamp + 후처리 Volume)
 ├─ DayNightCycle(태양·앰비언트 — WeatherData) + SystemsSceneEnforcer(2D 시절 글로벌 조명 중복 정리 — 잔재)
 └─ GameBoot
        └─ additive ─┐
   [Safehouse(=마을)] / [Hideout] / [Pawnshop] / [Zone1]  ← 여기만 교체 로드 (옛 `Int_*` 실내 씬은 2026-09-12 삭제)
     (맵 / 프롭 / 스폰포인트 / 인터랙터블만)
```

### 진입 시나리오
| 시작 씬 | 동작 |
|---------|------|
| **Systems** | `GameBoot`이 기본 게임플레이 씬(`defaultScene`, 기본 `Safehouse`)을 additive 로드 |
| **게임플레이 씬**(Safehouse/Zone1 등) | `SystemsScene.EnsureLoaded`가 Systems를 additive로 끌어와 매니저/플레이어/UI 공급. ⚠️ 동작은 하지만 **개발·테스트는 항상 Systems에서 시작**(단독 Play 금지 — 사용자 규칙) |
| **맵툴 씬**(MapTool*) | 아무것도 안 함(자체 완결) |
| **Systems 빌드세팅에 없음**(빌더 미실행) | Systems 안 끌어옴 → 기존 코드 스폰 폴백 동작 |

### 씬 전환 (SceneTransitionManager)
`SystemsScene.Available`일 때 `TransitionTo/WithDelay`는:
1. 현재 게임플레이 씬 기억 → 2. 새 씬 **additive 로드** → 3. 새 씬을 `SetActiveScene` →
4. 이전 게임플레이 씬만 언로드. **Systems는 절대 언로드하지 않는다.**
Systems 미빌드 시엔 기존 단일(Single) 로드 + DontDestroyOnLoad로 폴백.

### 코드 스폰 폴백과의 양립
기존 자동 부트스트랩(`GameBootstrap`, `UIManager.Bootstrap`, `TopDownPlayer.Bootstrap`,
`SceneTransitionManager.Bootstrap`)은 맨 앞에 `if (SystemsScene.ProvidesSystems) return;`
가드를 둬서, **Systems가 공급하면 건너뛰고 / 없으면(또는 맵툴) 기존대로 코드 스폰**한다.
→ Systems 씬을 만들기 전에도 모든 씬이 그대로 동작(점진 도입 가능).

### 영속성 메모
- 매니저 / UIManager / PlayerRig는 각자 Awake에서 `DontDestroyOnLoad`를 호출하므로
  런타임엔 **DontDestroyOnLoad 씬**으로 이동한다(=Systems 비워짐). 기능상 동일하게 영속.
- 글로벌 조명 / DayNightCycle / SystemsSceneEnforcer / GameBoot는 DDOL 안 함 →
  Systems 씬에 상주하며, Systems가 언로드되지 않으므로 모든 additive 게임플레이 씬을 비춤.
- 게임플레이 씬에 남아 있는 **중복 카메라**는 `CameraFollow.DisableOtherCameras`,
  **중복 Global Light2D**는 `SystemsSceneEnforcer`가 로드 시 비활성화(2D 시절 장치 — 3D에선 잔재, 정리 5단계).

### 파일 맵
| 파일 | 역할 |
|------|------|
| `Scripts/Systems/SystemsScene.cs` | Systems 씬 판별/로드 유틸 + `EnsureLoaded` 부트스트랩 |
| `Scripts/Systems/GameBoot.cs` | Systems 단독 진입 시 기본 게임플레이 씬 로드 (씬 배치) |
| `Scripts/Systems/SystemsSceneEnforcer.cs` | 중복 Global Light2D 비활성화 (씬 배치) — 2D 잔재 |
| `Editor/SystemsSceneBuilder.cs` | `Tools/TopDown/개발/시스템 씬` — Systems.unity 1발 생성 + 빌드세팅 |
| `Interaction/SceneTransitionManager.cs` | additive 교체 로드(Systems 유지)로 변경 |

### 게임플레이 씬 = 맵 콘텐츠 전용
Safehouse(마을) / Hideout / Pawnshop / Zone1 등 게임플레이 씬은 **시스템 오브젝트를 두지 않는다**
(카메라/EventSystem/매니저/플레이어는 전부 Systems가 공급). 게임플레이 씬엔 **맵 콘텐츠만**:
3D 지오메트리, SpawnPoint, 프롭, 인터랙터블, 탈출존, 씬별 마커, 맵 태양.
3D 맵은 빌더가 만든다(`Zone1GreyboxLayout`·`Map3DBuild`·`Safehouse3DLayout`·`Hideout3DLayout` 등).
⚠️ 2D 시절의 `InGameScene`·`CombatSandbox` 씬과 이를 만들던 옛 빌더(`GameSceneBuilder`·`CombatSandboxBuilder`)는 2026-09-12 정리 5단계에서 삭제했다.

### 셋업 방법
1. PlayerRig는 `Assets/Resources/PlayerRig.prefab`으로 이미 있다 — 옛 `Player Rig` 빌더(`TopDownPlayerBuilder`)는 삭제됐으니 새로 만들지 않는다.
2. `Tools/TopDown/개발/시스템 씬` 실행 → `Assets/Scenes/Systems.unity` 생성 + 빌드세팅 등록.
3. 맵 콘텐츠 제작: 3D 맵 빌더(위 목록) 실행. (옛 `Safehouse|InGame Scene` 2D 골격 빌더는 쓰지 않는다)
4. 전체 게임 테스트: **Systems 씬을 열고 Play**(GameBoot이 Safehouse를 엶).
   게임플레이 씬에서 바로 Play해도 Systems가 자동 additive 로드됨.

## 게임 루프 (안전가옥 ↔ 레이드)

세이프하우스 → 맵보드 → 레이드 → 파밍 → 탈출 → 보상 → 세이프하우스.
모든 단계가 이미 코드로 연결돼 있고, 씬엔 '맵 콘텐츠' 오브젝트만 배치하면 동작한다(3D 맵 빌더가 배치).

| 단계 | 트리거 | 코드 |
|------|--------|------|
| 세이프하우스 | 걸어다니는 허브(timeScale=1) | — |
| → 맵보드 | `InteractableObject(MapBoard)` 상호작용 | `UIManager.ShowMapSelect()` → `MapSelectUI` |
| → 레이드 | 지역 선택(`WorldRegionCatalog` — 현재 지역1→`Zone1`만 씬이 있다) | `SceneTransitionManager.TransitionTo(sceneName, spawnId)` |
| 레이드 | `RaidManager`(20분 타이머/사망/시간초과) | 레이드 씬(Zone1)에 배치, Start에서 `PendingResult=true` |
| 파밍 | `InteractableObject(Pickup/Container)` | `PlayerInventory` + `RaidManager.TrackLoot` |
| → 탈출 | `InteractableObject(ExitPoint, exitWaitTime>0)` | `OnExtractSuccess` + `TransitionWithDelay`(거리 이탈 시 취소) |
| → 보상 | 안전가옥 로드 + `RaidManager.PendingResult` | `RaidResultUI` 자동 표시(루트/생존시간/가치) |
| → 세이프하우스 | 정산 닫기 | 루프 완료 |

- **정산 트리거**: `RaidManager.PendingResult`(static)로 "레이드를 실제로 다녀왔는지" 판정 → 부팅 직후 진입에서 정산창 오발 방지.
  additive 로드 순서상 마을 로드 시점엔 레이드 씬이 아직 살아있어 `LootedItems` 캡처 가능.
- **맵 콘텐츠**: 3D 맵 빌더가 스폰·지도판·시설(마을/은신처)과 스폰·RaidManager·루팅·탈출구(레이드)를 배치한다.

## 입력 시스템 (Input System, 2026-06-24 전환)

레거시 **Input Manager → 신 Input System 패키지**로 전환. `activeInputHandler: 1`(New 전용).

- **호환 셰임 `Scripts/Core/GameInput.cs`** — 레거시와 동일 시그니처(`GetKey/GetKeyDown/GetKeyUp(KeyCode)`,
  `GetMouseButton(Down/Up)(int)`, `mousePosition`, `mouseScrollDelta`, `GetAxisRaw("Horizontal"/"Vertical")`)를
  노출하고 내부는 `Keyboard.current`/`Mouse.current` 폴링으로 구현. 디바이스 null 가드 포함.
  KeyCode→Key 매핑이 이 한 곳에 집중됨.
- **규약: 게임플레이/UI 코드는 `UnityEngine.Input`을 직접 쓰지 말고 `GameInput`을 쓴다.**
  New 전용 모드에선 레거시 `Input.*` 호출이 런타임 예외를 던지므로, 새 입력 코드도 반드시 `GameInput` 경유.
  WASD 이동·마우스 조준 등 기존 로직은 폴링 구조 그대로 유지(액션 에셋/콜백으로 재배선하지 않음).
- **UI 입력 모듈**: 코드로 EventSystem 생성 시 `StandaloneInputModule` 대신
  **`UnityEngine.InputSystem.UI.InputSystemUIInputModule`** 를 붙이고 **반드시 `.AssignDefaultActions()` 호출**
  (UIManager/PauseMenu/TitleScreen/HideoutController).
  New 전용에서 레거시 모듈은 UI 입력을 못 받고, 코드로 붙인 신 모듈도 `AssignDefaultActions()` 없이는
  포인터/클릭 액션이 비어 **마우스 클릭이 안 먹는다**(중요 함정).
- `Game.Scripts.asmdef`는 `Unity.InputSystem`을 참조. 패키지: `com.unity.inputsystem`.

## 씬 하이어라키 규약 (2026-09-11)

> 사용자: *"hierarchy 좀 정리해줄래 씬마다 너무 복잡하네 기능별 구조별로 딱딱 나눠줘"*

| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-09-11 | 맵 씬을 어떤 틀로 나눌까 — ①기능별 + 지형만 구조별 ②기능별만 ③구역별 먼저(그 안에 기능별) | **① 기능별 + 지형만 구조별.** 1단계는 컴포넌트로 기능별, 지형(콜라이더+메시뿐인 것)만 2단계로 구조별. ③은 Zone1 빌더(2223줄)를 구역 단위로 뜯어고쳐야 해서 보류. |
| 2026-09-11 | Systems 씬(매니저 27개가 루트에 평평함) — ①폴더로 묶기 + DDOL 호출 수정 ②구분선만 ③제외 | **① 폴더 + DDOL 수정.** Core / World / Progress / Story / UI / Player. |

### 구조

```
[맵 씬 — Zone1 · Safehouse · Hideout · ScrapMarket · Pawnshop]
Map
├─ Environment      지형 — 콜라이더·메시뿐인 것, 컴포넌트 없는 묶음(아트 킷 등)
│   ├─ Ground          바닥·도로면 (Floor/Ground/Road/LotF…)
│   ├─ Structures      건물·블록·벽 (Bldg/Apt/Tower/Wall/W_/Rib/Container…, BuildingInterior 건물 포함)
│   ├─ Roads           도로 차단물·중앙분리대·맵 가장자리 (OB_/MD_/RB_/Edge/JX)
│   ├─ Scatter         흩뿌린 소품 (SP_/SZ_/P_/Dressing/Board)
│   └─ Misc            표에 없는 것
├─ Lighting         Light · Volume · 천장등 묶음 · Sun3D
├─ Gameplay         SpawnPoint · SceneDoor3D · BlockedPassage · QuestPoiZone ·
│                   StoryAreaTrigger · InteractableObject · 빈 위치 표식(RoomCenter 등)
├─ Loot             LootContainer · ItemSpawnPoint · WorldItem
├─ Enemies          SpawnZone · EnemyController · EnemySpawner
├─ NPCs             NPCController
└─ Controllers      MapSpawnController · RaidManager · NavGrid · Hideout* · 이름이 ~Controller/Manager/Director인 비렌더 스크립트

[Systems 씬]
Core      GameBoot · SaveManager · SceneTransitionManager · ScreenEffectManager · SystemsSceneEnforcer
World     DayNightCycle · RaidManager · RaidMapManager · HideoutModuleManager
Progress  Quest · DailyQuest · Achievement · Trait · Reputation · NPCRelationship · Currency · MainStash · PostRaidEvent
Story     StoryLocale · StoryPlayer · StoryTriggerManager · NarrationUI · NoteUI · TutorialPrompt
UI        UIManager · ToastManager
Player    PlayerRig

[Map이 없는 씬 — 룩 체크 등] 같은 판정을 루트 폴더로 + Cameras · Characters
```

승인안에서 두 가지를 바꿨다: 지형 2단계 `Buildings` → **`Structures`**(실내 벽 `W_*`·하이드아웃 `Rib_*`도 들어가야 해서),
**`NPCs` 폴더 추가**(안전가옥 NPC 3명 — NPC가 있는 씬에만 생긴다).

### 규칙
- **분류는 이름이 아니라 컴포넌트로.** 이름 접두사는 뜻이 섞여 있다 — 실측: Zone1의 `SP_`는 소품 188 + 루트 상자 47 +
  스폰 지점 5가 한 접두사다. 이름 표는 **지형 안을 구조별로 나눌 때만** 쓴다.
  판정 순서(먼저 맞는 것): Lighting → Controllers → Enemies → Loot → NPCs → Gameplay → Environment.
  루팅 상자는 `InteractableObject`도 갖고 있어서 Loot가 Gameplay보다 앞이다.
- **도구 하나로, 빌더가 저장 직전에 부른다.** `Editor/SceneHierarchyOrganizer.cs`. 씬 대부분이 빌더로 생성되므로 손으로
  정리하면 다음 재빌드에 날아간다. 연결 지점: `EditorSceneBuildUtil.SaveAndClose`(GreyboxBuild → Zone1·실내 전부,
  Systems 등) + 직접 저장하는 `Safehouse3DLayout`·`Hideout3DLayout`.
  기존 씬은 `Tools ▸ TopDown ▸ 개발 ▸ 하이어라키 정리 (빌드세팅 전체 씬)`.
- 몇 번 돌려도 결과가 같다 — 이미 폴더에 든 것도 다시 판정해 제자리로 보내고 빈 폴더는 지운다. 폴더는 `HierarchyFolder`
  컴포넌트로 표시한다. **`Map` 이름은 유지** — `GreyboxBuild.EndScene`(천장등)·`Prop2DCatalogEditor`가 이름으로 찾는다.
- 옮기지 않는 것: **프리팹 인스턴스 내부**(바깥 루트만 옮김), **싱글톤(정적 `Instance`)이 붙은 떠돌이 루트**(아래 DDOL).
- 새 Systems 매니저를 추가하면 `SceneHierarchyOrganizer.SystemsTable`에 한 줄 더할 것 — 빠지면 `Misc`로 가고 경고가 뜬다.

### ⚠️ DontDestroyOnLoad와 폴더
Unity는 **루트가 아닌 오브젝트엔 DDOL을 조용히 무시**하고 경고만 낸다. Systems 매니저 18개는 원래 루트에 있다가 Awake에서
DDOL 씬으로 옮겨졌는데, 폴더에 넣으면 그게 끊긴다. 그래서:
- 매니저의 `DontDestroyOnLoad(gameObject)` → **`HierarchyFolder.Persist(gameObject)`**. 부모가 영속 폴더
  (`detachOnPersist`, Systems 폴더만 켬)면 루트로 뺀 뒤 DDOL을 건다. **런타임 동작은 폴더가 없던 때와 똑같다.**
  그 외엔 `DontDestroyOnLoad`와 완전히 같다 — UI 패널처럼 다른 부모 밑의 오브젝트는 영향 없음.
- `TopDownPlayer`는 `transform.root`로 PlayerRig를 잡았는데, Player 폴더가 끼면 **폴더째** 중복 제거·DDOL을 하게 된다.
  → **`HierarchyFolder.OwnerRoot(transform)`**(폴더를 건너뛴 소유 루트)로 교체.
- 맵·일반 씬의 싱글톤 루트는 폴더에 넣지 않는다(맵 폴더는 `detachOnPersist`를 끄므로 넣으면 DDOL이 끊긴다).

### ⚠️ 런타임 스폰은 "누구의 씬"인지 넘길 것
`new GameObject()`/`Instantiate`는 **활성 씬**에 생긴다. 그런데 맵을 additive로 로드하면 그 맵의 `Start()`가
`SetActiveScene(맵)`보다 **먼저** 돈다 — 그 순간 활성 씬은 Systems다. 그래서 루팅 아이템이 Systems에 쌓이고
맵을 떠나도 안 지워졌다(2026-09-11 실측: Zone1 한 번에 `WorldItem` 32개 누수).
- `WorldItem.Drop(item, pos, owner)` — 맵 쪽 호출부(ItemSpawnPoint(Fixed 고정 아이템)·MapSpawnController(바닥 루팅)·
  Breakable·EnemyController)는 `this`를 넘긴다. 아이템은 owner의 씬으로 옮겨져 맵과 함께 언로드된다.
- 플레이어·UI(DDOL) 쪽 드롭은 owner 없이 둔다 — 그땐 이미 활성 씬이 현재 맵이다.
- 맵 로드 직후 무언가를 스폰하는 새 코드도 같은 규칙: 스폰한 오브젝트를 **자기 씬으로** 옮길 것.

### 런타임 스폰물 폴더 `[Runtime]` (2026-09-11)
| 날짜 | 질문 | 결정 |
|------|------|------|
| 2026-09-11 | 런타임 스폰된 적(Zone1 38기)이 맵 씬 루트에 평평하게 깔림 | **폴더로 묶는다.** 맵 씬 루트의 `[Runtime]/Enemies` |
| 2026-09-11 | 루팅 아이템(Zone1 약 39개)도 루트에 평평함 — 이어서 묶을까 | **묶는다.** `[Runtime]/Loot` — `WorldItem.Drop`이 게임플레이 씬이면 넣는다(플레이어가 버린 것 포함) |

- `HierarchyFolder.RuntimeFolder(scene, "Enemies")` — 씬 루트에 `[Runtime]`(원점·단위 스케일)과 그 아래 이름 폴더를 찾거나 만든다.
  플레이 중에만 생기고 씬 파일엔 저장되지 않으며, 맵 씬과 함께 언로드된다.
- 에디터 정리 도구가 만드는 `Map/Enemies`(스폰 존)와는 별개다 — `Map`은 저장된 배치물, `[Runtime]`은 플레이 중 생긴 것.
- 현재 폴더: `Enemies`(EnemySpawner), `Loot`(WorldItem.Drop — Systems·맵툴·DDOL 씬엔 안 만든다).
- 새 런타임 스폰도 여기에 이름 폴더를 더해 넣는다. `transform.root`에 기대는 코드는 없다(2026-09-11 확인).

## 변경 로그

| 날짜 | 질문 | 결정 | 근거 |
|------|------|------|------|
| 2026-06-03 | 흩어진 자동 부트스트랩(RuntimeInitializeOnLoadMethod + DontDestroyOnLoad)으로 매니저/플레이어/라이트/카메라/UI가 코드 스폰됨 — 한 곳에서 보고 관리하기 어렵고 상점 등 UI를 씬에서 미리 배치하고 싶음 | **영속 "Systems" 부트 씬 + additive 게임플레이** 채택. Systems에 매니저+UIManager(+모든 UI, 상점 포함)+PlayerRig+글로벌조명+GameBoot 배치. 게임플레이 씬은 additive로 교체 로드(Systems 유지). 기존 부트스트랩은 `SystemsScene.ProvidesSystems` 가드로 폴백 전환. `Editor/SystemsSceneBuilder`로 1발 생성 | 모든 시스템을 한 씬에서 보고 편집/관리(특히 UI를 씬 배치). 씬마다 재생성 없이 한 세트 유지. 폴백 가드로 점진 도입(빌더 전에도 동작). |
| 2026-06-03 | 코드는 `"Safehouse"` 씬을 로드하는데 실제 파일은 `SafehouseScene.unity`(씬 이름 불일치) → `LoadScene("Safehouse")` 및 `scene.name=="Safehouse"`(timeScale 정지) 동작 안 함 | 씬 파일을 **`Safehouse.unity`로 리네임**(meta GUID 보존, git mv). RaidManager/RaidResultUI/StoryTrigger/Debug 등 모든 `"Safehouse"` 참조와 일치 | 귀환 루프(레이드→안전가옥)의 잠재 버그 수정. additive 부트 흐름의 기본 진입 씬 이름과 정합. |
| 2026-06-03 | 핵심 게임 루프(안전가옥→맵보드→레이드→파밍→탈출→보상) 골격 | 루프 시스템은 이미 연결돼 있어, 빈 게임플레이 씬에 `GameSceneBuilder`로 루프 오브젝트(스폰/MapBoard/RaidManager/줍기5/ExitPoint) 자동 배치. `RaidResultUI`는 `RaidManager.PendingResult`(static)로 트리거 — 부팅 직후 오발 방지 | "나갔다 돌아오는 루프 먼저" 원칙. 시스템 재사용 + 씬 콘텐츠만 추가. |
| 2026-06-03 | 안전가옥 timeScale=0이면 Rigidbody2D 이동(FixedUpdate)이 얼어 맵보드까지 못 감 | 안전가옥도 **timeScale=1**(걸어다니는 허브). 낮/밤·지역시계는 `ActiveRegionId=null`+이벤트(T키)로 정지하므로 timeScale과 무관 | 루프 전 단계가 걸어다니는 모델 — 일관성. |
| 2026-06-24 | 레거시 Input Manager 폐기 예정 경고. 신 Input System으로 옮길지/방식 | 신 Input System으로 전환(`activeInputHandler:1`, New 전용). 입력이 전부 폴링 구조라 액션 에셋·콜백 재배선 대신 **호환 셰임 `GameInput`**(Keyboard/Mouse.current 래핑)으로 87개 호출부를 1:1 치환. UI 입력 모듈은 `InputSystemUIInputModule`로 교체. Spine 예제 폴더(레거시 Input 사용, 본편 미참조) 삭제 | 폐기 경고 제거 + 미래 호환. 셰임 방식이 폴링 코드베이스에 위험·diff 최소. |
| 2026-09-11 | 씬 하이어라키가 씬마다 복잡함(Zone1 `Map` 아래 766개 평평, Systems 루트 27개 평평) | **기능별 폴더 규약 + 자동 정리 도구.** 맵 씬은 컴포넌트로 기능별(지형만 이름표로 구조별), Systems는 Core/World/Progress/Story/UI/Player. 빌더 저장 지점에서 자동 적용. DDOL은 `HierarchyFolder.Persist`로(폴더 안이면 루트로 빼고 건다), `TopDownPlayer`는 `OwnerRoot` — 런타임 동작 불변 | 손 정리는 재빌드에 날아간다. 이름 접두사는 뜻이 섞여 있어(SP_ = 소품·상자·스폰) 컴포넌트가 확실하다. §씬 하이어라키 규약 |
| 2026-09-11 | (버그) 레이드 루팅 아이템이 맵이 아니라 Systems 씬에 생겨, 맵을 떠나도 남음 | `WorldItem.Drop`에 owner 인자 — 맵 쪽 호출부는 `this`를 넘겨 자기 씬으로 옮긴다. 검증: Zone1 체류 39개 모두 Zone1, 안전가옥 이동 후 0개 | 맵 `Start()`가 `SetActiveScene` 전에 돈다. §런타임 스폰은 "누구의 씬"인지 넘길 것 |
| 2026-09-11 | 런타임 스폰된 적도 폴더로 묶어 달라 | `EnemySpawner`가 적을 맵 씬 루트의 `[Runtime]/Enemies`에 넣는다(`HierarchyFolder.RuntimeFolder`) | 적 수십 기가 루트에 평평함. §런타임 스폰물 폴더 |
| 2026-09-11 | 루팅 아이템도 폴더로 | `WorldItem.Drop`이 게임플레이 씬의 아이템을 `[Runtime]/Loot`에 넣는다 | 같은 이유. §런타임 스폰물 폴더 |
| 2026-09-11 | (문서 정합) 루팅 정리로 `RegionLootBootstrap` 삭제 · `ItemSpawnPoint` 자체 스폰 제거 | §런타임 스폰은 "누구의 씬인지" 호출부 목록에서 `RegionLootBootstrap` 제거, ItemSpawnPoint = Fixed 고정 아이템만 · 바닥 루팅 = MapSpawnController로 정정 | [region-loot.md §루팅 정리 결정](region-loot.md) |
