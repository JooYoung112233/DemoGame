# 안전가옥 타일/펜스/프랍 — 에셋 생성 프롬프트

> **시점 (2026-06-02 확정)**: 2:1 아이소메트릭 → **탑다운 80° baked-in**
> - 카메라 = **2D 직교(orthographic) 평면 뷰** (3D 틸트 카메라 아님)
> - **틸트(80°)는 그림 자체에 미리 그려넣음** — 모든 에셋이 80° 눕힌 시점으로 렌더
> - 맵 = **타일 조립식** (바닥 타일 격자 + 위에 스프라이트 배치)
> **사용법**: 각 프롬프트 + 컨셉아트 레퍼런스 이미지를 함께 첨부.

---

## 시점 정의 (중요 — 전체 통일)

```
- 모든 에셋(바닥/벽/펜스/프랍/캐릭터)은 동일한 80° 탑다운 시점으로 그린다.
- 80° = 거의 위에서 내려다보되 살짝 눕힌 각도.
  → 오브젝트의 윗면이 주로 보이고, 카메라 쪽(화면 아래=남쪽) 정면이 약간 보임.
  → 높이/두께는 낮게 압축 (살짝의 입체감만).
- 바닥도 80°이므로 정사각 타일이 세로(깊이축)로 살짝만 눌린 정사각형으로 보임.
  (완전 직하 90°가 아님 — 전체 아트 일관성을 위해 80°로 통일)
- 그리드는 정사각형 (아이소메트릭 다이아몬드 아님).
- 벽 방향:
    북벽(N) = 화면 위쪽 가로벽 (윗면 + 바깥쪽이 약간 보임)
    남벽(S) = 화면 아래쪽 가로벽 (윗면 + 안쪽 정면이 보임, 카메라에 가장 가까움)
    동벽(E) = 화면 오른쪽 세로벽 (윗면 + 왼쪽 안쪽면 약간)
    서벽(W) = 화면 왼쪽 세로벽 (윗면 + 오른쪽 안쪽면 약간)
```

---

## 벽 렌더링 방식: 하이브리드 (확정 2026-06-02)

80° 베이크 원근은 정면 띠 방향이 한쪽(남향)일 때만 성립 → 사방 분기(T/십자/세로/코너)는 깨짐.
그래서 벽을 **하이브리드**로 통일:

| 벽 종류 | 표현 |
|---|---|
| **가로벽 (남향, 카메라쪽 면)** | 80° — 윗면 캡 + **정면 띠(facade band)** |
| **세로벽 / 북향 / 코너 / T / 십자 / 엔드캡 / 개구부** | **윗면(탑캡)만** (정면 띠 없음, 평면 90°) |

- **핵심 일관성 규칙**: 윗면(탑캡) 재질·색·두께는 **모든 조각이 동일**해야 함.
  정면 띠는 남향 가로벽에만 "추가로" 붙는 요소.
- 즉 방을 만들면: 아래쪽(남) 벽만 정면 띠가 보이고, 좌/우/위 벽과 모든 분기는 윗면만 → 배틀맵 룩.
- 입체감 보완은 런타임 셰이더 라이팅/그림자가 담당.

---

## 공통 스타일 블록 (모든 프롬프트에 붙여넣기)

> **스타일 결정 (2026-06-02)**: 1차 프랍 생성물이 너무 페인터리·고디테일이라
> 평면도 레퍼처럼 **플랫 + 도트(pixel) 느낌**으로 톤다운. 아래 강화 블록 사용.

