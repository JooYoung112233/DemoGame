# Town interiors — connected in Unity, 2026-09-13

> Follow-up: [InteriorSeams62](../InteriorSeams62/README.md) connects exterior signs to interior visibility and corrects locked-room roof/floor/front-return joins. Apply its correction after the initial Import/Apply/Finish sequence below.

The user found that previously produced interiors were absent from gameplay. `TownInteriorsPartition62` was still an offline package, and the earlier ComicLighting62 review had inspected the old temporary pawnshop interior. This integration closes that gap.

## Saved assets

- 101 existing FBX models imported from the latest partition package; preview cutaways/outlines excluded. Original Blender/source models are untouched.
- 131 model placements in five stores, 15 independent locked-room blockers, exact mesh collision for doors/partitions/furniture, five reusable interior prefabs, and entry-controlled service lighting.
- 50 mapped materials (28 newly externalized, others reused), current Comic B surfaces; 25 authored colors recovered from linear Blender values. Glass stays lightly transparent so display goods remain visible.
- Five building roots retain existing dimensions/positions/colliders/quest flags. East entrances require Unity yaw -90°, correcting the offline manifest's +90° placement.
- Old pawnshop furniture is inactive. The original NPC/interaction remains, moved to local (-1.6,0,-2.2). The market crate and tarp move to z=-2.8 to clear the rear door approach.
- Floor render offset +.025m and rugs +.06m avoid coplanar surfaces. They have no raised floor collider; the player remains on the existing Y=0 ground.
- BuildingInterior hides the exterior shell/roof and obstructing upper lining while inside; locked-room roof covers remain. A collider set prevents a player's hurtbox exit from closing the roof before the body exits.

## Reproduction and evidence

Use the project-pinned Unity MCP wrapper. `Import.cs` → `Apply.cs` → `Finish.cs` in edit mode; Import/Apply create the initial connection, Finish applies the measured floor/aisle corrections. Apply intentionally refuses duplicate scene assemblies. `Applied.json` describes the initial connection; `FinalAudit.json` records final runtime state.

- `Probe.cs`: actual virtual-input walking into/out of all five stores, fixed Y, roof hide/restore, and E interaction opening the pawnshop dialogue. It temporarily removes locked-shop shutters only for testing, then restores them and does not set quest flags. Final `WalkValidation.json`: 5/5 pass.
- `Paths.cs`: actual Unity physics, .3m capsule radius, 1.8m height and .15m grid, flood-filled from each service entrance. Counter-front and three locked-door approach points per store: 20/20 reachable. Door rays: 15/15 blocked. This is a static clearance audit, not NavMesh or full AI gameplay testing.
- `Audit.cs`: 131 mesh instances, 50 materials, no missing UVs/materials or shader diagnostics; all 15 room blockers enabled and four existing store shutters still locked.
- `*_Inside.png`: saved scene rendered by Unity at pitch 62°, yaw 0°. After first review, fixed missing FBX colors, floor/rug overlap and aisle obstructions; reran the same checks. These are world renders, not screen-overlay UI screenshots. Final user art approval remains separate.
- Unity console errors/warnings: 0 on final checks. No new store services, room unlock conditions or save features added.

References: existing latest package preview and the original GPT pawnshop complete interior were opened and compared. The five-room layout/source-reference history remains in `TownInteriorsPartition62/README.md` and `docs/building-interior.md`.
