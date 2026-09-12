# PaintedSurface62 — 애니메이션 배경풍 표면 시안

2026-09-12. 사용자 요청: 셰이더 대신 텍스처 자체에 약간의 애니풍과 감성을 추가한다.

## 결과와 범위

- 내장 image_gen으로 콘크리트·흙 Base Color 2종 제작. 최종 생성 프롬프트는 `Prompts.json`과 `RefinementPrompt.txt`에 기록했다. 생성 이미지를 자르거나 재색칠하지 않고 그대로 복사했다.
- Unity 파일: `Assets/Art/Environments/PaintedSurface62/Textures`, 재질 5개와 별도 `Prefabs/HideoutGroundFinish62_Painted.prefab`.
- GameLit/GroundFadeLit 셰이더, 조명, 컬러 틴트, 노멀/마스크 파일은 유지한다. 후보 재질의 Base Map과 반복 배율(.4)만 변경했다. GameLit의 공통 UV를 쓰므로 노멀/마스크의 샘플링 크기도 따라 변한다.
- 현재 로드된 Safehouse의 신규 바닥 마감에만 런타임 미리보기를 켰다. 원래 씬·프리팹·재질과 텍스처는 보존했다. 씬 재로드/플레이 재시작 시 원래 모습으로 돌아온다. 사용자의 최종 스타일 승인이나 마을 전체 영구 적용으로 간주하지 않는다.

## 자체 검수

- 기존 Town02 Plaster_Base와 `Assets/GPT/데칼/안전구역데칼1.png`를 대조했다. 국소 마모와 절제된 팔레트를 참고했고 원화를 직접 텍스처로 사용하지 않았다.
- 첫 콘크리트는 사실적인 석고 박리처럼 보여 붓질을 단순화했다. 첫 Unity 렌더에서 작은 얼룩의 반복이 위장무늬처럼 보여 텍스처 대비를 낮추고 반복 크기를 2.5배로 키웠다.
- Mirror 래핑으로 타일 가장자리의 색 단절을 방지한다. 생성 이미지가 수학적으로 seamless하다고 보증하지 않는다. 미러 반복의 대칭성이 넓은 면에서 보일 수 있으므로 마을 전체 확장 전 큰 면에서 추가 검수가 필요하다.
- 수정 후 같은 형상·카메라·조명에서 A/B와 야간 62° 렌더 재검수. shaderErrors false, MCP 콘솔 경고·오류 각각 0. 통행·충돌은 이번에 수정하지 않았다.
- 콘크리트 변화는 낮은 대비로 절제되어 있다. 애니메이션 배경 감성의 전체 기준을 확정하려면 벽 페인트와 목재처럼 넓은 재질 면의 다음 시안이 필요하다.

## 검수 자료와 재현

- `Unity/A_Original_Close.png`, `B_Painted_Close.png`: 같은 호출 안에서 비교한 원본/최종 후보. 실제 게임 씬의 별도 검수 카메라 렌더다.
- `Unity/B_Night_Close.png`: 임시 야간 조명 검수 후 원래 조명 복원.
- `Unity/Live_Painted_*.png`: 최종 런타임 미리보기.
- `Unity/PreviewValidation.json`, `LivePreview.json`: 검사 범위/원본 보존/저장 상태.
- Unity MCP `run_script`에 `Unity/preview.json`: 임포트 및 A/B 렌더, 종료 시 원본 재질 복원. A/B 재현은 원본 Safehouse를 새로 로드한 상태에서 시작한다.
- `Unity/show.json`: 별도 프리팹 저장 및 현재 로드된 마을에서 후보 표시. 현재 플레이를 종료하지 않는다.
