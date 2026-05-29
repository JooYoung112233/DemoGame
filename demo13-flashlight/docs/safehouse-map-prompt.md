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

## 프롬프트 7: 철창 펜스 모듈 (제작용 — 낱장)

> **방침**: 컨셉아트(프롬프트 1)는 비전 확정용. 실제 Unity 배치는 **타일+프랍 분리**.
> 펜스는 타일이 아니라 **엣지 프랍** — 타일 경계선 위에 얹힘. 반투명 메쉬라 통짜 금지.
> **모듈을 낱장으로** 뽑아 Unity에서 셀 경계마다 prefab 반복 배치.
> 각 프롬프트에 **컨셉아트 + 격자 이미지를 레퍼런스로 첨부**하고 STRICT STYLE LOCK.

### 공통 헤더 (모든 펜스 모듈에 붙임)

```
[레퍼런스 이미지 #1: 안전가옥 컨셉아트 첨부]
[레퍼런스 이미지 #2: 아이소메트릭 격자 첨부]

STRICT STYLE LOCK — match reference #1 EXACTLY:
same chain-link fence look, same rust, same color palette,
same 1-2px dark outline, same flat cel-shaded detail level.

ISOMETRIC GRID — reference #2 is the absolute authority for angles.
2:1 projection (26.57°), 120° between axes. TRACE the grid lines exactly.

Single isolated asset, transparent background (PNG) — or solid
magenta #FF00FF fill if transparent unavailable.
NO ground, NO other objects, NO city background. JUST the fence piece.
Flat diagram style — ZERO cast shadows on the ground.
The chain-link MESH must read as semi-transparent (you can see
through the gaps) — draw the mesh as a fine cross-hatch pattern,
NOT a solid panel.
```

### 모듈 목록 (각각 낱장으로 생성)

**① fence_straight_NE** — ↗ 방향 직선 1세그먼트
```
[공통 헤더]
A single straight chain-link fence segment, 1 tile long (128px base),
running along the NE-SW isometric axis (going up-right / ↗).
Two metal posts at each end, chain-link mesh stretched between,
a coil of barbed wire along the top, one small patch of rust.
Post height: about 1.2x the tile width. Mesh is see-through.
Output: 256x256px, segment centered, 25% margin.
```

**② fence_straight_NW** — ↘ 방향 직선 (①의 미러로 대체 가능)
```
[공통 헤더]
Same as the NE segment but running along the NW-SE isometric axis
(going down-right / ↘). Mirror of the NE segment.
Output: 256x256px, segment centered, 25% margin.
```

**③ fence_corner** — 90° 코너 (회전/미러로 4코너 커버)
```
[공통 헤더]
A 90° isometric corner of chain-link fence — two segments meeting
at one corner post, forming a sharp right angle in iso space
(one wing going ↗ NE, one wing going ↘ NW).
One taller corner post at the joint, mesh on both wings,
barbed wire continuous over the top.
Output: 320x320px, corner centered, 20% margin.
```

**④ fence_post** — 기둥 단독 (세그먼트 사이 반복 / 길이 조절용)
```
[공통 헤더]
A single standalone chain-link fence post — one vertical metal pole,
rusted, with a short stub of mesh and barbed wire on each side
so it tiles seamlessly between straight segments.
Output: 128x256px, post centered.
```

**⑤ gate_closed** — 게이트 (닫힘)
```
[공통 헤더]
A heavy chain-link DOUBLE GATE, closed, 3 tiles wide (384px base),
set into the front fence. Two swinging gate panels meeting in the
middle, heavy chain wrapped around the center with a padlock.
Thicker steel frame than the regular fence, barbed wire on top.
Mesh see-through. This is THE raid exit.
Output: 512x384px, gate centered, 15% margin.
```

**⑥ gate_open** — 게이트 (열림, ⑤와 동일 프레임)
```
[공통 헤더]
Same double gate as the closed version, but both panels swung
OPEN inward, chain hanging loose from one side, padlock dangling.
The opening is clear (transparent gap) to walk through.
Keep the frame and posts IDENTICAL to the closed version.
Output: 512x384px, gate centered, 15% margin.
```

**⑦ fence_patch (오버레이 3종)** — 보강 패치
```
[공통 헤더]
A small makeshift reinforcement patch sized to cover ONE fence
segment, drawn as a flat overlay to place ON TOP of a fence piece.
Make THREE separate variants:
  (a) blue tarp lashed over the mesh
  (b) corrugated metal sheet wired on
  (c) plywood board nailed across
Each fills roughly 1 tile, ragged improvised edges.
Output each: 256x256px, patch centered.
```

### Unity 배치 규칙

