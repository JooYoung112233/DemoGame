# 셰이더 시스템

## 현 상태

아트 방향: 흰 종이 + 검은 잉크 기반 2.5D 도시. 낮/밤 동일 맵, 셰이더와 후처리로 분위기 전환.

### 구현된 셰이더

| 셰이더 | 파일 | 용도 | 상태 |
|--------|------|------|------|
| InkCity/SpriteOutline | SpriteOutline.shader | 2D 스프라이트 검은 외곽선 | 구현완료 |
| InkCity/InkShadow | InkShadow.shader | 발밑 잉크 그림자 | 구현완료 |
| InkCity/NightOverlay | NightOverlay.shader | 밤 오버레이 (카메라 Quad) | 구현완료 |
| InkCity/PanelWiggle | PanelWiggle.shader | 적 판넬 흔들림 + 외곽선 | 구현완료 |
| InkCity/InkDissolve | InkDissolve.shader | 잉크 디졸브 (사망/소멸) | 구현완료 |
| InkCity/InkFloor | InkFloor.shader | 바닥 잉크 효과 (on/off) | 구현완료 |
| InkCity/DotFloor | DotFloor.shader | 바닥 도트 효과 | 구현완료 |
| InkCity/SpriteBillboard | SpriteBillboard.shader | SpriteRenderer용 빌보드 (ZWrite+조명+아웃라인) | 구현완료 |

### 우선순위 (가이드 기준)

- 1순위(데모 필수): 외곽선, 발밑 그림자, 밤 오버레이
- 2순위(느낌 강화): 판넬 흔들림, 잉크 디졸브
- 3순위(나중): 바닥 잉크 얼룩, UI 잉크 번짐, 화면 가장자리 노이즈

### 머티리얼 네이밍

| 용도 | 이름 |
|------|------|
| 플레이어 외곽선 | M_Player_Outline |
| 적 판넬 | M_Enemy_Panel_Wiggle |
| 잉크 그림자 | M_InkShadow |
| 잉크 디졸브 | M_InkDissolve |
| 밤 오버레이 | M_NightInkOverlay |
| 종이 바닥 | M_PaperFloor |

---

## 결정 로그

### 2026-05-22

- **질문**: 바닥 셰이더 방향 — 잉크 느낌 vs 도트 느낌 vs 아포칼립스 느낌?
- **결정**: 잉크 셰이더(InkFloor)와 도트 셰이더(DotFloor) 분리 제작. 잉크 셰이더는 on/off 토글 포함.
- **근거**: 잉크 느낌과 도트 느낌은 다른 목적. 각각 독립 셰이더로 관리.

- **질문**: 전체 셰이더 시스템 제작 순서?
- **결정**: 프로덕션 가이드 기준 1~2순위 5개 셰이더 우선 구현 (외곽선, 발밑 그림자, 밤 오버레이, 판넬 흔들림, 디졸브)
- **근거**: 가이드 문서(Unity_Ink_Shader_Production_Guide.md) 우선순위 따름

### 2026-05-23

- **질문**: 2D 스프라이트 캐릭터가 3D 큐브 벽과 깊이 처리가 안 됨 (SpriteRenderer 기본 머티리얼 ZWrite Off)
- **결정**: SpriteRenderer 전용 `InkCity/SpriteBillboard` 셰이더 제작. ZWrite On + AlphaCutout + 추가조명(손전등) + 최소밝기 + 아웃라인 옵션 + ShadowCaster 패스.
- **근거**: 벽은 3D 큐브, 캐릭터는 2D 빌보드 → 깊이 버퍼 공유 필수. 기존 PlayerSprite 셰이더는 스프라이트 시트(Columns/Rows) 전용이라 SpriteRenderer와 비호환.
