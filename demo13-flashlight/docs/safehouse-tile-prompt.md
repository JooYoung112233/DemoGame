# 안전가옥 타일/펜스/프랍 — 에셋 생성 프롬프트

> **사용법**: 각 프롬프트 + 컨셉아트 레퍼런스 이미지를 함께 첨부.
> 바닥은 **정면 정사각형 (top-down)**, 펜스/벽은 **아이소메트릭 방향별** 생성.

---

## 공통 스타일 블록 (모든 프롬프트에 붙여넣기)

```
[레퍼런스 이미지 첨부]

STRICT STYLE LOCK — match the attached reference image EXACTLY.
Same art style, same color palette, same outline thickness,
same level of detail. Do NOT deviate from the reference style.

Art style: crisp readable pixel art.
Bold 1-2px dark outlines on all structural edges.
Flat cel-shaded surfaces with only 3-4 color steps per material.
NOT photorealistic, NOT painterly, NOT hyper-detailed.
Prioritize silhouette clarity over surface detail.

Transparent background (PNG).
```

---

## 1. 바닥 타일 (Floor Tiles) — 정면 정사각형

> 바닥은 Unity 3D 평면에 텍스처로 깔리므로 **top-down 정사각형** 으로 생성.
> 게임 내에서 아이소메트릭 카메라가 알아서 다이아몬드로 보이게 함.

### 프롬프트 1-1: 균열 아스팔트 타일 세트

```
[공통 스타일 블록]

Top-down view, perfectly square tile, viewed from DIRECTLY ABOVE.
NO perspective, NO isometric angle — pure orthographic top-down.

Generate a TILE SHEET of 4 seamless ground tile variations,
arranged in a 2x2 grid. Each tile is 256x256 pixels.
Total output: 512x512 pixels.

All 4 tiles must tile seamlessly with each other in any combination.

TILE 1 (top-left): Cracked asphalt — basic.
Gray-brown asphalt with subtle cracks and wear.
Faded white parking line fragment crossing one corner.

TILE 2 (top-right): Cracked asphalt — heavy damage.
Larger cracks, chunks missing, darker stains.
Bits of gravel visible in cracks.

TILE 3 (bottom-left): Asphalt with weeds.
Same asphalt base but small green weeds/grass
growing through cracks. 2-3 small tufts.

TILE 4 (bottom-right): Asphalt with puddle.
Shallow water puddle on cracked asphalt surface.
Puddle is roughly circular, subtle blue-gray reflection.

Palette: dark gray asphalt base, lighter gray crack lines,
faded white/yellow for old road markings,
muted green for weeds, blue-gray for puddle.

These tiles will be placed on a 3D ground plane in Unity.
The isometric camera will handle the perspective — 
draw them FLAT, top-down only.
```

### 프롬프트 1-2: 흙바닥 / 특수 타일 세트

```
[공통 스타일 블록]

Top-down view, perfectly square tiles, viewed from DIRECTLY ABOVE.
NO perspective — pure orthographic top-down.

Generate a TILE SHEET of 4 seamless ground tile variations,
arranged in a 2x2 grid. Each tile is 256x256 pixels.
Total output: 512x512 pixels.

TILE 1 (top-left): Packed dirt ground.
Brown packed earth, dry and flat. Small pebbles scattered.
Subtle footprint impressions.

TILE 2 (top-right): Dirt-to-asphalt transition.
Left half is packed dirt, right half is cracked asphalt.
Rough irregular edge where asphalt has broken away.

TILE 3 (bottom-left): Rubber floor mat.
Dark gray industrial rubber mat with diamond plate texture.
Slightly worn. Used behind pawnshop counter.

TILE 4 (bottom-right): Road asphalt (exterior).
Darker, smoother asphalt than interior tiles.
Yellow center line marking. For outside the fence.

Palette: warm brown earth, gray-brown asphalt,
dark gray rubber, yellow road marking.
```

---

## 2. 펜스 (Fence) — 아이소메트릭 4방향

> Unity 3D에서 Quad/Sprite로 배치. 아이소메트릭 카메라 기준 
> **좌벽(NW-SE)** 과 **우벽(NE-SW)** 2가지 각도가 핵심.
> 뒷면(S, W)은 앞면 좌우반전으로 대체 가능.

### 프롬프트 2-1: 체인링크 펜스 — 좌벽 (NW→SE 방향)

