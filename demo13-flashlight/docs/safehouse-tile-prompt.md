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

## 배경 규칙 (확정 2026-06-03 — 예외 없음)

- **모든 생성 시트는 동일한 짙은 회색 배경**(`#3a3d40`)으로 뽑는다.
- **흰색 배경 금지, 투명(알파) 배경 금지.** 시트마다 배경이 달라지면 일관성이
  깨지고, 흰 배경은 잘라낼 때 가장자리에 흰 프린지가 남는다.
- 짙은 회색으로 통일 → **엔진(Unity)에서 그 회색을 키 아웃**해 투명 처리.
- 바닥 타일·배경(스카이박스)처럼 꽉 채우는 이미지는 키 아웃 대상이 아니므로
  배경 개념 자체가 없음(이미지 전체가 콘텐츠).

---

## 프랍 방향 규칙 (확정 2026-06-04)

- **벽에 붙는 프랍(금고·선반·책상·냉장고·TV 등)은 방향별로 뽑는다.** 한 방향만
  있으면 북/동/서 벽에 붙일 때 등을 못 보여 어색함.
- 탑다운 80°에서 벽 부착 프랍은 "벽 반대쪽을 바라봄":
    - 남벽 부착 → **북향(정면 아래, 등이 위)** = 기본
    - 북벽 부착 → **남향(정면 위)**
    - 서벽 부착 → **동향(오른쪽)** / 동벽 부착 → **서향(왼쪽)**
- 보통 **3종(정면/후면/측면)** 만 그리고 측면은 **좌우 플립**으로 재활용.
  측면이 비대칭이면(손잡이·문 방향 등) 4종 전부 생성.
- **둥근/대칭 프랍(드럼통·쓰레기통·타이어·박스)은 1방향이면 충분.**
- 템플릿 프롬프트: 아래 **10번** 참고.

---

## 낡음 = 데칼 분리 규칙 (확정 2026-06-03)

- **베이스 에셋(벽·문·바닥·프랍·건물)은 비교적 깔끔하게** 뽑는다.
  (기본 균열/마모 정도만, 과한 녹·물때·이끼는 빼기)
- **낡음/오염은 별도 "데칼 시트"로 따로 뽑아** 엔진에서 베이스 위에 겹쳐쓴다.
  (녹 줄기, 물때 흘림, 균열, 이끼, 먼지때, 그을음, 그래피티 등)
