# Building signs and locked-room joins — 2026-09-13

The user reported exterior building names remaining indoors and misaligned closed-off interior areas across all stores.

## Saved changes

- Bind the five existing `Sign_*` TextMesh objects to their own building's roof visibility list. Existing BuildingInterior enter/exit logic now hides/restores the sign together with the exterior. Add the existing Billboard component so text follows the current 62-degree camera. Keep minimap names and interaction UI.
- The roof rectangles used nominal service-room dimensions, whereas partition centrelines were .175m inward. Actual gaps were .095m behind the rear wall and .17m outside side walls; front returns left .155m gaps. Fit 15 lids, 15 closed-room floors and 15 blockers to the partition centrelines, and extend the inner ends of five front returns. Wall joins now overlap by .08m inside the .16m wall thickness; adjacent lids meet.
- Five derived front-return Mesh assets retain the original FBX and material/UV assignments. Scene and five reusable interior prefabs share the corrected meshes. Doors, hinges, furniture, shop unlock flags and player floor remain unchanged.

## Reproduction

Use `demo13-flashlight/tools/unity_mcp_call.py`, pinned to this project's Editor. Run `Apply.cs` (`SeamApply.Run`) in edit mode after the previous InteriorIntegration62 Import/Apply/Finish sequence. It saves scene/prefabs and refuses to save a dirty scene. `Inspect.cs` records original world text and mesh bounds. No direct YAML editing or computer UI automation was used.

## Verification

- `WalkValidation.json`: virtual-input walking entry/exit, roof and sign hide/restore: **5/5 pass**; pawnshop E dialogue opens. Temporarily hide locked-shop shutters during the probe, then restore their previous state without setting quest flags.
- `PathValidation.json`: actual capsule clearance (.3m radius/1.8m height, .15m grid), **20/20 approach points reachable**, **15/15 doors blocked**.
- `FinalAudit.json`: **35 joins have no positive gap at bounds level**, all 15 blockers align with lids, each building has one sign binding, prefab bounds match the scene, and original four shop locks remain intact.
- Five `*_Inside.png` renders at 62 degrees were visually checked for the corrected seams, door fit and readable service space. `Final_GameUI.png` also includes the actual overlay HUD; the world pawnshop name no longer crosses the player. Self-review is separate from user art approval.
- No Unity gameplay warnings/errors were observed. The initial screenshot request produced one MCP path-validation error because `..` is disallowed; the corrected project-relative request succeeded. No game code failure was involved.

Reference review: opened `Assets/GPT/안전구역/전당포/완전체.png` and the existing five-store original-art contact sheet `ArtWork/TownInteriors62/ReferenceIndex.jpg`. No new prop styling or unlock progression was introduced. These checks do not cover NavMesh, AI traversal or new shop services.
