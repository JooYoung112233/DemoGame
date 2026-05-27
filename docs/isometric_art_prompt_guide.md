# Isometric Art Prompt Guide

AI 이미지 생성용 프롬프트 스타일 가이드. 모든 건물/소품 에셋에 동일하게 적용.

---

## 기본 규격

| 항목 | 값 |
|------|-----|
| 투영 | 2:1 isometric (26.57° from horizontal) |
| 카메라 | 45° top-down diagonal, facing front-left corner |
| 픽셀 비율 | 가로 2px : 세로 1px (모든 대각선 엣지) |
| 조명 | Top-left |
| 배경 | Transparent (PNG) |
| 렌더링 | Clean pixel art, no anti-aliasing, **bold readable shapes** |
| 색상 | **3-4 shade color ramps per material** (NOT 6+) |

## Unity 그리드 & 사이즈 규격

| 단위 | 픽셀 사이즈 |
|------|------------|
| **1 grid unit (1타일)** | 128×64 px (isometric diamond) |
| **벽 높이 (1층)** | 192 px (3타일 높이) |

### 스케일 기준: 캐릭터 = 1타일

**1×1 그리드 = 캐릭터 1명이 서는 최소 공간.**

| 항목 | 값 |
|------|-----|
| 캐릭터 높이 | ~70px (치비 픽셀아트) |
| 캐릭터 발 폭 | ~35px |
| 1타일 (128×64) | 캐릭터 1명 + 여유 공간 |
| 5×5 건물 | 최대 25명 동시 배치 가능한 넓이 |

> 모든 건물/소품 사이즈는 "캐릭터 몇 명 들어가나"로 검증.
> 1×1 소품 = 캐릭터 1명분 차지, 2×1 카운터 = 캐릭터 2명분 차지.

프롬프트 필수 문구:
```
SCALE REFERENCE:
- Each floor grid cell (128x64px isometric diamond) fits one chibi pixel art 
  character (~70px tall) standing comfortably with space around them.
- 1x1 grid = minimum space for 1 character. Size all objects relative to this.
```

### 건물 사이즈 계산

| 건물 크기 | 그리드 | 캐릭터 수용 | 이미지 최소 해상도 (여백 포함) |
|-----------|--------|------------|-------------------------------|
| 소형 (편의점, 전당포) | 5×5 | 25명분 | 약 1280×960 px |
| 중형 (사무실, 모텔) | 7×6 | 약 1664×1152 px |
| 대형 (병원, 역사) | 10×8 | 약 2304×1536 px |

> 프롬프트에 `Output image size: [W]x[H] pixels minimum` 반드시 명시.
> 소품은 그리드 footprint × 128px 기준으로 산출.

### 이미지 잘림 방지 (필수)

프롬프트에 반드시 포함:
```
FRAMING — CRITICAL, DO NOT IGNORE:
Output image size: 2048x1536 pixels minimum.
The building must be CENTERED in the canvas.
Leave at least 20% empty space on ALL sides — top, bottom, left, right.
The BOTTOM of the building (foundation, floor edge) must be fully visible.
NOTHING may be cropped. If any part touches the image edge, the image is WRONG.
Zoom OUT if needed — a smaller building with full margins is better than 
a cropped building that fills the canvas.
```

### 소품 사이즈

| footprint | 이미지 사이즈 |
|-----------|--------------|
| 1×1 | 256×256 px |
| 2×1 | 384×320 px |
| 2×2 | 512×384 px |

---

## 아트 스타일 (레퍼런스: 한강아파트 107동)

핵심은 **"읽기 쉬운 픽셀아트"** — 과도한 디테일이 아닌 명확한 실루엣과 대비.

