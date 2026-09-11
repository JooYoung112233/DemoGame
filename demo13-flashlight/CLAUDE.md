# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity **3D** looter RPG-shooter seen from a **straight-on top-down orthographic camera (62° pitch, yaw 0)**. Core loop: village (safe zone) → pick a region → 20-minute raid (loot, gunfights) → extract → settle → back to the village/hideout.

**Direction (2026-09-11):** an RPG like 낙원 — **earn money and loot good items**. Combat has **two primary pillars, Zomboid-style: guns** (aim reticle, recoil, gun/ammo specs) **and melee weapons** (bats, blades…). Unarmed attacks were removed. Dodge is **switched off** until it has an animation (`GameTuning.dodgeEnabled`). Decisions: `docs/combat.md` §무기 구성 결정. Earlier the same month the Tarkov-style hardcore systems were cut back (`docs/scope-cut.md`: body-part medical, noise, grid inventory, dispatch, 41→11 traits).

> **History:** isometric → pure top-down 2D (2026-06) → **full 3D (2026-09-07~09, Stages 0–4 done)**. Plan, measurements and the leftover-2D ledger: `docs/3d-migration.md`. Many class names still say "TopDown"/"2D" for history only. Stage 5 (deleting 2D leftovers) is open — see "Known leftovers" below.

**Engine:** Unity 6.6 (6000.6.0f1), URP **3D** (Forward renderer, `Assets/Settings/URP-3D.asset`)
**Language:** C#
**Assembly:** `Game.Scripts.asmdef` references `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`, `Unity.InputSystem`. QA code is a separate `Game.QA` assembly, compiled only with the `QA_ENABLED` define.

## Always start from the Systems scene
Open `Assets/Scenes/Systems.unity` and press Play. **Never play a gameplay scene standalone** (user rule). The editor is shared by several sessions — another session may stop play mode or have scenes open; commit only your own files.

## Architecture

### Scene Flow
```
Systems (persistent boot scene — managers, all UI, PlayerRig)
  → Safehouse (= the VILLAGE, walkable safe zone) ↔ Hideout (hideout room) · Pawnshop (interior)
    → MapSelectUI (region select)
      → Zone1 (region 1 raid, 20 min) · Int_* (15 building-interior scenes via doors)
        → extract → PostRaidEvent (40%) → RaidResultUI → back to the village
```
Naming trap: the scene called `Safehouse` is the village; `Hideout` is the player's room; docs also say 안전구역/안전가옥/Town. Cleanup step 4 will settle the names. Only region 1 of 5 has a scene (`WorldRegionCatalog`). `ScrapMarket_GB` is an old raid map (debug only), `MapTool_LookDev` a look-dev scene.

### Systems Boot Scene (persistent additive) — 2026-06-03
`Assets/Scenes/Systems.unity` (built by `Tools/TopDown/개발/시스템 씬`) holds **all managers + UIManager (+every UI panel) + PlayerRig (player, camera, worn lamp, post-process Volume) + GameBoot**. Gameplay scenes are loaded **additively on top** and swapped by `SceneTransitionManager` (Systems is never unloaded). Old auto-bootstraps early-return when `SystemsScene.ProvidesSystems`. **See `docs/architecture.md`.**

### Singletons
- **TopDownPlayer** — in `Resources/PlayerRig.prefab`. Persists across scenes. (Name is historical.)
- **SceneTransitionManager** — fade, additive scene loading, spawn routing, extraction countdown with distance-cancel.
- **UIManager** — UI state, blocks player input while UI is open.

