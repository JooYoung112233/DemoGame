# Isometric Art Prompt Guide

AI 이미지 생성용 프롬프트 가이드. 모든 건물/소품 에셋에 동일하게 적용.

---

## 기본 규격

| 항목 | 값 |
|------|-----|
| 투영 | True isometric (30° from horizontal, 120° between all axes) |
| 카메라 | front-left corner를 바라봄 |
| 타일 비율 | 2:1 (가로:세로, 128×64px 다이아몬드) |
| 배경 | 마젠타 단색 (#FF00FF) → 후처리 투명화 |
| 렌더링 | Clean pixel art, bold 1-2px outlines, 3-4 color steps per material |
| 그림자 | 완전 금지 — flat diagram style |

---

## 아트 스타일 (레퍼런스: 한강아파트 107동)

| DO | DON'T |
|----|-------|
| 굵은 외곽선 (1-2px dark outline) | 외곽선 없는 부드러운 렌더링 |
| 면 단위 채색 (flat, 3-4 steps) | 그라데이션, 디더링 과다 |
| 큰 단위 디테일 | 미세한 크랙/먼지 입자 |
| 실루엣만으로 인식 가능 | 확대해야 보이는 디테일 |

---

## 색상 팔레트

| 재질 | 색상 |
|------|------|
| 콘크리트 외벽 | cream-beige, muted gray |
| 금속 | gunmetal gray, dark steel |
| 녹/부식 | oxidized brown-orange |
| 목재 | dark walnut brown, warm brown |
| 유리/창문 | dark teal-black |

---

## 건물별 재질 가이드

| 건물 | 벽 재질 | 바닥 재질 | 프롬프트 키워드 |
|------|---------|-----------|----------------|
| ★ 편의점 거점 (8×5) | 콘크리트 + 타일 | 밝은 리놀륨 | concrete with tile accent, light linoleum |
| 전당포 (7×4) | 콘크리트 | 콘크리트 타일 | weathered concrete, concrete tile |
| 수리점 (4×4) | 적갈색 벽돌 | 기름때 콘크리트 | aged red-brown brick, oil-stained concrete |
| 블랙마켓 (3×3) | 합판 + 방수포 | 팔레트 나무 | plywood boards, tarp, pallet wood floor |
| 의료소 (6×4) | 흰색 타일벽 | 밝은 리놀륨 | cracked white tile, light linoleum |
| 가구점 (5×5, 2층) | 목재 패널 | 나무 마루 | wood panel wall, hardwood plank floor |

---

## 세계관 톤

프롬프트 필수 문구:
```
Environment tone: abandoned and weathered — NOT clean, NOT new, NOT maintained.
```

- 한국 도시 폐건물 (abandoned Korean urban)
- 간판: 빈 프레임만 or 지워진 흔적. 한국어 텍스트 직접 쓰지 않음
- NO text, NO signs, NO Korean characters on any wall

---

## 생성 방식: 4면 박스 → 분리

**건물은 4면 벽 + 바닥 완성 박스로 한 번에 뽑고, 후처리로 내부/외벽을 분리한다.**

이유: 내부와 외벽을 따로 뽑으면 크기/각도가 안 맞음. 한 이미지에서 분리해야 100% 일치.

### 생성 순서

```
① 4면 박스 (벽 + 바닥, 지붕 없음)  ← 격자 + 아파트 레퍼런스 첨부
   ↓ 후처리로 분리
   ├─ 내부 (뒷벽 L자 + 바닥) — 앞벽 제거, 가려진 부분 AI로 보완 요청
   └─ 외벽 (앞벽 V자) — 뒷벽 제거
   ↓ [4면 박스 이미지] 첨부
② 소품 (스프라이트 시트)
   ↓ [외벽 이미지] 첨부
③ 문 (열림/닫힘)
   ↓ [4면 박스 이미지] 첨부
④ 지붕
```

### 분리 후처리

1. **내부 추출**: 앞벽 2개 지우기 → 가려졌던 바닥/벽 부분을 AI에게 보완 요청
2. **외벽 추출**: 뒷벽 + 바닥 지우기 → V자 앞벽만 남김

---

## 성공한 프롬프트 템플릿

### 4면 박스 (기본 — 모든 건물의 시작점)

아래 프롬프트가 실제로 잘 작동한 검증된 템플릿. **첨부: ① 격자 이미지 + ② 아파트 레퍼런스**

```
Isometric pixel art of an abandoned [건물명] building — COMPLETE 4-WALL BOX, no roof.

*** REFERENCE IMAGE #1 — ISOMETRIC GRID (MOST IMPORTANT) ***
- The attached isometric grid is the ABSOLUTE AUTHORITY for all angles.
- EVERY line in the building MUST align to this grid. No exceptions.
- Floor tile edges = grid lines. Wall edges = grid lines. ALL diagonals = grid lines.
- If any edge does NOT follow the grid, it is WRONG. Redraw it on the grid.
- Do NOT estimate or approximate the angle — TRACE the grid lines exactly.
- TRUE ISOMETRIC: 30° from horizontal, 120° between axes.
- Floor diamond tiles: width-to-height ratio exactly 2:1.
- Reference games: SimCity 2000, Habbo Hotel, RollerCoaster Tycoon.

*** REFERENCE IMAGE #2 — ART STYLE (Korean apartment pixel art) ***
- Match this art style EXACTLY: crisp pixel art, bold 1-2px dark outlines, flat cel-shaded.
- Same level of detail, same outline thickness, same color depth (3-4 steps per material).
- Do NOT add more detail than the apartment reference. Do NOT make it more realistic.

*** COMPLETE BOX — ALL 4 WALLS + FLOOR, NO ROOF ***
- Draw a full closed building with all 4 walls and floor visible from above.
- NO roof, NO ceiling — open top, interior floor visible from above.
- Camera faces the front-left corner.
- All 4 walls same height, same material, same style.

*** FLOOR GRID SIZE — COUNT THE TILES ***
- The floor MUST have exactly [N] columns × [M] rows of diamond tiles.
- The building is [WIDE/SQUARE] — [비율 설명].
- The floor area occupies 60% of the building. Walls are short — 1/3 of floor width height.

*** EMPTY ROOM — NO FURNITURE, NO PROPS, NO OBJECTS ***
- The interior is COMPLETELY EMPTY. Only walls and floor.
- NO shelves, NO counters, NO tables, NO boxes, NO safes, NO display cases.
- NO items on the floor. NO items on the walls.
- NO text, NO signs, NO Korean characters on any wall.
- NOTHING inside the room. Just bare walls and bare floor.

*** DOOR OPTION (choose one) ***
[A — NO DOOR]: NO door, NO door frame. All walls solid.
[B — WITH DOOR]: One empty door frame on the [FRONT-LEFT/FRONT-RIGHT] wall. 128px base width, 154px tall. Empty hole showing magenta.

STRICT STYLE LOCK — NOT photorealistic, NOT painterly, NOT hyper-detailed.
Color palette: muted concrete grays, cream-beige walls, oxidized brown, dark teal-black.
NO fine cracks, NO dust particles, NO micro-texture. Only large-shape weathering.
Every surface is flat uniform color blocks only.

ABSOLUTELY NO LIGHTING OR SHADOW:
- ZERO shadows anywhere. ZERO ambient occlusion. ZERO glow.
- Each surface is ONE flat color. No light source — flat diagram style.

Floor: [바닥 재질].
All walls: [벽 재질].
Environment tone: abandoned and weathered — NOT clean, NOT new, NOT maintained.

Output image size: [W]×[H] pixels. Building centered with 20% margin on all sides. NOTHING may be cropped.

Solid magenta background (#FF00FF). Fill ALL empty space with flat #FF00FF magenta — no gradients, no shadows on background.
```

### 전당포 (7×4) — 검증 완료

```
Isometric pixel art of an abandoned pawnshop building — COMPLETE 4-WALL BOX, no roof.

*** REFERENCE IMAGE #1 — ISOMETRIC GRID (MOST IMPORTANT) ***
- The attached isometric grid is the ABSOLUTE AUTHORITY for all angles.
- EVERY line in the building MUST align to this grid. No exceptions.
- Floor tile edges = grid lines. Wall edges = grid lines. ALL diagonals = grid lines.
- If any edge does NOT follow the grid, it is WRONG. Redraw it on the grid.
- Do NOT estimate or approximate the angle — TRACE the grid lines exactly.
- TRUE ISOMETRIC: 30° from horizontal, 120° between axes.
- Floor diamond tiles: width-to-height ratio exactly 2:1.
- Reference games: SimCity 2000, Habbo Hotel, RollerCoaster Tycoon.

*** REFERENCE IMAGE #2 — ART STYLE (Korean apartment pixel art) ***
- Match this art style EXACTLY: crisp pixel art, bold 1-2px dark outlines, flat cel-shaded.
- Same level of detail, same outline thickness, same color depth (3-4 steps per material).
- Do NOT add more detail than the apartment reference. Do NOT make it more realistic.

*** COMPLETE BOX — ALL 4 WALLS + FLOOR, NO ROOF ***
- Draw a full closed building with all 4 walls and floor visible from above.
- NO roof, NO ceiling — open top, interior floor visible from above.
- Camera faces the front-left corner.
- All 4 walls same height, same material, same style.

*** FLOOR GRID SIZE — COUNT THE TILES ***
- The floor MUST have exactly 7 columns × 4 rows of diamond tiles.
- Count them: 7 diamonds across (left-right), 4 diamonds deep (front-back).
- The building is WIDE and SHALLOW — wider than it is deep.
- The floor area occupies 60% of the building. Walls are short — 1/3 of floor width height.

*** EMPTY ROOM — NO FURNITURE, NO PROPS, NO OBJECTS ***
- The interior is COMPLETELY EMPTY. Only walls and floor.
- NO shelves, NO counters, NO tables, NO boxes, NO safes, NO display cases.
- NO items on the floor. NO items on the walls.
- NO text, NO signs, NO Korean characters on any wall.
- NOTHING inside the room. Just bare walls and bare floor.

*** DOOR — FRONT-LEFT WALL ONLY ***
- One empty door frame on the FRONT-LEFT wall (long wall facing camera).
- Door frame: 128px base width, 154px tall. Empty hole showing magenta through it.
- NO door on any other wall. All other walls are solid.

STRICT STYLE LOCK — NOT photorealistic, NOT painterly, NOT hyper-detailed.
Color palette: muted concrete grays, cream-beige walls, oxidized brown, dark teal-black.
NO fine cracks, NO dust particles, NO micro-texture. Only large-shape weathering.
Every surface is flat uniform color blocks only.

ABSOLUTELY NO LIGHTING OR SHADOW:
- ZERO shadows anywhere. ZERO ambient occlusion. ZERO glow.
- Each surface is ONE flat color. No light source — flat diagram style.

Floor: cracked concrete tile, faded and dirty.
All walls: aged concrete with peeling paint.
Environment tone: abandoned and weathered — NOT clean, NOT new, NOT maintained.

Output image size: 1280×720 pixels. Building centered with 20% margin on all sides. NOTHING may be cropped.

Solid magenta background (#FF00FF). Fill ALL empty space with flat #FF00FF magenta — no gradients, no shadows on background.
```

---

## 프롬프트 제출 전 체크리스트 (필수)

| # | 항목 | 필수 문구/키워드 |
|---|------|-----------------|
| 1 | 격자 레퍼런스 첨부 | `ISOMETRIC GRID`, `ABSOLUTE AUTHORITY`, `TRACE the grid lines` |
| 2 | 그림체 레퍼런스 첨부 | `ART STYLE`, `Korean apartment pixel art` |
| 3 | 아이소 각도 | `30° from horizontal, 120° between axes` |
| 4 | 2:1 타일 비율 | `width-to-height ratio exactly 2:1` |
| 5 | 그리드 사이즈 | `N columns × M rows of diamond tiles` |
| 6 | 바닥/벽 비율 | `floor 60%`, `walls 1/3 of floor width` |
| 7 | 스타일 락 | `STRICT STYLE LOCK`, `NOT photorealistic` |
| 8 | 컬러 팔레트 | `muted concrete grays, cream-beige walls, oxidized brown, dark teal-black` |
| 9 | 그림자 금지 | `ZERO shadows`, `ONE flat color`, `No light source` |
| 10 | 문 옵션 | 있음/없음 명시 + 위치 + 크기(128×154) |
| 11 | 폐허 톤 | `abandoned and weathered — NOT clean, NOT new` |
| 12 | 마젠타 배경 | `#FF00FF`, `no gradients, no shadows on background` |
| 13 | 출력 사이즈 | `Output image size: WxH pixels` |
| 14 | 크롭 금지 | `NOTHING may be cropped`, `margin` |
| 15 | 텍스트 금지 | `NO text, NO signs, NO Korean characters` |
| 16 | 빈 방 | `COMPLETELY EMPTY`, `no furniture, no props` |