- 장점: 같은 데칼을 모든 벽/문/바닥에 재사용 → 일관성 + 다양성 + 효율.
- 데칼 시트는 **투명 배경이 아니라 짙은 회색(#3a3d40)** 으로 뽑고 엔진에서 키 아웃,
  곱하기/오버레이 블렌드로 사용 (→ 프롬프트 9-1 참고).
- 따라서 앞으로 i2i로 에셋을 직접 더럽히지 않는다. 더러움은 데칼 레이어 담당.

---

## 글자 금지 규칙 (확정 2026-06-03 — 예외 없음)

- **모든 에셋(물건·프랍·건물·간판·표지)에 글자(텍스트/문자)를 절대 넣지 않는다.**
  AI가 한글·영문을 항상 깨뜨려서 알아볼 수 없는 가짜 글자가 생김.
- 간판/표지/문서/책/달력 등은 **빈 판, 색 띠, 픽토그램/심볼**로만 표현.
  (예: "전당포" 글자 대신 빈 나무 간판 + 전구 / 지도판은 글자 없는 지도 그림)
- **실제 상호·글씨는 나중에 엔진에서** UI 텍스트나 별도 텍스처로 얹는다.
- 모든 프롬프트 끝에 다음 한 줄 포함:
  `NO text, NO letters, NO numbers anywhere. Signs/papers are BLANK or
  symbol-only (text is added later in engine).`

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

NO text, NO letters, NO numbers anywhere. Signs/papers/books/calendars are
BLANK or symbol-only (real text is added later in engine).

Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
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
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
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
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
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
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
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
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
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
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
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
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
```

---

## 5. 바리게이트 (길막용 — 임시, 나중에 제거 가능)

> 통로/골목을 막아 동선을 제한하는 오브젝트. **게임 진행에 따라 치워질 수 있는 임시 장애물**.
> 단독 스프라이트로 길 위에 얹음. 탑다운 80°, 낮게 압축된 더미 형태.
> 레퍼처럼 잡동사니·폐자재를 쌓아 "급조한" 느낌.

### 프롬프트 5-1: 바리게이트 세트 4종

```
[공통 스타일 블록]

Top-down 80° view makeshift street BARRICADES for a post-apocalyptic
Korean alley (matches the attached ruined-city top-down map reference).
Near top-down tilt — you see the top of the pile and a low compressed
front. Keep height LOW (this is near top-down, not a tall wall).

Generate 4 barricade variants in a 2x2 grid. Each cell 256x256 pixels.
Total output: 512x512 pixels. Each is a standalone object dropped
onto a road/alley to block passage.

BARRICADE 1 (top-left): Sandbag wall.
Stacked dirty sandbags (tan/olive), 2-3 rows high, sagging,
one torn bag spilling sand. Improvised, uneven.

BARRICADE 2 (top-right): Junk pile blockade.
Scrap metal, broken furniture, a stripped chair, wooden boards,
rusted sheet metal, a tire — piled across the path. Chaotic.

BARRICADE 3 (bottom-left): Concrete + rebar block.
Broken chunks of concrete with exposed rusty rebar, a toppled
concrete barrier (Jersey barrier), gray with rust streaks.

BARRICADE 4 (bottom-right): Wood pallet + barrel barricade.
Wooden pallets stood up and wired together, two rusty oil drums,
a stretched rope, warning rag tied on. Improvised checkpoint look.

Palette consistent with the ruined city: muted browns, rust orange,
olive, gray concrete. Weathered but readable silhouettes.
Light contact shadow under each pile only (no long baked shadows).
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
```

> **용도 메모**: 길막용이므로 Unity에서 콜라이더 ON. 퀘스트/진행 플래그로
> 비활성화(제거) 가능하게 배치. 변형 4종을 섞어 동선 차단.

---

## 6. 완전체 건물 (진입 불가 — 배경/외곽 채움)

> 들어갈 수 없는 **닫힌 상가/건물 덩어리**. 레퍼처럼 셔터 내린 점포 + 빈 간판(글자 없음).
> 맵 외곽과 빈 구역을 채워 "도시 속 공터"처럼 보이게 함.
> 지붕이 덮여 내부가 안 보이는 **완전체 블록** (open-roof 룸과 구분).

### 프롬프트 6-1: 닫힌 상가 건물 세트 (셔터 점포)

```
[공통 스타일 블록]

Top-down 80° view SEALED storefront buildings — closed, NON-enterable
shop blocks for a ruined Korean city (matches the attached top-down
map reference: shutter shops with signboards).
These are solid roofed blocks: you see the ROOFTOP plus the lower
SOUTH facade (shutter + sign) because of the 80° forward tilt.
The roof is closed/opaque — interior NOT visible.

Generate 3 building blocks stacked in a 1x3 vertical strip.
Each block roughly 4 tiles wide x 2 tiles deep.
Each cell 768x384 pixels. Total output: 768x1152 pixels.

Shared look: aged concrete row-shop building, flat rooftop with
AC units / vents / water tanks / debris on top, a worn parapet edge.
The south-facing ground floor shows a CLOSED metal roller SHUTTER
(graffiti, rust, dents) and a faded BLANK SIGNBOARD (no text).

BLOCK 1 (top): two shops, two blank signboards.
  Shutters down, one half-collapsed awning.
BLOCK 2 (middle): two shops, two blank signboards.
  Cracked, vines creeping up one corner.
BLOCK 3 (bottom): single wider shop, one blank signboard.
  Boarded window beside the shutter, stacked crates outside.

Signboards: short, blocky, slightly worn frames but EMPTY — no text or
letters (real shop names added later in engine).
Muted palette: gray concrete, rust, dark shutters, dim warm sign glow.
Weak even lighting (runtime light added later).
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
```

### 프롬프트 6-2: 폐허 건물 / 외곽 채움 덩어리 3종

```
[공통 스타일 블록]

Top-down 80° view NON-enterable background structures to fill the map
edges of a ruined city (matches the attached reference). Roofed solid
blocks, interior not visible. Slight forward tilt shows rooftop + a
sliver of the near (south) wall.

Generate 3 variants in a 1x3 vertical strip. Each cell 512x384 pixels.
Total output: 512x1152 pixels.

VARIANT 1 (top): Collapsed building / rubble mound.
Partially fallen concrete building, caved-in roof, exposed rebar,
a pile of broken slabs and bricks. Dark and dead.

VARIANT 2 (middle): Intact sealed warehouse.
Larger flat-roof concrete box, no entrances on this side,
roof vents and a rusty water tank, faded paint streaks.

VARIANT 3 (bottom): Boarded shophouse.
Two-story narrow building, all openings boarded/bricked up,
hanging broken sign (no readable text), AC units, satellite dish.

These are pure background mass (collider ON, never enterable).
Muted ruined-city palette, weak even lighting.
Background: solid DARK GRAY flat fill (#3a3d40), the SAME deep gray on
every sheet. NOT white, NOT transparent. (Keyed out later in engine.)
```

> **용도 메모**: 6-1/6-2 모두 진입 불가 → 콜라이더 풀 점유. open-roof 플레이
> 가능한 방(전당포 등)과 시각적으로 구분되게 **지붕이 덮인 덩어리**로 렌더.
> 맵 외곽·빈 구역·골목 사이를 채워 도시 밀도 연출.

---

## 7. 레이드 출입문 (추출/출발 게이트)

> 안전구역 외곽의 보강된 출구. 여기서 폐도시(레이드)로 나가고 들어옴.
> 상호작용 오브젝트(추출 트리거). 콘크리트 외벽/펜스 어느 쪽에도 이어붙일 수 있게.

### 프롬프트 7-1: 레이드 출입문 (동벽, 세로)

```
[공통 스타일 블록]

Top-down 80° view RAID DEPARTURE / EXTRACTION GATE on the EAST perimeter
(right side, vertical wall running up-down). Near top-down tilt: top of
the wall + a sliver of the inner (left) face.

Output: 384x768 pixels (1 tile wide x ~3 tiles tall, vertical).

A fortified GAP in the compound perimeter — the way out to the city.
From inside (left) to outside (right):
- Reinforced opening framed by stacked IBC tote tanks (plastic cubes in
  rusty metal cages) and stacked wooden crates on both sides.
- Sandbags piled at the base of the opening.
- A raised swing-arm checkpoint pole / rope barrier across the gap.
- A small BLANK signboard with a painted arrow only (no text).
- One utility light on a pole above the opening (off).
- Through the gap on the right: a short strip of dark cracked road
  leading off toward the ruined city (fades to dark).

Palette: gray concrete, rust, white totes, tan sandbags, dark road.
Tiles seamlessly into the vertical east fence/wall above and below.
NO text, NO letters, NO numbers (sign is a blank plate + arrow).
Background: solid DARK GRAY flat fill (#3a3d40). NOT white, NOT transparent.
```

> **용도 메모**: 추출 트리거(SceneTransitionManager). 닫힘/열림 2상태가 필요하면
> 열린 개구부 버전 별도 생성. 남벽(가로) 배치가 필요하면 가로 버전도 추가.

---

## 8. 스카이박스 / 맵 바깥 배경

> 탑다운이라 진짜 스카이박스는 없음 → **플레이 구역 바깥을 채우는 원경 배경 레이어**.
> 펜스/건물 너머로 보이는 폐도시. 어둡게 깔아 안전구역(밝은 초점)을 부각.
> 꽉 채우는 이미지 → 키 아웃/투명 불필요(전체가 콘텐츠).

### 프롬프트 8-1: 폐도시 원경 배경 (외곽 둘러싸기)

```
[폐도시 탑다운 맵 레퍼런스 이미지 첨부 — 특히 우상단 석양 스카이라인]

Art style: FLAT cel-shaded, muted, atmospheric top-down. NO painterly,
NO 3D-render, NO text/letters/numbers.

A large BACKGROUND layer for the area OUTSIDE the playable safehouse,
seen beyond the fence/buildings. High oblique top-down view of a dead
Korean city receding into distance: rows of broken concrete buildings,
collapsed rooftops, a few tall ruined towers far back, empty roads, dark
rubble. Very dim. A faint orange-purple dusk glow only along the far
horizon, fading to deep darkness toward the edges.

Output 1024x1024. Keep it DARK and low-contrast so a lit compound placed
on top reads as the focal point. Solid full-bleed image (no transparency).
Dark desaturated palette: gray-brown ruins, faint dusk orange-purple horizon.
```

### 프롬프트 8-2: 아웃오브바운즈 어둠 타일 (반복용)

```
[공통 스타일 블록]

Top-down near-overhead DARK out-of-bounds ground, seamlessly TILEABLE in
all directions. Cracked rubble, scattered debris, dead grass patches, very
low light, mostly shadow. Used to fill the void around the compound.
Output 512x512, edges wrap for infinite tiling.
Dark desaturated grays/browns. NO text. Solid full image (no transparency).
```

---

## 8.5 바닥 데칼 (브러시 페인팅용)

> **벽/오브젝트 데칼(9번)과 구분.** 이건 맵 에디터에서 **브러시로 바닥에 칠하는** 용도.
> 핵심 차이: **굵은 외곽 아웃라인 없음**, 가장자리는 **불규칙·페더(soft)** 처리 →
> 여러 번 덧칠/겹쳐도 이음새 없이 섞임. 탑다운 거의 평면(바닥) 시점.
> 배경은 짙은 회색(#3a3d40) → 키 아웃 (브러시 툴이 알파 필요하면 transparent로).

### 프롬프트 8.5-A: 젖은/오염 바닥 데칼 (얼룩 계열)

```
[공통 스타일 블록은 쓰되, "bold outline" 항목은 무시]

Top-down ground DECAL sheet for BRUSH painting onto floors in a map editor.
12 decals in a 4x3 grid, output 1024x768. Each decal isolated with empty
space around it. IMPORTANT: NO bold containing outline; edges are RAGGED
and softly faded so strokes blend seamlessly when overlapped.

Wet/stain variety: dark oil/grease pool, spreading water puddle (blue-gray
sheen), mud smear, dark damp blotch, dripping liquid trail, soot/scorch
burn mark, greasy tire skid streak, chemical stain, blood-like dark splatter,
sewage dark patch, wet leaf mush, slick reflective patch.

FLAT cel-shaded fills (3-4 muted color steps) but with soft ragged edges,
no hard silhouette outline. Muted realistic dirt colors.
NO text. Background: solid DARK GRAY flat fill (#3a3d40). NOT transparent.
```

### 프롬프트 8.5-B: 마른/잔해 바닥 데칼 (흙·먼지 계열)

```
[공통 스타일 블록은 쓰되, "bold outline" 항목은 무시]

Top-down ground DECAL sheet for BRUSH painting onto floors.
12 decals in a 4x3 grid, output 1024x768. Each isolated, empty space around.
NO bold outline; edges RAGGED and softly faded for seamless blending.

Dry/debris variety: dirt/dust drift patch, gravel scatter, sand patch,
cracked-earth patch, scattered pebbles, dead-grass tuft scatter, dry leaf
litter, rubble/concrete crumb scatter, ash/dust film, faint footprints,
weed sprouts in dirt, light scratch/scuff marks.

FLAT cel-shaded fills (3-4 muted earth steps) with soft ragged edges,
no hard silhouette outline. Brown/tan/gray dirt palette.
NO text. Background: solid DARK GRAY flat fill (#3a3d40). NOT transparent.
```

---

## 9. 낡음 데칼 시트 (벽·문·바닥 공용 오버레이)

> **방식 확정 (2026-06-03)**: 셰이더 입력 아님 → **일반 데칼 이미지**.
> 색 입힌 데칼을 짙은 회색(#3a3d40) 배경으로 뽑아 엔진에서 키 아웃 →
> 베이스 위에 오버레이 스프라이트로 겹쳐쓰기. 같은 데칼을 모든 표면에 재사용.
> 재질별 4시트(A~D)로 분리, 각 12종.

### 프롬프트 9-A: 녹 / 금속 부식

```
[공통 스타일 블록]

Top-down WEATHERING DECAL sheet — RUST & metal corrosion overlays.
Each decal isolated on the dark gray background with empty space around it
(so each can be cut out and reused over walls/doors/props in-engine).

Grid of 12 decals, 4 columns x 3 rows. Output 1024x768.
Rust variety: thin rust drip streaks (short/long/branching), wide rusty
bleed stains, orange-brown rust crust patches (small/med/large), corroded
flaking metal with dark pitting, rust rings around bolts/rivets, heavy
corner rust eating through.

Muted rust palette: orange, red-brown, dark brown, ochre. FLAT cel-shaded
(3-4 steps, hard edges, thin outline). NO gradients, NO airbrush.
NO text. Background: solid DARK GRAY flat fill (#3a3d40). NOT transparent.
```

### 프롬프트 9-B: 물때 / 곰팡이 / 얼룩

```
[공통 스타일 블록]

Top-down WEATHERING DECAL sheet — WATER STAINS, MOLD & grime overlays.
Each decal isolated on the dark gray background with empty space around it.

Grid of 12 decals, 4 columns x 3 rows. Output 1024x768.
Variety: dark vertical water-streak drips (thin/wide/clustered), spreading
damp blotches, black mold creeping patches, green-black mildew clusters,
soot/smoke smudge clouds, greasy dark stains, dripping liquid trails,
pale mineral/efflorescence crust.

Muted palette: dark gray-brown, black-green mold, dirty olive, faded white
crust. FLAT cel-shaded (3-4 steps, hard edges, thin outline).
NO gradients, NO airbrush. NO text.
Background: solid DARK GRAY flat fill (#3a3d40). NOT transparent.
```

### 프롬프트 9-C: 균열 / 구조 손상

```
[공통 스타일 블록]

Top-down WEATHERING DECAL sheet — CRACKS & structural damage overlays.
Each decal isolated on the dark gray background with empty space around it.

Grid of 12 decals, 4 columns x 3 rows. Output 1024x768.
Variety: thin hairline cracks, branching crack networks, big jagged
cracks, concrete spall chunks with exposed inner material, bullet/impact
pock clusters, chipped broken edges, a hole punched through, crumbled
corner with rubble bits.

Muted palette: dark crack lines, gray inner concrete, brown exposed brick,
small debris specks. FLAT cel-shaded (3-4 steps, hard edges, thin outline).
NO gradients, NO airbrush. NO text.
Background: solid DARK GRAY flat fill (#3a3d40). NOT transparent.
```

### 프롬프트 9-D: 이끼 / 잡초 / 흙먼지

```
[공통 스타일 블록]

Top-down WEATHERING DECAL sheet — MOSS, weeds & dirt overlays.
Each decal isolated on the dark gray background with empty space around it.

Grid of 12 decals, 4 columns x 3 rows. Output 1024x768.
Variety: green moss patches (small/large), creeping vines, weed tufts
growing from a crack, dead-grass clumps, dirt/mud smear clouds, scattered
pebble + leaf debris, a small puddle stain, sand/dust drift.

Muted palette: mossy green, olive, dead-grass tan, brown dirt. FLAT
cel-shaded (3-4 steps, hard edges, thin outline). NO gradients, NO airbrush.
NO text. Background: solid DARK GRAY flat fill (#3a3d40). NOT transparent.
```

---

## 10. 벽 부착 프랍 — 4방향 세트 (범용 템플릿)

> 벽에 기대는 프랍을 방향별로 뽑는 템플릿. `[PROP]` 자리만 교체.
> 둥근/대칭 프랍은 생략(1방향이면 충분).

### 프롬프트 10-1: 프랍 4방향 템플릿

```
[해당 프랍 이미지 첨부 — 스타일/팔레트/스케일 락]

Art style: FLAT clean cel-shaded top-down game asset, 3-4 color steps,
hard edges, soft dark outline. NO gradients, NO airbrush, NO noise, NO
painterly, NO 3D-render. NO text, NO letters, NO numbers.
Match the attached prop's exact style, palette, and scale.

Top-down 80° view of ONE object — [PROP] — drawn in 4 ORIENTATIONS for
placing against different walls. Same object, same size, only rotated.
2x2 grid, each cell 256x256, total 512x512. Each isolated.

TOP-LEFT  — FRONT (faces DOWN/south, against the NORTH wall): top + front.
TOP-RIGHT — BACK  (faces UP/north,  against the SOUTH wall): top + back.
BOTTOM-LEFT  — faces RIGHT/east (against the WEST wall): top + right side.
BOTTOM-RIGHT — faces LEFT/west  (against the EAST wall): top + left side.

Keep the 80° tilt consistent in all 4 (mostly top, a little of the facing side).
Background: solid DARK GRAY flat fill (#3a3d40). NOT white, NOT transparent.
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
9. [ ] **5-1** 바리게이트 4종 (길막 임시)
10. [ ] **6-1** 닫힌 상가 건물 세트 (셔터 점포)
11. [ ] **6-2** 폐허/외곽 채움 덩어리 3종
11.5 [ ] **7-1** 레이드 출입문 (추출 게이트)
12. [ ] **9-A** 낡음 데칼 — 녹/부식
13. [ ] **9-B** 낡음 데칼 — 물때/곰팡이
14. [ ] **9-C** 낡음 데칼 — 균열/손상
15. [ ] **9-D** 낡음 데칼 — 이끼/잡초/흙

> 바닥(1) → 펜스 직선(2-1) → 코너(2-2) → 게이트(2-4) → 패치(2-3) → 기둥(3-1) → 프랍(4-1)
> → 바리게이트(5-1) → 배경 건물(6-1, 6-2)
