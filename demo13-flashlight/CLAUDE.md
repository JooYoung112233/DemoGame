# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 3D isometric action game (Tarkov/Zomboid-style). 3D cube world with 2D billboard sprites (Spine animation). URP pipeline with custom shaders. The core loop: leave safehouse → raid a map → loot → extract → return.

**Engine:** Unity 2022+ with URP (Universal Render Pipeline)
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

### Singletons (DontDestroyOnLoad)
- **PlayerController** — Auto-spawns from `Resources/Player` prefab. Persists across scenes.
- **SceneTransitionManager** — Bootstrap singleton. Handles fade, async scene loading, spawn point routing, extraction countdown with distance-cancel.
- **UIManager** — UI state management, blocks player input when UI is open.

### Player System Stack
PlayerController owns movement (NavMeshAgent + WASD), mouse-facing, flashlight pivot, combat state machine (Idle/LightAttack/HeavyCharge/Dodge/Exhausted), stamina, and sprite billboarding. Components on the same GameObject:
- **PlayerInventory** — Grid-based inventory (5x8, 30kg max). `[RequireComponent(typeof(PlayerController))]`.
- **PlayerMedicalSystem** — 5 body parts, 3 injury types (Bleeding/Fracture/Pain).
- **Health** — HP tracking, damage/heal/death.
- **FlashlightController** — Battery-based, auto-off at daytime via DayNightCycle event.

### Combat
- **EnemyController** — State machine AI (Patrol/Chase/AttackWindup/Attack/Hit/Stunned/Dead). Groggy system for stun-on-max. Spine animation. Uses `unitKey` to load stats from StatDB.
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
- **PostProcessController** — Mood presets (NightCity/DesolateRuin/Anomaly) with day/night variants. Anomaly pulse + glitch effects on transition. **Does NOT apply values on Start()** — preserves editor state. Effects only activate on T-key phase change.
- **BuildingGlow** — Window glow via MaterialPropertyBlock `_GlowIntensity`. Applies immediately on Start based on current phase.
- **InkWorldController**, **NeonSign** — Same pattern: subscribe to phase change, don't override editor state on Start.
- **FlashlightBeam** — Procedural cone mesh, follows pivot yaw only (stays flat).

### Rendering & Shaders
- **Stencil system** — Buildings (CityBuilding.shader, CityWall.shader) write stencil Ref=1. Ground shaders (RuinFloor, RoadFloor) skip where stencil=1. Ground uses ZWrite Off + FallBack Off.
- **IsometricDepthSorter** — Runtime depth sorting for isometric view.
- **SpriteBillboard.shader** — Billboard facing for 2D sprites in 3D world.
- Custom shaders: CityBuilding (glow, height fade), FlashlightBeam (transparent cone), OcclusionOutline, PlayerSprite, SpineLitURP.

### Scene Transition & Extraction
`SceneTransitionManager.TransitionWithDelay()` handles extraction: countdown UI, distance-based cancel if player leaves trigger range. Fade in/out with async scene loading. `SpawnPoint` components mark where player appears after transition.

### Data Layer
- **StatDB** (`Resources/Data/StatDB.asset`) — Central stat database. PlayerStatData + UnitStatData list. Key-based access: `StatDB.Instance.GetUnit("bandit_melee")`.
- **WeatherData** (`Assets/Settings/WeatherData.asset`) — Day/night lighting, fog, rain settings. Used by DayNightCycle and RainController.
- **WorldRegionCatalog** — 7 districts with day/night characteristics.
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

### UI Construction
UI is built procedurally in code (uGUI), not scene-placed. GameHUD, RaidResultUI, MedicalHUD all create their own Canvas in `Awake()`/`BuildUI()`. Reference resolution: 1920x1080.

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

## Design Documentation
All game design decisions are recorded in `docs/` as system-specific markdown files. When a design decision is made, record it immediately in the appropriate file with date, question, and decision. See parent `CLAUDE.md` for full recording rules.

Key docs: `combat.md`, `medical.md`, `inventory.md`, `items.md`, `crafting.md`, `world-map.md`, `safehouse.md`, `rendering.md`, `shader-system.md`, `npc-dialogue.md`, `quest.md`, `post-raid-event.md`, `story.md`, `story-script.md`, `Night_City_System_Draft_v2.md`.
