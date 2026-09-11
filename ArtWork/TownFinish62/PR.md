## Changes

Integrates the accumulated hideout, town exterior/interior, NPC and character art pass, then completes the pawnshop forecourt as the first finished town dressing area. Adds reusable art assets and source files, authored GameLit material response and packed masks, restrained SSAO/shadows, wood/steel/warden surface detail, and home-PC handoff documentation.

The forecourt now has worn repair paving, varied upper sidewalk slabs, six wall-side props and a small porch light. Existing door, NPC and collider configuration is preserved; the central 3.4m approach stays visually clear. Hideout framing accommodates the new model and existing UI.

## Integration and validation

- Integrated latest main (#16–#25); preserved main's night palette, HDR/40m shadow range, worn-lamp fixes and system cleanup while retaining reviewed art/shader settings.
- Unity MCP: 62° day/night comparisons, correction and re-review of FBX axes/grounding, and actual Play capture. No missing scripts, invalid forecourt materials or console/shader errors in the checks.
- All 292 compiled source/shader/assembly/manifest files match the running Unity validation checkout. All 334 Safehouse/Hideout/prefab dependencies and their asset metadata are present in the integration checkout.
- No shared-worktree branch switch or unrelated index changes. Scene and material construction occurred through Unity MCP.

## Remaining work

This is the art delivery and one forecourt finishing area, not completion of every town region or gameplay QA. The previously observed player-near-origin spawn/camera issue, full movement/trading/return loop, platform compression and GPU profiling remain follow-up items. Screenshots in `ArtWork/TownFinish62` use a temporary inspection camera; they do not claim the player's camera was moved to the shop. Start from `docs/dev-handoff.md` on the home PC.
