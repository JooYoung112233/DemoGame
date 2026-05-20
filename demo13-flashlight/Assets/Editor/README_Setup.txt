===== Flashlight Prototype Setup Guide =====

1. Unity에서 이 프로젝트를 열면 URP 패키지가 자동 임포트됩니다 (최초 1회, 수 분 소요)

2. 패키지 임포트 완료 후:
   [Tools] > [Setup URP 2D Renderer] 클릭
   → URP 2D 파이프라인이 자동 설정됩니다

3. 씬 세팅:
   [Tools] > [Setup Flashlight Prototype Scene] 클릭
   → Player, 손전등, Fog of War, 샘플 환경이 자동 생성됩니다

4. Play 버튼 누르면 테스트 시작!

===== Controls =====
WASD  - 이동
Mouse - 손전등 조준
F     - 손전등 ON/OFF
T     - 낮/밤 전환 (테스트용)
Ctrl  - 웅크리기 (느리게 이동)

===== 테스트 항목 =====
[x] 부채꼴 손전등 (Light2D cone, 90도, 반경 7칸)
[x] 밤 기본 시야 (원형 2칸)
[x] 낮 시야 (원형 6칸)
[x] Fog of War (탐색/미탐색/현재 시야)
[x] 낮→밤 전환 (Global Light 디밍)
[x] 배터리 소모 + 잔량 낮을 때 깜빡임
[x] HUD (시간, 배터리, 조작법)
