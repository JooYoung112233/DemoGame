# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity **top-down 2D** action game (Tarkov/Zomboid-style). 2D sprites with near-overhead perspective baked into the art (Spine animation). URP **2D Renderer** with 2D lighting (Light2D). The core loop: leave safehouse → raid a map → loot → extract → return.

> **2026-06-02: migrated isometric (2.5D hybrid 3D) → pure top-down 2D.** Camera is 2D Orthographic; world is XY plane with sortingOrder depth; movement is Rigidbody2D + Collider2D; lighting is Light2D; maps are Tilemap + Prop2D. See `docs/topdown-migration.md` and `docs/rendering.md`. Removed: PlayerController, NavMesh, 3D cube walls, stencil shaders, IsometricDepthSorter, custom MapBuilder, 3D spot-light flashlight.

**Engine:** Unity 6 with URP 2D Renderer
**Language:** C#
**Assembly:** `Game.Scripts.asmdef` references `Unity.RenderPipelines.Universal.Runtime` and `Unity.RenderPipelines.Core.Runtime`

## Architecture

### Scene Flow
```
Safehouse (timeScale=0, hub) → MapSelectUI → InGameScene (raid) → Extract → Safehouse + RaidResultUI
```
- **Safehouse** — Safe hub. No combat, time paused. Facilities: bed, storage, workbench, map board.
- **InGameScene** — Active raid map. Combat, looting, day/night cycle, extraction timer.
- **MapScene** — World map (legacy/alternate entry).

