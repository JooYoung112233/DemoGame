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

## 프롬프트 6: 미니맵 / 배치 레퍼런스 (탑다운 플로어플랜)

```
Top-down 2D floor plan / minimap diagram of a safehouse compound 
for a 2.5D isometric survival looting game. 
This is a BLUEPRINT / LAYOUT REFERENCE for placing objects in Unity,
not a pretty illustration.

OUTPUT: clean top-down diagram on dark background. 
Output image size: 1920x1080 pixels.

=== GRID ===
Show a visible 16x12 grid (16 columns, 12 rows). 
Each cell is a square with thin dotted lines. 
Grid cells labeled along edges: columns 1-16 (left to right), 
rows A-L (top to bottom).

=== PERIMETER ===
A thick orange outline marks the chain-link fence boundary. 
The fence follows the grid edge with 1-cell margin on all sides.
Bottom-center: a wide GATE opening (3 cells wide), marked in RED.

=== SLOT ZONES ===
Each slot is a colored rectangle on the grid with a label inside.
Use distinct colors per slot status:

ACTIVE (filled, bright):
- [A1] PAWNSHOP — top-left area, ~5x3 cells, YELLOW fill
  Label: "A1: 전당포 (5x3)"
- [A2] CONTAINERS — mid-left area, ~6x3 cells (L-shape), BLUE fill
  Label: "A2: 컨테이너 창고 (6x3)"

LOCKED (empty, dim outline only):
- [B1] — top-right area, ~4x3 cells, GRAY dashed outline
  Label: "B1: (수리점) 4x3"
- [B2] — mid-right area, ~4x3 cells, GRAY dashed outline
  Label: "B2: (가구점) 4x3"
- [A3] — bottom-left area, ~4x3 cells, GRAY dashed outline
  Label: "A3: (블랙마켓) 4x3"
- [B3] — bottom-right area, ~4x3 cells, GRAY dashed outline
  Label: "B3: (의료소) 4x3"

=== FIXED OBJECTS ===
Mark these with simple ICONS or labeled dots on the grid:

- 📋 BULLETIN BOARD — 1x1, bottom area near the gate, LEFT side. 
  GREEN dot, label "게시판"
- 🚪 GATE — 3-cell wide opening at bottom-center fence. 
  RED rectangle, label "철창문 (출구)"
- 🔥 DRUM FIRE — 1x1, center yard. ORANGE dot, label "드럼통"
- 💡 UTILITY POLE — 1x1, center yard. WHITE dot, label "가로등"
- ⚡ GENERATOR — 1x1, near containers. CYAN dot, label "발전기"

=== OPEN YARD ===
The center area (roughly 6x6 cells) should be clearly EMPTY — 
label it "공터 (이동 공간)" in white text. 
This is where the player walks freely.

=== ARROWS / FLOW ===
Draw thin white arrows showing the intended player flow:
Gate → Center yard → Pawnshop (loop)
Gate → Center yard → Containers (loop)
Label the arrow path: "플레이어 동선"

=== LEGEND ===
Bottom or side of the image, a simple legend:
- Yellow fill = Active facility
- Blue fill = Storage
- Gray dashed = Locked slot (future)
- Red = Exit gate
- Green = Interactable object
- Orange outline = Fence perimeter

=== COMPASS ===
Top-right corner: show isometric compass rose.
Mark which direction is "camera facing" in the actual game:
- N (top-right in iso) 
- Arrow labeled "카메라 방향 ↗"

=== STYLE ===
Clean, flat, diagrammatic. Dark navy/charcoal background (#1a1a2e).
Bright colored fills and outlines for contrast.
Monospace or clean sans-serif font for all labels.
Korean text for labels.
Grid lines: thin, dotted, subtle gray (#333).
NO perspective, NO isometric — pure top-down orthographic.
NO textures, NO shadows, NO pixel art — this is a DIAGRAM.
```

---

## 사용 순서

1. **프롬프트 6** (미니맵) → 그리드 좌표 기반 배치 레퍼런스 먼저
2. **프롬프트 1** (전체 조감도) → 맵 전체 분위기 + 레이아웃 확인
3. **프롬프트 4** (철창문) → 출구 분위기 확인
4. **프롬프트 5** (전당포) → 주요 시설 디테일 확인
5. 필요시 **프롬프트 3** (확장 후) → 최종 비전 공유용
6. 개별 건물 모듈은 `isometric_art_prompt_guide.md` 템플릿으로 별도 생성
