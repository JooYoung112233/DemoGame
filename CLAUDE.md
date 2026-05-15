# Demo 프로젝트 가이드

## 개요
Phaser 3 기반 전투 데모 프로토타입. "전투 자체가 재미있는가?"를 검증하기 위한 프로토타입 모음.

## 배포
- **GitHub 저장소로만 관리** — 변경 작업 완료 후 git commit & push
- Netlify 사용하지 않음

## 데모 넘버링 규칙
- 폴더 형식: `demo{N}-{영문이름}/`
- 현재: demo1-line, demo2-atb, demo3-wave
- 새 모드 추가 시 번호를 순서대로 늘린다 (다음은 demo4-xxx)
- index.html에 해당 데모 카드도 추가할 것

## 프로젝트 구조
```
D:\Demo/
├── index.html                  # 메인 랜딩페이지 (데모 카드 목록)
├── package.json                # http-server, phaser 의존성
├── src/                        # Demo 1 원본 (demo1-line과 동일 구조)
├── demo1-line/                 # Demo 1: 라인 자동전투 (실시간)
├── demo2-atb/                  # Demo 2: ATB 턴 압축 전투
├── demo3-wave/                 # Demo 3: 웨이브 생존 (미완성)
└── CLAUDE.md                   # 이 파일
```

## 기술 스택
- **엔진**: Phaser 3 (Canvas 렌더링, Arcade Physics)
- **언어**: JavaScript (바닐라, 번들러 없음)
- **서버**: http-server (port 8080)
- **실행**: `npx http-server -p 8080`

## Demo 1: 라인 자동전투 (`demo1-line/`, `src/`)
실시간 자동전투. 아군과 적이 1D 라인(Y=440) 위에서 자동 전투.

### 핵심 파일
| 파일 | 역할 |
|------|------|
| `src/entities/Character.js` | 캐릭터 클래스 (이동, 공격, 렌더링, 충돌) |
| `src/scenes/SetupScene.js` | 파티 선택 화면 |
| `src/scenes/BattleScene.js` | 전투 메인 로직, 이펙트, UI |
| `src/scenes/ResultScene.js` | 전투 결과 화면 |
| `src/systems/CombatSystem.js` | 전투 업데이트 루프 |
| `src/systems/SkillSystem.js` | 캐릭터별 스킬 할당 |
| `src/systems/StatusEffect.js` | 상태이상 처리 |
| `src/data/characters.js` | 캐릭터 스탯 (CHAR_DATA) |
| `src/data/skills.js` | 스킬 정의 (SKILL_DATA) |
| `src/ui/HealthBar.js` | HP바 + 실드 표시 |
| `src/ui/DamagePopup.js` | 플로팅 데미지 숫자 |
| `src/main.js` | Phaser 설정 및 게임 인스턴스 |

### 캐릭터 데이터
**아군**: 탱커(tank), 도적(rogue), 사제(priest), 마법사(mage)
**적군**: 고블린(enemy_normal), 오거(enemy_tank), 아처(enemy_ranged)

### 배틀 레이아웃
- 화면: 1280x720, 지면: Y=460
- 아군: X=180부터 80px 간격 (좌→우)
- 적군: X=1100부터 80px 간격 (우→좌)

### 이동/충돌 시스템 (Character.js update())
- 1D X축 이동, 타겟과의 거리 > range이면 이동
- 적팀 충돌 차단: 반대 팀 유닛과 최소 28px 간격
- 같은 팀 겹침 방지: 아군 간 최소 30px 간격
- 원거리 유닛(range>100)은 근접 유닛 뒤에 위치

### 스킬
- 탱커: 도발 (Taunt)
- 도적: 출혈 (Bleed DoT)
- 사제: 힐 + 실드
- 마법사: 메테오 (AoE)

## Demo 2: ATB 턴 압축 전투 (`demo2-atb/`)
ATB 게이지 시스템. 게이지가 차면 자동 행동.

### 핵심 파일
| 파일 | 역할 |
|------|------|
| `src/entities/ATBCharacter.js` | ATB 캐릭터 (게이지 기반) |
| `src/systems/ATBSystem.js` | 턴 큐 관리, 게이지 업데이트 |
| `src/scenes/ATBBattleScene.js` | ATB 전투 씬 |
| `src/scenes/ATBSetupScene.js` | ATB 파티 선택 |
| `src/scenes/ATBResultScene.js` | ATB 결과 |

### 메커니즘
- 게이지가 1000에 도달하면 해당 캐릭터 턴 실행
- 속도(speed)에 비례해 게이지 충전
- 스킬은 턴 기반 쿨다운

## Demo 3: 웨이브 생존 (`demo3-wave/`) — 미완성
로그라이크 서바이벌. 데이터/UI 파일만 존재, 씬 로직 미구현.

### 존재하는 파일
- `src/data/characters.js`, `src/data/upgrades.js`
- `src/entities/WaveCharacter.js`
- `src/scenes/WaveSetupScene.js`, `WaveBattleScene.js`, `WaveResultScene.js`, `UpgradeScene.js`
- `src/systems/WaveManager.js`
- `src/ui/HealthBar.js`, `DamagePopup.js`

## 주의사항
- `src/`와 `demo1-line/src/`는 동일 구조의 복사본. Demo 1 수정 시 **양쪽 모두** 수정할 것
- 캐릭터는 스프라이트 없이 Graphics로 프로시저럴 렌더링
- Phaser는 CDN이 아닌 node_modules에서 로드
