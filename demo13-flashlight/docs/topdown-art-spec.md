# 탑다운 아트 스펙 (AI 이미지 생성용)

> 레퍼런스 스타일: 탑다운 ~80° 실내 서바이벌 호러 (레퍼: Darkwood)
> 어둑한 무드 + **시야 콘(FOV)** 기반 가시성(손전등 폐기, 하이브리드). Desaturated.
> **카메라 = 2D Orthographic. 원근(80° 틸트)은 카메라가 아니라 아트 자체에 베이크.**

---

## 0. 시점 규격 (확정 2026-06-02)

- **카메라**: 2D Orthographic 정면 뷰 (카메라는 기울이지 않음)
- **아트 각도 = ~80° 틸트를 이미지에 미리 그려넣음** — 거의 위에서 보되 살짝 눕혀, **정면/측면이 조금 보임**(완전 직하 90° 아님)
- **모든 타일·오브젝트는 Unity에서 회전 (0,0,0)으로 배치** — 순수 2D, 스프라이트가 카메라 정면을 향함. (회전·빌보드·눕힘 없음)
- 벽 = 윗면 + **앞면이 살짝 보이는** 낮은 두께 (80° 틸트라 정면이 약간 노출)
- 바닥 = 평평한 텍스처 (80° 틸트 그림자/원근 살짝)
- 오브젝트(책/천/상자) = 위에서 살짝 눕혀 본 모습 (약간의 높이감)
- ⚠️ **바닥/벽은 80° 시점** 기준
- **가구·오브젝트·캐릭터는 예외: high 3/4 약 60°** (확정 2026-06-02)
  - 진짜 80°로 그리면 가구가 윗면만 보여 안 읽힘(책장/금고/진열장 등) → 가독성 위해 더 정면으로
  - 기준 각도 = **약 60° (바닥 80°와 정면 사이)**. 캐릭터 스프라이트 각도와 맞춤
  - 가구끼리 각도 통일이 핵심 (제각각 금지). 거의 정면(90°)으로 튀는 것 금지
  - 접지 그림자 + 좌상단 광원으로 바닥과의 위화감 최소화
  - 레퍼(배틀맵·스타듀·좀보이드)도 바닥은 탑다운/가구는 더 정면으로 그리는 동일 방식

---

## 1. 공통 규칙