| DO (따라야 할 것) | DON'T (피해야 할 것) |
|-------------------|---------------------|
| 굵은 외곽선 (1-2px dark outline) | 외곽선 없는 부드러운 렌더링 |
| 면 단위 채색 (flat shading with 3-4 steps) | 그라데이션, 디더링 과다 |
| 큰 단위 디테일 (창문=사각 블록, 벽돌=패턴) | 미세한 크랙/먼지 입자 하나하나 |
| 색상 대비로 구조 구분 (벽/바닥/소품 명확 분리) | 전체적으로 비슷한 톤으로 뭉개짐 |
| 오브젝트 실루엣만으로 뭔지 알 수 있음 | 확대해야 뭔지 보이는 수준의 디테일 |

### 디테일 일관성 규칙

**프롬프트에서 오브젝트를 묘사할 때:**
- 형용사 최소화 — "glass display counter"지 "thick tempered glass panels with spiderweb fractures"가 아님
- 한 오브젝트당 묘사 1줄 이내
- 재질/상태 묘사 금지 — "rusted", "cracked", "dusty" 같은 단어는 스타일 문구에서만 처리
- 구체적 소품 나열 금지 — "watches, rings, phones on display" → "display counter"로 충분

**나쁜 예:** `L-shaped glass display counter with thick tempered glass panels, some cracked with spiderweb fractures, dark wood base cabinet with locked sliding doors, dusty velvet pad inside, a few forgotten items visible through glass: tarnished watch, old ring, broken phone`

**좋은 예:** `L-shaped glass display counter with wood base and cash register on top`

프롬프트 필수 문구:
```
Art style: crisp readable pixel art matching the attached reference image EXACTLY. 
STRICT STYLE LOCK — do NOT increase detail beyond the reference.
Bold 1-2px dark outlines on all structural edges. Flat cel-shaded surfaces 
with only 3-4 color steps per material. Large simple shapes only.
NOT photorealistic, NOT painterly, NOT hyper-detailed, NOT illustrated.
If in doubt, use FEWER details and SIMPLER shapes.
Prioritize silhouette clarity over surface detail.
```

## 색상 팔레트

| 재질 | 색상 |
|------|------|
| 콘크리트 외벽 | cream-beige, muted gray |
| 금속 | gunmetal gray, dark steel |
| 녹/부식 | oxidized brown-orange |
| 목재 | dark walnut brown, warm brown |
| 유리/창문 | dark teal-black |
| 포인트 | faded burgundy, desaturated olive green |
| 먼지/오염 | muted yellow-gray film |

## 건물별 재질 가이드

각 건물은 **주 재질**로 차별화. 한 건물에 여러 재질 혼합 가능.

| 건물 | 벽 재질 | 바닥 재질 | 포인트 요소 | 프롬프트 키워드 |
|------|---------|-----------|------------|----------------|
| 컨테이너 (3×5) | 골판 금속 | 콘크리트 슬래브 | 리벳, 용접 자국 | corrugated metal, riveted seams |
| 전당포 (7×4) | 콘크리트 | 어두운 리놀륨 타일 | 철창, 간판 프레임 | weathered concrete, linoleum tile |
| 수리점 (3×4) | 적갈색 벽돌 | 기름때 콘크리트 | 환풍구, 공구 페그보드 | exposed red-brown brick, oil-stained concrete |
| 블랙마켓 (3×3) | 합판 + 방수포/천막 | 깔판(팔레트) 나무 | 전선 줄, 임시 조명 | plywood boards, tarp covering, pallet wood floor |
| 의료소 (5×4) | 흰색 타일벽 (깨진 곳 콘크리트 노출) | 밝은 리놀륨 | 십자 마크 흔적, 형광등 | cracked white tile wall, light linoleum, faded red cross |
| 가구점 (6×5, 2층) | 목재 패널 | 나무 마루 | 목재 기둥, 계단 | wood panel wall, hardwood plank floor, wooden pillar |

> **혼합 예시**: 수리점 벽 = 아래 절반 벽돌 + 위 절반 콘크리트 → `lower half exposed brick, upper half raw concrete`
> 
> 건물 성격에 맞게 자유롭게 조합하되, **주 재질은 위 표를 따르고** 나머지는 서브로 섞는다.

---

