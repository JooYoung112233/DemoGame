# Surface detail pilot — 2026-09-11

User approved the next step after the material response pass. This pass edits base-color texture assets using imagegen, imports and applies them with Unity MCP, then reviews the actual running Safehouse. Play was never stopped, and player/main camera, lighting and scene state were not changed.

## Assets and scope

- `Generated/Wood_Base_v2.png`: horizontal grain and small splits; applied to Town_Wood, Town_WoodDark, Pawnshop_Counter.
- `Generated/Steel_Base_v2.png`: restrained brushed grain and scratches; applied to Town_Steel only. Town_Edge also shades tire treads and pavement cracks, so was excluded.
- `Generated/DistrictWarden_BaseColor_v2.png`: atlas edit adding clothing folds/stitching and hair groupings; applied only to DistrictWarden.
- Unity copies: `Assets/Art/SurfaceDetail62/Textures`. Original texture assets remain untouched.
- The five saved materials are listed in `AppliedMaterials.json`. Existing normal/mask maps, shader parameters other than base color, UVs, meshes, rigs, and animations remain unchanged.

## Image-generation provenance

Mode: **edit**. Exact prompts and reference paths are in `Prompts.json`. The original warden atlas and NPCALL7_1 concept lineup informed the character; the hideout interior concept and prior approved HideoutReview/InGame informed environmental surfaces. The original wood and steel maps were supplied as source textures.

Native outputs were copied unchanged into `Generated` and then Unity. Outputs are actually 1254×1254, despite requested dimensions. No Python image editing, resizing or compositing was performed. `ImageAudit.json` contains analysis only. All three are currently uncompressed, sRGB, mipmapped, trilinear/aniso 4; wood/steel repeat and warden clamps. These are review-quality imports, not a completed platform compression pass.

The warden atlas is not pixel-identical outside clothing. Skin-region overlap was approximately 0.983 after normalized-size comparison, mean absolute skin RGB change approximately 7/255. Actual model review found the face/expression and UV placement acceptable at the reviewed view. This does not establish perfect seam continuity from all angles.

## Brightness correction

Generated wood and steel maps were darker than requested. The initial scalar tint correction was rejected. Final RGB multipliers use old/new average **linear** texture values:

- Wood: (1.35569545, 1.39424937, 1.45775068).
- Steel: (1.67287622, 1.69343595, 1.71713072).
- Character: existing tint preserved.

This approximately preserves the prior overall palette while changing surface pattern. Original material backups are local-only in ignored `BeforeFiles`.

## Review and evidence

`Before_*` and `Candidate_*` compare original and selected temporary material clones under the same live lighting. All renderer arrays and roof visibility were restored in `finally`. NPC animation remains running, so successive captures may show minor pose differences.

`Applied_*` were captured after the five actual material assets were saved through MCP. These are **actual runtime scene objects rendered with a temporary 62° camera**, not screenshots from the player's unchanged main-camera position. Main camera URP postprocessing settings were copied. Pawnshop views temporarily hide roof renderers and restore them afterwards. Metal view also shows the nearby container roof, ventilation casing and wood crate.

The final review confirmed readable wood grain, less featureless clothing, restrained steel detail and no obvious new pattern seams in the examined views. This is a pilot, not final approval of every character or the entire town. Large plain ground areas and sparse scene composition still need their own pass. New detail has not been added to normal/roughness maps. Night readability, movement aliasing and platform compression are not validated in this pass.

`Validation.json`: 5/5 material texture assignments, sRGB/mips, preserved normal/mask references, shader errors 0, temporary cameras 0, Play true, Systems + Safehouse, main camera 62°/0°. Unity MCP console query returned total errors 0. No combat/spawn tests were run because those systems were not changed. The previously recorded player-near-origin issue remains outside this art pass.

## Reproduction

`tools/import.cs`, `compare.cs`, `apply.cs`, `capture_applied.cs`, `validate.cs` are eval files for the official Unity MCP wrapper. Import is repeatable. Apply checks for existing assignments and refuses to multiply tint twice. `compare.cs` assumes original materials and is for the pre-apply state; after applying, use `capture_applied.cs` to capture saved materials. All capture scripts restore temporary objects/roof states.

Artifacts and selected material changes are committed locally to the art branch using an isolated index. Remote publication remains blocked by the earlier automatic approval rejection; no retry or bypass is performed here.