```
[레퍼런스 이미지 첨부]

STRICT STYLE LOCK — match the attached top-down floorplan reference's
FLATNESS and simplicity. Do NOT make it more detailed than the reference.

Art style: FLAT, simple PIXEL ART for a top-down game.
- LOW detail. Large readable shapes only. Simplify every surface.
- Flat cel-shading: only 3-4 solid color steps per material, HARD edges
  between shades. NO smooth gradients, NO soft airbrush shading.
- Bold dark 1-2px outline around each object silhouette.
- Visible chunky pixels, limited palette — looks hand-placed pixel art,
  coarse pixel grid, NOT a high-resolution painting.
- NO fine surface noise, NO grime speckling, NO painterly rendering,
  NO photorealism, NO 3D-render look.
- If in doubt, use FEWER details and FLATTER colors.
Prioritize silhouette clarity over surface detail.

FLAT SHADING (셰이더 후처리 전제): no baked directional shadows,
no gradients, no dithering. Even flat lighting only — runtime 2D Light
and dot/grain texture are applied later via shader. Keep surfaces flat.

Transparent background (PNG).
```

> **핵심 키워드**: FLAT, low detail, 3-4 color steps, hard-edge cel shading,
> chunky pixels, NO gradients, NO noise, NO painterly. (1차 화풍 톤다운용)

> **스타일 레퍼런스 (확정 2026-06-02)**: D&D 배틀맵풍 80° 탑다운 실내 레퍼 이미지를 첨부해 화풍 락.
> - 이 레퍼는 **렌더링 화풍·디테일·각도(80°)의 기준** — 내용물(실내 마루)이 아니라 "룩"만 가져옴
> - 우리 안전구역은 **야외 공터**(아스팔트/흙 바닥 + 철조망 펜스)이므로 내용은 다르게
> - 레퍼엔 그림자가 약하게 구워져 있으나, 런타임 라이팅(시야 콘/낮밤)과 충돌 방지 위해
>   **방향 그림자는 약하게/균일하게** 유지 (오브젝트 근처 약한 접지 그림자 정도만)

---

## 1. 바닥 타일 (Floor Tiles) — 80° 탑다운 (전체 통일)

> 바닥도 80° 틸트로 그림 (전체 일관성). 2D 직교 카메라에 타일로 깔림.
> 정사각 타일이 세로축으로 살짝만 눌린 정사각형. 서로 이음새 없이 반복.

### 프롬프트 1-1: 균열 아스팔트 타일 세트

```
[공통 스타일 블록]

Top-down 80° view ground tile — viewed from high above with a slight
forward tilt (NOT a flat 90° straight-down, NOT isometric).
The tile is a square seen at 80°, so it reads as a square only slightly
compressed along the vertical (depth) axis. Almost flat, faint top-down tilt.

Generate a TILE SHEET of 4 seamless ground tile variations,
arranged in a 2x2 grid. Each tile is 256x256 pixels.
Total output: 512x512 pixels.

All 4 tiles must tile seamlessly with each other in any combination
(edges wrap so they can repeat infinitely).

TILE 1 (top-left): Cracked asphalt — basic.
Gray-brown asphalt with subtle cracks and wear.
Faded white parking line fragment crossing one corner.

TILE 2 (top-right): Cracked asphalt — heavy damage.
Larger cracks, chunks missing, darker stains, gravel in cracks.

TILE 3 (bottom-left): Asphalt with weeds.
Same asphalt base but small green weeds growing through cracks.
2-3 small tufts.

TILE 4 (bottom-right): Asphalt with puddle.
Shallow water puddle on cracked asphalt, roughly circular,
subtle blue-gray reflection.

Palette: dark gray asphalt base, lighter gray crack lines,
faded white/yellow road markings, muted green weeds, blue-gray puddle.

The 80° tilt is baked into the art (2D orthographic camera, no 3D tilt).
```

### 프롬프트 1-2: 흙바닥 / 특수 타일 세트

```
[공통 스타일 블록]

Top-down 80° view ground tiles — high above with a slight forward tilt
(NOT flat 90°, NOT isometric). Squares slightly compressed along depth axis.

Generate a TILE SHEET of 4 seamless ground tiles in a 2x2 grid.
Each tile 256x256 pixels. Total 512x512 pixels.

TILE 1 (top-left): Packed dirt ground.
Brown packed earth, dry and flat, small pebbles, subtle footprints.

TILE 2 (top-right): Dirt-to-asphalt transition.
Left half packed dirt, right half cracked asphalt,
rough irregular broken edge between them.

TILE 3 (bottom-left): Rubber floor mat.
Dark gray industrial rubber mat, diamond-plate texture, worn.

TILE 4 (bottom-right): Road asphalt (exterior).
Darker smoother asphalt, yellow center line. For outside the fence.

Palette: warm brown earth, gray-brown asphalt, dark gray rubber,
yellow road marking.
The 80° tilt is baked into the art (2D orthographic camera).
```