## 세계관 톤

- 한국 도시 폐건물 (abandoned Korean urban)
- 좀비/종말 아님 — "버려진 도시가 밤에 열리는" 현상
- 먼지, 균열, 녹, 벗겨진 페인트, 덩굴 (대형 디테일만, 미세 입자 X)
- 간판/텍스트: 빈 간판 프레임만 남기거나, 한국어 텍스트 지워진 흔적
- **모든 건물은 폐허** — 깨끗하거나 새것처럼 보이면 안 됨

프롬프트 필수 문구 (모든 건물/소품에 포함):
```
Environment tone: abandoned and weathered — NOT clean, NOT new, NOT maintained.
All surfaces show age: warped wood, cracked concrete, rust, water stains, missing pieces.
```

---

## 모듈 분리 규칙 (중요)

**각 모듈은 반드시 별도 이미지로 생성한다. 하나의 이미지에 여러 모듈을 합치지 않는다.**

프롬프트 첫 줄에 반드시 포함:
```
Generate ONLY the [모듈명] — nothing else. 
Do NOT include [제외 항목]. This is ONE piece of a modular building set.
```

| 모듈 | 이 이미지에 있는 것 | 이 이미지에 없는 것 |
|------|---------------------|---------------------|
| 내부 (컷어웨이) | 뒷벽 2면 + 바닥 | 앞벽, 지붕, 소품, 문 |
| 소품 시트 | 가구/오브젝트만 나열 | 벽, 바닥, 건물 구조 |
| 문 | 문 1개 (열림/닫힘) | 벽, 바닥, 건물 구조 |
| 외벽 | 앞면 벽 2면 (V자) + 빈 문틀 | 뒷벽, 바닥, 지붕, 소품, 문 |
| 지붕 | 지붕 1장 | 벽, 바닥, 소품 |

---

## 카메라 방향 (입구 위치)

건물마다 입구 방향을 바꿔서 맵에 변화를 준다. **내부 + 외벽 프롬프트에 방향을 명시**.

### Type A: 앞쪽 입구 (기본)

카메라가 front-left를 바라봄. 뒷벽이 보이고 앞이 열림.

```
Camera facing front-left corner.
BACK walls visible (L-shape at far corner): back-left wall + back-right wall.
FRONT is open (removed for cutaway). Entrance faces the camera.
```

```
[평면도]
      ■ ■ ■ ■        ■ = 뒷벽 (보임)
    ■ · · · · ■      · = 바닥 (보임)
  ■ · · · · · · ■    
    · · · · · ·       ← 앞쪽 열림 (입구)
      · · · ·           카메라 방향 ↗
```

### Type B: 뒤쪽 입구 (반전)

카메라가 front-left를 바라봄. 앞벽이 보이고 뒤가 열림.
입구가 카메라 반대편 → 건물 뒤쪽/윗쪽에 문.

```
Camera facing front-left corner.
FRONT walls visible (V-shape toward camera): front-left wall + front-right wall.
BACK is open (removed for cutaway). Entrance faces AWAY from camera.
```

```
[평면도]
      · · · ·         ← 뒤쪽 열림 (입구)
    · · · · · ·       
  ■ · · · · · · ■    · = 바닥 (보임)
    ■ · · · · ■      ■ = 앞벽 (보임)
      ■ ■ ■ ■          카메라 방향 ↗
```

> **프롬프트에 Type A / Type B 중 하나를 선택해서 명시.**
> 외벽도 방향에 맞춰: Type A → 외벽은 V자(앞) / Type B → 외벽은 L자(뒤)

---

## 프롬프트 템플릿

### A. 내부 (컷어웨이) — ★ 가장 먼저 생성

