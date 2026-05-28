# 안전가옥 뒷골목 맵 — 컨셉 아트 프롬프트

> 이 프롬프트로 전체 맵 조감도(bird's eye overview)를 먼저 뽑고,
> 이후 개별 건물/소품은 `isometric_art_prompt_guide.md` 모듈 템플릿으로 분리 생성.

---

## 프롬프트 1: 전체 맵 조감도 (레이아웃 컨셉)

```
Isometric pixel art bird's-eye overview of a safehouse compound 
for a 2.5D survival looting game set in an abandoned Korean city.
The safehouse is a wide open lot surrounded by chain-link fences — 
a shantytown / squatter camp feeling (Korean "달동네" aesthetic).

CAMERA: 2:1 isometric projection (26.57° from horizontal), 
camera facing front-left corner, top-left lighting.
Camera pulled far back to show the ENTIRE compound like a minimap.

OUTPUT: full compound visible in one image, generous open space. 
Output image size: 2560x1440 pixels minimum.

=== COMPOUND SHAPE ===

A large roughly rectangular open lot (about 16x12 tiles). 
NOT a narrow alley — a wide open yard with plenty of walking space.
The lot is surrounded by CHAIN-LINK FENCES with barbed wire on top,
NOT concrete walls or building walls. Through the fences, 
the ruined city skyline is visible on all sides.

Fence material: rusted chain-link mesh on metal poles, 
some sections patched with corrugated sheets or tarp, 
barbed wire coils along the top. Makeshift, improvised.

=== INTERIOR LAYOUT ===

The compound interior is mostly OPEN GROUND with objects and 
makeshift structures placed around the edges, leaving a large 
walkable center area. NO permanent buildings inside.

TOP-LEFT AREA — PAWNSHOP:
- [SLOT A1] Makeshift pawnshop — tarp/canvas canopy stretched 
  between fence and wooden poles, counter made of stacked crates, 
  metal shelving behind, single hanging bulb, NPC behind counter
- Feels like a street market stall, not a building

TOP-RIGHT AREA — FUTURE SLOT:
- [SLOT B1] Empty space with just a tarp-covered pile of junk 
  and a "reserved" feeling — flat patch of ground
  (Future: repair workshop bench + tool wall)

CENTER-LEFT — STORAGE:
- [SLOT A2] 2-3 shipping containers (different colors: red, blue, 
  green) arranged in an L-shape. Doors ajar, crates visible inside.
  Player's main stash. Largest structure in the compound.

CENTER-RIGHT — FUTURE SLOT:
- [SLOT B2] A wooden pallet platform with a few empty crates on it.
  Placeholder space. (Future: furniture dealer tent)

BOTTOM-LEFT — FUTURE SLOT:
- [SLOT A3] A dark corner near the fence with just a drum barrel 
  and a tattered tarp overhead. (Future: black market dealer)

BOTTOM-RIGHT — FUTURE SLOT:
- [SLOT B3] A cleared patch of ground with a folding table and chair.
  (Future: medical clinic tent)

BOTTOM CENTER — EXIT ZONE:
- A wooden bulletin board standing on posts, papers pinned to it
- A large chain-link GATE (double-door style, wide enough for 
  a vehicle) — THE exit to raids. Heavy chain and padlock.
- Beyond the gate: a cracked road leading into the dark ruined 
  city. Crumbling buildings, distant flickering lights, slight haze.

CENTER — OPEN YARD:
- Large empty walkable area, cracked asphalt/dirt ground
- A drum barrel with fire (warmth, light source)
- A utility pole with one working light
- Scattered: plastic chairs, cardboard boxes, tire stack, 
  clothesline strung between poles, a puddle or two

=== MINIMAP OVERLAY (optional) ===
Small semi-transparent minimap in the top-right corner of the image 
showing the compound layout as a simple diagram:
- Fence perimeter (white outline)
- Slot positions (colored dots: active=yellow, locked=gray)
- Gate position (red marker)
- Player position (green dot)

=== CHARACTERS ===
- One small chibi pixel art character (~70px tall) standing 
  in the center of the open yard, for scale reference
- One NPC behind the pawnshop counter (hooded merchant)

=== ATMOSPHERE ===
- Time: dusk/early evening — wide open sky visible above 
  (the compound has NO roof), deep blue-orange sunset gradient
- Lighting: warm yellow from pawnshop bulb and drum barrel fire, 
  cold blue-white from the utility pole light,
  orange glow on the horizon beyond the fence (distant city fires)
- Mood: scrappy but homey, makeshift shantytown basecamp, 
  "we built this from nothing" feeling
- NOT horror, NOT military base — improvised survivor camp
- The fence lets you SEE the dangerous city outside, 
  creating constant visual tension between safe inside / danger outside

=== SHANTYTOWN DETAILS ===
- Ground: mix of cracked asphalt and packed dirt, old parking 
  line markings faded, small puddles, scattered debris
- Fence patches: some sections reinforced with plywood, 
  corrugated metal sheets, or blue tarp
- Improvised furniture: plastic stools, wooden cable spool as table, 
  milk crates as seats, hanging laundry
- Vegetation: weeds growing through cracks, small scrubby plants 
  along fence base
- Infrastructure: tangled overhead wires between poles, 
  a generator (off) near the containers, water jugs

=== OUTSIDE THE FENCE (visible through chain-link) ===
- Ruined city buildings — dark silhouettes, broken windows
- Empty streets with abandoned vehicles
- A few distant streetlights (some flickering)
- The sky: dramatic sunset clouds, emphasizing the "between 
  day and night" transition that is central to the game

=== STYLE ===
Art style: crisp readable pixel art. 
STRICT STYLE LOCK — bold 1-2px dark outlines on all structural edges. 
Flat cel-shaded surfaces with only 3-4 color steps per material. 
Large simple shapes only.
NOT photorealistic, NOT painterly, NOT hyper-detailed, NOT illustrated.
If in doubt, use FEWER details and SIMPLER shapes.
Prioritize silhouette clarity over surface detail.

Palette: muted earth tones (dirt brown, dusty beige), 
rusted metal orange-brown, chain-link silver-gray, 
tarp blue-green, warm yellow accent for lights, 
dark teal-black for shadows and night sky.

SCALE REFERENCE:
- Each floor grid cell (128x64px isometric diamond) fits one chibi 
  pixel art character (~70px tall) standing comfortably.
- The entire compound is about 16x12 grid units — 
  a character should look SMALL in this space.
- Pawnshop stall: about 5x3 grid units.
- Each container: about 3x2 grid units.
- Open yard center: at least 6x6 grid units of empty walkable space.

Transparent background NOT needed for this overview — 
use the dusk sky as natural background.
Strict 2:1 pixel ratio on all isometric edges.
```

---

## 프롬프트 2: 낮 버전 (밝은 조명)

위 프롬프트에서 ATMOSPHERE 섹션만 교체:

```
=== ATMOSPHERE ===
- Time: midday — bright overcast sky, flat diffused light
- Lighting: natural daylight, no dramatic shadows, 
  pawnshop bulb off, drum barrel fire out
- Mood: quiet, empty, slightly boring — 
  contrast with the tense nighttime raids
- The gate beyond shows the ruined city in harsh daylight — 
  less mysterious, more desolate
```

---

## 프롬프트 3: 확장 후 버전 (모든 슬롯 해금)

위 프롬프트의 ZONE 설명에서 빈 슬롯을 활성 건물로 교체:

```
ZONE 1 changes:
- RIGHT [B1]: NOW OPEN — A repair workshop. Rolling shutter 
  half-raised, workbench visible inside with tools hanging on 
  wall-mounted pegboard, anvil, grinding wheel. Warm interior light.

ZONE 2 changes:
- RIGHT [B2]: NOW OPEN — A furniture dealer. Shutter fully up, 
  display of miniature furniture models on shelves, wooden sign 
  with furniture icon. Cozy warm light inside.

ZONE 3 changes:
- LEFT [A3]: NOW OPEN — Black market dealer. Shutter cracked open 
  just enough to enter, dim red interior light leaking out, 
  suspicious. Neon-like glow strip around door frame.
- RIGHT [B3]: NOW OPEN — Medical clinic. Clean white curtain 
  replacing the shutter, red cross symbol, bright sterile light 
  inside contrasting with the grimy alley.

Additional alley changes when fully upgraded:
- More lights — string lights between buildings
- Cleaner floor near active shops
- A small radio on a crate playing static (atmosphere)
- The drum barrel fire replaced with a proper brazier
```

---

## 프롬프트 4: 철창문 클로즈업

```
Isometric pixel art close-up of a heavy chain-link gate 
blocking a narrow alley exit in an abandoned Korean city.

2:1 isometric projection (26.57°), camera facing front-left corner.
Output image size: 1280x960 pixels minimum.

The gate spans the full alley width (~4-5 tiles). 
Heavy steel frame with chain-link mesh and barbed wire coils on top. 
A thick padlock and chain on the left side (player-operated).
Gate can swing open to the right.

LEFT of gate: a weathered wooden bulletin board on a post, 
with several pinned papers/notices (no readable text — 
just colored rectangles suggesting documents and a rough map sketch).

RIGHT of gate: concrete wall with a fuse box and bundled cables.

BEYOND the gate: dark street stretching into distance, 
crumbling building silhouettes, one distant flickering streetlight, 
slight fog/haze. Ominous but not horror.

Floor: cracked asphalt with a painted "STOP" line (faded, 
barely visible) in front of the gate.

Art style: crisp readable pixel art.
STRICT STYLE LOCK — bold 1-2px dark outlines. 
Flat cel-shaded, 3-4 color steps per material.
NOT photorealistic, NOT hyper-detailed.
Silhouette clarity over surface detail.

Palette: gunmetal gray gate, rust-brown chain/lock, 
muted concrete, dark teal beyond gate, 
warm yellow from alley-side lighting.

Top-left lighting. Transparent background (PNG).
Strict 2:1 pixel ratio.
```

---

## 프롬프트 5: 전당포 클로즈업

```
Isometric pixel art close-up of a makeshift pawnshop in a narrow 
back alley of an abandoned Korean city.

2:1 isometric projection (26.57°), camera facing front-left corner.
Output image size: 1280x960 pixels minimum.
5x3 grid unit footprint (1 grid = 128x64px).

Structure: lean-to shelter built against alley wall. 
Corrugated metal roof (angled, draining away from wall) 
supported by rough wooden posts. 
One side partially enclosed with plywood and tarp.

Counter: waist-high barrier made of stacked wooden crates 
and a plank on top. A few items on display 
(silhouette shapes only — no specific items). 
Cash register or metal box on counter corner.

Behind counter: shelving unit against the wall 
(metal utility shelf), organized boxes and containers. 
A single hanging light bulb (warm yellow glow). 
A stool for the NPC.

Floor: same cracked asphalt as the alley, 
with a rubber mat behind the counter area.

One chibi NPC character behind the counter for scale — 
hooded figure, merchant pose.

Art style: crisp readable pixel art.
STRICT STYLE LOCK — bold 1-2px dark outlines. 
Flat cel-shaded, 3-4 color steps per material.
NOT photorealistic, NOT hyper-detailed.

Palette: rust-brown corrugated metal, warm wood browns, 
dark tarp blue-gray, warm yellow light accent.

Top-left lighting. Transparent background (PNG).
Strict 2:1 pixel ratio.
```

---

## 프롬프트 6: 전체 배치도 (격자 기반 아이소메트릭, 레퍼 화풍)

> **사용법**: 이 프롬프트 + 첨부 레퍼런스 이미지(게임 스크린샷 또는 컨셉아트)를 함께 전달.
> "STRICT STYLE LOCK"으로 레퍼 화풍을 강제함.
> **핵심**: 모든 오브젝트·벽·펜스가 아이소메트릭 격자 라인 위에 정렬됨.

```
[레퍼런스 이미지 첨부]

STRICT STYLE LOCK — match the attached reference image EXACTLY. 
Same art style, same color palette, same level of detail, 
same outline thickness, same lighting mood. 
Do NOT deviate from the reference style in any way.

=== CRITICAL: GRID-ALIGNED ISOMETRIC ===

This is a TILE-BASED GAME. Every single element must snap to 
an isometric grid:

- The ground is a visible isometric TILE GRID (diamond-shaped tiles).
- ALL fences run along grid lines (diagonal lines in screen space).
- ALL containers, structures, objects are axis-aligned to the grid.
- NO rotated or free-placed objects — everything aligns to 
  the two isometric axes (NE-SW and NW-SE diagonals).
- The fence perimeter follows the grid edge EXACTLY — 
  straight lines along isometric axes, 90° corners only.
- Think of it like a chess board rotated 45° — 
  every piece sits in a cell.

=== OUTPUT ===

Isometric pixel art full-view layout of a safehouse compound.
Camera pulled back to show the ENTIRE compound in one image.
This is a placement reference for building the scene in Unity.

CAMERA: 2:1 isometric projection (26.57° from horizontal), 
camera facing front-left corner, top-left lighting.
Output image size: 2560x1440 pixels minimum.

=== GROUND GRID ===

The compound floor is a 16x12 isometric tile grid.
Each tile is a diamond shape. The grid lines should be 
subtly visible on the ground (cracked asphalt with faded 
parking lot lines that happen to align with the grid).
This helps the viewer understand the tile layout.

=== FENCE PERIMETER (grid-aligned) ===

A chain-link fence runs along the OUTER EDGE of the 16x12 grid.
The fence is made of STRAIGHT SEGMENTS along isometric axes:
- Back-left edge: 16 tiles long, running NW-SE
- Back-right edge: 12 tiles long, running NE-SW  
- Front-left edge: 16 tiles long, running NW-SE
- Front-right edge: 12 tiles long, running NE-SW
- All corners are sharp 90° isometric corners

Fence appearance: rusted chain-link mesh on metal poles, 
barbed wire on top, some sections patched with corrugated 
metal or tarp. Through the fence: ruined city visible.

GATE: A 3-tile-wide opening in the FRONT fence (bottom of screen).
Heavy chain-link double gate with chain and padlock.

=== TILE-ALIGNED OBJECT PLACEMENT ===

All objects occupy exact grid cells. Listed as (column, row) 
from the back-left corner of the 16x12 grid.

BACK-LEFT AREA — PAWNSHOP [A1] (occupies ~cols 2-6, rows 2-4):
- Tarp canopy structure, 5x3 tiles footprint
- Aligned to grid edges, rectangular in isometric view
- Counter, shelving, hanging bulb, NPC behind counter

BACK-RIGHT AREA — EMPTY SLOT [B1] (occupies ~cols 10-13, rows 2-4):
- 4x3 tile area, mostly empty ground
- A tarp-covered junk pile (1x1), loose crates (1x1 each)
- Grid cells clearly visible on empty ground

MID-LEFT — CONTAINERS [A2] (occupies ~cols 2-7, rows 5-8):
- 2-3 shipping containers in L-shape
- Each container is exactly 3x1 or 3x2 tiles
- Containers aligned to grid axes (not rotated!)
- Crates beside them (1x1 tiles each)

MID-RIGHT — EMPTY SLOT [B2] (occupies ~cols 10-13, rows 6-8):
- 4x3 tile area, pallet platform (2x2), empty crates
- Grid visible

FRONT-LEFT — EMPTY SLOT [A3] (occupies ~cols 2-5, rows 9-11):
- 4x3 tile area, drum barrel (1x1), tattered tarp

FRONT-RIGHT — EMPTY SLOT [B3] (occupies ~cols 10-13, rows 9-11):
- 4x3 tile area, folding table (1x1), chair (1x1)

FRONT-CENTER — EXIT ZONE (cols 7-9, row 12):
- Bulletin board (1x1 tile) at col 6, row 11
- Gate opening (3 tiles wide) in the front fence
- Beyond gate: road into dark ruined city

CENTER YARD (cols 7-9, rows 5-9):
- Open empty tiles — at least 4x4 clear walking space
- Drum barrel fire (1x1) at roughly center
- Utility pole (1x1) with working light
- Scattered small objects: each exactly 1x1 tile

=== ATMOSPHERE ===
- Time: dusk/early evening, open sky above
- Sunset gradient: deep blue-orange
- Warm yellow from pawnshop + barrel fire
- Cold white from utility pole light
- Distant orange glow beyond fence
- Shantytown / 달동네 mood

=== MINIMAP OVERLAY ===
Top-right corner of the image: a small semi-transparent 
minimap diagram showing:
- Grid outline (16x12 diamond)
- Fence perimeter (white border)
- Slot positions labeled A1, A2, A3, B1, B2, B3
- Active slots: yellow dots (A1, A2)
- Empty slots: gray dots (B1, B2, A3, B3)
- Gate: red mark at bottom
- Player: green dot at center

=== STYLE NOTES ===
- Every object edge aligns to the isometric grid
- The grid itself should be faintly visible on the ground
- The overall look should feel like a GAME SCREEN, 
  not a painting — clean, readable, tile-based
- Character should look small — this is a zoomed-out overview

Match the attached reference EXACTLY in art style.
Strict 2:1 pixel ratio on all isometric edges.
```

---

## 사용 순서

1. **프롬프트 6** (미니맵) → 그리드 좌표 기반 배치 레퍼런스 먼저
2. **프롬프트 1** (전체 조감도) → 맵 전체 분위기 + 레이아웃 확인
3. **프롬프트 4** (철창문) → 출구 분위기 확인
4. **프롬프트 5** (전당포) → 주요 시설 디테일 확인
5. 필요시 **프롬프트 3** (확장 후) → 최종 비전 공유용
6. 개별 건물 모듈은 `isometric_art_prompt_guide.md` 템플릿으로 별도 생성