```
[공통 스타일 블록]

Isometric pixel art fence segment.
2:1 isometric projection (26.57° from horizontal).

This fence runs along the NW-SE axis (going from upper-left 
to lower-right in screen space). It is viewed from the 
standard isometric camera facing the front-left corner.

The fence is 3 TILES LONG (one seamless segment).
Output size: 512x384 pixels.

Fence structure:
- Chain-link mesh on vertical metal poles (one pole per tile)
- Poles are rusted steel tubes, slightly leaning
- Barbed wire coil running along the top
- Mesh is gray-silver with rust spots
- Bottom of fence touches ground level (include ~16px of ground shadow)

The fence faces the CAMERA (you can see through the mesh).
Behind the fence: leave transparent — assets will be layered in Unity.

Height: roughly 1.5x a character tile height (~100px from ground to barbed wire top in isometric).

Seamless edges: left and right edges of the segment must tile 
perfectly when placed next to another copy of this fence.

Transparent background (PNG).
```

### 프롬프트 2-2: 체인링크 펜스 — 우벽 (NE→SW 방향)

```
[공통 스타일 블록]

Isometric pixel art fence segment.
2:1 isometric projection (26.57° from horizontal).

This fence runs along the NE-SW axis (going from upper-right 
to lower-left in screen space). It is viewed from the 
standard isometric camera facing the front-left corner.

The fence is 3 TILES LONG (one seamless segment).
Output size: 512x384 pixels.

Fence structure:
- Chain-link mesh on vertical metal poles (one pole per tile)
- Poles are rusted steel tubes
- Barbed wire coil along the top
- Gray-silver mesh with rust spots
- Ground shadow at base (~16px)

This wall recedes away from camera — you see it at an angle.
The mesh pattern appears more compressed/foreshortened 
compared to the NW-SE version.

Seamless tiling on left and right edges.
Transparent background (PNG).
```

### 프롬프트 2-3: 펜스 코너 (4종)

```
[공통 스타일 블록]

Isometric pixel art fence CORNER pieces.
2:1 isometric projection (26.57° from horizontal).

Generate 4 corner pieces arranged in a 2x2 grid.
Each corner piece: 256x256 pixels.
Total output: 512x512 pixels.

All corners are 90° turns of chain-link fence with barbed wire.
Same materials as straight segments: rusted poles, chain-link mesh, 
barbed wire on top.

TOP-LEFT: Corner turning from NW-SE wall → NE-SW wall (back-left corner).
Camera sees the outside of both fence faces.

TOP-RIGHT: Corner turning from NE-SW wall → NW-SE wall (back-right corner).
Camera sees one face head-on, other receding.

BOTTOM-LEFT: Corner turning from NW-SE wall → NE-SW wall (front-left corner).
Camera sees the inside of both fence faces.

BOTTOM-RIGHT: Corner turning from NE-SW wall → NW-SE wall (front-right corner).
This is the corner nearest the camera.

Each corner has ONE reinforced corner pole (thicker, with diagonal brace).
Transparent background (PNG).
```

### 프롬프트 2-4: 펜스 변형 — 패치 세그먼트 (좌벽 NW-SE)

```
[공통 스타일 블록]

Isometric pixel art PATCHED fence segments (NW-SE direction).
2:1 isometric projection (26.57° from horizontal).

Generate 3 patched fence variations in a 1x3 vertical strip.
Each segment: 512x384 pixels (3 tiles long).
Total output: 512x1152 pixels.

These are chain-link fence segments where parts have been 
covered or patched by survivors:

VARIATION 1 (top): Corrugated metal patch.
Rusted corrugated metal sheet bolted/wired over the lower 2/3 
of the chain-link. Barbed wire still visible on top.
Orange-brown rust color.

VARIATION 2 (middle): Blue tarp patch.
Blue plastic tarp/tarpaulin stretched over the fence, 
tied with rope at corners. Wrinkled, weathered.
Some chain-link visible above and below tarp.

VARIATION 3 (bottom): Plywood patch.
Raw plywood boards nailed over the fence section.
Some boards cracked or water-stained. 
Nails and wire visible. Light wood brown color.

All three must tile seamlessly with the plain chain-link 
fence segment (prompt 2-1) on both ends.
Transparent background (PNG).
```