```
Generate ONLY the interior cutaway — nothing else.
Do NOT include front walls, roof, doors, furniture, or props.

Isometric pixel art cutaway building interior of [건물 설명]. 
Front wall and roof completely removed, dollhouse cross-section view. 
Only BACK-LEFT wall and BACK-RIGHT wall remain (L-shape corner) + floor.

[N]x[M] grid unit footprint (1 grid = 128x64px). 
Single story, wall height 192px. 
Output image size: [W]x[H] pixels minimum.

Exterior details:
- [벽면 디테일 3-4개 — 큰 단위만]
- Faded empty sign board on exterior wall — rusted metal frame, 
  blank surface where a shop sign once hung

Interior floor: [바닥 재질 묘사], square grid visible.
Cut edges show concrete cross-section.

2:1 isometric projection (26.57°), camera facing front-left corner.

Art style: crisp readable pixel art matching the Korean apartment building reference. 
Bold 1-2px dark outlines on all structural edges. Flat cel-shaded surfaces 
with only 3-4 color steps per material — NOT photorealistic, NOT painterly, 
NOT hyper-detailed. Shapes must be clearly readable at 50% zoom. 
Prioritize silhouette clarity over surface detail.

Palette: muted concrete grays, cream-beige walls, oxidized brown, dark teal-black.
Top-left lighting. Transparent background (PNG). 
Strict 2:1 pixel ratio on all isometric edges.
```

### B. 지붕 모듈

```
Isometric pixel art modular rooftop for [건물 설명]. 
Flat concrete roof slab, [N]x[M] grid unit footprint (1 grid = 128x64px), 
diamond shape in top view.
Output image size: [W]x[H] pixels minimum.

Surface: [지붕 위 디테일 2-3개 — 큰 오브젝트만].
Edge: low concrete parapet with [엣지 디테일].
Front-left cut edge shows concrete slab thickness.

2:1 isometric projection (26.57°).

Art style: crisp readable pixel art matching the Korean apartment building reference. 
Bold 1-2px dark outlines on all structural edges. Flat cel-shaded surfaces 
with only 3-4 color steps per material — NOT photorealistic, NOT painterly, 
NOT hyper-detailed. Shapes must be clearly readable at 50% zoom.

Muted palette matching Korean apartment reference. Top-left lighting.
Transparent background (PNG). Strict 2:1 pixel ratio.
```

### C. 내부 파티션 벽

```
Isometric pixel art interior partition wall for [용도 설명]. 
Runs along the [LEFT/RIGHT] isometric axis, [N] grid units long (1 grid = 128x64px), 
wall height 192px.
Output image size: [W]x[H] pixels minimum.

Features: 
- [벽면 디테일 2-3개]
- [문/개구부 유무]

2:1 isometric projection (26.57°).

Art style: crisp readable pixel art matching the Korean apartment building reference. 
Bold 1-2px dark outlines. Flat cel-shaded, 3-4 color steps per material.
NOT photorealistic, NOT hyper-detailed.

Palette: [주요 색상 3개].
Top-left lighting. Transparent background (PNG). Strict 2:1 pixel ratio.
```

### D. 내부 소품/가구

```
Isometric pixel art of [오브젝트 설명] for placing inside a cutaway building.

2:1 isometric projection (26.57°), [NxM] grid footprint (1 grid = 128x64px).
Output image size: [W]x[H] pixels.

Details: [구체적 외형 묘사 3-4줄 — 큰 형태 위주, 미세 디테일 X]

Abandoned Korean interior style.
[주요 색상/재질 3개].

Art style: crisp readable pixel art matching the Korean apartment building reference. 
Bold 1-2px dark outlines. Flat cel-shaded, 3-4 color steps per material.
NOT photorealistic, NOT hyper-detailed. Clear silhouette at 50% zoom.

2:1 isometric projection (26.57°). Top-left lighting.
Transparent background (PNG). Strict 2:1 pixel ratio on all isometric edges.
Every item must face the same 2:1 isometric angle — 
camera facing front-left corner at 45° top-down diagonal.
No item should face a different direction.
```

### E. 바닥 타일 (타일러블)

