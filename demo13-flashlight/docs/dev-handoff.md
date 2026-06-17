# 개발 핸드오프 (이어서 작업)

> 다른 PC에서 이어서 작업할 때 **여기부터** 읽기. `/resume` 슬래시 명령으로 자동 로드됨.
> 최신 갱신: 2026-06-16

## 지금 위치
**코어 루프 빌드 — Phase 0(타이틀) 완료, Phase 1(안전가옥) 진행 중.**
빌드 순서 전체 = [dev-roadmap.md](dev-roadmap.md) "빌드 순서 — 코어 루프부터 1단계씩" 표.

**스코프**: 리소스(아트/스프라이트/실맵/사운드)만 제외, 그 외 **전 게임 시스템을 그레이박스로 한 단계씩** 제작+테스트.

## 이번 세션에 만든 것 (코드 = 정적 컴파일 감사 통과 / Unity 실행 검증은 사용자)
- **게임 시작 화면**: `TitleScreen`(새게임/이어하기/종료) + `GameBoot` 타이틀 게이트(`showTitleOnBoot`)
- **HP바 깜빡임 제거**: `GameHUD`는 게임플레이 씬 활성 시에만 표시(타이틀=숨김)
- **전환 폴리시**: `SceneTransition` 즉시커버→셋업→reveal + 스폰 시 `CameraFollow.SnapToTarget`(슬라이드 제거)
- **손전등 무드 라이트**: 따뜻·부드러움 + origin 캐릭터 부착(뾰족함 제거). 값은 `FlashlightController` Inspector 라이브 튜닝. (끝처리 더 부드럽게 = 라이트 쿠키, **보류**)
- **평판 시스템(Phase 7 선제작)**: `ReputationManager`(F~A)+영속화+자가검증(`Tools ▸ 테스트 ▸ 평판 시스템 자가검증`)
- **밸런스**: `region_loot` 1지역 짙은현상 `container_anomaly`/`ground_anomaly` 티어 신설(루디 소득원) + `RegionLootTier` enum 2값

## 다음 할 일 — NPC 대화 테스트 (사용자 차례, Unity)
1. `Tools ▸ TopDown ▸ 빌드 ▸ ▶ 안전구역 일괄 빌드` → **NPCData 생성 + NPC 자동 배치**(전당포·회수꾼·관리인·떠돌이상인 4마커, NPCController+NPCData 자동연결)
2. Build Settings에 **Systems + Safehouse** 등록 → **Systems만** Play → 새 게임 → 안전가옥
3. NPC(전당포/회수꾼/관리인)에 **E** → `DialogueUI` 인사 대사 뜨는지 확인
   - ⚠️ **떠돌이 상인 = 빈 마커**(wandering_merchant NPCData 미생성). 나머지 3명만 대화됨.

## 테스트 방식 (고정)
- 정식 실행 = **Systems 씬만** 열고 Play (타이틀→새게임→Safehouse가 additive로 로드).
- 코드는 Claude가 작성+정적 감사 → **Unity 실행/테스트는 사용자** → 결과(특히 컴파일 에러) 받고 다음 단계.

## 미해결·백로그
- 떠돌이 상인 NPCData(대화) 미생성 → 채우려면 요청
- **I32**: 타이틀↔`GameStartHandler` 프롤로그/세이브로드 배선(세이브/인트로 단계)
- 손전등 끝처리 쿠키(보류) · 다음 큰 단계 = **Phase 2 레이드 맵 + 씬전환 루프**
- 평판 적립 연동·HUD(Phase 7) — 코어 루프 후