### 프롬프트 2-5: 메인 게이트 (3타일 폭)

```
[공통 스타일 블록]

Isometric pixel art main gate for a safehouse compound.
2:1 isometric projection (26.57° from horizontal).

The gate faces the CAMERA (front fence, NW-SE direction).
Output size: 768x512 pixels.

Gate structure (3 tiles wide):
- Double-door chain-link gate, each door ~1.5 tiles wide
- Heavy steel frame (thicker than fence poles)
- Chain-link mesh on both doors
- Barbed wire coil on top frame rail
- A thick CHAIN and PADLOCK holding the two doors together 
  at center seam (important visual detail — shiny lock)
- Gate can swing open outward (toward camera)

FLANKING ELEMENTS:
- Left side: connects to regular fence or plywood patch
- Right side: connects to regular fence
- Gate frame poles are beefier than regular fence poles
  (concrete base visible at ground level)

GROUND: cracked asphalt directly under and in front of gate.
Faded "STOP" text or road marking barely visible on asphalt.

Beyond the gate (visible through mesh):
Dark road leading away, slight fog. Keep it minimal — 
just enough to suggest "outside is dangerous."

Transparent background (PNG).
```

---

## 3. 펜스 기둥 / 연결 부품

### 프롬프트 3-1: 펜스 기둥 세트

```
[공통 스타일 블록]

Isometric pixel art fence POLES / posts.
2:1 isometric projection (26.57° from horizontal).

Generate a SPRITE SHEET of 4 pole types in a 2x2 grid.
Each pole: 128x256 pixels.
Total output: 256x512 pixels.

POLE 1 (top-left): Standard fence pole.
Rusted steel tube, ~2m tall. Simple top cap.
Bolted to cracked concrete base block.

POLE 2 (top-right): Corner reinforced pole.
Thicker pole with diagonal brace strut welded on.
Heavier concrete base. Used at fence corners.

POLE 3 (bottom-left): Gate frame pole.
Heaviest pole. Square steel profile instead of round.
Concrete base is larger. Hinge hardware on one side.
Used at gate frame.

POLE 4 (bottom-right): Utility pole with light.
Wooden utility pole (taller than fence).
One working light fixture on top (cold white glow).
Tangled wires. This goes INSIDE the compound near center.

All poles are standalone sprites — no fence mesh attached.
They will be overlaid on fence segments in Unity.
Transparent background (PNG).
```

---

## 4. 게이트 주변 프랍

### 프롬프트 4-1: 게시판 + 지도판

```
[공통 스타일 블록]

Isometric pixel art bulletin boards for a safehouse compound.
2:1 isometric projection (26.57° from horizontal).

Generate 2 boards side by side.
Output size: 512x384 pixels.

LEFT — Notice bulletin board (1x1 tile footprint):
- Weathered wooden frame on two posts stuck in ground
- Cork/plywood backing
- Several pinned papers/notices (colored rectangles — 
  cream, white, yellow — NO readable text)
- A rough hand-drawn map sketch pinned at center
- Some papers curling at edges, one pin missing
- Push pins and tape visible

RIGHT — Map board (1x1 tile footprint):
- Larger standing frame (like a display easel)
- A detailed-looking MAP inside (bird's eye city layout sketch)
- Compass rose / directional marker in corner of map
- Red circle or pin marking on the map (target location)
- Frame is welded scrap metal, functional not pretty

Both are freestanding objects, not attached to any wall.
They sit on cracked asphalt ground.
Transparent background (PNG).
```

---

## 생성 순서 체크리스트

1. [ ] **1-1** 균열 아스팔트 타일 세트 (4종)
2. [ ] **1-2** 흙바닥/특수 타일 세트 (4종)
3. [ ] **2-1** 체인링크 펜스 좌벽 NW-SE
4. [ ] **2-2** 체인링크 펜스 우벽 NE-SW
5. [ ] **2-3** 펜스 코너 4종
6. [ ] **2-4** 펜스 패치 변형 3종
7. [ ] **2-5** 메인 게이트
8. [ ] **3-1** 펜스 기둥 세트 4종
9. [ ] **4-1** 게시판 + 지도판

> 바닥(1) → 펜스 직선(2-1,2-2) → 코너(2-3) → 게이트(2-5) → 패치(2-4) → 기둥(3-1) → 프랍(4-1)