### Player
**TopDownPlayer** — movement (Rigidbody + 3D collider, WASD **camera-relative**: `CameraRelative`, inverse `WorldToInput` for the QA bot), **facing = movement direction; mouse only while aiming** (right-click), sprint, and the melee state machine (light/heavy attack, stamina; dodge is gated off by `GameTuning.dodgeEnabled`). The melee code dates from the 2D era and is being cleaned up (cleanup step 2), not removed. Plan-space logic stays `Vector2` (x = world X, y = world Z) and converts at the physics/transform boundary via **`Core/Plan3D`** — so public APIs like `FacingDirection` stayed 2D.
- **PlayerGun** — player firearms: cursor reticle (`AimReticle`, shown only while aiming), aim camera, recoil/spread, ammo specs. `PlayerFirearmVisual` for the model.
- **ChibiPlayerVisual** — 3D low-poly chibi model + Animator.
- **PlayerInventory** — **slot-based** (1 item = 1 slot) + weight limit since 2026-09-09. Class/API names (`InventoryGrid`, `gridX/gridY`, `rotated`) are kept for save compatibility. See `docs/inventory.md`.
- **Health** — single HP; healing items restore HP (body-part medical removed 2026-09-09, `docs/medical.md`).
- **WornLamp** — the lamp worn on the body: forward beam (Zomboid-style) + short spill + faint body glow.

### Combat
- **EnemyController** — 3D state-machine AI (melee and firearm bandits; firearm bandits aim-warn before firing dodgeable bullets). Pathfinding is a custom grid A* (`NavGrid`/`NavAgent`, not NavMesh). Uses `unitKey` → StatDB.
- **StatDB** (`Resources/Data/StatDB.asset`) — central stats. `StatDB.Instance.GetUnit(key)`, `StatDB.Instance.playerStat`.
- **Death loot (PUBG-style)** — `BanditRagdoll` + `CorpseMarker` (rarity beam) + `LootListUI` list; scrap currency (`scrap_money`) via `ScrapWallet` (cash is quest/event-only).
- A gun is currently described in several places (ItemData, WeaponData, PlayerFirearmSet, StatDB) — cleanup step 2.

### Inventory & Items
- **ItemData** (ScriptableObject) — categories Weapon/Medical/Consumable/Material/Valuable/Key/Misc, rarity, stacking. (`gridWidth/Height` are hidden and unread.)
- **RecipeData** (`Resources/Data/Recipes/`), **CraftingSystem** — recipes, crafting, weapon repair. `docs/crafting.md`.
- **LootContainer**, **WorldItem** (drops go under `[Runtime]/Loot`), **GroundPickupUI**.
- **Loot (2026-09-11, `docs/region-loot.md` §루팅 정리 결정)**: `MapSpawnController` decides **how many & where** (budget = rolls; scrap sources — registers/safes/stalls — always filled). **What** comes only from `RegionLootCatalog`: `Resources/region_loot.txt` (region × time) + `Resources/loot_tables.txt` (region × container kind: junk/trunk/stall/crate/register/safe/ground/corpse/int_*). `LootContainer.lootKind` picks the table; `ItemSpawnPoint` is an anchor only. Scrap currency only from stall/register/safe/corpses.
- **Loot UI**: field containers & corpses open `LootListUI` (search reveal, then take all / one by one, no value shown); `GroundPickupUI` for ground piles; storage uses the character panel. Recording goes through `LootTake`.

### Lighting & Atmosphere
- **DayNightCycle** — day/night phase (`isNight`, `OnPhaseChanged`, `SetNight(bool)`); colors/intensities from **`Resources/Data/WeatherData.asset`** (day = warm orange, night/"evening" = dim purple). **SunLight** = the map's sun.
- **PlayerVision** — enemies outside the facing cone are hidden (3D line of sight).
- **DenseAnomalyController/Zone** — anomaly fog; its shader is missing (see leftovers).

### Rendering
- **Pipeline:** URP 3D, Forward. **Camera:** orthographic, `CameraFollow.viewPitch/viewYaw` = **62/0** (also saved on the PlayerRig prefab and mirrored in `LookDevScene` constants).
- **Shader:** one game shader **`BRB/GameLit`** (characters + environment, dark realistic look). Convert materials with the `GameLitConverter` editor tool. Post-processing lives on the PlayerRig Volume (color grading/tonemapping, bloom, vignette, film grain).
- **Occlusion:** screen-space cutaway (decided in Stage 0).
- SSOT: **`docs/rendering.md`** (top table = current 3D implementation; the URP 2D body below it is history).