- 펜스는 **타일 경계선(엣지)** 에 배치, 바닥 타일 위 depth
- 둘레 = 직선(①②) 반복 + 코너(③) 4개(회전/미러) + 기둥(④)으로 길이 미세조정
- 게이트(⑤⑥)는 전면 펜스 3타일 구간 교체, 상호작용 시 스프라이트 스왑
- 패치(⑦)는 무작위 세그먼트에 오버레이로 얹어 "패치워크 달동네" 느낌
- 펜스 너머 폐도시는 **별도 배경 레이어**(스카이박스/페럴럭스)로 처리 — 펜스 프랍에 포함 X

## 프롬프트 8: 바닥 타일 (제작용 — 낱장)

> **방침**: 바닥은 단일 아이소 다이아몬드(128×64) 1셀 단위로 낱장 생성.
> Unity에서 타일맵처럼 깔아 16×12 바닥을 구성. 변형 타일을 섞어 단조로움 방지.
> 컨셉아트 + 격자 첨부, STRICT STYLE LOCK.

### 공통 헤더 (모든 바닥 타일에 붙임)

```
[레퍼런스 이미지 #1: 안전가옥 컨셉아트 첨부]
[레퍼런스 이미지 #2: 아이소메트릭 격자 첨부]

STRICT STYLE LOCK — match reference #1 ground EXACTLY:
same cracked asphalt look, same dirt color, same palette,
same 1-2px outline, same flat cel-shaded detail level.

ISOMETRIC TILE — a SINGLE diamond floor tile, 2:1 ratio,
exactly 128px wide x 64px tall. The tile is a perfect rhombus
that TILES SEAMLESSLY — edges must match neighboring tiles
(no border, no outline around the diamond, content bleeds to edge).

Top-down-ish flat floor only — NO walls, NO objects, NO props,
NO shadows, NO height. Just the ground surface.
Transparent background outside the diamond (PNG),
or magenta #FF00FF fill. Output: 128x64px exact.
```

### 타일 목록 (각각 낱장 / 시밍리스)

**① floor_asphalt_plain** — 기본 갈라진 아스팔트
```
[공통 헤더]
Worn cracked asphalt, dark gray, a few hairline cracks,
muted and weathered. The most common base tile.
```

**② floor_asphalt_crack** — 큰 균열 변형
```
[공통 헤더]
Same asphalt but with one larger crack and a small pothole,
weeds poking through. Variation tile to break up repetition.
```

**③ floor_dirt** — 흙/맨땅
```
[공통 헤더]
Packed dirt / bare earth, brown, a few small stones and
scrubby weeds. For patches where asphalt has worn away.
```

**④ floor_asphalt_dirt_blend** — 아스팔트↔흙 전환
```
[공통 헤더]
Half cracked asphalt, half packed dirt, blended across the
diamond — a transition tile between asphalt and dirt areas.
```

**⑤ floor_parking_line** — 주차선 (바랜 페인트)
```
[공통 헤더]
Cracked asphalt with one faded painted parking-lot line
(yellow or white, worn and peeling) crossing the tile.
Make it align to the isometric axis so lines connect across tiles.
```

**⑥ floor_puddle** — 물웅덩이
```
[공통 헤더]
Cracked asphalt with a shallow rain puddle reflecting a dim
blue-gray sky. Wet sheen, subtle. Accent tile, used sparingly.
```

### Unity 배치 규칙

- 기본은 ①으로 깔고, ②③④⑤⑥을 **10~20% 비율로 무작위 섞어** 단조로움 제거
- ⑤ 주차선은 축 방향이 이어지도록 같은 라인끼리 연결 배치
- 바닥은 depth 최하단, 그 위에 펜스(엣지)·오브젝트(Y정렬) 순서
- 슬롯 영역도 같은 바닥 위 — 빈 슬롯엔 흙(③) 비율 높여 "정리 안 된" 느낌

---

## 사용 순서

### 비전 확정 (컨셉)
1. **프롬프트 1** (전체 조감도) → 맵 전체 분위기 + 레이아웃 확인 ✅ 완료
2. **프롬프트 6** (격자 배치도) → 그리드 좌표 기반 배치 레퍼런스
3. 필요시 **프롬프트 3** (확장 후) → 최종 비전 공유용

### 제작 (Unity 에셋) — 타일+프랍 분리
4. **프롬프트 8** (바닥 타일) → 바닥부터 깔기. 낱장 6종 시밍리스 ★현재 단계
5. **프롬프트 7** (펜스 모듈) → 둘레. 가장 까다로움, 낱장 모듈로 ★현재 단계
6. (예정) 오브젝트 프랍 → 컨테이너/드럼통/의자/게시판/지도판 등 개별
7. **프롬프트 5** (전당포 천막) → 주요 시설 디테일
8. 개별 건물 모듈은 `isometric_art_prompt_guide.md` 템플릿으로 별도 생성
