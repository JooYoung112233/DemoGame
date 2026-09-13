# 시작 위치 원점 복귀 수정 — 2026-09-13

- 시작 구성: Systems 활성, Safehouse additive로 열린 상태. Safehouse `default`는 (53.5, 0, 12), 편집 PlayerRig는 (0,0,0).
- `Before.json` / `Probe.cs`: 기존 GameBoot 배치 직후 Transform은 (53.5,0,12), Rigidbody는 (-.3,0,0). 0.3초 뒤 Transform도 (-.3,0,0)으로 되돌아가는 현상을 재현했다. Rigidbody Interpolate와 autoSyncTransforms=false 상태에서 Transform만 바꾸던 결함.
- `SpawnPoint.PlacePlayer`는 Transform/Rigidbody 위치를 함께 지정하고 잔여 속도를 지운 뒤 물리 동기화·카메라 스냅을 수행한다. GameBoot 직접 시작과 SceneTransitionManager 전환 스폰이 함께 사용한다.
- `Validate.cs` / `Validation.json`: 수정 후 실제 Play 재시작 위치, Rigidbody 일치, 잔여 속도 제거, 물리 프레임 이후 위치 유지, Hideout→Safehouse 왕복을 포함한 8/8 통과. 콘솔 오류 0건.
- 검증 중 세이브 쓰기를 억제했으며 사용자 아이템을 수정하지 않았다. 최종 플레이는 마을 집 앞 기본 스폰에 둔다. `StartAtHome.png`는 실제 게임 화면이다.
- Systems 단독의 타이틀 버튼 흐름 및 모든 레이드 스폰 후보를 전수 실행한 결과는 아니다. 재현한 에디터 시작 구성과 실제 건물 씬 왕복을 검증했다.
