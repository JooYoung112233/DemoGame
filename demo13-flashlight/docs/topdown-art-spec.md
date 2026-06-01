# 탑다운 아트 스펙 (AI 이미지 생성용)

> 레퍼런스 스타일: 탑다운 ~80° 실내 서바이벌 호러 (레퍼: Darkwood)
> 거의 완전 암흑 + 손전등 콘이 주광원. Desaturated + 따뜻한 손전등 색.
> **카메라 = 2D Orthographic. 원근(80° 틸트)은 카메라가 아니라 아트 자체에 베이크.**

---

## 0. 시점 규격 (확정 2026-06-02)

- **카메라**: 2D Orthographic 정면 뷰 (카메라는 기울이지 않음)
- **아트 각도 = ~80° 틸트를 이미지에 미리 그려넣음** — 거의 위에서 보되 살짝 눕혀, **정면/측면이 조금 보임**(완전 직하 90° 아님)
- **모든 타일·오브젝트는 Unity에서 회전 (0,0,0)으로 배치** — 순수 2D, 스프라이트가 카메라 정면을 향함. (회전·빌보드·눕힘 없음)
- 벽 = 윗면 + **앞면이 살짝 보이는** 낮은 두께 (80° 틸트라 정면이 약간 노출)
- 바닥 = 평평한 텍스처 (80° 틸트 그림자/원근 살짝)
- 오브젝트(책/천/상자) = 위에서 살짝 눕혀 본 모습 (약간의 높이감)
- ⚠️ **모든 에셋(바닥/벽/펜스/프랍/캐릭터)이 같은 80° 시점**으로 그려져야 합쳐졌을 때 일관됨

---

## 1. 공통 규칙

| 항목 | 값 |
|---|---|
| **배경** | 마젠타(#FF00FF) — Color to Alpha로 제거 |
| **라이팅** | **플랫/균일** — 방향 그림자 없음. 런타임 2D Light가 조명 |
| **색감** | **Desaturated(탈채도)** 기본. 따뜻한 손전등 아래서 살짝 노랗게 |
| **PPU** | 전 에셋 통일 (예: 64~128 PPU) |

---

## 2. 에셋별 스펙

### 바닥 타일 (Floor)
```
Top-down floor tile seen from a steep ~80° angle (almost overhead, slight tilt).
Square tile (NOT isometric diamond), worn [wood planks / concrete / dirt].
Seamless edges so tiles repeat without visible seams.
Flat even lighting, desaturated muted tones.
Transparent or magenta (#FF00FF) background.
Canvas: 256×256 (square).
Pivot: Center.
```

### 벽 (Wall)
```
Wall segment seen from a steep ~80° top-down angle — top surface plus a SHORT sliver
of the front face visible (slight height), NOT a full elevation view.
Cracked plaster, brick edge. One segment = one tile width. Tileable left-right.
Canvas: 256×192 (a bit of vertical room for the front-face sliver).
Pivot: bottom-center (so the base sits on the tile).
```

### 오브젝트/프랍 (Props)
```
[book / cloth / crate / rug / trash] seen from a steep ~80° top-down angle.
Slight height/front face visible (not a flat 90° silhouette).
Single object, magenta (#FF00FF) background, no shadow.
Pivot: bottom-center for standing objects, center for flat objects (rug/trash).
```

### 캐릭터
```
Character seen from a steep ~80° top-down angle.
Head/shoulders prominent, slight body length visible (not a pure overhead disc).
Can be abstract (a rounded body with a flashlight cone indicator).
Canvas: 128×128. Pivot: bottom-center.
```

---

## 3. 라이팅 (엔진 설정)

| 항목 | 값 |
|---|---|
| 글로벌 앰비언트 | intensity **0.04** (거의 완전 암흑) |
| 글로벌 색상 | (0.3, 0.35, 0.4) 차가운 어둠 |
| 손전등 색상 | (1.0, 0.92, 0.75) 따뜻한 노란빛 |
| 손전등 inner angle | 20° (날카로운 콘) |
| 손전등 outer angle | 50° (소프트 가장자리) |
| 그림자 | enabled, intensity 1.0 |

---

## 변경 로그
| 날짜 | 질문 | 결정 |
|---|---|---|
| 2026-06-02 | 레퍼런스 룩? | 탑다운 서바이벌 호러(Darkwood). 아트 스펙 작성. |
| 2026-06-02 | 아트 각도 90° 정탑다운 vs 80° 베이크 원근? + 오브젝트 배치 회전? | **아트 자체에 ~80° 틸트를 베이크**(카메라는 2D 정면, 기울이지 않음). **타일·모든 오브젝트는 회전 (0,0,0)으로 배치**(순수 2D, 스프라이트가 카메라 정면). 90° 정탑다운 안 폐기. |
