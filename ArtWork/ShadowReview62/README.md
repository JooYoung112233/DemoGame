# 62° 그림자 보정 — 2026-09-11

사용자 피드백: “그림자도 이상하네”. NPC의 길고 흐린 그림자를 Unity MCP로 비교·수정했다.

- Before.png / After.png: 동일 실제 Safehouse 씬, 임시 카메라 62°/0°, orthographic size 3.6, 카메라 거리 25m, 1280×960. 주광/주변광은 낮 WeatherData 기준. 게임 카메라의 URP 후처리 설정을 복사했다. Unity Editor 렌더이며 Play 캡처와 구분한다.
- Runtime_district_warden.png / Runtime_veteran_scavenger.png: Play의 실제 조명/애니메이션을 임시 카메라로 렌더한 재검수. 플레이어와 게임 카메라는 이동하지 않았다.
- RuntimeValidation.json: 적용값과 셰이더 오류 검사. 임시 카메라는 finally에서 제거.
- BeforeURP.txt: 수정 전 설정을 보존한 참고 사본. Unity 임포트용 파일 아님.

낮 태양 38° → 58°; URP 2 → 4 cascades, split .35/.60/.85; depth bias 1 → .5, normal bias 1 → .25. 2048 해상도/40m 거리/Soft는 유지했다. 1.9m 수직 물체의 지면 그림자 길이는 약 2.43m → 1.19m다. NPC 캡슐 Renderer는 비활성으로, 중복 캐스터 원인이 아니다.

저장 대상은 WeatherData.daySunAngle 및 URP-3D의 다섯 필드뿐이다. 씬 파일을 저장하거나 밤 광원 값을 바꾸지 않았다. 전후 비교를 위한 일시적인 씬 조명·파이프라인 값은 복원했다. 완료 전 Zone1로 전환된 Play는 보존했다.

한계: 야간 재촬영, 이동 중 캐스케이드 전환, GPU 시간/드로콜 프로파일링은 수행하지 않았다. 4 cascades는 기존보다 렌더 패스가 늘어난다. 별도 게임플레이 코드 변경은 없다.
