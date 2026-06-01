# 탑다운 아트 스펙 (AI 이미지 생성용)

> 레퍼런스 스타일: 순수 탑다운 90° 실내 서바이벌 호러
> 거의 완전 암흑 + 손전등 콘이 주광원. Desaturated + 따뜻한 손전등 색.
> 카메라는 순수 2D Orthographic (위에서 정직하게 내려다봄).

---

## 0. 시점 규격

- **카메라**: 90° 정탑다운 (완전히 위에서 내려봄)
- **아트 각도**: 오브젝트를 **위에서 직접 내려본 모습** (top-down 실루엣)
- 벽 = 위에서 본 **두꺼운 직사각형 테두리**
- 바닥 = 평평한 텍스처
- 오브젝트(책/천/상자) = 위에서 본 **납작한 실루엣**

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
Top-down floor tile viewed from directly above (90°).
Square or slightly rectangular tile, worn [wood planks / concrete / dirt].
Seamless edges so tiles repeat without visible seams.
Flat even lighting, desaturated muted tones.
Transparent or magenta (#FF00FF) background.
Canvas: 256×256 (square, not isometric diamond).
Pivot: Center.
```

### 벽 (Wall)
```
Top-down wall segment viewed from directly above — appears as a thick rectangle/border.
[Inside face visible / just the top edge]. Cracked plaster, brick edge.
One segment = one tile width. Tileable left-right.
Canvas: 256×128 (wide, thin — wall seen from above).
Pivot: Center.
```

### 오브젝트/프랍 (Props)
```
Top-down [book / cloth / crate / rug / trash] viewed from directly above (90°).
Flat silhouette, no perspective distortion.
Single object, magenta background, no shadow.
```

### 캐릭터
```
Top-down character viewed from directly above.
Small oval/circle body, visible head/shoulders.
Can be abstract (just a circle with flashlight cone indicator).
Canvas: 128×128. Pivot: Center.
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
| 날짜 | 내용 |
|---|---|
| 2026-06-02 | 레퍼런스 확정 — 탑다운 90° 서바이벌 호러 룩. 아트 스펙 작성. |
