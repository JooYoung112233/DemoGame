# 마을 분위기 보완 — 2026-09-11

사용자 피드백: “전체적으로 밋밋하고 분위기가 약함”. 승인된 하이드아웃 `ArtWork/HideoutReview/InGame.png`와 직전 마을 화면을 대조해 밤의 중간 명암과 전당포 입구의 초점을 보완했다. 실내/실외 및 카메라 방위가 다른 참조이므로 같은 장면으로 간주하지 않는다.

- `Before_*`: 이번 변경 전 실제 런타임 장면을 임시 62°/0° 카메라로 촬영한 낮밤 비교.
- `Saved_*`: 저장·Play 재시작 후 같은 구도 촬영. 전당포/관리인/실내/마을 4구역. 카메라·조명·Volume·지붕·전구 임시 상태는 매 호출 종료 시 복원한다. 실내는 지붕을 숨기고 기존 전구를 켠 검수 조건이다.
- 카메라는 실제 게임의 HDR/URP/Volume 설정을 복사하지만 게임 Main Camera 자체 화면이나 UI 캡처는 아니다. 플레이어 애니메이션·착용등은 시간대 강제 동기화하지 않으므로 전후 픽셀 동일 조건은 아니다.
- `Applied.json`: 저장값/게임플레이·콜라이더 불변 검사. `playPreserved=false`는 사용자 승인 후 저장을 위해 Play를 종료했기 때문이다. 이후 재실행·복원 완료.
- `Validation.json`: 저장 후 실제 페이즈 이벤트로 조명 7개 낮밤/재활성/야간 프리팹 생성 검사, HDR/Volume/셰이더/누락 스크립트/임시 카메라 검사 통과.
- `Inspect.json`: 최종 재실행된 런타임 값. `PlayState.json`: 재실행 전 위치·방향·낮밤. 지역 시계가 없는 Safehouse여서 regionId=null이다.

밤 주광 .38 / RGB(.66,.58,.85) / 각도(58,-35,0), 주변광(.20,.17,.27), 기존 보랏빛 안개 밀도 .022. 가로등 낮6/밤14·범위8, 현관 낮2.4/밤7.5·범위6.5·Soft 그림자. 낮 WeatherData/공용 Volume/셰이더/재질/모델 유지. 범위·색·현관 그림자는 낮에도 적용된다. 밤 고도48°의 첫 후보는 그림자가 길어58°로 수정했다.

Unity MCP 전용: 저장소 `demo13-flashlight/tools/unity_mcp_call.py`의 `run_script`에 tools/*.json을 전달한다. `apply.json`은 Play 정지 및 원본 Safehouse 미저장 편집이 없을 때만 실행한다. Unity는 Play 중 preview scene 저장도 거부하므로 Play 유지용 저장 경로를 사용하지 않는다. 저장 후 기존 `ArtWork/Atmosphere62/tools/prepare_play.json` → editor_play → enter_town.json → 여기 restore_play.json 순으로 위치를 복원했다. `saved.json`은 현재 저장값을 촬영한다. `before.json`도 현재값을 촬영하므로 과거 Before를 덮어쓰지 말 것.

자체 시각 검수는 완료했으며 사용자 최종 외형 승인은 별도다. 넓고 반복적인 포장/단순한 건물 구성의 밋밋함은 남는다. 다른 레이드 씬의 공용 밤 설정 영향, Point Light 그림자 최대6면 추가 GPU 비용, 연속 이동 그림자/SSAO 및 플레이 상호작용 종합 검증은 미완료. 기존 AnomalyFog 경고는 이번 변경에서 해결하지 않았다. 최종 조회 콘솔 오류0.
