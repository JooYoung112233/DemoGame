# Town atmosphere continuation — 2026-09-11

Continues the user's request to establish the atmosphere after the previous art finish. All scene, asset, and Play operations used the official Unity CLI stdio MCP. No model, texture, or GameLit shader source was changed.

## Selected result

- Daylight: intensity .95, color (1,.93,.84), ambient (.29,.32,.35). Pitch 62 / yaw 0 and sun pitch 58 are retained. Night WeatherData, including purple palette and fog, is unchanged.
- Shared PlayerRig volume: Neutral tonemapping retained; contrast 14, saturation -6, exposure +.10 EV, bloom .15, vignette .16. Grain, SSAO and material maps are unchanged.
- Six existing street lights: day 6 / night 18, range 7.5 m, warm color (1,.80,.57).
- Existing porch light and reusable forecourt prefab: day 2.4 / night 4.8, range 5 m. Interior bulb intensity 4 and building-entry rules are unchanged.
- `PropLight3D` synchronizes on enable and `DayNightCycle.OnPhaseChanged`, unsubscribes on disable, and preserves authored intensity when no clock exists. No per-frame polling or Start-time lighting reset.

## Self-review and corrections

The approved HideoutReview/InGame reference informed subdued warm practical lights and worn material separation. Lighting-only and graded candidates were compared. Changing the night palette/fog did not help and was rejected. Porch intensity 7 produced excessive bright door edges, so it was reduced to 4.8. Persisting night strength in daytime washed the warden area yellow; day/night intensity separation corrected that before delivery.

## Evidence

- `Before_*`: pre-change runtime scene objects under controlled day/night lighting at 62 degrees.
- `Saved_*`: final saved assets reloaded through Systems Play and the normal Safehouse transition, under the same controlled lighting/cameras. Cameras are temporary inspection cameras with the actual gameplay URP postprocess settings. Interior comparisons temporarily hide the roof and enable the existing bulb together. These comparisons restore all temporary states; they do not simulate the complete phase event or move the player.
- `Game_Day.png`, `Game_Night.png`: actual Main Camera renders after real gameplay phase events, with the worn lamp. No inspection-camera repositioning. Screen Space Overlay UI is not included.
- `Applied.json`: selected values and existing gameplay/collider serialization invariant check (excludes the newly added phase-light components).
- `Validation.json`: runtime checks for seven lights, day/night/day transition, re-enable during night, prefab instantiation during night, camera/HDR/volume configuration, missing scripts, shader errors and temporary camera leaks.
- `RuntimeDay.json`, `RuntimeNight.json`, `Console.json`: post-restart state and console readback.

This is a self-reviewed atmosphere pass, not user final art approval. The unlit parts of town remain deliberately dark, and the interior bulb remains restrained. Full interaction/raid QA, continuous-motion shadow/SSAO stability, GPU profiling and other scenes using the shared daytime/profile settings remain separate checks. The existing missing `BRB/AnomalyFog` warning repeats during Play and is not resolved by this pass.

## Reproduction

`tools/Review.cs` and `Validate.cs` are run via MCP `run_script`; JSON inputs are included. `Apply.cs` requires stopped Play and a clean Safehouse scene and backs up original files to ignored `BeforeFiles`. Apply after the package has imported `PropLight3D`. Save Safehouse again if Unity's delayed prefab refresh marks it dirty, then restart from Systems and request the normal Safehouse transition. `capture_game_view` writes the Game captures under `Assets/Temp/Atmosphere62` in this Pipeline version; copy those files here and run `cleanup.json` to remove the imported copies. Do not run the old Before capture on the completed state and label it as a historical baseline.