### Scene Transition & Extraction
`SceneTransitionManager.TransitionWithDelay()` handles extraction: countdown UI, distance-based cancel. 3D doors use `SceneDoor3D`; `BuildingEntrance` is the 2D-era door still used in places. `SpawnPoint` marks arrival points.

### Data Layer
- **StatDB** — central stats (above).
- **GameTuning** (`Resources/Data/GameTuning.asset`) — all balance values, edited via the Control Panel. `raidDuration` = 1200 s. Index: `docs/balance.md`.
- **WeatherData** — `Resources/Data/WeatherData.asset` (above).
- **WorldRegionCatalog** — 5 districts; only region 1 has a scene. **RegionTimeManager** — per-region day/night clocks.

### Known leftovers (don't build on them)
- 2D physics/lights still in ~16 runtime files (`HideoutController`, `BuildingEntrance`, `Prop2D*`, `MapTriggerZone2D`, …) and 2D-era editor tools.
- `FlashlightController` is looked up by a few scripts but exists on no prefab/scene.
- Code loads 5 shaders that no longer exist: `BRB/AnomalyFog`, `VisionDarkness`, `SpriteFlash`, `DamageOverlay`, `WallPixel`.
- The full ranked list and cleanup order: `docs/dev-roadmap.md` §시스템 정리.

## Key Patterns

### Editor State Preservation
Many runtime scripts intentionally **do not** apply values in `Start()`, to preserve what was set in the Editor. Effects change on events (day/night phase change, triggers). Follow this for new lighting/atmosphere scripts.

### Event-Driven Phase System
```csharp
void Start() {
    dayNight = FindFirstObjectByType<DayNightCycle>();
    if (dayNight != null) dayNight.OnPhaseChanged += OnPhaseChanged;
}
void OnPhaseChanged(bool isNight) { /* react */ }
void OnDestroy() { if (dayNight != null) dayNight.OnPhaseChanged -= OnPhaseChanged; }
```

### Prefab values beat C# defaults
Serialized values on `PlayerRig.prefab` (and scene instances) override field initializers — and the editor can restore old in-memory values across a domain reload. When changing a default that matters, **write it into the prefab explicitly** (e.g. via `PrefabUtility.LoadPrefabContents` + `SerializedObject`), don't hand-edit prefab YAML.

### UI Construction (prefab-baked, 2026-06~)
UI moved from pure-code generation to **prefab-baked + Instantiate.** SSOT: **`docs/ui-prefab-plan.md`**. Reference resolution 1920x1080.
- **Bake:** each panel keeps its uGUI builder and exposes `EditorBake()`. `Editor/UI/UIPrefabBaker.cs` (`Tools/TopDown/UI/프리팹 베이크/*`) saves **`Assets/Resources/UI/<TypeName>.prefab`**.
- **Instantiate:** A-type (lazy self-boot) `Ensure()` loads `Resources/UI/<Type>` and falls back to code generation if missing. B-type (Systems-scene placed): `SystemsSceneBuilder.InstantiateUIOrComponent` (re-bake → rebuild Systems scene).
- **View binding = `[SerializeField]`** set during bake, not find-by-name. Dynamic content stays procedural under a serialized container.
- Re-bind in `Awake()`: `button.onClick` via **`WireEvents()`**, dynamic OS fonts via **`ApplyFonts()`**. A new serialized button ref needs a re-bake.
- Code-created EventSystem: `InputSystemUIInputModule` + **`.AssignDefaultActions()`**.

### Input (new Input System, 2026-06-24)
New Input System only (`activeInputHandler:1`). **Never call `UnityEngine.Input.*`** — it throws. Use **`Scripts/Core/GameInput.cs`** (`GetKeyDown(KeyCode)`, `mousePosition`, `GetAxisRaw`, gamepad merge, virtual input for QA). Mapping SSOT: `docs/controls.md`.

