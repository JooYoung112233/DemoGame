# 2026-09-13 interaction and armory integration

Unity MCP, project `E:/personalProject/Demo/demo13-flashlight`, Unity 6000.6.0f1.

- `Apply.cs`: persist interaction tuning and static UI hints in both the reusable prefab and baked Systems scene copy. Does not rebuild adjacent UI.
- `Icons.cs`: render the existing PistolWeapon and Hero_Bat meshes/materials to transparent inventory icons. No new weapon geometry or reference art was created.
- `Probe.cs`: runtime checks through existing UI handlers/GameInput, physical occlusion, weapon equipment/ownership, stash, quickslots, firing/reload and serialized save round-trip. Test item grids/equipment and input state restored in finally; disk writes suppressed during test.
- `Validation.json`: 26/26 pass. Includes rejected interaction through a temporary physical wall, roofed NPC hiding, real pawnshop dialogue, 1.8m limit, no highlight; primary/secondary equip; quickslot transitions; ammo preservation; full-space failure; reload interrupted by switching; full pockets magazine swap; Ctrl transfers; single starter grant and actual scene UI hint.
- Visual self-review: NearNPC, InventoryStash, PistolEquipped and BatEquipped at current 62-degree game camera; two inventory icons inspected individually. Missing hint in the baked scene was fixed and rechecked. User visual approval remains separate.
- Existing unknown legacy equipment copies are not deleted heuristically. New equipment saves preserve each weapon slot's full item state.
- `IsolatedSaveProbe.cs` / `DiskSaveValidation.json`: detached test DTO written/read only under ArtWork. No actual player save replacement or live equipment loading. Real-save overwrite validation was blocked by automatic approval review due to risk to existing progress; this isolated file check succeeded instead.
- `RestartGrantValidation.json`: caught and fixed missing starter-claim restoration in SaveManager.Load; restarting now preserves both the claim and stash item count. Two extra test grants from the earlier faulty iterations remain in the user's stash; existing possessions were not heuristically removed.

## Controls

Tab in town/hideout opens equipment + stash. Right click a weapon to equip; drag to a weapon slot to specify storage slot. Select a carried/stashed weapon then press 1–6 to register a quickslot; outside UI the number draws it only if carried/equipped. Ctrl+click transfers between stash and inventory. Equipped weapons have a stash action when the safe-area stash is open.
