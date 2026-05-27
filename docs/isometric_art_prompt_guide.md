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

## 세계관 톤

- 한국 도시 폐건물 (abandoned Korean urban)
- 좀비/종말 아님 — "버려진 도시가 밤에 열리는" 현상
- 먼지, 균열, 녹, 벗겨진 페인트, 덩굴 (대형 디테일만, 미세 입자 X)
- 간판/텍스트: 빈 간판 프레임만 남기거나, 한국어 텍스트 지워진 흔적

---

## 프롬프트 템플릿

### A. 건물 외벽 쉘 (컷어웨이)

```
Isometric pixel art cutaway building shell of [건물 설명]. 
Front wall and roof completely removed, dollhouse cross-section view. 
Only left wall, right wall, back wall, and floor remain visible.

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

Top-left lighting. Transparent background (PNG). Strict 2:1 pixel ratio.
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

카메라는 front-left corner를 바라봄 → 외벽은 **V자 형태로 카메라를 향해 열림**.

```
This wall piece fills the OPEN FRONT of the cutaway interior.
The camera faces the front-left corner — so this front wall piece 
forms a V-shape OPENING TOWARD THE CAMERA:
- LEFT wing: runs along the FRONT-LEFT edge ([N] cells long), 
  faces the camera's left side. [디테일]
- RIGHT wing: runs along the FRONT-RIGHT edge ([M] cells long), 
  faces the camera's right side. [디테일]
- The two wings meet at the FRONT CORNER (closest point to camera).

IMPORTANT: door frame is an EMPTY OPENING. No door drawn.
Wall material and height identical to back walls.

Art style: match reference EXACTLY. Bold 1-2px outlines.
Flat cel-shaded, 3-4 color steps. NOT photorealistic.
2:1 isometric projection (26.57°). Top-left lighting.
Transparent background (PNG). Strict 2:1 pixel ratio.
```

### G. 문 (단독 에셋)

문은 항상 외벽과 **별도 이미지**로 생성. 열림/닫힘 2버전.

```
Isometric pixel art of a single [문 재질/타입] door.
1x1 grid footprint. Door height matches wall height.
[문 외형 1줄 묘사]

Two versions side by side on one image:
LEFT: door closed (flat against frame)
RIGHT: door open (swung 90° inward)

Art style: match reference EXACTLY. Bold 1-2px outlines.
Flat cel-shaded, 3-4 color steps. NOT photorealistic.
2:1 isometric projection (26.57°). Top-left lighting.
Transparent background (PNG). Strict 2:1 pixel ratio.
```

---

## 생성 순서 (필수)

**내부를 먼저 만들고, 그 이미지를 레퍼런스로 나머지를 만든다.**

```
① 내부 (컷어웨이)  ← 스타일 기준이 되는 첫 이미지
   ↓ 이 이미지를 레퍼런스로 첨부
② 소품 (스프라이트 시트)  ← 내부 바닥 그리드에 배치 확인
   ↓ 내부 이미지 레퍼런스 유지
③ 문 (열림/닫힘)  ← 사이즈 기준
   ↓ 내부 이미지 레퍼런스 유지
④ 외벽 (V자 앞면)  ← 문틀 크기 = 문 사이즈에 맞춤
   ↓ 내부 이미지 레퍼런스 유지
⑤ 지붕  ← 외벽 상단에 맞춤
```

> 모든 후속 프롬프트에 `[내부 이미지 첨부]` + `STRICT STYLE LOCK — match the attached reference EXACTLY` 포함.
> 이렇게 해야 색감, 외곽선 굵기, 디테일 수준이 한 건물 내에서 일관됨.

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
