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

### 셋업 방법
1. (PlayerRig 없으면) `Tools/TopDown/Build/Player Rig` 먼저 실행.
2. `Tools/TopDown/Build/Systems Scene` 실행 → `Assets/Scenes/Systems.unity` 생성 + 빌드세팅 등록.
3. 전체 게임 테스트: **Systems 씬을 열고 Play**(GameBoot이 Safehouse를 엶).
   게임플레이 씬에서 바로 Play해도 Systems가 자동 additive 로드됨.

## 변경 로그

| 날짜 | 질문 | 결정 | 근거 |
|------|------|------|------|
| 2026-06-03 | 흩어진 자동 부트스트랩(RuntimeInitializeOnLoadMethod + DontDestroyOnLoad)으로 매니저/플레이어/라이트/카메라/UI가 코드 스폰됨 — 한 곳에서 보고 관리하기 어렵고 상점 등 UI를 씬에서 미리 배치하고 싶음 | **영속 "Systems" 부트 씬 + additive 게임플레이** 채택. Systems에 매니저+UIManager(+모든 UI, 상점 포함)+PlayerRig+글로벌조명+GameBoot 배치. 게임플레이 씬은 additive로 교체 로드(Systems 유지). 기존 부트스트랩은 `SystemsScene.ProvidesSystems` 가드로 폴백 전환. `Editor/SystemsSceneBuilder`로 1발 생성 | 모든 시스템을 한 씬에서 보고 편집/관리(특히 UI를 씬 배치). 씬마다 재생성 없이 한 세트 유지. 폴백 가드로 점진 도입(빌더 전에도 동작). |
| 2026-06-03 | 코드는 `"Safehouse"` 씬을 로드하는데 실제 파일은 `SafehouseScene.unity`(씬 이름 불일치) → `LoadScene("Safehouse")` 및 `scene.name=="Safehouse"`(timeScale 정지) 동작 안 함 | 씬 파일을 **`Safehouse.unity`로 리네임**(meta GUID 보존, git mv). RaidManager/RaidResultUI/StoryTrigger/Debug 등 모든 `"Safehouse"` 참조와 일치 | 귀환 루프(레이드→안전가옥)의 잠재 버그 수정. additive 부트 흐름의 기본 진입 씬 이름과 정합. |
```
