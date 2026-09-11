# Demo 프로젝트 가이드

## 개요
Unity 6 기반 top-down 2D 액션 게임 프로토타입 (`demo13-flashlight`). Tarkov/Zomboid 스타일 — 안전가옥 출발 → 맵 레이드 → 루팅 → 탈출 → 귀환 루프.

> 이 저장소는 현재 **demo13-flashlight 단일 프로젝트**만 유지한다. (이전 Phaser 데모들 demo1~12는 정리되어 제거됨)

## 배포
- **GitHub 저장소로만 관리** — 변경 작업 완료 후 git commit & push
- **브랜치를 따서 작업 후 PR로 main에 병합할 것** (main 직접 push 금지)
- Netlify 사용하지 않음

## 기획 기록 룰 (예외 없음)
- **모든 기획 질문/사용자 결정은 무조건 md에 즉시 기록한다.** 예외 없음.
- 저장 위치: `demo13-flashlight/docs/` 안 **시스템별 md 파일** (예: `combat.md`, `economy.md`, `inventory.md`)
  - 적절한 시스템 md가 없으면 새로 만든다
  - 어느 시스템에 속하는지 모호하면 사용자에게 분류만 빠르게 확인
- 저장 시점: 사용자가 결정을 내린 **직후 즉시** (세션 끝까지 미루지 않음)
- 기록 형식 (각 항목): 날짜(YYYY-MM-DD) / 던진 질문(선택지 포함) / 사용자 선택·결정 / (선택) 근거·메모
- 시스템 md는 "현 상태 = 진실" 원칙. 결정이 바뀌면 이전 내용을 덮어쓰고, 변경 이력은 같은 파일 하단 변경 로그 섹션에 추가
- 적용 대상: 기획 결정, 밸런스 수치, UX 선택, 시스템 메커니즘, 데이터 구조 등 **게임 디자인 관련 모든 결정**
  - 단순 코드 리팩터링/버그 픽스는 제외

## 프로젝트 구조
```
D:\Demo/
├── CLAUDE.md            # 이 파일 (저장소 공통 룰)
└── demo13-flashlight/   # Unity 6 top-down 2D 액션 게임
    ├── CLAUDE.md        # demo13 코드/아키텍처 가이드
    ├── docs/            # 시스템별 기획 문서 (MASTER.md = 색인)
    └── Assets/ ...
```

## demo13-flashlight
- 상세 코드/아키텍처 가이드는 **`demo13-flashlight/CLAUDE.md`** 참조
- 기획 문서 색인은 **`demo13-flashlight/docs/MASTER.md`** 부터 시작
