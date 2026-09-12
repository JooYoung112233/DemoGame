# UI 비율 검수 — 2026-09-13

대상: `demo13-flashlight`, Unity MCP로 적용 및 플레이 검증. 사용자 요청은 UI 비율과 주요 화면 전체 점검이다.

## 반영 사항

- 1920×1080 기준 CanvasScaler Expand 통일: 런타임 생성 코드, UI 프리팹 26개와 Systems 씬의 CanvasScaler 총 40개.
- 인벤토리 기본 글자 16px, 주요 수치 18px, 제목 24px. 좁은 화면에서 중앙 내용이 옆 열을 침범하던 배율 문제 해소.
- 마을 미니맵 336×288, 의뢰 HUD 상단 간격 연동.
- 하이드아웃 제목/안내 stretch, 실제 Canvas 치수에 맞춘 시설 바와 카메라 회피 영역, 도크 제목 32px·본문 22px.
- 출전 지도 제목의 이중 높이, 의뢰인 제목 음수 높이 및 이름/미리보기 겹침 수정.
- 의뢰 목록 stencil Mask를 RectMask2D로 변경해 보이지 않던 내용을 복구.
- 전체 창 닫기에서 누락된 도감과 특성 창 포함.

## 검수 결과

실제 GameView 해상도 1280×1024, 1920×1080, 2560×1080에서 다음 10개 화면을 각각 캡처했다.

마을 HUD, 인벤토리, 출전 지도, 일시정지, 설정, 의뢰, 도감, 상점, 하이드아웃 기본 화면, 침대 시설 화면.

- `Final_<해상도>_<화면>.png`: 총 30장. Unity가 직접 캡처한 게임 화면이며 후처리하지 않았다.
- 같은 이름의 `-overflow.json`: 활성 텍스트의 화면 이탈 및 preferred size 넘침 검사. 30개 모두 빈 배열이다. 스크롤 마스크 안의 의도적 잘림은 검사에서 제외한다.
- 주요 화면을 육안으로 확인해 배치와 가독성을 검수했다. 자동 검사는 모든 그래픽의 겹침이나 모든 게임 상태를 보장하지 않는다.
- `InteractionValidation.json`: EventSystem 중복, 도감/특성 닫기, 미니맵 숨김/복귀, 인벤토리 닫기 버튼 raycast, 설정→일시정지 복원, 재개 버튼과 시간, 의뢰 표시/스크롤/닫기, Canvas 배율 일관성 등 10/10 통과.
- 검수 종료 시 Unity Console 오류 0건, 경고 0건. 플레이 중 마을 화면으로 복귀하고 원래 GameView 선택을 복원했다.
- 실제 거래, 제작, 의뢰 수주/보상 지급, 전투 및 다양한 소지품 조합은 이번 검수 범위에 포함하지 않았다.

## 재현

프로젝트가 연결된 공식 Unity MCP를 `demo13-flashlight/tools/unity_mcp_call.py`로 호출한다. 다른 Unity 프로젝트로 연결된 MCP 인스턴스를 사용하지 않는다.

- `Review.cs`: `FullSweep`로 세 해상도×10화면 캡처. `full-sweep.json`은 해당 요청이다. 플레이 상태에서 실행하며 완료 표식은 `FullSweep-done.txt`다.
- `Verify.cs`: `Run`으로 조작 검사. `verify.json`을 사용한다. 완료 후 `InteractionValidation.json`에서 각 결과를 확인한다.
- `Apply.cs`: `Run`으로 기존 프리팹과 Systems 씬에 배율/정적 글자 크기를 저장. `apply.json`을 사용하며 플레이 종료 상태가 필요하다. 전체 UI를 재생성하지 않는다.
- `Inventory.json`은 수정 전 Canvas 조사 결과다. `AppliedAssets.json`은 저장한 프리팹 목록과 처리한 scaler 수다.
- `Before_*`, `After_*`, 진단용 캡처와 원래 GameView 선택 정보는 로컬 중간 산출물로 git에서 제외했다.

이번 결과는 자체 검수 완료 상태이며 사용자의 최종 미감 승인과는 구분한다.
