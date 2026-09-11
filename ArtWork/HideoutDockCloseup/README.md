# 하이드아웃 기본 확대와 도크 딤 검수 — 2026-09-12

- 사용자 요청: 우측 UI 뒤 약한 딤, 좌측 맵 약 2배 확대. 후속 요청에 따라 최초 진입과 시설 창을 닫은 기본 화면에도 적용.
- GameTuning: hideoutViewZoom=2, hideoutDockDimAlpha=0.32. 최초 진입은 방 중심, 시설 선택은 시설과 캐릭터 대기 지점 중심. 확대된 방 외곽 일부는 화면 밖으로 나간다.
- Unity MCP 재컴파일 성공, 재실행 후 실제 Hideout 씬 진입 및 7시설 선택 검수. 상하단 UI 뒤 배경 겹침을 가린 뒤 재검수했다.
- Before.json: 이전 시설 창 오소 6.44865. Validation.json: 최초 진입 3.224326, 모든 시설 및 닫힘 3.224325. 선형 크기 2배 유지.
- Controls.json: 시설 바로가기 7개와 닫기 버튼 중심의 EventSystem raycast 통과. 닫기 onClick 실행. 딤은 raycastTarget=false이며 닫으면 숨겨짐.
- initial-after.png / closed-after.png / 시설별 after.png: 실제 1920×1080 플레이 캡처. workbench-before.png는 변경 전 비교용.
- 최종 Unity 콘솔 오류 0. 자체 검수 완료; 사용자 최종 시각 승인은 별도.
- 에셋을 Unity API로 저장하면서 기존 코드 기본값 12개가 함께 직렬화됐다. 탐색 시간·회피·총기 감쇠·조준 필드 모두 기존 초기값과 동일하며 이번 밸런스 변경은 없다.

검수 도구는 Assets 밖의 Review.cs를 Unity MCP run_script로 메모리 컴파일한다. review.json으로 화면 순회를 시작한 후 Validation.json 생성을 확인하고 test-controls.json을 실행한다. 초기 화면 검수는 실제 Hideout 씬에 새로 진입한 상태에서 시작한다.
