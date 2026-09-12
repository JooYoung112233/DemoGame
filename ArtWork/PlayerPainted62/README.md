# 플레이어 애니풍 텍스처 — Unity 연결 완료

2026-09-12. 요청: 플레이어가 개성 없는 찰흙 같으니 텍스처부터 바꿔 확인하고 실제 Unity에 연결한다.

## 변경

- 본체/장비 Base Color 아틀라스 2장을 내장 image_gen으로 제작했다. 입력은 기존 SimpleHero_BaseColor / SimpleHero_Gear_BaseColor이고 최종 프롬프트는 `Prompts.json`에 있다. 별도 후처리 없이 생성 PNG를 복사했다.
- 적갈색 모자와 작은 천 배지, 청록색 작업복과 가슴 주머니/지퍼/주름, 짙은 청회색 바지, 가죽 부츠, 주황 목도리, 가방 봉제선/버클/보수 패치, 황토색 작은 장식으로 구성했다. 이 팔레트는 요청에 따른 첫 제작안이며 사용자 최종 색상 승인은 별도다.
- 원화는 `Assets/GPT/레퍼/캐릭터 레퍼런스.png`를 직접 확인했다. 낡은 작업복의 주름·마모와 어두운 바지를 참고했다. 현재 모델의 모자·기존 단순 얼굴·배낭 실루엣은 유지했다.
- 실제 자산은 `Assets/ChibiSurvivor/Player/Painted62/Textures` 및 `Materials`에 저장했다. 재질은 원본을 복제해 Base Map 참조만 바꿨다. GameLit, 조명, 노멀/마스크 설정, 틴트, 타일링 값은 유지한다.
- **SimpleHero.prefab의 스킨 재질 슬롯 17개를 후보 2개에 연결해 저장했다.** Resources/PlayerRig의 character3DPrefab이 이 프리팹을 참조한다. 현재 플레이어에도 연결되어 있고 다음 실행에도 유지된다. 원본 텍스처/재질 파일은 덮어쓰지 않았다.

## 자체 검수

- 같은 모델·포즈·조명의 A/B를 25° 정면/측면/후면, 62° 양방향에서 확인했다. 모자 이음새·가슴 포켓·팔/바지 주름·가방 앞뒤와 끈을 검수했다. 62°에서 모자와 재킷/가방 색 분리가 읽힌다. 사용자 최종 스타일 승인은 별도다.
- `Unity/Validation.json`: 17개 슬롯, 셰이더 오류 없음. 모델/UV/리그/클립 파일 변경 없음.
- `Unity/Saved.json`: 프리팹 저장 후 다시 로드해 모든 슬롯 연결과 Transform/스킨 메시/본/Animator 설정의 동일성을 검사했다. 전체 플레이 재시작을 실행한 것은 아니다.
- 검사 도구가 2D UI 재질의 없는 BaseMap을 조회해 오류를 냈던 부분은 HasProperty 가드로 수정했다. 콘솔 초기화 후 동일 검사 재실행 오류 0, 수정 전에 조회한 경고도 0이었다. 게임 코드 변경은 없다.
- `Unity/A_Original_*.png` / `B_Painted_*.png`: 실제 마을의 별도 검수 카메라 렌더. 파일의 Front/Back은 월드 카메라 yaw 0/180을 뜻하며 촬영 당시 캐릭터는 반대 방향을 보므로 **Back 파일이 얼굴 쪽**, Front 파일이 배낭 쪽이다. Top62는 실제 게임의 yaw0/pitch62 기준, TopBack62는 반대 yaw180이다. 일부 얼굴 쪽 사진에는 기존 가로등이 걸리며 추가 62°/측면에서 가림 없는 부분을 확인했다.

## 재현 / 되돌리기

- Unity MCP `run_script`: `Unity/prepare.json`은 마을 시작·캐릭터 위치 준비(현재 사용자가 플레이 중이면 호출하지 않는다). `review.json`은 임포트/A-B 검사 후 호출 전 재질 복원. `show.json`은 현재 플레이어 연결, `save.json`은 원본 프리팹 슬롯을 새 재질에 연결한다(이미 적용된 경우 중복 저장 방지로 중단).
- 원복은 Unity MCP로 SimpleHero.prefab의 Player_Painted → SimpleHero_Surface, PlayerGear_Painted → SimpleHero_Gear 재질 참조를 돌린다. 텍스처 파일 삭제나 메시 재생성이 필요하지 않다.
