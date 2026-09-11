# Pawnshop forecourt finish — 2026-09-11

Applied and saved through Unity MCP to Safehouse. Existing gameplay component/collider serialization is preserved. The editor was already stopped when work began; Systems had unrelated unsaved changes and was never saved or closed. After scene saves, a Systems-based validation Play was started and left running.

## Delivered

- One irregular asphalt repair surface using a new imagegen albedo, plus sparse aggregate along its lower edge. Image intent: generate; exact prompt in `GroundPrompt.json`. Native output copied unchanged to `Forecourt_Asphalt.png`, then imported into `Assets/Art/Environments/TownFinish62/Textures` with sRGB/mips, trilinear/aniso4, CompressedHQ, NPOT dimensions retained. Existing normal and packed surface mask remain in use; this is not a newly authored full PBR set.
- Nine upper sidewalk slabs vary gently in value. Twenty-seven old renderers (lower slabs and repetitive slash cracks) are disabled only in the forecourt. Existing colliders remain.
- Six reused props: CardboardStack02, YardChair02, Toolbox02, FoldedTarp02, Bucket02, RopeCoil02. Mapped to persistent GameLit materials. No new interaction/collider scripts.
- One warm porch point light: intensity 2.4, range 4.2, no shadows. Kept on as a local practical light; it does not change the global day/night system.
- Saved reusable prefab `Assets/Art/Environments/TownFinish62/Prefabs/Pawnshop_Forecourt62.prefab` and placed scene group `Map/Pawnshop_Forecourt62`.

## Review

`Before_Forecourt_*` and `After_Forecourt_*` use the same temporary 62° camera, main camera postprocessing and preset day/night lighting. The scripts restore all light/camera/roof state. Night comparison does not simulate player movement or the player's wearable light.

The first placement revealed incorrect FBX root rotation and partial underground props. Material mapping and axis composition were corrected, then all prop bounds were grounded at y=0.052m. `RefinedPlacement.json` contains final dimensions. `Placement.json` documents the initial placement and invariant checks, not final prop rotations.

`Runtime_Forecourt.png` is the saved art on actual Play objects rendered with a temporary inspection camera. The player's camera/position were not moved. Runtime/editor validation: 15 GameLit materials valid, missing scripts 0, no new colliders, no review-camera leaks, central 3.4m visual approach corridor clear. Console returned 0 errors after Play. This is not a full navigation or shopping interaction test.

The result improves the uniform grid and adds a few practical objects while leaving the entrance readable. The forecourt's side boundaries and broader town ground remain visible as unfinished adjacent areas. No claim of whole-town completion is made.

## Reproduction / handoff

Assets and scene are already saved; do not rerun placement on the completed scene. `tools/build.cs` followed by `tools/refine.cs` records the actual construction/correction sequence from the pre-pass scene; `capture_after.cs` and `capture_runtime.cs` are repeatable review tools. `recover.json` was used only to discard this pass's failed unsaved first attempt; do not run it on a completed/edited scene. The ignored `BeforeFiles` directory is a local backup, not a runtime dependency.

Read `demo13-flashlight/docs/dev-handoff.md` first on the home PC. Earlier art deliveries in this branch are integrated with current main: GameLit masks, SSAO, authored materials, character/hideout/town work retained; main's updated night palette, HDR/40m shadows, worn-lamp fixes and system cleanup retained. Current-scene playability issues recorded in earlier reviews remain separate follow-up work.