---

## 2. 펜스 (Chain-link Fence) — 탑다운 80°, 동서남북 4방향

> Unity에서 격자 모서리에 Sprite로 배치. 약간 기울인 탑다운이라 
> 펜스의 윗면(가시철사 코일)이 주로 보이고 한쪽 면이 살짝 보임.

### 프롬프트 2-1: 체인링크 펜스 4방향 직선 세트

```
[공통 스타일 블록]

Top-down 80° view chain-link fence segments for a top-down game.
Slight downward tilt — you see mostly the TOP of the fence
(barbed wire coil running along it) and a thin sliver of one face.
NOT isometric, NOT side-view — near top-down.

Generate 4 fence direction variants in a 2x2 grid.
Each segment is 3 tiles long. Each cell 384x192 pixels.
Total output: 768x384 pixels.

The fence: rusted chain-link mesh on metal poles (one pole per tile),
barbed wire coil along the top edge, gray-silver mesh with rust spots.
Keep height LOW/compressed (this is near top-down).

TILE-CELL LAYOUT:
TOP-LEFT — NORTH wall (horizontal, runs left-right across screen).
  This wall is at the TOP/far side. You see its top + a sliver of
  the OUTER face. Barbed wire coil along the top.

TOP-RIGHT — SOUTH wall (horizontal, runs left-right).
  This wall is at the BOTTOM/near side (closest to camera).
  You see its top + a sliver of the INNER face. Mesh more visible.

BOTTOM-LEFT — WEST wall (vertical, runs up-down on the left).
  You see its top + a sliver of the right (inner) face.

BOTTOM-RIGHT — EAST wall (vertical, runs up-down on the right).
  You see its top + a sliver of the left (inner) face.

Each segment must tile seamlessly end-to-end with copies of itself.
Through the mesh: transparent (city layered separately in Unity).
Transparent background (PNG).
```

### 프롬프트 2-2: 펜스 코너 4종

```
[공통 스타일 블록]

Top-down 80° view chain-link fence CORNER pieces.
Same near-top-down tilt and materials as the straight segments.

Generate 4 corners in a 2x2 grid. Each cell 192x192 pixels.
Total output: 384x384 pixels.

All corners are 90° turns of chain-link fence with barbed wire on top,
rusted poles with one reinforced corner pole (diagonal brace).

TOP-LEFT — NW corner: connects NORTH wall to WEST wall (top-left of compound).
TOP-RIGHT — NE corner: connects NORTH wall to EAST wall (top-right).
BOTTOM-LEFT — SW corner: connects SOUTH wall to WEST wall (bottom-left).
BOTTOM-RIGHT — SE corner: connects SOUTH wall to EAST wall (bottom-right,
  nearest camera).

Each corner aligns seamlessly with the matching straight segments.
Transparent background (PNG).
```

### 프롬프트 2-3: 펜스 패치 변형 3종 (가로벽 기준)

```
[공통 스타일 블록]

Top-down 80° view PATCHED chain-link fence segments.
Horizontal orientation (south/north wall, runs left-right).
Same tilt and base as plain fence.

Generate 3 patched variations in a 1x3 vertical strip.
Each segment 3 tiles long, 384x192 pixels.
Total output: 384x576 pixels.

VARIATION 1 (top): Corrugated metal patch.
Rusted corrugated metal sheet wired over lower part of the chain-link.
Barbed wire still on top. Orange-brown rust.

VARIATION 2 (middle): Blue tarp patch.
Blue plastic tarp stretched over the fence, tied with rope,
wrinkled and weathered. Chain-link visible above/below.

VARIATION 3 (bottom): Plywood patch.
Plywood boards nailed over the section, some cracked/water-stained,
nails and wire visible. Light wood brown.

All tile seamlessly with the plain horizontal fence (2-1).
Transparent background (PNG).
```