### Scene Hierarchy (2026-09-11)
Scenes are grouped into **function folders** by `Editor/SceneHierarchyOrganizer.cs` — map scenes: `Map/{Environment/{Ground,Structures,Roads,Scatter,Misc}, Lighting, Gameplay, Loot, Enemies, NPCs, Controllers}`; Systems: `Core/World/Progress/Story/UI/Player`. Classification is by **component**. Every scene builder calls it before saving; existing scenes: `Tools/TopDown/개발/하이어라키 정리`. Folders carry a `HierarchyFolder` marker. **Use `HierarchyFolder.Persist(gameObject)` instead of `DontDestroyOnLoad`** for Systems-scene objects, and `HierarchyFolder.OwnerRoot(transform)` instead of `transform.root`. New Systems manager → add a row to `SystemsTable`. **Runtime spawns** go under `[Runtime]/<Name>` via `HierarchyFolder.RuntimeFolder(scene, "Enemies")`; objects spawned during a map's `Start()` must be moved into that map's scene (e.g. `WorldItem.Drop(item, pos, this)`). See `docs/architecture.md` §씬 하이어라키 규약.

### NPC & Quest System
- **NPCData** (SO), **NPCRelationshipManager** (3-axis affinity), **NPCController**, **DialogueUI**.
- **QuestData** (SO, Collect/Kill/Explore/Deliver), **QuestManager**, **QuestHUD**. Two daily-quest sources exist (`DailyQuestManager`, `QuestBoard`) — to be merged.

### Story System
- **StoryData** (JSON: StoryScript → StoryScene → StoryNode), **StoryLocale** (`Resources/Story/Locale/{lang}.json`), **StoryPlayer** (`Resources/Story/Scripts/*.json`), **StoryTriggerManager** (event → flag → auto-trigger), **StoryAreaTrigger**.
- **GameStartHandler** — self-bootstrapping, once per session: loads the save or plays the prologue.
- **NarrationUI**, **TutorialPrompt**, **ScreenEffectManager**.
- Story content predates the 3D/RPG direction and needs re-scoping.

### Post-Raid Event System
**PostRaidEventData** (SO), **PostRaidEventManager** (40% after extraction, weighted), **PostRaidEventUI**. Flow: Extract → village load → PostRaidEvent (if triggered) → RaidResultUI.

## Where we are
- 3D migration: Stages 0–4 done, Stage 5 open — `docs/3d-migration.md`.
- Pending PRs and the current step: **`docs/dev-handoff.md`** (read it first when resuming).
- System cleanup plan (6 steps): `docs/dev-roadmap.md` §시스템 정리.

## Feature-Add Protocol (기능 추가 규칙)
**Every new feature/content passes one gate: "open it, close it in the same session."** A feature = code (5 runtime conventions) + values (GameTuning) + design record (one SSOT doc) + index (MASTER), all updated as one set. Full gate + DONE checklist: **`docs/dev-protocol.md`**.

## Design Documentation
All design decisions are recorded in `docs/` as system-specific markdown with date, question and decision (see parent `CLAUDE.md`). Start with **`docs/MASTER.md`** (index). Structure/tech: `architecture.md`, `controls.md`, `ui-prefab-plan.md`, `save.md`, `rendering.md`, `3d-migration.md`. Systems: `combat.md`, `bandit-firearms.md`, `traits.md`, `medical.md`, `survival.md`, `inventory.md`, `items.md`, `crafting.md`, `economy.md`, `region-loot.md`, `raid.md`, `world-map.md`, `safehouse.md`, `hideout-3d.md`, `npc-dialogue.md`, `quest.md`, `post-raid-event.md`, `story.md`, `scope-cut.md`. Incomplete/TODO items: `dev-roadmap.md`.