```
Isometric pixel art floor tile — [바닥 재질 설명].
2:1 isometric projection (26.57°), perfect diamond shape, 128x64 pixels.

Surface: [표면 디테일 — 패턴 위주, 미세 크랙 X].

Tileable edges: seamless on all four sides.

Art style: crisp readable pixel art matching the Korean apartment building reference. 
Flat cel-shaded, 3-4 color steps. NOT photorealistic.

Muted tones matching abandoned Korean building aesthetic.
Top-left lighting. Transparent background (PNG).
```

---

### F. 외벽 (앞면)

카메라는 front-left corner를 바라봄. Type A/B에 따라 방향 다름.
**AI가 방향을 자주 틀리므로, V/L 형태를 반복적으로 강조해야 함.**

```
[내부 이미지 첨부]
Generate ONLY the front wall piece — nothing else.
Do NOT include back walls, floor, roof, doors, furniture, or interior.

STRICT STYLE LOCK — match the attached reference image EXACTLY.

WALL ORIENTATION — READ CAREFULLY:
[Type A인 경우]
This is a V-shape, NOT an L-shape.
The two walls meet at the BOTTOM point CLOSEST to the camera.
They spread APART toward top-left and top-right.
The OPENING faces AWAY from camera (toward top of image).
Do NOT make an L-shape where walls meet at the far corner.
- LEFT wing: bottom center → TOP-LEFT ([N] cells). [디테일]
- RIGHT wing: bottom center → TOP-RIGHT ([M] cells). [디테일]

[Type B인 경우]
This is an L-shape, NOT a V-shape.
The two walls meet at the TOP point FARTHEST from the camera.
They spread APART toward bottom-left and bottom-right.
The OPENING faces TOWARD the camera (toward bottom of image).
Do NOT make a V-shape where walls meet at the near corner.
- LEFT wing: top center → BOTTOM-LEFT ([N] cells). [디테일]
- RIGHT wing: top center → BOTTOM-RIGHT ([M] cells). [디테일]

Door frame: EMPTY rectangular opening, 128px isometric base width, 154px tall. No door in it.
Wall material, height, thickness identical to back walls in reference.

Art style: match reference EXACTLY. Bold 1-2px outlines.
Flat cel-shaded, 3-4 color steps. NOT photorealistic.
2:1 isometric projection (26.57°). Top-left lighting.
Transparent background (PNG). Strict 2:1 pixel ratio.
```

### 문 규격 (외벽 + 문 공통)

| 항목 | 값 |
|------|-----|
| 문틀 폭 | 1 grid cell = 128px isometric base |
| 문틀 높이 | 벽 높이의 80% = 약 154px |
| 문 크기 | 문틀과 동일 (128x154px) |
| 문틀 위치 | 외벽 프롬프트에서 지정 (center, left 등) |

> 외벽 프롬프트에 `Door frame: 128px wide, 154px tall` 명시.
> 문 프롬프트에 `Door size: 128px wide, 154px tall` 동일하게 명시.

### G. 문 (단독 에셋)

```
[외벽 이미지 첨부]
Generate ONLY a door — nothing else.
Do NOT include walls, floor, roof, or interior.

STRICT STYLE LOCK — match the attached wall reference EXACTLY.

DOOR SIZE: 128px isometric base width, 154px tall.
This is a fixed specification — match these dimensions exactly.
The door must fit the door frame in the wall with zero gap and zero overlap.

[문 재질] door. [문 외형 1줄]

Two versions side by side on one image, spaced apart:
LEFT: door closed (flat, fills the frame exactly)
RIGHT: door open (swung 90° inward)

Art style: match reference EXACTLY. Bold 1-2px outlines.
Flat cel-shaded, 3-4 color steps. NOT photorealistic.
2:1 isometric projection (26.57°). Top-left lighting.
Transparent background (PNG). Strict 2:1 pixel ratio.
```

---

## 생성 순서 (필수)

**각 단계의 결과물이 다음 단계의 레퍼런스가 된다. 레퍼런스 체이닝 엄수.**

### 1층 건물