| 항목 | 값 |
|---|---|
| **배경** | 마젠타(#FF00FF) — Color to Alpha로 제거 |
| **라이팅** | **플랫/균일** — 방향 그림자 없음. 런타임 2D Light가 조명 |
| **색감** | **Desaturated(탈채도)** 기본. 시야 콘 안에서 살짝 밝게 |
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

#### 확정 주인공 디자인 (2026-06-02)
- 곱슬 흑발, 마른 체형, **긴팔 셔츠**(올리브/카키), 카고팬츠+벨트, 갈색 부츠
- 3등신 SD 비율
- **플랫 셰이딩**: 재질당 베이스 1색 + 그림자 1톤. 그라데이션/디더링/픽셀노이즈 금지
  - 도트 질감·라이팅은 **셰이더로 후처리** → 원본은 단순 플랫하게 유지(방향 추가·장비 교체 유연)

#### 방향 제작 순서
1. **1차**: 횡방향 — 동(오른쪽 보기) / 서(왼쪽 보기, 좌우 플립). 80° 틸트에서 동/서 = 거의 측면 실루엣
2. **2차 (나중)**: 상/하 또는 대각 방향 추가
- 좌우 플립으로 동/서를 한 세트로 커버 → 검증·제작 비용 최소

#### 측면 캐릭터 공통 규칙 (2026-06-02)
- **양팔/양손이 항상 보이게** (왠만하면). 순수 정측면이면 먼 팔이 가려지므로, 먼 쪽 팔을 살짝 앞/밖으로 빼서 양손 다 읽히게
- 팔다리가 몸통·소지품(방패/무기)에 가려 사라지지 않게 — 전신 완전체로
- 측면이되 살짝 틀어 양손 노출 OK (완전 정측면 고집 X)

#### 적/밴딧 캐릭터 (2026-06-02)
- **화풍·디테일·비율을 플레이어와 100% 통일** (같은 아티스트가 그린 느낌). 플레이어보다 디테일 높이지 않음
- **측면(동/서) 우선 + 좌우 플립** — 플레이어와 동일한 방향 제작 방식
- 플랫 셰이딩 동일 (베이스 1색 + 그림자 1톤)
- 밴딧 종류: 방패 경비병(SEC), 후드 칼잡이, 넝마 환자형, 후드 스캐빈저 등 — 디자인은 유지하되 화풍만 통일

---

## 3. 라이팅 / 가시성 (엔진 설정)

> **손전등(주광원) 폐기 → 좀보이드식 시야 콘(FOV).** 어둠=하이브리드(지역/시간대별 가변). 상세 [`rendering.md`](rendering.md).

| 항목 | 값 |
|---|---|
| 글로벌 앰비언트 | **가변**(밤·실내 어둑 ~0.1 / 낮·야외 밝게). 하이브리드 |
| 글로벌 색상 | (0.3, 0.35, 0.4) 차가운 어둠 계열 |
| 시야 콘(FOV) | 플레이어 facing 부채꼴. 콘 밖의 적은 안 보임/어둑 |
| 시야 콘 inner/outer | inner ≈20° / outer ≈50° (소프트 가장자리) |
| 그림자/차폐 | ShadowCaster2D — 벽 뒤는 시야·빛 차단 |

> 아트는 여전히 **Desaturated + 어둑한 무드** 전제로 그리되, 균일 플랫 라이팅으로(런타임 Light2D/시야가 명암 담당).

---

## 변경 로그
| 날짜 | 질문 | 결정 |
|---|---|---|
| 2026-06-02 | 레퍼런스 룩? | 탑다운 서바이벌 호러(Darkwood). 아트 스펙 작성. |
| 2026-06-02 | 아트 각도 90° 정탑다운 vs 80° 베이크 원근? + 오브젝트 배치 회전? | **아트 자체에 ~80° 틸트를 베이크**(카메라는 2D 정면, 기울이지 않음). **타일·모든 오브젝트는 회전 (0,0,0)으로 배치**(순수 2D, 스프라이트가 카메라 정면). 90° 정탑다운 안 폐기. |
| 2026-06-02 | 손전등 주광원 유지 vs 시야 FOV? | **손전등 폐기 → 좀보이드식 시야 콘(FOV).** 어둠=하이브리드(지역/시간대별). 손전등 색/angle 라이팅 스펙을 시야 콘 스펙으로 교체. |
| 2026-06-02 | 주인공 디자인 확정 + 셰이딩 수준? | 곱슬 흑발/마른 체형/긴팔 셔츠/카고팬츠/부츠, 3등신. **플랫 셰이딩**(베이스 1색+그림자 1톤), 도트 질감·라이팅은 셰이더 후처리. |
| 2026-06-02 | 캐릭터 방향 제작 순서? | **횡(동/서) 우선** — 80° 틸트에서 거의 측면. 좌우 플립으로 동/서 커버. 상/하·대각은 나중에 추가. |
| 2026-06-02 | 가구·오브젝트도 80°로? | **아니오 — high 3/4 약 60°로 통일.** 80°면 가구가 윗면만 보여 안 읽힘. 바닥/벽만 80°, 가구·오브젝트·캐릭터는 60° 정면 쪽. 가구끼리 각도 일관 + 접지 그림자로 위화감 최소화. (배틀맵/스타듀/좀보이드 동일 방식) |
| 2026-06-02 | 적/밴딧 아트 방향? | **플레이어와 화풍·디테일·비율 100% 통일, 측면(동/서) 우선 + 좌우 플립.** 기존 밴딧 스프라이트(방패경비병/후드칼잡이/넝마환자/스캐빈저)를 플레이어 스타일로 재제작. 플레이어보다 디테일 높이지 않음. |
