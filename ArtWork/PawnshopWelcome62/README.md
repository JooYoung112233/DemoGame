# Pawnshop owner visibility — 2026-09-13

The owner was hard to find in the large room and sat behind the left display. Investigation also found a placement bug: the integration scripts treated the NPC capsule's centre pivot as a feet pivot and moved its root from Y=.9 to Y=0. The skinned feet were .9m underground, leaving mainly the head visible.

The saved owner transform now moves from `(32.4,0,41.8)` to `(34,.9,40.1)`: central, 1.7m nearer the entry, in front of the displays. No model, room geometry, camera settings or interaction ranges changed. The original integration Apply/Finish scripts now preserve the corrected position when rerun.

- `Applied.json`: original position and actual skinned feet height before correction.
- `GameplayValidation.json`: real virtual-input walking from outside through the threshold, owner feet near Y=0 / head about 1.75m, all skinned vertices within viewport x .46–.54 / y .70–.85, E dialogue, left/right approach after clearing the door jambs, exit and sign restoration. All pass.
- `PathValidation.json`: current dialogue stand point plus three locked-door approaches remain reachable using the existing .3m radius / 1.8m capsule clearance audit. All three doors remain blocked. This does not implement or validate NavMesh/AI.
- `Before_GameUI.png` and `Entrance_GameUI.png`: actual composed game view with overlay UI, 62-degree camera. Reviewed the face, full body, floor contact and dialogue prompt before sharing. Yellow tint is the existing interaction highlight. Self-review is separate from user approval.

Run `Apply.cs` through the project-pinned Unity MCP in edit mode with clean Safehouse loaded. It preserves model scale/rotation and only changes the NPC's position. Run `Probe.cs` and `Paths.cs` in Play Mode for current validation. Older InteriorIntegration/InteriorSeams capture/probe coordinates reflect their historical owner placement.