```
① 내부 (컷어웨이)        ← 모든 것의 기준. 레퍼런스 없이 생성.
   ↓ [내부 이미지] 첨부
② 소품 (스프라이트 시트)  ← 내부 스타일/색감 맞춤
   ↓ [내부 이미지] 첨부
③ 외벽 (빈 문틀 포함)    ← 내부 벽 높이/재질/스타일 맞춤
   ↓ [외벽 이미지] 첨부
④ 문 (열림/닫힘)         ← 외벽 문틀 크기/스타일 맞춤
   ↓ [내부 이미지] 첨부
⑤ 지붕                   ← 내부 벽 상단에 맞춤
```

### 2층 건물 (가구점 등)

```
① 1층 내부 (컷어웨이)    ← 모든 것의 기준
   ↓ [1층 내부 이미지] 첨부
② 1층 소품               ← 1층 내부 맞춤
   ↓ [1층 내부 이미지] 첨부
③ 2층 내부 (컷어웨이)    ← 1층과 동일 폭, 스타일 맞춤
   ↓ [2층 내부 이미지] 첨부
④ 2층 소품               ← 2층 내부 맞춤
   ↓ [1층 내부 이미지] 첨부
⑤ 외벽 (2층 높이 통짜)   ← 1층 내부 벽 재질/스타일 맞춤
   ↓ [외벽 이미지] 첨부
⑥ 문 (열림/닫힘)         ← 외벽 문틀 맞춤
   ↓ [1층 내부 이미지] 첨부
⑦ 지붕                   ← 내부 벽 상단에 맞춤
```

### 레퍼런스 체이닝 규칙

| 생성 대상 | 첨부할 레퍼런스 | 이유 |
|-----------|----------------|------|
| 내부 | 없음 (또는 다른 건물 내부) | 스타일 기준점 |
| 소품 | 내부 이미지 | 색감/디테일 맞춤 |
| 외벽 | 내부 이미지 | 벽 높이/재질 맞춤 |
| 문 | 외벽 이미지 | 문틀 크기/스타일 맞춤 |
| 지붕 | 내부 이미지 | 벽 상단 맞춤 |
| 2층 내부 | 1층 내부 이미지 | 폭/스타일 맞춤 |

> 모든 프롬프트에 `STRICT STYLE LOCK — match the attached reference EXACTLY` 포함.

---

## 공통 주의사항

1. **사이즈 필수**: 모든 프롬프트에 `Output image size: WxH pixels` 명시
2. **스타일 제어**: `STRICT STYLE LOCK` + "NOT photorealistic, NOT painterly, NOT hyper-detailed" 반드시 포함
3. **각도 강조**: "2:1 isometric projection, 26.57°, strict 2:1 pixel ratio" 반복
4. **간판 텍스트**: 한국어 텍스트 직접 쓰지 않음 → 빈 프레임 or 지워진 흔적
5. **컷어웨이 구조**: 건물은 항상 앞면+지붕 제거, 내부 배치 가능하도록
6. **그리드 호환**: 모든 에셋의 footprint를 NxM 그리드 단위로 명시
7. **디테일 수준**: 50% 축소에서도 읽히는 큰 단위 디테일만. 미세 크랙/먼지 입자 금지
8. **디테일 일관성**: 오브젝트 묘사 1줄 이내. 형용사/재질/상태 묘사 최소화
9. **모듈 조합**: 내부(컷어웨이) + 외벽(V자, 앞면) + 문(별도) + 지붕(별도) + 소품 = 하나의 건물
10. **외벽 방향**: 카메라가 front-left를 바라보므로, 외벽은 V자로 카메라를 향해 열리는 형태
11. **문은 항상 별도**: 외벽에 빈 문틀만 그리고, 문 자체는 독립 에셋 (열림/닫힘 2버전)
12. **소품/간판 각도 필수**: 스프라이트 시트의 모든 아이템이 동일한 2:1 isometric 각도. "Every item must face the same 2:1 isometric angle — camera facing front-left corner" 반드시 포함
