# Hideout02 reference art pass — 2026-09-11

- Primary reference: `demo13-flashlight/Assets/GPT/안전구역/주인공집/ChatGPT Image 2026년 6월 10일 오후 06_13_26.png`.
- `Before.png`: saved Hideout scene's Safehouse01 kit before this art pass, rendered with Unity PreviewRenderUtility.
- `After.png`, `QuarterView.png`, `Detail.png`: actual Hideout02 prefab meshes and URP Lit materials; regenerate using `demo13-flashlight/tools/render_hideout02.cs`.
- Following the user's quarter-view correction, `After.png` uses the gameplay-relative 55° pitch / 145° yaw (room yaw 215°, gameplay camera yaw 0°). The earlier frontal review is retained as `FirstPassFront.png`.
- `MaterialBefore.png` and `MaterialBeforeDetail.png` preserve the material baseline. Compare against `After.png` and `Detail.png` with the same camera and lights: calmer wood grain, reduced painted-metal blotches/reflection, quieter fabric normals, rounded pillow/mattress and a draped quilt with repaired patch. This revised candidate awaits user review.
- These are model-review renders with review lights, not screenshots of the running UI. Before/after use separately framed rooms and different review-light intensities.
- The saved gameplay scene is `Assets/Scenes/Hideout.unity`. Existing nine facility interactions were copied; standing clearance and surface channels were checked in the saved scene. Runtime UI playtesting remains separate.
- Final import, saved-scene validation and preview rendering used Unity MCP. The user's workflow requires MCP for Unity operations, with no desktop UI automation.