### 프롬프트 2-4: 메인 게이트 (3타일 폭, 남벽)

```
[공통 스타일 블록]

Top-down 80° view main gate for a safehouse compound.
The gate is on the SOUTH wall (bottom of screen, nearest camera),
horizontal orientation. Same near-top-down tilt.

Output size: 576x256 pixels. 3 tiles wide.

Gate structure:
- Double-door chain-link gate, each door ~1.5 tiles wide
- Heavy steel frame (thicker poles than fence), concrete bases
- Chain-link mesh on both doors, barbed wire coil on top rail
- A thick CHAIN and shiny PADLOCK at the center seam where the two
  doors meet (key visual detail)
- Doors closed in this sprite

Connects to regular fence / patch on both ends.
Show a thin strip of cracked asphalt under the gate
with a faded "STOP" marking barely visible.

Through the mesh: transparent.
Transparent background (PNG).
```

---

## 3. 펜스 기둥 / 전봇대

### 프롬프트 3-1: 기둥 세트 4종

```
[공통 스타일 블록]

Top-down 80° view fence POLES / posts for a top-down game.
Near top-down tilt — you see the top of each pole and a short
compressed shaft (not a tall side-view pole).

Generate 4 pole types in a 2x2 grid. Each cell 128x192 pixels.
Total output: 256x384 pixels.

POLE 1 (top-left): Standard fence pole.
Rusted steel tube with top cap, cracked concrete base block.

POLE 2 (top-right): Corner reinforced pole.
Thicker, with diagonal brace strut, heavier concrete base.

POLE 3 (bottom-left): Gate frame pole.
Heaviest, square steel profile, large concrete base, hinge hardware.

POLE 4 (bottom-right): Utility pole with light.
Taller wooden utility pole, one working light on top
(cold white glow pool on ground beneath it), tangled wires.
Goes inside the compound near center.

Standalone sprites — no mesh attached, overlaid on fence in Unity.
Transparent background (PNG).
```

---

## 4. 게이트 주변 프랍

### 프롬프트 4-1: 게시판 + 지도판

```
[공통 스타일 블록]

Top-down 80° view bulletin boards for a safehouse compound.
Near top-down tilt — slight angle showing the board face.

Generate 2 boards side by side. Output 512x320 pixels.

LEFT — Notice bulletin board (1 tile footprint):
Weathered wooden frame on two posts, cork backing,
several pinned papers (cream/white/yellow rectangles, NO text),
a rough hand-drawn map sketch pinned at center, curling edges, push pins.

RIGHT — Map board (1 tile footprint):
Larger easel-style frame (welded scrap metal), a detailed city map inside,
compass rose in a corner, a red circle/pin marking a target.

Both freestanding on cracked asphalt.
Transparent background (PNG).
```

---

## 생성 순서 체크리스트

1. [ ] **1-1** 균열 아스팔트 타일 세트 (4종) — 80° 탑다운
2. [ ] **1-2** 흙바닥/특수 타일 세트 (4종) — 80° 탑다운
3. [ ] **2-1** 펜스 4방향 직선 (N/S/E/W) — 80° 탑다운
4. [ ] **2-2** 펜스 코너 4종 (NW/NE/SW/SE)
5. [ ] **2-3** 펜스 패치 변형 3종
6. [ ] **2-4** 메인 게이트 (남벽)
7. [ ] **3-1** 펜스 기둥 세트 4종
8. [ ] **4-1** 게시판 + 지도판

> 바닥(1) → 펜스 직선(2-1) → 코너(2-2) → 게이트(2-4) → 패치(2-3) → 기둥(3-1) → 프랍(4-1)
