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
 ├─ PlayerRig (Player + Main Camera + Ambient/Cone Light2D + 후처리 Volume)
 ├─ Global Light 2D (Dark) + DayNightCycle + SystemsSceneEnforcer
 └─ GameBoot
        └─ additive ─┐
   [Safehouse] / [InGameScene] / [CombatSandbox]  ← 여기만 교체 로드
     (맵 / 프롭 / 스폰포인트 / 인터랙터블만)
```

### 진입 시나리오
| 시작 씬 | 동작 |
|---------|------|
| **Systems** | `GameBoot`이 기본 게임플레이 씬(`defaultScene`, 기본 `Safehouse`)을 additive 로드 |
| **게임플레이 씬**(Safehouse/InGameScene 등) | `SystemsScene.EnsureLoaded`가 Systems를 additive로 끌어와 매니저/플레이어/UI 공급 |
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
  **중복 Global Light2D**는 `SystemsSceneEnforcer`가 로드 시 비활성화.

### 파일 맵
| 파일 | 역할 |
|------|------|
| `Scripts/Systems/SystemsScene.cs` | Systems 씬 판별/로드 유틸 + `EnsureLoaded` 부트스트랩 |
| `Scripts/Systems/GameBoot.cs` | Systems 단독 진입 시 기본 게임플레이 씬 로드 (씬 배치) |
| `Scripts/Systems/SystemsSceneEnforcer.cs` | 중복 Global Light2D 비활성화 (씬 배치) |
| `Editor/SystemsSceneBuilder.cs` | `Tools/TopDown/Build/Systems Scene` — Systems.unity 1발 생성 + 빌드세팅 |
| `Interaction/SceneTransitionManager.cs` | additive 교체 로드(Systems 유지)로 변경 |

### 게임플레이 씬 = 맵 콘텐츠 전용
Safehouse / InGameScene / CombatSandbox 등 게임플레이 씬은 **시스템 오브젝트를 두지 않는다**
(카메라/조명/EventSystem/매니저/플레이어는 전부 Systems가 공급). 게임플레이 씬엔 **맵 콘텐츠만**:
Grid/Tilemap, SpawnPoint, 프롭, 인터랙터블, 탈출존, 씬별 마커.
`Tools/TopDown/Build/InGame|Safehouse Scene`(GameSceneBuilder)는 이제 EventSystem/Global Light를
만들지 않고 맵 콘텐츠 골격만 생성한다. (2026-06-03 기준 Safehouse/InGameScene은 비어 있어 맵 제작 필요)

### 셋업 방법
1. (PlayerRig 없으면) `Tools/TopDown/Build/Player Rig` 먼저 실행.
2. `Tools/TopDown/Build/Systems Scene` 실행 → `Assets/Scenes/Systems.unity` 생성 + 빌드세팅 등록.
3. 맵 콘텐츠 제작: `Tools/TopDown/Build/Safehouse|InGame Scene`로 골격 생성 후 Tilemap/스폰 배치.
4. 전체 게임 테스트: **Systems 씬을 열고 Play**(GameBoot이 Safehouse를 엶).
   게임플레이 씬에서 바로 Play해도 Systems가 자동 additive 로드됨.

## 게임 루프 (안전가옥 ↔ 레이드)

세이프하우스 → 맵보드 → 레이드 → 파밍 → 탈출 → 보상 → 세이프하우스.
모든 단계가 이미 코드로 연결돼 있고, 씬엔 '맵 콘텐츠' 오브젝트만 배치하면 동작한다(`GameSceneBuilder`가 생성).

| 단계 | 트리거 | 코드 |
|------|--------|------|
| 세이프하우스 | 걸어다니는 허브(timeScale=1) | — |
| → 맵보드 | `InteractableObject(MapBoard)` 상호작용 | `UIManager.ShowMapSelect()` → `MapSelectUI` |
| → 레이드 | 지역 선택(현재 scrap_market→InGameScene) | `SceneTransitionManager.TransitionTo(sceneName, spawnId)` |
| 레이드 | `RaidManager`(타이머/사망/시간초과) | InGameScene 배치, Start에서 `PendingResult=true` |
| 파밍 | `InteractableObject(Pickup/Container)` | `PlayerInventory` + `RaidManager.TrackLoot` |
| → 탈출 | `InteractableObject(ExitPoint, exitWaitTime>0)` | `OnExtractSuccess` + `TransitionWithDelay`(거리 이탈 시 취소) |
| → 보상 | 안전가옥 로드 + `RaidManager.PendingResult` | `RaidResultUI` 자동 표시(루트/생존시간/가치) |
| → 세이프하우스 | 정산 닫기 | 루프 완료 |

- **정산 트리거**: `RaidManager.PendingResult`(static)로 "레이드를 실제로 다녀왔는지" 판정 → 부팅 직후 진입에서 정산창 오발 방지.
  additive 로드 순서상 안전가옥 로드 시점엔 InGameScene이 아직 살아있어 `LootedItems` 캡처 가능.
- **맵 콘텐츠**: `Tools/TopDown/Build/Safehouse|InGame Scene`가 스폰/MapBoard/Bed/Workbench(안전가옥),
  스폰/RaidManager/줍기5/ExitPoint(인게임)를 배치. 타일맵 바닥/벽 아트는 이후 직접 그림(현재 마커는 빌트인 스프라이트 플레이스홀더).

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
  (UIManager/PauseMenu/TitleScreen/HideoutController + Editor/CombatSandboxBuilder).
  New 전용에서 레거시 모듈은 UI 입력을 못 받고, 코드로 붙인 신 모듈도 `AssignDefaultActions()` 없이는
  포인터/클릭 액션이 비어 **마우스 클릭이 안 먹는다**(중요 함정).
- `Game.Scripts.asmdef`는 `Unity.InputSystem`을 참조. 패키지: `com.unity.inputsystem`.

## 변경 로그

| 날짜 | 질문 | 결정 | 근거 |
|------|------|------|------|
| 2026-06-03 | 흩어진 자동 부트스트랩(RuntimeInitializeOnLoadMethod + DontDestroyOnLoad)으로 매니저/플레이어/라이트/카메라/UI가 코드 스폰됨 — 한 곳에서 보고 관리하기 어렵고 상점 등 UI를 씬에서 미리 배치하고 싶음 | **영속 "Systems" 부트 씬 + additive 게임플레이** 채택. Systems에 매니저+UIManager(+모든 UI, 상점 포함)+PlayerRig+글로벌조명+GameBoot 배치. 게임플레이 씬은 additive로 교체 로드(Systems 유지). 기존 부트스트랩은 `SystemsScene.ProvidesSystems` 가드로 폴백 전환. `Editor/SystemsSceneBuilder`로 1발 생성 | 모든 시스템을 한 씬에서 보고 편집/관리(특히 UI를 씬 배치). 씬마다 재생성 없이 한 세트 유지. 폴백 가드로 점진 도입(빌더 전에도 동작). |
| 2026-06-03 | 코드는 `"Safehouse"` 씬을 로드하는데 실제 파일은 `SafehouseScene.unity`(씬 이름 불일치) → `LoadScene("Safehouse")` 및 `scene.name=="Safehouse"`(timeScale 정지) 동작 안 함 | 씬 파일을 **`Safehouse.unity`로 리네임**(meta GUID 보존, git mv). RaidManager/RaidResultUI/StoryTrigger/Debug 등 모든 `"Safehouse"` 참조와 일치 | 귀환 루프(레이드→안전가옥)의 잠재 버그 수정. additive 부트 흐름의 기본 진입 씬 이름과 정합. |
| 2026-06-03 | 핵심 게임 루프(안전가옥→맵보드→레이드→파밍→탈출→보상) 골격 | 루프 시스템은 이미 연결돼 있어, 빈 게임플레이 씬에 `GameSceneBuilder`로 루프 오브젝트(스폰/MapBoard/RaidManager/줍기5/ExitPoint) 자동 배치. `RaidResultUI`는 `RaidManager.PendingResult`(static)로 트리거 — 부팅 직후 오발 방지 | "나갔다 돌아오는 루프 먼저" 원칙. 시스템 재사용 + 씬 콘텐츠만 추가. |
| 2026-06-03 | 안전가옥 timeScale=0이면 Rigidbody2D 이동(FixedUpdate)이 얼어 맵보드까지 못 감 | 안전가옥도 **timeScale=1**(걸어다니는 허브). 낮/밤·지역시계는 `ActiveRegionId=null`+이벤트(T키)로 정지하므로 timeScale과 무관 | 루프 전 단계가 걸어다니는 모델 — 일관성. |
| 2026-06-24 | 레거시 Input Manager 폐기 예정 경고. 신 Input System으로 옮길지/방식 | 신 Input System으로 전환(`activeInputHandler:1`, New 전용). 입력이 전부 폴링 구조라 액션 에셋·콜백 재배선 대신 **호환 셰임 `GameInput`**(Keyboard/Mouse.current 래핑)으로 87개 호출부를 1:1 치환. UI 입력 모듈은 `InputSystemUIInputModule`로 교체. Spine 예제 폴더(레거시 Input 사용, 본편 미참조) 삭제 | 폐기 경고 제거 + 미래 호환. 셰임 방식이 폴링 코드베이스에 위험·diff 최소. |
