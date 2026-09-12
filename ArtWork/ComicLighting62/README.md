# Comic B lighting review — 2026-09-13

User requested lighting/shaders that fit the approved comic textures. Native Unity renders were reviewed before and after persistence; this is self-review, not final user art approval.

## Saved result

- GameLit and GroundFadeLit default to matte painted diffuse lighting. Texture ink, hue and painted shadows remain; the old procedural grime, desaturation and rim are inactive in painted mode. Normal/AO influence is restrained. URP's own forward lighting/shadows and fog remain; no emissive brightness floor.
- 55 opaque URP Lit materials with WorldComic62/Painted62 base maps converted. See `Applied.json`. Original textures, UVs, animation and collider data are unchanged.
- Weather, town practical lighting and global profile values are recorded in `docs/rendering.md`. `PostProfileBuilder` also creates the saved comic values.
- `Apply.cs` is an edit-mode, project-pinned MCP `run_script`; it persists selected assets and scene lighting. The porch prefab was saved by a separate MCP eval during the initial run and is included explicitly in the repeatable Apply script.

## Evidence

- `Before_*`: previous shaders/lighting. `Candidate_*`: temporary candidate lighting with new shaders, before remaining material conversion. `Saved_*`: persisted values after restarting Play, including the material conversions.
- `Review.cs`, entry `ComicLightingReview.Capture`, args `[0,"Saved"]`: ten native 1280×960 renders at pitch 62°, yaw 0°, five views × day/night. It temporarily switches lighting and roof presentation, then restores all touched runtime render state. These paired renders do not advance region clocks. The interior view is a rendering inspection, not an entry gameplay test.
- `Validate.cs`, entry `ComicLightingValidate.Town`: actual DayNightCycle phase events and re-enable synchronization, seven practicals × two phases; checks saved volume values, shader diagnostics and runtime missing/legacy comic materials. Restores phase, region elapsed (when a timed region exists), and player-light enable state.
- `Night_FlashlightOff/On`: actual WornLamp lighting response at night, rendered synchronously with child Light enable state changed temporarily; does not test battery or controls. The ground pool and boots react while the backpack does not become emissive.
- `Hideout_Saved`: actual hideout camera after scene transition, showing saved material changes with existing warm room lights. Rendered world view excludes screen overlay UI.
- `Validation.json`: pass, 14/14 phase/re-enable checks; no shader messages, missing shaders, remaining town URP Lit comic materials, or active old PostProcessController. Unity reported no console errors after the editor builder recompiled.

Selected saved images are versioned. All local comparison PNGs remain in this folder. Rain, raid-wide gameplay and standalone platform builds were not tested by this pass.