### Systems Boot Scene (persistent additive) — 2026-06-03
The canonical runtime layout is a persistent **`Systems` scene** (`Assets/Scenes/Systems.unity`, built by `Tools/TopDown/Build/Systems Scene`) holding **all managers + UIManager(+every UI panel, incl. ShopUI) + PlayerRig (camera/lights/Volume) + global lighting + GameBoot**. Gameplay scenes (Safehouse/InGameScene/CombatSandbox = map/props/spawns only) are loaded **additively on top** and swapped via `SceneTransitionManager` (Systems is never unloaded). The old auto-bootstraps below now early-return when `SystemsScene.ProvidesSystems` (and act as a fallback only when Systems isn't built / in MapTool scenes). **See `docs/architecture.md`.**

### Singletons (DontDestroyOnLoad)
> Placed in the `Systems` scene (above). Auto-bootstrap is a fallback when Systems isn't built.
- **TopDownPlayer** — Lives in the `PlayerRig` (`Resources/PlayerRig.prefab`). Fallback auto-spawns it via `Bootstrap` (RuntimeInitializeOnLoadMethod). Persists across scenes. (Replaces the old `PlayerController`.)
- **SceneTransitionManager** — Fade, **additive** scene loading (keeps Systems, swaps gameplay scene), spawn point routing, extraction countdown with distance-cancel.
- **UIManager** — UI state management, blocks player input when UI is open. UI panels are placed under it in the Systems scene.

### Player System Stack
**TopDownPlayer** owns movement (Rigidbody2D + WASD 8-direction), mouse-facing, sprite flip, sprint, and flashlight pivot rotation (Light2D). Combat state machine / stamina are stubs pending re-port. Game-logic components on the same GameObject (view-agnostic):
- **PlayerInventory** — Multi-container grid inventory: **bag** (size from the equipped Backpack, 0×0 when none) + fixed **pockets** (4×1) + fixed **secure container** (3×3). `MaxWeight` = base 30kg × `TraitManager.Mod("weight_max")` (trait-modified). See `docs/inventory.md`.
- **PlayerMedicalSystem** — 5 body parts, 3 injury types (Bleeding/Fracture/Pain).
- **Health** — HP tracking, damage/heal/death.
- **FlashlightController** — Battery-based, auto-off at daytime via DayNightCycle event.

### Combat
- **EnemyController** — State machine AI (Patrol/Chase/AttackWindup/Attack/Hit/Stunned/Dead). **Rigidbody2D-based movement** (NavMesh removed). Groggy system for stun-on-max. HP/groggy bars are SpriteRenderer-based. Uses `unitKey` to load stats from StatDB.
- **StatDB** (ScriptableObject, `Resources/Data/StatDB.asset`) — Central stat database. `StatDB.Instance.GetUnit(key)` for unit stats, `StatDB.Instance.playerStat` for player stats.
- **PlayerStatData** — Player combat/movement stats (light/heavy attack, dodge, stamina, sprint, crouch).
- **UnitStatData** — Per-unit stats (combat, visual, groggy, movement, detection, AI, rewards). Accessed by string key.

### Inventory & Items
- **ItemData** (ScriptableObject) — Categories: Weapon/Medical/Consumable/Material/Valuable/Key/Misc. Rarity tiers. Grid footprint (1x1 to 3x3). Stacking.
- **RecipeData** (ScriptableObject) — `Resources/Data/Recipes/`. Stations: Workbench/MedicalBench/CookingBench. `unlockedByDefault` or `unlockRecipeItemId`. See `docs/crafting.md`.
- **CraftingSystem** — Recipe unlock, craft, weapon repair. Loads all RecipeData on bootstrap.
- **InventoryGrid** — Pure data, UI-independent. Used by both PlayerInventory and LootContainer.
- **LootContainer** — World loot boxes with own InventoryGrid. `Open()`/`Close()` interface (UI binding TODO).
- **WorldItem** — Dropped items in world with pickup trigger.

### Lighting & Atmosphere
All lighting scripts subscribe to `DayNightCycle.OnPhaseChanged` event:
- **DayNightCycle** — Day/night toggle (T key). `isNight` flag, `OnPhaseChanged` event.
- **PostProcessController** — Mood presets (NeonNight/DesolateRuin/Anomaly) with day/night variants. Anomaly pulse + glitch effects on transition. **Does NOT apply values on Start()** — preserves editor state. Effects only activate on T-key phase change.
- **BuildingGlow** — Window glow via MaterialPropertyBlock `_GlowIntensity`. Applies immediately on Start based on current phase.
- **NeonSign** — Same pattern: subscribe to phase change, don't override editor state on Start.
- **Visibility (FOV):** flashlight-as-primary-light is being **removed** in favor of a Zomboid-style vision cone (enemies hidden outside the player's facing arc; hybrid darkness per region/time). Old flashlight code (`FlashlightController`, `FlashlightBeam` shader, flashlight Light2D) is slated for full removal + a new vision system. See `docs/rendering.md` (가시성) and `docs/combat.md` (타격감).

### Rendering & Shaders (top-down 2D)
- **Pipeline:** URP **2D Renderer**. Camera 2D Orthographic; depth via `sortingOrder` (CameraSortSetup sets TransparencySortMode.CustomAxis `(0,1,0)` — lower Y = front).
- **Lighting:** Light2D — a global light (night ambient) + flashlight point/spot. `ShadowCaster2D` on walls for occlusion.
- **Maps:** Unity Tilemap (floor/walls) + TilemapCollider2D/CompositeCollider2D; props via Prop2D catalog (SpriteRenderer + Collider2D).
- **Shaders (`BRB/` namespace, 8):** Pixelated, PlayerSprite, SpriteSheet, SpriteBillboard, SpineLitURP (Light2D-reactive); ShadowProjector, FlashlightBeam, OcclusionOutline (unlit, partly vestigial). All have a `Universal2D` pass. See `docs/rendering.md`.
- Removed in migration: stencil shaders (City*/Ruin*/Road*), InkCity/* shaders, IsometricDepthSorter, 3D-world billboard setup.

### Scene Transition & Extraction
`SceneTransitionManager.TransitionWithDelay()` handles extraction: countdown UI, distance-based cancel if player leaves trigger range. Fade in/out with async scene loading. `SpawnPoint` components mark where player appears after transition.

### Data Layer
- **StatDB** (`Resources/Data/StatDB.asset`) — Central stat database. PlayerStatData + UnitStatData list. Key-based access: `StatDB.Instance.GetUnit("bandit_melee")`.
- **WeatherData** (`Assets/Settings/WeatherData.asset`) — Day/night lighting, fog, rain settings. Used by DayNightCycle and RainController.
- **WorldRegionCatalog** — 5 districts (7→5 merged 2026-06-11; see docs/story.md §4). Region order = 지역1~5 progression.
- **RegionTimeManager** — Per-region independent day/night cycles (unscaledDeltaTime).
- **RegionLootCatalog/RegionLootTier/RegionLootBootstrap** — Loot distribution per region.

## Key Patterns

### Editor State Preservation
Many runtime scripts intentionally **do not** call ApplyValues/ApplyGlow/etc in `Start()`. This preserves whatever values were set in the Unity Editor. Effects only change on events (DayNightCycle phase change, triggers). When adding new lighting/atmosphere scripts, follow this pattern.

### Event-Driven Phase System
All day/night-reactive components follow the same pattern:
```csharp
void Start() {
    dayNight = FindFirstObjectByType<DayNightCycle>();
    if (dayNight != null)
        dayNight.OnPhaseChanged += OnPhaseChanged;
    // Set target only, don't apply immediately
}
void OnPhaseChanged(bool isNight) { /* react */ }
void OnDestroy() { dayNight.OnPhaseChanged -= OnPhaseChanged; }
```

### UI Construction (prefab-baked, 2026-06~)
UI was **migrated from pure-code procedural generation → prefab-baked + Instantiate.** SSOT: **`docs/ui-prefab-plan.md`** (§4-A all panels converted; §4-B 시안 skin via `UISkin` is deferred). Reference resolution: 1920x1080.
- **Bake:** each panel keeps its uGUI builder but exposes `EditorBake()`. The editor tool **`Editor/UI/UIPrefabBaker.cs`** (`Tools/TopDown/UI/프리팹 베이크/*`, `── 전부 ──`) runs the builder once and `SaveAsPrefabAsset`s to **`Assets/Resources/UI/<TypeName>.prefab`** (26 panels).
- **Instantiate:** bootstrap loads the prefab instead of building. A-type (lazy self-boot, e.g. `ItemDetailUI`): `Ensure()` does `Resources.Load<GameObject>("UI/<Type>")` → Instantiate, **falling back to code generation if the prefab is missing.** B-type (Systems-scene placed): `Editor/SystemsSceneBuilder.cs` `InstantiateUIOrComponent` instantiates the prefab if present, else `AddComponent` (so re-bake → rebuild Systems scene to apply).
- **View binding = `[SerializeField]`** references on the panel script (which IS the view component), set during the bake — **not find-by-name.** Dynamic content (grid cells, list rows) stays procedural under a serialized container transform.
- **Two things are NOT serialized and must be re-bound in `Awake()`:** (1) `button.onClick` listeners → re-attach via **`WireEvents()`** (`RemoveAllListeners()`+`AddListener`) on both prefab and code paths; (2) dynamic OS fonts → re-bind via **`ApplyFonts()`** after Instantiate. Adding a new serialized button ref requires a re-bake.
- When creating an EventSystem in code, use `InputSystemUIInputModule` (not `StandaloneInputModule`) and **call `.AssignDefaultActions()`** on it — without it, pointer/click actions are empty and mouse clicks do nothing.

### Input (new Input System, 2026-06-24)
Project uses the **new Input System** (`activeInputHandler:1`, New-only). **Do NOT call `UnityEngine.Input.*` directly** — it throws at runtime in New-only mode. Use the compat shim **`Scripts/Core/GameInput.cs`** instead (`GameInput.GetKeyDown(KeyCode)`, `GameInput.mousePosition`, `GameInput.GetAxisRaw("Horizontal"/"Vertical")`, etc.). It wraps `Keyboard.current`/`Mouse.current` with the same legacy signatures; add new keys to its `KeyCode→Key` map. See `docs/architecture.md` §입력 시스템.

### Scene Hierarchy (2026-09-11)
Scenes are grouped into **function folders** by `Editor/SceneHierarchyOrganizer.cs` — map scenes: `Map/{Environment/{Ground,Structures,Roads,Scatter,Misc}, Lighting, Gameplay, Loot, Enemies, NPCs, Controllers}`; Systems: `Core/World/Progress/Story/UI/Player`. Classification is by **component**, not name. Every scene builder calls it right before saving (via `EditorSceneBuildUtil.SaveAndClose` or directly), so rebuilds stay organized; existing scenes: `Tools/TopDown/개발/하이어라키 정리`. Folders carry a `HierarchyFolder` marker. **Call `HierarchyFolder.Persist(gameObject)` instead of `DontDestroyOnLoad(gameObject)`** for anything placed in the Systems scene (Unity ignores DDOL on non-root objects), and use `HierarchyFolder.OwnerRoot(transform)` instead of `transform.root`. New Systems manager → add a row to `SystemsTable`. **Runtime spawns** go under `[Runtime]/<Name>` at the map scene root via `HierarchyFolder.RuntimeFolder(scene, "Enemies")` (EnemySpawner → `Enemies`, WorldItem.Drop → `Loot`); objects spawned during a map's `Start()` must be moved into that map's scene (it isn't active yet — e.g. `WorldItem.Drop(item, pos, this)`). See `docs/architecture.md` §씬 하이어라키 규약.

### NPC & Quest System
- **NPCData** (ScriptableObject) — NPC identity, dialogues (state-based + event branching), available quests.
- **NPCRelationshipManager** (Singleton, DontDestroyOnLoad) — 3-axis affinity (Affinity/Trust/Fear) per NPC.
- **NPCController** — Component on InteractableObject(NPC). Triggers DialogueUI.
- **DialogueUI** — Bottom dialogue panel with typing effect, choices, quest offer/report integration.
- **QuestData** (ScriptableObject) — Quest definitions (Collect/Kill/Explore/Deliver).
- **QuestManager** (Singleton, DontDestroyOnLoad) — Tracks active/completed quests, objective progress.
- **QuestHUD** — Right-side active quest tracker + notification popup.

### Story System
- **StoryData** — JSON serialization classes: StoryScript → StoryScene → StoryNode. Node types: narration, dialogue, choice, tutorial, effect, system, condition.
- **StoryLocale** (Singleton) — Multi-language text loader. `Resources/Story/Locale/{lang}.json`. Key-value pairs.
- **StoryPlayer** (Singleton) — Scene playback orchestrator. Loads `Resources/Story/Scripts/*.json`. `PlayScene(id)`, `CheckAutoTriggers()`.
- **StoryTriggerManager** (Singleton) — Game event → flag → auto-trigger wiring. Subscribes to SceneManager.sceneLoaded. Handles: prologue, NPC story scenes, bed rest, first loot/combat, rudi pickup, extraction, night gate, basement entry.
- **StoryAreaTrigger** — Generic trigger zone component. Types: Basement, CustomFlag, PlayScene.
- **GameStartHandler** — Scene component for game start. Loads save or plays prologue.
- **NarrationUI** (Singleton) — Internal monologue display. Italic, semi-transparent.
- **TutorialPrompt** (Singleton) — Contextual hints with one-shot tracking.
- **ScreenEffectManager** (Singleton) — FadeIn/Out, ChromaticPulse, ScreenShake, FreezeFrame, WhiteFlash.
- Flow: Game events → StoryTriggerManager.SetFlag → StoryPlayer.CheckAutoTriggers → auto-play matching scene.

### Post-Raid Event System
- **PostRaidEventData** (ScriptableObject) — Random event definitions with choices, rewards, penalties.
- **PostRaidEventManager** (Singleton, DontDestroyOnLoad) — 40% chance trigger after extraction, weighted random selection.
- **PostRaidEventUI** — Full-screen event popup between extraction and RaidResultUI.
- Flow: Extract → Safehouse load → PostRaidEvent (if triggered) → RaidResultUI.

## Development Roadmap
Current state: Stage 2 (Safehouse container map). See `docs/dev-roadmap.md` for full 10-stage plan. Core principle: "complete the go-out-and-return loop first, combat later."

## Feature-Add Protocol (기능 추가 규칙)
**Every new feature/content passes one gate: "open it, close it in the same session."** A feature = code (5 runtime conventions) + values (GameTuning) + design record (one SSOT doc) + index (MASTER), all updated as one set. Half-done = drift. Full gate + DONE checklist: **`docs/dev-protocol.md`**. This exists because the repo twice needed doc-consistency corrections (index gaps, lore duplication, stale prose) from features that weren't closed out.

## Design Documentation
All game design decisions are recorded in `docs/` as system-specific markdown files. When a design decision is made, record it immediately in the appropriate file with date, question, and decision. See parent `CLAUDE.md` for full recording rules.

Key docs: start with **`docs/MASTER.md`** (index). Structure/tech: **`architecture.md`** (Systems scene + Input System), **`ui-prefab-plan.md`** (UI prefab-bake), `save.md`. GDD master split into `gdd-core.md` / `gdd-progression.md` / `gdd-demo.md`. System docs: `combat.md`, `traits.md`, `medical.md`, `survival.md`, `inventory.md`, `items.md` (+ `items-crafting-farming.md`), `crafting.md`, `economy.md`, `world-map.md`, `safehouse.md`, `rendering.md` (+ `topdown-migration.md` / `topdown-art-spec.md` / `map-tool.md`), `npc-dialogue.md`, `quest.md` (+ `quests-region1.md`), `post-raid-event.md`, `story.md`, `story-script.md`. Incomplete/TODO items tracked in `dev-roadmap.md`.
